namespace ToysRuParser
{
	public class CatalogSetting
	{
        public CatalogSetting(string link)
        {
			Link = link;
			PaginateCountLink = Flurl.Url.Combine(Link + $"/?{_productCount}={_productCountValue}");
		}

		public readonly string Link;

		public readonly string PaginateCountLink;

		private const string _productCount = "count";
		private const int _productCountValue = 45;

		private const string _pageIndex = "PAGEN_5";

		public string GetParametrizeUrl(int pageIndex) {

			return Flurl.Url.Combine(Link, $"/?{_productCount}={_productCountValue}&{_pageIndex}={pageIndex}");
		}
	}
}
