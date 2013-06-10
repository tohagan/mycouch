﻿using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using EnsureThat;
using MyCouch.Core;
using MyCouch.Resources;

namespace MyCouch.Net
{
    public class BasicHttpClientConnection : IConnection
    {
        protected HttpClient HttpClient;

        public Uri Address
        {
            get { return HttpClient.BaseAddress; }
        }

        public BasicHttpClientConnection(Uri uri)
        {
            HttpClient = CreateHttpClient(uri);
        }

        public virtual void Dispose()
        {
            if (HttpClient == null)
                throw new ObjectDisposedException(typeof(BasicHttpClientConnection).Name);

            HttpClient.CancelPendingRequests();
            HttpClient.Dispose();
            HttpClient = null;
        }

        protected virtual HttpClient CreateHttpClient(Uri uri)
        {
            var client = new HttpClient { BaseAddress = new Uri(BuildCleanUrl(uri)) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(HttpContentTypes.Json));

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var parts = uri.UserInfo
                    .Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => Uri.UnescapeDataString(p))
                    .ToArray();

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", string.Join(":", parts).AsBase64Encoded());
            }

            return client;
        }

        protected virtual string BuildCleanUrl(Uri uri)
        {
            EnsureValidUri(uri);

            var url = string.Format("{0}://{1}{2}", uri.Scheme, uri.Authority, uri.LocalPath);
            while (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);

            return url;
        }

        protected virtual void EnsureValidUri(Uri uri)
        {
            Ensure.That(uri, "uri").IsNotNull();
            Ensure.That(uri.LocalPath, "uri.LocalPath")
                  .IsNotNullOrEmpty()
                  .WithExtraMessageOf(() => ExceptionStrings.BasicHttpClientConnection_UriIsMissingDb);
        }

        public virtual HttpResponseMessage Send(HttpRequestMessage request)
        {
            return SendAsync(request).Result;
        }

        public virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            return await HttpClient.SendAsync(request).ConfigureAwait(false);
        }
    }
}