using System;
using System.Collections.Generic;
using System.Dynamic;
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

        public ActionResult JsSDK()
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

            ViewBag.appId = appId;
            ViewBag.timestamp = timestamp;
            ViewBag.noncestr = noncestr;
            ViewBag.signature = signature;
            return View();
        }

        public ActionResult MenuCreate(Common.WxHelper.wx_menu m)
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
            wxtest.Common.WxHelper.wxmenu wxmenu = helper.ConverTowxmenu(m);
            //var menu = GetMenu(wxmenu);
            //string json = jss.Serialize(menu);
            string json = jss.Serialize(wxmenu.menu);
            bool result = helper.CreateMenu(tickek.Access_token, json);
            ViewBag.result = result ? "操作成功" : "操作失败";
            return View();
        }

        private object GetMenu(WxHelper.wxmenu wxmenu)
        {
            List<object> btns = new List<object>();
            foreach (WxHelper.button button in wxmenu.menu.button)
            {
                if (button.sub_button == null || button.sub_button.Length == 0)
                {
                    switch (button.type)
                    {
                        case "click":
                            btns.Add(new { type = button.type, name = button.name, key = button.key });
                            break;
                        case "view":
                            btns.Add(new { type = button.type, name = button.name, url = button.url });
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    List<object> sbtns = new List<object>();
                    foreach (WxHelper.sub_button sub_button in button.sub_button)
                    {
                        switch (sub_button.type)
                        {
                            case "click":
                                sbtns.Add(new { type = sub_button.type, name = sub_button.name, key = sub_button.key, sub_button = new object[] { } });
                                break;
                            case "view":
                                sbtns.Add(new { type = sub_button.type, name = sub_button.name, url = sub_button.url, sub_button = new object[] { } });
                                break;
                            default:
                                break;
                        }
                    }
                    btns.Add(new { name = button.name, sub_button = sbtns.ToArray() });
                }
            }
            return new { button = btns.ToArray() };
        }

        public ActionResult Menu()
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
            string json = helper.GetMenu(tickek.Access_token);
            wxtest.Common.WxHelper.wxmenu wxmenu = jss.Deserialize<wxtest.Common.WxHelper.wxmenu>(json);
            wxtest.Common.WxHelper.wx_menu wx_menu = helper.ConverTowx_menu(wxmenu);
            ViewBag.wx_menu = wx_menu;
            return View();
        }
    }
}
