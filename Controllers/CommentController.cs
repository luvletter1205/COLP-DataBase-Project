using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace DbProj.Controllers
{
    public class CommentController : Controller
    {
        public IActionResult Index()
        {
            return NoContent();
        }

        [HttpPost]
        public IActionResult Add(long id)
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.VarChar, HttpContext.Session.GetString("user"));
            si.AddParameter("@p2", SqlIntegrate.DataType.NVarChar, HttpContext.Request.Form["content"].ToString());
            si.AddParameter("@p3", SqlIntegrate.DataType.BigInt, id);
            var result = si.Execute("INSERT INTO [Comment] ([UID], [content], [RID]) VALUES (" +
                                    "(SELECT [ID] FROM [User] WHERE [username]=@p1)," +
                                    "@p2," +
                                    "@p3)");
            if (result == 1) return Ok();
            return NotFound();
        }

        [HttpGet]
        public IActionResult List(long id)
        {
            var si = new SqlIntegrate();
            si.AddParameter("@p1", SqlIntegrate.DataType.BigInt, id);
            var result = si.AdapterJson("SELECT [User].[username], [Comment].[content], [Comment].[datetime] " +
                                        "FROM [User], [Comment] " +
                                        "WHERE [Comment].[UID]=[User].[ID] AND [Comment].[RID]=@p1");
            return new ObjectResult(result);
        }
    }
}