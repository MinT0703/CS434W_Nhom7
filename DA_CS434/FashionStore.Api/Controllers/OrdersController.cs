using System.Security.Claims;
using FashionStore.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    public OrdersController(AppDbContext db){ _db=db; }

    public record CartItemDto(int variantId, string name, int qty, int priceK);
    public record CheckoutDto(List<CartItemDto> items, string? coupon, string? receiverName, string? receiverPhone, string? receiverAddress);

    [Authorize]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutDto dto)
    {
        if (dto.items is null || dto.items.Count == 0) return BadRequest("Giỏ hàng trống.");

        // Lấy userId từ token
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr)) return Unauthorized();
        var userId = int.Parse(userIdStr);

        // Tính tạm tính
        decimal sub = 0m;
        foreach (var it in dto.items)
        {
            var v = await _db.Variants.FindAsync(it.variantId);
            if (v == null) return BadRequest($"Biến thể {it.variantId} không tồn tại.");
            if (it.qty <= 0 || it.qty > v.SoLuongTon) return BadRequest($"Số lượng không hợp lệ cho {v.SKU}.");

            // dùng giá ở client (k) nhưng xác thực lại bằng DB
            var price = v.Gia; // VND
            if ((int)Math.Round(price/1000m) != it.priceK)
                return BadRequest("Giá sản phẩm đã thay đổi, vui lòng tải lại giỏ hàng.");

            sub += price * it.qty;
        }

        // Coupon
        decimal discount = 0m;
        Coupon? cp = null;
        if (!string.IsNullOrWhiteSpace(dto.coupon))
        {
            var now = DateTime.UtcNow;
            cp = await _db.Coupons.FirstOrDefaultAsync(c =>
                c.MaCode == dto.coupon && (c.HetHanLuc == null || c.HetHanLuc > now));
            if (cp != null)
                discount = cp.Kieu == 1 ? Math.Round(sub * (cp.GiaTri/100m)) : cp.GiaTri;
        }

        decimal ship = 30000m; // 30k
        decimal total = Math.Max(0m, sub - discount + ship);

        // Tạo order
        var order = new Order {
            NguoiDungId = userId,
            PhieuGiamGiaId = cp?.PhieuGiamGiaId,
            TrangThai = 0,
            TamTinh = sub,
            GiamGia = discount,
            PhiVanChuyen = ship,
            TongThanhToan = total,
            TenNhan = dto.receiverName,
            SDTNhan = dto.receiverPhone,
            DiaChiNhan = dto.receiverAddress,
            NgayTao = DateTime.UtcNow
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // Order items + trừ kho
        foreach (var it in dto.items)
        {
            var v = await _db.Variants.FindAsync(it.variantId);
            v!.SoLuongTon -= it.qty;

            _db.OrderItems.Add(new OrderItem {
                DonHangId = order.DonHangId,
                BienTheId = v.BienTheId,
                TenSanPham = it.name,
                ThuocTinh = $"{v.MauSac}/{v.KichCo}",
                SoLuong = it.qty,
                DonGia = v.Gia,
                ThanhTien = v.Gia * it.qty
            });
        }
        await _db.SaveChangesAsync();

        return Ok(new { orderId = order.DonHangId, totalVnd = total });
    }
}
