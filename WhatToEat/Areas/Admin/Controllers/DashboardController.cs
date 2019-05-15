using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WhatToEat.Areas.Admin.Controllers
    {
        [Authorize(Roles = "Admin")]
        public class DasboardController : Controller
        {
            // GET: Admin/Dashboard
            public ActionResult Index()
            {
                return View();
            }
        }
    }
