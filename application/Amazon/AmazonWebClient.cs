using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using application.Models;

namespace application.Amazon
{
	public class AmazonWebClient
	{
		private readonly String _associateTag;
		private RequestSigner _requestSigner;

		public AmazonWebClient(String awsAccessKeyId, String awsSecretKey, String associateTag)
		{
			_associateTag = associateTag;
			_requestSigner = new RequestSigner(awsAccessKeyId, awsSecretKey, "ecs.amazonaws.com");
		}
        
		private async Task<String> GetProductDetailsXml(String asin)
		{
			var requestParameters = new Dictionary<String, String>
			{
				{ "IdType", "ASIN" },
				{ "Operation", "ItemLookup" },
				{ "ResponseGroup", "Offers,ItemAttributes" },//"Images,Offers,ItemAttributes"
				{ "Service", "AWSECommerceService" },
				{ "AssociateTag", _associateTag },
				{ "ItemId", asin }
			};

			var amazonRequestUri = _requestSigner.Sign(requestParameters);
			var httpClient = new HttpClient();
		    httpClient.Timeout = TimeSpan.FromSeconds(5);
            return await httpClient.GetStringAsync(amazonRequestUri);
		}

        public async Task<ProductSummary> GetProductSummary(String asin)
        {
            var xmlString = await GetProductDetailsXml(asin);

            return xmlString.ToProductSummary();
        }

        public String ConvertImageLinkToHttps(String source)
		{
			var uriBuilder = new UriBuilder(source);
			uriBuilder.Scheme = "https";
			uriBuilder.Host = "images-na.ssl-images-amazon.com";
			uriBuilder.Port = -1;
			return uriBuilder.ToString();
		}

        public String CreateAssociateLink(String asin)
        {
            return $"https://www.amazon.com/gp/product/{asin}/?tag={_associateTag}";
        }
    }
}
