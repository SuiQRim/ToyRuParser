using ToysRuParser;

CatalogSetting cs = new("https://www.toy.ru/catalog/boy_transport");
ToysCatalogParser toyParser = new (cs);


await toyParser.Parse();