using AngleSharp;
using AngleSharp.Dom;
using ParserAnglesharp;

ToysCatalogParser toyParser = new ToysCatalogParser();

await toyParser.Parse();