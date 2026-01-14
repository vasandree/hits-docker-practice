# Load testing scenario for Mockups (.NET)

## Scenario outline

The scenario focuses on the most common read-only endpoints so it can run without authentication:

| Endpoint | Method | Weight | Purpose |
| --- | --- | --- | --- |
| `/Menu` | GET | 60% | Main menu listing page |
| `/analytics/summary` | GET | 15% | Overall statistics |
| `/analytics/usage` | GET | 15% | Endpoint usage statistics |
| `/analytics/errors` | GET | 10% | Error statistics |

The load test runs for a fixed duration with a configurable number of concurrent workers. Each worker randomly selects an endpoint according to the weights and measures latency per request.

## Running the load test

1. Start the application and database.
2. Run the load test script from this folder (results are saved under `results/`):

```bash
python3 load_test.py --base-url http://localhost:8080 --duration 60 --concurrency 30
```

You can override the output directory with `--output-dir` if needed.
If you target local HTTPS endpoints with a self-signed certificate, add `--insecure` to disable TLS verification.

The script reports:
- **RPS** (requests per second)
- **Latency** (average, p50, p90, p95, p99, max)
- **Error rate** (percentage of non-2xx/3xx responses)

## Notes

- The script uses only Python standard library modules.
- If you need write flows (registration, cart, orders), extend the scenario with authenticated POST requests and cookies/session handling.
