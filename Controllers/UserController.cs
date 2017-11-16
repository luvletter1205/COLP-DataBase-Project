using System;
using System.Data;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DbProj.Controllers
{
    public class UserController : Controller
    {
        public IActionResult Index()
        {
            return NoContent();
        }

        
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, username);
            si.AddParameter("@p2", SqlIntegrate.DataType.VarChar, password);
            var result =
                Convert.ToInt32(si.Query("SELECT COUNT(*) FROM [User] WHERE [username]=@p1 AND [password]=@p2"));
            if (result == 1)
            {
                HttpContext.Session.SetString("user", username);
                return Ok();
            }
            return NotFound();
        }
        
        [HttpGet]
        public IActionResult CheckLogin()
        {
            if (HttpContext.Session.GetString("user") != null)
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

        [HttpGet]
        public IActionResult Info()
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("user"));
            var result =
                si.QueryJson(
                    "SELECT [username], [password], " +
                    "ISNULL([phone], '') AS phone, " +
                    "ISNULL([deliverAddress], '') as deliverAddress " +
                    "FROM [User] " +
                    "WHERE [username]=@p1");
            return new ObjectResult(result);
        }

        [HttpPost]
        public IActionResult Update(string password, string phone, string deliverAddress)
        {
            if (password == null) return NotFound();
            if (phone == null) phone = "";
            if (deliverAddress == null) deliverAddress = "";
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("user"));
            si.AddParameter("@p2", SqlIntegrate.DataType.VarChar, password);
            si.AddParameter("@p3", SqlIntegrate.DataType.NVarChar, deliverAddress);
            si.AddParameter("@p4", SqlIntegrate.DataType.VarChar, phone);
            var result =
                si.Execute("EXECUTE UserUpdate @p1, @p2, @p3, @p4");
            if (result == 1) return Ok();
            return NotFound();
        }
        
        [HttpPost]
        public IActionResult Register(string username, string password, string phone, string address)
        {            
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, username);
            si.AddParameter("@p2", SqlIntegrate.DataType.VarChar, password);
            if (address != null)
                si.AddParameter("@p3", SqlIntegrate.DataType.NVarChar, address);
            if (phone != null)
                si.AddParameter("@p4", SqlIntegrate.DataType.VarChar, phone);
            int result;
            try
            {
                result = si.Execute("EXECUTE UserRegister @p1, @p2" 
                                    + (address != null ? " ,@p3" : " ,NULL") 
                                    + (phone   != null ? " ,@p4" : " ,NULL"));
            }
            catch
            {
                return NotFound();
            }
            if (result == 1)
            {
                return Ok();
            }
            return NotFound();
        }
    }
}