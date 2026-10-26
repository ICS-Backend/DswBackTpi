
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Dsw2025Tpi.Domain.Entities;

namespace Dsw2025Tpi.Data.Helpers
{
    public static class DbContextExtensions
    {
        private static readonly JsonSerializerOptions CachedJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static void SeedDatabase(this Dsw2025TpiContext context)
        {
            if (!context.Customers.Any())
            {
                var customersJson = File.ReadAllText(
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "Sources",
                        "customers.json"
                    )
                );

                var customers = JsonSerializer.Deserialize<List<Customer>>(
                    customersJson,
                    CachedJsonOptions
                );

                if (customers != null && customers.Count > 0)
                {
                    context.Customers.AddRange(customers);
                    context.SaveChanges();
                }
            }
        }
    }
}
