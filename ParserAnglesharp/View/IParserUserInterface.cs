using ToysRuParser.Models;

namespace ToysRuParser.View
{
    public interface IParserUserInterface
	{
		void PrintCatalogLink(string link);

		void PrintPaginationNumber(string paginationLink);
		void PrintProduct(Product product);

		int ProductProgress{ get; }
		int ProductCount { get; }
		void PrintProgressBar();

	}
}
