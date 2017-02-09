using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Aop.Api;
using Aop.Api.Util;
using Com.Alipay;
using System.Text;

namespace XiaoMiProject.Helper
{
    public class AliPayHelper
    {
        private static string APP_ID = "";
        private static string CHARSET = "UTF-8";

        /// <summary>
        /// 生成RSA签名后的订单字符串
        /// </summary>
        /// <param name="price"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static string createRSASignedOrderString(double price,string description)
        {
            Dictionary<string, string> orderStringDict = new Dictionary<string, string>();
            orderStringDict.Add("app_id", APP_ID);
            orderStringDict.Add("method", "alipay.trade.app.pay");
            orderStringDict.Add("format", "JSON");
            orderStringDict.Add("charset", "utf-8");
            orderStringDict.Add("sign_type", "RSA");
            orderStringDict.Add("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            orderStringDict.Add("version", "1.0");
            orderStringDict.Add("notify_url", "");
            orderStringDict.Add("biz_content", generateBizContentString(price.ToString(), description));

            // 排序拼接成字符串
            string orderInfo = AlipaySignature.GetSignContent(orderStringDict);
            string orderInfoEncoded = Core.CreateLinkStringUrlencode(orderStringDict, (new System.Text.UTF8Encoding()));

            // 签名
            string privateKeyPem = GetCurrentPath() + "rsa_private_key.pem";
            string signString = AlipaySignature.RSASign(orderInfo, privateKeyPem, null, "RSA");

            signString = HttpUtility.UrlEncode(signString, new UTF8Encoding());

            // 加上sign
            string orderString = orderInfoEncoded + "&sign=" + signString;

            // 拼接最终返回给客户端的字符串
            return orderString;
        }

        static String BytesToBase64(Byte[] bytes)
        {
            try
            {
                return Convert.ToBase64String(bytes);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取私钥的路径
        /// </summary>
        /// <returns></returns>
        private static string GetCurrentPath()
        {
            string strPath = "/Helper/";
            if (HttpContext.Current != null)
            {
                return HttpContext.Current.Server.MapPath(strPath);
            }
            else //非web程序引用 
            {
                strPath = strPath.Replace("/", "\\");
                if (strPath.StartsWith("\\"))
                {
                    //strPath = strPath.Substring(strPath.IndexOf('\\', 1)).TrimStart('\\'); 
                    strPath = strPath.TrimStart('\\');
                }
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, strPath);
            }
        }

        /// <summary>
        /// 生成业务参数
        /// </summary>
        /// <param name="price"></param>
        /// <param name="descripiton"></param>
        /// <returns></returns>
        private static string generateBizContentString(string price, string descripiton)
        {
            Dictionary<string, string> bizContent = new Dictionary<string, string>();
            bizContent.Add("subject", descripiton);
            bizContent.Add("body", descripiton);
            bizContent.Add("out_trade_no", generateOrderNumber());
            bizContent.Add("timeout_express", "90m");
            bizContent.Add("total_amount", price);
            bizContent.Add("product_code", "QUICK_MSECURITY_PAY");

            string bizContentJsonString = (new System.Web.Script.Serialization.JavaScriptSerializer()).Serialize(bizContent);
            return bizContentJsonString;
        }

        private static string generateOrderNumber()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }
    }
}