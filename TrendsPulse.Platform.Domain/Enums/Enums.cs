namespace TrendsPulse.Platform.Domain.Enums;

public enum CategoryType    { Commodities=1, Metals=2, Energy=3, Crypto=4, Travel=5, Forex=6, Stocks=7, Custom=8 }
public enum ItemStatus      { Active=1, Paused=2, Deprecated=3 }
public enum ItemVisibility  { Global=1, Tenant=2 }
public enum DataSourceType  { AlphaVantage=1, CoinGecko=2, Amadeus=3, NewsApi=4, CustomHttp=5, Manual=6 }
public enum FetchFrequency  { EveryFifteenMinutes=1, EveryThirtyMinutes=2, Hourly=3, Every4Hours=4, Daily=5, Weekly=6 }
public enum PriceUnit       { UsdPerOunce=1, UsdPerBarrel=2, UsdPerTon=3, Usd=4, Eur=5, Gbp=6, Aed=7, Percentage=8, Index=9, Custom=10 }
