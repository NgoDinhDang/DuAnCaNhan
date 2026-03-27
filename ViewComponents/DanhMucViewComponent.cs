using Microsoft.AspNetCore.Mvc;
using STOREBOOKS.Data; // đúng namespace của bạn
using STOREBOOKS.Models; // đúng namespace của bạn
using Microsoft.EntityFrameworkCore;

public class DanhMucViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _context;

    public DanhMucViewComponent(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var danhMucs = await _context.DanhMuc.ToListAsync();
        return View(danhMucs);
    }
}


