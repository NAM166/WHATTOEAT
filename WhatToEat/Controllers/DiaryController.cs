using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Mvc;
using WhatToEat.Models.Data;
using WhatToEat.Models.ViewModels.Diary;

namespace WhatToEat.Controllers
{
    public class DiaryController : Controller
    { // GET: Diary
        public ActionResult Index()
        {
            // Init the Diary list
            var Diary = Session["Diary"] as List<DiaryVM> ?? new List<DiaryVM>();

            // Check if diary is empty
            if (Diary.Count == 0 || Session["diary"] == null)
            {
                ViewBag.Message = "Your diary is empty.";
                return View();
            }

            // Calculate total and save to ViewBag

            decimal total = 0m;

            foreach (var item in Diary)
            {
                total += item.Total;
            }

            ViewBag.GrandTotal = total;

            // Return view with list
            return View(Diary);
        }

        public ActionResult DiaryPartial()
        {
            // Init DiaryVM
            DiaryVM model = new DiaryVM();

            // Init quantity
            int qty = 0;

            // Init Calorie
            decimal Calorie = 0m;

            // Check for diary session
            if (Session["diary"] != null)
            {
                // Get total qty and Calorie
                var list = (List<DiaryVM>)Session["diary"];

                foreach (var item in list)
                {
                    qty += item.Quantity;
                    Calorie += item.Quantity * item.Calorie;
                }

                model.Quantity = qty;
                model.Calorie = Calorie;

            }
            else
            {
                // Or set qty and Calorie to 0
                model.Quantity = 0;
                model.Calorie = 0m;
            }

            // Return partial view with model
            return PartialView(model);
        }

        public ActionResult AddToDiaryPartial(int id)
        {
            // Init diaryVM list
            List<DiaryVM> diary = Session["diary"] as List<DiaryVM> ?? new List<DiaryVM>();

            // Init DiaryVM
            DiaryVM model = new DiaryVM();

            using (Db db = new Db())
            {
                // Get the product
                ProductDTO product = db.Products.Find(id);

                // Check if the product is already in Diary
                var productInDiary = diary.FirstOrDefault(x => x.ProductId == id);

                // If not, add new
                if (productInDiary == null)
                {
                    diary.Add(new DiaryVM()
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        Calorie = product.Calorie,
                        Image = product.ImageName
                    });
                }
                else
                {
                    // If it is, increment
                    productInDiary.Quantity++;
                }
            }

            // Get total qty and Calorie and add to model

            int qty = 0;
            decimal Calorie = 0m;

            foreach (var item in diary)
            {
                qty += item.Quantity;
                Calorie += item.Quantity * item.Calorie;
            }

            model.Quantity = qty;
            model.Calorie = Calorie;

            // Save Diary back to session
            Session["Diary"] = diary;

            // Return partial view with model
            return PartialView(model);
        }

        // GET: /Diary/IncrementProduct
        public JsonResult IncrementProduct(int productId)
        {
            // Init Diary list
            List<DiaryVM> Diary = Session["Diary"] as List<DiaryVM>;

            using (Db db = new Db())
            {
                // Get DiaryVM from list
                DiaryVM model = Diary.FirstOrDefault(x => x.ProductId == productId);

                // Increment qty
                model.Quantity++;

                // Store needed data
                var result = new { qty = model.Quantity, Calorie = model.Calorie };

                // Return json with data
                return Json(result, JsonRequestBehavior.AllowGet);
            }

        }

        // GET: /Diary/DecrementProduct
        public ActionResult DecrementProduct(int productId)
        {
            // Init Diary
            List<DiaryVM> Diary = Session["Diary"] as List<DiaryVM>;

            using (Db db = new Db())
            {
                // Get model from list
                DiaryVM model = Diary.FirstOrDefault(x => x.ProductId == productId);

                // Decrement qty
                if (model.Quantity > 1)
                {
                    model.Quantity--;
                }
                else
                {
                    model.Quantity = 0;
                    Diary.Remove(model);
                }

                // Store needed data
                var result = new { qty = model.Quantity, Calorie = model.Calorie };

                // Return json
                return Json(result, JsonRequestBehavior.AllowGet);
            }

        }

        // GET: /Diary/RemoveProduct
        public void RemoveProduct(int productId)
        {
            // Init Diary list
            List<DiaryVM> Diary = Session["Diary"] as List<DiaryVM>;

            using (Db db = new Db())
            {
                // Get model from list
                DiaryVM model = Diary.FirstOrDefault(x => x.ProductId == productId);

                // Remove model from list
                Diary.Remove(model);
            }

        }

        public ActionResult PaypalPartial()
        {
            List<DiaryVM> Diary = Session["Diary"] as List<DiaryVM>;

            return PartialView(Diary);
        }

        // POST: /Diary/PlaceOrder
        [HttpPost]
        public void PlaceOrder()
        {
            // Get Diary list
            List<DiaryVM> Diary = Session["Diary"] as List<DiaryVM>;

            // Get username
            string username = User.Identity.Name;

            int orderId = 0;

            using (Db db = new Db())
            {
                // Init OrderDTO
                OrderDTO orderDTO = new OrderDTO();

                // Get user id
                var q = db.Users.FirstOrDefault(x => x.Username == username);
                int userId = q.Id;

                // Add to OrderDTO and save
                orderDTO.UserId = userId;
                orderDTO.CreatedAt = DateTime.Now;

                db.Orders.Add(orderDTO);

                db.SaveChanges();

                // Get inserted id
                orderId = orderDTO.OrderId;

                // Init OrderDetailsDTO
                OrderDetailsDTO orderDetailsDTO = new OrderDetailsDTO();

                // Add to OrderDetailsDTO
                foreach (var item in Diary)
                {
                    orderDetailsDTO.OrderId = orderId;
                    orderDetailsDTO.UserId = userId;
                    orderDetailsDTO.ProductId = item.ProductId;
                    orderDetailsDTO.Quantity = item.Quantity;

                    db.OrderDetails.Add(orderDetailsDTO);

                    db.SaveChanges();
                }
            }

            // Email admin
            var client = new SmtpClient("mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("21f57cbb94cf88", "e9d7055c69f02d"),
                EnableSsl = true
            };
            client.Send("admin@example.com", "admin@example.com", "New Order", "You have a new order. Order number " + orderId);

            // Reset session
            Session["Diary"] = null;
        }
    }
}