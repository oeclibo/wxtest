using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace wxtest.Controllers
{
    public class WxController : Controller
    {
        System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();
        Common.WxHelper helper = new Common.WxHelper()
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
            Common.WxHelper.WxUserInfo userinfo = helper.GetUserInfo(code);
            ViewBag.userinfo = userinfo;
            return View();
        }

        public ActionResult WebLogin()
        {
            ViewBag.helper = helper;
            ViewBag.encodeUri = Url.Encode("http://class.sjtu-oec.com/sjtu/wxtest/wx/login");
            return View();
        }

        public ActionResult TestJs()
        {
            Common.WxHelper.JsApi_Ticket tickek = HttpContext.Application["jsapi_ticket"] as Common.WxHelper.JsApi_Ticket;
            //无票据或票据过期
            if (tickek == null || tickek.Ticket == null || tickek.Ticket.Length == 0 || tickek.Expires < DateTime.Now)
            {
                tickek = helper.GetJsApi_Ticket();
                if (string.IsNullOrEmpty(tickek.Ticket) || tickek.Expires < DateTime.Now)
                {
                    return RedirectToAction("Error", new { msg = "接口凭证不存在或已过期" });
                }
                HttpContext.Application.Add("jsapi_ticket", tickek);
            }
            string appId = helper.AppID;
            string timestamp = helper.GetTimestamp();
            string noncestr = helper.GetNonceStr();
            string url = Request.Url.AbsoluteUri;
            string signature = helper.GetJsSignature(noncestr, tickek.Ticket, timestamp, url);

            /*调试
            List<string> list = new List<string>() { "noncestr", "jsapi_ticket", "timestamp", "url" };
            list.Sort();
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("noncestr", noncestr);
            dict.Add("jsapi_ticket", tickek.Ticket);
            dict.Add("timestamp", timestamp);
            dict.Add("url", url);
            StringBuilder strBld = new StringBuilder();
            foreach (string s in list)
            {
                strBld.AppendFormat("{0}={1}&", s, dict[s]);
            }
            strBld.Remove(strBld.Length - 1, 1);
            string str1 = strBld.ToString();
            string sha1 = helper.GetSHA1(str1);
            ViewBag.str1 = str1;
            ViewBag.sha1 = sha1;

            ViewBag.url = url;
            ViewBag.tickek = tickek;
            */

            ViewBag.appId = appId;
            ViewBag.timestamp = timestamp;
            ViewBag.noncestr = noncestr;
            ViewBag.signature = signature;


            return View();
        }
    }
}
