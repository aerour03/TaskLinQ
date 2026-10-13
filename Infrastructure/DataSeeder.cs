using System;
using System.Collections.Generic;
using System.Linq;
using TaskLinQ.Models;

namespace TaskLinQ.Infrastructure
{
    /// <summary>
    /// Fills StoreDB with random but "interesting" data, so every one of the 25 LINQ
    /// questions returns something. Uses a fixed seed, so results are repeatable.
    /// </summary>
    public static class DataSeeder
    {
        public static void Seed(AppDbContext db, int randomSeed = 42)
        {
            if (db.Customers.Any() || db.Products.Any())
            {
                Console.WriteLine("Database already contains data. Seeding skipped.");
                return;
            }

            var rnd = new Random(randomSeed);
            var now = DateTime.Now;

            // ---------------------------------------------------------------
            // 1) Categories + Products
            // ---------------------------------------------------------------
            var catalog = new (string Category, string[] Names, double Min, double Max)[]
            {
                ("Laptops",    new[] { "Dell XPS 13", "MacBook Air M3", "Lenovo ThinkPad X1", "HP Spectre x360", "ASUS ZenBook 14" }, 800, 2200),
                ("Accessories",new[] { "Wireless Mouse", "Mechanical Keyboard", "USB-C Hub", "Laptop Sleeve", "Webcam HD", "Laptop Stand", "Phone Charger", "HDMI Cable" }, 8, 120),
                ("Phones",     new[] { "iPhone 15", "Galaxy S24", "Pixel 8", "OnePlus 12", "Xiaomi 14" }, 400, 1300),
                ("Monitors",   new[] { "LG UltraGear 27", "Dell U2723QE", "Samsung Odyssey G5", "BenQ PD2705Q" }, 150, 700),
                ("Audio",      new[] { "Sony WH-1000XM5", "AirPods Pro", "JBL Flip 6", "Bose QC45", "Logitech Z313" }, 30, 380),
                ("Gaming",     new[] { "PS5 Controller", "Xbox Controller", "Gaming Headset", "Racing Wheel" }, 40, 250),
                // Category with no sales at all (Q15)
                ("Gift Cards", new[] { "Gift Card 50", "Gift Card 100" }, 50, 100),
            };

            var categories = new List<Category>();
            var products = new List<Product>();

            foreach (var (catName, names, min, max) in catalog)
            {
                var cat = new Category { Name = catName };
                categories.Add(cat);

                for (int i = 0; i < names.Length; i++)
                {
                    int stock = rnd.Next(10, 150);

                    // Some low-stock items: used by Q13 and Q24 (Accessories with stock < 5)
                    if (catName == "Accessories" && i < 3) stock = rnd.Next(0, 5);
                    else if (rnd.NextDouble() < 0.10) stock = rnd.Next(0, 9);

                    var product = new Product
                    {
                        Name = names[i],
                        Price = Math.Round((decimal)(min + rnd.NextDouble() * (max - min)), 2),
                        Stock = stock,
                        Category = cat
                    };
                    cat.Products.Add(product);
                    products.Add(product);
                }
            }

            Product P(string name) => products.First(p => p.Name == name);

            var laptops = products.Where(p => p.Category.Name == "Laptops").ToList();

            // Products that are never ordered (Q4): all gift cards + 2 random others (not laptops)
            var neverOrdered = products.Where(p => p.Category.Name == "Gift Cards").ToList();
            neverOrdered.AddRange(
                products.Where(p => p.Category.Name != "Gift Cards" && p.Category.Name != "Laptops")
                        .OrderBy(_ => rnd.Next()).Take(2));

            var orderable = products.Except(neverOrdered).ToList();

            // Pairs that are often bought together (Q19)
            var bundles = new[]
            {
                new[] { P("Dell XPS 13"),  P("Wireless Mouse") },
                new[] { P("iPhone 15"),    P("Phone Charger") },
                new[] { P("LG UltraGear 27"), P("HDMI Cable") },
            }.Select(b => b.Where(orderable.Contains).ToArray()).Where(b => b.Length == 2).ToArray();

            // ---------------------------------------------------------------
            // 2) Customers
            // ---------------------------------------------------------------
            string[] firstNames = { "Ahmed", "Mohamed", "Sara", "Omar", "Nour", "Youssef", "Mona", "Khaled", "Laila", "Hassan",
                                    "Dina", "Tarek", "Salma", "Ali", "Hana", "Karim", "Reem", "Mostafa", "Yasmin", "Ibrahim" };
            string[] lastNames = { "Hassan", "Ali", "Mahmoud", "Ibrahim", "Saleh", "Farouk", "Nasser", "Adel", "Soliman", "Fathy",
                                   "Gamal", "Hamdy", "Mansour", "Zaki", "Samir" };
            string[] cities = { "Alexandria", "Cairo", "Giza", "Mansoura", "Tanta", "Aswan", "Luxor", "Port Said", "Suez", "Ismailia" };

            const int customerCount = 120;
            var customers = new List<Customer>();

            // Index layout (so every question has data):
            //   0-11   registered in the last 30 days (Q5)
            //   12-29  never ordered (Q10, Q18)
            //   30-35  heavy cancellers, registered over a year ago (Q6, Q25)
            //   36-38  bought every laptop (Q16)
            //   39+    regular customers
            for (int i = 0; i < customerCount; i++)
            {
                string first = firstNames[rnd.Next(firstNames.Length)];
                string last = lastNames[rnd.Next(lastNames.Length)];

                DateTime registered;
                if (i < 12) registered = now.AddDays(-rnd.Next(2, 30));
                else if (i >= 30 && i <= 35) registered = now.AddDays(-rnd.Next(400, 600));
                else registered = new DateTime(2024, 6, 1).AddDays(rnd.Next(0, (int)(now.AddDays(-35) - new DateTime(2024, 6, 1)).TotalDays));

                customers.Add(new Customer
                {
                    FullName = $"{first} {last}",
                    Email = $"{first}.{last}{i}@example.com".ToLower(),
                    City = cities[rnd.Next(cities.Length)],
                    RegisteredAt = registered
                });
            }

            // ---------------------------------------------------------------
            // 3) Orders + Items
            // ---------------------------------------------------------------
            decimal[] discounts = { 0m, 0m, 0m, 0m, 0.05m, 0.10m, 0.15m, 0.20m };

            OrderStatus PickStatus(DateTime date)
            {
                double ageDays = (now - date).TotalDays;
                int roll = rnd.Next(100);

                if (ageDays <= 30)   // recent: may still be pending/shipped
                    return roll < 35 ? OrderStatus.Pending
                         : roll < 60 ? OrderStatus.Shipped
                         : roll < 90 ? OrderStatus.Delivered
                         : OrderStatus.Cancelled;

                if (ageDays <= 60)
                    return roll < 20 ? OrderStatus.Shipped
                         : roll < 85 ? OrderStatus.Delivered
                         : OrderStatus.Cancelled;

                return roll < 78 ? OrderStatus.Delivered : OrderStatus.Cancelled;
            }

            Order MakeOrder(Customer c, OrderStatus? forcedStatus = null, List<Product>? forcedProducts = null)
            {
                double span = Math.Max(0.1, (now - c.RegisteredAt).TotalDays);
                var date = c.RegisteredAt.AddDays(rnd.NextDouble() * span);

                var order = new Order
                {
                    Customer = c,
                    OrderDate = date,
                    Status = forcedStatus ?? PickStatus(date)
                };

                var chosen = new List<Product>();
                if (forcedProducts != null)
                {
                    chosen.AddRange(forcedProducts);
                }
                else
                {
                    if (bundles.Length > 0 && rnd.NextDouble() < 0.30)
                        chosen.AddRange(bundles[rnd.Next(bundles.Length)]);

                    int extra = chosen.Count == 0 ? rnd.Next(1, 4) : rnd.Next(0, 2);
                    for (int k = 0; k < extra; k++)
                    {
                        var p = orderable[rnd.Next(orderable.Count)];
                        if (!chosen.Contains(p)) chosen.Add(p);
                    }
                }

                foreach (var p in chosen)
                {
                    order.Items.Add(new OrderItem
                    {
                        Order = order,
                        Product = p,
                        Quantity = rnd.Next(1, 5),
                        UnitPrice = p.Price,
                        Discount = discounts[rnd.Next(discounts.Length)]
                    });
                }

                return order;
            }

            var orders = new List<Order>();

            for (int i = 0; i < customers.Count; i++)
            {
                var c = customers[i];

                if (i >= 12 && i <= 29) continue;                 // never ordered

                if (i >= 30 && i <= 35)                           // heavy cancellers (3-5 cancelled orders)
                {
                    int cancelled = rnd.Next(3, 6);
                    for (int k = 0; k < cancelled; k++) orders.Add(MakeOrder(c, OrderStatus.Cancelled));
                    for (int k = 0; k < rnd.Next(1, 4); k++) orders.Add(MakeOrder(c));
                    continue;
                }

                if (i >= 36 && i <= 38)                           // bought every laptop
                    orders.Add(MakeOrder(c, OrderStatus.Delivered, laptops));

                int count = i < 12 ? rnd.Next(1, 4) : rnd.Next(1, 13);
                for (int k = 0; k < count; k++) orders.Add(MakeOrder(c));
            }

            // ---------------------------------------------------------------
            // 4) Reviews
            // ---------------------------------------------------------------
            string[] goodComments = { "Excellent quality!", "Totally worth the price.", "Fast delivery and great product.", "Highly recommended.", "Works perfectly." };
            string[] badComments = { "Not as described.", "Stopped working after a week.", "Too expensive for what it is.", "Average at best.", "Disappointed." };

            DateTime RandomReviewDate(Customer c)
            {
                double span = Math.Max(0.1, (now - c.RegisteredAt).TotalDays);
                return c.RegisteredAt.AddDays(rnd.NextDouble() * span);
            }

            var reviews = new List<Review>();

            // "Star" products: >= 10 reviews with average >= 4.5 (Q11)
            var starProducts = new[]
            {
                P("Dell XPS 13"), P("MacBook Air M3"), P("iPhone 15"),
                P("Sony WH-1000XM5"), P("Wireless Mouse"), P("LG UltraGear 27")
            }.Where(orderable.Contains).ToList();

            foreach (var p in orderable)
            {
                bool isStar = starProducts.Contains(p);
                int reviewCount = isStar ? rnd.Next(12, 21) : rnd.Next(0, 9);

                foreach (var c in customers.OrderBy(_ => rnd.Next()).Take(reviewCount))
                {
                    int rating = isStar
                        ? (rnd.NextDouble() < 0.75 ? 5 : 4)
                        : rnd.Next(1, 6);

                    reviews.Add(new Review
                    {
                        Product = p,
                        Customer = c,
                        Rating = rating,
                        Comment = rating >= 4 ? goodComments[rnd.Next(goodComments.Length)]
                                              : badComments[rnd.Next(badComments.Length)],
                        CreatedAt = RandomReviewDate(c)
                    });
                }
            }

            // ---------------------------------------------------------------
            // 5) Save
            // ---------------------------------------------------------------
            db.Categories.AddRange(categories);
            db.Customers.AddRange(customers);
            db.Orders.AddRange(orders);
            db.Reviews.AddRange(reviews);
            db.SaveChanges();

            Console.WriteLine("Seed completed:");
            Console.WriteLine($"  Categories : {categories.Count}");
            Console.WriteLine($"  Products   : {products.Count}");
            Console.WriteLine($"  Customers  : {customers.Count}");
            Console.WriteLine($"  Orders     : {orders.Count}");
            Console.WriteLine($"  OrderItems : {orders.Sum(o => o.Items.Count)}");
            Console.WriteLine($"  Reviews    : {reviews.Count}");
        }
    }
}
