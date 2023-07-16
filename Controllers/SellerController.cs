using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Data;
using Project.Models;
using Project.Service;
using System.Data;

namespace Project.Controllers
{
	[Authorize(Roles = "Seller")]

	public class SellerController : Controller
	{
		private readonly ShopContext _shopContext;
		private readonly MailSettings _mailSettings;

		public SellerController(ShopContext shopContext)
		{
			_shopContext = shopContext;
		}


		public IActionResult Index()
		{

			return View();
		}

		public IActionResult ViewAll()
		{
			List<Bill> bills = _shopContext.Bills.ToList()
				.Where(bill => int.TryParse(bill.BillStatus, out int billStatus) && billStatus < 3)
				.ToList();

			return View(bills);
		}


		public IActionResult ViewOrder()
		{
			var currentUser = HttpContext.User;

			if (currentUser.Identity.IsAuthenticated)
			{
				List<Bill> bills = _shopContext.Bills
					.Where(bill => bill.Email == currentUser.Identity.Name)
					.ToList();

				bills = bills.Where(bill => int.TryParse(bill.BillStatus, out int billStatus) && billStatus < 3)
					.ToList();

				return View(bills);
			}
			else
			{
				return RedirectToAction("Index");
			}
		}





		public IActionResult DetailBill(int billId)
		{

			var bill = _shopContext.Bills.
					Include(b => b.BillDetails)
					.ThenInclude(bd => bd.Product)
					.FirstOrDefault(b => b.BillId == billId);
			if (bill != null)
			{
				if (TempData.ContainsKey("OutOfStockFlag"))
				{
					ViewBag.OutOfStockMessage = "Hàng đã hết. Bạn có muốn xóa không?";
					TempData.Remove("OutOfStockFlag");
				}
				else
				{
					ViewBag.OutOfStockMessage = "Bạn không thể khôi phục được đơn hàng đã xóa";

				}

				return View(bill);

			}
			else
			{
				return Redirect("Index");
			}

		}
		

		public IActionResult Delete(int billId)
		{
			var bill = _shopContext.Bills.Include(b => b.BillDetails).FirstOrDefault(b => b.BillId == billId);
			if (bill != null)
			{
				string fromEmail = "huongdl40@gmail.com";
				string toEmail = bill.Email;
				string subject = "Xác nhận đơn hàng";
				string body = @"
                                    <html>
                                    <head>
                                        <style>
                                            
                                        </style>
                                    </head>
                                    <body>
                                        <h1>Đơn hàng của bạn đã bị hủy do gặp vấn đề. Chúng tôi vô cùng xin lỗi về vấn đề này và sẽ sớm hoàn lại tiền cho bạn.</h1>
                                        <p>Thông tin đơn hàng:</p>
                                        <ul>
                                            <li>Tên người dùng: " + bill.Email + @"</li>                                                                                     
                                             <li>Tổng giá tiền: " + bill.TotalPrice + @".000vnd</li>
                                        </ul>
                                    </body>
                                    </html>";
				string gmail = "huongdl40@gmail.com";
				string password = "gepcdegcpjjzceke";
				var sendResult = SendMailConfirmOrder.SendGmail(fromEmail, toEmail, subject, body, gmail, password).GetAwaiter().GetResult();
				if (sendResult == "gui email thanh cong")
				{
					// Xóa tất cả các đối tượng BillDetail liên quan
					_shopContext.BillDetails.RemoveRange(bill.BillDetails);
					_shopContext.Bills.Remove(bill);
					_shopContext.SaveChanges();
				}
				else
				{
					// Xử lý khi gửi email thất bại
					// ...
				}

			}

			return RedirectToAction("ViewAll", "Seller");

		}
		public IActionResult ProcessBill(int billId)
		{


			var bill = _shopContext.Bills
		.Include(b => b.BillDetails)
			.ThenInclude(bd => bd.Product)
				.ThenInclude(p => p.ProductDetails)
		.FirstOrDefault(b => b.BillId == billId);

			if (bill != null)
			{
				if (bill.BillStatus == "0")
				{
					foreach (var billDetail in bill.BillDetails)
					{
						var product = billDetail.Product;
						var productDetail = product.ProductDetails
							.FirstOrDefault(pd => pd.color == billDetail.color && pd.size == billDetail.size);

						if (productDetail != null && productDetail.quantity >= billDetail.quantity)
						{

							productDetail.quantity -= billDetail.quantity;


							_shopContext.SaveChanges();
						}
						else
						{
							TempData["OutOfStockFlag"] = true;
							return RedirectToAction("DetailBill", new { billId });
						}
					}

					int billStatus = int.Parse(bill.BillStatus) + 1;
					bill.BillStatus = billStatus.ToString();
					_shopContext.SaveChanges();

					string fromEmail = "huongdl40@gmail.com";
					string toEmail = bill.Email;
					string subject = "Xác nhận đơn hàng";
					string body = @"
                <html>
                <head>
                    <style>
                        
                    </style>
                </head>
                <body>
                    <h1>Đơn hàng của bạn đã được xác nhận.</h1>
                    <p>Thông tin đơn hàng:</p>
                    <ul>
                        <li>Tên người dùng: " + bill.Email + @"</li>                                                                                     
                        <li>Tổng giá tiền: " + bill.TotalPrice + @".000vnd</li>
                    </ul>
                </body>
                </html>";
					string gmail = "huongdl40@gmail.com";
					string password = "gepcdegcpjjzceke";

					var sendResult = SendMailConfirmOrder.SendGmail(fromEmail, toEmail, subject, body, gmail, password).GetAwaiter().GetResult();
					if (sendResult != "gui email thanh cong")
					{
						// Handle email sending failure
						// ...
					}
				}
				else
				{
                    if (int.Parse(bill.BillStatus) < 3)
                    {

                        int billStatus = int.Parse(bill.BillStatus) + 1;
                        bill.BillStatus = billStatus.ToString();
                        _shopContext.SaveChanges();
                    }
                    else
                    {
                        return NotFound();
                    }
                }
			}


			return RedirectToAction("ViewOrder");
		}
		public IActionResult ProcessBillAll(int billId)
		{


			var bill = _shopContext.Bills
		.Include(b => b.BillDetails)
			.ThenInclude(bd => bd.Product)
				.ThenInclude(p => p.ProductDetails)
		.FirstOrDefault(b => b.BillId == billId);

			if (bill != null)
			{
				if (bill.BillStatus == "0")
				{
					foreach (var billDetail in bill.BillDetails)
					{
						var product = billDetail.Product;
						var productDetail = product.ProductDetails
							.FirstOrDefault(pd => pd.color == billDetail.color && pd.size == billDetail.size);

						if (productDetail != null && productDetail.quantity >= billDetail.quantity)
						{

							productDetail.quantity -= billDetail.quantity;


							_shopContext.SaveChanges();
						}
						else
						{
							TempData["OutOfStockFlag"] = true;
							return RedirectToAction("DetailBill", new { billId });
						}
					}

					int billStatus = int.Parse(bill.BillStatus) + 1;
					bill.BillStatus = billStatus.ToString();
					_shopContext.SaveChanges();

					string fromEmail = "huongdl40@gmail.com";
					string toEmail = bill.Email;
					string subject = "Xác nhận đơn hàng";
					string body = @"
                <html>
                <head>
                    <style>
                        
                    </style>
                </head>
                <body>
                    <h1>Đơn hàng của bạn đã được xác nhận.</h1>
                    <p>Thông tin đơn hàng:</p>
                    <ul>
                        <li>Tên người dùng: " + bill.Email + @"</li>                                                                                     
                        <li>Tổng giá tiền: " + bill.TotalPrice + @".000vnd</li>
                    </ul>
                </body>
                </html>";
					string gmail = "huongdl40@gmail.com";
					string password = "gepcdegcpjjzceke";

					var sendResult = SendMailConfirmOrder.SendGmail(fromEmail, toEmail, subject, body, gmail, password).GetAwaiter().GetResult();
					if (sendResult != "gui email thanh cong")
					{
						// Handle email sending failure
						// ...
					}
				}
				else
				{
					if (int.Parse(bill.BillStatus) <3)
					{

						int billStatus = int.Parse(bill.BillStatus) + 1;
						bill.BillStatus = billStatus.ToString();
						_shopContext.SaveChanges();
					}
					else
					{
						return NotFound();
					}
				}
			}



			return RedirectToAction("ViewAll");
		}


	}
}
