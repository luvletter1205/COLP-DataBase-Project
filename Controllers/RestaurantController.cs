using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DbProj.Controllers
{
    public class RestaurantController : Controller
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
                Convert.ToInt32(si.Query("SELECT COUNT(*) FROM [Restaurant] WHERE [username]=@p1 AND [password]=@p2"));
            if (result == 1)
            {
                HttpContext.Session.SetString("vendor", username);
                return Ok();
            }
            return NotFound();
        }
        
        [HttpGet]
        public IActionResult CheckLogin()
        {
            if (HttpContext.Session.GetString("vendor") != null)
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
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("vendor"));
            var result =
                si.QueryJson(
                    "SELECT [name], [username], [password], ISNULL([description], '') AS [d], ISNULL([type], '') AS [t] " +
                    "FROM [Restaurant] " +
                    "WHERE [username]=@p1");
            return new ObjectResult(result);
        }

        [HttpPost]
        public IActionResult Update(string password, string description, string type)
        {
            if (password == null) return NotFound();
            if (description == null) description = "";
            if (type == null) type = "";
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, password);
            si.AddParameter("@p2", SqlIntegrate.DataType.NVarChar, description);
            si.AddParameter("@p3", SqlIntegrate.DataType.NVarChar, type);
            si.AddParameter("@p4", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("vendor"));
            var result =
                si.Execute("UPDATE [Restaurant] SET " +
                           "[password]=@p1, " +
                           "[description]=@p2, " +
                           "[type]=@p3 " +
                           "WHERE [username]=@p4");
            if (result == 1) return Ok();
            return NotFound();
        }

        private struct MenuItem
        {
            public int order;
            public string name;
            public string description;
            public decimal price;
        }
        
        [HttpPost]
        public IActionResult UpdateMenu(string batch)
        {
            var list = JsonConvert.DeserializeObject<List<MenuItem>>(batch);

            var paras = new List<SqlParameter>();
            var command = "DECLARE @rid BIGINT;" +
                          "SELECT @rid=[ID] FROM [Restaurant] WHERE [username]=@p0;" +
                          "UPDATE [Menu] SET [order]=NULL WHERE [RID]=@rid;";
            paras.Add(new SqlParameter("@p0", SqlDbType.VarChar) {Value = HttpContext.Session.GetString("vendor")});
            for (var i = 0; i < list.Count; i++)
            {
                command += "INSERT INTO [Menu] ([RID], [name], [description], [order], [price]) " +
                           $"VALUES (@rid, @p{i}1, @p{i}2, @p{i}3, @p{i}4);";
                paras.Add(new SqlParameter($"@p{i}1", SqlDbType.NVarChar) {Value = list[i].name});
                paras.Add(new SqlParameter($"@p{i}2", SqlDbType.NVarChar) {Value = list[i].description});
                paras.Add(new SqlParameter($"@p{i}3", SqlDbType.Int) {Value = list[i].order});
                paras.Add(new SqlParameter($"@p{i}4", SqlDbType.Money) {Value = list[i].price});
            }
            
            using (var conn = new SqlConnection(Startup.ConnStr))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    using (var cmd = new SqlCommand(command, conn))
                    {
                        foreach (var para in paras)
                            cmd.Parameters.Add(para);
                        cmd.Transaction = transaction;
                        try
                        {
                            cmd.ExecuteNonQuery();
                            transaction.Commit();
                            return Ok();
                        }
                        catch
                        {
                            transaction.Rollback();
                            return NotFound();
                        }
                    }
                }
            }
        }
        
        
        [HttpGet]
        public IActionResult Menu()
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("vendor"));
            var result = si.AdapterJson(
                "SELECT [name], [description], [price] " +
                "FROM [Menu] " +
                "WHERE [order] IS NOT NULL AND [RID]=(SELECT [ID] FROM [Restaurant] WHERE [username]=@p1) " +
                "ORDER BY [order]");
            return new ObjectResult(result);
        }
        
        
        [HttpGet]
        public IActionResult OrderList()
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("vendor"));
            var result =
                si.AdapterJson("SELECT [Order].[ID], [User].[deliverAddress], [Order].[amount], [Order].[datetime] " +
                               "FROM [Order], [User] " +
                               "WHERE [Order].[UID]=[User].[ID] AND [Order].[RID]=(" +
                               "SELECT [ID] FROM [Restaurant] WHERE [username]=@p1" +
                               ") " +
                               "ORDER BY datetime DESC");
            return new ObjectResult(result);
        }

        [HttpGet]
        public IActionResult OrderContent(long id)
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.BigInt, id);
            var result = si.AdapterJson("SELECT [Menu].[name], [Menu].[price], [OrderContent].[quantity] " +
                                        "FROM [Menu], [OrderContent] " +
                                        "WHERE [Menu].[ID]=[OrderContent].[MID] " +
                                        "AND [OrderContent].[OID]=@p1");
            return new ObjectResult(result);
        }
        
        [HttpGet]
        public IActionResult Type()
        {
            var si = new SqlIntegrate();
            var dt = si.Adapter(
                "SELECT DISTINCT [type] FROM [Restaurant] WHERE [type] IS NOT NULL ORDER BY [type]"
            );
            var result = new JArray();
            foreach (DataRow row in dt.Rows)
            {
                result.Add(row["type"].ToString());
            }
            return new ObjectResult(result);
        }
        
        [HttpGet]
        public IActionResult List()
        {
            var si = new SqlIntegrate();
            var result = si.AdapterJson(
                "SELECT [ID], [name], [description], [type] FROM [Restaurant] " +
                "WHERE [type] IS NOT NULL AND [description] IS NOT NULL " +
                "ORDER BY [name]"
            );
            return new ObjectResult(result);
        }
    }
}