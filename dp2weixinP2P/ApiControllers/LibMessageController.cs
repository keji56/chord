﻿using DigitalPlatform.IO;
using dp2weixin.service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace dp2weixinWeb.ApiControllers
{
    public class LibMessageController : ApiController
    {
        public ApiResult GetTemplate(string group,
            string libId,
            string subject)
        {
            ApiResult result = new ApiResult();
            //result.info = "test";


            LibItem lib = LibDatabase.Current.GetLibById(libId);
            if (lib == null)
            {
                result.errorCode = -1;
                result.errorInfo = "未找到id为'"+libId+"'的图书馆";
                return result;
            }


            string file = dp2WeiXinService.Instance.weiXinDataDir 
                + "/lib/" + lib.capoUserName 
                + "/homePage/" 
                + subject+".html";

                // 文件存在，取出文件 的内容
            string text = "";
            string strError = "";
            if (System.IO.File.Exists(file) == true)
            {
                Encoding encoding;
                // 能自动识别文件内容的编码方式的读入文本文件内容模块
                // parameters:
                //      lMaxLength  装入的最大长度。如果超过，则超过的部分不装入。如果为-1，表示不限制装入长度
                // return:
                //      -1  出错 strError中有返回值
                //      0   文件不存在 strError中有返回值
                //      1   文件存在
                //      2   读入的内容不是全部
                int nRet = FileUtil.ReadTextFileContent(file,
                    -1,
                    out text,
                    out encoding,
                    out strError);
                if (nRet == -1 || nRet == 0)
                {
                    goto ERROR1;
                }
                if (nRet == 2)
                {
                    strError="FileUtil.ReadTextFileContent() error";
                    goto ERROR1;
                }

                result.info = text;
            }


            return result;

        ERROR1:
            result.errorInfo = strError;
        result.errorCode = -1;
        return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="weixinId"></param>
        /// <param name="libId"></param>
        /// <param name="msgId"></param>
        /// <param name="style">browse/full</param>
        /// <returns></returns>
        public SubjectResult GetSubject(string weixinId, 
            string group,
            string libId,
            string selSubject,
            string param)
        {
            SubjectResult result = new SubjectResult();
            string strError = "";

            // 获取栏目
            List<SubjectItem> list = null;
            int nRet = dp2WeiXinService.Instance.GetSubject(libId, group,
                out list, out strError);
            if (nRet == -1)
            {
                result.errorCode = -1;
                result.errorInfo = strError;
            }
            if (param.Contains("list")==true)
                result.list = list;

            string html = dp2WeiXinService.Instance.GetSubjectHtml(libId,
                dp2WeiXinService.C_Group_Book,
               selSubject,
               true,
               list);
            if (param.Contains("html") == true)
                result.html = html;

            result.errorCode = nRet;
            result.errorInfo = strError;

            return result;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="weixinId"></param>
        /// <param name="libId"></param>
        /// <param name="msgId"></param>
        /// <param name="style">browse/full</param>
        /// <returns></returns>
        public MessageResult GetMessage(string weixinId, 
            string group,
            string libId, 
            string msgId,
            string subject,
            string style)
        {
            MessageResult result = new MessageResult();

            if (group != dp2WeiXinService.C_Group_Bb
                && group != dp2WeiXinService.C_Group_Book
                && group != dp2WeiXinService.C_Group_HomePage)
            {
                result.errorInfo = "不支持的群" + group;
                result.errorCode = -1;
                return result;
            }

            string strError="";

            // 检查下有无绑定工作人员账号
            result.userName = "";
            string userName = "";
            if (string.IsNullOrEmpty(weixinId) == false)
            {
                // 查找当前微信用户绑定的工作人员账号
                WxUserItem user = WxUserDatabase.Current.GetWorker(weixinId, libId);
                if (user != null)
                {
                    // 检索是否有权限 _wx_setbbj
                    string needRight = "";
                    if (group == dp2WeiXinService.C_Group_Bb)
                        needRight = dp2WeiXinService.C_Right_SetBb;
                    else if (group == dp2WeiXinService.C_Group_Book)
                        needRight = dp2WeiXinService.C_Right_SetBook;
                    else if (group == dp2WeiXinService.C_Group_HomePage)
                        needRight = dp2WeiXinService.C_Right_SetHomePage;

                    LibItem lib = LibDatabase.Current.GetLibById(libId);
                    if (lib == null)
                    {
                        result.errorInfo = "未找到id为[" + libId + "]的图书馆定义。";
                        result.errorCode = -1;
                        return result;
                    }

                    int nHasRights = dp2WeiXinService.Instance.CheckRights(lib.capoUserName,
                        user.userName,
                        needRight,
                        out strError);
                    if (nHasRights == -1)
                    {
                        result.errorInfo = strError;
                        result.errorCode = -1;
                        return result;
                    }
                    if (nHasRights == 1)
                    {
                        userName = user.userName;
                    }
                    else
                    {
                        userName = "";
                    }
                }
            }
            result.userName = userName;

            List<MessageItem> list = null;
            int nRet = dp2WeiXinService.Instance.GetMessage(group,
                libId,
                msgId, 
                subject,
                style,
                out list,
                out strError);
            if (nRet == -1)
            {
                result.errorCode = -1;
                result.errorInfo = strError;
            }
            result.items = list;
            
            result.errorCode = nRet;
            result.errorInfo = strError;

            return result;
        }

        // POST api/<controller>
        public MessageResult Post(string group, string libId, string parameters, MessageItem item)
        {
            // 服务器会自动产生id
            //item.id = Guid.NewGuid().ToString();`'
            return dp2WeiXinService.Instance.CoverMessage(group, libId, item,"create",parameters );
        }

        // PUT api/<controller>/5
        public MessageResult Put(string group, string libId, MessageItem item)
        {
            return dp2WeiXinService.Instance.CoverMessage(group, libId, item,"change", "");
     }

        // DELETE api/<controller>/5
        [HttpDelete]
        public MessageResult Delete(string group, string libId, string msgId,string userName)
        {
            MessageItem item = new MessageItem();
            item.id = msgId;
            item.creator = userName;
            //style == delete
            return dp2WeiXinService.Instance.CoverMessage(group, libId, item, "delete","");
        }
    }
}