﻿// ======================================================================
//  
//          Copyright (C) 2016-2020 湖南心莱信息科技有限公司    
//          All rights reserved
//  
//          filename : WeChatConfigManager.cs
//          description :
//  
//          created by 李文强 at  2016/09/23 17:10
//          Blog：http://www.cnblogs.com/codelove/
//          GitHub ： https://github.com/xin-lai
//          Home：http://xin-lai.com
//  
// ======================================================================

namespace Magicodes.WeChat.SDK
{
    using Magicodes.WeChat.SDK.Apis.Ticket;
    using Magicodes.WeChat.SDK.Apis.Token;
    using Magicodes.WeChat.SDK.Helper;
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// 微信配置管理对象
    /// </summary>
    public class WeChatConfigManager
    {
        /// <summary>
        /// Defines the Lazy
        /// </summary>
        private static readonly Lazy<WeChatConfigManager> Lazy =
            new Lazy<WeChatConfigManager>(() => new WeChatConfigManager());

        /// <summary>
        /// 访问凭据存储
        /// </summary>
        internal ConcurrentDictionary<string, TokenApiResult> AccessTokenConcurrentDictionary =
            new ConcurrentDictionary<string, TokenApiResult>();

        /// <summary>
        /// 卡券JSSDK访问凭证
        /// </summary>
        internal ConcurrentDictionary<string, TicketApiResult> CardTicketConcurrentDictionary =
            new ConcurrentDictionary<string, TicketApiResult>();

        /// <summary>
        /// 凭证
        /// </summary>
        internal ConcurrentDictionary<string, TicketApiResult> TicketConcurrentDictionary =
            new ConcurrentDictionary<string, TicketApiResult>();

        /// <summary>
        /// 公众号配置信息
        /// </summary>
        protected ConcurrentDictionary<object, IWeChatConfig> WeChatConfigs =
            new ConcurrentDictionary<object, IWeChatConfig>();

        /// <summary>
        /// 微信支付配置信息
        /// </summary>
        protected ConcurrentDictionary<object, IWeChatPayConfig> WeChatPayConfigs =
            new ConcurrentDictionary<object, IWeChatPayConfig>();

        /// <summary>
        /// Gets the Current
        /// </summary>
        public static WeChatConfigManager Current => Lazy.Value;

        /// <summary>
        /// The GetKey
        /// </summary>
        /// <returns>The <see cref="object"/></returns>
        public object GetKey()
        {
            return WeChatFrameworkFuncsManager.Current.InvokeFunc(WeChatFrameworkFuncTypes.GetKey,
                new WeChatApiCallbackFuncArgInfo());
        }

        /// <summary>
        /// 获取支付配置
        /// </summary>
        /// <param name="key">唯一Key</param>
        /// <returns></returns>
        public IWeChatPayConfig GetPayConfig(object key = null)
        {
            if (key == null)
                key = GetKey();
            if (key == null)
                throw new Exception("Key不能为NULL！");

            if (WeChatPayConfigs.ContainsKey(key))
                return WeChatPayConfigs[key];
            var result =
                WeChatFrameworkFuncsManager.Current.InvokeFunc(
                    WeChatFrameworkFuncTypes.Config_GetWeChatPayConfigByKey, new WeChatPayCallbackFuncArgInfo
                    {
                        //Api = null,
                        Data = key
                    });
            if (result == null) throw new Exception(string.Format("通过Key：{0}获取Config失败！", key));
            var weChatConfig = result as IWeChatPayConfig;
            WeChatPayConfigs.AddOrUpdate(key, weChatConfig, (tKey, existingVal) => weChatConfig);
            return weChatConfig;
        }

        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="key">唯一Key</param>
        /// <returns></returns>
        public IWeChatConfig GetConfig(object key = null)
        {
            if (key == null)
                key = GetKey();
            if (key == null)
                throw new Exception("Key不能为NULL！");

            IWeChatConfig weChatConfig = null;
            if (WeChatConfigs.ContainsKey(key))
                weChatConfig = WeChatConfigs[key];
            if (weChatConfig != null)
                return weChatConfig;
            var result =
                WeChatFrameworkFuncsManager.Current.InvokeFunc(WeChatFrameworkFuncTypes.Config_GetWeChatConfigByKey,
                    new WeChatApiCallbackFuncArgInfo
                    {
                        Api = null,
                        Data = key
                    });
            weChatConfig = result as IWeChatConfig ?? throw new Exception(string.Format("通过Key：{0}获取Config失败！", key));
            if (weChatConfig == null)
                throw new Exception("获取微信配置失败。Key:" + key);
            if (string.IsNullOrWhiteSpace(weChatConfig.AppId))
                throw new ApiArgumentException("微信配置错误，参数不能为空。Key:" + key, "AppId");
            if (string.IsNullOrWhiteSpace(weChatConfig.AppSecret))
                throw new ApiArgumentException("微信配置错误，参数不能为空。Key:" + key, "AppSecret");
            WeChatConfigs.AddOrUpdate(key, weChatConfig, (tKey, existingVal) => weChatConfig);
            return weChatConfig;
        }

        /// <summary>
        /// 获取当前页面JS配置信息
        /// </summary>
        /// <param name="absoluteUrl">The absoluteUrl<see cref="string"/></param>
        /// <returns></returns>
        public JSSDKConfigInfo GetJSSDKConfigInfo(string absoluteUrl)
        {
            var ticket = WeChatApisContext.Current.TicketApi.SafeGet().Ticket;
            var configInfo = new JSSDKConfigInfo
            {
                AppId = Current.GetConfig().AppId,
                Timestamp = JSSDKHelper.GetTimestamp(),
                NonceStr = JSSDKHelper.GetNoncestr()
            };
            configInfo.Signature = JSSDKHelper.GetSignature(ticket, configInfo.NonceStr, configInfo.Timestamp,
                absoluteUrl);
            return configInfo;
        }

        /// <summary>
        /// 获取当前页面卡券JS配置信息
        /// </summary>
        /// <returns></returns>
        public JSSDKConfigInfo GetCardJSSDKConfigInfo()
        {
            var ticket = WeChatApisContext.Current.TicketApi.GetSafeCardAccessToken().Ticket;
            var configInfo = new JSSDKConfigInfo
            {
                AppId = Current.GetConfig().AppId,
                Timestamp = JSSDKHelper.GetTimestamp(),
                NonceStr = JSSDKHelper.GetNoncestr()
            };
            return configInfo;
        }

        /// <summary>
        /// 接口访问凭据
        /// </summary>
        /// <param name="key">The key<see cref="object"/></param>
        /// <returns></returns>
        public string GetAccessToken(object key = null)
        {
            if (key == null)
                return WeChatApisContext.Current.TokenApi.SafeGet().AccessToken;
            var api = new TokenApi();
            api.SetKey(key);
            return api.SafeGet().AccessToken;
        }

        /// <summary>
        /// 刷新访问凭据
        /// </summary>
        /// <param name="key">The key<see cref="object"/></param>
        public void RefreshAccessToken(object key = null)
        {
            if (key == null)
            {
                WeChatApisContext.Current.TokenApi.Update();
                return;
            }
            var api = new TokenApi();
            api.SetKey(key);
            api.Update();
        }

        /// <summary>
        /// 刷新配置
        /// </summary>
        /// <param name="key">存储配置key</param>
        /// <param name="config">微信配置</param>
        public void RefreshConfig(object key, IWeChatConfig config)
        {
            WeChatConfigs.AddOrUpdate(key, config, (tKey, existingVal) => { return config; });
        }

        /// <summary>
        /// 刷新支付配置
        /// </summary>
        /// <param name="key">存储配置key</param>
        /// <param name="config">支付配置，选填</param>
        public void RefreshPayConfig(object key, IWeChatPayConfig config = null)
        {
            if (config == null)
                config = GetPayConfig(key);
            WeChatPayConfigs.AddOrUpdate(key, config, (tKey, existingVal) => { return config; });
        }

        /// <summary>
        /// 刷新配置以及访问凭据
        /// </summary>
        /// <param name="key"></param>
        /// <param name="config"></param>
        public void RefreshConfigAndAccessToken(object key, IWeChatConfig config)
        {
            RefreshConfig(key, config);
            RefreshAccessToken(key);
        }
    }
}
