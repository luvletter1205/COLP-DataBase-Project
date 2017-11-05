using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DbProj.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Index()
        {
            return NoContent();
        }

        [HttpGet]
        public IActionResult Menu(long id)
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.BigInt, id);
            var result = si.AdapterJson(
                "SELECT [ID], [name], [description], [price] " +
                "FROM [Menu] " +
                "WHERE [order] IS NOT NULL AND [RID]=@p1 " +
                "ORDER BY [order]");
            return new ObjectResult(result);
        }

        [HttpGet]
        public IActionResult RestaurantInfo(long id)
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.BigInt, id);
            var result = si.QueryJson("SELECT [name], [description] FROM [Restaurant] WHERE [ID]=@p1");
            return new ObjectResult(result);
        }

        private struct OrderItem
        {
            public long id;
            public int quantity;
        }
        
        private struct Order
        {
            public decimal total;
            public long rid;
            public List<OrderItem> list;
        }

        private static decimal CalcTotal(IEnumerable<OrderItem> list)
        {
            decimal total = 0;
            foreach (var item in list)
            {
                var si = new SqlIntegrate();
                si.AddParameter("@p1", SqlIntegrate.DataType.BigInt, item.id);
                total += item.quantity * Convert.ToDecimal(si.Query("SELECT [price] FROM [Menu] WHERE [ID]=@p1"));
            }
            return total;
        }
        
        [HttpPost]
        public IActionResult Submit(string batch)
        {
            Order order;
            try
            {
                var o = JsonConvert.DeserializeObject<Order>(batch);
                if (o.total != CalcTotal(o.list))
                    return NotFound();
                order = o;
            }
            catch
            {
                return NotFound();
            }
            
            var paras = new List<SqlParameter>();
            var command = "DECLARE @oid BIGINT;" +
                          "INSERT INTO [Order] ([RID], [UID], [amount]) " +
                          "VALUES (@m1, (SELECT [ID] FROM [User] WHERE [username]=@m2), @m3) " +
                          "SELECT @oid=@@IDENTITY;";
            paras.Add(new SqlParameter("@m1", SqlDbType.BigInt) {Value = order.rid});
            paras.Add(new SqlParameter("@m2", SqlDbType.VarChar) {Value = HttpContext.Session.GetString("user")});
            paras.Add(new SqlParameter("@m3", SqlDbType.Decimal) {Value = order.total});

            for (var i = 0; i < order.list.Count; i++)
            {
                command += "INSERT INTO [OrderContent] ([OID], [MID], [quantity]) " +
                           $"VALUES (@oid, @p{i}1, @p{i}2);";
                paras.Add(new SqlParameter($"@p{i}1", SqlDbType.BigInt) {Value = order.list[i].id});
                paras.Add(new SqlParameter($"@p{i}2", SqlDbType.Int) {Value = order.list[i].quantity});
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
        public IActionResult List()
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("user"));
            var result =
                si.AdapterJson("SELECT [Order].[ID], [Restaurant].[name], [Order].[amount], [Order].[datetime] " +
                               "FROM [Order], [Restaurant] " +
                               "WHERE [Order].[RID]=[Restaurant].[ID] AND [Order].[UID]=(" +
                               "SELECT [ID] FROM [User] WHERE [username]=@p1" +
                               ") " +
                               "ORDER BY datetime DESC");
            return new ObjectResult(result);
        }

        [HttpGet]
        public IActionResult Content(long id)
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.BigInt, id);
            var result = si.AdapterJson("SELECT [Menu].[name], [Menu].[price], [OrderContent].[quantity] " +
                                        "FROM [Menu], [OrderContent] " +
                                        "WHERE [Menu].[ID]=[OrderContent].[MID] " +
                                        "AND [OrderContent].[OID]=@p1");
            return new ObjectResult(result);
        }
    }
}