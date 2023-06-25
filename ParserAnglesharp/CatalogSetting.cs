namespace ToysRuParser
{
	public class CatalogSetting
	{
        public CatalogSetting(string link)
        {
			Link = link;
        }

		public readonly string Link;

		private const string _productCount = "count";
		private const int _productCountValue = 45;

		private const string _pageIndex = "PAGEN_5";

		public string GetParametrizeUrl(int pageIndex) { 

			return $"{Link}/?{_productCount}={_productCountValue}&{_pageIndex}={pageIndex}";

		}
	}
}
