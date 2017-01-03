﻿namespace SmartyStreets
{
	using System;
	using System.IO;
	using System.Net;
    using System.Threading.Tasks;

    public class StandardLibrarySender : ISender
	{
		private static readonly Version AssemblyVersion = typeof(StandardLibrarySender).Assembly.GetName().Version;
		private static readonly string UserAgent = string.Format("smartystreets (sdk:csharp@{0}.{1}.{2})", AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);
		private TimeSpan timeout;

		public StandardLibrarySender()
		{
			this.timeout = TimeSpan.FromSeconds(10);
		}
		public StandardLibrarySender(TimeSpan timeout)
		{
			this.timeout = timeout;
		}

		public Response Send(Request request)
		{
			var frameworkRequest = this.BuildRequest(request);
			this.CopyHeaders(request, frameworkRequest);

			TryWritePayload(request, frameworkRequest);

			var frameworkResponse = GetResponse(frameworkRequest);
			var statusCode = (int)frameworkResponse.StatusCode;
			var payload = GetResponseBody(frameworkResponse);

			return new Response(statusCode, payload);
		}

	    public async Task<Response> SendAsync(Request request)
        {
            var frameworkRequest = this.BuildRequest(request);
            this.CopyHeaders(request, frameworkRequest);

            await TryWritePayloadAsync(request, frameworkRequest).ConfigureAwait(false);

            var frameworkResponse = await GetResponseAsync(frameworkRequest).ConfigureAwait(false);
            var statusCode = (int)frameworkResponse.StatusCode;
            var payload = GetResponseBody(frameworkResponse);

            return new Response(statusCode, payload);
        }

	    private HttpWebRequest BuildRequest(Request request)
		{
			var frameworkRequest = (HttpWebRequest)WebRequest.Create(request.GetUrl());
			frameworkRequest.Timeout = (int)this.timeout.TotalMilliseconds;
			frameworkRequest.Method = request.Method;
			frameworkRequest.Proxy = null;
			return frameworkRequest;
		}
		void CopyHeaders(Request request, HttpWebRequest frameworkRequest)
		{
			foreach (var item in request.Headers)
			{
				if (item.Key == "Referer")
					frameworkRequest.Referer = item.Value;
				else
					frameworkRequest.Headers.Add(item.Key, item.Value);
			}

			frameworkRequest.UserAgent = UserAgent;
		}
		private static void TryWritePayload(Request request, HttpWebRequest frameworkRequest)
		{
			if (request.Method == "POST" && request.Payload != null)
				using (var sourceStream = new MemoryStream(request.Payload))
					CopyStream(sourceStream, GetRequestStream(frameworkRequest));
        }
        private static async Task TryWritePayloadAsync(Request request, HttpWebRequest frameworkRequest)
        {
            if (request.Method == "POST" && request.Payload != null)
                using (var sourceStream = new MemoryStream(request.Payload))
                    CopyStream(sourceStream, await GetRequestStreamAsync(frameworkRequest).ConfigureAwait(false));
        }
        private static void CopyStream(Stream source, Stream target)
		{
			try
			{
				source.CopyTo(target);
			}
			catch (IOException)
			{
				throw new SmartyException();
			}
		}
		private static Stream GetRequestStream(WebRequest request)
		{
			try
			{
				return request.GetRequestStream();
			}
			catch (WebException)
			{
				throw new SmartyException();
			}
		}
		private static HttpWebResponse GetResponse(WebRequest request)
		{
			try
			{
				return (HttpWebResponse)request.GetResponse();
			}
			catch (WebException e)
			{
				if (e.Response == null)
					throw;

				return (HttpWebResponse)e.Response;
			}
        }
        private static async Task<HttpWebResponse> GetResponseAsync(WebRequest request)
        {
            try
            {
                return
                    (HttpWebResponse)
                    await
                        Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null)
                            .ConfigureAwait(false);
            }
            catch (WebException e)
            {
                if (e.Response == null)
                    throw;

                return (HttpWebResponse) e.Response;
            }
        }
        private static byte[] GetResponseBody(WebResponse response)
		{
			var length = response.ContentLength >= 0 ? (int)response.ContentLength : 0;

			using (var targetStream = new MemoryStream(length))
			using (var responseStream = response.GetResponseStream())
			{
				CopyStream(responseStream, targetStream);
				return targetStream.ToArray();
			}
        }
        private static Task<Stream> GetRequestStreamAsync(WebRequest request)
        {
            return Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, null);
        }
    }
}