#property copyright "trevone"
#property link "https://login.mql5.com/en/users/trevone"   
 
#property strict 
#property version "1.41"  
string ver = "1.41";
extern int MAGIC = 1480; 
extern string TradeComment = "Currency Strength Price Action";
extern string SymbolSuffix = "f";  
extern string CURRENCY_SIGNAL = ".............................................................................................................";    
extern bool SwingSignal = true; 
extern bool ScalpingSignal = true;  
extern double ScalpingValue = 4.0;
extern double SwingValue = 5.0;
extern string NEWS_SIGNAL_IMPACT = ".............................................................................................................";    
extern bool EnableNews = true;   
bool TradeLowImpact = true;
bool TradeMediumImpact = true;
bool TradeHighImpact = false; 
extern string TIME_SETTINGS = ".............................................................................................................";   
extern int BeforeNewsSeconds = 1800;
extern int AfterNewsSeconds = 1800; 
extern int TradeTimeSeconds = 3600; 
extern int ProfitTimeSeconds = 1800;   
extern int LongTimeSeconds = 28800;  
extern string TRADE_HOURS = ".............................................................................................................";   
extern int SignalSwingStartHour = 15; 
extern int SignalSwingEndHour = 9;  
extern int SignalScalpingStartHour = 22; 
extern int SignalScalpingEndHour = 2;  
extern string MARGIN_SETTINGS = ".............................................................................................................";  
extern double BaseLotPerDollar = 30;
extern double RecoverPower = 1.5;
extern double BasketProfit = 1.3; 
double BasketLoss = 0.95;
extern int StarterTrades = 1;
extern int MaxTrades = 5; 
extern int ExtraTrades = 9; 
extern string PROFIT_SETTINGS = ".............................................................................................................";      
extern double StartProfitATR = 9; 
extern double MinProfitATR  = 5;   
extern string STOP_SETTINGS = ".............................................................................................................";      
extern double MinStopATR = 5;    
extern double MaxStopATR = 15; 
extern string CURRENCY_PERIOD = ".............................................................................................................";    
extern int TimeFrame = 30;  
extern int TimeFrame_Long = 240;  
extern string CURRENCY_SHIFT = ".............................................................................................................";    
extern int Shift = 1; 
extern int Shift_Long = 0;
extern string TREND_SIGNAL_CURRENCIES = ".............................................................................................................";    
extern int WeakCurrencies = 3; 
extern int LongCurrencies = 3;
extern int ShortCurrencies = 4;     
extern string SCALPING_SIGNAL_CURRENCIES = ".............................................................................................................";    
extern int ScalpingWeakCurrencies = 2; 
extern int ScalpingLongCurrencies = 2;
extern int ScalpingShortCurrencies = 3;         
extern string INDICATOR_ATR = ".............................................................................................................";    
extern int ATRPeriod = 14;
extern int ATRShiftCheck = 5;
extern string INDICATOR_RSI = ".............................................................................................................";    
extern int RSIPeriod = 7;
extern int RSIShiftCheck = 3;  
extern string ENABLE_PAIRS = ".............................................................................................................";    
extern bool EnableAUDCAD = true;
extern bool EnableAUDCHF = true;
extern bool EnableAUDJPY = true; 
extern bool EnableAUDNZD = true; 
extern bool EnableAUDUSD = true; 
extern bool EnableCADCHF = true; 
extern bool EnableCADJPY = true; 
extern bool EnableCHFJPY = true; 
extern bool EnableEURAUD = true; 
extern bool EnableEURCAD = true; 
extern bool EnableEURCHF = true; 
extern bool EnableEURGBP = true; 
extern bool EnableEURJPY = true; 
extern bool EnableEURNZD = true; 
extern bool EnableEURUSD = true; 
extern bool EnableGBPAUD = true; 
extern bool EnableGBPCAD = true; 
extern bool EnableGBPCHF = true; 
extern bool EnableGBPJPY = true; 
extern bool EnableGBPNZD = true; 
extern bool EnableGBPUSD = true; 
extern bool EnableNZDCAD = true; 
extern bool EnableNZDCHF = true; 
extern bool EnableNZDJPY = true; 
extern bool EnableNZDUSD = true; 
extern bool EnableUSDCAD = true; 
extern bool EnableUSDCHF = true; 
extern bool EnableUSDJPY = true; 
extern string TRADE_PAIRS = ".............................................................................................................";    
extern bool TradeAUDCAD = true;
extern bool TradeAUDCHF = false;
extern bool TradeAUDJPY = true; 
extern bool TradeAUDNZD = true; 
extern bool TradeAUDUSD = true; 
extern bool TradeCADCHF = false; 
extern bool TradeCADJPY = true; 
extern bool TradeCHFJPY = false; 
extern bool TradeEURAUD = true; 
extern bool TradeEURCAD = true; 
extern bool TradeEURCHF = false; 
extern bool TradeEURGBP = true; 
extern bool TradeEURJPY = true; 
extern bool TradeEURNZD = true; 
extern bool TradeEURUSD = true; 
extern bool TradeGBPAUD = true; 
extern bool TradeGBPCAD = true; 
extern bool TradeGBPCHF = false; 
extern bool TradeGBPJPY = true; 
extern bool TradeGBPNZD = false; 
extern bool TradeGBPUSD = true; 
extern bool TradeNZDCAD = false; 
extern bool TradeNZDCHF = false; 
extern bool TradeNZDJPY = true; 
extern bool TradeNZDUSD = true; 
extern bool TradeUSDCAD = true; 
extern bool TradeUSDCHF = false; 
extern bool TradeUSDJPY = true; 
string BACKTESTING = ".............................................................................................................";    
int BacktestTradeBars = 5; 
bool NewsDiagnostics = true;
bool TrendingDiagnostics = true;
bool ScalpingDiagnostics = true;
int MinNewsDiagnostic = 3;
int MinTrendDiagnostic = 5; 
int MinScalpingDiagnostic = 4;
string tradeDiagnostics = "";
string scalpingDiagnostics = ""; 

int Slippage = 1;
int totalSecondsToNews = 0;

int tempTotalTrades = 0;

int initTime;
int CandlePeriod = 14; 
   
int ATRTimeFrame = 5;   
string display = "";  

int totalAccountLoosers;
 
double spread, pipPoints, slippage, MA1Cur, MA1Prev, avgCandle, totalCharges, avgCandleSize, recoverFactor,
buyLots, sellLots, marginRequirement, lotSize, digits, exponentGrowth, totalTrades, maxLots,  minLots,
accountBalance, totalProfit, totalLoss, accountEquity, currentSpread, currentAvgSpread,
zigZag0, zigZag1, zigZag2;
 
int r, maxTicks, prevHour, totalBuyTrades, totalSellTrades, totalBuyOrders, totalSellOrders, hoursToNews, minutesToNews,
totalAccountTrades, extraProfitTime, preventDuplicates, zigZag0Time, zigZag1Time, zigZag2Time;

string newsTime, newsCurrency, newsImpact, upCommingRecent; 

string signalComment = ""; 
string newsDiagnostics = "";

double DynamicSlippage = 1;
double BaseLotSize = 0.01;
double ExponentQuality = 1;
double highest = 0; 
double high0 = 0;
double low0 = 0;
double high1 = 0;
double low1 = 0;        
double openSpread = 0;
double accountProfit = 0;
double range = 0;
 
int avgOpenTime = 0;   

int ZZBars = 500;   
int LotPrecision = 2; 
int MMAShift = 0;
int MAShift = 0; 
int lastTradeTime = 0;  
int lastExponentTime = 0;
int totalTargets = 0;
int turn = 0;
int totalCycles = 0;   
int RSIShift = 1;
int ATRShift = 1; 

bool openBuyPosition = false;
bool openSellPosition = false;  

bool extraModify = false;
bool drawdownModify = true;   
 
double spreadSize[9999] = {};   

double currencyStrength_USD = 0;
double currencyStrength_EUR = 0;
double currencyStrength_GBP = 0;
double currencyStrength_CHF = 0;
double currencyStrength_CAD = 0;
double currencyStrength_AUD = 0;
double currencyStrength_NZD = 0;
double currencyStrength_JPY = 0;

double currencyStrength_USD_Long = 0;
double currencyStrength_EUR_Long = 0;
double currencyStrength_GBP_Long = 0;
double currencyStrength_CHF_Long = 0;
double currencyStrength_CAD_Long = 0;
double currencyStrength_AUD_Long = 0;
double currencyStrength_NZD_Long = 0;
double currencyStrength_JPY_Long  = 0;  

double currencyStrength_USD_History[7] = { 0, 0, 0, 0, 0, 0, 0 };  
double currencyStrength_EUR_History[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_GBP_History[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_CHF_History[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_CAD_History[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_AUD_History[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_NZD_History[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_JPY_History[7] = { 0, 0, 0, 0, 0, 0, 0 };  

double currencyStrength_USD_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 };  
double currencyStrength_EUR_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_GBP_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_CHF_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_CAD_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_AUD_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_NZD_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 }; 
double currencyStrength_JPY_History_Long[7] = { 0, 0, 0, 0, 0, 0, 0 };  

double pipRange_AUDCAD = 0;
double pipRange_AUDCHF = 0;
double pipRange_AUDJPY = 0;
double pipRange_AUDNZD = 0;
double pipRange_AUDUSD = 0;
double pipRange_CADCHF = 0;
double pipRange_CADJPY = 0;
double pipRange_CHFJPY = 0;
double pipRange_EURAUD = 0;
double pipRange_EURCAD = 0;
double pipRange_EURCHF = 0; 
double pipRange_EURGBP = 0;
double pipRange_EURJPY = 0;
double pipRange_EURNZD = 0;
double pipRange_EURUSD = 0;
double pipRange_GBPAUD = 0;
double pipRange_GBPCAD = 0;
double pipRange_GBPCHF = 0;
double pipRange_GBPJPY = 0;
double pipRange_GBPNZD = 0;
double pipRange_GBPUSD = 0;
double pipRange_NZDCAD = 0;
double pipRange_NZDCHF = 0;
double pipRange_NZDJPY = 0;
double pipRange_NZDUSD = 0;
double pipRange_USDCAD = 0;
double pipRange_USDCHF = 0;
double pipRange_USDJPY = 0; 

double pipRange_AUDCAD_Long = 0;
double pipRange_AUDCHF_Long = 0;
double pipRange_AUDJPY_Long = 0;
double pipRange_AUDNZD_Long = 0;
double pipRange_AUDUSD_Long = 0;
double pipRange_CADCHF_Long = 0;
double pipRange_CADJPY_Long = 0;
double pipRange_CHFJPY_Long = 0;
double pipRange_EURAUD_Long = 0;
double pipRange_EURCAD_Long = 0;
double pipRange_EURCHF_Long = 0; 
double pipRange_EURGBP_Long = 0;
double pipRange_EURJPY_Long = 0;
double pipRange_EURNZD_Long = 0;
double pipRange_EURUSD_Long = 0;
double pipRange_GBPAUD_Long = 0;
double pipRange_GBPCAD_Long = 0;
double pipRange_GBPCHF_Long = 0;
double pipRange_GBPJPY_Long = 0;
double pipRange_GBPNZD_Long = 0;
double pipRange_GBPUSD_Long = 0;
double pipRange_NZDCAD_Long = 0;
double pipRange_NZDCHF_Long = 0;
double pipRange_NZDJPY_Long = 0;
double pipRange_NZDUSD_Long = 0;
double pipRange_USDCAD_Long = 0;
double pipRange_USDCHF_Long = 0;
double pipRange_USDJPY_Long = 0;  

double bidRatio_AUDCAD = 0;
double bidRatio_AUDCHF = 0;
double bidRatio_AUDJPY = 0;
double bidRatio_AUDNZD = 0;
double bidRatio_AUDUSD = 0;
double bidRatio_CADCHF = 0;
double bidRatio_CADJPY = 0;
double bidRatio_CHFJPY = 0;
double bidRatio_EURAUD = 0;
double bidRatio_EURCAD = 0;
double bidRatio_EURCHF = 0; 
double bidRatio_EURGBP = 0;
double bidRatio_EURJPY = 0;
double bidRatio_EURNZD = 0;
double bidRatio_EURUSD = 0;
double bidRatio_GBPAUD = 0;
double bidRatio_GBPCAD = 0;
double bidRatio_GBPCHF = 0;
double bidRatio_GBPJPY = 0;
double bidRatio_GBPNZD = 0;
double bidRatio_GBPUSD = 0;
double bidRatio_NZDCAD = 0;
double bidRatio_NZDCHF = 0;
double bidRatio_NZDJPY = 0;
double bidRatio_NZDUSD = 0;
double bidRatio_USDCAD = 0;
double bidRatio_USDCHF = 0;
double bidRatio_USDJPY = 0;

double bidRatio_AUDCAD_Long = 0;
double bidRatio_AUDCHF_Long = 0;
double bidRatio_AUDJPY_Long = 0;
double bidRatio_AUDNZD_Long = 0;
double bidRatio_AUDUSD_Long = 0;
double bidRatio_CADCHF_Long = 0;
double bidRatio_CADJPY_Long = 0;
double bidRatio_CHFJPY_Long = 0;
double bidRatio_EURAUD_Long = 0;
double bidRatio_EURCAD_Long = 0;
double bidRatio_EURCHF_Long = 0; 
double bidRatio_EURGBP_Long = 0;
double bidRatio_EURJPY_Long = 0;
double bidRatio_EURNZD_Long = 0;
double bidRatio_EURUSD_Long = 0;
double bidRatio_GBPAUD_Long = 0;
double bidRatio_GBPCAD_Long = 0;
double bidRatio_GBPCHF_Long = 0;
double bidRatio_GBPJPY_Long = 0;
double bidRatio_GBPNZD_Long = 0;
double bidRatio_GBPUSD_Long = 0;
double bidRatio_NZDCAD_Long = 0;
double bidRatio_NZDCHF_Long = 0;
double bidRatio_NZDJPY_Long = 0;
double bidRatio_NZDUSD_Long = 0;
double bidRatio_USDCAD_Long = 0;
double bidRatio_USDCHF_Long = 0;
double bidRatio_USDJPY_Long = 0; 

double relative_AUDCAD_AUD = 0;
double relative_AUDCAD_CAD = 0;

double relative_AUDCHF_AUD = 0;
double relative_AUDCHF_CHF = 0;

double relative_AUDJPY_AUD = 0;
double relative_AUDJPY_JPY = 0; 

double relative_AUDNZD_AUD = 0;
double relative_AUDNZD_NZD = 0;

double relative_AUDUSD_AUD = 0;
double relative_AUDUSD_USD = 0;

double relative_CADCHF_CAD = 0;
double relative_CADCHF_CHF = 0; 

double relative_CADJPY_CAD = 0;
double relative_CADJPY_JPY = 0;

double relative_CHFJPY_CHF = 0;
double relative_CHFJPY_JPY = 0;

double relative_EURAUD_EUR = 0;
double relative_EURAUD_AUD = 0;

double relative_EURCAD_EUR = 0;
double relative_EURCAD_CAD = 0;

double relative_EURCHF_EUR = 0; 
double relative_EURCHF_CHF = 0; 

double relative_EURGBP_EUR = 0;
double relative_EURGBP_GBP = 0;

double relative_EURJPY_EUR = 0;
double relative_EURJPY_JPY = 0;

double relative_EURNZD_EUR = 0;
double relative_EURNZD_NZD = 0;

double relative_EURUSD_EUR = 0;
double relative_EURUSD_USD = 0;

double relative_GBPAUD_GBP = 0;
double relative_GBPAUD_AUD = 0;

double relative_GBPCAD_GBP = 0;
double relative_GBPCAD_CAD = 0;

double relative_GBPCHF_GBP = 0;
double relative_GBPCHF_CHF = 0;

double relative_GBPJPY_GBP = 0;
double relative_GBPJPY_JPY = 0;

double relative_GBPNZD_GBP = 0;
double relative_GBPNZD_NZD = 0;

double relative_GBPUSD_GBP = 0;
double relative_GBPUSD_USD = 0;

double relative_NZDCAD_NZD = 0;
double relative_NZDCAD_CAD = 0;

double relative_NZDCHF_NZD = 0;
double relative_NZDCHF_CHF = 0;

double relative_NZDJPY_NZD = 0;
double relative_NZDJPY_JPY = 0;

double relative_NZDUSD_NZD = 0;
double relative_NZDUSD_USD = 0;

double relative_USDCAD_USD = 0;
double relative_USDCAD_CAD = 0;

double relative_USDCHF_USD = 0;
double relative_USDCHF_CHF = 0; 

double relative_USDJPY_USD = 0;
double relative_USDJPY_JPY = 0; 

double relative_AUDCAD_AUD_Long = 0;
double relative_AUDCAD_CAD_Long = 0;

double relative_AUDCHF_AUD_Long = 0;
double relative_AUDCHF_CHF_Long = 0;

double relative_AUDJPY_AUD_Long = 0;
double relative_AUDJPY_JPY_Long = 0; 

double relative_AUDNZD_AUD_Long = 0;
double relative_AUDNZD_NZD_Long = 0;

double relative_AUDUSD_AUD_Long = 0;
double relative_AUDUSD_USD_Long = 0;

double relative_CADCHF_CAD_Long = 0;
double relative_CADCHF_CHF_Long = 0; 

double relative_CADJPY_CAD_Long = 0;
double relative_CADJPY_JPY_Long = 0;

double relative_CHFJPY_CHF_Long = 0;
double relative_CHFJPY_JPY_Long = 0;

double relative_EURAUD_EUR_Long = 0;
double relative_EURAUD_AUD_Long = 0;

double relative_EURCAD_EUR_Long = 0;
double relative_EURCAD_CAD_Long = 0;

double relative_EURCHF_EUR_Long = 0; 
double relative_EURCHF_CHF_Long = 0; 

double relative_EURGBP_EUR_Long = 0;
double relative_EURGBP_GBP_Long = 0;

double relative_EURJPY_EUR_Long = 0;
double relative_EURJPY_JPY_Long = 0;

double relative_EURNZD_EUR_Long = 0;
double relative_EURNZD_NZD_Long = 0;

double relative_EURUSD_EUR_Long = 0;
double relative_EURUSD_USD_Long = 0;

double relative_GBPAUD_GBP_Long = 0;
double relative_GBPAUD_AUD_Long = 0;

double relative_GBPCAD_GBP_Long = 0;
double relative_GBPCAD_CAD_Long = 0;

double relative_GBPCHF_GBP_Long = 0;
double relative_GBPCHF_CHF_Long = 0;

double relative_GBPJPY_GBP_Long = 0;
double relative_GBPJPY_JPY_Long = 0;

double relative_GBPNZD_GBP_Long = 0;
double relative_GBPNZD_NZD_Long = 0;

double relative_GBPUSD_GBP_Long = 0;
double relative_GBPUSD_USD_Long = 0;

double relative_NZDCAD_NZD_Long = 0;
double relative_NZDCAD_CAD_Long = 0;

double relative_NZDCHF_NZD_Long = 0;
double relative_NZDCHF_CHF_Long = 0;

double relative_NZDJPY_NZD_Long = 0;
double relative_NZDJPY_JPY_Long = 0;

double relative_NZDUSD_NZD_Long = 0;
double relative_NZDUSD_USD_Long = 0;

double relative_USDCAD_USD_Long = 0;
double relative_USDCAD_CAD_Long = 0;

double relative_USDCHF_USD_Long = 0;
double relative_USDCHF_CHF_Long = 0; 

double relative_USDJPY_USD_Long = 0;
double relative_USDJPY_JPY_Long = 0; 
 
double AUDCAD_high = 0.00000; 
double AUDCAD_bid = 0.00000;
double AUDCAD_low = 0.00000; 

double AUDCAD_high_Long = 0.00000; 
double AUDCAD_bid_Long = 0.00000;
double AUDCAD_low_Long = 0.00000; 

double AUDCHF_high = 0.00000; 
double AUDCHF_bid = 0.00000;
double AUDCHF_low = 0.00000; 

double AUDCHF_high_Long = 0.00000; 
double AUDCHF_bid_Long = 0.00000;
double AUDCHF_low_Long = 0.00000;  
   
double AUDJPY_high = 0.00000; 
double AUDJPY_bid = 0.00000;
double AUDJPY_low = 0.00000;

double AUDJPY_high_Long = 0.00000; 
double AUDJPY_bid_Long = 0.00000;
double AUDJPY_low_Long = 0.00000; 
     
double AUDNZD_high = 0.00000; 
double AUDNZD_bid = 0.00000;
double AUDNZD_low = 0.00000;

double AUDNZD_high_Long = 0.00000; 
double AUDNZD_bid_Long = 0.00000;
double AUDNZD_low_Long = 0.00000; 
    
double AUDUSD_high = 0.00000; 
double AUDUSD_bid = 0.00000;
double AUDUSD_low = 0.00000; 

double AUDUSD_high_Long = 0.00000; 
double AUDUSD_bid_Long = 0.00000;
double AUDUSD_low_Long = 0.00000; 
   
double CADCHF_high = 0.00000; 
double CADCHF_bid = 0.00000;
double CADCHF_low = 0.00000;

double CADCHF_high_Long = 0.00000; 
double CADCHF_bid_Long = 0.00000;
double CADCHF_low_Long = 0.00000; 
   
double CADJPY_high = 0.00000; 
double CADJPY_bid = 0.00000;
double CADJPY_low = 0.00000;

double CADJPY_high_Long = 0.00000; 
double CADJPY_bid_Long = 0.00000;
double CADJPY_low_Long = 0.00000; 
   
double CHFJPY_high = 0.00000; 
double CHFJPY_bid = 0.00000;
double CHFJPY_low = 0.00000;

double CHFJPY_high_Long = 0.00000; 
double CHFJPY_bid_Long = 0.00000;
double CHFJPY_low_Long = 0.00000;  

double EURAUD_high = 0.00000; 
double EURAUD_bid = 0.00000;
double EURAUD_low = 0.00000;

double EURAUD_high_Long = 0.00000; 
double EURAUD_bid_Long = 0.00000;
double EURAUD_low_Long = 0.00000; 
   
double EURCAD_high = 0.00000; 
double EURCAD_bid = 0.00000;
double EURCAD_low = 0.00000;

double EURCAD_high_Long = 0.00000; 
double EURCAD_bid_Long = 0.00000;
double EURCAD_low_Long = 0.00000; 
   
double EURCHF_high = 0.00000; 
double EURCHF_bid = 0.00000;
double EURCHF_low = 0.00000;

double EURCHF_high_Long = 0.00000; 
double EURCHF_bid_Long = 0.00000;
double EURCHF_low_Long = 0.00000; 
   
double EURGBP_high = 0.00000; 
double EURGBP_bid = 0.00000;
double EURGBP_low = 0.00000;

double EURGBP_high_Long = 0.00000; 
double EURGBP_bid_Long = 0.00000;
double EURGBP_low_Long = 0.00000;  

double EURJPY_high = 0.00000; 
double EURJPY_bid = 0.00000;
double EURJPY_low = 0.00000;

double EURJPY_high_Long = 0.00000; 
double EURJPY_bid_Long = 0.00000;
double EURJPY_low_Long = 0.00000; 
   
double EURNZD_high = 0.00000; 
double EURNZD_bid = 0.00000;
double EURNZD_low = 0.00000;

double EURNZD_high_Long = 0.00000; 
double EURNZD_bid_Long = 0.00000;
double EURNZD_low_Long = 0.00000; 
   
double EURUSD_high = 0.00000; 
double EURUSD_bid = 0.00000;
double EURUSD_low = 0.00000;

double EURUSD_high_Long = 0.00000; 
double EURUSD_bid_Long = 0.00000;
double EURUSD_low_Long = 0.00000; 
   
double GBPAUD_high = 0.00000; 
double GBPAUD_bid = 0.00000;
double GBPAUD_low = 0.00000;

double GBPAUD_high_Long = 0.00000; 
double GBPAUD_bid_Long = 0.00000;
double GBPAUD_low_Long = 0.00000; 
   
double GBPCAD_high = 0.00000; 
double GBPCAD_bid = 0.00000;
double GBPCAD_low = 0.00000;

double GBPCAD_high_Long = 0.00000; 
double GBPCAD_bid_Long = 0.00000;
double GBPCAD_low_Long = 0.00000; 
   
double GBPCHF_high = 0.00000;
double GBPCHF_bid = 0.00000;
double GBPCHF_low = 0.00000;

double GBPCHF_high_Long = 0.00000; 
double GBPCHF_bid_Long = 0.00000;
double GBPCHF_low_Long = 0.00000; 
   
double GBPJPY_high = 0.000; 
double GBPJPY_bid = 0.000;
double GBPJPY_low = 0.000;

double GBPJPY_high_Long = 0.000; 
double GBPJPY_bid_Long = 0.000;
double GBPJPY_low_Long = 0.000; 
   
double GBPNZD_high = 0.00000; 
double GBPNZD_bid = 0.00000;
double GBPNZD_low = 0.00000;

double GBPNZD_high_Long = 0.00000; 
double GBPNZD_bid_Long = 0.00000;
double GBPNZD_low_Long = 0.00000; 
 
double GBPUSD_high = 0.00000; 
double GBPUSD_bid = 0.00000;
double GBPUSD_low = 0.00000;

double GBPUSD_high_Long = 0.00000; 
double GBPUSD_bid_Long = 0.00000;
double GBPUSD_low_Long = 0.00000; 
   
double NZDCAD_high = 0.00000; 
double NZDCAD_bid = 0.00000;
double NZDCAD_low = 0.00000;

double NZDCAD_high_Long = 0.00000; 
double NZDCAD_bid_Long = 0.00000;
double NZDCAD_low_Long = 0.00000; 
   
double NZDCHF_high = 0.00000; 
double NZDCHF_bid = 0.00000;
double NZDCHF_low = 0.00000;

double NZDCHF_high_Long = 0.00000; 
double NZDCHF_bid_Long = 0.00000;
double NZDCHF_low_Long = 0.00000; 
   
double NZDJPY_high = 0.000;
double NZDJPY_bid = 0.000;
double NZDJPY_low = 0.000;

double NZDJPY_high_Long = 0.000; 
double NZDJPY_bid_Long = 0.000;
double NZDJPY_low_Long = 0.000;  
   
double NZDUSD_high = 0.00000; 
double NZDUSD_bid = 0.00000;
double NZDUSD_low = 0.00000;

double NZDUSD_high_Long = 0.00000; 
double NZDUSD_bid_Long = 0.00000;
double NZDUSD_low_Long = 0.00000; 
   
double USDCAD_high = 0.00000; 
double USDCAD_bid = 0.00000;
double USDCAD_low = 0.00000;

double USDCAD_high_Long = 0.00000; 
double USDCAD_bid_Long = 0.00000;
double USDCAD_low_Long = 0.00000; 
   
double USDCHF_high = 0.00000; 
double USDCHF_bid = 0.00000;
double USDCHF_low = 0.00000; 

double USDCHF_high_Long = 0.00000; 
double USDCHF_bid_Long = 0.00000;
double USDCHF_low_Long = 0.00000;  
   
double USDJPY_high = 0.000; 
double USDJPY_bid = 0.000;
double USDJPY_low = 0.000;  

double USDJPY_high_Long = 0.000; 
double USDJPY_bid_Long = 0.000;
double USDJPY_low_Long = 0.000;   

string symDefault[28] = { 
   // standard forex
   "AUDCAD", // 0
   "AUDCHF", // 1
   "AUDJPY", // 2
   "AUDNZD", // 3
   "AUDUSD", // 4
   "CADCHF", // 5
   "CADJPY", // 6
   "CHFJPY", // 7
   "EURAUD", // 8
   "EURCAD", // 9
   "EURCHF", // 10
   "EURGBP", // 11
   "EURJPY", // 12
   "EURNZD", // 13
   "EURUSD", // 14
   "GBPAUD", // 15
   "GBPCAD", // 16
   "GBPCHF", // 17
   "GBPJPY", // 18
   "GBPNZD", // 19
   "GBPUSD", // 20
   "NZDCAD", // 21
   "NZDCHF", // 22
   "NZDJPY", // 23
   "NZDUSD", // 24
   "USDCAD", // 25
   "USDCHF", // 26
   "USDJPY" // 27 total = 28
};

string sym[28] = { 
   // standard forex
   "AUDCAD", // 0
   "AUDCHF", // 1
   "AUDJPY", // 2
   "AUDNZD", // 3
   "AUDUSD", // 4
   "CADCHF", // 5
   "CADJPY", // 6
   "CHFJPY", // 7
   "EURAUD", // 8
   "EURCAD", // 9
   "EURCHF", // 10
   "EURGBP", // 11
   "EURJPY", // 12
   "EURNZD", // 13
   "EURUSD", // 14
   "GBPAUD", // 15
   "GBPCAD", // 16
   "GBPCHF", // 17
   "GBPJPY", // 18
   "GBPNZD", // 19
   "GBPUSD", // 20
   "NZDCAD", // 21
   "NZDCHF", // 22
   "NZDJPY", // 23
   "NZDUSD", // 24
   "USDCAD", // 25
   "USDCHF", // 26
   "USDJPY" // 27 total = 28
};

double pipPoint[28] = { 
   // standard forex
   0.0001, // 0 AUDCAD
   0.0001, // 1 AUDCHF
   0.01, // 2 AUDJPY
   0.0001, // 3 AUDNZD
   0.0001, // 4 AUDUSD
   0.0001, // 5 CADCHF
   0.01, // 6 CADJPY
   0.01, // 7 CHFJPY
   0.0001, // 8 EURAUD
   0.0001, // 9 EURCAD
   0.0001, // 10 EURCHF
   0.0001, // 11 EURGBP
   0.01, // 12 EURJPY
   0.0001, // 13 EURNZD
   0.0001, // 14 EURUSD
   0.0001, // 15 GBPAUD
   0.0001, // 16 GBPCAD
   0.0001, // 17 GBPCHF
   0.01, // 18 GBPJPY
   0.0001, // 19 GBPNZD
   0.0001, // 20 GBPUSD
   0.0001, // 21 NZDCAD
   0.0001, // 22 NZDCHF
   0.01, // 23 NZDJPY
   0.0001, // 24 NZDUSD
   0.0001, // 25 USDCAD
   0.0001, // 26 USDCHF
   0.01 // 27 USDJPY total = 28
};  

int digitsArray[28] = { 
   // standard forex
   5, // 0 AUDCAD
   5, // 1 AUDCHF
   3, // 2 AUDJPY
   5, // 3 AUDNZD
   5, // 4 AUDUSD
   5, // 5 CADCHF
   3, // 6 CADJPY
   3, // 7 CHFJPY
   5, // 8 EURAUD
   5, // 9 EURCAD
   5, // 10 EURCHF
   5, // 11 EURGBP
   3, // 12 EURJPY
   5, // 13 EURNZD
   5, // 14 EURUSD
   5, // 15 GBPAUD
   5, // 16 GBPCAD
   5, // 17 GBPCHF
   3, // 18 GBPJPY
   5, // 19 GBPNZD
   5, // 20 GBPUSD
   5, // 21 NZDCAD
   5, // 22 NZDCHF
   3, // 23 NZDJPY
   5, // 24 NZDUSD
   5, // 25 USDCAD
   5, // 26 USDCHF
   3 // 27 USDJPY total = 28
};

int generalArrayCount = 28;
    
double high2Array[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };      
double low2Array[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

double buyLotsArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
double sellLotsArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

double trailingProfitArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

double totalProfitArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };  
double totalChargesArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
double totalLossArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

double accountProfitArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
 
double rsiCurrent[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; 
double rsiPrevious[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
double atrCurrent[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
double atrPrevious[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
 
int tradeDiagnosticsFilterArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
int scalpingDiagnosticsFilterArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
int newsDiagnosticsFilterArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
  
int totalTradesArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; 
int totalBuyTradesArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; 
int totalSellTradesArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };  
int totalUprotectedArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; 
int lastTradeTimeArray[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };                           


double symbolHigh[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };                           
double symbolLow[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };                           
double symbolBid[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };                           

double symbolHigh_Long[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };                           
double symbolLow_Long[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };                           
double symbolBid_Long[28] = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };                           
 
bool closeAllSymArray[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
bool openBuyPositionArray[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
bool openSellPositionArray[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };   
                         
bool openBuyPositionScalpingArray[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }; 
bool openSellPositionScalpingArray[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }; 
      
bool openBuyPositionNewsArray[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };       
bool openSellPositionNewsArray[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false }; 
                        
bool profitable[28] = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };                     
               
string signalCommentArray[28] = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };                         
string tradeDiagnosticsArray[28] = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
string scalpingDiagnosticsArray[] = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
string newsDiagnosticsArray[] = { "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "" };
    
double currencyStrengthArrayASC[8][2] = {{0,0},{0,1},{0,2},{0,3},{0,4},{0,5},{0,6},{0,7}}; 
double currencyStrengthArrayDESC[8][2] = {{0,0},{0,1},{0,2},{0,3},{0,4},{0,5},{0,6},{0,7}}; 

double currencyStrengthArrayASC_Long[8][2] = {{0,0},{0,1},{0,2},{0,3},{0,4},{0,5},{0,6},{0,7}}; 
double currencyStrengthArrayDESC_Long[8][2] = {{0,0},{0,1},{0,2},{0,3},{0,4},{0,5},{0,6},{0,7}}; 

double currencyStrengthArrayASC_News[8][2] = {{0,0},{0,1},{0,2},{0,3},{0,4},{0,5},{0,6},{0,7}}; 
double currencyStrengthArrayDESC_News[8][2] = {{0,0},{0,1},{0,2},{0,3},{0,4},{0,5},{0,6},{0,7}}; 

int closeBasketTicket[8] = {-1,-1,-1,-1,-1,-1,-1,-1};
int closeBasketType[8] = {-1,-1,-1,-1,-1,-1,-1,-1};
string closeBasketSymbol[8] = {"","","","","","","",""};
double closeBasketLots[8] = {0,0,0,0,0,0,0,0};
bool closingBasket = false;

int OnInit(){
   initTime = ( int ) TimeCurrent();
   accountBalance = AccountBalance();
   accountEquity = AccountEquity();
   highest = AccountBalance();   
   setPipPoint(); 
   maxLots = MarketInfo( Symbol(), MODE_MAXLOT );  
   minLots = MarketInfo( Symbol(), MODE_MINLOT );    
   display = "";
   if( EnableNews ){
      display = StringConcatenate( display, " \n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n" ); 
   } else {
      display = StringConcatenate( display, " \n" );
   }
   display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );
   display = StringConcatenate( display, " ", TradeComment, " v", ver, "\n" );
   display = StringConcatenate( display, " Waiting for next tick \n" ); 
   
   
   for( int i = 0; i < generalArrayCount; i++ ){
      sym[i] = symDefault[i];
      sym[i] = StringConcatenate( sym[i], SymbolSuffix );
   }
   
   Comment ( display );
   
   return( INIT_SUCCEEDED );
}
 
void OnDeinit( const int reason ){ 
   Comment( "" );  
}
   
void preparePrices(){
   
   if( EnableAUDCAD ){
      AUDCAD_high = iHigh( sym[0], TimeFrame, Shift );  
      AUDCAD_bid = iClose( sym[0], TimeFrame, Shift );
      AUDCAD_low = iLow( sym[0], TimeFrame, Shift );  
      symbolHigh[0] = AUDCAD_high;
      symbolLow[0] = AUDCAD_low;
      symbolBid[0] = AUDCAD_bid;
   }
   if( EnableAUDCHF ){
      AUDCHF_high = iHigh( sym[1], TimeFrame, Shift ); 
      AUDCHF_bid = iClose( sym[1], TimeFrame, Shift );
      AUDCHF_low = iLow( sym[1], TimeFrame, Shift ); 
      symbolHigh[1] = AUDCHF_high;
      symbolLow[1] = AUDCHF_low;
      symbolBid[1] = AUDCHF_bid;
   }
   
   if( EnableAUDJPY ){
      AUDJPY_high = iHigh( sym[2], TimeFrame, Shift ); 
      AUDJPY_bid = iClose( sym[2], TimeFrame, Shift );
      AUDJPY_low = iLow( sym[2], TimeFrame, Shift );
      symbolHigh[2] = AUDJPY_high;
      symbolLow[2] = AUDJPY_low;
      symbolBid[2] = AUDJPY_bid;
   } 
     
   if( EnableAUDNZD ){  
      AUDNZD_high = iHigh( sym[3], TimeFrame, Shift ); 
      AUDNZD_bid = iClose( sym[3], TimeFrame, Shift );
      AUDNZD_low = iLow( sym[3], TimeFrame, Shift );
      symbolHigh[3] = AUDNZD_high;
      symbolLow[3] = AUDNZD_low;
      symbolBid[3] = AUDNZD_bid;
   } 
    
   if( EnableAUDUSD ){  
      AUDUSD_high = iHigh( sym[4], TimeFrame, Shift ); 
      AUDUSD_bid = iClose( sym[4], TimeFrame, Shift );
      AUDUSD_low = iLow( sym[4], TimeFrame, Shift );
      symbolHigh[4] = AUDUSD_high;
      symbolLow[4] = AUDUSD_low;
      symbolBid[4] = AUDUSD_bid;
   }
   
   if( EnableCADCHF ){
      CADCHF_high = iHigh( sym[5], TimeFrame, Shift ); 
      CADCHF_bid = iClose( sym[5], TimeFrame, Shift );
      CADCHF_low = iLow( sym[5], TimeFrame, Shift );
      symbolHigh[5] = CADCHF_high;
      symbolLow[5] = CADCHF_low;
      symbolBid[5] = CADCHF_bid;
   }
   
   if( EnableCADJPY ){
      CADJPY_high = iHigh( sym[6], TimeFrame, Shift ); 
      CADJPY_bid = iClose( sym[6], TimeFrame, Shift );
      CADJPY_low = iLow( sym[6], TimeFrame, Shift );
      symbolHigh[6] = CADJPY_high;
      symbolLow[6] = CADJPY_low;
      symbolBid[6] = CADJPY_bid;
   }
   
   if( EnableCHFJPY ){
      CHFJPY_high = iHigh( sym[7], TimeFrame, Shift ); 
      CHFJPY_bid = iClose( sym[7], TimeFrame, Shift );
      CHFJPY_low = iLow( sym[7], TimeFrame, Shift );
      symbolHigh[7] = CHFJPY_high;
      symbolLow[7] = CHFJPY_low;
      symbolBid[7] = CHFJPY_bid;
   }
   
   if( EnableEURAUD ){
      EURAUD_high = iHigh( sym[8], TimeFrame, Shift ); 
      EURAUD_bid = iClose( sym[8], TimeFrame, Shift );
      EURAUD_low = iLow( sym[8], TimeFrame, Shift );
      symbolHigh[8] = EURAUD_high;
      symbolLow[8] = EURAUD_low;
      symbolBid[8] = EURAUD_bid;
   }
   
   if( EnableEURCAD ){
      EURCAD_high = iHigh( sym[9], TimeFrame, Shift ); 
      EURCAD_bid = iClose( sym[9], TimeFrame, Shift );
      EURCAD_low = iLow( sym[9], TimeFrame, Shift );
      symbolHigh[9] = EURCAD_high;
      symbolLow[9] = EURCAD_low;
      symbolBid[9] = EURCAD_bid;
   }
   
   if( EnableEURCHF ){
      EURCHF_high = iHigh( sym[10], TimeFrame, Shift ); 
      EURCHF_bid = iClose( sym[10], TimeFrame, Shift );
      EURCHF_low = iLow( sym[10], TimeFrame, Shift );
      symbolHigh[10] = EURCHF_high;
      symbolLow[10] = EURCHF_low;
      symbolBid[10] = EURCHF_bid;
   }
   
   if( EnableEURGBP ){
      EURGBP_high = iHigh( sym[11], TimeFrame, Shift ); 
      EURGBP_bid = iClose( sym[11], TimeFrame, Shift );
      EURGBP_low = iLow( sym[11], TimeFrame, Shift );
      symbolHigh[11] = EURGBP_high;
      symbolLow[11] = EURGBP_low;
      symbolBid[11] = EURGBP_bid;
   }
   
   if( EnableEURJPY ){
      EURJPY_high = iHigh( sym[12], TimeFrame, Shift ); 
      EURJPY_bid = iClose( sym[12], TimeFrame, Shift );
      EURJPY_low = iLow( sym[12], TimeFrame, Shift );
      symbolHigh[12] = EURJPY_high;
      symbolLow[12] = EURJPY_low;
      symbolBid[12] = EURJPY_bid;
   }
   
   if( EnableEURNZD ){
      EURNZD_high = iHigh( sym[13], TimeFrame, Shift ); 
      EURNZD_bid = iClose( sym[13], TimeFrame, Shift );
      EURNZD_low = iLow( sym[13], TimeFrame, Shift );
      symbolHigh[13] = EURNZD_high;
      symbolLow[13] = EURNZD_low;
      symbolBid[13] = EURNZD_bid;
   }
   
   if( EnableEURUSD ){
      EURUSD_high = iHigh( sym[14], TimeFrame, Shift ); 
      EURUSD_bid = iClose( sym[14], TimeFrame, Shift );
      EURUSD_low = iLow( sym[14], TimeFrame, Shift );
      symbolHigh[14] = EURUSD_high;
      symbolLow[14] = EURUSD_low;
      symbolBid[14] = EURUSD_bid;
   }
   
   if( EnableGBPAUD ){
      GBPAUD_high = iHigh( sym[15], TimeFrame, Shift ); 
      GBPAUD_bid = iClose( sym[15], TimeFrame, Shift );
      GBPAUD_low = iLow( sym[15], TimeFrame, Shift );
      symbolHigh[15] = GBPAUD_high;
      symbolLow[15] = GBPAUD_low;
      symbolBid[15] = GBPAUD_bid;
   }
   
   if( EnableGBPCAD ){
      GBPCAD_high = iHigh( sym[16], TimeFrame, Shift ); 
      GBPCAD_bid = iClose( sym[16], TimeFrame, Shift );
      GBPCAD_low = iLow( sym[16], TimeFrame, Shift );
      symbolHigh[16] = GBPCAD_high;
      symbolLow[16] = GBPCAD_low;
      symbolBid[16] = GBPCAD_bid;
   }
   
   if( EnableGBPCHF ){
      GBPCHF_high = iHigh( sym[17], TimeFrame, Shift ); 
      GBPCHF_bid = iClose( sym[17], TimeFrame, Shift );
      GBPCHF_low = iLow( sym[17], TimeFrame, Shift );
      symbolHigh[17] = GBPCHF_high;
      symbolLow[17] = GBPCHF_low;
      symbolBid[17] = GBPCHF_bid;
   }
   
   if( EnableGBPJPY ){
      GBPJPY_high = iHigh( sym[18], TimeFrame, Shift ); 
      GBPJPY_bid = iClose( sym[18], TimeFrame, Shift );
      GBPJPY_low = iLow( sym[18], TimeFrame, Shift );
      symbolHigh[18] = GBPJPY_high;
      symbolLow[18] = GBPJPY_low;
      symbolBid[18] = GBPJPY_bid;
   }
   
   if( EnableGBPNZD ){
      GBPNZD_high = iHigh( sym[19], TimeFrame, Shift ); 
      GBPNZD_bid = iClose( sym[19], TimeFrame, Shift );
      GBPNZD_low = iLow( sym[19], TimeFrame, Shift );
      symbolHigh[19] = GBPNZD_high;
      symbolLow[19] = GBPNZD_low;
      symbolBid[19] = GBPNZD_bid;
   }
 
   if( EnableGBPUSD ){
      GBPUSD_high = iHigh( sym[20], TimeFrame, Shift ); 
      GBPUSD_bid = iClose( sym[20], TimeFrame, Shift );
      GBPUSD_low = iLow( sym[20], TimeFrame, Shift );
      symbolHigh[20] = GBPUSD_high;
      symbolLow[20] = GBPUSD_low;
      symbolBid[20] = GBPUSD_bid;
   }
   
   if( EnableNZDCAD ){
      NZDCAD_high = iHigh( sym[21], TimeFrame, Shift ); 
      NZDCAD_bid = iClose( sym[21], TimeFrame, Shift );
      NZDCAD_low = iLow( sym[21], TimeFrame, Shift );
      symbolHigh[21] = NZDCAD_high;
      symbolLow[21] = NZDCAD_low;
      symbolBid[21] = NZDCAD_bid;
   }
   
   if( EnableNZDCHF ){
      NZDCHF_high = iHigh( sym[22], TimeFrame, Shift ); 
      NZDCHF_bid = iClose( sym[22], TimeFrame, Shift );
      NZDCHF_low = iLow( sym[22], TimeFrame, Shift );
      symbolHigh[22] = NZDCHF_high;
      symbolLow[22] = NZDCHF_low;
      symbolBid[22] = NZDCHF_bid;
   }
   
   if( EnableNZDJPY ){
      NZDJPY_high = iHigh( sym[23], TimeFrame, Shift ); 
      NZDJPY_bid = iClose( sym[23], TimeFrame, Shift );
      NZDJPY_low = iLow( sym[23], TimeFrame, Shift );
      symbolHigh[23] = NZDJPY_high;
      symbolLow[23] = NZDJPY_low;
      symbolBid[23] = NZDJPY_bid;
   }
   
   if( EnableNZDUSD ){
      NZDUSD_high = iHigh( sym[24], TimeFrame, Shift ); 
      NZDUSD_bid = iClose( sym[24], TimeFrame, Shift );
      NZDUSD_low = iLow( sym[24], TimeFrame, Shift );
      symbolHigh[24] = NZDUSD_high;
      symbolLow[24] = NZDUSD_low;
      symbolBid[24] = NZDUSD_bid;
   }
   
   if( EnableUSDCAD ){
      USDCAD_high = iHigh( sym[25], TimeFrame, Shift ); 
      USDCAD_bid = iClose( sym[25], TimeFrame, Shift );
      USDCAD_low = iLow( sym[25], TimeFrame, Shift );
      symbolHigh[25] = USDCAD_high;
      symbolLow[25] = USDCAD_low;
      symbolBid[25] = USDCAD_bid;
   }
   
   if( EnableUSDCHF ){
      USDCHF_high = iHigh( sym[26], TimeFrame, Shift ); 
      USDCHF_bid = iClose( sym[26], TimeFrame, Shift );
      USDCHF_low = iLow( sym[26], TimeFrame, Shift );
      symbolHigh[26] = USDCHF_high;
      symbolLow[26] = USDCHF_low;
      symbolBid[26] = USDCHF_bid;
   }
   
   if( EnableUSDJPY ){
      USDJPY_high = iHigh( sym[27], TimeFrame, Shift ); 
      USDJPY_bid = iClose( sym[27], TimeFrame, Shift );
      USDJPY_low = iLow( sym[27], TimeFrame, Shift );
      symbolHigh[27] = USDJPY_high;
      symbolLow[27] = USDJPY_low;
      symbolBid[27] = USDJPY_bid;
   } 
   
}  

void preparePrices_Long(){
   
   if( EnableAUDCAD ){
      AUDCAD_high_Long = iHigh( sym[0], TimeFrame_Long, Shift_Long );  
      AUDCAD_bid_Long = iClose( sym[0], TimeFrame_Long, Shift_Long );
      AUDCAD_low_Long = iLow( sym[0], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[0] = USDJPY_high_Long;
      symbolLow_Long[0] = USDJPY_low_Long;
      symbolBid_Long[0] = USDJPY_bid_Long;
   }
   if( EnableAUDCHF ){
      AUDCHF_high_Long = iHigh( sym[1], TimeFrame_Long, Shift_Long ); 
      AUDCHF_bid_Long = iClose( sym[1], TimeFrame_Long, Shift_Long );
      AUDCHF_low_Long = iLow( sym[1], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[1] = AUDCHF_high_Long;
      symbolLow_Long[1] = AUDCHF_low_Long;
      symbolBid_Long[1] = AUDCHF_bid_Long;
   }
   
   if( EnableAUDJPY ){
      AUDJPY_high_Long = iHigh( sym[2], TimeFrame_Long, Shift_Long ); 
      AUDJPY_bid_Long = iClose( sym[2], TimeFrame_Long, Shift_Long );
      AUDJPY_low_Long = iLow( sym[2], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[2] = AUDJPY_high_Long;
      symbolLow_Long[2] = AUDJPY_low_Long;
      symbolBid_Long[2] = AUDJPY_bid_Long;
   } 
     
   if( EnableAUDNZD ){  
      AUDNZD_high_Long = iHigh( sym[3], TimeFrame_Long, Shift_Long ); 
      AUDNZD_bid_Long = iClose( sym[3], TimeFrame_Long, Shift_Long );
      AUDNZD_low_Long = iLow( sym[3], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[3] = AUDNZD_high_Long;
      symbolLow_Long[3] = AUDNZD_low_Long;
      symbolBid_Long[3] = AUDNZD_bid_Long;
   } 
    
   if( EnableAUDUSD ){  
      AUDUSD_high_Long = iHigh( sym[4], TimeFrame_Long, Shift_Long ); 
      AUDUSD_bid_Long = iClose( sym[4], TimeFrame_Long, Shift_Long );
      AUDUSD_low_Long = iLow( sym[4], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[4] = AUDUSD_high_Long;
      symbolLow_Long[4] = AUDUSD_low_Long;
      symbolBid_Long[4] = AUDUSD_bid_Long;
   }
   
   if( EnableCADCHF ){
      CADCHF_high_Long = iHigh( sym[5], TimeFrame_Long, Shift_Long ); 
      CADCHF_bid_Long = iClose( sym[5], TimeFrame_Long, Shift_Long );
      CADCHF_low_Long = iLow( sym[5], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[5] = CADCHF_high_Long;
      symbolLow_Long[5] = CADCHF_low_Long;
      symbolBid_Long[5] = CADCHF_bid_Long;
   }
   
   if( EnableCADJPY ){
      CADJPY_high_Long = iHigh( sym[6], TimeFrame_Long, Shift_Long ); 
      CADJPY_bid_Long = iClose( sym[6], TimeFrame_Long, Shift_Long );
      CADJPY_low_Long = iLow( sym[6], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[6] = CADJPY_high_Long;
      symbolLow_Long[6] = CADJPY_low_Long;
      symbolBid_Long[6] = CADJPY_bid_Long;
   }
   
   if( EnableCHFJPY ){
      CHFJPY_high_Long = iHigh( sym[7], TimeFrame_Long, Shift_Long ); 
      CHFJPY_bid_Long = iClose( sym[7], TimeFrame_Long, Shift_Long );
      CHFJPY_low_Long = iLow( sym[7], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[7] = CHFJPY_high_Long;
      symbolLow_Long[7] = CHFJPY_low_Long;
      symbolBid_Long[7] = CHFJPY_bid_Long;
   }
   
   if( EnableEURAUD ){
      EURAUD_high_Long = iHigh( sym[8], TimeFrame_Long, Shift_Long ); 
      EURAUD_bid_Long = iClose( sym[8], TimeFrame_Long, Shift_Long );
      EURAUD_low_Long = iLow( sym[8], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[8] = EURAUD_high_Long;
      symbolLow_Long[8] = EURAUD_low_Long;
      symbolBid_Long[8] = EURAUD_bid_Long;
   }
   
   if( EnableEURCAD ){
      EURCAD_high_Long = iHigh( sym[9], TimeFrame_Long, Shift_Long ); 
      EURCAD_bid_Long = iClose( sym[9], TimeFrame_Long, Shift_Long );
      EURCAD_low_Long = iLow( sym[9], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[9] = EURCAD_high_Long;
      symbolLow_Long[9] = EURCAD_low_Long;
      symbolBid_Long[9] = EURCAD_bid_Long;
   }
   
   if( EnableEURCHF ){
      EURCHF_high_Long = iHigh( sym[10], TimeFrame_Long, Shift_Long ); 
      EURCHF_bid_Long = iClose( sym[10], TimeFrame_Long, Shift_Long );
      EURCHF_low_Long = iLow( sym[10], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[10] = EURCHF_high_Long;
      symbolLow_Long[10] = EURCHF_low_Long;
      symbolBid_Long[10] = EURCHF_bid_Long;
   }
   
   if( EnableEURGBP ){
      EURGBP_high_Long = iHigh( sym[11], TimeFrame_Long, Shift_Long ); 
      EURGBP_bid_Long = iClose( sym[11], TimeFrame_Long, Shift_Long );
      EURGBP_low_Long = iLow( sym[11], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[11] = EURGBP_high_Long;
      symbolLow_Long[11] = EURGBP_low_Long;
      symbolBid_Long[11] = EURGBP_bid_Long;
   }
   
   if( EnableEURJPY ){
      EURJPY_high_Long = iHigh( sym[12], TimeFrame_Long, Shift_Long ); 
      EURJPY_bid_Long = iClose( sym[12], TimeFrame_Long, Shift_Long );
      EURJPY_low_Long = iLow( sym[12], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[12] = EURJPY_high_Long;
      symbolLow_Long[12] = EURJPY_low_Long;
      symbolBid_Long[12] = EURJPY_bid_Long;
   }
   
   if( EnableEURNZD ){
      EURNZD_high_Long = iHigh( sym[13], TimeFrame_Long, Shift_Long ); 
      EURNZD_bid_Long = iClose( sym[13], TimeFrame_Long, Shift_Long );
      EURNZD_low_Long = iLow( sym[13], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[13] = EURNZD_high_Long;
      symbolLow_Long[13] = EURNZD_low_Long;
      symbolBid_Long[13] = EURNZD_bid_Long;
   }
   
   if( EnableEURUSD ){
      EURUSD_high_Long = iHigh( sym[14], TimeFrame_Long, Shift_Long ); 
      EURUSD_bid_Long = iClose( sym[14], TimeFrame_Long, Shift_Long );
      EURUSD_low_Long = iLow( sym[14], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[14] = EURUSD_high_Long;
      symbolLow_Long[14] = EURUSD_low_Long;
      symbolBid_Long[14] = EURUSD_bid_Long;
   }
   
   if( EnableGBPAUD ){
      GBPAUD_high_Long = iHigh( sym[15], TimeFrame_Long, Shift_Long ); 
      GBPAUD_bid_Long = iClose( sym[15], TimeFrame_Long, Shift_Long );
      GBPAUD_low_Long = iLow( sym[15], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[15] = GBPAUD_high_Long;
      symbolLow_Long[15] = GBPAUD_low_Long;
      symbolBid_Long[15] = GBPAUD_bid_Long;
   }
   
   if( EnableGBPCAD ){
      GBPCAD_high_Long = iHigh( sym[16], TimeFrame_Long, Shift_Long ); 
      GBPCAD_bid_Long = iClose( sym[16], TimeFrame_Long, Shift_Long );
      GBPCAD_low_Long = iLow( sym[16], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[16] = GBPCAD_high_Long;
      symbolLow_Long[16] = GBPCAD_low_Long;
      symbolBid_Long[16] = GBPCAD_bid_Long;
   }
   
   if( EnableGBPCHF ){
      GBPCHF_high_Long = iHigh( sym[17], TimeFrame_Long, Shift_Long ); 
      GBPCHF_bid_Long = iClose( sym[17], TimeFrame_Long, Shift_Long );
      GBPCHF_low_Long = iLow( sym[17], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[17] = GBPCHF_high_Long;
      symbolLow_Long[17] = GBPCHF_low_Long;
      symbolBid_Long[17] = GBPCHF_bid_Long;
   }
   
   if( EnableGBPJPY ){
      GBPJPY_high_Long = iHigh( sym[18], TimeFrame_Long, Shift_Long ); 
      GBPJPY_bid_Long = iClose( sym[18], TimeFrame_Long, Shift_Long );
      GBPJPY_low_Long = iLow( sym[18], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[18] = GBPJPY_high_Long;
      symbolLow_Long[18] = GBPJPY_low_Long;
      symbolBid_Long[18] = GBPJPY_bid_Long;
   }
   
   if( EnableGBPNZD ){
      GBPNZD_high_Long = iHigh( sym[19], TimeFrame_Long, Shift_Long ); 
      GBPNZD_bid_Long = iClose( sym[19], TimeFrame_Long, Shift_Long );
      GBPNZD_low_Long = iLow( sym[19], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[19] = GBPNZD_high_Long;
      symbolLow_Long[19] = GBPNZD_low_Long;
      symbolBid_Long[19] = GBPNZD_bid_Long;
   }
 
   if( EnableGBPUSD ){
      GBPUSD_high_Long = iHigh( sym[20], TimeFrame_Long, Shift_Long ); 
      GBPUSD_bid_Long = iClose( sym[20], TimeFrame_Long, Shift_Long );
      GBPUSD_low_Long = iLow( sym[20], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[20] = GBPUSD_high_Long;
      symbolLow_Long[20] = GBPUSD_low_Long;
      symbolBid_Long[20] = GBPUSD_bid_Long;
   }
   
   if( EnableNZDCAD ){
      NZDCAD_high_Long = iHigh( sym[21], TimeFrame_Long, Shift_Long ); 
      NZDCAD_bid_Long = iClose( sym[21], TimeFrame_Long, Shift_Long );
      NZDCAD_low_Long = iLow( sym[21], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[21] = NZDCAD_high_Long;
      symbolLow_Long[21] = NZDCAD_low_Long;
      symbolBid_Long[21] = NZDCAD_bid_Long;
   }
   
   if( EnableNZDCHF ){
      NZDCHF_high_Long = iHigh( sym[22], TimeFrame_Long, Shift_Long ); 
      NZDCHF_bid_Long = iClose( sym[22], TimeFrame_Long, Shift_Long );
      NZDCHF_low_Long = iLow( sym[22], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[22] = NZDCHF_high_Long;
      symbolLow_Long[22] = NZDCHF_low_Long;
      symbolBid_Long[22] = NZDCHF_bid_Long;
   }
   
   if( EnableNZDJPY ){
      NZDJPY_high_Long = iHigh( sym[23], TimeFrame_Long, Shift_Long ); 
      NZDJPY_bid_Long = iClose( sym[23], TimeFrame_Long, Shift_Long );
      NZDJPY_low_Long = iLow( sym[23], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[23] = NZDJPY_high_Long;
      symbolLow_Long[23] = NZDJPY_low_Long;
      symbolBid_Long[23] = NZDJPY_bid_Long;
   }
   
   if( EnableNZDUSD ){
      NZDUSD_high_Long = iHigh( sym[24], TimeFrame_Long, Shift_Long ); 
      NZDUSD_bid_Long = iClose( sym[24], TimeFrame_Long, Shift_Long );
      NZDUSD_low_Long = iLow( sym[24], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[24] = NZDUSD_high_Long;
      symbolLow_Long[24] = NZDUSD_low_Long;
      symbolBid_Long[24] = NZDUSD_bid_Long;
   }
   
   if( EnableUSDCAD ){
      USDCAD_high_Long = iHigh( sym[25], TimeFrame_Long, Shift_Long ); 
      USDCAD_bid_Long = iClose( sym[25], TimeFrame_Long, Shift_Long );
      USDCAD_low_Long = iLow( sym[25], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[25] = USDCAD_high_Long;
      symbolLow_Long[25] = USDCAD_low_Long;
      symbolBid_Long[25] = USDCAD_bid_Long;
   }
   
   if( EnableUSDCHF ){
      USDCHF_high_Long = iHigh( sym[26], TimeFrame_Long, Shift_Long ); 
      USDCHF_bid_Long = iClose( sym[26], TimeFrame_Long, Shift_Long );
      USDCHF_low_Long = iLow( sym[26], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[26] = USDCHF_high_Long;
      symbolLow_Long[26] = USDCHF_low_Long;
      symbolBid_Long[26] = USDCHF_bid_Long;
   }
   
   if( EnableUSDJPY ){
      USDJPY_high_Long = iHigh( sym[27], TimeFrame_Long, Shift_Long ); 
      USDJPY_bid_Long = iClose( sym[27], TimeFrame_Long, Shift_Long );
      USDJPY_low_Long = iLow( sym[27], TimeFrame_Long, Shift_Long );
      symbolHigh_Long[27] = USDJPY_high_Long;
      symbolLow_Long[27] = USDJPY_low_Long;
      symbolBid_Long[27] = USDJPY_bid_Long;
   }
} 

void calculatePipRange(){
   if( EnableAUDCAD ) pipRange_AUDCAD = ( AUDCAD_high - AUDCAD_low ) * 10000;  
   if( EnableAUDCHF ) pipRange_AUDCHF = ( AUDCHF_high - AUDCHF_low ) * 10000;
   if( EnableAUDJPY ) pipRange_AUDJPY = ( AUDJPY_high - AUDJPY_low ) * 100;
   if( EnableAUDNZD ) pipRange_AUDNZD = ( AUDNZD_high - AUDNZD_low ) * 10000;
   if( EnableAUDUSD ) pipRange_AUDUSD = ( AUDUSD_high - AUDUSD_low ) * 10000; 
   if( EnableCADCHF ) pipRange_CADCHF = ( CADCHF_high - CADCHF_low ) * 10000;
   if( EnableCADJPY ) pipRange_CADJPY = ( CADJPY_high - CADJPY_low ) * 100;
   if( EnableCHFJPY ) pipRange_CHFJPY = ( CHFJPY_high - CHFJPY_low ) * 100;
   if( EnableEURAUD ) pipRange_EURAUD = ( EURAUD_high - EURAUD_low ) * 10000;
   if( EnableEURCAD ) pipRange_EURCAD = ( EURCAD_high - EURCAD_low ) * 10000;
   if( EnableEURCHF ) pipRange_EURCHF = ( EURCHF_high - EURCHF_low ) * 10000; 
   if( EnableEURGBP ) pipRange_EURGBP = ( EURGBP_high - EURGBP_low ) * 10000; 
   if( EnableEURJPY ) pipRange_EURJPY = ( EURJPY_high - EURJPY_low ) * 100; 
   if( EnableEURNZD ) pipRange_EURNZD = ( EURNZD_high - EURNZD_low ) * 10000; 
   if( EnableEURUSD ) pipRange_EURUSD = ( EURUSD_high - EURUSD_low ) * 10000; 
   if( EnableGBPAUD ) pipRange_GBPAUD = ( GBPAUD_high - GBPAUD_low ) * 10000; 
   if( EnableGBPCAD ) pipRange_GBPCAD = ( GBPCAD_high - GBPCAD_low ) * 10000;
   if( EnableGBPCHF ) pipRange_GBPCHF = ( GBPCHF_high - GBPCHF_low ) * 10000;
   if( EnableGBPJPY ) pipRange_GBPJPY = ( GBPJPY_high - GBPJPY_low ) * 100;
   if( EnableGBPNZD ) pipRange_GBPNZD = ( GBPNZD_high - GBPNZD_low ) * 10000;
   if( EnableGBPUSD ) pipRange_GBPUSD = ( GBPUSD_high - GBPUSD_low ) * 10000;
   if( EnableNZDCAD ) pipRange_NZDCAD = ( NZDCAD_high - NZDCAD_low ) * 10000;
   if( EnableNZDCHF ) pipRange_NZDCHF = ( NZDCHF_high - NZDCHF_low ) * 10000;
   if( EnableNZDJPY ) pipRange_NZDJPY = ( NZDJPY_high - NZDJPY_low ) * 100;
   if( EnableNZDUSD ) pipRange_NZDUSD = ( NZDUSD_high - NZDUSD_low ) * 10000;
   if( EnableUSDCAD ) pipRange_USDCAD = ( USDCAD_high - USDCAD_low ) * 10000;
   if( EnableUSDCHF ) pipRange_USDCHF = ( USDCHF_high - USDCHF_low ) * 10000;
   if( EnableUSDJPY ) pipRange_USDJPY = ( USDJPY_high - USDJPY_low ) * 100;
}

void calculatePipRange_Long(){
   if( EnableAUDCAD ) pipRange_AUDCAD_Long = ( AUDCAD_high_Long - AUDCAD_low_Long ) * 10000;  
   if( EnableAUDCHF ) pipRange_AUDCHF_Long = ( AUDCHF_high_Long - AUDCHF_low_Long ) * 10000;
   if( EnableAUDJPY ) pipRange_AUDJPY_Long = ( AUDJPY_high_Long - AUDJPY_low_Long ) * 100;
   if( EnableAUDNZD ) pipRange_AUDNZD_Long = ( AUDNZD_high_Long - AUDNZD_low_Long ) * 10000;
   if( EnableAUDUSD ) pipRange_AUDUSD_Long = ( AUDUSD_high_Long - AUDUSD_low_Long ) * 10000; 
   if( EnableCADCHF ) pipRange_CADCHF_Long = ( CADCHF_high_Long - CADCHF_low_Long ) * 10000;
   if( EnableCADJPY ) pipRange_CADJPY_Long = ( CADJPY_high_Long - CADJPY_low_Long ) * 100;
   if( EnableCHFJPY ) pipRange_CHFJPY_Long = ( CHFJPY_high_Long - CHFJPY_low_Long ) * 100;
   if( EnableEURAUD ) pipRange_EURAUD_Long = ( EURAUD_high_Long - EURAUD_low_Long ) * 10000;
   if( EnableEURCAD ) pipRange_EURCAD_Long = ( EURCAD_high_Long - EURCAD_low_Long ) * 10000;
   if( EnableEURCHF ) pipRange_EURCHF_Long = ( EURCHF_high_Long - EURCHF_low_Long ) * 10000; 
   if( EnableEURGBP ) pipRange_EURGBP_Long = ( EURGBP_high_Long - EURGBP_low_Long ) * 10000; 
   if( EnableEURJPY ) pipRange_EURJPY_Long = ( EURJPY_high_Long - EURJPY_low_Long ) * 100; 
   if( EnableEURNZD ) pipRange_EURNZD_Long = ( EURNZD_high_Long - EURNZD_low_Long ) * 10000; 
   if( EnableEURUSD ) pipRange_EURUSD_Long = ( EURUSD_high_Long - EURUSD_low_Long ) * 10000; 
   if( EnableGBPAUD ) pipRange_GBPAUD_Long = ( GBPAUD_high_Long - GBPAUD_low_Long ) * 10000; 
   if( EnableGBPCAD ) pipRange_GBPCAD_Long = ( GBPCAD_high_Long - GBPCAD_low_Long ) * 10000;
   if( EnableGBPCHF ) pipRange_GBPCHF_Long = ( GBPCHF_high_Long - GBPCHF_low_Long ) * 10000;
   if( EnableGBPJPY ) pipRange_GBPJPY_Long = ( GBPJPY_high_Long - GBPJPY_low_Long ) * 100;
   if( EnableGBPNZD ) pipRange_GBPNZD_Long = ( GBPNZD_high_Long - GBPNZD_low_Long ) * 10000;
   if( EnableGBPUSD ) pipRange_GBPUSD_Long = ( GBPUSD_high_Long - GBPUSD_low_Long ) * 10000;
   if( EnableNZDCAD ) pipRange_NZDCAD_Long = ( NZDCAD_high_Long - NZDCAD_low_Long ) * 10000;
   if( EnableNZDCHF ) pipRange_NZDCHF_Long = ( NZDCHF_high_Long - NZDCHF_low_Long ) * 10000;
   if( EnableNZDJPY ) pipRange_NZDJPY_Long = ( NZDJPY_high_Long - NZDJPY_low_Long ) * 100;
   if( EnableNZDUSD ) pipRange_NZDUSD_Long = ( NZDUSD_high_Long - NZDUSD_low_Long ) * 10000;
   if( EnableUSDCAD ) pipRange_USDCAD_Long = ( USDCAD_high_Long - USDCAD_low_Long ) * 10000;
   if( EnableUSDCHF ) pipRange_USDCHF_Long = ( USDCHF_high_Long - USDCHF_low_Long ) * 10000;
   if( EnableUSDJPY ) pipRange_USDJPY_Long = ( USDJPY_high_Long - USDJPY_low_Long ) * 100;
} 

void calculateBidRatio(){ 
   if( EnableAUDCAD && pipRange_AUDCAD != 0 ) bidRatio_AUDCAD = ( AUDCAD_bid - AUDCAD_low ) / pipRange_AUDCAD * 10000 * 100;
   if( EnableAUDCHF && pipRange_AUDCHF != 0 ) bidRatio_AUDCHF = ( AUDCHF_bid - AUDCHF_low ) / pipRange_AUDCHF * 10000 * 100;
   if( EnableAUDJPY && pipRange_AUDJPY != 0 ) bidRatio_AUDJPY = ( AUDJPY_bid - AUDJPY_low ) / pipRange_AUDJPY * 100 * 100;
   if( EnableAUDNZD && pipRange_AUDNZD != 0 ) bidRatio_AUDNZD = ( AUDNZD_bid - AUDNZD_low ) / pipRange_AUDNZD * 10000 * 100;
   if( EnableAUDUSD && pipRange_AUDUSD != 0 ) bidRatio_AUDUSD = ( AUDUSD_bid - AUDUSD_low ) / pipRange_AUDUSD * 10000 * 100;  
   if( EnableCADCHF && pipRange_CADCHF != 0 ) bidRatio_CADCHF = ( CADCHF_bid - CADCHF_low ) / pipRange_CADCHF * 10000 * 100;
   if( EnableCADJPY && pipRange_CADJPY != 0 ) bidRatio_CADJPY = ( CADJPY_bid - CADJPY_low ) / pipRange_CADJPY * 100 * 100;
   if( EnableCHFJPY && pipRange_CHFJPY != 0 ) bidRatio_CHFJPY = ( CHFJPY_bid - CHFJPY_low ) / pipRange_CHFJPY * 100 * 100;
   if( EnableEURAUD && pipRange_EURAUD != 0 ) bidRatio_EURAUD = ( EURAUD_bid - EURAUD_low ) / pipRange_EURAUD * 10000 * 100;
   if( EnableEURCAD && pipRange_EURCAD != 0 ) bidRatio_EURCAD = ( EURCAD_bid - EURCAD_low ) / pipRange_EURCAD * 10000 * 100;
   if( EnableEURCHF && pipRange_EURCHF != 0 ) bidRatio_EURCHF = ( EURCHF_bid - EURCHF_low ) / pipRange_EURCHF * 10000 * 100; 
   if( EnableEURGBP && pipRange_EURGBP != 0 ) bidRatio_EURGBP = ( EURGBP_bid - EURGBP_low ) / pipRange_EURGBP * 10000 * 100;
   if( EnableEURJPY && pipRange_EURJPY != 0 ) bidRatio_EURJPY = ( EURJPY_bid - EURJPY_low ) / pipRange_EURJPY * 100 * 100;
   if( EnableEURNZD && pipRange_EURNZD != 0 ) bidRatio_EURNZD = ( EURNZD_bid - EURNZD_low ) / pipRange_EURNZD * 10000 * 100;
   if( EnableEURUSD && pipRange_EURUSD != 0 ) bidRatio_EURUSD = ( EURUSD_bid - EURUSD_low ) / pipRange_EURUSD * 10000 * 100;
   if( EnableGBPAUD && pipRange_GBPAUD != 0 ) bidRatio_GBPAUD = ( GBPAUD_bid - GBPAUD_low ) / pipRange_GBPAUD * 10000 * 100;
   if( EnableGBPCAD && pipRange_GBPCAD != 0 ) bidRatio_GBPCAD = ( GBPCAD_bid - GBPCAD_low ) / pipRange_GBPCAD * 10000 * 100;
   if( EnableGBPCHF && pipRange_GBPCHF != 0 ) bidRatio_GBPCHF = ( GBPCHF_bid - GBPCHF_low ) / pipRange_GBPCHF * 10000 * 100;
   if( EnableGBPJPY && pipRange_GBPJPY != 0 ) bidRatio_GBPJPY = ( GBPJPY_bid - GBPJPY_low ) / pipRange_GBPJPY * 100 * 100;
   if( EnableGBPNZD && pipRange_GBPNZD != 0 ) bidRatio_GBPNZD = ( GBPNZD_bid - GBPNZD_low ) / pipRange_GBPNZD * 10000 * 100;
   if( EnableGBPUSD && pipRange_GBPUSD != 0 ) bidRatio_GBPUSD = ( GBPUSD_bid - GBPUSD_low ) / pipRange_GBPUSD * 10000 * 100;
   if( EnableNZDCAD && pipRange_NZDCAD != 0 ) bidRatio_NZDCAD = ( NZDCAD_bid - NZDCAD_low ) / pipRange_NZDCAD * 10000 * 100;
   if( EnableNZDCHF && pipRange_NZDCHF != 0 ) bidRatio_NZDCHF = ( NZDCHF_bid - NZDCHF_low ) / pipRange_NZDCHF * 10000 * 100;
   if( EnableNZDJPY && pipRange_NZDJPY != 0 ) bidRatio_NZDJPY = ( NZDJPY_bid - NZDJPY_low ) / pipRange_NZDJPY * 100 * 100;
   if( EnableNZDUSD && pipRange_NZDUSD != 0 ) bidRatio_NZDUSD = ( NZDUSD_bid - NZDUSD_low ) / pipRange_NZDUSD * 10000 * 100;
   if( EnableUSDCAD && pipRange_USDCAD != 0 ) bidRatio_USDCAD = ( USDCAD_bid - USDCAD_low ) / pipRange_USDCAD * 10000 * 100;
   if( EnableUSDCHF && pipRange_USDCHF != 0 ) bidRatio_USDCHF = ( USDCHF_bid - USDCHF_low ) / pipRange_USDCHF * 10000 * 100;
   if( EnableUSDJPY && pipRange_USDJPY != 0 ) bidRatio_USDJPY = ( USDJPY_bid - USDJPY_low ) / pipRange_USDJPY * 100 * 100;
}

void calculateBidRatio_Long(){ 
   if( EnableAUDCAD && pipRange_AUDCAD_Long != 0 ) bidRatio_AUDCAD_Long = ( AUDCAD_bid_Long - AUDCAD_low_Long ) / pipRange_AUDCAD_Long * 10000 * 100;
   if( EnableAUDCHF && pipRange_AUDCHF_Long != 0 ) bidRatio_AUDCHF_Long = ( AUDCHF_bid_Long - AUDCHF_low_Long ) / pipRange_AUDCHF_Long * 10000 * 100;
   if( EnableAUDJPY && pipRange_AUDJPY_Long != 0 ) bidRatio_AUDJPY_Long = ( AUDJPY_bid_Long - AUDJPY_low_Long ) / pipRange_AUDJPY_Long * 100 * 100;
   if( EnableAUDNZD && pipRange_AUDNZD_Long != 0 ) bidRatio_AUDNZD_Long = ( AUDNZD_bid_Long - AUDNZD_low_Long ) / pipRange_AUDNZD_Long * 10000 * 100;
   if( EnableAUDUSD && pipRange_AUDUSD_Long != 0 ) bidRatio_AUDUSD_Long = ( AUDUSD_bid_Long - AUDUSD_low_Long ) / pipRange_AUDUSD_Long * 10000 * 100;  
   if( EnableCADCHF && pipRange_CADCHF_Long != 0 ) bidRatio_CADCHF_Long = ( CADCHF_bid_Long - CADCHF_low_Long ) / pipRange_CADCHF_Long * 10000 * 100;
   if( EnableCADJPY && pipRange_CADJPY_Long != 0 ) bidRatio_CADJPY_Long = ( CADJPY_bid_Long - CADJPY_low_Long ) / pipRange_CADJPY_Long * 100 * 100;
   if( EnableCHFJPY && pipRange_CHFJPY_Long != 0 ) bidRatio_CHFJPY_Long = ( CHFJPY_bid_Long - CHFJPY_low_Long ) / pipRange_CHFJPY_Long * 100 * 100;
   if( EnableEURAUD && pipRange_EURAUD_Long != 0 ) bidRatio_EURAUD_Long = ( EURAUD_bid_Long - EURAUD_low_Long ) / pipRange_EURAUD_Long * 10000 * 100;
   if( EnableEURCAD && pipRange_EURCAD_Long != 0 ) bidRatio_EURCAD_Long = ( EURCAD_bid_Long - EURCAD_low_Long ) / pipRange_EURCAD_Long * 10000 * 100;
   if( EnableEURCHF && pipRange_EURCHF_Long != 0 ) bidRatio_EURCHF_Long = ( EURCHF_bid_Long - EURCHF_low_Long ) / pipRange_EURCHF_Long * 10000 * 100; 
   if( EnableEURGBP && pipRange_EURGBP_Long != 0 ) bidRatio_EURGBP_Long = ( EURGBP_bid_Long - EURGBP_low_Long ) / pipRange_EURGBP_Long * 10000 * 100;
   if( EnableEURJPY && pipRange_EURJPY_Long != 0 ) bidRatio_EURJPY_Long = ( EURJPY_bid_Long - EURJPY_low_Long ) / pipRange_EURJPY_Long * 100 * 100;
   if( EnableEURNZD && pipRange_EURNZD_Long != 0 ) bidRatio_EURNZD_Long = ( EURNZD_bid_Long - EURNZD_low_Long ) / pipRange_EURNZD_Long * 10000 * 100;
   if( EnableEURUSD && pipRange_EURUSD_Long != 0 ) bidRatio_EURUSD_Long = ( EURUSD_bid_Long - EURUSD_low_Long ) / pipRange_EURUSD_Long * 10000 * 100;
   if( EnableGBPAUD && pipRange_GBPAUD_Long != 0 ) bidRatio_GBPAUD_Long = ( GBPAUD_bid_Long - GBPAUD_low_Long ) / pipRange_GBPAUD_Long * 10000 * 100;
   if( EnableGBPCAD && pipRange_GBPCAD_Long != 0 ) bidRatio_GBPCAD_Long = ( GBPCAD_bid_Long - GBPCAD_low_Long ) / pipRange_GBPCAD_Long * 10000 * 100;
   if( EnableGBPCHF && pipRange_GBPCHF_Long != 0 ) bidRatio_GBPCHF_Long = ( GBPCHF_bid_Long - GBPCHF_low_Long ) / pipRange_GBPCHF_Long * 10000 * 100;
   if( EnableGBPJPY && pipRange_GBPJPY_Long != 0 ) bidRatio_GBPJPY_Long = ( GBPJPY_bid_Long - GBPJPY_low_Long ) / pipRange_GBPJPY_Long * 100 * 100;
   if( EnableGBPNZD && pipRange_GBPNZD_Long != 0 ) bidRatio_GBPNZD_Long = ( GBPNZD_bid_Long - GBPNZD_low_Long ) / pipRange_GBPNZD_Long * 10000 * 100;
   if( EnableGBPUSD && pipRange_GBPUSD_Long != 0 ) bidRatio_GBPUSD_Long = ( GBPUSD_bid_Long - GBPUSD_low_Long ) / pipRange_GBPUSD_Long * 10000 * 100;
   if( EnableNZDCAD && pipRange_NZDCAD_Long != 0 ) bidRatio_NZDCAD_Long = ( NZDCAD_bid_Long - NZDCAD_low_Long ) / pipRange_NZDCAD_Long * 10000 * 100;
   if( EnableNZDCHF && pipRange_NZDCHF_Long != 0 ) bidRatio_NZDCHF_Long = ( NZDCHF_bid_Long - NZDCHF_low_Long ) / pipRange_NZDCHF_Long * 10000 * 100;
   if( EnableNZDJPY && pipRange_NZDJPY_Long != 0 ) bidRatio_NZDJPY_Long = ( NZDJPY_bid_Long - NZDJPY_low_Long ) / pipRange_NZDJPY_Long * 100 * 100;
   if( EnableNZDUSD && pipRange_NZDUSD_Long != 0 ) bidRatio_NZDUSD_Long = ( NZDUSD_bid_Long - NZDUSD_low_Long ) / pipRange_NZDUSD_Long * 10000 * 100;
   if( EnableUSDCAD && pipRange_USDCAD_Long != 0 ) bidRatio_USDCAD_Long = ( USDCAD_bid_Long - USDCAD_low_Long ) / pipRange_USDCAD_Long * 10000 * 100;
   if( EnableUSDCHF && pipRange_USDCHF_Long != 0 ) bidRatio_USDCHF_Long = ( USDCHF_bid_Long - USDCHF_low_Long ) / pipRange_USDCHF_Long * 10000 * 100;
   if( EnableUSDJPY && pipRange_USDJPY_Long != 0 ) bidRatio_USDJPY_Long = ( USDJPY_bid_Long - USDJPY_low_Long ) / pipRange_USDJPY_Long * 100 * 100;
}
 
void calculateRelativeStrength(){
   if( EnableAUDCAD ){
      relative_AUDCAD_AUD = lookup( bidRatio_AUDCAD );
      relative_AUDCAD_CAD = 9 - relative_AUDCAD_AUD; 
   }

   if( EnableAUDCHF ){
      relative_AUDCHF_AUD = lookup( bidRatio_AUDCHF );
      relative_AUDCHF_CHF = 9 - relative_AUDCHF_AUD;
   }

   if( EnableAUDJPY ){
      relative_AUDJPY_AUD = lookup( bidRatio_AUDJPY );
      relative_AUDJPY_JPY = 9 - relative_AUDJPY_AUD; 
   }
 
   if( EnableAUDNZD ){
      relative_AUDNZD_AUD = lookup( bidRatio_AUDNZD );
      relative_AUDNZD_NZD = 9 - relative_AUDNZD_AUD;
   }

   if( EnableAUDUSD ){
      relative_AUDUSD_AUD = lookup( bidRatio_AUDUSD );
      relative_AUDUSD_USD = 9 - relative_AUDUSD_AUD ; 
   }

   if( EnableCADCHF ){
      relative_CADCHF_CAD = lookup( bidRatio_CADCHF );
      relative_CADCHF_CHF = 9 - relative_CADCHF_CAD; 
   }
   
   if( EnableCADJPY ){
      relative_CADJPY_CAD = lookup( bidRatio_CADJPY );
      relative_CADJPY_JPY = 9 - relative_CADJPY_CAD;
   }

   if( EnableCHFJPY ){
      relative_CHFJPY_CHF = lookup( bidRatio_CHFJPY );
      relative_CHFJPY_JPY = 9 - relative_CHFJPY_CHF;
   }

   if( EnableEURAUD ){
      relative_EURAUD_EUR = lookup( bidRatio_EURAUD );
      relative_EURAUD_AUD = 9 - relative_EURAUD_EUR;
   }
   
   if( EnableEURCAD ){
      relative_EURCAD_EUR = lookup( bidRatio_EURCAD );
      relative_EURCAD_CAD = 9 - relative_EURCAD_EUR;
   }

   if( EnableEURCHF ){
      relative_EURCHF_EUR = lookup( bidRatio_EURCHF ); 
      relative_EURCHF_CHF = 9 - relative_EURCHF_EUR; 
   }

   if( EnableEURGBP ){
      relative_EURGBP_EUR = lookup( bidRatio_EURGBP );
      relative_EURGBP_GBP = 9 - relative_EURGBP_EUR;
   }

   if( EnableEURJPY ){
      relative_EURJPY_EUR = lookup( bidRatio_EURJPY );
      relative_EURJPY_JPY = 9 - relative_EURJPY_EUR;
   }

   if( EnableEURNZD ){
      relative_EURNZD_EUR = lookup( bidRatio_EURNZD );
      relative_EURNZD_NZD = 9 - relative_EURNZD_EUR;
   }

   if( EnableEURUSD ){
      relative_EURUSD_EUR = lookup( bidRatio_EURUSD );
      relative_EURUSD_USD = 9 - relative_EURUSD_EUR;
   }

   if( EnableGBPAUD ){
      relative_GBPAUD_GBP = lookup( bidRatio_GBPAUD );
      relative_GBPAUD_AUD = 9 - relative_GBPAUD_GBP;
   }

   if( EnableGBPCAD ){
      relative_GBPCAD_GBP = lookup( bidRatio_GBPCAD );
      relative_GBPCAD_CAD = 9 - relative_GBPCAD_GBP;
   }

   if( EnableGBPCHF ){
      relative_GBPCHF_GBP = lookup( bidRatio_GBPCHF );
      relative_GBPCHF_CHF = 9 - relative_GBPCHF_GBP;
   }

   if( EnableGBPJPY ){
      relative_GBPJPY_GBP = lookup( bidRatio_GBPJPY );
      relative_GBPJPY_JPY = 9 - relative_GBPJPY_GBP;
   }

   if( EnableGBPNZD ){
      relative_GBPNZD_GBP = lookup( bidRatio_GBPNZD );
      relative_GBPNZD_NZD = 9 - relative_GBPNZD_GBP;
   }

   if( EnableGBPUSD ){
      relative_GBPUSD_GBP = lookup( bidRatio_GBPUSD );
      relative_GBPUSD_USD = 9 - relative_GBPUSD_GBP;
   }

   if( EnableNZDCAD ){
      relative_NZDCAD_NZD = lookup( bidRatio_NZDCAD );
      relative_NZDCAD_CAD = 9 - relative_NZDCAD_NZD;
   }

   if( EnableNZDCAD ){
      relative_NZDCHF_NZD = lookup( bidRatio_NZDCHF );
      relative_NZDCHF_CHF = 9 - relative_NZDCHF_NZD;
   }
   
   if( EnableNZDJPY ){
      relative_NZDJPY_NZD = lookup( bidRatio_NZDJPY );
      relative_NZDJPY_JPY = 9 - relative_NZDJPY_NZD;
   }
   
   if( EnableNZDUSD ){
      relative_NZDUSD_NZD = lookup( bidRatio_NZDUSD );
      relative_NZDUSD_USD = 9 - relative_NZDUSD_NZD;
   }

   if( EnableUSDCAD ){
      relative_USDCAD_USD = lookup( bidRatio_USDCAD );
      relative_USDCAD_CAD = 9 - relative_USDCAD_USD;
   }

   if( EnableUSDCHF ){
      relative_USDCHF_USD = lookup( bidRatio_USDCHF );
      relative_USDCHF_CHF = 9 - relative_USDCHF_USD; 
   }
   
   if( EnableUSDJPY ){
      relative_USDJPY_USD = lookup( bidRatio_USDJPY );
      relative_USDJPY_JPY = 9 - relative_USDJPY_USD;
   }
}

void calculateRelativeStrength_Long(){
   if( EnableAUDCAD ){
      relative_AUDCAD_AUD_Long = lookup( bidRatio_AUDCAD_Long );
      relative_AUDCAD_CAD_Long = 9 - relative_AUDCAD_AUD_Long; 
   }

   if( EnableAUDCHF ){
      relative_AUDCHF_AUD_Long = lookup( bidRatio_AUDCHF_Long );
      relative_AUDCHF_CHF_Long = 9 - relative_AUDCHF_AUD_Long;
   }

   if( EnableAUDJPY ){
      relative_AUDJPY_AUD_Long = lookup( bidRatio_AUDJPY_Long );
      relative_AUDJPY_JPY_Long = 9 - relative_AUDJPY_AUD_Long; 
   }
 
   if( EnableAUDNZD ){
      relative_AUDNZD_AUD_Long = lookup( bidRatio_AUDNZD_Long );
      relative_AUDNZD_NZD_Long = 9 - relative_AUDNZD_AUD_Long;
   }

   if( EnableAUDUSD ){
      relative_AUDUSD_AUD_Long = lookup( bidRatio_AUDUSD_Long );
      relative_AUDUSD_USD_Long = 9 - relative_AUDUSD_AUD_Long ; 
   }

   if( EnableCADCHF ){
      relative_CADCHF_CAD_Long = lookup( bidRatio_CADCHF_Long );
      relative_CADCHF_CHF_Long = 9 - relative_CADCHF_CAD_Long; 
   }
   
   if( EnableCADJPY ){
      relative_CADJPY_CAD_Long = lookup( bidRatio_CADJPY_Long );
      relative_CADJPY_JPY_Long = 9 - relative_CADJPY_CAD_Long;
   }

   if( EnableCHFJPY ){
      relative_CHFJPY_CHF_Long = lookup( bidRatio_CHFJPY_Long );
      relative_CHFJPY_JPY_Long = 9 - relative_CHFJPY_CHF_Long;
   }

   if( EnableEURAUD ){
      relative_EURAUD_EUR_Long = lookup( bidRatio_EURAUD_Long );
      relative_EURAUD_AUD_Long = 9 - relative_EURAUD_EUR_Long;
   }
   
   if( EnableEURCAD ){
      relative_EURCAD_EUR_Long = lookup( bidRatio_EURCAD_Long );
      relative_EURCAD_CAD_Long = 9 - relative_EURCAD_EUR_Long;
   }

   if( EnableEURCHF ){
      relative_EURCHF_EUR_Long = lookup( bidRatio_EURCHF_Long ); 
      relative_EURCHF_CHF_Long = 9 - relative_EURCHF_EUR_Long; 
   }

   if( EnableEURGBP ){
      relative_EURGBP_EUR_Long = lookup( bidRatio_EURGBP_Long );
      relative_EURGBP_GBP_Long = 9 - relative_EURGBP_EUR_Long;
   }

   if( EnableEURJPY ){
      relative_EURJPY_EUR_Long = lookup( bidRatio_EURJPY_Long );
      relative_EURJPY_JPY_Long = 9 - relative_EURJPY_EUR_Long;
   }

   if( EnableEURNZD ){
      relative_EURNZD_EUR_Long = lookup( bidRatio_EURNZD_Long );
      relative_EURNZD_NZD_Long = 9 - relative_EURNZD_EUR_Long;
   }

   if( EnableEURUSD ){
      relative_EURUSD_EUR_Long = lookup( bidRatio_EURUSD_Long );
      relative_EURUSD_USD_Long = 9 - relative_EURUSD_EUR_Long;
   }

   if( EnableGBPAUD ){
      relative_GBPAUD_GBP_Long = lookup( bidRatio_GBPAUD_Long );
      relative_GBPAUD_AUD_Long = 9 - relative_GBPAUD_GBP_Long;
   }

   if( EnableGBPCAD ){
      relative_GBPCAD_GBP_Long = lookup( bidRatio_GBPCAD_Long );
      relative_GBPCAD_CAD_Long = 9 - relative_GBPCAD_GBP_Long;
   }

   if( EnableGBPCHF ){
      relative_GBPCHF_GBP_Long = lookup( bidRatio_GBPCHF_Long );
      relative_GBPCHF_CHF_Long = 9 - relative_GBPCHF_GBP_Long;
   }

   if( EnableGBPJPY ){
      relative_GBPJPY_GBP_Long = lookup( bidRatio_GBPJPY_Long );
      relative_GBPJPY_JPY_Long = 9 - relative_GBPJPY_GBP_Long;
   }

   if( EnableGBPNZD ){
      relative_GBPNZD_GBP_Long = lookup( bidRatio_GBPNZD_Long );
      relative_GBPNZD_NZD_Long = 9 - relative_GBPNZD_GBP_Long;
   }

   if( EnableGBPUSD ){
      relative_GBPUSD_GBP_Long = lookup( bidRatio_GBPUSD_Long );
      relative_GBPUSD_USD_Long = 9 - relative_GBPUSD_GBP_Long;
   }

   if( EnableNZDCAD ){
      relative_NZDCAD_NZD_Long = lookup( bidRatio_NZDCAD_Long );
      relative_NZDCAD_CAD_Long = 9 - relative_NZDCAD_NZD_Long;
   }

   if( EnableNZDCAD ){
      relative_NZDCHF_NZD_Long = lookup( bidRatio_NZDCHF_Long );
      relative_NZDCHF_CHF_Long = 9 - relative_NZDCHF_NZD_Long;
   }
   
   if( EnableNZDJPY ){
      relative_NZDJPY_NZD_Long = lookup( bidRatio_NZDJPY_Long );
      relative_NZDJPY_JPY_Long = 9 - relative_NZDJPY_NZD_Long;
   }
   
   if( EnableNZDUSD ){
      relative_NZDUSD_NZD_Long = lookup( bidRatio_NZDUSD_Long );
      relative_NZDUSD_USD_Long = 9 - relative_NZDUSD_NZD_Long;
   }

   if( EnableUSDCAD ){
      relative_USDCAD_USD_Long = lookup( bidRatio_USDCAD_Long );
      relative_USDCAD_CAD_Long = 9 - relative_USDCAD_USD_Long;
   }

   if( EnableUSDCHF ){
      relative_USDCHF_USD_Long = lookup( bidRatio_USDCHF_Long );
      relative_USDCHF_CHF_Long = 9 - relative_USDCHF_USD_Long; 
   }
   
   if( EnableUSDJPY ){
      relative_USDJPY_USD_Long = lookup( bidRatio_USDJPY_Long );
      relative_USDJPY_JPY_Long = 9 - relative_USDJPY_USD_Long;
   }
}  

void calculateCurrencyStrength(){
   double currencyStrength_USD_total = 0;
   int count_USD = 0;
   if( EnableAUDUSD ){
      currencyStrength_USD_total = currencyStrength_USD_total + relative_AUDUSD_USD;
      count_USD++;
   }
   if( EnableEURUSD ){
      currencyStrength_USD_total = currencyStrength_USD_total + relative_EURUSD_USD;
      count_USD++;
   }
   if( EnableGBPUSD ){
      currencyStrength_USD_total = currencyStrength_USD_total + relative_GBPUSD_USD;
      count_USD++;
   }
   if( EnableNZDUSD ){
      currencyStrength_USD_total = currencyStrength_USD_total + relative_NZDUSD_USD;
      count_USD++;
   }
   if( EnableUSDCAD ){
      currencyStrength_USD_total = currencyStrength_USD_total + relative_USDCAD_USD;
      count_USD++; 
   } 
   if( EnableUSDCHF ){
      currencyStrength_USD_total = currencyStrength_USD_total + relative_USDCHF_USD;
      count_USD++;
   }
   if( EnableUSDJPY ){
      currencyStrength_USD_total = currencyStrength_USD_total + relative_USDJPY_USD;
      count_USD++;
   }
   if( count_USD > 0 ) currencyStrength_USD = currencyStrength_USD_total / count_USD;
   else currencyStrength_USD = 0; 
   
   double currencyStrength_EUR_total = 0;
   int count_EUR = 0;
   if( EnableEURAUD ){
      currencyStrength_EUR_total = currencyStrength_EUR_total + relative_EURAUD_EUR;
      count_EUR++;
   }
   if( EnableEURCAD ){
      currencyStrength_EUR_total = currencyStrength_EUR_total + relative_EURCAD_EUR;
      count_EUR++;
   }
   if( EnableEURCHF ){
      currencyStrength_EUR_total = currencyStrength_EUR_total + relative_EURCHF_EUR;
      count_EUR++;
   }
   if( EnableEURGBP ){
      currencyStrength_EUR_total = currencyStrength_EUR_total + relative_EURGBP_EUR;
      count_EUR++;
   }
   if( EnableEURJPY ){
      currencyStrength_EUR_total = currencyStrength_EUR_total + relative_EURJPY_EUR;
      count_EUR++;
   }
   if( EnableEURNZD ){
      currencyStrength_EUR_total = currencyStrength_EUR_total + relative_EURNZD_EUR;
      count_EUR++;
   }
   if( EnableEURUSD ){
      currencyStrength_EUR_total = currencyStrength_EUR_total + relative_EURUSD_EUR;
      count_EUR++;
   }
   if( count_EUR > 0 ) currencyStrength_EUR = currencyStrength_EUR_total / count_EUR;
   else currencyStrength_EUR = 0;
     
   double currencyStrength_GBP_total = 0;
   int count_GBP = 0;
   if( EnableEURGBP ){
      currencyStrength_GBP_total = currencyStrength_GBP_total + relative_EURGBP_GBP;
      count_GBP++;
   }
   if( EnableGBPAUD ){
      currencyStrength_GBP_total = currencyStrength_GBP_total + relative_GBPAUD_GBP;
      count_GBP++;
   }
   if( EnableGBPCAD ){
      currencyStrength_GBP_total = currencyStrength_GBP_total + relative_GBPCAD_GBP;
      count_GBP++;
   }
   if( EnableGBPCHF ){
      currencyStrength_GBP_total = currencyStrength_GBP_total + relative_GBPCHF_GBP;
      count_GBP++;
   }
   if( EnableGBPJPY ){
      currencyStrength_GBP_total = currencyStrength_GBP_total + relative_GBPJPY_GBP;
      count_GBP++;
   }
   if( EnableGBPNZD ){
      currencyStrength_GBP_total = currencyStrength_GBP_total + relative_GBPNZD_GBP;
      count_GBP++;
   }
   if( EnableGBPUSD ){
      currencyStrength_GBP_total = currencyStrength_GBP_total + relative_GBPUSD_GBP;
      count_GBP++;
   }
   if( count_GBP > 0 ) currencyStrength_GBP = currencyStrength_GBP_total / count_GBP;
   else currencyStrength_GBP = 0;
   
   double currencyStrength_CHF_total = 0;
   int count_CHF = 0;
   if( EnableAUDCHF ){
      currencyStrength_CHF_total = currencyStrength_CHF_total + relative_AUDCHF_CHF;
      count_CHF++;
   }
   if( EnableCADCHF ){
      currencyStrength_CHF_total = currencyStrength_CHF_total + relative_CADCHF_CHF;
      count_CHF++;
   }
   if( EnableEURCHF ){
      currencyStrength_CHF_total = currencyStrength_CHF_total + relative_EURCHF_CHF;
      count_CHF++;
   }
   if( EnableGBPCHF ){
      currencyStrength_CHF_total = currencyStrength_CHF_total + relative_GBPCHF_CHF;
      count_CHF++;
   }
   if( EnableNZDCHF ){
      currencyStrength_CHF_total = currencyStrength_CHF_total + relative_NZDCHF_CHF;
      count_CHF++;
   }
   if( EnableUSDCHF ){
      currencyStrength_CHF_total = currencyStrength_CHF_total + relative_USDCHF_CHF;
      count_CHF++;
   }
   if( EnableCHFJPY ){
      currencyStrength_CHF_total = currencyStrength_CHF_total + relative_CHFJPY_CHF;
      count_CHF++;
   }
   if( count_CHF > 0 ) currencyStrength_CHF = currencyStrength_CHF_total / count_CHF;
   else currencyStrength_CHF = 0;
   
   double currencyStrength_CAD_total = 0;
   int count_CAD = 0;
   if( EnableAUDCAD ){
      currencyStrength_CAD_total = currencyStrength_CAD_total + relative_AUDCAD_CAD;
      count_CAD++;
   }
   if( EnableCADCHF ){
      currencyStrength_CAD_total = currencyStrength_CAD_total + relative_CADCHF_CAD;
      count_CAD++;
   }
   if( EnableEURCAD ){
      currencyStrength_CAD_total = currencyStrength_CAD_total + relative_EURCAD_CAD;
      count_CAD++;
   }
   if( EnableGBPCAD ){
      currencyStrength_CAD_total = currencyStrength_CAD_total + relative_GBPCAD_CAD;
      count_CAD++;
   }
   if( EnableNZDCAD ){
      currencyStrength_CAD_total = currencyStrength_CAD_total + relative_NZDCAD_CAD;
      count_CAD++;
   }
   if( EnableUSDCAD ){
      currencyStrength_CAD_total = currencyStrength_CAD_total + relative_USDCAD_CAD;
      count_CAD++;
   }
   if( EnableCADJPY ){
      currencyStrength_CAD_total = currencyStrength_CAD_total + relative_CADJPY_CAD;
      count_CAD++;
   }
   if( count_CAD > 0 ) currencyStrength_CAD = currencyStrength_CAD_total / count_CAD;
   else currencyStrength_CAD = 0;
   
   double currencyStrength_AUD_total = 0;
   int count_AUD = 0;
   if( EnableAUDCAD ){
      currencyStrength_AUD_total = currencyStrength_AUD_total + relative_AUDCAD_AUD;
      count_AUD++;
   }
   if( EnableAUDCHF ){
      currencyStrength_AUD_total = currencyStrength_AUD_total + relative_AUDCHF_AUD;
      count_AUD++;
   }
   if( EnableAUDJPY ){
      currencyStrength_AUD_total = currencyStrength_AUD_total + relative_AUDJPY_AUD;
      count_AUD++;
   }
   if( EnableAUDUSD ){
      currencyStrength_AUD_total = currencyStrength_AUD_total + relative_AUDUSD_AUD;
      count_AUD++;
   }
   if( EnableEURAUD ){
      currencyStrength_AUD_total = currencyStrength_AUD_total + relative_EURAUD_AUD;
      count_AUD++;
   }
   if( EnableGBPAUD ){
      currencyStrength_AUD_total = currencyStrength_AUD_total + relative_GBPAUD_AUD;
      count_AUD++;
   }
   if( EnableAUDNZD ){
      currencyStrength_AUD_total = currencyStrength_AUD_total + relative_AUDNZD_AUD;
      count_AUD++;
   }
   if( count_AUD > 0 ) currencyStrength_AUD = currencyStrength_AUD_total / count_AUD;
   else currencyStrength_AUD = 0;
   
   double currencyStrength_NZD_total = 0;
   int count_NZD = 0;
   if( EnableAUDCAD ){
      currencyStrength_NZD_total = currencyStrength_NZD_total + relative_AUDNZD_NZD;
      count_NZD++;
   }
   if( EnableGBPNZD ){
      currencyStrength_NZD_total = currencyStrength_NZD_total + relative_GBPNZD_NZD;
      count_NZD++;
   }
   if( EnableNZDCAD ){
      currencyStrength_NZD_total = currencyStrength_NZD_total + relative_NZDCAD_NZD;
      count_NZD++;
   }
   if( EnableNZDCHF ){
      currencyStrength_NZD_total = currencyStrength_NZD_total + relative_NZDCHF_NZD;
      count_NZD++;
   }
   if( EnableNZDJPY ){
      currencyStrength_NZD_total = currencyStrength_NZD_total + relative_NZDJPY_NZD;
      count_NZD++;
   }
   if( EnableNZDUSD ){
      currencyStrength_NZD_total = currencyStrength_NZD_total + relative_NZDUSD_NZD;
      count_NZD++;
   }
   if( EnableEURNZD ){
      currencyStrength_NZD_total = currencyStrength_NZD_total + relative_EURNZD_NZD;
      count_NZD++;
   }
   if( count_NZD > 0 ) currencyStrength_NZD = currencyStrength_NZD_total / count_NZD;
   else currencyStrength_NZD = 0;
   
   double currencyStrength_JPY_total = 0;
   int count_JPY = 0;
   if( EnableAUDJPY ){
      currencyStrength_JPY_total = currencyStrength_JPY_total + relative_AUDJPY_JPY;
      count_JPY++;
   }
   if( EnableCADJPY ){
      currencyStrength_JPY_total = currencyStrength_JPY_total + relative_CADJPY_JPY;
      count_JPY++;
   }
   if( EnableCHFJPY ){
      currencyStrength_JPY_total = currencyStrength_JPY_total + relative_CHFJPY_JPY;
      count_JPY++;
   }
   if( EnableEURJPY ){
      currencyStrength_JPY_total = currencyStrength_JPY_total + relative_EURJPY_JPY;
      count_JPY++;
   }
   if( EnableGBPJPY ){
      currencyStrength_JPY_total = currencyStrength_JPY_total + relative_GBPJPY_JPY;
      count_JPY++;
   }
   if( EnableNZDJPY ){
      currencyStrength_JPY_total = currencyStrength_JPY_total + relative_NZDJPY_JPY;
      count_JPY++;
   }
   if( EnableUSDJPY ){
      currencyStrength_JPY_total = currencyStrength_JPY_total + relative_USDJPY_JPY;
      count_JPY++;
   }
   if( count_JPY > 0 ) currencyStrength_JPY = currencyStrength_JPY_total / count_JPY;
   else currencyStrength_JPY = 0;
   
} 

void calculateCurrencyStrength_Long(){
   double currencyStrength_USD_total_Long = 0;
   int count_USD_Long = 0;
   if( EnableAUDUSD ){
      currencyStrength_USD_total_Long = currencyStrength_USD_total_Long + relative_AUDUSD_USD_Long;
      count_USD_Long++;
   }
   if( EnableEURUSD ){
      currencyStrength_USD_total_Long = currencyStrength_USD_total_Long + relative_EURUSD_USD_Long;
      count_USD_Long++;
   }
   if( EnableGBPUSD ){
      currencyStrength_USD_total_Long = currencyStrength_USD_total_Long + relative_GBPUSD_USD_Long;
      count_USD_Long++;
   }
   if( EnableNZDUSD ){
      currencyStrength_USD_total_Long = currencyStrength_USD_total_Long + relative_NZDUSD_USD_Long;
      count_USD_Long++;
   }
   if( EnableUSDCAD ){
      currencyStrength_USD_total_Long = currencyStrength_USD_total_Long + relative_USDCAD_USD_Long;
      count_USD_Long++; 
   } 
   if( EnableUSDCHF ){
      currencyStrength_USD_total_Long = currencyStrength_USD_total_Long + relative_USDCHF_USD_Long;
      count_USD_Long++;
   }
   if( EnableUSDJPY ){
      currencyStrength_USD_total_Long = currencyStrength_USD_total_Long + relative_USDJPY_USD_Long;
      count_USD_Long++;
   }
   if( count_USD_Long > 0 ) currencyStrength_USD_Long = currencyStrength_USD_total_Long / count_USD_Long;
   else currencyStrength_USD_Long = 0; 
   
   double currencyStrength_EUR_total_Long = 0;
   int count_EUR_Long = 0;
   if( EnableEURAUD ){
      currencyStrength_EUR_total_Long = currencyStrength_EUR_total_Long + relative_EURAUD_EUR_Long;
      count_EUR_Long++;
   }
   if( EnableEURCAD ){
      currencyStrength_EUR_total_Long = currencyStrength_EUR_total_Long + relative_EURCAD_EUR_Long;
      count_EUR_Long++;
   }
   if( EnableEURCHF ){
      currencyStrength_EUR_total_Long = currencyStrength_EUR_total_Long + relative_EURCHF_EUR_Long;
      count_EUR_Long++;
   }
   if( EnableEURGBP ){
      currencyStrength_EUR_total_Long = currencyStrength_EUR_total_Long + relative_EURGBP_EUR_Long;
      count_EUR_Long++;
   }
   if( EnableEURJPY ){
      currencyStrength_EUR_total_Long = currencyStrength_EUR_total_Long + relative_EURJPY_EUR_Long;
      count_EUR_Long++;
   }
   if( EnableEURNZD ){
      currencyStrength_EUR_total_Long = currencyStrength_EUR_total_Long + relative_EURNZD_EUR_Long;
      count_EUR_Long++;
   }
   if( EnableEURUSD ){
      currencyStrength_EUR_total_Long = currencyStrength_EUR_total_Long + relative_EURUSD_EUR_Long;
      count_EUR_Long++;
   }
   if( count_EUR_Long > 0 ) currencyStrength_EUR_Long = currencyStrength_EUR_total_Long / count_EUR_Long;
   else currencyStrength_EUR_Long = 0;
     
   double currencyStrength_GBP_total_Long = 0;
   int count_GBP_Long = 0;
   if( EnableEURGBP ){
      currencyStrength_GBP_total_Long = currencyStrength_GBP_total_Long + relative_EURGBP_GBP_Long;
      count_GBP_Long++;
   }
   if( EnableGBPAUD ){
      currencyStrength_GBP_total_Long = currencyStrength_GBP_total_Long + relative_GBPAUD_GBP_Long;
      count_GBP_Long++;
   }
   if( EnableGBPCAD ){
      currencyStrength_GBP_total_Long = currencyStrength_GBP_total_Long + relative_GBPCAD_GBP_Long;
      count_GBP_Long++;
   }
   if( EnableGBPCHF ){
      currencyStrength_GBP_total_Long = currencyStrength_GBP_total_Long + relative_GBPCHF_GBP_Long;
      count_GBP_Long++;
   }
   if( EnableGBPJPY ){
      currencyStrength_GBP_total_Long = currencyStrength_GBP_total_Long + relative_GBPJPY_GBP_Long;
      count_GBP_Long++;
   }
   if( EnableGBPNZD ){
      currencyStrength_GBP_total_Long = currencyStrength_GBP_total_Long + relative_GBPNZD_GBP_Long;
      count_GBP_Long++;
   }
   if( EnableGBPUSD ){
      currencyStrength_GBP_total_Long = currencyStrength_GBP_total_Long + relative_GBPUSD_GBP_Long;
      count_GBP_Long++;
   }
   if( count_GBP_Long > 0 ) currencyStrength_GBP_Long = currencyStrength_GBP_total_Long / count_GBP_Long;
   else currencyStrength_GBP_Long = 0;
   
   double currencyStrength_CHF_total_Long = 0;
   int count_CHF_Long = 0;
   if( EnableAUDCHF ){
      currencyStrength_CHF_total_Long = currencyStrength_CHF_total_Long + relative_AUDCHF_CHF_Long;
      count_CHF_Long++;
   }
   if( EnableCADCHF ){
      currencyStrength_CHF_total_Long = currencyStrength_CHF_total_Long + relative_CADCHF_CHF_Long;
      count_CHF_Long++;
   }
   if( EnableEURCHF ){
      currencyStrength_CHF_total_Long = currencyStrength_CHF_total_Long + relative_EURCHF_CHF_Long;
      count_CHF_Long++;
   }
   if( EnableGBPCHF ){
      currencyStrength_CHF_total_Long = currencyStrength_CHF_total_Long + relative_GBPCHF_CHF_Long;
      count_CHF_Long++;
   }
   if( EnableNZDCHF ){
      currencyStrength_CHF_total_Long = currencyStrength_CHF_total_Long + relative_NZDCHF_CHF_Long;
      count_CHF_Long++;
   }
   if( EnableUSDCHF ){
      currencyStrength_CHF_total_Long = currencyStrength_CHF_total_Long + relative_USDCHF_CHF_Long;
      count_CHF_Long++;
   }
   if( EnableCHFJPY ){
      currencyStrength_CHF_total_Long = currencyStrength_CHF_total_Long + relative_CHFJPY_CHF_Long;
      count_CHF_Long++;
   }
   if( count_CHF_Long > 0 ) currencyStrength_CHF_Long = currencyStrength_CHF_total_Long / count_CHF_Long;
   else currencyStrength_CHF_Long = 0;
   
   double currencyStrength_CAD_total_Long = 0;
   int count_CAD_Long = 0;
   if( EnableAUDCAD ){
      currencyStrength_CAD_total_Long = currencyStrength_CAD_total_Long + relative_AUDCAD_CAD_Long;
      count_CAD_Long++;
   }
   if( EnableCADCHF ){
      currencyStrength_CAD_total_Long = currencyStrength_CAD_total_Long+ relative_CADCHF_CAD_Long;
      count_CAD_Long++;
   }
   if( EnableEURCAD ){
      currencyStrength_CAD_total_Long = currencyStrength_CAD_total_Long + relative_EURCAD_CAD_Long;
      count_CAD_Long++;
   }
   if( EnableGBPCAD ){
      currencyStrength_CAD_total_Long = currencyStrength_CAD_total_Long + relative_GBPCAD_CAD_Long;
      count_CAD_Long++;
   }
   if( EnableNZDCAD ){
      currencyStrength_CAD_total_Long = currencyStrength_CAD_total_Long + relative_NZDCAD_CAD_Long;
      count_CAD_Long++;
   }
   if( EnableUSDCAD ){
      currencyStrength_CAD_total_Long = currencyStrength_CAD_total_Long + relative_USDCAD_CAD_Long;
      count_CAD_Long++;
   }
   if( EnableCADJPY ){
      currencyStrength_CAD_total_Long = currencyStrength_CAD_total_Long + relative_CADJPY_CAD_Long;
      count_CAD_Long++;
   }
   if( count_CAD_Long > 0 ) currencyStrength_CAD_Long = currencyStrength_CAD_total_Long / count_CAD_Long;
   else currencyStrength_CAD_Long = 0;
   
   double currencyStrength_AUD_total_Long = 0;
   int count_AUD_Long = 0;
   if( EnableAUDCAD ){
      currencyStrength_AUD_total_Long = currencyStrength_AUD_total_Long + relative_AUDCAD_AUD_Long;
      count_AUD_Long++;
   }
   if( EnableAUDCHF ){
      currencyStrength_AUD_total_Long = currencyStrength_AUD_total_Long + relative_AUDCHF_AUD_Long;
      count_AUD_Long++;
   }
   if( EnableAUDJPY ){
      currencyStrength_AUD_total_Long = currencyStrength_AUD_total_Long + relative_AUDJPY_AUD_Long;
      count_AUD_Long++;
   }
   if( EnableAUDUSD ){
      currencyStrength_AUD_total_Long = currencyStrength_AUD_total_Long + relative_AUDUSD_AUD_Long;
      count_AUD_Long++;
   }
   if( EnableEURAUD ){
      currencyStrength_AUD_total_Long = currencyStrength_AUD_total_Long + relative_EURAUD_AUD_Long;
      count_AUD_Long++;
   }
   if( EnableGBPAUD ){
      currencyStrength_AUD_total_Long = currencyStrength_AUD_total_Long + relative_GBPAUD_AUD_Long;
      count_AUD_Long++;
   }
   if( EnableAUDNZD ){
      currencyStrength_AUD_total_Long = currencyStrength_AUD_total_Long + relative_AUDNZD_AUD_Long;
      count_AUD_Long++;
   }
   if( count_AUD_Long > 0 ) currencyStrength_AUD_Long = currencyStrength_AUD_total_Long / count_AUD_Long;
   else currencyStrength_AUD_Long = 0;
   
   double currencyStrength_NZD_total_Long = 0;
   int count_NZD_Long = 0;
   if( EnableAUDCAD ){
      currencyStrength_NZD_total_Long = currencyStrength_NZD_total_Long + relative_AUDNZD_NZD_Long;
      count_NZD_Long++;
   }
   if( EnableGBPNZD ){
      currencyStrength_NZD_total_Long = currencyStrength_NZD_total_Long + relative_GBPNZD_NZD_Long;
      count_NZD_Long++;
   }
   if( EnableNZDCAD ){
      currencyStrength_NZD_total_Long = currencyStrength_NZD_total_Long + relative_NZDCAD_NZD_Long;
      count_NZD_Long++;
   }
   if( EnableNZDCHF ){
      currencyStrength_NZD_total_Long = currencyStrength_NZD_total_Long + relative_NZDCHF_NZD_Long;
      count_NZD_Long++;
   }
   if( EnableNZDJPY ){
      currencyStrength_NZD_total_Long = currencyStrength_NZD_total_Long + relative_NZDJPY_NZD_Long;
      count_NZD_Long++;
   }
   if( EnableNZDUSD ){
      currencyStrength_NZD_total_Long = currencyStrength_NZD_total_Long + relative_NZDUSD_NZD_Long;
      count_NZD_Long++;
   }
   if( EnableEURNZD ){
      currencyStrength_NZD_total_Long = currencyStrength_NZD_total_Long + relative_EURNZD_NZD_Long;
      count_NZD_Long++;
   }
   if( count_NZD_Long > 0 ) currencyStrength_NZD_Long = currencyStrength_NZD_total_Long / count_NZD_Long;
   else currencyStrength_NZD_Long = 0;
   
   double currencyStrength_JPY_total_Long = 0;
   int count_JPY_Long = 0;
   if( EnableAUDJPY ){
      currencyStrength_JPY_total_Long = currencyStrength_JPY_total_Long + relative_AUDJPY_JPY_Long;
      count_JPY_Long++;
   }
   if( EnableCADJPY ){
      currencyStrength_JPY_total_Long = currencyStrength_JPY_total_Long + relative_CADJPY_JPY_Long;
      count_JPY_Long++;
   }
   if( EnableCHFJPY ){
      currencyStrength_JPY_total_Long = currencyStrength_JPY_total_Long + relative_CHFJPY_JPY_Long;
      count_JPY_Long++;
   }
   if( EnableEURJPY ){
      currencyStrength_JPY_total_Long = currencyStrength_JPY_total_Long + relative_EURJPY_JPY_Long;
      count_JPY_Long++;
   }
   if( EnableGBPJPY ){
      currencyStrength_JPY_total_Long = currencyStrength_JPY_total_Long + relative_GBPJPY_JPY_Long;
      count_JPY_Long++;
   }
   if( EnableNZDJPY ){
      currencyStrength_JPY_total_Long = currencyStrength_JPY_total_Long + relative_NZDJPY_JPY_Long;
      count_JPY_Long++;
   }
   if( EnableUSDJPY ){
      currencyStrength_JPY_total_Long = currencyStrength_JPY_total_Long + relative_USDJPY_JPY_Long;
      count_JPY_Long++;
   }
   if( count_JPY_Long > 0 ) currencyStrength_JPY_Long = currencyStrength_JPY_total_Long / count_JPY_Long;
   else currencyStrength_JPY_Long = 0;
   
} 

double firstCurrencyValue( string symbol ){
   string firstCurrency = StringSubstr( symbol, 0, 3 );   
   double firstCurrencyValue = 0; 
   if( firstCurrency == "USD" ){
      firstCurrencyValue = currencyStrength_USD;
   } else if( firstCurrency == "EUR" ){
      firstCurrencyValue = currencyStrength_EUR;
   } else if( firstCurrency == "GBP" ){
      firstCurrencyValue = currencyStrength_GBP;
   } else if( firstCurrency == "CHF" ){
      firstCurrencyValue = currencyStrength_CHF;
   } else if( firstCurrency == "CAD" ){
      firstCurrencyValue = currencyStrength_CAD;
   } else if( firstCurrency == "AUD" ){
      firstCurrencyValue = currencyStrength_AUD;
   } else if( firstCurrency == "NZD" ){
      firstCurrencyValue = currencyStrength_NZD;
   } else if( firstCurrency == "JPY" ){
      firstCurrencyValue = currencyStrength_JPY;
   }
   return ( firstCurrencyValue );
}

double secondCurrencyValue( string symbol ){ 
   string secondCurrency = StringSubstr( symbol, 3, 3 );   
   double secondCurrencyValue = 0; 
   if( secondCurrency == "USD" ){
      secondCurrencyValue = currencyStrength_USD;
   } else if( secondCurrency == "EUR" ){
      secondCurrencyValue = currencyStrength_EUR;
   } else if( secondCurrency == "GBP" ){
      secondCurrencyValue = currencyStrength_GBP;
   } else if( secondCurrency == "CHF" ){
      secondCurrencyValue = currencyStrength_CHF;
   } else if( secondCurrency == "CAD" ){
      secondCurrencyValue = currencyStrength_CAD;
   } else if( secondCurrency == "AUD" ){
      secondCurrencyValue = currencyStrength_AUD;
   } else if( secondCurrency == "NZD" ){
      secondCurrencyValue = currencyStrength_NZD;
   } else if( secondCurrency == "JPY" ){
      secondCurrencyValue = currencyStrength_JPY;
   } 
   return ( secondCurrencyValue );
}

double firstCurrencyValue_Long( string symbol ){
   string firstCurrency = StringSubstr( symbol, 0, 3 );   
   double firstCurrencyValue = 0; 
   if( firstCurrency == "USD" ){
      firstCurrencyValue = currencyStrength_USD_Long;
   } else if( firstCurrency == "EUR" ){
      firstCurrencyValue = currencyStrength_EUR_Long;
   } else if( firstCurrency == "GBP" ){
      firstCurrencyValue = currencyStrength_GBP_Long;
   } else if( firstCurrency == "CHF" ){
      firstCurrencyValue = currencyStrength_CHF_Long;
   } else if( firstCurrency == "CAD" ){
      firstCurrencyValue = currencyStrength_CAD_Long;
   } else if( firstCurrency == "AUD" ){
      firstCurrencyValue = currencyStrength_AUD_Long;
   } else if( firstCurrency == "NZD" ){
      firstCurrencyValue = currencyStrength_NZD_Long;
   } else if( firstCurrency == "JPY" ){
      firstCurrencyValue = currencyStrength_JPY_Long;
   }
   return ( firstCurrencyValue );
}

double secondCurrencyValue_Long( string symbol ){ 
   string secondCurrency = StringSubstr( symbol, 3, 3 );   
   double secondCurrencyValue = 0; 
   if( secondCurrency == "USD" ){
      secondCurrencyValue = currencyStrength_USD_Long;
   } else if( secondCurrency == "EUR" ){
      secondCurrencyValue = currencyStrength_EUR_Long;
   } else if( secondCurrency == "GBP" ){
      secondCurrencyValue = currencyStrength_GBP_Long;
   } else if( secondCurrency == "CHF" ){
      secondCurrencyValue = currencyStrength_CHF_Long;
   } else if( secondCurrency == "CAD" ){
      secondCurrencyValue = currencyStrength_CAD_Long;
   } else if( secondCurrency == "AUD" ){
      secondCurrencyValue = currencyStrength_AUD_Long;
   } else if( secondCurrency == "NZD" ){
      secondCurrencyValue = currencyStrength_NZD_Long;
   } else if( secondCurrency == "JPY" ){
      secondCurrencyValue = currencyStrength_JPY_Long;
   } 
   return ( secondCurrencyValue );
}

void sortCurrencyStrengthArrayASC(){  
   currencyStrengthArrayASC[0][0] = currencyStrength_USD;
   currencyStrengthArrayASC[1][0] = currencyStrength_EUR;
   currencyStrengthArrayASC[2][0] = currencyStrength_GBP;
   currencyStrengthArrayASC[3][0] = currencyStrength_CHF;
   currencyStrengthArrayASC[4][0] = currencyStrength_CAD;
   currencyStrengthArrayASC[5][0] = currencyStrength_AUD;
   currencyStrengthArrayASC[6][0] = currencyStrength_NZD;
   currencyStrengthArrayASC[7][0] = currencyStrength_JPY;
   
   currencyStrengthArrayASC[0][1] = 0;
   currencyStrengthArrayASC[1][1] = 1;
   currencyStrengthArrayASC[2][1] = 2;
   currencyStrengthArrayASC[3][1] = 3;
   currencyStrengthArrayASC[4][1] = 4;
   currencyStrengthArrayASC[5][1] = 5;
   currencyStrengthArrayASC[6][1] = 6;
   currencyStrengthArrayASC[7][1] = 7; 
   ArraySort( currencyStrengthArrayASC, WHOLE_ARRAY, 0, MODE_ASCEND );
}

void sortCurrencyStrengthArrayDESC(){  
   currencyStrengthArrayDESC[0][0] = currencyStrength_USD;
   currencyStrengthArrayDESC[1][0] = currencyStrength_EUR;
   currencyStrengthArrayDESC[2][0] = currencyStrength_GBP;
   currencyStrengthArrayDESC[3][0] = currencyStrength_CHF;
   currencyStrengthArrayDESC[4][0] = currencyStrength_CAD;
   currencyStrengthArrayDESC[5][0] = currencyStrength_AUD;
   currencyStrengthArrayDESC[6][0] = currencyStrength_NZD;
   currencyStrengthArrayDESC[7][0] = currencyStrength_JPY;
   
   currencyStrengthArrayDESC[0][1] = 0;
   currencyStrengthArrayDESC[1][1] = 1;
   currencyStrengthArrayDESC[2][1] = 2;
   currencyStrengthArrayDESC[3][1] = 3;
   currencyStrengthArrayDESC[4][1] = 4;
   currencyStrengthArrayDESC[5][1] = 5;
   currencyStrengthArrayDESC[6][1] = 6;
   currencyStrengthArrayDESC[7][1] = 7;
   ArraySort( currencyStrengthArrayDESC, WHOLE_ARRAY, 0, MODE_DESCEND );
}

void sortCurrencyStrengthArrayASC_Long(){  
   currencyStrengthArrayASC_Long[0][0] = currencyStrength_USD_Long;
   currencyStrengthArrayASC_Long[1][0] = currencyStrength_EUR_Long;
   currencyStrengthArrayASC_Long[2][0] = currencyStrength_GBP_Long;
   currencyStrengthArrayASC_Long[3][0] = currencyStrength_CHF_Long;
   currencyStrengthArrayASC_Long[4][0] = currencyStrength_CAD_Long;
   currencyStrengthArrayASC_Long[5][0] = currencyStrength_AUD_Long;
   currencyStrengthArrayASC_Long[6][0] = currencyStrength_NZD_Long;
   currencyStrengthArrayASC_Long[7][0] = currencyStrength_JPY_Long;
   
   currencyStrengthArrayASC_Long[0][1] = 0;
   currencyStrengthArrayASC_Long[1][1] = 1;
   currencyStrengthArrayASC_Long[2][1] = 2;
   currencyStrengthArrayASC_Long[3][1] = 3;
   currencyStrengthArrayASC_Long[4][1] = 4;
   currencyStrengthArrayASC_Long[5][1] = 5;
   currencyStrengthArrayASC_Long[6][1] = 6;
   currencyStrengthArrayASC_Long[7][1] = 7;
   ArraySort( currencyStrengthArrayASC_Long, WHOLE_ARRAY, 0, MODE_ASCEND );
}

void sortCurrencyStrengthArrayDESC_Long(){ 
 
   currencyStrengthArrayDESC_Long[0][0] = currencyStrength_USD_Long;
   currencyStrengthArrayDESC_Long[1][0] = currencyStrength_EUR_Long;
   currencyStrengthArrayDESC_Long[2][0] = currencyStrength_GBP_Long;
   currencyStrengthArrayDESC_Long[3][0] = currencyStrength_CHF_Long;
   currencyStrengthArrayDESC_Long[4][0] = currencyStrength_CAD_Long; 
   currencyStrengthArrayDESC_Long[5][0] = currencyStrength_AUD_Long;
   currencyStrengthArrayDESC_Long[6][0] = currencyStrength_NZD_Long;
   currencyStrengthArrayDESC_Long[7][0] = currencyStrength_JPY_Long;
   
   currencyStrengthArrayDESC_Long[0][1] = 0;
   currencyStrengthArrayDESC_Long[1][1] = 1;
   currencyStrengthArrayDESC_Long[2][1] = 2;
   currencyStrengthArrayDESC_Long[3][1] = 3;
   currencyStrengthArrayDESC_Long[4][1] = 4;
   currencyStrengthArrayDESC_Long[5][1] = 5;
   currencyStrengthArrayDESC_Long[6][1] = 6;
   currencyStrengthArrayDESC_Long[7][1] = 7;
   
   ArraySort( currencyStrengthArrayDESC_Long, WHOLE_ARRAY, 0, MODE_DESCEND );
   
}

double lookup( double value ){
   double lookup[10] = { 0, 3, 10, 25, 40, 50, 60, 75, 90, 97 };
   double lookupResult[10] = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
   double res = 0;
   for( int i = 0; i < 9; i++ ){
      if( value >= lookup[i] && value < lookup[i+1] ){
         res = lookupResult[i];
         break;
      } else {
         res = lookupResult[9];
      }
   }
   return ( res );
}      

int symTotal( string symbol ){
   int total = 0;
   for( int s = 0; s < generalArrayCount; s++ ) { 
      if( symbol == sym[s] ) {
         total = totalTradesArray[s];
         break;
      }
   }  
   return ( total );
}

int symI( string symbol ){
   int si = -1;
   for( int s = 0; s < generalArrayCount; s++ ) { 
      if( symbol == sym[s] ) {
         si = s;
         break;
      }
   }
   return ( si );
}

double getPipPoint( string symbol ){
   double pp = 0.0001;
   for( int s = 0; s < generalArrayCount; s++ ) {
      if( symbol == sym[s] ){
         pp = pipPoint[s];
         break;
      }
   }
   return ( pp );
} 

int getDigits( string symbol ){
   int digitsSym = 5;
   for( int s = 0; s < generalArrayCount; s++ ) {
      if( symbol == sym[s] ){
         digitsSym = digitsArray[s];
         break;
      }
   }
   return ( digitsSym );
}

void setPipPoint(){
   digits = MarketInfo( Symbol(), MODE_DIGITS );
   if( digits == 3 || digits == 2 ) pipPoints = 0.01;
   else if( digits == 5 || digits == 4 ) pipPoints = 0.0001;
}  

double marginCalculate( string symbol, double volume ){ 
   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
}  

void lotSize(){  
   spread = ( Ask - Bid ) / pipPoints; 
   slippage = NormalizeDouble( ( spread / pipPoints ) * DynamicSlippage, 1 );   
   if( AccountBalance() > highest ) highest = AccountBalance(); 
   if( AccountBalance() > 0 ) recoverFactor = highest / AccountBalance(); 
   else recoverFactor = 1;
   recoverFactor = MathPow( recoverFactor, RecoverPower );
   lotSize = NormalizeDouble( AccountBalance() / BaseLotPerDollar * BaseLotSize * recoverFactor, 2 );
   if( LotPrecision == 2 && lotSize < 0.01 ) lotSize = 0.01;
   else if( LotPrecision == 1 && lotSize < 0.1 ) lotSize = 0.1;  
   if( lotSize > maxLots ) lotSize = maxLots; 
   if( lotSize < minLots ) lotSize = minLots;
}      

void manageNews(){ 
   if( totalSecondsToNews < BeforeNewsSeconds && totalProfit > 0 ){
       for( int s = 0; s < generalArrayCount; s++ ) { 
          if( ( totalLossArray[s] + totalChargesArray[s] ) != 0 
               && ( totalLossArray[s] + totalChargesArray[s] ) < 0 
               && totalTradesArray[s] > 0
               && totalProfitArray[s] / MathAbs( totalLossArray[s] + totalChargesArray[s] ) > 1 ){  
                  Print( "Attempting to close news ", sym[s] );
                  closeAll( sym[s] ); 
          }
      }
   }
} 

void manageProfits(){
   for( int s = 0; s < generalArrayCount; s++ ) {    
      double firstCurrencyValueSym_Long = firstCurrencyValue_Long( sym[s] );
      double secondCurrencyValueSym_Long = secondCurrencyValue_Long( sym[s] ); 
      if( totalAccountTrades <= StarterTrades || avgOpenTime > LongTimeSeconds ){
         for( int i = 1; i <= OrdersTotal(); i++ ) { 
            if( OrderSelect( i - 1, SELECT_BY_POS ) == true ) {  
               if( OrderMagicNumber() == MAGIC ){
                  double bid = ( double ) MarketInfo( OrderSymbol(), MODE_BID );
                  double ask = ( double ) MarketInfo( OrderSymbol(), MODE_ASK );
                   
                  
                  if( OrderSymbol() == sym[s] ){    
                     double minStopLevelPips = ( double ) MarketInfo( sym[s], MODE_STOPLEVEL ) / 10;  
                     int digitsSym = getDigits( OrderSymbol() ); 
                     double pipPointSym = getPipPoint( OrderSymbol() ); 
                     double high = NormalizeDouble( symbolHigh[s], digitsSym );
                     double low = NormalizeDouble( symbolLow[s], digitsSym );
                     double high_long = NormalizeDouble( symbolHigh_Long[s], digitsSym );
                     double low_long = NormalizeDouble( symbolLow_Long[s], digitsSym ); 
                     if( OrderType() == OP_BUY && firstCurrencyValueSym_Long < secondCurrencyValueSym_Long 
                        && ( int ) TimeCurrent() - ( int ) OrderOpenTime() > ProfitTimeSeconds 
                        && OrderProfit() > 0 && bid > OrderOpenPrice() + ( MinProfitATR * atrCurrent[s] * recoverFactor ) ){    
                        r = OrderClose( OrderTicket(), OrderLots(), ask, ( int ) slippage ); 
                        if( !r ) Print( "1923. Error in OrderModify. Error code=", GetLastError() );
                        else Print( "980. Closed for stop: ", OrderSymbol() );
                        break; 
                     } else if( OrderType() == OP_SELL && firstCurrencyValueSym_Long > secondCurrencyValueSym_Long 
                        && ( int ) TimeCurrent() - ( int ) OrderOpenTime() > ProfitTimeSeconds 
                        && OrderProfit() > 0 && ask < OrderOpenPrice() - ( MinProfitATR * atrCurrent[s] * recoverFactor ) ){   
                        r = OrderClose( OrderTicket(), OrderLots(), bid, ( int ) slippage ); 
                        if( !r ) Print( "9582. Error in OrderClose. Error code=", GetLastError() );
                        else Print( "972. Closed for stop: ", OrderSymbol() );
                        break; 
                     }  
                     if( OrderType() == OP_BUY && OrderTakeProfit() == 0 && totalTradesArray[s] <= StarterTrades && firstCurrencyValueSym_Long < secondCurrencyValueSym_Long ){    
                        if( OrderProfit() > 0 && bid > OrderOpenPrice() + ( StartProfitATR * atrCurrent[s] * recoverFactor ) ){
                           r = OrderClose( OrderTicket(), OrderLots(), bid, ( int ) slippage ); 
                           if( !r ) Print( "9809. Error in OrderClose. Error code=", GetLastError() );
                           else Print( "8709. Primary target: ", OrderSymbol() );
                           break;
                        }
                     } else if( OrderType() == OP_SELL && OrderTakeProfit() == 0 && totalTradesArray[s] <= StarterTrades && firstCurrencyValueSym_Long > secondCurrencyValueSym_Long ){ 
                        if( OrderProfit() > 0 && ask < OrderOpenPrice() - ( StartProfitATR * atrCurrent[s] * recoverFactor ) ){
                           r = OrderClose( OrderTicket(), OrderLots(), ask, ( int ) slippage ); 
                           if( !r ) Print( "149. Error in OrderClose. Error code=", GetLastError() );
                           else Print( "1254. Primary target: ", OrderSymbol() );
                           break;
                        } 
                     }    
                     if( OrderTakeProfit() == 0 ){
                        if( OrderType() == OP_BUY ){
                           if( bid < OrderOpenPrice() - ( MinStopATR * atrCurrent[s] ) && high_long < OrderOpenPrice() && high_long > bid + ( 1 * atrCurrent[s] ) ){
                              r = OrderModify( OrderTicket(), OrderOpenPrice(), 0, NormalizeDouble( high_long, digitsSym ), 0, 0 );
                              if( !r ) Print( "1921. Error in OrderModify. Error code=", GetLastError() ); 
                              else Print( "221. Added initial hard stop: ", OrderSymbol() );
                           } 
                        } else if( OrderType() == OP_SELL ){
                           if( ask > OrderOpenPrice() + ( MinStopATR * atrCurrent[s] ) && low_long > OrderOpenPrice() && low_long < ask - ( 1 * atrCurrent[s] ) ){
                              r = OrderModify( OrderTicket(), OrderOpenPrice(), 0, NormalizeDouble( low_long, digitsSym ), 0, 0 );
                              if( !r ) Print( "1921. Error in OrderModify. Error code=", GetLastError() ); 
                              else Print( "221. Added initial hard stop: ", OrderSymbol() );
                           } 
                        }
                        if( OrderType() == OP_BUY 
                           && ( int ) TimeCurrent() - ( int ) OrderOpenTime() > LongTimeSeconds && firstCurrencyValueSym_Long < secondCurrencyValueSym_Long
                           && OrderProfit() > 0 ){  
                           r = OrderClose( OrderTicket(), OrderLots(), ask, ( int ) slippage ); 
                           if( !r ) Print( "879. Error in OrderClose. Error code=", GetLastError() );
                           else Print( "6436. Closed long time: ", OrderSymbol() );
                           break;
                        } else if( OrderType() == OP_SELL 
                           && ( int ) TimeCurrent() - ( int ) OrderOpenTime() > LongTimeSeconds && firstCurrencyValueSym_Long > secondCurrencyValueSym_Long
                           && OrderProfit() > 0 ){
                           r = OrderClose( OrderTicket(), OrderLots(), bid, ( int ) slippage ); 
                           if( !r ) Print( "546. Error in OrderClose. Error code=", GetLastError() );
                           else Print( "2853. Closed long time: ", OrderSymbol() );
                           break;
                        } 
                     } else {
                        if( OrderType() == OP_BUY ){
                           if( high_long < OrderTakeProfit() && high_long > OrderOpenPrice() - ( MaxStopATR * atrCurrent[s] ) && high_long < OrderOpenPrice() && bid < high_long - ( minStopLevelPips * pipPointSym )   ){
                              r = OrderModify( OrderTicket(), OrderOpenPrice(), 0, NormalizeDouble( high_long, digitsSym ), 0, 0 );
                              if( !r ) Print( "1925. Error in OrderModify. Error code=", GetLastError() ); 
                              else Print( "221. Added initial hard stop: ", OrderSymbol() );
                           } 
                        } else if( OrderType() == OP_SELL ){
                           if( low_long > OrderTakeProfit() && low_long < OrderOpenPrice() + ( MaxStopATR * atrCurrent[s] ) && low_long > OrderOpenPrice() && ask > low_long + ( minStopLevelPips * pipPointSym )   ){
                              r = OrderModify( OrderTicket(), OrderOpenPrice(), 0, NormalizeDouble( low_long, digitsSym ), 0, 0 );
                              if( !r ) Print( "1952. Error in OrderModify. Error code=", GetLastError() ); 
                              else Print( "221. Added initial hard stop: ", OrderSymbol() );
                           } 
                        }  
                     }  
                  }
               }
            }
         }
      }
   }
}    

void prepareNews(){
   if( EnableNews ){
      double newsCalendar = iCustom( Symbol(), 0, "NewsCal-V107",  0, 0 );   
      string newsIndex = "1"; 
      if( ObjectDescription( "zNews LB Calendar¬~C1" ) == "ALL" )
         newsIndex = "2";   
      newsTime = ObjectDescription( StringConcatenate( "zNews LB Calendar¬~T", newsIndex ) );
      newsCurrency = ObjectDescription( StringConcatenate( "zNews LB Calendar¬~C", newsIndex ) );
      newsImpact = "none";  
      if( ObjectGet( StringConcatenate( "zNews LB Calendar¬~E", newsIndex ), OBJPROP_COLOR ) == 8447.0 ){
         newsImpact = "high";
      }  else if( ObjectGet( StringConcatenate( "zNews LB Calendar¬~E", newsIndex ), OBJPROP_COLOR ) == 40959.0 ){
         newsImpact = "medium";
      } else if( ObjectGet( StringConcatenate( "zNews LB Calendar¬~E", newsIndex ), OBJPROP_COLOR ) == 1955804.0 ){
         newsImpact = "low";
      }    
      string timeHour1 = StringSubstr( newsTime, 1, 1 );
      string timeHour2 = StringSubstr( newsTime, 2, 1 ); 
      if( timeHour1 == "h" ){
         hoursToNews = ( int ) StringToInteger( StringSubstr( newsTime, 0, 1 ) );
         minutesToNews = ( int ) StringToInteger( StringSubstr( newsTime, 2, 2 ) );
      } else if( timeHour2 == "h" ){
         hoursToNews = ( int ) StringToInteger( StringSubstr( newsTime, 0, 2 ) );
         minutesToNews = ( int ) StringToInteger( StringSubstr( newsTime, 3, 2 ) );
      } else {
         hoursToNews = 0;
         minutesToNews = ( int ) StringToInteger( StringSubstr( newsTime, 0, 2 ) );
      } 
      string timeHour1M = StringSubstr( newsTime, 0, 1 ); 
      if( timeHour1M == "-" ){
         upCommingRecent = "since";
         string timeHour1m = StringSubstr( newsTime, 2, 1 );
         string timeHour2m = StringSubstr( newsTime, 3, 1 ); 
         if( timeHour1m == "h" ){
            hoursToNews = ( int ) StringToInteger( StringSubstr( newsTime, 1, 1 ) );
            minutesToNews = ( int ) StringToInteger( StringSubstr( newsTime, 2, 2 ) );
         } else if( timeHour2m == "h" ){
            hoursToNews = ( int ) StringToInteger( StringSubstr( newsTime, 1, 2 ) );
            minutesToNews = ( int ) StringToInteger( StringSubstr( newsTime, 4, 2 ) );
         } else {
            hoursToNews = 0;
            minutesToNews = ( int ) StringToInteger( StringSubstr( newsTime, 1, 2 ) );
         } 
      } else upCommingRecent = "until";
      totalSecondsToNews = ( hoursToNews * 60 * 60 ) + ( minutesToNews * 60 ); 
   }
} 

void prepareIndicators(){ 
   HideTestIndicators( true );    
   for( int s = 0; s < generalArrayCount; s++ ) {
      rsiCurrent[s] = iRSI( sym[s], PERIOD_M5, RSIPeriod, PRICE_CLOSE, RSIShift );  
      rsiPrevious[s] = iRSI( sym[s], PERIOD_M5, RSIPeriod, PRICE_CLOSE, RSIShift + RSIShiftCheck );   
      atrCurrent[s] = iATR( sym[s], PERIOD_M5, ATRPeriod, ATRShift );  
      atrPrevious[s] = iATR( sym[s], PERIOD_M5, ATRPeriod, ATRShift + ATRShiftCheck );
   } 
}       

void prepareCloseAll(){
   for( int s = 0; s < generalArrayCount; s++ ) {
      if( closeAllSymArray[s] && totalTradesArray[s] > 0 ){
         closeAll( sym[s] );
      } else if( closeAllSymArray[s] && totalTradesArray[s] == 0 ){
         closeAllSymArray[s] = false;
      }
   }
}  

void manageBaskets(){ 
   double totalBasketProfits = 0;
   double totalLosses = 0;
   for( int s = 0; s < generalArrayCount; s++ ) {   
       totalBasketProfits = totalBasketProfits + totalProfitArray[s];
       totalLosses = totalLosses + totalLossArray[s] + totalChargesArray[s]; 
   } 
   if( ( totalLosses < 0 && totalBasketProfits / MathAbs( totalLosses ) > BasketProfit && totalAccountTrades > StarterTrades ) ){
      for( int s = 0; s < generalArrayCount; s++ ) 
         closeAll( sym[s] ); 
   }  
}  

void closeAll( string symbol ){   
   for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      if( OrderMagicNumber() == MAGIC ){    
         RefreshRates();
         int si = symI( OrderSymbol() );
         closeAllSymArray[si] = true;
         double b = ( double ) MarketInfo( OrderSymbol(), MODE_BID );
         double a = ( double ) MarketInfo( OrderSymbol(), MODE_ASK );
         if( OrderType() == OP_BUY ){  
            r = OrderClose( OrderTicket(), OrderLots(), b, ( int ) Slippage );
            if( !r ) Print( "5. Error in OrderClose. Error code=", GetLastError() ); 
            else {
               lastTradeTimeArray[si] = ( int ) TimeCurrent();
               Print( "5. Close all" ); 
            }
         }
         if( OrderType() == OP_SELL ) { 
            r = OrderClose( OrderTicket(), OrderLots(), a, ( int ) Slippage );
            if( !r ) Print( "6. Error in OrderClose. Error code=", GetLastError() ); 
            else {
               lastTradeTimeArray[si] = ( int ) TimeCurrent();
               Print( "5. Close all" );
            }
         }
      } 
   }
}    

bool isStrongestPair( string firstCurrency, string secondCurrency ){ 
   bool topPair = false;
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < ShortCurrencies; i++ ){ 
         if( currencyStrengthArrayASC[i][1] == 0 ){
             if( firstCurrency == "USD" || secondCurrency == "USD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 1 ){
             if( firstCurrency == "EUR" || secondCurrency == "EUR" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 2 ){
             if( firstCurrency == "GBP" || secondCurrency == "GBP" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 3 ){
             if( firstCurrency == "CHF" || secondCurrency == "CHF" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 4 ){
             if( firstCurrency == "CAD" || secondCurrency == "CAD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 5 ){
             if( firstCurrency == "AUD" || secondCurrency == "AUD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 6 ){
             if( firstCurrency == "NZD" || secondCurrency == "NZD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 7 ){
             if( firstCurrency == "JPY" || secondCurrency == "JPY" ){
               topPair = true;
             }
         } 
      }
   }
   return ( topPair );
}

bool isStrongestPairScalping( string firstCurrency, string secondCurrency ){ 
   bool topPair = false;
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < ScalpingShortCurrencies; i++ ){ 
         if( currencyStrengthArrayASC[i][1] == 0 ){
             if( firstCurrency == "USD" || secondCurrency == "USD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 1 ){
             if( firstCurrency == "EUR" || secondCurrency == "EUR" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 2 ){
             if( firstCurrency == "GBP" || secondCurrency == "GBP" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 3 ){
             if( firstCurrency == "CHF" || secondCurrency == "CHF" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 4 ){
             if( firstCurrency == "CAD" || secondCurrency == "CAD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 5 ){
             if( firstCurrency == "AUD" || secondCurrency == "AUD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 6 ){
             if( firstCurrency == "NZD" || secondCurrency == "NZD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC[i][1] == 7 ){
             if( firstCurrency == "JPY" || secondCurrency == "JPY" ){
               topPair = true;
             }
         } 
      }
   }
   return ( topPair );
}
 

bool isStrongestPair_Long( string firstCurrency, string secondCurrency ){ 
   bool topPair = false;
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < LongCurrencies; i++ ){ 
         if( currencyStrengthArrayASC_Long[i][1] == 0 ){
             if( firstCurrency == "USD" || secondCurrency == "USD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 1 ){
             if( firstCurrency == "EUR" || secondCurrency == "EUR" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 2 ){
             if( firstCurrency == "GBP" || secondCurrency == "GBP" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 3 ){
             if( firstCurrency == "CHF" || secondCurrency == "CHF" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 4 ){
             if( firstCurrency == "CAD" || secondCurrency == "CAD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 5 ){
             if( firstCurrency == "AUD" || secondCurrency == "AUD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 6 ){
             if( firstCurrency == "NZD" || secondCurrency == "NZD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 7 ){
             if( firstCurrency == "JPY" || secondCurrency == "JPY" ){
               topPair = true;
             }
         } 
      }
   }
   return ( topPair );
} 

bool isStrongestPairScalping_Long( string firstCurrency, string secondCurrency ){ 
   bool topPair = false;
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < ScalpingLongCurrencies; i++ ){ 
         if( currencyStrengthArrayASC_Long[i][1] == 0 ){
             if( firstCurrency == "USD" || secondCurrency == "USD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 1 ){
             if( firstCurrency == "EUR" || secondCurrency == "EUR" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 2 ){
             if( firstCurrency == "GBP" || secondCurrency == "GBP" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 3 ){
             if( firstCurrency == "CHF" || secondCurrency == "CHF" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 4 ){
             if( firstCurrency == "CAD" || secondCurrency == "CAD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 5 ){
             if( firstCurrency == "AUD" || secondCurrency == "AUD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 6 ){
             if( firstCurrency == "NZD" || secondCurrency == "NZD" ){
               topPair = true;
             }
         } else if( currencyStrengthArrayASC_Long[i][1] == 7 ){
             if( firstCurrency == "JPY" || secondCurrency == "JPY" ){
               topPair = true;
             }
         } 
      }
   }
   return ( topPair );
}

bool isWeakestPair( string firstCurrency, string secondCurrency ){ 
   bool weakestPair = false;  
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < ShortCurrencies; i++ ){ 
         if( currencyStrengthArrayDESC[i][1] == 0 ){ 
            if( firstCurrency == "USD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "USD" ){ 
               weakestPair = true; 
            }  
         } else if( currencyStrengthArrayDESC[i][1] == 1 ){
             if( firstCurrency == "EUR" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "EUR" ){  
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 2 ){
            if( firstCurrency == "GBP" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "GBP" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 3 ){
             if( firstCurrency == "CHF" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "CHF" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 4 ){
             if( firstCurrency == "CAD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "CAD" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 5 ){
             if( firstCurrency == "AUD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "AUD" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 6 ){
             if( firstCurrency == "NZD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "NZD" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 7 ){
            if( firstCurrency == "JPY" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "JPY" ){ 
               weakestPair = true; 
            } 
         } 
      }   
   }
   return ( weakestPair );
}


bool isWeakestPairScalping( string firstCurrency, string secondCurrency ){ 
   bool weakestPair = false;  
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < ScalpingShortCurrencies; i++ ){ 
         if( currencyStrengthArrayDESC[i][1] == 0 ){ 
            if( firstCurrency == "USD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "USD" ){ 
               weakestPair = true; 
            }  
         } else if( currencyStrengthArrayDESC[i][1] == 1 ){
             if( firstCurrency == "EUR" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "EUR" ){  
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 2 ){
            if( firstCurrency == "GBP" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "GBP" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 3 ){
             if( firstCurrency == "CHF" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "CHF" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 4 ){
             if( firstCurrency == "CAD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "CAD" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 5 ){
             if( firstCurrency == "AUD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "AUD" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 6 ){
             if( firstCurrency == "NZD" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "NZD" ){ 
               weakestPair = true; 
            } 
         } else if( currencyStrengthArrayDESC[i][1] == 7 ){
            if( firstCurrency == "JPY" ){ 
               weakestPair = true; 
            } else if( secondCurrency == "JPY" ){ 
               weakestPair = true; 
            } 
         } 
      }   
   }
   return ( weakestPair );
} 
 
bool isWeakestPair_Long( string firstCurrency, string secondCurrency ){ 
   bool weakestPair = false;  
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < WeakCurrencies; i++ ){ 
         if( currencyStrengthArrayDESC_Long[i][1] == 0 ){ 
            if( firstCurrency == "USD" ){ 
               weakestPair = true;
               break;
            } else if( secondCurrency == "USD" ){ 
               weakestPair = true;
               break;
            }  
         } else if( currencyStrengthArrayDESC_Long[i][1] == 1 ){
             if( firstCurrency == "EUR" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "EUR" ){  
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 2 ){
            if( firstCurrency == "GBP" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "GBP" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 3 ){
             if( firstCurrency == "CHF" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "CHF" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 4 ){
             if( firstCurrency == "CAD" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "CAD" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 5 ){
             if( firstCurrency == "AUD" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "AUD" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 6 ){
             if( firstCurrency == "NZD" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "NZD" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 7 ){
            if( firstCurrency == "JPY" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "JPY" ){ 
               weakestPair = true;
               break; 
            } 
         } 
      }   
   }
   return ( weakestPair );
} 

bool isWeakestPairScalping_Long( string firstCurrency, string secondCurrency ){ 
   bool weakestPair = false;  
   if( Bars > BacktestTradeBars ){
      for( int i = 0; i < ScalpingWeakCurrencies; i++ ){ 
         if( currencyStrengthArrayDESC_Long[i][1] == 0 ){ 
            if( firstCurrency == "USD" ){ 
               weakestPair = true;
               break;
            } else if( secondCurrency == "USD" ){ 
               weakestPair = true;
               break;
            }  
         } else if( currencyStrengthArrayDESC_Long[i][1] == 1 ){
             if( firstCurrency == "EUR" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "EUR" ){  
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 2 ){
            if( firstCurrency == "GBP" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "GBP" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 3 ){
             if( firstCurrency == "CHF" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "CHF" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 4 ){
             if( firstCurrency == "CAD" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "CAD" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 5 ){
             if( firstCurrency == "AUD" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "AUD" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 6 ){
             if( firstCurrency == "NZD" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "NZD" ){ 
               weakestPair = true;
               break; 
            } 
         } else if( currencyStrengthArrayDESC_Long[i][1] == 7 ){
            if( firstCurrency == "JPY" ){ 
               weakestPair = true;
               break; 
            } else if( secondCurrency == "JPY" ){ 
               weakestPair = true;
               break; 
            } 
         } 
      }   
   }
   return ( weakestPair );
} 
 
bool pairIsEnabled( string symbol ){    
   if( EnableAUDCAD && symbol == sym[0] ) return ( true );  
   if( EnableAUDCHF && symbol == sym[1] ) return ( true );  
   if( EnableAUDJPY && symbol == sym[2] ) return ( true );  
   if( EnableAUDNZD && symbol == sym[3] ) return ( true );  
   if( EnableAUDUSD && symbol == sym[4] ) return ( true );  
   if( EnableCADCHF && symbol == sym[5] ) return ( true );  
   if( EnableCADJPY && symbol == sym[6] ) return ( true );  
   if( EnableCHFJPY && symbol == sym[7] ) return ( true );  
   if( EnableEURAUD && symbol == sym[8] ) return ( true );  
   if( EnableEURCAD && symbol == sym[9] ) return ( true );  
   if( EnableEURCHF && symbol == sym[10] ) return ( true );  
   if( EnableEURGBP && symbol == sym[11] ) return ( true );  
   if( EnableEURJPY && symbol == sym[12] ) return ( true ); 
   if( EnableEURNZD && symbol == sym[13] ) return ( true ); 
   if( EnableEURUSD && symbol == sym[14] ) return ( true ); 
   if( EnableGBPAUD && symbol == sym[15] ) return ( true ); 
   if( EnableGBPCAD && symbol == sym[16] ) return ( true ); 
   if( EnableGBPCHF && symbol == sym[17] ) return ( true ); 
   if( EnableGBPJPY && symbol == sym[18] ) return ( true ); 
   if( EnableGBPNZD && symbol == sym[19] ) return ( true ); 
   if( EnableGBPUSD && symbol == sym[20] ) return ( true ); 
   if( EnableNZDCAD && symbol == sym[21] ) return ( true ); 
   if( EnableNZDCHF && symbol == sym[22] ) return ( true ); 
   if( EnableNZDJPY && symbol == sym[23] ) return ( true ); 
   if( EnableNZDUSD && symbol == sym[24] ) return ( true ); 
   if( EnableUSDCAD && symbol == sym[25] ) return ( true ); 
   if( EnableUSDCHF && symbol == sym[26] ) return ( true ); 
   if( EnableUSDJPY && symbol == sym[27] ) return ( true ); 
   return ( false );
}

bool pairIsTradable( string symbol ){    
   if( TradeAUDCAD && symbol == sym[0] ) return ( true );  
   if( TradeAUDCHF && symbol == sym[1] ) return ( true );  
   if( TradeAUDJPY && symbol == sym[2] ) return ( true );  
   if( TradeAUDNZD && symbol == sym[3] ) return ( true );  
   if( TradeAUDUSD && symbol == sym[4] ) return ( true );  
   if( TradeCADCHF && symbol == sym[5] ) return ( true );  
   if( TradeCADJPY && symbol == sym[6] ) return ( true );  
   if( TradeCHFJPY && symbol == sym[7] ) return ( true );  
   if( TradeEURAUD && symbol == sym[8] ) return ( true );  
   if( TradeEURCAD && symbol == sym[9] ) return ( true );  
   if( TradeEURCHF && symbol == sym[10] ) return ( true );  
   if( TradeEURGBP && symbol == sym[11] ) return ( true );  
   if( TradeEURJPY && symbol == sym[12] ) return ( true ); 
   if( TradeEURNZD && symbol == sym[13] ) return ( true ); 
   if( TradeEURUSD && symbol == sym[14] ) return ( true ); 
   if( TradeGBPAUD && symbol == sym[15] ) return ( true ); 
   if( TradeGBPCAD && symbol == sym[16] ) return ( true ); 
   if( TradeGBPCHF && symbol == sym[17] ) return ( true ); 
   if( TradeGBPJPY && symbol == sym[18] ) return ( true ); 
   if( TradeGBPNZD && symbol == sym[19] ) return ( true ); 
   if( TradeGBPUSD && symbol == sym[20] ) return ( true ); 
   if( TradeNZDCAD && symbol == sym[21] ) return ( true ); 
   if( TradeNZDCHF && symbol == sym[22] ) return ( true ); 
   if( TradeNZDJPY && symbol == sym[23] ) return ( true ); 
   if( TradeNZDUSD && symbol == sym[24] ) return ( true ); 
   if( TradeUSDCAD && symbol == sym[25] ) return ( true ); 
   if( TradeUSDCHF && symbol == sym[26] ) return ( true ); 
   if( TradeUSDJPY && symbol == sym[27] ) return ( true ); 
   return ( false );
}

void prepareTrendingSignal(){   
   if( SwingSignal ) {
      int maxDiagnostic = 0;
      for( int s = 0; s < generalArrayCount; s++ ) {  
         bool isEnabled = pairIsEnabled( sym[s] ); 
         
         if( isEnabled ){ 
            openBuyPositionArray[s] = false;
            openSellPositionArray[s] = false;  
            
            string firstCurrencySym = StringSubstr( sym[s], 0, 3 );
            string secondCurrencySym = StringSubstr( sym[s], 3, 3 ); 
            
            double firstCurrencyValueSym = firstCurrencyValue( sym[s] );
            double secondCurrencyValueSym = secondCurrencyValue( sym[s] );
            
            double firstCurrencyValueSym_Long = firstCurrencyValue_Long( sym[s] );
            double secondCurrencyValueSym_Long = secondCurrencyValue_Long( sym[s] );
            
            double strongestPairTrending = isStrongestPair( firstCurrencySym, secondCurrencySym );
            double weakestPairTrending = isWeakestPair( firstCurrencySym, secondCurrencySym );
            
            int tradeTypeTrending = -1;
            if( firstCurrencyValueSym > secondCurrencyValueSym ) tradeTypeTrending = 0;
            else if( firstCurrencyValueSym < secondCurrencyValueSym ) tradeTypeTrending = 1;
            
            double strongestPairTrending_Long = isStrongestPair_Long( firstCurrencySym, secondCurrencySym );
            double weakestPairTrending_Long = isWeakestPair_Long( firstCurrencySym, secondCurrencySym );
             
            int tradeTypeTrending_Long = -1;
            if( firstCurrencyValueSym_Long > secondCurrencyValueSym_Long ) tradeTypeTrending_Long = 0;
            else if( firstCurrencyValueSym_Long < secondCurrencyValueSym_Long ) tradeTypeTrending_Long = 1;   
            
            if( !EnableNews || ( ( ( totalSecondsToNews > BeforeNewsSeconds && upCommingRecent == "until" ) || ( totalSecondsToNews > AfterNewsSeconds && upCommingRecent == "since" ) ) && EnableNews ) ) {
               tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
               tradeDiagnosticsFilterArray[s] = 1; 
               if( ( SignalSwingStartHour < SignalSwingEndHour && Hour() >= SignalSwingStartHour && Hour() <= SignalSwingEndHour ) || ( SignalSwingStartHour > SignalSwingEndHour && ( ( Hour() <= SignalSwingEndHour && Hour() >= 0 ) || ( Hour() <= 23 && Hour() >= SignalSwingStartHour ) || Hour() == 0 ) ) ){                                                 
                  tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                  tradeDiagnosticsFilterArray[s] = 2;
                  if( tradeTypeTrending_Long == OP_BUY ){
                     tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                     tradeDiagnosticsFilterArray[s] = 3;
                     if( weakestPairTrending_Long ){
                        tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                        tradeDiagnosticsFilterArray[s] = 4;
                        if( strongestPairTrending_Long ){
                           tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" ); 
                           tradeDiagnosticsFilterArray[s] = 5;
                           if( tradeTypeTrending == OP_BUY ){
                              tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                              tradeDiagnosticsFilterArray[s] = 6;
                              if( weakestPairTrending ){
                                 tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                                 tradeDiagnosticsFilterArray[s] = 7;
                                 if( strongestPairTrending ){
                                    tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                                    tradeDiagnosticsFilterArray[s] = 8;
                                    if( rsiCurrent[s] < rsiPrevious[s] ){
                                       tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                                       tradeDiagnosticsFilterArray[s] = 9;
                                       if( firstCurrencyValueSym_Long > SwingValue || secondCurrencyValueSym_Long > SwingValue ){  
                                          openBuyPositionArray[s] = false;
                                          openSellPositionArray[s] = true; 
                                          signalCommentArray[s] = "Swing";
                                          tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], " SIGNAL: SELL " );
                                       }
                                    }
                                 }
                              }  
                           }
                        }
                     }
                  } else if( tradeTypeTrending_Long == OP_SELL ){
                     tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" ); 
                     tradeDiagnosticsFilterArray[s] = 3;
                     if( weakestPairTrending_Long ){
                        tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                        tradeDiagnosticsFilterArray[s] = 4;
                        if( strongestPairTrending_Long ){
                           tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" ); 
                           tradeDiagnosticsFilterArray[s] = 5;
                           if( tradeTypeTrending == OP_SELL ){
                              tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                              tradeDiagnosticsFilterArray[s] = 6;
                              if( weakestPairTrending ){
                                 tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                                 tradeDiagnosticsFilterArray[s] = 7;
                                 if( strongestPairTrending ){
                                    tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], "=" );
                                    tradeDiagnosticsFilterArray[s] = 9;
                                    if( rsiCurrent[s] > rsiPrevious[s] ){
                                       if( firstCurrencyValueSym_Long > SwingValue || secondCurrencyValueSym_Long > SwingValue ){ 
                                          openBuyPositionArray[s] = true;
                                          openSellPositionArray[s] = false; 
                                          signalCommentArray[s] = "Swing";
                                          tradeDiagnosticsArray[s] = StringConcatenate( tradeDiagnosticsArray[s], " SIGNAL: BUY" );
                                       }
                                    }
                                 }
                              }  
                           }
                        }
                     }
                  } 
               }   
            }
         }
      }   
   }
}  

void prepareScalpingSignal(){   
   if( ScalpingSignal ) {
      int maxDiagnostic = 0;
      for( int s = 0; s < generalArrayCount; s++ ) {   
         bool isEnabled = pairIsEnabled( sym[s] );  
         if( isEnabled ){ 
            openBuyPositionScalpingArray[s] = false;
            openSellPositionScalpingArray[s] = false;  
            
            string firstCurrencySym = StringSubstr( sym[s], 0, 3 );
            string secondCurrencySym = StringSubstr( sym[s], 3, 3 ); 
            
            double firstCurrencyValueSym = firstCurrencyValue( sym[s] );
            double secondCurrencyValueSym = secondCurrencyValue( sym[s] );
            
            double firstCurrencyValueSym_Long = firstCurrencyValue_Long( sym[s] );
            double secondCurrencyValueSym_Long = secondCurrencyValue_Long( sym[s] );
            
            double strongestPairScalping = isStrongestPairScalping( firstCurrencySym, secondCurrencySym );
            double weakestPairScalping = isWeakestPairScalping( firstCurrencySym, secondCurrencySym );
            
            int tradeTypeScalping = -1;
            if( firstCurrencyValueSym > secondCurrencyValueSym ) tradeTypeScalping = 0;
            else if( firstCurrencyValueSym < secondCurrencyValueSym ) tradeTypeScalping = 1;
            
            double strongestPairScalping_Long = isStrongestPairScalping_Long( firstCurrencySym, secondCurrencySym );
            double weakestPairScalping_Long = isWeakestPairScalping_Long( firstCurrencySym, secondCurrencySym );
             
            int tradeTypeScalping_Long = -1;
            if( firstCurrencyValueSym_Long > secondCurrencyValueSym_Long ) tradeTypeScalping_Long = 0;
            else if( firstCurrencyValueSym_Long < secondCurrencyValueSym_Long ) tradeTypeScalping_Long = 1;  
           
            if( !EnableNews || ( ( ( totalSecondsToNews > BeforeNewsSeconds && upCommingRecent == "until" ) || ( totalSecondsToNews > AfterNewsSeconds && upCommingRecent == "since" ) ) && EnableNews ) ) {
               scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
               scalpingDiagnosticsFilterArray[s] = 1; 
               if( ( SignalScalpingStartHour < SignalScalpingEndHour && Hour() >= SignalScalpingStartHour && Hour() <= SignalScalpingEndHour ) || ( SignalScalpingStartHour > SignalScalpingEndHour && ( ( Hour() <= SignalScalpingEndHour && Hour() >= 0 ) || ( Hour() <= 23 && Hour() >= SignalScalpingStartHour ) || Hour() == 0 ) ) ){                                                 
                  scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                  scalpingDiagnosticsFilterArray[s] = 2; 
                  if( tradeTypeScalping == OP_BUY ){
                     scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                     scalpingDiagnosticsFilterArray[s] = 3;
                     if( weakestPairScalping ){
                        scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                        scalpingDiagnosticsFilterArray[s] = 4;
                        if( strongestPairScalping ){
                           scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                           scalpingDiagnosticsFilterArray[s] = 5;
                           if( rsiCurrent[s] > rsiPrevious[s] ){
                              scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                              scalpingDiagnosticsFilterArray[s] = 6;
                              if( firstCurrencyValueSym_Long < ScalpingValue && secondCurrencyValueSym_Long < ScalpingValue ){  
                                 scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], " SIGNAL: SELL" );
                                 openBuyPositionScalpingArray[s] = false;
                                 openSellPositionScalpingArray[s] = true;  
                                 signalCommentArray[s] = "Scalping"; 
                              }
                           }
                        }
                     }  
                  } else if( tradeTypeScalping == OP_SELL ){
                     scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                     scalpingDiagnosticsFilterArray[s] = 3;
                     if( weakestPairScalping ){
                        scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                        scalpingDiagnosticsFilterArray[s] = 4;
                        if( strongestPairScalping ){
                           scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                           scalpingDiagnosticsFilterArray[s] = 5;
                           if( rsiCurrent[s] < rsiPrevious[s] ){
                              scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], "=" );
                              scalpingDiagnosticsFilterArray[s] = 6;
                              if( firstCurrencyValueSym_Long < ScalpingValue && secondCurrencyValueSym_Long < ScalpingValue ){   
                                 scalpingDiagnosticsArray[s] = StringConcatenate( scalpingDiagnosticsArray[s], " SIGNAL: BUY " );
                                 openBuyPositionScalpingArray[s] = true;
                                 openSellPositionScalpingArray[s] = false;  
                                 signalCommentArray[s] = "Scalping"; 
                              }
                           }
                        }
                     }  
                  } 
               }  
            } 
         }
      }   
   }
}    

int totalTrades(){
   int total = 0; 
   tempTotalTrades = 0;
   for( int s = 0; s < generalArrayCount; s++ ) 
      total = total + totalTradesArray[s]; 
   return ( total );
}

void openPosition(){  
   if( ( int ) TimeCurrent() - preventDuplicates > 20 ){ 
      preventDuplicates = ( int ) TimeCurrent();
      if( ( ( totalAccountTrades < MaxTrades && avgOpenTime < LongTimeSeconds ) || ( totalAccountTrades < ExtraTrades && avgOpenTime > LongTimeSeconds ) ) 
         && ( totalAccountLoosers == totalAccountTrades || totalAccountTrades < StarterTrades ) && ( int ) TimeCurrent() - initTime > 20 ){          
         for( int s = 0; s < generalArrayCount; s++ ) { 
            bool isEnabled = pairIsEnabled( sym[s] );
            bool isTradable = pairIsTradable( sym[s] );
            if( isEnabled && isTradable && totalTradesArray[s] == 0 ){ 
               if( ( int ) TimeCurrent() - lastTradeTimeArray[s] > TradeTimeSeconds ){ 
                  double bid = ( double ) MarketInfo( sym[s], MODE_BID );
                  double ask = ( double ) MarketInfo( sym[s], MODE_ASK );   
                  if( openBuyPositionArray[s] || openBuyPositionScalpingArray[s] ){   
                     if( AccountFreeMarginCheck( sym[s], OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
                     r = OrderSend( sym[s], OP_BUY, lotSize, ask, ( int ) slippage, 0, 0, signalCommentArray[s], MAGIC );
                     if( !r ) Print( "71. Error in OrderSend. Error code=", GetLastError() );     
                     else lastTradeTimeArray[s] = ( int ) TimeCurrent(); 
                     tempTotalTrades++;
                     break;
                  } else if( openSellPositionArray[s] || openSellPositionScalpingArray[s] ){ 
                     if( AccountFreeMarginCheck( sym[s], OP_SELL, lotSize ) <= 0 || GetLastError() == 134 ) return;
                     r = OrderSend( sym[s], OP_SELL, lotSize, bid, ( int ) slippage, 0, 0, signalCommentArray[s], MAGIC );
                     if( !r ) Print( "89. Error in OrderSend. Error code=", GetLastError() );    
                     else lastTradeTimeArray[s] = ( int ) TimeCurrent(); 
                     tempTotalTrades++;
                     break;
                  }
               } 
            }
         }
      } 
   } 
}   

void display(){   
   display = ""; 
   if( EnableNews ){
      display = StringConcatenate( display, " \n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n" ); 
   } else {
      display = StringConcatenate( display, " \n" );
   } 
   display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );
   display = StringConcatenate( display, " ", TradeComment, " v", ver, "\n" );
   display = StringConcatenate( display, " Leverage: ", AccountLeverage(),  ", Lots: ", DoubleToStr( lotSize, LotPrecision ), "\n" ); 
    
   if( totalAccountTrades > 0 ){ 
      tradeDiagnostics = StringConcatenate( tradeDiagnostics, " Total Loosers: ", totalAccountLoosers, ", Total Trades: ", totalAccountTrades );
      display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );
      display = StringConcatenate( display, " OPEN TRADES: ", tradeDiagnostics, "/", MaxTrades, ", Avg Open Time: ", avgOpenTime, "\n" );     
      for( int s = 0; s < generalArrayCount; s++ ) { 
         if( totalTradesArray[s] > 0 ) display = StringConcatenate( display, " ", sym[s], " Trades: ", totalTradesArray[s], " B: ", totalBuyTradesArray[s], " S: ", totalSellTradesArray[s], " Profit: ", DoubleToStr( totalProfitArray[s] + totalLossArray[s], 2 ), " Charges: ", DoubleToStr( totalChargesArray[s], 2 ), "\n" );  
      }
   }
   
   display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );
   
   display = StringConcatenate( display, " LONG PERIOD ", TimeFrame_Long, " \n" );    
   
   for( int i = 0; i < 8; i++ ){  
      if( currencyStrengthArrayDESC_Long[i][0] > 0 ) {
         if( currencyStrengthArrayDESC_Long[i][1] == 0 ) 
            display = StringConcatenate( display, " USD: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC_Long[i][1] == 1 ) 
            display = StringConcatenate( display, " EUR: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC_Long[i][1] == 2 ) 
            display = StringConcatenate( display, " GBP: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC_Long[i][1] == 3 ) 
            display = StringConcatenate( display, " CHF: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC_Long[i][1] == 4 ) 
            display = StringConcatenate( display, " CAD: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC_Long[i][1] == 5 ) 
            display = StringConcatenate( display, " AUD: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC_Long[i][1] == 6 ) 
            display = StringConcatenate( display, " NZD: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC_Long[i][1] == 7 ) 
            display = StringConcatenate( display, " JPY: ", DoubleToStr( currencyStrengthArrayDESC_Long[i][0], 1 ), ", " );  
      }
   }
   display = StringConcatenate( display, "\n" ); 
   display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" ); 
   display = StringConcatenate( display, " SHORT PERIOD ", TimeFrame, " \n" );
     
   for( int i = 0; i < 8; i++ ){  
      if( currencyStrengthArrayDESC[i][0] > 0 ) {
         if( currencyStrengthArrayDESC[i][1] == 0 ) 
            display = StringConcatenate( display, " USD: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC[i][1] == 1 ) 
            display = StringConcatenate( display, " EUR: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC[i][1] == 2 ) 
            display = StringConcatenate( display, " GBP: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC[i][1] == 3 ) 
            display = StringConcatenate( display, " CHF: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC[i][1] == 4 ) 
            display = StringConcatenate( display, " CAD: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC[i][1] == 5 ) 
            display = StringConcatenate( display, " AUD: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC[i][1] == 6 ) 
            display = StringConcatenate( display, " NZD: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );         
         else if( currencyStrengthArrayDESC[i][1] == 7 ) 
            display = StringConcatenate( display, " JPY: ", DoubleToStr( currencyStrengthArrayDESC[i][0], 1 ), ", " );  
      }
   }
   display = StringConcatenate( display, "\n" ); 
    
   display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );                
   
   bool displaySignals = false;
   if( ( ( totalSecondsToNews > BeforeNewsSeconds && upCommingRecent == "until" ) || ( totalSecondsToNews > AfterNewsSeconds && upCommingRecent == "since" ) ) ) {
      display = StringConcatenate( display, " CONTINUE TRADING AS NORMAL", " \n" ); 
      displaySignals = true;
   } else display = StringConcatenate( display, " STOP TRADING FOR NEWS", " \n" ); 
   
   if( hoursToNews > 0 ){
      display = StringConcatenate( display, " News: ",  newsCurrency, " ", hoursToNews, "hr ", minutesToNews, "min, ", upCommingRecent, " ", newsImpact, " impact", "\n" ); 
   } else display = StringConcatenate( display, " News: ",  newsCurrency, " ", minutesToNews, "min, ", upCommingRecent, " ", newsImpact, " impact", "\n" );
   for( int s = 0; s < generalArrayCount; s++ ) { 
      bool isEnabled = pairIsEnabled( sym[s] );   
      if( newsDiagnosticsFilterArray[s] > MinNewsDiagnostic && isEnabled ) display = StringConcatenate( display, " ", sym[s], ": ", newsDiagnosticsArray[s], "\n" );  
   }

   if( ( displaySignals || !EnableNews ) && SwingSignal && TrendingDiagnostics ){
      display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );   
      display = StringConcatenate( display, " SWING SIGNAL " " \n" );  
      for( int s = 0; s < generalArrayCount; s++ ) { 
         bool isEnabled = pairIsEnabled( sym[s] );   
         if( tradeDiagnosticsFilterArray[s] > MinTrendDiagnostic && isEnabled ) display = StringConcatenate( display, " ", sym[s], ": ", tradeDiagnosticsArray[s], "\n" );  
      } 
   }
   
   if( ( displaySignals || !EnableNews ) && ScalpingSignal && ScalpingDiagnostics ){
      display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );   
      display = StringConcatenate( display, " SCALPING SIGNAL " " \n" );   
      for( int s = 0; s < generalArrayCount; s++ ) { 
         bool isEnabled = pairIsEnabled( sym[s] );
         if( scalpingDiagnosticsFilterArray[s] > MinScalpingDiagnostic && isEnabled ) display = StringConcatenate( display, " ", sym[s], ": ", scalpingDiagnosticsArray[s], "\n" );  
      } 
   } 
   display = StringConcatenate( display, " -------------------------------------------------------------------------------------------------------------\n" );   
      
   Comment( display );  
}  
 

void prepareDiagnostics(){
   tradeDiagnostics = "";
   for( int s = 0; s < generalArrayCount; s++ )
      tradeDiagnosticsArray[s] = ""; 
   scalpingDiagnostics = "";
   for( int s = 0; s < generalArrayCount; s++ )
      scalpingDiagnosticsArray[s] = "";
   newsDiagnostics = "";
   for( int s = 0; s < generalArrayCount; s++ )
      newsDiagnosticsArray[s] = "";
}

void preparePositionsArray() {   
   int avgCount = 0;
   double avgTotal = 0; 
   totalAccountLoosers = 0;
   for( int s = 0; s < generalArrayCount; s++ ) { 
      totalTradesArray[s] = 0; 
      totalProfitArray[s] = 0;
      totalLossArray[s] = 0;
      buyLotsArray[s] = 0;
      sellLotsArray[s] = 0;    
      totalChargesArray[s] = 0;
      totalBuyTradesArray[s] = 0;
      totalSellTradesArray[s] = 0;  
      
      for( int i = 0; i < OrdersTotal(); i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;    
         if( OrderMagicNumber() == MAGIC ){   
            if( OrderSymbol() == sym[s] ){
               if( OrderType() == OP_BUY ){
                  buyLotsArray[s] = buyLotsArray[s] + OrderLots(); 
                  totalBuyTradesArray[s]++; 
               } else if( OrderType() == OP_SELL ) {
                  sellLotsArray[s] = sellLotsArray[s] + OrderLots();   
                  totalSellTradesArray[s]++;
               }   
               if( ( int ) TimeCurrent() - OrderOpenTime() < ProfitTimeSeconds && ( int ) TimeCurrent() - OrderOpenTime() > 360 && OrderProfit() > 0 ) profitable[s] = true;  
               if( ( int ) TimeCurrent() - OrderOpenTime() > ProfitTimeSeconds && OrderProfit() < 0 ) totalAccountLoosers++;  
               if( OrderProfit() > 0 ) totalProfitArray[s] = totalProfitArray[s] + OrderProfit() + OrderCommission() + OrderSwap();
               else totalLossArray[s] = totalLossArray[s] + OrderProfit() + OrderCommission() + OrderSwap();
               accountProfitArray[s] = accountProfitArray[s] + OrderProfit() + OrderCommission() + OrderSwap();
               totalChargesArray[s] = totalChargesArray[s] + OrderCommission() + OrderSwap();
               totalTradesArray[s]++;
            }  
         }
      }    
      if( totalBuyTradesArray[s] == 0 && totalSellTradesArray[s] == 0 ){
         trailingProfitArray[s] = 0;
         profitable[s] = false; 
      }
   } 
   totalAccountTrades = totalTrades(); 
   if( totalAccountTrades == 0 ){
      for( int t = 0; t < 8; t++ ){
         closeBasketSymbol[t] = "";
         closeBasketType[t] = -1;
         closeBasketTicket[t] = -1;
         closeBasketLots[t] = 0;
      } 
   }
} 

void closeAllBasket(){   
   for( int t = 0; t < 8; t++ ){  
      RefreshRates(); 
      if( closeBasketTicket[t] > -1 ){
         double bid = ( double ) MarketInfo( closeBasketSymbol[t], MODE_BID );
         double ask = ( double ) MarketInfo( closeBasketSymbol[t], MODE_ASK );
         if( closeBasketType[t] == OP_BUY ){  
            r = OrderClose( closeBasketTicket[t], closeBasketLots[t], bid, ( int ) Slippage );
            if( !r ) Print( "5. Error in OrderClose. Error code=", GetLastError() ); 
            else { 
               closeBasketSymbol[t] = "";
               closeBasketType[t] = -1;
               closeBasketTicket[t] = -1;
               closeBasketLots[t] = 0;
               Print( "54. Close all basket: ", closeBasketTicket[t] );
            }  
         }
         if( closeBasketType[t] == OP_SELL ) { 
            r = OrderClose( closeBasketTicket[t], closeBasketLots[t], ask, ( int ) Slippage );
            if( !r ) Print( "6. Error in OrderClose. Error code=", GetLastError() ); 
            else { 
               closeBasketSymbol[t] = "";
               closeBasketType[t] = -1;
               closeBasketTicket[t] = -1;
               closeBasketLots[t] = 0;
               Print( "53. Close all basket: ", closeBasketTicket[t] );
            } 
         } 
      }
   }
} 

void manageDD(){  
   string largestLossSym = "";
   double largeLossValue = -9999999;
   double totalProfits = 0;  
   double avgTotal = 0;
   int avgTradeTime = 0;
   int avgCount = 0; 
   int lossTicket = -1;   
 
   for( int i = 0; i < OrdersTotal(); i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
      if( OrderMagicNumber() == MAGIC ){     
         if( OrderProfit() < 0 && OrderProfit() > largeLossValue && ( int ) TimeCurrent() - ( int ) OrderOpenTime() > ProfitTimeSeconds ) {
            largeLossValue = OrderProfit(); 
            lossTicket = OrderTicket();
         }
         avgTotal = avgTotal + ( ( int ) TimeCurrent() - ( int ) OrderOpenTime() );
         avgCount++; 
      }
   } 
   
   if( avgCount > 0 ) avgTradeTime = ( int ) avgTotal / avgCount;
   avgOpenTime = ( int ) avgTradeTime;  
   
   for( int s = 0; s < generalArrayCount; s++ )   
       totalProfits = totalProfits + totalProfitArray[s];    
    
   int index = 0;    
   if( largeLossValue != 0 && totalProfits > 0 && largeLossValue < 0 && totalProfits / MathAbs( largeLossValue ) > BasketProfit ){
      for( int i = 0; i < OrdersTotal(); i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderMagicNumber() == MAGIC ){     
            if( OrderProfit() > 0 || OrderTicket() == lossTicket ){
               double b = ( double ) MarketInfo( OrderSymbol(), MODE_BID );
               double a = ( double ) MarketInfo( OrderSymbol(), MODE_ASK );
               if( OrderType() == OP_BUY ){   
                  closeBasketTicket[index] = OrderTicket();
                  closeBasketSymbol[index] = OrderSymbol();
                  closeBasketType[index] = OrderType();
                  closeBasketLots[index] = OrderLots();
                  index++; 
               } else if( OrderType() == OP_SELL ) { 
                  closeBasketTicket[index] = OrderTicket();
                  closeBasketSymbol[index] = OrderSymbol();
                  closeBasketType[index] = OrderType();
                  closeBasketLots[index] = OrderLots();
                  index++; 
               }
            }
         }
      } 
   } 
}
  
void prepare(){  
   prepareDiagnostics();
   prepareIndicators(); 
   prepareNews();
   preparePositionsArray();  
   prepareTrendingSignal();  
   prepareScalpingSignal(); 
   lotSize();    
} 

void OnTick(){    
   closingBasket = false;
   for( int t = 0; t < 8; t++ ){ 
      if( closeBasketTicket[t] > -1 ) {
         closingBasket = true;
         break; 
      }
   } 
   if( Bars > BacktestTradeBars ){     
      if( closingBasket ) closeAllBasket();
      else {
         preparePrices();
         calculatePipRange();
         calculateBidRatio();
         calculateRelativeStrength();
         calculateCurrencyStrength();   
         sortCurrencyStrengthArrayASC();
         sortCurrencyStrengthArrayDESC();
          
         preparePrices_Long();
         calculatePipRange_Long();
         calculateBidRatio_Long();
         calculateRelativeStrength_Long();
         calculateCurrencyStrength_Long();  
         sortCurrencyStrengthArrayASC_Long();
         sortCurrencyStrengthArrayDESC_Long(); 
      
         prepare(); 
         openPosition();      
         manageProfits(); 
         manageNews(); 
         manageBaskets(); 
         manageDD();
         display();
      }
   }     
}     