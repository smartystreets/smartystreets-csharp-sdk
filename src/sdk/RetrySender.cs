﻿namespace SmartyStreets
{
    using System.Threading.Tasks;
    using System;

	public class RetrySender : ISender
	{
		private readonly int maxRetries;
		private readonly ISender inner;

		public RetrySender(int maxRetries, ISender inner)
		{
			this.maxRetries = maxRetries;
			this.inner = inner;
		}

		public Response Send(Request request)
		{
			for (var i = 0; i <= this.maxRetries; i++)
			{
				var response = this.TrySend(request, i);
				if (response != null)
					return response;
			}

			return null;
		}

	    public async Task<Response> SendAsync(Request request)
        {
            for (var i = 0; i <= this.maxRetries; i++)
            {
                var response = await this.TrySendAsync(request, i).ConfigureAwait(false);
                if (response != null)
                    return response;
            }

            return null;
        }

	    private Response TrySend(Request request, int attempt)
		{
			try
			{
				return this.inner.Send(request);
			}
			catch (Exception) // TODO: catch HTTP 400, 413, 422 and just throw.
			{
				if (attempt >= this.maxRetries)
					throw;
			}

			return null;
        }

        private async Task<Response> TrySendAsync(Request request, int attempt)
        {
            try
            {
                return await this.inner.SendAsync(request);
            }
            catch (Exception) // TODO: catch HTTP 400, 413, 422 and just throw.
            {
                if (attempt >= this.maxRetries)
                    throw;
            }

            return null;
        }
    }
}