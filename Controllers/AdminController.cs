using System;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DbProj.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return NoContent();
        }

        private static readonly Random Random = new Random();

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Next(s.Length)]).ToArray());
        }

        [HttpPost]
        public IActionResult Login()
        {
            var username = HttpContext.Request.Form["username"];
            var password = HttpContext.Request.Form["password"];

            if (username == "tonny" && password == "824673915")
            {
                HttpContext.Session.SetString("admin", "tonny");
                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet]
        public IActionResult CheckLogin()
        {
            if (HttpContext.Session.GetString("admin") != null)
            {
                return Ok();
            }
            return NotFound();
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok();
        }
        
        [HttpPost]
        public IActionResult AddRestaurant()
        {
            if (HttpContext.Session.GetString("admin") == null) return NotFound();
            
            var name = HttpContext.Request.Form["name"].ToString();
            var username = HttpContext.Request.Form["username"].ToString();
            var password = RandomString(8);
            
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.NVarChar, name);
            si.AddParameter("@p2", SqlIntegrate.DataType.VarChar, username);
            si.AddParameter("@p3", SqlIntegrate.DataType.VarChar, password);
            var result = si.Execute("INSERT INTO [Restaurant] ([name], [username], [password]) VALUES (@p1, @p2, @p3)");
            
            if (result == 1)
            {
                return new ObjectResult(new JObject
                {
                    ["password"] = password
                });
            }
            return NotFound();
        }
    }
}