using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;

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
            //dict.Add(noncestr, "noncestr");
            //dict.Add(jsapi_ticket, "jsapi_ticket");
            //dict.Add(timestamp, "timestamp");
            //dict.Add(url, "url");
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

        //读取一个请求的返回全文
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
    }
}