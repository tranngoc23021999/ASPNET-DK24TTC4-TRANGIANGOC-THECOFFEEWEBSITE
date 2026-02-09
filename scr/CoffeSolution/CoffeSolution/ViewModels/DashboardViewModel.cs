using System.Collections.Generic;
using CoffeSolution.Models.Entities;

namespace CoffeSolution.ViewModels;

public class DashboardViewModel
{
    public decimal TotalRevenueToday { get; set; }
    public int TotalOrdersToday { get; set; }
    public int TotalProducts { get; set; }
    public int ActiveStores { get; set; }
    public List<Order> RecentOrders { get; set; } = new List<Order>();
    public List<TopProductViewModel> TopSellingProducts { get; set; } = new List<TopProductViewModel>();
}

public class TopProductViewModel
{
    public string ProductName { get; set; } = null!;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}
