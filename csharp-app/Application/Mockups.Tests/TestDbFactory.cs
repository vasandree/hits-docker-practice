using Microsoft.EntityFrameworkCore;
using Mockups.Storage;

namespace Mockups.Tests;

public static class TestDbFactory
{
    public static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
