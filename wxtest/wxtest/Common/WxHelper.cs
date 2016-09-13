using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace wxtest.Common
{
    public class WxHelper
    {
        public string AppID { get; set; }
        public string AppSecret { get; set; }
        public string Token { get; set; }

        System.Web.Script.Serialization.JavaScriptSerializer jss = new System.Web.Script.Serialization.JavaScriptSerializer();

        // 微信接入验证
        public bool Valid(string sSignature, string sTimeStamp, string sNonce)
        {
            List<string> list = new List<string>() { Token, sTimeStamp, sNonce };
            list.Sort();
            string s = string.Join("", list);
            string sha1 = GetSHA1(s);
            if (sha1 == sSignature)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //微信登录链接
        public string GetLoginUrl(string encodeUri)
        {
            string link = "https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope={2}&state=STATE#wechat_redirect";
            return string.Format(link, AppID, encodeUri, "snsapi_userinfo");
        }

        //获取用户信息
        public WxUserInfo GetUserInfo(string code)
        {
            string url = string.Format("https://api.weixin.qq.com/sns/oauth2/access_token?appid={0}&secret={1}&code={2}&grant_type=authorization_code", AppID, AppSecret, code);
            string s = GetWebRequest_Get(url);
            Dictionary<string, string> dict = jss.Deserialize<Dictionary<string, string>>(s);
            url = string.Format("https://api.weixin.qq.com/sns/userinfo?access_token={0}&openid={1}&lang=zh_CN", dict["access_token"], dict["openid"]);
            s = GetWebRequest_Get(url);
            WxUserInfo userinfo = jss.Deserialize<WxUserInfo>(s);
            return userinfo;
        }

        //获取JsApi_Ticket
        public JsApi_Ticket GetJsApi_Ticket()
        {
            JsApi_Ticket ticket = new JsApi_Ticket() { Access_token = "", Ticket = "", Expires = DateTime.Now };
            //获取Access_token
            ticket.Access_token = GetAccess_Token();
            if (string.IsNullOrEmpty(ticket.Access_token))
            {
                return ticket;
            }
            //获取Ticket
            ticket.Ticket = GetJsApi_Ticket(ticket.Access_token);
            if (string.IsNullOrEmpty(ticket.Ticket))
            {
                return ticket;
            }
            //设置过期时间
            ticket.Expires = DateTime.Now.AddSeconds(6000);//微信jsapi_ticket 7200秒过期
            return ticket;
        }

        //获取Access_Token
        private string GetAccess_Token()
        {
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type=client_credential&appid={0}&secret={1}", AppID, AppSecret);
            string s = GetWebRequest_Get(url);
            Dictionary<string, string> dict = jss.Deserialize<Dictionary<string, string>>(s);
            if (dict.ContainsKey("access_token"))
            {
                return dict["access_token"];
            }
            return "";
        }

        //获取JsApi_Ticket
        private string GetJsApi_Ticket(string access_token)
        {
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={0}&type=jsapi", access_token);
            string s = GetWebRequest_Get(url);
            Dictionary<string, string> dict = jss.Deserialize<Dictionary<string, string>>(s);
            if (dict.ContainsKey("ticket"))
            {
                return dict["ticket"];
            }
            return "";
        }

        //获取jsSDK签名算法
        public string GetJsSignature(string noncestr, string jsapi_ticket, string timestamp, string url)
        {
            List<string> list = new List<string>() { "noncestr", "jsapi_ticket", "timestamp", "url" };
            list.Sort();
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict.Add("noncestr", noncestr);
            dict.Add("jsapi_ticket", jsapi_ticket);
            dict.Add("timestamp", timestamp);
            dict.Add("url", url);
            StringBuilder strBld = new StringBuilder();
            foreach (string s in list)
            {
                strBld.AppendFormat(@"{0}={1}&", s, dict[s]);
            }
            strBld.Remove(strBld.Length - 1, 1);
            return GetSHA1(strBld.ToString());
        }

        public bool CreateMenu(string access_token, string json)
        {
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/menu/create?access_token={0}", access_token);
            //string s = GetWebRequest_Post(url, json);
            string s = ServerRequest(url, "POST", "application/json", 20000, json);
            Dictionary<string, string> dict = jss.Deserialize<Dictionary<string, string>>(s);
            if (dict.ContainsKey("errcode") && dict["errcode"] == "0")
            {
                return true;
            }
            return false;
        }

        public string GetMenu(string access_token)
        {
            string url = string.Format("https://api.weixin.qq.com/cgi-bin/menu/get?access_token={0}", access_token);
            string s = GetWebRequest_Get(url);
            //string s = ServerRequest(url, "GET", "", 20000, "");
            return s;
            //return jss.Deserialize<Menu>(s);
        }

        #region 转化menu
        public wx_menu ConverTowx_menu(wxmenu wxmenu)
        {
            wx_menu m = new wx_menu();
            #region 菜单1
            if (wxmenu.menu.button.Length >= 1)
            {
                button btn = wxmenu.menu.button[0];
                m.cbxEnable1 = true;
                if (btn.sub_button.Length == 0)
                {
                    m.txtbtnName1 = btn.name;
                    m.rdotp1 = "0";
                    m.txtbtntp1 = btn.type;
                    switch (btn.type)
                    {
                        case "click":
                            m.txtbtnvalue1 = btn.key;
                            break;
                        case "view":
                            m.txtbtnvalue1 = btn.url;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    m.txtmenuName1 = btn.name;
                    m.rdotp1 = "1";
                    #region 子按钮列表1-5
                    if (btn.sub_button.Length >= 1)
                    {
                        sub_button sbtn = btn.sub_button[0];
                        m.txtsubbtnName1 = sbtn.name;
                        m.txtsubbtntp1 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue1 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue1 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 2)
                    {
                        sub_button sbtn = btn.sub_button[1];
                        m.txtsubbtnName2 = sbtn.name;
                        m.txtsubbtntp2 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue2 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue2 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 3)
                    {
                        sub_button sbtn = btn.sub_button[2];
                        m.txtsubbtnName3 = sbtn.name;
                        m.txtsubbtntp3 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue3 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue3 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 4)
                    {
                        sub_button sbtn = btn.sub_button[3];
                        m.txtsubbtnName4 = sbtn.name;
                        m.txtsubbtntp4 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue4 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue4 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 5)
                    {
                        sub_button sbtn = btn.sub_button[4];
                        m.txtsubbtnName5 = sbtn.name;
                        m.txtsubbtntp5 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue5 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue5 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    #endregion
                }
            }
            #endregion
            #region 菜单2
            if (wxmenu.menu.button.Length >= 2)
            {
                button btn = wxmenu.menu.button[1];
                m.cbxEnable2 = true;
                if (btn.sub_button.Length == 0)
                {
                    m.txtbtnName2 = btn.name;
                    m.rdotp2 = "0";
                    m.txtbtntp2 = btn.type;
                    switch (btn.type)
                    {
                        case "click":
                            m.txtbtnvalue2 = btn.key;
                            break;
                        case "view":
                            m.txtbtnvalue2 = btn.url;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    m.txtmenuName2 = btn.name;
                    m.rdotp2 = "1";
                    #region 子按钮列表6-10
                    if (btn.sub_button.Length >= 1)
                    {
                        sub_button sbtn = btn.sub_button[0];
                        m.txtsubbtnName6 = sbtn.name;
                        m.txtsubbtntp6 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue6 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue6 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 2)
                    {
                        sub_button sbtn = btn.sub_button[1];
                        m.txtsubbtnName7 = sbtn.name;
                        m.txtsubbtntp7 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue7 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue7 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 3)
                    {
                        sub_button sbtn = btn.sub_button[2];
                        m.txtsubbtnName8 = sbtn.name;
                        m.txtsubbtntp8 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue8 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue8 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 4)
                    {
                        sub_button sbtn = btn.sub_button[3];
                        m.txtsubbtnName9 = sbtn.name;
                        m.txtsubbtntp9 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue9 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue9 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 5)
                    {
                        sub_button sbtn = btn.sub_button[4];
                        m.txtsubbtnName10 = sbtn.name;
                        m.txtsubbtntp10 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue10 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue10 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    #endregion
                }
            }
            #endregion
            #region 菜单3
            if (wxmenu.menu.button.Length >= 3)
            {
                button btn = wxmenu.menu.button[2];
                m.cbxEnable3 = true;
                if (btn.sub_button.Length == 0)
                {
                    m.txtbtnName3 = btn.name;
                    m.rdotp3 = "0";
                    m.txtbtntp3 = btn.type;
                    switch (btn.type)
                    {
                        case "click":
                            m.txtbtnvalue3 = btn.key;
                            break;
                        case "view":
                            m.txtbtnvalue3 = btn.url;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    m.txtmenuName3 = btn.name;
                    m.rdotp3 = "1";
                    #region 子按钮列表11-15
                    if (btn.sub_button.Length >= 1)
                    {
                        sub_button sbtn = btn.sub_button[0];
                        m.txtsubbtnName11 = sbtn.name;
                        m.txtsubbtntp11 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue11 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue11 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 2)
                    {
                        sub_button sbtn = btn.sub_button[1];
                        m.txtsubbtnName12 = sbtn.name;
                        m.txtsubbtntp12 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue12 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue12 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 3)
                    {
                        sub_button sbtn = btn.sub_button[2];
                        m.txtsubbtnName13 = sbtn.name;
                        m.txtsubbtntp13 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue13 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue13 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 4)
                    {
                        sub_button sbtn = btn.sub_button[3];
                        m.txtsubbtnName14 = sbtn.name;
                        m.txtsubbtntp14 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue14 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue14 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    if (btn.sub_button.Length >= 5)
                    {
                        sub_button sbtn = btn.sub_button[4];
                        m.txtsubbtnName15 = sbtn.name;
                        m.txtsubbtntp15 = sbtn.type;
                        switch (sbtn.type)
                        {
                            case "click":
                                m.txtsubbtnvalue15 = sbtn.key;
                                break;
                            case "view":
                                m.txtsubbtnvalue15 = sbtn.url;
                                break;
                            default:
                                break;
                        }
                    }
                    #endregion
                }
            }
            #endregion
            return m;
        }
        public wxmenu ConverTowxmenu(wx_menu m)
        {
            wxmenu wxmenu = new wxmenu();
            List<button> listbtn = new List<button>();
            #region 菜单1
            if (m.cbxEnable1)
            {
                button btn = new button();
                if (m.rdotp1 == "0")
                {
                    btn.name = m.txtbtnName1;
                    btn.type = m.txtbtntp1;
                    switch (btn.type)
                    {
                        case "click":
                            btn.key = m.txtbtnvalue1;
                            break;
                        case "view":
                            btn.url = m.txtbtnvalue1;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    btn.name = m.txtmenuName1;
                    List<sub_button> list = new List<sub_button>();
                    #region 子按钮列表1-5
                    if (!string.IsNullOrEmpty(m.txtsubbtnName1) && !string.IsNullOrEmpty(m.txtsubbtnvalue1))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName1;
                        sbtn.type = m.txtsubbtntp1;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue1;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue1;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName2) && !string.IsNullOrEmpty(m.txtsubbtnvalue2))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName2;
                        sbtn.type = m.txtsubbtntp2;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue2;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue2;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName3) && !string.IsNullOrEmpty(m.txtsubbtnvalue3))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName3;
                        sbtn.type = m.txtsubbtntp3;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue3;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue3;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName4) && !string.IsNullOrEmpty(m.txtsubbtnvalue4))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName4;
                        sbtn.type = m.txtsubbtntp4;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue4;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue4;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName5) && !string.IsNullOrEmpty(m.txtsubbtnvalue5))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName5;
                        sbtn.type = m.txtsubbtntp5;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue5;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue5;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    #endregion
                    btn.sub_button = list.ToArray();
                }
                listbtn.Add(btn);
            }
            #endregion
            #region 菜单2
            if (m.cbxEnable2)
            {
                button btn = new button();
                if (m.rdotp2 == "0")
                {
                    btn.name = m.txtbtnName2;
                    btn.type = m.txtbtntp2;
                    switch (btn.type)
                    {
                        case "click":
                            btn.key = m.txtbtnvalue2;
                            break;
                        case "view":
                            btn.url = m.txtbtnvalue2;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    btn.name = m.txtmenuName2;
                    List<sub_button> list = new List<sub_button>();
                    #region 子按钮列表6-10
                    if (!string.IsNullOrEmpty(m.txtsubbtnName6) && !string.IsNullOrEmpty(m.txtsubbtnvalue6))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName6;
                        sbtn.type = m.txtsubbtntp6;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue6;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue6;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName7) && !string.IsNullOrEmpty(m.txtsubbtnvalue7))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName7;
                        sbtn.type = m.txtsubbtntp7;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue7;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue7;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName8) && !string.IsNullOrEmpty(m.txtsubbtnvalue8))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName8;
                        sbtn.type = m.txtsubbtntp8;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue8;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue8;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName9) && !string.IsNullOrEmpty(m.txtsubbtnvalue9))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName9;
                        sbtn.type = m.txtsubbtntp9;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue9;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue9;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName10) && !string.IsNullOrEmpty(m.txtsubbtnvalue10))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName10;
                        sbtn.type = m.txtsubbtntp10;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue10;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue10;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    #endregion
                    btn.sub_button = list.ToArray();
                }
                listbtn.Add(btn);
            }
            #endregion
            #region 菜单3
            if (m.cbxEnable3)
            {
                button btn = new button();
                if (m.rdotp3 == "0")
                {
                    btn.name = m.txtbtnName3;
                    btn.type = m.txtbtntp3;
                    switch (btn.type)
                    {
                        case "click":
                            btn.key = m.txtbtnvalue3;
                            break;
                        case "view":
                            btn.url = m.txtbtnvalue3;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    btn.name = m.txtmenuName3;
                    List<sub_button> list = new List<sub_button>();
                    #region 子按钮列表11-15
                    if (!string.IsNullOrEmpty(m.txtsubbtnName11) && !string.IsNullOrEmpty(m.txtsubbtnvalue11))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName11;
                        sbtn.type = m.txtsubbtntp11;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue11;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue11;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName12) && !string.IsNullOrEmpty(m.txtsubbtnvalue12))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName12;
                        sbtn.type = m.txtsubbtntp12;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue12;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue12;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName13) && !string.IsNullOrEmpty(m.txtsubbtnvalue13))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName13;
                        sbtn.type = m.txtsubbtntp13;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue13;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue13;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName14) && !string.IsNullOrEmpty(m.txtsubbtnvalue14))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName14;
                        sbtn.type = m.txtsubbtntp14;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue14;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue14;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    if (!string.IsNullOrEmpty(m.txtsubbtnName15) && !string.IsNullOrEmpty(m.txtsubbtnvalue15))
                    {
                        sub_button sbtn = new sub_button();
                        sbtn.name = m.txtsubbtnName15;
                        sbtn.type = m.txtsubbtntp15;
                        switch (sbtn.type)
                        {
                            case "click":
                                sbtn.key = m.txtsubbtnvalue15;
                                break;
                            case "view":
                                sbtn.url = m.txtsubbtnvalue15;
                                break;
                            default:
                                break;
                        }
                        list.Add(sbtn);
                    }
                    #endregion
                    btn.sub_button = list.ToArray();
                }
                listbtn.Add(btn);
            }
            #endregion
            wxmenu.menu = new menu();
            wxmenu.menu.button = listbtn.ToArray();
            return wxmenu;
        }
        #endregion

        #region 常用算法
        //获取时间戳
        public string GetTimestamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt32(ts.TotalSeconds).ToString();
        }

        //获取随机字符串
        public string GetNonceStr()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        //SHA1算法计算字符串
        public string GetSHA1(string text)
        {
            System.Security.Cryptography.SHA1 sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider();
            byte[] secArr = sha1.ComputeHash(System.Text.Encoding.Default.GetBytes(text));
            return BitConverter.ToString(secArr).Replace("-", "").ToLower();
        }
        #endregion

        #region 后台发送GET Post请求
        //发送一个Get请求并获得返回全文
        private string GetWebRequest_Get(string url)
        {
            //声明一个HttpWebRequest请求  
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentLength = 0;
            request.ContentType = "text/xml";
            request.Timeout = 20000;//20秒
            request.Headers.Set("Pragma", "no-cache");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream streamReceive = response.GetResponseStream();
            StreamReader streamReader = new StreamReader(streamReceive, Encoding.UTF8);
            string strResult = streamReader.ReadToEnd();
            streamReceive.Dispose();
            streamReader.Dispose();
            return strResult;
        }

        //发送一个Post请求并获得返回全文
        private string GetWebRequest_Post(string url, string postDataStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json";
            //request.ContentType = "application/x-www-form-urlencoded";
            request.Timeout = 20000;//20秒

            byte[] data = Encoding.UTF8.GetBytes(postDataStr);
            request.ContentLength = data.Length;
            Stream myRequestStream = request.GetRequestStream();
            myRequestStream.Write(data, 0, data.Length);
            myRequestStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader myStreamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd().Trim();
            myStreamReader.Close();

            return retString;
        }

        private string ServerRequest(string url, string method, string contentType, int timeOut, string content)
        {
            HttpWebRequest request = null;
            HttpWebResponse response = null;
            Stream requestStream = null;
            Stream responseStream = null;
            string result = "";
            try
            {
                request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = method;
                request.ContentType = contentType;// "application/json";
                request.Timeout = timeOut;
                byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
                request.ContentLength = data.Length;
                requestStream = request.GetRequestStream();
                requestStream.Write(data, 0, data.Length);
                requestStream.Close();
                response = (HttpWebResponse)request.GetResponse();
                responseStream = response.GetResponseStream();
                StreamReader sr = new StreamReader(responseStream, Encoding.UTF8);
                result = sr.ReadToEnd().Trim();
                sr.Close();

            }
            catch (WebException e)
            {
            }
            catch (Exception e)
            {
            }
            finally
            {
                //关闭连接和流
                if (response != null)
                {
                    response.Close();
                }
                if (request != null)
                {
                    request.Abort();
                }
            }
            return result;
        }
        #endregion

        public class WxUserInfo
        {
            public string Openid { get; set; }//用户的唯一标识
            public string Nickname { get; set; } //用户昵称
            public string Sex { get; set; }//用户的性别，值为1时是男性，值为2时是女性，值为0时是未知
            public string Province { get; set; }  //用户个人资料填写的省份
            public string City { get; set; }  //普通用户个人资料填写的城市
            public string Country { get; set; } //国家，如中国为CN
            public string Headimgurl { get; set; } //用户头像，最后一个数值代表正方形头像大小（有0、46、64、96、132数值可选，0代表640*640正方形头像），用户没有头像时该项为空。若用户更换头像，原有头像URL将失效。
            public List<string> Privilege { get; set; } //用户特权信息，json 数组，如微信沃卡用户为（chinaunicom）
            public string Unionid { get; set; }//只有在用户将公众号绑定到微信开放平台帐号后，才会出现该字段。详见：获取用户个人信息（UnionID机制）
        }

        public class JsApi_Ticket
        {
            public string Access_token { get; set; }
            public string Ticket { get; set; }
            public DateTime Expires { get; set; }
        }

        public class wxmenu
        {
            public menu menu { get; set; }
        }

        public class menu
        {
            public button[] button { get; set; }
        }
        public class button
        {
            public string key { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string url { get; set; }
            public sub_button[] sub_button { get; set; }
        }
        public class sub_button
        {
            public string key { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string url { get; set; }
        }

        public class wx_menu
        {
            public bool cbxEnable1 { get; set; }
            public bool cbxEnable2 { get; set; }
            public bool cbxEnable3 { get; set; }
            public string rdotp1 { get; set; }
            public string rdotp2 { get; set; }
            public string rdotp3 { get; set; }
            public string txtbtnName1 { get; set; }
            public string txtbtnName2 { get; set; }
            public string txtbtnName3 { get; set; }
            public string txtmenuName1 { get; set; }
            public string txtmenuName2 { get; set; }
            public string txtmenuName3 { get; set; }
            public string txtbtntp1 { get; set; }
            public string txtbtntp2 { get; set; }
            public string txtbtntp3 { get; set; }
            public string txtbtnvalue1 { get; set; }
            public string txtbtnvalue2 { get; set; }
            public string txtbtnvalue3 { get; set; }

            public string txtsubbtnName1 { get; set; }
            public string txtsubbtntp1 { get; set; }
            public string txtsubbtnvalue1 { get; set; }
            public string txtsubbtnName2 { get; set; }
            public string txtsubbtntp2 { get; set; }
            public string txtsubbtnvalue2 { get; set; }
            public string txtsubbtnName3 { get; set; }
            public string txtsubbtntp3 { get; set; }
            public string txtsubbtnvalue3 { get; set; }
            public string txtsubbtnName4 { get; set; }
            public string txtsubbtntp4 { get; set; }
            public string txtsubbtnvalue4 { get; set; }
            public string txtsubbtnName5 { get; set; }
            public string txtsubbtntp5 { get; set; }
            public string txtsubbtnvalue5 { get; set; }
            public string txtsubbtnName6 { get; set; }
            public string txtsubbtntp6 { get; set; }
            public string txtsubbtnvalue6 { get; set; }
            public string txtsubbtnName7 { get; set; }
            public string txtsubbtntp7 { get; set; }
            public string txtsubbtnvalue7 { get; set; }
            public string txtsubbtnName8 { get; set; }
            public string txtsubbtntp8 { get; set; }
            public string txtsubbtnvalue8 { get; set; }
            public string txtsubbtnName9 { get; set; }
            public string txtsubbtntp9 { get; set; }
            public string txtsubbtnvalue9 { get; set; }
            public string txtsubbtnName10 { get; set; }
            public string txtsubbtntp10 { get; set; }
            public string txtsubbtnvalue10 { get; set; }
            public string txtsubbtnName11 { get; set; }
            public string txtsubbtntp11 { get; set; }
            public string txtsubbtnvalue11 { get; set; }
            public string txtsubbtnName12 { get; set; }
            public string txtsubbtntp12 { get; set; }
            public string txtsubbtnvalue12 { get; set; }
            public string txtsubbtnName13 { get; set; }
            public string txtsubbtntp13 { get; set; }
            public string txtsubbtnvalue13 { get; set; }
            public string txtsubbtnName14 { get; set; }
            public string txtsubbtntp14 { get; set; }
            public string txtsubbtnvalue14 { get; set; }
            public string txtsubbtnName15 { get; set; }
            public string txtsubbtntp15 { get; set; }
            public string txtsubbtnvalue15 { get; set; }
            public wx_menu()
            {
                cbxEnable1 = false;
                cbxEnable2 = false;
                cbxEnable3 = false;
                rdotp1 = string.Empty;
                rdotp2 = string.Empty;
                rdotp3 = string.Empty;
                txtbtnName1 = string.Empty;
                txtbtnName2 = string.Empty;
                txtbtnName3 = string.Empty;
                txtmenuName1 = string.Empty;
                txtmenuName2 = string.Empty;
                txtmenuName3 = string.Empty;
                txtbtntp1 = string.Empty;
                txtbtntp2 = string.Empty;
                txtbtntp3 = string.Empty;
                txtbtnvalue1 = string.Empty;
                txtbtnvalue2 = string.Empty;
                txtbtnvalue3 = string.Empty;

                txtsubbtnName1 = string.Empty;
                txtsubbtntp1 = string.Empty;
                txtsubbtnvalue1 = string.Empty;
                txtsubbtnName2 = string.Empty;
                txtsubbtntp2 = string.Empty;
                txtsubbtnvalue2 = string.Empty;
                txtsubbtnName3 = string.Empty;
                txtsubbtntp3 = string.Empty;
                txtsubbtnvalue3 = string.Empty;
                txtsubbtnName4 = string.Empty;
                txtsubbtntp4 = string.Empty;
                txtsubbtnvalue4 = string.Empty;
                txtsubbtnName5 = string.Empty;
                txtsubbtntp5 = string.Empty;
                txtsubbtnvalue5 = string.Empty;
                txtsubbtnName6 = string.Empty;
                txtsubbtntp6 = string.Empty;
                txtsubbtnvalue6 = string.Empty;
                txtsubbtnName7 = string.Empty;
                txtsubbtntp7 = string.Empty;
                txtsubbtnvalue7 = string.Empty;
                txtsubbtnName8 = string.Empty;
                txtsubbtntp8 = string.Empty;
                txtsubbtnvalue8 = string.Empty;
                txtsubbtnName9 = string.Empty;
                txtsubbtntp9 = string.Empty;
                txtsubbtnvalue9 = string.Empty;
                txtsubbtnName10 = string.Empty;
                txtsubbtntp10 = string.Empty;
                txtsubbtnvalue10 = string.Empty;
                txtsubbtnName11 = string.Empty;
                txtsubbtntp11 = string.Empty;
                txtsubbtnvalue11 = string.Empty;
                txtsubbtnName12 = string.Empty;
                txtsubbtntp12 = string.Empty;
                txtsubbtnvalue12 = string.Empty;
                txtsubbtnName13 = string.Empty;
                txtsubbtntp13 = string.Empty;
                txtsubbtnvalue13 = string.Empty;
                txtsubbtnName14 = string.Empty;
                txtsubbtntp14 = string.Empty;
                txtsubbtnvalue14 = string.Empty;
                txtsubbtnName15 = string.Empty;
                txtsubbtntp15 = string.Empty;
                txtsubbtnvalue15 = string.Empty;
            }
        }
    }
}