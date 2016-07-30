using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Zoltu.BagsAmazonSynchronizer.Amazon
{
	public class AmazonUtilities
	{
		private readonly String _associateTag;
		private RequestSigner _requestSigner;

		public AmazonUtilities(String awsAccessKeyId, String awsSecretKey, String associateTag)
		{
			_associateTag = associateTag;
			_requestSigner = new RequestSigner(awsAccessKeyId, awsSecretKey, "ecs.amazonaws.com");
		}

		public String CreateAssociateLink(String asin)
		{
			return $"https://www.amazon.com/gp/product/{asin}/?tag={_associateTag}";
		}

		public async Task<String> GetProductDetailsXml(String asin)
		{
			var requestParameters = new Dictionary<String, String>
			{
				{ "IdType", "ASIN" },
				{ "Operation", "ItemLookup" },
				{ "ResponseGroup", "Images,Offers,ItemAttributes" },
				{ "Service", "AWSECommerceService" },
				{ "AssociateTag", _associateTag },
				{ "ItemId", asin }
			};

			var amazonRequestUri = _requestSigner.Sign(requestParameters);
			var httpClient = new HttpClient();
			return await httpClient.GetStringAsync(amazonRequestUri);
		}

		public String ConvertImageLinkToHttps(String source)
		{
			var uriBuilder = new UriBuilder(source);
			uriBuilder.Scheme = "https";
			uriBuilder.Host = "images-na.ssl-images-amazon.com";
			uriBuilder.Port = -1;
			return uriBuilder.ToString();
		}
	}
}
