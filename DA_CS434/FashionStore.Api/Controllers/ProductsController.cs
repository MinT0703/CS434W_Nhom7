using FashionStore.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db){ _db=db; }

    public record ProductListDto(int id, string name, string cat, int priceK, int oldK, int stock, double rating);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? q, [FromQuery] string[]? cats,
                                         [FromQuery] int? minK, [FromQuery] int? maxK,
                                         [FromQuery] string? sort, [FromQuery] int page=1, [FromQuery] int per=12)
    {
        // lấy SKU rẻ nhất & tồn kho tổng từ variants
        var baseQuery =
            from p in _db.Products
            join c in _db.Categories on p.DanhMucId equals c.DanhMucId
            join v in _db.Variants on p.SanPhamId equals v.SanPhamId
            where p.DangBan
            select new {
                p.SanPhamId, p.Ten, Cat = c.DuongDan,
                Price = v.Gia, Old = p.GiaGoc,
                Stock = v.SoLuongTon
            };

        if (!string.IsNullOrWhiteSpace(q))
            baseQuery = baseQuery.Where(x => x.Ten.Contains(q));

        if (cats is { Length: >0 })
            baseQuery = baseQuery.Where(x => cats.Contains(x.Cat));

        if (minK.HasValue) baseQuery = baseQuery.Where(x => x.Price >= (minK.Value * 1000m));
        if (maxK.HasValue) baseQuery = baseQuery.Where(x => x.Price <= (maxK.Value * 1000m));

        // gộp theo sản phẩm (lấy min price, sum stock)
        var grouped = baseQuery
            .GroupBy(x => new { x.SanPhamId, x.Ten, x.Cat, x.Old })
            .Select(g => new {
                id = g.Key.SanPhamId,
                name = g.Key.Ten,
                cat = g.Key.Cat,
                price = g.Min(x=>x.Price),
                old = g.Key.Old,
                stock = g.Sum(x=>x.Stock)
            });

        grouped = sort switch {
            "price-asc"  => grouped.OrderBy(x=>x.price),
            "price-desc" => grouped.OrderByDescending(x=>x.price),
            "name-asc"   => grouped.OrderBy(x=>x.name),
            "name-desc"  => grouped.OrderByDescending(x=>x.name),
            _            => grouped
        };

        var total = await grouped.CountAsync();
        var items = await grouped.Skip((page-1)*per).Take(per).ToListAsync();

        var result = items.Select(x => new ProductListDto(
            x.id, x.name, x.cat,
            priceK: (int)Math.Round(x.price/1000m),
            oldK:   (int)Math.Round((x.old>0?x.old:0)/1000m),
            stock:  x.stock,
            rating: 4.3 // demo: UI có sao, chưa có bảng rating riêng
        ));
        return Ok(new { total, items = result });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetOne(int id)
    {
        var p = await _db.Products.FindAsync(id);
        if (p == null) return NotFound();
        var cat = await _db.Categories.Where(c=>c.DanhMucId==p.DanhMucId).Select(c=>c.DuongDan).FirstOrDefaultAsync() ?? "";
        var variants = await _db.Variants.Where(v=>v.SanPhamId==id).ToListAsync();
        return Ok(new {
            id = p.SanPhamId,
            name = p.Ten,
            cat,
            variants = variants.Select(v => new {
                id = v.BienTheId, color = v.MauSac, size = v.KichCo,
                priceK = (int)Math.Round(v.Gia/1000m),
                stock = v.SoLuongTon, sku = v.SKU
            })
        });
    }
}
