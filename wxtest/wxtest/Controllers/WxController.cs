using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using wxtest.Common;

namespace wxtest.Controllers
{
    public class WxController : Controller
    {
        System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
        WxHelper helper = new WxHelper()
        {
            AppID = "wxfa4ce3c8d3c1e4cd",
            AppSecret = "26d33de4bdf4bcc01b8196a1e8791929",
            Token = "wxtest"
        };

        public ActionResult Index()
        {
            string encodeUri = Url.Encode("http://class.sjtu-oec.com/sjtu/wxtest/wx/login");
            string wxLoginUrl = helper.GetLoginUrl(encodeUri);
            ViewBag.wxLoginUrl = wxLoginUrl;
            return View();
        }

        public ActionResult Valid()
        {
            string signature = Request["signature"];
            string timestamp = Request["timestamp"];
            string nonce = Request["nonce"];
            string echostr = Request["echostr"];

            bool b = helper.Valid(signature, timestamp, nonce);
            if (b)
            {
                System.IO.StreamReader stream = new System.IO.StreamReader(Request.InputStream);
                string sReqData = stream.ReadToEnd();
                Response.Write(echostr);
                Response.End();
            }
            return View();
        }

        public ActionResult Login()
        {
            string code = Request["code"];
            if (string.IsNullOrEmpty(code))
            {
                return RedirectToAction("NoAuthor");
            }
            WxHelper.WxUserInfo userinfo = helper.GetUserInfo(code);
            ViewBag.userinfo = userinfo;
            return View();
        }
    }
}