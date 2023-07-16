using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Project.Data;
using Project.Models;
using System.Collections.Generic;

namespace Project.Controllers
{
    public class ProductController : Controller
    {

        private readonly ILogger<ProductController> _logger;
        private readonly ShopContext _shopContext;
        private readonly SignInManager<IdentityUser> _signInManager;
        public ProductController(ILogger<ProductController> logger, ShopContext ct, SignInManager<IdentityUser> s1)
        {
            _shopContext = ct;
            _logger = logger;
            _signInManager = s1;
        }


        public IActionResult Index(int id, bool gender, int mode)
        {

            List<SubCategory> subCategories = new List<SubCategory>();
            var list = (from p in _shopContext.Products
                        where p.SubCategoryID == id
                        select p).ToList();
            var subCate = (from c in _shopContext.SubCategory
                           where c.SubCategoryId == id
                           select c).FirstOrDefault();
            var cateGory = (from c in _shopContext.Categories
                            where c.CategoryId == subCate.CateogoryId
                            select c).First();
            if (cateGory != null)
            {
                var e = _shopContext.Entry(cateGory);
                e.Collection(c => c.SubCategories).Load();
                subCategories = cateGory.SubCategories;
            }
            subCategories.Sort((x, y) => x.SubCategoryId.CompareTo(y.SubCategoryId));
            ViewData["id"] = id;
            ViewData["gender"] = gender;
            ViewData["listSubCate"] = subCategories;
            ViewData["subCate"] = subCate;
            ViewData["gender"] = gender;

            if (mode == 1)
            {
                list.Sort((x, y) => (x.ProductPrice - x.Discount).CompareTo(y.ProductPrice - y.Discount));
            }
            if (mode == 2)
            {
                list.Sort((x, y) => (y.ProductPrice - y.Discount).CompareTo(x.ProductPrice - x.Discount));
            }
            return View(list);

        }

        public IActionResult DetailProduct(int id)
        {
            var product = _shopContext.Products.Include(p => p.Feedbacks).Where(p => p.ProductId == id).FirstOrDefault();



            if (product != null)
            {
                ViewData["productId"] = product.ProductId;
                ViewData["image"] = product.ImageMain;
                var e = _shopContext.Entry(product);
                e.Collection(c => c.ImageProducts).Load();
                e.Collection(c => c.ProductDetails).Load();
            }
            else
            {
                return RedirectToAction("Index", "Home", new { mode = "EDetailProduct" });
            }

            string size = "";
            string color = "";
            var list = new List<string>();
            foreach (var l in product.ProductDetails)
            {
                if (l.quantity > 0)
                {
                    size = l.size;
                    color = l.color;
                    break;
                }
            }

            foreach (var l in product.ProductDetails)
            {
                if (l.color.Equals(color) && l.quantity == 0)
                {
                    list.Add(l.size);
                }
            }
            ViewData["list"] = list;
            ViewData["c"] = color;
            ViewData["s"] = size;

            return View(product);
        }

        public IActionResult Search(string name)
        {
            if (string.IsNullOrEmpty(name))
                return View(null);

            _logger.LogError(_shopContext.Products.Where(c => c.ProductName.Contains(name)).ToList().Count() + "");

            return View("Index", _shopContext.Products.Where(c => c.ProductName.Contains(name)).ToList());
        }


        public IActionResult AddWishList(int id, int choice)
        {
            if (!_signInManager.IsSignedIn(User))
            {

                string? cookieValue = Request.Cookies["wish"];
                if (cookieValue == null)
                {
                    var option = new CookieOptions()
                    {
                        Expires = DateTime.Now.AddDays(90)
                    };
                    Response.Cookies.Append("wish", id + ",", option);
                }
                else
                {
                    var option = new CookieOptions()
                    {
                        Expires = DateTime.Now.AddDays(90)
                    };
                    Response.Cookies.Append("wish", cookieValue + id + ",", option);
                }
            }
            else
            {
                var p = _shopContext.Products.Find(id);

                if (p != null)
                {
                    var l = _shopContext.WishList.Where(p => p.UserId == _signInManager.UserManager.GetUserId(User) && p.ProductId == id).ToList();

                    if (l.Count != 0)
                    {
                        _shopContext.WishList.Add(new WishList()
                        {
                            UserId = _signInManager.UserManager.GetUserId(User),
                            ProductId = p.ProductId
                        });
                    }


                    _shopContext.SaveChanges();
                }
            }

            if (choice == 1)
                return Redirect("/Home/Index");

            return Redirect("/Home/Index");
        }


        public IActionResult RemoveWishList(int id)
        {


            List<int> wishList = new List<int>();
            string? cookieValue = Request.Cookies["wish"];
            if (cookieValue != null)
            {
                foreach (var c in cookieValue.Split(","))
                {
                    if (!string.IsNullOrEmpty(c))
                        wishList.Add(int.Parse(c));
                }
                wishList.Remove(id);
                if (wishList.Count == 0)
                {
                    Response.Cookies.Delete("wish");
                }
                else
                {
                    string temp = "";
                    foreach (var c in wishList)
                    {
                        temp = temp + c + ",";
                    }

                    var option = new CookieOptions()
                    {
                        Expires = DateTime.Now.AddDays(90)
                    };
                    Response.Cookies.Append("wish", temp, option);
                }
            }

            var wish = _shopContext.WishList.Where(p => p.UserId == _signInManager.UserManager.GetUserId(User) && p.ProductId == id).ToList();
            if (wish.Count > 0)
            {
                _shopContext.WishList.Remove(wish[0]);
                _shopContext.SaveChanges();
            }

            return Redirect("/Product/WishList");
        }

        public IActionResult WishList()
        {
            List<int> list = new List<int>();

            if (_signInManager.IsSignedIn(User))
            {
                list.AddRange(_shopContext.WishList.Where(p => p.UserId == _signInManager.UserManager.GetUserId(User)).Select(p => p.ProductId).ToList());
            }


            string? cookieValue = Request.Cookies["wish"];
            if (cookieValue != null)
            {
                foreach (var c in cookieValue.Split(","))
                {
                    if (!string.IsNullOrEmpty(c))
                        list.Add(int.Parse(c));
                }
            }

            return View(_shopContext.Products.Where(c => list.Contains(c.ProductId)).ToList());
        }
    }
}
