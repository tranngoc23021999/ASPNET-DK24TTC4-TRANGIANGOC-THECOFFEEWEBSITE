using CoffeSolution.Data;
using CoffeSolution.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeSolution.Services;

public interface IShiftService
{
    Task<Shift?> GetOpenShiftAsync(int staffId);
    Task<Shift> StartShiftAsync(int staffId, int storeId, decimal startingCash, string? note);
    Task<Shift> EndShiftAsync(int shiftId, decimal actualCash);
    Task<bool> HasOpenShiftAsync(int staffId);
}

public class ShiftService : IShiftService
{
    private readonly ApplicationDbContext _context;

    public ShiftService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Shift?> GetOpenShiftAsync(int staffId)
    {
        return await _context.Shifts
            .Include(s => s.Store)
            .FirstOrDefaultAsync(s => s.StaffId == staffId && s.Status == "Open");
    }

    public async Task<bool> HasOpenShiftAsync(int staffId)
    {
        return await _context.Shifts
            .AnyAsync(s => s.StaffId == staffId && s.Status == "Open");
    }

    public async Task<Shift> StartShiftAsync(int staffId, int storeId, decimal startingCash, string? note)
    {
        // Check if already has open shift
        var existingShift = await GetOpenShiftAsync(staffId);
        if (existingShift != null)
        {
            throw new InvalidOperationException("Nhân viên đang có ca làm việc chưa kết thúc.");
        }

        var shift = new Shift
        {
            StaffId = staffId,
            StoreId = storeId,
            StartingCash = startingCash,
            StartTime = DateTime.Now,
            Status = "Open",
            Note = note
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();
        return shift;
    }

    public async Task<Shift> EndShiftAsync(int shiftId, decimal actualCash)
    {
         var shift = await _context.Shifts
            .Include(s => s.Orders)
            .FirstOrDefaultAsync(s => s.Id == shiftId);
            
        if (shift == null) throw new ArgumentException("Ca làm việc không tồn tại");
        if (shift.Status != "Open") throw new InvalidOperationException("Ca làm việc đã kết thúc");

        // Calculate revenue
        shift.TotalRevenueCash = shift.Orders.Where(o => o.PaymentMethod == "Cash" && o.Status == "Completed").Sum(o => o.TotalAmount);
        shift.TotalRevenueCard = shift.Orders.Where(o => o.PaymentMethod == "Card" && o.Status == "Completed").Sum(o => o.TotalAmount);
        shift.TotalRevenueTransfer = shift.Orders.Where(o => o.PaymentMethod == "Transfer" && o.Status == "Completed").Sum(o => o.TotalAmount);
        
        shift.EndTime = DateTime.Now;
        shift.EndingCash = actualCash; // Tiền thực tế đếm được
        shift.Status = "Closed";

        // Logic check: Expected Cash = StartingCash + TotalRevenueCash
        
        await _context.SaveChangesAsync();
        return shift;
    }
}
