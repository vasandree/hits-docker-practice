#!/usr/bin/env python3
import argparse
import json
import os
import random
import ssl
import statistics
import threading
import time
from concurrent.futures import ThreadPoolExecutor
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Dict, List, Tuple
from urllib import error, request


@dataclass
class EndpointScenario:
    method: str
    path: str
    weight: int


def build_scenario() -> List[EndpointScenario]:
    return [
        EndpointScenario("GET", "/Menu", 60),
        EndpointScenario("GET", "/analytics/summary", 15),
        EndpointScenario("GET", "/analytics/usage", 15),
        EndpointScenario("GET", "/analytics/errors", 10),
    ]


def weighted_choice(scenarios: List[EndpointScenario]) -> EndpointScenario:
    total = sum(item.weight for item in scenarios)
    pivot = random.randint(1, total)
    running = 0
    for item in scenarios:
        running += item.weight
        if pivot <= running:
            return item
    return scenarios[-1]


def percentile(values: List[float], percent: float) -> float:
    if not values:
        return 0.0
    if percent <= 0:
        return min(values)
    if percent >= 100:
        return max(values)
    values_sorted = sorted(values)
    k = (len(values_sorted) - 1) * (percent / 100.0)
    f = int(k)
    c = min(f + 1, len(values_sorted) - 1)
    if f == c:
        return values_sorted[int(k)]
    d0 = values_sorted[f] * (c - k)
    d1 = values_sorted[c] * (k - f)
    return d0 + d1


def worker(base_url: str, stop_at: float, timeout: float, scenarios: List[EndpointScenario],
           results: List[float], status_counts: Dict[int, int], error_counts: Dict[str, int],
           lock: threading.Lock, ssl_context: ssl.SSLContext | None) -> None:
    while time.monotonic() < stop_at:
        scenario = weighted_choice(scenarios)
        url = f"{base_url}{scenario.path}"
        req = request.Request(url, method=scenario.method)
        start = time.perf_counter()
        try:
            with request.urlopen(req, timeout=timeout, context=ssl_context) as response:
                response.read()
                duration_ms = (time.perf_counter() - start) * 1000
                with lock:
                    results.append(duration_ms)
                    status_counts[response.status] = status_counts.get(response.status, 0) + 1
        except error.HTTPError as exc:
            duration_ms = (time.perf_counter() - start) * 1000
            with lock:
                results.append(duration_ms)
                status_counts[exc.code] = status_counts.get(exc.code, 0) + 1
        except Exception as exc:  # pragma: no cover - runtime telemetry
            with lock:
                error_key = exc.__class__.__name__
                error_counts[error_key] = error_counts.get(error_key, 0) + 1


def run_load_test(
    base_url: str,
    duration_s: int,
    concurrency: int,
    timeout: float,
    ssl_context: ssl.SSLContext | None,
) -> Tuple[Dict[str, float], Dict[int, int], Dict[str, int]]:
    scenarios = build_scenario()
    stop_at = time.monotonic() + duration_s
    results: List[float] = []
    status_counts: Dict[int, int] = {}
    error_counts: Dict[str, int] = {}
    lock = threading.Lock()

    with ThreadPoolExecutor(max_workers=concurrency) as executor:
        for _ in range(concurrency):
            executor.submit(
                worker,
                base_url,
                stop_at,
                timeout,
                scenarios,
                results,
                status_counts,
                error_counts,
                lock,
                ssl_context,
            )

    total_requests = sum(status_counts.values()) + sum(error_counts.values())
    success_requests = sum(count for code, count in status_counts.items() if 200 <= code < 400)
    error_requests = total_requests - success_requests
    rps = total_requests / duration_s if duration_s else 0

    metrics = {
        "total_requests": total_requests,
        "success_requests": success_requests,
        "error_requests": error_requests,
        "error_rate": (error_requests / total_requests) * 100 if total_requests else 0,
        "rps": rps,
        "latency_avg_ms": statistics.mean(results) if results else 0.0,
        "latency_p50_ms": percentile(results, 50),
        "latency_p90_ms": percentile(results, 90),
        "latency_p95_ms": percentile(results, 95),
        "latency_p99_ms": percentile(results, 99),
        "latency_max_ms": max(results) if results else 0.0,
    }
    return metrics, status_counts, error_counts


def save_results(
    output_dir: str,
    metrics: Dict[str, float],
    status_counts: Dict[int, int],
    error_counts: Dict[str, int],
    args: argparse.Namespace,
) -> str:
    os.makedirs(output_dir, exist_ok=True)
    timestamp = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    filename = f"load_test_{timestamp}.json"
    payload = {
        "timestamp_utc": timestamp,
        "base_url": args.base_url,
        "duration_s": args.duration,
        "concurrency": args.concurrency,
        "timeout_s": args.timeout,
        "metrics": metrics,
        "status_counts": {str(code): count for code, count in status_counts.items()},
        "error_counts": error_counts,
    }
    output_path = os.path.join(output_dir, filename)
    with open(output_path, "w", encoding="utf-8") as file:
        json.dump(payload, file, indent=2, sort_keys=True)
    return output_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Simple load test runner for Mockups app.")
    parser.add_argument("--base-url", default="http://localhost:8080", help="Base URL of the app")
    parser.add_argument("--duration", type=int, default=30, help="Test duration in seconds")
    parser.add_argument("--concurrency", type=int, default=20, help="Number of parallel workers")
    parser.add_argument("--timeout", type=float, default=5.0, help="Request timeout in seconds")
    parser.add_argument(
        "--insecure",
        action="store_true",
        help="Disable TLS certificate verification (useful for local HTTPS testing)",
    )
    parser.add_argument(
        "--output-dir",
        default=os.path.join(os.path.dirname(__file__), "results"),
        help="Directory for saving load test results",
    )
    args = parser.parse_args()

    ssl_context = None
    if args.insecure:
        ssl_context = ssl._create_unverified_context()

    metrics, status_counts, error_counts = run_load_test(
        args.base_url,
        args.duration,
        args.concurrency,
        args.timeout,
        ssl_context,
    )

    print("Load test summary")
    print("=================")
    for key, value in metrics.items():
        if isinstance(value, float):
            print(f"{key}: {value:.2f}")
        else:
            print(f"{key}: {value}")
    print("\nStatus codes:")
    for code, count in sorted(status_counts.items()):
        print(f"  {code}: {count}")
    if error_counts:
        print("\nErrors:")
        for error_key, count in sorted(error_counts.items()):
            print(f"  {error_key}: {count}")

    output_path = save_results(args.output_dir, metrics, status_counts, error_counts, args)
    print(f"\nSaved results to {output_path}")


if __name__ == "__main__":
    main()
