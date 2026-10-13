
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TaskLinQ.Infrastructure;
using TaskLinQ.Models;

using var db = new AppDbContext();
db.Database.Migrate();
DataSeeder.Seed(db);

while (true)
{
    Console.WriteLine("\n==========================================");
    Console.WriteLine("           TASKLINQ QUERY MENU            ");
    Console.WriteLine("==========================================");
    Console.WriteLine("1. Delivered orders (OrderId, CustomerName, Total) ordered by Total descending\n");
    Console.WriteLine("2. Count orders per status and calculate percentage\n");
    Console.WriteLine("3. Products priced above their own category's average\n");
    Console.WriteLine("4. Products that have never been ordered\n");
    Console.WriteLine("5. Customers registered in the last 30 days with at least one order\n");
    Console.WriteLine("6. Customers with more than 2 cancelled orders\n");
    Console.WriteLine("7. Top 5 customers by total spending (Delivered orders only)\n");
    Console.WriteLine("8. Monthly report for 2025\n");
    Console.WriteLine("9. Revenue per category\n");
    Console.WriteLine("10. Customers who never placed an order, grouped by city\n");
    Console.WriteLine("11. Products with at least 10 reviews and average rating >= 4.5\n");
    Console.WriteLine("12. For each category, top 2 most expensive products\n");
    Console.WriteLine("13. Products whose stock is lower than total Pending quantity\n");
    Console.WriteLine("14. Customers whose total spending is above average\n");
    Console.WriteLine("15. Best-selling product in each category\n");
    Console.WriteLine("16. Customers who bought every product in 'Laptops'\n");
    Console.WriteLine("17. Reviews written by a customer for an unbought product\n");
    Console.WriteLine("18. All customers with order count (GroupJoin)\n");
    Console.WriteLine("19. Top 5 product pairs that appear together most often\n");
    Console.WriteLine("20. For each customer, name and most recent order with items\n");
    Console.WriteLine("21. Each customer's favorite category\n");
    Console.WriteLine("22. Rank products by revenue\n");
    Console.WriteLine("23. Search products with dynamic filters (text, category, price, stock, sorting, paging)\n");
    Console.WriteLine("24. Increase price by 10% for 'Accessories' with stock below 5 (ExecuteUpdate)\n");
    Console.WriteLine("25. Delete cancelled orders older than 1 year (ExecuteDelete)\n");
    Console.WriteLine("0. Exit");
    Console.Write("\nSelect an option (0-25): ");

    if (!int.TryParse(Console.ReadLine(), out int choice))
    {
        Console.WriteLine("\nInvalid input. Please enter a valid number.");
        continue;
    }

    if (choice == 0)
    {
        break;
    }

    Console.WriteLine($"\n--- Output for Question {choice} ---\n");

    switch (choice)
    {
        case 1:
            var deliveredOrders = db.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .Select(o => new
                {
                    OrderId = o.Id,
                    CustomerName = o.Customer.FullName,
                    Total = o.Items.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
                })
                .OrderByDescending(o => o.Total)
                .ToList();

            foreach (var order in deliveredOrders)
            {
                Console.WriteLine($"Order: {order.OrderId}, Customer: {order.CustomerName}, Total: {order.Total}\n");
            }
            break;

        case 2:
            int totalOrders = db.Orders.Count();

            var ordersByStatus = db.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            foreach (var item in ordersByStatus)
            {
                double percentage = totalOrders == 0 ? 0 : (double)item.Count / totalOrders * 100;
                Console.WriteLine($"Status: {item.Status}, Count: {item.Count}, Percentage: {percentage:F2}%\n");
            }
            break;

        case 3:
            var aboveAverageProducts = db.Products
                .Where(p => p.Price > db.Products.Where(other => other.CategoryId == p.CategoryId).Average(other => other.Price))
                .Select(p => new
                {
                    ProductName = p.Name,
                    p.Price,
                    CategoryName = p.Category.Name
                })
                .ToList();

            foreach (var product in aboveAverageProducts)
            {
                Console.WriteLine($"Product: {product.ProductName}, Price: {product.Price}, Category: {product.CategoryName}\n");
            }
            break;

        case 4:
            var neverOrderedProducts = db.Products
                .Where(p => !p.OrderItems.Any())
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price
                })
                .ToList();

            foreach (var product in neverOrderedProducts)
            {
                Console.WriteLine($"Id: {product.Id}, Product: {product.Name}, Price: {product.Price}\n");
            }
            break;

        case 5:
            DateTime thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var recentCustomers = db.Customers
                .Where(c => c.RegisteredAt >= thirtyDaysAgo && c.RegisteredAt <= DateTime.Now && c.Orders.Any())
                .Select(c => new
                {
                    c.Id,
                    c.FullName,
                    c.Email,
                    c.RegisteredAt
                })
                .ToList();

            foreach (var customer in recentCustomers)
            {
                Console.WriteLine($"Customer: {customer.FullName}, Email: {customer.Email}, Registered: {customer.RegisteredAt}\n");
            }
            break;

        case 6:
            var customersWithCancelledOrders = db.Customers
                .Select(c => new
                {
                    c.Id,
                    c.FullName,
                    CancelledCount = c.Orders.Count(o => o.Status == OrderStatus.Cancelled)
                })
                .Where(c => c.CancelledCount > 2)
                .ToList();

            foreach (var customer in customersWithCancelledOrders)
            {
                Console.WriteLine($"Customer: {customer.FullName}, Cancelled Orders: {customer.CancelledCount}\n");
            }
            break;

        case 7:
            var topCustomers = db.Customers
                .Select(c => new
                {
                    c.FullName,
                    OrderCount = c.Orders.Count(o => o.Status == OrderStatus.Delivered),
                    TotalSpent = c.Orders.Where(o => o.Status == OrderStatus.Delivered).SelectMany(o => o.Items).Sum(i => (decimal?)(i.Quantity * i.UnitPrice * (1 - i.Discount))) ?? 0m
                })
                .Where(c => c.OrderCount > 0)
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .Select(c => new
                {
                    c.FullName,
                    c.OrderCount,
                    c.TotalSpent,
                    AverageOrderValue = c.TotalSpent / c.OrderCount
                })
                .ToList();

            foreach (var customer in topCustomers)
            {
                Console.WriteLine($"Customer: {customer.FullName}, Orders: {customer.OrderCount}, Total Spent: {customer.TotalSpent:F2}, Average Order: {customer.AverageOrderValue:F2}\n");
            }
            break;

        case 8:
            var monthlyReport = db.OrderItems
                .Where(i => i.Order.Status == OrderStatus.Delivered && i.Order.OrderDate.Year == 2025)
                .GroupBy(i => new { i.Order.OrderDate.Year, i.Order.OrderDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    OrderCount = g.Select(i => i.OrderId).Distinct().Count(),
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
                })
                .OrderBy(r => r.Month)
                .ToList();

            foreach (var row in monthlyReport)
            {
                Console.WriteLine($"Year: {row.Year}, Month: {row.Month}, Orders: {row.OrderCount}, Revenue: {row.Revenue:F2}\n");
            }
            break;

        case 9:
            var revenuePerCategory = db.OrderItems
                .Where(i => i.Order.Status == OrderStatus.Delivered)
                .GroupBy(i => i.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,
                    Revenue = g.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount))
                })
                .OrderByDescending(r => r.Revenue)
                .ToList();

            foreach (var row in revenuePerCategory)
            {
                Console.WriteLine($"Category: {row.Category}, Revenue: {row.Revenue:F2}\n");
            }
            break;

        case 10:
            var noOrderCustomersByCity = db.Customers
                .Where(c => !c.Orders.Any())
                .GroupBy(c => c.City)
                .Select(g => new
                {
                    City = g.Key,
                    CustomerCount = g.Count()
                })
                .OrderByDescending(r => r.CustomerCount)
                .ToList();

            foreach (var row in noOrderCustomersByCity)
            {
                Console.WriteLine($"City: {row.City}, Customers without orders: {row.CustomerCount}\n");
            }
            break;

        case 11:
            var highlyRatedProducts = db.Products
                .Where(p => p.Reviews.Count() >= 10 && p.Reviews.Average(r => r.Rating) >= 4.5)
                .Select(p => new
                {
                    ProductName = p.Name,
                    CategoryName = p.Category.Name,
                    ReviewCount = p.Reviews.Count(),
                    AverageRating = p.Reviews.Average(r => r.Rating)
                })
                .OrderByDescending(p => p.AverageRating)
                .ToList();

            foreach (var product in highlyRatedProducts)
            {
                Console.WriteLine($"Product: {product.ProductName}, Category: {product.CategoryName}, Reviews: {product.ReviewCount}, Average Rating: {product.AverageRating:F2}\n");
            }
            break;

        case 12:
            var topProductsPerCategory = db.Categories
                .Select(c => new
                {
                    CategoryName = c.Name,
                    TopProducts = c.Products.OrderByDescending(p => p.Price).Take(2).Select(p => new { p.Name, p.Price }).ToList()
                })
                .ToList();

            foreach (var category in topProductsPerCategory)
            {
                Console.WriteLine($"Category: {category.CategoryName}\n");
                foreach (var product in category.TopProducts)
                {
                    Console.WriteLine($"    Product: {product.Name}, Price: {product.Price}\n");
                }
            }
            break;

        case 13:
            var lowStockProducts = db.Products
                .Select(p => new
                {
                    p.Name,
                    p.Stock,
                    PendingQuantity = p.OrderItems.Where(i => i.Order.Status == OrderStatus.Pending).Sum(i => (int?)i.Quantity) ?? 0
                })
                .Where(p => p.Stock < p.PendingQuantity)
                .ToList();

            foreach (var product in lowStockProducts)
            {
                Console.WriteLine($"Product: {product.Name}, Stock: {product.Stock}, Pending Quantity: {product.PendingQuantity}\n");
            }
            break;

        case 14:
            var totalsPerCustomer = db.Customers
                .Select(c => new
                {
                    c.Id,
                    c.FullName,
                    TotalSpent = c.Orders.Where(o => o.Status == OrderStatus.Delivered).SelectMany(o => o.Items).Sum(i => (decimal?)(i.Quantity * i.UnitPrice * (1 - i.Discount))) ?? 0m
                });

            decimal averageSpending = totalsPerCustomer.Average(c => c.TotalSpent);
            Console.WriteLine($"Average spending of all customers: {averageSpending:F2}\n");

            var aboveAverageCustomers = totalsPerCustomer
                .Where(c => c.TotalSpent > averageSpending)
                .OrderByDescending(c => c.TotalSpent)
                .ToList();

            foreach (var customer in aboveAverageCustomers)
            {
                Console.WriteLine($"Customer: {customer.FullName}, Total Spent: {customer.TotalSpent:F2}\n");
            }
            break;

        case 15:
            var bestSellers = db.Categories
                .Select(c => new
                {
                    CategoryName = c.Name,
                    BestProduct = c.Products.Select(p => new
                    {
                        p.Name,
                        Quantity = p.OrderItems.Where(i => i.Order.Status == OrderStatus.Delivered).Sum(i => (int?)i.Quantity) ?? 0
                    })
                    .Where(p => p.Quantity > 0)
                    .OrderByDescending(p => p.Quantity)
                    .ThenBy(p => p.Name)
                    .FirstOrDefault()
                })
                .ToList();

            foreach (var row in bestSellers)
            {
                if (row.BestProduct == null)
                {
                    Console.WriteLine($"Category: {row.CategoryName}, Best Product: No sales\n");
                }
                else
                {
                    Console.WriteLine($"Category: {row.CategoryName}, Best Product: {row.BestProduct.Name}, Quantity Sold: {row.BestProduct.Quantity}\n");
                }
            }
            break;

        case 16:
            var laptopBuyers = db.Customers
                .Where(c => db.Products.Where(p => p.Category.Name == "Laptops").All(p => c.Orders.Any(o => o.Status == OrderStatus.Delivered && o.Items.Any(i => i.ProductId == p.Id))))
                .Select(c => new
                {
                    c.Id,
                    c.FullName
                })
                .ToList();

            foreach (var customer in laptopBuyers)
            {
                Console.WriteLine($"Id: {customer.Id}, Customer: {customer.FullName}\n");
            }
            break;

        case 17:
            var reviewsWithoutPurchase = db.Reviews
                .Where(r => !db.OrderItems.Any(i => i.ProductId == r.ProductId && i.Order.CustomerId == r.CustomerId && i.Order.Status == OrderStatus.Delivered))
                .Select(r => new
                {
                    CustomerName = r.Customer.FullName,
                    ProductName = r.Product.Name,
                    r.Rating,
                    r.Comment
                })
                .ToList();

            foreach (var review in reviewsWithoutPurchase)
            {
                Console.WriteLine($"Customer: {review.CustomerName}, Product: {review.ProductName}, Rating: {review.Rating}, Comment: {review.Comment}\n");
            }
            break;

        case 18:
            var customerOrderCounts = db.Customers
                .GroupJoin(
                    db.Orders,
                    c => c.Id,
                    o => o.CustomerId,
                    (c, orders) => new
                    {
                        c.FullName,
                        OrderCount = orders.Count()
                    })
                .OrderByDescending(c => c.OrderCount)
                .ToList();

            foreach (var customer in customerOrderCounts)
            {
                Console.WriteLine($"Customer: {customer.FullName}, Orders: {customer.OrderCount}\n");
            }
            break;

        case 19:
            var topProductPairs = db.OrderItems
                .Join(
                    db.OrderItems,
                    a => a.OrderId,
                    b => b.OrderId,
                    (a, b) => new { A = a, B = b })
                .Where(x => x.A.ProductId < x.B.ProductId)
                .GroupBy(x => new
                {
                    IdA = x.A.ProductId,
                    IdB = x.B.ProductId,
                    ProductA = x.A.Product.Name,
                    ProductB = x.B.Product.Name
                })
                .Select(g => new
                {
                    g.Key.ProductA,
                    g.Key.ProductB,
                    TimesTogether = g.Select(x => x.A.OrderId).Distinct().Count()
                })
                .OrderByDescending(p => p.TimesTogether)
                .Take(5)
                .ToList();

            foreach (var pair in topProductPairs)
            {
                Console.WriteLine($"Pair: {pair.ProductA} + {pair.ProductB}, Times Together: {pair.TimesTogether}\n");
            }
            break;

        case 20:
            var latestOrders = db.Customers
                .Select(c => new
                {
                    c.FullName,
                    LatestOrder = c.Orders.OrderByDescending(o => o.OrderDate).Select(o => new
                    {
                        o.Id,
                        o.OrderDate,
                        Items = o.Items.Select(i => new
                        {
                            ProductName = i.Product.Name,
                            i.Quantity
                        }).ToList()
                    }).FirstOrDefault()
                })
                .ToList();

            foreach (var row in latestOrders)
            {
                Console.WriteLine($"Customer: {row.FullName}\n");
                if (row.LatestOrder == null)
                {
                    Console.WriteLine("    No orders\n");
                    continue;
                }
                Console.WriteLine($"    Latest Order: {row.LatestOrder.Id}, Date: {row.LatestOrder.OrderDate}\n");
                foreach (var orderItem in row.LatestOrder.Items)
                {
                    Console.WriteLine($"      Product: {orderItem.ProductName}, Quantity: {orderItem.Quantity}\n");
                }
            }
            break;

        case 21:
            var quantitiesByCustomerCategory = db.OrderItems
                .Where(i => i.Order.Status == OrderStatus.Delivered)
                .GroupBy(i => new
                {
                    i.Order.CustomerId,
                    CustomerName = i.Order.Customer.FullName,
                    CategoryName = i.Product.Category.Name
                })
                .Select(g => new
                {
                    g.Key.CustomerId,
                    g.Key.CustomerName,
                    g.Key.CategoryName,
                    TotalQuantity = g.Sum(i => i.Quantity)
                })
                .ToList();

            var favoriteCategories = quantitiesByCustomerCategory
                .GroupBy(x => new { x.CustomerId, x.CustomerName })
                .Select(g => g.OrderByDescending(x => x.TotalQuantity).ThenBy(x => x.CategoryName).First())
                .OrderBy(x => x.CustomerName)
                .ToList();

            foreach (var row in favoriteCategories)
            {
                Console.WriteLine($"Customer: {row.CustomerName}, Favorite Category: {row.CategoryName}, Total Quantity: {row.TotalQuantity}\n");
            }
            break;

        case 22:
            var productRanks = db.Products
                .Select(p => new
                {
                    ProductName = p.Name,
                    Revenue = p.OrderItems.Where(i => i.Order.Status == OrderStatus.Delivered).Sum(i => (decimal?)(i.Quantity * i.UnitPrice * (1 - i.Discount))) ?? 0m
                })
                .OrderByDescending(p => p.Revenue)
                .ToList()
                .Select((p, index) => new
                {
                    Rank = index + 1,
                    p.ProductName,
                    p.Revenue
                })
                .ToList();

            foreach (var row in productRanks)
            {
                Console.WriteLine($"Rank: {row.Rank}, Product: {row.ProductName}, Revenue: {row.Revenue:F2}\n");
            }
            break;

        case 23:
            var filter = new { Text = "", CategoryName = "", MinPrice = (decimal?)null, MaxPrice = (decimal?)null, InStockOnly = false, SortBy = "Name", Page = 1, PageSize = 5 };

            IQueryable<Product> productQuery = db.Products.Include(p => p.Category).Include(p => p.Reviews);

            if (!string.IsNullOrEmpty(filter.Text))
                productQuery = productQuery.Where(p => p.Name.Contains(filter.Text));

            if (!string.IsNullOrEmpty(filter.CategoryName))
                productQuery = productQuery.Where(p => p.Category.Name == filter.CategoryName);

            if (filter.MinPrice.HasValue)
                productQuery = productQuery.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                productQuery = productQuery.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.InStockOnly)
                productQuery = productQuery.Where(p => p.Stock > 0);

            int totalCount = productQuery.Count();

            productQuery = filter.SortBy switch
            {
                "Name" => productQuery.OrderBy(p => p.Name),
                "Price" => productQuery.OrderBy(p => p.Price),
                "Rating" => productQuery.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
                _ => productQuery.OrderBy(p => p.Id)
            };

            var pagedProducts = productQuery
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => new
                {
                    p.Name,
                    p.Price,
                    p.Stock,
                    Category = p.Category.Name,
                    AverageRating = p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : (double?)null
                })
                .ToList();

            Console.WriteLine($"Total Count matching filter: {totalCount}\n");
            foreach (var p in pagedProducts)
            {
                Console.WriteLine($"Product: {p.Name}, Price: {p.Price}, Stock: {p.Stock}, Category: {p.Category}, Rating: {p.AverageRating?.ToString("F2") ?? "No reviews"}\n");
            }
            break;

        case 24:
            int updatedRows = db.Products
                .Where(p => p.Category.Name == "Accessories" && p.Stock < 5)
                .ExecuteUpdate(s => s.SetProperty(p => p.Price, p => p.Price * 1.1m));

            Console.WriteLine($"Successfully updated prices for {updatedRows} products.\n");
            break;

        case 25:
            DateTime oneYearAgo = DateTime.Now.AddYears(-1);
            int deletedRows = db.Orders
                .Where(o => o.Status == OrderStatus.Cancelled && o.OrderDate < oneYearAgo)
                .ExecuteDelete();

            Console.WriteLine($"Successfully deleted {deletedRows} old cancelled orders.\n");
            break;

        default:
            Console.WriteLine("Invalid option. Please select a number between 0 and 25.\n");
            break;
    }

    Console.WriteLine("\nPress any key to continue...");
    Console.ReadKey();
    Console.Clear();
}
