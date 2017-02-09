# AliPayWithWebAPI
A sample code for .NET WEB API integrate with AliPay 
# .NET WEB API 对接支付宝支付
最近一个项目中需要自己前后台全栈，几经权衡之后，在还是选择了自己最为熟悉的.NET WEB API技术来实现服务器端。可能是由于太久没接触.NET了，在对接支付宝APP支付的时候，遇到了不少坑，废话不多说，直接上代码吧。

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
    
用法如下：
        
         public async Task<HttpResponseMessage> AliPaySignString(AlipayRequestModel model)
        {
            var response = new SingleModelResponse<String>() as ISingleModelResponse<String>;
            try
            {
                await Task.Run(() =>
                {
                    string orderString = AliPayHelper.createRSASignedOrderString(Convert.ToDouble(model.price), model.description);
                    if (null == orderString)
                    {
                        response.DidError = true;
                        response.info = "签名失败";
                    }
                    else
                    {
                        response.Model = orderString;
                        response.info = "签名成功";
                    }
                });
            }
            catch (Exception ex)
            {
                response.DidError = true;
                response.info = ex.InnerException.Message;
            }
            return response.ToHttpResponse();
        }

上面的代码主要干了一件事情，生成签名后的订单字符串返回给app客户端，然后app客户端拿着这个字符串去调用支付宝SDK，发起支付请求。为什么要这么麻烦呢？一切都是为了安全，根据支付宝官方开发平台的解释，把签名的过程放在服务器端是要比放在客户端更为安全的一种策略。
由于网上关于.NET对接支付宝的文章和教程时效性都已经很低了，所以我把项目中的`AliPayHelper`代码放在这，供大家参考。

