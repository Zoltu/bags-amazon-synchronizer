using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using application.Models;

namespace application.Amazon
{
    public enum OperationType
    {
        ItemSearch,
        ItemLookup
    }
	public class AmazonUtilities
	{
		private readonly String _associateTag;
		private RequestSigner _requestSigner;

		public AmazonUtilities(String awsAccessKeyId, String awsSecretKey, String associateTag)
		{
			_associateTag = associateTag;
			_requestSigner = new RequestSigner(awsAccessKeyId, awsSecretKey, "ecs.amazonaws.com");
		}
        
		private async Task<String> GetProductDetailsXml(String asins, OperationType type = OperationType.ItemLookup)
		{
			var requestParameters = new Dictionary<String, String>
			{
				{ "IdType", "ASIN" },
				{ "ResponseGroup", "Large" },
				{ "Service", "AWSECommerceService" },
				{ "AssociateTag", _associateTag }
			};

		    if (type == OperationType.ItemLookup)
		    {
                requestParameters.Add("Operation", "ItemLookup");
                requestParameters.Add("ItemId", asins);
            }
            else if (type == OperationType.ItemSearch)
            {
                requestParameters.Add("Operation", "ItemSearch");
                requestParameters.Add("SearchIndex", "All");
                requestParameters.Add("Keywords", asins);
            }

			var amazonRequestUri = _requestSigner.Sign(requestParameters);
		    var httpClient = new HttpClient
		                        {
		                            Timeout = TimeSpan.FromSeconds(5)
		                        };

		    return await httpClient.GetStringAsync(amazonRequestUri);
		}
        
        public async Task<List<ProductSummary>> GetProductSummary(String asins)
        {
            var xmlString = await GetProductDetailsXml(asins);

            return xmlString.ToProductSummaryList();
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
