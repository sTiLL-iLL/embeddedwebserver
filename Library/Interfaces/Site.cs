﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Org.Reddragonit.EmbeddedWebServer.Components;
using Org.Reddragonit.EmbeddedWebServer.BasicHandlers;
using Org.Reddragonit.EmbeddedWebServer.Sessions;
using System.Threading;
using Org.Reddragonit.EmbeddedWebServer.Attributes;
using Org.Reddragonit.EmbeddedWebServer.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Org.Reddragonit.EmbeddedWebServer.Components.Message;

namespace Org.Reddragonit.EmbeddedWebServer.Interfaces
{
    public abstract class Site : IBackgroundOperationContainer
    {
        //Statically holds the current site being used in the current thread
        [ThreadStatic()]
        private static Site _currentSite;
        public static Site CurrentSite
        {
            get { return _currentSite; }
        }

        //called by the handler at the start of the asyncrhonous process to tag which is the current site for the 
        //currently running thread
        internal static void SetCurrentSite(Site site)
        {
            _currentSite = site;
        }

        #region virtual
        //what IPaddress and port pairs to bind the site to
        public virtual sIPPortPair[] ListenOn
        {
            get { return new sIPPortPair[] { new sIPPortPair(IPAddress.Any, 80,false) }; }
        }

        public virtual X509Certificate GetCertificateForEndpoint(sIPPortPair pair)
        {
            throw new NotImplementedException();
        }

        //The host name that the site represents
        public virtual string ServerName
        {
            get { return null; }
        }

        //hose aliases that will work as representation
        public virtual string[] Aliases
        {
            get { return null; }
        }

        //indicates if the site should allow post
        public virtual bool AllowPOST
        {
            get { return true; }
        }

        //indicates if the site should allow get
        public virtual bool AllowGET
        {
            get { return true; }
        }

        //indiciates if the site should compress JS files
        public virtual bool CompressJS
        {
            get { return true; }
        }

        //indiciates if the site should compress CSS files
        public virtual bool CompressCSS
        {
            get { return true; }
        }

        //indicates the type of session storage that the site uses, defaults to no sessions
        public virtual SiteSessionTypes SessionStateType
        {
            get { return SiteSessionTypes.None; }
        }

        //indicates the temporary path that the site uses to store information
        public virtual string TMPPath
        {
            get { return "/tmp"; }
        }

        //indicates the base file system path to use for the site 
        public virtual string BaseSitePath
        {
            get { return null; }
        }

        //returns the list of embedded files available for the site
        public virtual Dictionary<string,sEmbeddedFile> EmbeddedFiles
        {
            get { return null; }
        }

        //indicates the time to hold a session before expiring it
        public virtual int SessionTimeoutMinutes
        {
            get { return 60; }
        }

        //indicates the time to hold a cookie for the site.  If not set, it assumes 60 minute default
        public virtual int CookieExpireMinutes
        {
            get { return Org.Reddragonit.EmbeddedWebServer.Components.Message.CookieCollection.DEFAULT_COOKIE_DURATION_MINUTES; }
        }

        private static readonly IRequestHandler[] _defaultHandlers = new IRequestHandler[]{
            new DirectoryBrowsingHandler(),
            new EmbeddedServiceHandler(),
            new EmbeddedResourceHandler(),
            new FileHandler()
        };

        public void DeployPath(string url, IDirectoryFolder folder)
        {
            lock (Handlers)
            {
                foreach (IRequestHandler irh in Handlers)
                {
                    if (irh is DirectoryBrowsingHandler)
                    {
                        ((DirectoryBrowsingHandler)irh).DeployPath(url, folder);
                        break;
                    }
                }
            }
        }

        public void RemovePath(string url)
        {
            lock (Handlers)
            {
                foreach (IRequestHandler irh in Handlers)
                {
                    if (irh is DirectoryBrowsingHandler)
                    {
                        ((DirectoryBrowsingHandler)irh).RemovePath(url);
                        break;
                    }
                }
            }
        }

        //returns the list of handlers that the site uses
        public virtual List<IRequestHandler> Handlers
        {
            get
            {
                return new List<IRequestHandler>(_defaultHandlers);
            }
        }

        //indicates the amount of minutes a cached item should be cached for
        public virtual int CacheItemExpiryMinutes
        {
            get { return 60; }
        }

        //Indicates the available embedded services that the site uses
        public virtual List<Type> EmbeddedServiceTypes
        {
            get
            {
                return Utility.LocateTypeInstances(typeof(EmbeddedService));
            }
        }

        //indicates to add the json javascript with a service javascript request.
        //set true if the pages involved don't reference the json javascript by default
        public virtual bool AddJsonJavascript
        {
            get { return true; }
        }

        //indicates to add the jquery javascript with a service javascript request.
        //set true if the pages involved don't reference the jquery javascript by default
        public virtual bool AddJqueryJavascript
        {
            get { return true; }
        }

        //indicated the request timeout for a given site, if not overriden use the default
        public virtual int RequestTimeout
        {
            get { return Settings.RequestTimeout; }
        }

        //indicates the diagnostics level the site should use for logging
        public virtual DiagnosticsLevels DiagnosticsLevel
        {
            get{ return Settings.DiagnosticsLevel; }
        }

        //indicates the location to write these logs to
        public virtual DiagnosticsOutputs DiagnosticsOutput
        {
            get { return Settings.DiagnosticsOutput; }
        }

        //indicates the port to bind the logging socket to
        public virtual IPEndPoint RemoteLoggingServer
        {
            get { return new IPEndPoint(IPAddress.Broadcast,8081); }
        }

        //indicates the default path when none is specified in the request
        public virtual string DefaultPage(bool mobileBrowser)
        {
            return "/index.html";
        }

        //an implemented start that gets called before the handlers are initialized
        protected virtual void PreStart(){}
        //an implemented start that gets called after all the handlers are initialized
        protected virtual void PostStart(){}
        //an implemented stop that gets called before the handlers are deinitialized
        protected virtual void PreStop() { }
        //an implemented stop that gets called after the handlers are deinitialized
        protected virtual void PostStop() { }
        //an implemented function that gets called before sending the response headers
        public virtual void PreSendResponseHeaders(HttpRequest request) { }
        //and implemented function that gets called before sending the response data 
        public virtual void PreSendResponseData(HttpRequest request) { }
        //an implemented function that gets called before the processing of a request
        protected virtual void PreRequest(HttpRequest request) { }
        //an implemented function that gets called after the processing of a request
        protected virtual void PostRequest(HttpRequest request) { }
        //an implemented function that gets called when an error occurs while processing the request
        //returns true if this completes the request, else default response information gets sent
        protected virtual bool RequestError(HttpRequest request, Exception error)
        {
            return false;
        }

        protected virtual HttpAuthTypes GetAuthenticationTypeForUrl(Uri url,out string realm)
        {
            realm = null;
            return HttpAuthTypes.None;
        }

        protected virtual sHttpAuthUsernamePassword[] GetAuthenticationInformationForUrl(Uri url,string username)
        {
            return null;
        }

        protected virtual void PostAuthentication(HttpRequest request, sHttpAuthUsernamePassword user) { }

        protected virtual void PreAuthentication(HttpRequest request, out bool loadSession) { loadSession = false; }

        protected virtual void PostAuthenticationFailure(HttpRequest request,string username) { }
        #endregion

        private string _id;
        internal string ID
        {
            get { return _id; }
            set { _id = value; }
        }

        private DateTime _startTimestamp;
        public DateTime StartTimestamp { get { return _startTimestamp; } }

        //Called to start the site up, runs all required initializations
        public void Start()
        {
            _startTimestamp = DateTime.Now;
            _currentSite = this;
            _handlerCache = new Dictionary<string, CachedItemContainer>();
            PreStart();
            foreach (IRequestHandler handler in Handlers)
            {
                handler.Init();
            }
            PostStart();
        }

        //Called to stop the site, runs all deinits and the internal stop function
        public void Stop()
        {
            _currentSite = this;
            _handlerCache.Clear();
            PreStop();
            foreach (IRequestHandler handler in Handlers)
            {
                handler.DeInit();
            }
            PostStop();
        }

        //used to lock the site cache object for thread safe access
        private static object _lock = new object();
        //The cache designed to hold site specific data, its static to ensure proper usage between requests
        private static Dictionary<string,Dictionary<string, CachedItemContainer>> _cache;

        /*
         * This function is a background threaded operation that every minute
         * will check the cache to see what, if any items needs to be cleaned
         * for each site that is loaded in the server control.  Once all caches have 
         * been cleaned, it calls the Garbage Collector in order to attempt
         * a forced memory release.
         */
        [BackgroundOperationCall(-1, -1, -1, -1, BackgroundOperationDaysOfWeek.All)]
        public static void CleanCaches()
        {
            List<Site> sites = ServerControl.Sites;
            foreach (Site site in sites)
            {
                Monitor.Enter(_lock);
                if (_cache == null)
                    _cache = new Dictionary<string, Dictionary<string, CachedItemContainer>>();
                if (_cache.ContainsKey(site.ID))
                {
                    string[] keys = new string[_cache[site.ID].Count];
                    _cache[site.ID].Keys.CopyTo(keys, 0);
                    foreach (string str in keys)
                    {
                        if (DateTime.Now.Subtract(_cache[site.ID][str].LastAccess).TotalMinutes > site.CacheItemExpiryMinutes)
                            _cache[site.ID].Remove(str);
                    }
                }
                Monitor.Exit(_lock);
            }
            GC.Collect();
        }

        //This property is used to return an object contained within the cache,
        //or add/update one in the cache
        public object this[string cachedItemName]
        {
            get
            {
                object ret = null;
                Monitor.Enter(_lock);
                if (_cache == null)
                    _cache = new Dictionary<string, Dictionary<string, CachedItemContainer>>();
                if (!_cache.ContainsKey(ID))
                    _cache.Add(ID, new Dictionary<string, CachedItemContainer>());
                if (_cache[ID].ContainsKey(cachedItemName))
                    ret = _cache[ID][cachedItemName].Value;
                Monitor.Exit(_lock);
                return ret;
            }
            set
            {
                Monitor.Enter(_lock);
                if (_cache == null)
                    _cache = new Dictionary<string, Dictionary<string, CachedItemContainer>>();
                if (!_cache.ContainsKey(ID))
                    _cache.Add(ID, new Dictionary<string, CachedItemContainer>());
                if (_cache[ID].ContainsKey(cachedItemName))
                    _cache[ID].Remove(cachedItemName);
                if (value!=null)
                    _cache[ID].Add(cachedItemName, new CachedItemContainer(value));
                Monitor.Exit(_lock);
            }
        }

        public List<string> CachedItemKeys
        {
            get{
                Monitor.Enter(_lock);
                if (_cache == null)
                    _cache = new Dictionary<string, Dictionary<string, CachedItemContainer>>();
                if (!_cache.ContainsKey(ID))
                    _cache.Add(ID, new Dictionary<string, CachedItemContainer>());
                string[] tmp = new string[_cache[ID].Count];
                _cache[ID].Keys.CopyTo(tmp, 0);
                Monitor.Exit(_lock);
                return new List<string>(tmp);
            }
        }

        private Dictionary<string, CachedItemContainer> _handlerCache;

        [BackgroundOperationCall(-1,-1,-1,-1,BackgroundOperationDaysOfWeek.All)]
        internal static void CleanHandlers()
        {
            List<Site> sites = ServerControl.Sites;
            foreach (Site site in sites)
            {
                Monitor.Enter(site._handlerCache);
                string[] keys = new string[site._handlerCache.Count];
                site._handlerCache.Keys.CopyTo(keys, 0);
                foreach (string str in keys)
                {
                    if (DateTime.Now.Subtract(site._handlerCache[str].LastAccess).TotalMilliseconds > 60)
                        site._handlerCache.Remove(str);
                }
                Monitor.Exit(site._handlerCache);
            }
            GC.Collect();
        }

        /*
         * This function is called by the port listener once the appropriate site has
         * been located to process the given request.  It scans through all 
         * available handlers and attempts to find the appropriate one.  Once 
         * it has found the appropriate handler, it flags this fact, then 
         * checks if the handler is reusable.  If so, run the request through, checking for
         * the requirement of a session first.  If not, create a new instance, init the instance, and use the new instance 
         * to perform the same tasks, once completed deinit the handler.
         */
        public void ProcessRequest(HttpRequest request)
        {
            PreRequest(request);
            if (!request.IsResponseSent)
            {
                HttpRequest.SetCurrentRequest(request);
                DateTime start = DateTime.Now;
                _currentSite = this;
                string realm = null;
                bool authed = false;
                bool loadSession = false;
                this.PreAuthentication(request, out loadSession);
                if (loadSession)
                    SessionManager.LoadStateForConnection(request, this);
                HttpAuthTypes authType = this.GetAuthenticationTypeForUrl(request.URL, out realm);
                switch (authType)
                {
                    case HttpAuthTypes.Digest:
                        if (!(request.Headers["Authorization"] == null ? "" : request.Headers["Authorization"]).Trim().StartsWith("Digest"))
                        {
                            request.ResponseHeaders["WWW-Authenticate"] = "Digest realm=\"" + realm + "\", nonce=\"" + Convert.ToBase64String(_rand.NextBytes(4)) + "\"";
                            request.ResponseStatus = HttpStatusCodes.Unauthorized;
                        }
                        else
                        {
                            Dictionary<string, string> pars = _ExtractAuthData(request.Headers["Authorization"].Trim().Substring(request.Headers["Authorization"].Trim().IndexOf(' ') + 1));
                            string dpass = pars["response"];
                            foreach (sHttpAuthUsernamePassword usr in GetAuthenticationInformationForUrl(request.URL, pars["username"]))
                            {
                                if (usr.GetDigestString(realm, request.Method, pars["uri"], pars["nonce"]) == dpass)
                                {
                                    authed = true;
                                    this.PostAuthentication(request, usr);
                                    break;
                                }
                            }
                            if (!authed)
                            {
                                PostAuthenticationFailure(request, pars["username"]);
                                request.ResponseStatus = HttpStatusCodes.Unauthorized;
                                request.ResponseHeaders["WWW-Authenticate"] = "Digest realm=\"" + realm + "\", nonce=\"" + Convert.ToBase64String(_rand.NextBytes(4)) + "\"";
                            }
                        }
                        break;
                    case HttpAuthTypes.Basic:
                        if (!(request.Headers["Authorization"] == null ? "" : request.Headers["Authorization"]).Trim().StartsWith("Basic"))
                        {
                            request.ResponseHeaders["WWW-Authenticate"] = "Basic realm=\"" + realm + "\"";
                            request.ResponseStatus = HttpStatusCodes.Unauthorized;
                        }
                        else
                        {
                            string bpass = request.Headers["Authorization"].Trim().Split(' ')[1];
                            foreach (sHttpAuthUsernamePassword usr in GetAuthenticationInformationForUrl(request.URL, ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(bpass)).Split(':')[0]))
                            {
                                if (usr.BasicAuthorizationString == bpass)
                                {
                                    authed = true;
                                    this.PostAuthentication(request, usr);
                                    break;
                                }
                            }
                            if (!authed)
                            {
                                PostAuthenticationFailure(request, ASCIIEncoding.ASCII.GetString(Convert.FromBase64String(bpass)).Split(':')[0]);
                                request.ResponseStatus = HttpStatusCodes.Unauthorized;
                                request.ResponseHeaders["WWW-Authenticate"] = "Basic realm=\"" + realm + "\"";
                            }
                        }
                        break;
                    case HttpAuthTypes.None:
                        authed = true;
                        break;
                }
                if (authed)
                {
                    IRequestHandler handler = null;
                    Monitor.Enter(_handlerCache);
                    if (_handlerCache.ContainsKey(request.URL.AbsolutePath))
                        handler = (IRequestHandler)_handlerCache[request.URL.AbsolutePath].Value;
                    Monitor.Exit(_handlerCache);
                    if (handler == null)
                    {
                        foreach (IRequestHandler ih in Handlers)
                        {
                            if (ih.CanProcessRequest(request, this))
                            {
                                handler = ih;
                                Monitor.Enter(_handlerCache);
                                if (!_handlerCache.ContainsKey(request.URL.AbsolutePath))
                                    _handlerCache.Add(request.URL.AbsolutePath, new CachedItemContainer(handler));
                                Monitor.Exit(_handlerCache);
                                break;
                            }
                        }
                    }
                    if (handler == null)
                    {
                        request.ClearResponse();
                        request.ResponseStatus = HttpStatusCodes.Not_Found;
                    }
                    else
                    {
                        Logger.LogMessage(DiagnosticsLevels.TRACE, "Time to determine handler for URL " + request.URL.AbsolutePath + " = " + DateTime.Now.Subtract(start).TotalMilliseconds + " ms");
                        if (handler.IsReusable)
                        {
                            if (handler.RequiresSessionForRequest(request, this) || (DefaultPage(request.IsMobile) == request.URL.AbsolutePath && SessionStateType != SiteSessionTypes.None))
                                SessionManager.LoadStateForConnection(request, this);
                            try
                            {
                                handler.ProcessRequest(request, this);
                            }
                            catch (ThreadAbortException tae)
                            {
                                if (!request.IsResponseSent)
                                {
                                    request.ResponseStatus = HttpStatusCodes.Request_Timeout;
                                    request.ResponseWriter.WriteLine("The request has taken to long to process.");
                                    request.SendResponse();
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e);
                                if (!RequestError(request, e))
                                {
                                    request.ResponseStatus = HttpStatusCodes.Internal_Server_Error;
                                    request.ClearResponse();
                                    request.ResponseWriter.Write(e.Message);
                                }
                            }
                            if (handler.RequiresSessionForRequest(request, this) || (DefaultPage(request.IsMobile) == request.URL.AbsolutePath && SessionStateType != SiteSessionTypes.None))
                                SessionManager.StoreSessionForConnection(request, this);
                        }
                        else
                        {
                            IRequestHandler hndl = (IRequestHandler)handler.GetType().GetConstructor(Type.EmptyTypes).Invoke(new object[0]);
                            hndl.Init();
                            if (hndl.RequiresSessionForRequest(request, this))
                                SessionManager.LoadStateForConnection(request, this);
                            try
                            {
                                hndl.ProcessRequest(request, this);
                            }
                            catch (ThreadAbortException tae)
                            {
                                if (!request.IsResponseSent)
                                {
                                    request.ResponseStatus = HttpStatusCodes.Request_Timeout;
                                    request.ResponseWriter.WriteLine("The request has taken to long to process.");
                                    request.SendResponse();
                                }
                            }
                            catch (Exception e)
                            {
                                Logger.LogError(e);
                                if (!RequestError(request, e))
                                {
                                    request.ResponseStatus = HttpStatusCodes.Internal_Server_Error;
                                    request.ClearResponse();
                                    request.ResponseWriter.Write(e.Message);
                                }
                            }
                            if (hndl.RequiresSessionForRequest(request, this))
                                SessionManager.StoreSessionForConnection(request, this);
                            hndl.DeInit();
                        }
                    }
                }
            }
            PostRequest(request);
            if (request.IsResponseSent)
                Logger.LogMessage(DiagnosticsLevels.DEBUG, "WARNING:  Response has already been sent before site called to send it.");
            else
                request.SendResponse();
            try
            {
                request.Connection.DisposeRequest(request);
            }
            catch (Exception e) { }
        }

        private Dictionary<string, string> _ExtractAuthData(string bpass)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>();
            bpass = bpass.Trim();
            while (bpass.Contains("="))
            {
                try
                {
                    string name = bpass.Substring(0, bpass.IndexOf("=")).Trim();
                    bpass = bpass.Substring(bpass.IndexOf("=") + 1);
                    string val = "";
                    if (bpass.StartsWith("\""))
                    {
                        val = bpass.Substring(1, bpass.IndexOf("\"",1)-1).Trim();
                        bpass = bpass.Substring(bpass.IndexOf("\"",1)+1).TrimStart(',');
                    }
                    else
                    {
                        if (bpass.Contains(","))
                        {
                            val = bpass.Substring(0, bpass.IndexOf(","));
                            bpass = bpass.Substring(val.Length + 1).TrimStart(',');
                        }
                        else
                            val = bpass;
                    }
                    ret.Add(name, val);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            return ret;
        }

        private MT19937 _rand;

        public Site() {
            _rand = new MT19937(DateTime.Now.Ticks);
        }

        public string MapPath(string path)
        {
            if (BaseSitePath != null)
                return BaseSitePath + Path.DirectorySeparatorChar + path.Replace('/', Path.DirectorySeparatorChar);
            return null;
        }
    }
}
