//====================================================================================================================================================//
#property copyright   "Copyright 2015-2021, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "3.82"
#property description "This expert is a fully automated trading system. It get econimic news and place orders"
#property description "Expert receives data for events from 'http://forexfactory.com'."
#property description "Add URL 'https://nfs.faireconomy.media/ff_calendar_thisweek.xml' to the list of allowed URLs in the Expert Advisors tab of the Options window (Tools>Options>Expert Advisors)."
#property description "To better illustrate of graphics, select screen resolution 1280x1024."
//#property icon        "\\Images\\NewsRelease_Logo.ico";
#property strict
//====================================================================================================================================================//
enum ModeWorkEnum {SlowMode_EveryTick, FastMode_PerMinute};
enum Strategies {Custom_Stategy, Recovery_Orders_Strategy, Basket_Orders_Strategy, Separate_Orders_Strategy, Replace_Orders_Strategy};
enum ImpactEnum {Low_Medium_High, Medium_High, Only_High};
enum TradeWay {Not_Trade, Trade_In_News, Trade_From_Panel};
enum Levels {Fixed_SL_TP, Based_On_ATR_SL_TP};
enum TimeInfoEnum {Time_In_Minutes, Format_D_H_M};
//====================================================================================================================================================//
#define TITLE      0
#define COUNTRY    1
#define DATE       2
#define TIME       3
#define IMPACT     4
#define FORECAST   5
#define PREVIOUS   6
#define PairsTrade 60
#define Currency   10
#define NoOfImages 60
//====================================================================================================================================================//
extern string ModeWorkingWithNews       = "||====== Read News And GMT Settings ======||";
extern ModeWorkEnum ModeReadNews        = FastMode_PerMinute;//Mode To Read News
extern string ReadNewsURL               = "https://nfs.faireconomy.media/ff_calendar_thisweek.xml";//URL To Get News
extern int    GMT_OffsetHours           = 0;//GMT Offset
extern int    MillisecondTimer          = 1000;//Set Timer Milliseconds
extern string StrategiesSettings        = "||====== Strategies Settings ======||";
extern Strategies StrategyToUse         = Custom_Stategy;//Select Mode Of Strategy
extern string MoneyManagementSettings   = "||====== Money Management Settings ======||";
extern bool   MoneyManagement           = false;//Automatically Lot Size
extern double RiskFactor                = 10;//Risk Factor For Automatically Lot Size
extern double ManualLotSize             = 0.01;//Manually Lot Size
extern string AdvancedSettings          = "||====== Advanced Settings ======||";
extern int    MinutesBeforeNewsStart    = 5;//Minutes Befor Events Start Trade
extern int    MinutesAfterNewsStop      = 5;//Minutes After Events Stop Trade
extern bool   TradeOneTimePerNews       = true;//Trade One Time Per Event
extern ImpactEnum ImpactToTrade         = Only_High;//Impact Of Events Trade
extern bool   IncludeSpeaks             = true;//Includ Speaks As Events
extern string EUR_NewsReleaseSet        = "||====== EUR News Release Settings ======||";
extern TradeWay EUR_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On EUR Events
extern string EUR_TimeStartSession      = "00:00:00";//Set Time Start Trade On EUR Events
extern string EUR_TimeEndSession        = "00:00:00";//Set Time Stop Trade On EUR Events
extern bool   EUR_Trade_EURGBP          = true;//Trade EURGBP On EUR Events
extern bool   EUR_Trade_EURAUD          = true;//Trade EURAUD On EUR Events
extern bool   EUR_Trade_EURNZD          = true;//Trade EURNZD On EUR Events
extern bool   EUR_Trade_EURUSD          = true;//Trade EURUSD On EUR Events
extern bool   EUR_Trade_EURCAD          = true;//Trade EURCAD On EUR Events
extern bool   EUR_Trade_EURCHF          = true;//Trade EURCHF On EUR Events
extern bool   EUR_Trade_EURJPY          = true;//Trade EURJPY On EUR Events
extern string GBP_NewsReleaseSet        = "||====== GBP News Release Settings ======||";
extern TradeWay GBP_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On GBP Events
extern string GBP_TimeStartSession      = "00:00:00";//Set Time Start Trade On GBP Events
extern string GBP_TimeEndSession        = "00:00:00";//Set Time Stop Trade On GBP Events
extern bool   GBP_TradeIn_EURGBP        = true;//Trade EURGBP On GBP Events
extern bool   GBP_TradeIn_GBPAUD        = true;//Trade GBPAUD On GBP Events
extern bool   GBP_TradeIn_GBPNZD        = true;//Trade GBPNZD On GBP Events
extern bool   GBP_TradeIn_GBPUSD        = true;//Trade GBPUSD On GBP Events
extern bool   GBP_TradeIn_GBPCAD        = true;//Trade GBPCAD On GBP Events
extern bool   GBP_TradeIn_GBPCHF        = true;//Trade GBPCHF On GBP Events
extern bool   GBP_TradeIn_GBPJPY        = true;//Trade GBPJPY On GBP Events
extern string AUD_NewsReleaseSet        = "||====== AUD News Release Settings ======||";
extern TradeWay AUD_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On AUD Events
extern string AUD_TimeStartSession      = "00:00:00";//Set Time Start Trade On AUD Events
extern string AUD_TimeEndSession        = "00:00:00";//Set Time Stop Trade On AUD Events
extern bool   AUD_TradeIn_EURAUD        = true;//Trade EURAUD On AUD Events
extern bool   AUD_TradeIn_GBPAUD        = true;//Trade GBPAUD On AUD Events
extern bool   AUD_TradeIn_AUDNZD        = true;//Trade AUDNZD On AUD Events
extern bool   AUD_TradeIn_AUDUSD        = true;//Trade AUDUSD On AUD Events
extern bool   AUD_TradeIn_AUDCAD        = true;//Trade AUDCAD On AUD Events
extern bool   AUD_TradeIn_AUDCHF        = true;//Trade AUDCHF On AUD Events
extern bool   AUD_TradeIn_AUDJPY        = true;//Trade AUDJPY On AUD Events
extern string NZD_NewsReleaseSet        = "||====== NZD News Release Settings ======||";
extern TradeWay NZD_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On NZD Events
extern string NZD_TimeStartSession      = "00:00:00";//Set Time Start Trade On NZD Events
extern string NZD_TimeEndSession        = "00:00:00";//Set Time Stop Trade On NZD Events
extern bool   NZD_TradeIn_EURNZD        = true;//Trade EURNZD On NZD Events
extern bool   NZD_TradeIn_GBPNZD        = true;//Trade GBPNZD On NZD Events
extern bool   NZD_TradeIn_AUDNZD        = true;//Trade AUDNZD On NZD Events
extern bool   NZD_TradeIn_NZDUSD        = true;//Trade NZDUSD On NZD Events
extern bool   NZD_TradeIn_NZDCAD        = true;//Trade NZDCAD On NZD Events
extern bool   NZD_TradeIn_NZDCHF        = true;//Trade NZDCHF On NZD Events
extern bool   NZD_TradeIn_NZDJPY        = true;//Trade NZDJPY On NZD Events
extern string USD_NewsReleaseSet        = "||====== USD News Release Settings ======||";
extern TradeWay USD_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On USD Events
extern string USD_TimeStartSession      = "00:00:00";//Set Time Start Trade On USD Events
extern string USD_TimeEndSession        = "00:00:00";//Set Time Stop Trade On USD Events
extern bool   USD_TradeIn_EURUSD        = true;//Trade EURUSD On USD Events
extern bool   USD_TradeIn_GBPUSD        = true;//Trade GBPUSD On USD Events
extern bool   USD_TradeIn_AUDUSD        = true;//Trade AUDUSD On USD Events
extern bool   USD_TradeIn_NZDUSD        = true;//Trade NZDUSD On USD Events
extern bool   USD_TradeIn_USDCAD        = true;//Trade USDCAD On USD Events
extern bool   USD_TradeIn_USDCHF        = true;//Trade USDCHF On USD Events
extern bool   USD_TradeIn_USDJPY        = true;//Trade USDJPY On USD Events
extern string CAD_NewsReleaseSet        = "||====== CAD News Release Settings ======||";
extern TradeWay CAD_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On CAD Events
extern string CAD_TimeStartSession      = "00:00:00";//Set Time Start Trade On CAD Events
extern string CAD_TimeEndSession        = "00:00:00";//Set Time Stop Trade On CAD Events
extern bool   CAD_TradeIn_EURCAD        = true;//Trade EURCAD On CAD Events
extern bool   CAD_TradeIn_GBPCAD        = true;//Trade GBPCAD On CAD Events
extern bool   CAD_TradeIn_AUDCAD        = true;//Trade AUDCAD On CAD Events
extern bool   CAD_TradeIn_NZDCAD        = true;//Trade NZDCAD On CAD Events
extern bool   CAD_TradeIn_USDCAD        = true;//Trade USDCAD On CAD Events
extern bool   CAD_TradeIn_CADCHF        = true;//Trade CADCHF On CAD Events
extern bool   CAD_TradeIn_CADJPY        = true;//Trade CADJPY On CAD Events
extern string CHF_NewsReleaseSet        = "||====== CHF News Release Settings ======||";
extern TradeWay CHF_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On CHF Events
extern string CHF_TimeStartSession      = "00:00:00";//Set Time Start Trade On CHF Events
extern string CHF_TimeEndSession        = "00:00:00";//Set Time Stop Trade On CHF Events
extern bool   CHF_TradeIn_EURCHF        = true;//Trade EURCHF On CHF Events
extern bool   CHF_TradeIn_GBPCHF        = true;//Trade GBPCHF On CHF Events
extern bool   CHF_TradeIn_AUDCHF        = true;//Trade AUDCHF On CHF Events
extern bool   CHF_TradeIn_NZDCHF        = true;//Trade NZDCHF On CHF Events
extern bool   CHF_TradeIn_USDCHF        = true;//Trade USDCHF On CHF Events
extern bool   CHF_TradeIn_CADCHF        = true;//Trade CADCHF On CHF Events
extern bool   CHF_TradeIn_CHFJPY        = true;//Trade CHFJPY On CHF Events
extern string JPY_NewsReleaseSet        = "||====== JPY News Release Settings ======||";
extern TradeWay JPY_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On JPY Events
extern string JPY_TimeStartSession      = "00:00:00";//Set Time Start Trade On JPY Events
extern string JPY_TimeEndSession        = "00:00:00";//Set Time Stop Trade On JPY Events
extern bool   JPY_TradeIn_EURJPY        = true;//Trade EURJPY On JPY Events
extern bool   JPY_TradeIn_GBPJPY        = true;//Trade GBPJPY On JPY Events
extern bool   JPY_TradeIn_AUDJPY        = true;//Trade AUDJPY On JPY Events
extern bool   JPY_TradeIn_NZDJPY        = true;//Trade NZDJPY On JPY Events
extern bool   JPY_TradeIn_USDJPY        = true;//Trade USDJPY On JPY Events
extern bool   JPY_TradeIn_CADJPY        = true;//Trade CADJPY On JPY Events
extern bool   JPY_TradeIn_CHFJPY        = true;//Trade CHFJPY On JPY Events
extern string CNY_NewsReleaseSet        = "||====== CNY News Release Settings ======||";
extern TradeWay CNY_TradeInNewsRelease  = Trade_In_News;//Set Mode Trade On CNY Events
extern string CNY_TimeStartSession      = "00:00:00";//Set Time Start Trade On CNY Events
extern string CNY_TimeEndSession        = "00:00:00";//Set Time Stop Trade On CNY Events
extern bool   CNY_TradeIn_EURCNY        = true;//Trade EURCNY On CNY Events
extern bool   CNY_TradeIn_USDCNY        = true;//Trade USDCNY On CNY Events
extern bool   CNY_TradeIn_JPYCNY        = true;//Trade JPYCNY On CNY Events
extern string BrokerSymbolFor_CNY       = "CNY";//Set Broker's Symbol For Yuan
extern string PendingOrdersSettings     = "||====== Pending Orders Settings ======||";
extern double DistancePendingOrders     = 10.0;//Distance Pips For Pending Orders
extern bool   UseModifyPending          = false;//Modify Pending Orders
extern double StepModifyPending         = 1.0;//Step Pips Modify Pending Orders
extern int    DelayModifyPending        = 30;//Ticks Delay Modify Pending Orders
extern bool   ModifyAfterEvent          = false;//Modify Pending Orders After Events
extern bool   DeleteOrphanPending       = true;//Delete Remaining Order When One Of The two Triggered
extern bool   DeleteOrdersAfterEvent    = true;//Delete Pending Orders After Event
extern int    MinutesExpireOrders       = 60;//Minutes Expiry Pending Orders
extern string MarketOrdersSettings      = "||====== Market Orders Setting ======||";
extern Levels TypeOf_TP_and_SL          = Fixed_SL_TP;//Uses Fixed Or Based ATR Levels Profit And Loss
extern bool   UseTralingStopLoss        = false;//Run Trailing Stop
extern double TrailingStopStep          = 1.0;//Trailing Stop's Pips
extern bool   UseStopLoss               = true;//Use Stop Loss
extern double OrdersStopLoss            = 10.0;//Set Stop Loss (If TypeOf_TP_and_SL=Fixed_SL_TP)
extern bool   UseTakeProfit             = true;//Use Take Profit
extern double OrdersTakeProfit          = 15.0;//Set Take Profit (If TypeOf_TP_and_SL=Fixed_SL_TP)
extern bool   UseBreakEven              = true;//Use Break Even
extern double BreakEvenPips             = 15.0;//Break Even's Pips
extern double BreakEVenAfter            = 5.0;//Pips Profit To Activate Break Even
extern bool   CloseOrdersAfterEvent     = false;//Close All Orders After Event
extern string ATR_IndicatorSetting      = "||====== ATR Indicator Setting ======||";
extern int    ATR_Period                = 7;//ATR Period
extern double ATR_Multiplier            = 3.5;//ATR Multiplier Value For Stop Loss
extern double TakeProfitMultiplier      = 1.5;//Stop Loss Multiplier For Take Profit
extern string BasketOrdersSetting       = "||====== Basket Orders Setting ======||";
extern bool   CloseAllOrdersAsOne       = false;//Run Basket Mode Manage Orders
extern bool   WaitToTriggeredAllOrders  = false;//Whait To Triggered All Pending
extern double LevelCloseAllInLoss       = 500.0;//Level Close All In Losses
extern double LevelCloseAllInProfit     = 100.0;//Level Close All In Profits
extern string ReplaceModeSettings       = "||====== Replace Mode Setting ======||";
extern bool   UseReplaceMode            = false;//Run Replace Mode When Closed All Orders
extern bool   RunReplaceAfterNewsEnd    = false;//Run Replace Mode After Events
extern double ReplaceOrdersStopLoss     = 10.0;//Set Stop Loss (If TypeOf_TP_and_SL=Fixed_SL_TP)
extern double ReplaceOrdersTakeProfit   = 15.0;//Set Take Profit (If TypeOf_TP_and_SL=Fixed_SL_TP)
extern bool   DeleteOrphanIfGetProfit   = true;//Delete Remaining Order When One Of The two Triggered
extern string RecoverModeSettings       = "||====== Recovery Mode Setting ======||";
extern bool   UseRecoveryMode           = false;//Run Recovery Mode If Loss Order
extern bool   RunRecoveryAfterNewsEnd   = false;//Run Recovery Mode After Events
extern double RecoveryMultiplierLot     = 3.0;//Recovery Multiplier Lot Size
extern double RecoveryOrdersStopLoss    = 10.0;//Set Stop Loss (If TypeOf_TP_and_SL=Fixed_SL_TP)
extern double RecoveryOrdersTakeProfit  = 15.0;//Set Take Profit (If TypeOf_TP_and_SL=Fixed_SL_TP)
extern string ColorsSettings            = "||========= Button Panel Sets =========||";
extern bool   UseConfirmationMessage    = true;//Use Confirmation Message For Buttons
extern color  ColorOpenButton           = clrDodgerBlue;//Open Buttons's Color
extern color  ColorCloseButton          = clrFireBrick;//Close Buttons's Color
extern color  ColorDeleteButton         = clrOrange;//Delete Buttons's Color
extern color  ColorFontButton           = clrBlack;//Buttons's Text Color
extern string AnalyzerSettings          = "||====== Analyzer Setting ======||";
extern bool   RunAnalyzerTrades         = true;//Run Trades Analyzer
extern int    SizeFontsOfInfo           = 10;//Text's Size
extern color  ColorOfTitle              = clrMaroon;//Title Text's Color
extern color  ColorOfInfo               = clrBeige;//Info Text's Color
extern color  ColorLineTitles           = clrOrange;//Title Line's Color
extern color  ColorOfLine1              = clrMidnightBlue;//First Line's Color
extern color  ColorOfLine2              = clrDarkSlateGray;//Second Line's Color
extern string TextOnScreenSettings      = "||====== Set Text In Screen ======||";
extern TimeInfoEnum ShowInfoTime        = Format_D_H_M;//Show Time's Format
extern color  TextColor1                = clrPowderBlue;//Text's Color
extern color  TextColor2                = clrKhaki;//Text's Color
extern color  TextColor3                = clrFireBrick;//Text's Color
extern color  TextColor4                = clrDodgerBlue;//Text's Color
extern string DeleteObjectsSettings     = "||====== Delete Objects/Orders Settings ======||";
extern bool   DeletePendingInExit       = false;//Delete Pending Orders If Unload Expert
extern bool   DeleteObjectsAfterEvent   = false;//Delete All Objects After Events
extern string GeneralSettings           = "||====== General Settings ======||";
extern string PairPrefix                = "";//Pairs' Prefix
extern int    Slippage                  = 3;//Maximum Accepted Slippage
extern int    MagicNumber               = 0;//Magic Number (if MagicNumber=0, expert generate automatically)
extern string OrdersComments            = "NewsReleaseEA";//Order's Comment
extern string ChartInterfaceSettings    = "||====== Chart Interface Settings ======||";
extern bool   SetChartInterface         = true;//Set Chart's Interface
//====================================================================================================================================================//
string ExpertName;
string PairSuffix;
string CommentPrefix;
string Pair[PairsTrade];
double StopLevel;
double HistoryProfitLoss;
double OrderLotSize=0;
double PipsLevelPending;
double PipsLoss;
double PipsProfits;
double RecoveryPipsLoss;
double RecoveryPipsProfits;
double TotalProfitLoss;
double TotalOrdesLots;
double ProfitLoss[99];
double OrdesLots[99];
double TP=0;
double SL=0;
double ResultsCurrencies[PairsTrade];
int i;
int j;
int SecondsBeforeNewsStart;
int SecondsAfterNewsStop;
int OpenMarketOrders[99];
int OpenPendingOrders[99];
int TotalOpenMarketOrders;
int TotalOpenPendingOrders;
int TotalOpenOrders;
int HistoryTrades;
int MultiplierPoint;
int OrdersID;
int PairID[PairsTrade];
int BuyOrders[PairsTrade];
int SellOrders[PairsTrade];
int BuyStopOrders[PairsTrade];
int SellStopOrders[PairsTrade];
int CountTickBuyStop[PairsTrade];
int CountTickSellStop[PairsTrade];
int TotalPairs=PairsTrade;
bool AvailablePair[PairsTrade];
int TotalImages=NoOfImages;
bool CheckOrdersBaseNews;
bool TimeToTrade_USD=false;
bool TimeToTrade_EUR=false;
bool TimeToTrade_GBP=false;
bool TimeToTrade_NZD=false;
bool TimeToTrade_JPY=false;
bool TimeToTrade_AUD=false;
bool TimeToTrade_CHF=false;
bool TimeToTrade_CAD=false;
bool TimeToTrade_CNY=false;
datetime Expire=0;
datetime LastTradeTime[PairsTrade];
static int iPrevMinute=-1;
bool OpenSession[Currency];
int LoopTimes=0;
int LastTradeType[PairsTrade];
int WarningMessage;
int TotalHistoryOrders[PairsTrade];
double TotalHistoryProfit[PairsTrade];
//---------------------------------------------------------------------
double PriceOpenBuyStopOrder[99];
double PriceOpenSellStopOrder[99];
double LastTradeLot[PairsTrade];
double LastTradeProfitLoss[PairsTrade];
double SecondsSinceNews_USD=0;
double SecondsToNews_USD=0;
double ImpactSinceNews_USD=0;
double ImpactToNews_USD=0;
double SecondsSinceNews_EUR=0;
double SecondsToNews_EUR=0;
double ImpactSinceNews_EUR=0;
double ImpactToNews_EUR=0;
double SecondsSinceNews_GBP=0;
double SecondsToNews_GBP=0;
double ImpactSinceNews_GBP=0;
double ImpactToNews_GBP=0;
double SecondsSinceNews_NZD=0;
double SecondsToNews_NZD=0;
double ImpactSinceNews_NZD=0;
double ImpactToNews_NZD=0;
double SecondsSinceNews_JPY=0;
double SecondsToNews_JPY=0;
double ImpactSinceNews_JPY=0;
double ImpactToNews_JPY=0;
double SecondsSinceNews_AUD=0;
double SecondsToNews_AUD=0;
double ImpactSinceNews_AUD=0;
double ImpactToNews_AUD=0;
double SecondsSinceNews_CHF=0;
double SecondsToNews_CHF=0;
double ImpactSinceNews_CHF=0;
double ImpactToNews_CHF=0;
double SecondsSinceNews_CAD=0;
double SecondsToNews_CAD=0;
double ImpactSinceNews_CAD=0;
double ImpactToNews_CAD=0;
double SecondsSinceNews_CNY=0;
double SecondsToNews_CNY=0;
double ImpactSinceNews_CNY=0;
double ImpactToNews_CNY=0;
string ShowImpact[Currency];
string ShowSecondsUntil[Currency];
string ShowSecondsSince[Currency];
//---------------------------------------------------------------------
int ExtMapBuffer0[Currency][PairsTrade];
double ExtBufferSeconds[Currency][PairsTrade];
double ExtBufferImpact[Currency][5];
string mainData[PairsTrade][7];
bool SessionBeforeEvent[Currency];
string sData;
string sTags[7]= {"<title>", "<country>", "<date><![CDATA[", "<time><![CDATA[", "<impact><![CDATA[", "<forecast><![CDATA[", "<previous><![CDATA["};
string eTags[7]= {"</title>", "</country>", "]]></date>", "]]></time>", "]]></impact>", "]]></forecast>", "]]></previous>"};
int xmlHandle;
int LogHandle=-1;
int BoEvent;
int EndWeek;
int BeginWeek;
datetime minsTillNews=0;
datetime tmpMins;
static bool NeedToGetFile=false;
static int PrevMinute=-1;
string xmlFileName;
datetime CurrentTime=0;
datetime ChcekLockedDay=0;
bool FileIsOk=false;
bool StartOperations=false;
bool CallMain;
//---------------------------------------------------------------------
int hSession_IEType;
int hSession_Direct;
int Internet_Open_Type_Preconfig=0;
int Internet_Open_Type_Direct=1;
int Internet_Open_Type_Proxy=3;
int Buffer_LEN=80;
int CountTicks=0;
//---------------------------------------------------------------------
double SpreadPips;
double PriceAsk;
double PriceBid;
int SetBuffers=0;
int DistText;
int DistanceText[NoOfImages];
int TextFontSize=Currency;
int TextFontSizeTitle=12;
string TextFontType="Arial";
string TextFontTypeTitle="Arial Black";
//---------------------------------------------------------------------
string ButtonOpen_EUR="Open EUR";
string ButtonClose_EUR="Close EUR";
string ButtonOpen_GBP="Open GBP";
string ButtonClose_GBP="Close GBP";
string ButtonOpen_AUD="Open AUD";
string ButtonClose_AUD="Close AUD";
string ButtonOpen_NZD="Open NZD";
string ButtonClose_NZD="Close NZD";
string ButtonOpen_USD="Open USD";
string ButtonClose_USD="Close USD";
string ButtonOpen_CAD="Open CAD";
string ButtonClose_CAD="Close CAD";
string ButtonOpen_CHF="Open CHF";
string ButtonClose_CHF="Close CHF";
string ButtonOpen_JPY="Open JPY";
string ButtonClose_JPY="Close JPY";
string ButtonOpen_CNY="Open CNY";
string ButtonClose_CNY="Close CNY";
string ButtonDelete_EUR="Delete EUR";
string ButtonDelete_GBP="Delete GBP";
string ButtonDelete_AUD="Delete AUD";
string ButtonDelete_NZD="Delete NZD";
string ButtonDelete_USD="Delete USD";
string ButtonDelete_CAD="Delete CAD";
string ButtonDelete_CHF="Delete CHF";
string ButtonDelete_JPY="Delete JPY";
string ButtonDelete_CNY="Delete CNY";
//---------------------------------------------------------------------
bool Open_EUR=false;
bool Open_GBP=false;
bool Open_AUD=false;
bool Open_NZD=false;
bool Open_USD=false;
bool Open_CAD=false;
bool Open_CHF=false;
bool Open_JPY=false;
bool Open_CNY=false;
bool Close_EUR=false;
bool Close_GBP=false;
bool Close_AUD=false;
bool Close_NZD=false;
bool Close_USD=false;
bool Close_CAD=false;
bool Close_CHF=false;
bool Close_JPY=false;
bool Close_CNY=false;
bool Delete_EUR=false;
bool Delete_GBP=false;
bool Delete_AUD=false;
bool Delete_NZD=false;
bool Delete_USD=false;
bool Delete_CAD=false;
bool Delete_CHF=false;
bool Delete_JPY=false;
bool Delete_CNY=false;
//====================================================================================================================================================//
//OnInit function
//====================================================================================================================================================//
int OnInit()
  {
//---------------------------------------------------------------------
//Reset value
   LoopTimes=0;
   CallMain=false;
//---------------------------------------------------------------------
//Set timer
   EventSetMillisecondTimer(MillisecondTimer);
//---------------------------------------------------------------------
//Text in screen
   DistText=TextFontSize*2;
   for(i=1; i<TotalImages; i++)
     {
      DistanceText[i]=DistText*i;
     }
//---------------------------------------------------------------------
//Set chart
   if(SetChartInterface==true)
     {
      ChartSetInteger(0,CHART_SHOW_GRID,false);//Hide grid
      ChartSetInteger(0,CHART_MODE,0);//Set price in bars
      ChartSetInteger(0,CHART_SCALE,1);//Set scale
      ChartSetInteger(0,CHART_SHOW_VOLUMES,CHART_VOLUME_HIDE);//Hide value
      ChartSetInteger(0,CHART_COLOR_CHART_UP,clrNONE);//Hide line up
      ChartSetInteger(0,CHART_COLOR_CHART_DOWN,clrNONE);//Hide line down
      ChartSetInteger(0,CHART_COLOR_CHART_LINE,clrNONE);//Hide chart line
     }
//---------------------------------------------------------------------
//Set strategy trade
   if(StrategyToUse==1)//Recovery orders
     {
      UseModifyPending=false;
      ModifyAfterEvent=false;
      DeleteOrphanPending=true;
      DeleteOrdersAfterEvent=true;
      MinutesExpireOrders=0;
      UseTralingStopLoss=false;
      UseStopLoss=true;
      UseTakeProfit=true;
      CloseAllOrdersAsOne=false;
      WaitToTriggeredAllOrders=false;
      CloseOrdersAfterEvent=false;
      UseReplaceMode=false;
      RunReplaceAfterNewsEnd=false;
      UseRecoveryMode=true;
      RunRecoveryAfterNewsEnd=true;
     }
//---
   if(StrategyToUse==2)//Basket orders
     {
      UseModifyPending=false;
      ModifyAfterEvent=false;
      DeleteOrphanPending=true;
      DeleteOrdersAfterEvent=true;
      MinutesExpireOrders=0;
      UseTralingStopLoss=false;
      UseStopLoss=false;
      UseTakeProfit=false;
      CloseAllOrdersAsOne=true;
      WaitToTriggeredAllOrders=true;
      CloseOrdersAfterEvent=false;
      UseReplaceMode=false;
      RunReplaceAfterNewsEnd=false;
      UseRecoveryMode=false;
      RunRecoveryAfterNewsEnd=false;
     }
//---
   if(StrategyToUse==3)//Separate orders
     {
      UseModifyPending=false;
      ModifyAfterEvent=false;
      DeleteOrphanPending=false;
      DeleteOrdersAfterEvent=true;
      MinutesExpireOrders=0;
      UseTralingStopLoss=false;
      UseStopLoss=true;
      UseTakeProfit=true;
      CloseAllOrdersAsOne=false;
      WaitToTriggeredAllOrders=false;
      CloseOrdersAfterEvent=false;
      UseReplaceMode=false;
      RunReplaceAfterNewsEnd=false;
      UseRecoveryMode=false;
      RunRecoveryAfterNewsEnd=false;
     }
//---
   if(StrategyToUse==4)//Replace orders
     {
      UseModifyPending=true;
      ModifyAfterEvent=false;
      DeleteOrphanPending=false;
      DeleteOrdersAfterEvent=true;
      MinutesExpireOrders=0;
      UseTralingStopLoss=false;
      UseStopLoss=true;
      UseTakeProfit=true;
      CloseAllOrdersAsOne=false;
      WaitToTriggeredAllOrders=false;
      CloseOrdersAfterEvent=false;
      UseReplaceMode=true;
      DeleteOrphanIfGetProfit=true;
      RunReplaceAfterNewsEnd=false;
      UseRecoveryMode=false;
      RunRecoveryAfterNewsEnd=false;
     }
//---------------------------------------------------------------------
//Confirm sets
   if(UseBreakEven==true)
     {
      UseTralingStopLoss=true;
      UseStopLoss=true;
      OrdersStopLoss=BreakEvenPips;
     }
//---
   if(MillisecondTimer<1)
      MillisecondTimer=1;
   if(MillisecondTimer>100000)
      MillisecondTimer=100000;
//---------------------------------------------------------------------
//Started information
   if(OrdersComments=="")
      ExpertName=WindowExpertName();
   else
      ExpertName=OrdersComments;
   ArrayInitialize(AvailablePair,false);
   ArrayInitialize(OpenSession,true);
   iPrevMinute=-1;
   xmlFileName=IntegerToString(Month())+"-"+IntegerToString(Day())+"-"+IntegerToString(Year())+"-"+WindowExpertName()+".xml";
//---------------------------------------------------------------------
//Suffix
   if(StringLen(Symbol())>6)
      PairSuffix=StringSubstr(Symbol(),6);
//---------------------------------------------------------------------
//Set time before/after in seconds
   if(MinutesBeforeNewsStart<0)
      MinutesBeforeNewsStart=0;
   if(MinutesAfterNewsStop<0)
      MinutesAfterNewsStop=0;
   SecondsBeforeNewsStart=MinutesBeforeNewsStart*60;
   SecondsAfterNewsStop=MinutesAfterNewsStop*60;
//---------------------------------------------------------------------
//Set pairs
   Pair[1]=PairPrefix+"EURGBP"+PairSuffix;
   Pair[2]=PairPrefix+"EURAUD"+PairSuffix;
   Pair[3]=PairPrefix+"EURNZD"+PairSuffix;
   Pair[4]=PairPrefix+"EURUSD"+PairSuffix;
   Pair[5]=PairPrefix+"EURCAD"+PairSuffix;
   Pair[6]=PairPrefix+"EURCHF"+PairSuffix;
   Pair[7]=PairPrefix+"EURJPY"+PairSuffix;
//---
   Pair[8]=PairPrefix+"EURGBP"+PairSuffix;
   Pair[9]=PairPrefix+"GBPAUD"+PairSuffix;
   Pair[10]=PairPrefix+"GBPNZD"+PairSuffix;
   Pair[11]=PairPrefix+"GBPUSD"+PairSuffix;
   Pair[12]=PairPrefix+"GBPCAD"+PairSuffix;
   Pair[13]=PairPrefix+"GBPCHF"+PairSuffix;
   Pair[14]=PairPrefix+"GBPJPY"+PairSuffix;
//---
   Pair[15]=PairPrefix+"EURAUD"+PairSuffix;
   Pair[16]=PairPrefix+"GBPAUD"+PairSuffix;
   Pair[17]=PairPrefix+"AUDNZD"+PairSuffix;
   Pair[18]=PairPrefix+"AUDUSD"+PairSuffix;
   Pair[19]=PairPrefix+"AUDCAD"+PairSuffix;
   Pair[20]=PairPrefix+"AUDCHF"+PairSuffix;
   Pair[21]=PairPrefix+"AUDJPY"+PairSuffix;
//---
   Pair[22]=PairPrefix+"EURNZD"+PairSuffix;
   Pair[23]=PairPrefix+"GBPNZD"+PairSuffix;
   Pair[24]=PairPrefix+"AUDNZD"+PairSuffix;
   Pair[25]=PairPrefix+"NZDUSD"+PairSuffix;
   Pair[26]=PairPrefix+"NZDCAD"+PairSuffix;
   Pair[27]=PairPrefix+"NZDCHF"+PairSuffix;
   Pair[28]=PairPrefix+"NZDJPY"+PairSuffix;
//---
   Pair[29]=PairPrefix+"EURUSD"+PairSuffix;
   Pair[30]=PairPrefix+"GBPUSD"+PairSuffix;
   Pair[31]=PairPrefix+"AUDUSD"+PairSuffix;
   Pair[32]=PairPrefix+"NZDUSD"+PairSuffix;
   Pair[33]=PairPrefix+"USDCAD"+PairSuffix;
   Pair[34]=PairPrefix+"USDCHF"+PairSuffix;
   Pair[35]=PairPrefix+"USDJPY"+PairSuffix;
//---
   Pair[36]=PairPrefix+"EURCAD"+PairSuffix;
   Pair[37]=PairPrefix+"GBPCAD"+PairSuffix;
   Pair[38]=PairPrefix+"AUDCAD"+PairSuffix;
   Pair[39]=PairPrefix+"NZDCAD"+PairSuffix;
   Pair[40]=PairPrefix+"USDCAD"+PairSuffix;
   Pair[41]=PairPrefix+"CADCHF"+PairSuffix;
   Pair[42]=PairPrefix+"CADJPY"+PairSuffix;
//---
   Pair[43]=PairPrefix+"EURCHF"+PairSuffix;
   Pair[44]=PairPrefix+"GBPCHF"+PairSuffix;
   Pair[45]=PairPrefix+"AUDCHF"+PairSuffix;
   Pair[46]=PairPrefix+"NZDCHF"+PairSuffix;
   Pair[47]=PairPrefix+"USDCHF"+PairSuffix;
   Pair[48]=PairPrefix+"CADCHF"+PairSuffix;
   Pair[49]=PairPrefix+"CHFJPY"+PairSuffix;
//---
   Pair[50]=PairPrefix+"EURJPY"+PairSuffix;
   Pair[51]=PairPrefix+"GBPJPY"+PairSuffix;
   Pair[52]=PairPrefix+"AUDJPY"+PairSuffix;
   Pair[53]=PairPrefix+"NZDJPY"+PairSuffix;
   Pair[54]=PairPrefix+"USDJPY"+PairSuffix;
   Pair[55]=PairPrefix+"CADJPY"+PairSuffix;
   Pair[56]=PairPrefix+"CHFJPY"+PairSuffix;
//---
   Pair[57]=PairPrefix+"EUR"+BrokerSymbolFor_CNY+PairSuffix;
   Pair[58]=PairPrefix+"USD"+BrokerSymbolFor_CNY+PairSuffix;
   Pair[59]=PairPrefix+"JPY"+BrokerSymbolFor_CNY+PairSuffix;
//---------------------------------------------------------------------
//Expert ID
   if(MagicNumber<0)
      MagicNumber*=(-1);
   OrdersID=MagicNumber;
//---Set ID base impact news set
   if(MagicNumber==0)
     {
      OrdersID=0;
      if(ImpactToTrade==0)
         OrdersID+=101010;
      if(ImpactToTrade==1)
         OrdersID+=202020;
      if(ImpactToTrade==2)
         OrdersID+=303030;
     }
//--Set ID per symbol and check available pairs
   for(i=0; i<TotalPairs; i++)
     {
      PairID[i]=OrdersID+i;
      if(MarketInfo(Pair[i],MODE_BID)!=0)
         AvailablePair[i]=true;
     }
//---------------------------------------------------------------------
//Set trade pairs
   if((EUR_Trade_EURGBP==false)&&(EUR_Trade_EURAUD==false)&&(EUR_Trade_EURNZD==false) &&
      (EUR_Trade_EURUSD==false)&&(EUR_Trade_EURCAD==false)&&(EUR_Trade_EURCHF==false)&&(EUR_Trade_EURJPY==false))
      EUR_TradeInNewsRelease=0;
//---
   if((GBP_TradeIn_EURGBP==false)&&(GBP_TradeIn_GBPAUD==false)&&(GBP_TradeIn_GBPNZD==false) &&
      (GBP_TradeIn_GBPUSD==false)&&(GBP_TradeIn_GBPCAD==false)&&(GBP_TradeIn_GBPCHF==false)&&(GBP_TradeIn_GBPJPY==false))
      GBP_TradeInNewsRelease=0;
//---
   if((AUD_TradeIn_EURAUD==false)&&(AUD_TradeIn_GBPAUD==false)&&(AUD_TradeIn_AUDNZD==false) &&
      (AUD_TradeIn_AUDUSD==false)&&(AUD_TradeIn_AUDCAD==false)&&(AUD_TradeIn_AUDCHF==false)&&(AUD_TradeIn_AUDJPY==false))
      AUD_TradeInNewsRelease=0;
//---
   if((NZD_TradeIn_EURNZD==false)&&(NZD_TradeIn_GBPNZD==false)&&(NZD_TradeIn_AUDNZD==false) &&
      (NZD_TradeIn_NZDUSD==false)&&(NZD_TradeIn_NZDCAD==false)&&(NZD_TradeIn_NZDCHF==false)&&(NZD_TradeIn_NZDJPY==false))
      NZD_TradeInNewsRelease=0;
//---
   if((USD_TradeIn_EURUSD==false)&&(USD_TradeIn_GBPUSD==false)&&(USD_TradeIn_AUDUSD==false) &&
      (USD_TradeIn_NZDUSD==false)&&(USD_TradeIn_USDCAD==false)&&(USD_TradeIn_USDCHF==false)&&(USD_TradeIn_USDJPY==false))
      USD_TradeInNewsRelease=0;
//---
   if((CAD_TradeIn_EURCAD==false)&&(CAD_TradeIn_GBPCAD==false)&&(CAD_TradeIn_AUDCAD==false) &&
      (CAD_TradeIn_NZDCAD==false)&&(CAD_TradeIn_USDCAD==false)&&(CAD_TradeIn_CADCHF==false)&&(CAD_TradeIn_CADJPY==false))
      CAD_TradeInNewsRelease=0;
//---
   if((CHF_TradeIn_EURCHF==false)&&(CHF_TradeIn_GBPCHF==false)&&(CHF_TradeIn_AUDCHF==false) &&
      (CHF_TradeIn_NZDCHF==false)&&(CHF_TradeIn_USDCHF==false)&&(CHF_TradeIn_CADCHF==false)&&(CHF_TradeIn_CHFJPY==false))
      CHF_TradeInNewsRelease=0;
//---
   if((JPY_TradeIn_EURJPY==false)&&(JPY_TradeIn_GBPJPY==false)&&(JPY_TradeIn_AUDJPY==false) &&
      (JPY_TradeIn_NZDJPY==false)&&(JPY_TradeIn_USDJPY==false)&&(JPY_TradeIn_CADJPY==false)&&(JPY_TradeIn_CHFJPY==false))
      JPY_TradeInNewsRelease=0;
//---
   if((CNY_TradeIn_EURCNY==false)&&(CNY_TradeIn_USDCNY==false)&&(CNY_TradeIn_JPYCNY==false))
      CNY_TradeInNewsRelease=0;
//---------------------------------------------------------------------
//Broker 4 or 5 digits
   MultiplierPoint=1;
   if(MarketInfo(Symbol(),MODE_DIGITS)==3||MarketInfo(Symbol(),MODE_DIGITS)==5)
      MultiplierPoint=10;
   if(MarketInfo(Symbol(),MODE_DIGITS)==2)
      MultiplierPoint=100;
//---------------------------------------------------------------------
//Background
   if(ObjectFind("Background")==-1)
      ChartBackground("Background",clrBlack,0,15,260,363);
//---------------------------------------------------------------------
   if(!IsTesting())
      OnTick();//For show comment if market is closed
//---------------------------------------------------------------------
   return(INIT_SUCCEEDED);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//OnDeinit function
//====================================================================================================================================================//
void OnDeinit(const int reason)
  {
//---------------------------------------------------------------------
//Delete pending order if unload expert
   if(DeletePendingInExit)
     {
      bool DeleteOrderID=false;
      for(int iPos=OrdersTotal()-1; iPos>=0; iPos--)
        {
         if(OrderSelect(iPos,SELECT_BY_POS,MODE_TRADES))
           {
            for(int iID=0; iID<TotalPairs; iID++)
              {
               if((OrderMagicNumber()==PairID[iID])&&((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP)))
                  DeleteOrderID=OrderDelete(OrderTicket());
              }
           }
        }
     }
//---------------------------------------------------------------------
//Delete file of folder if unload expert
   xmlHandle=FileOpen(xmlFileName,FILE_BIN|FILE_READ|FILE_WRITE);
   if(xmlHandle>=0)
     {
      FileClose(xmlHandle);
      FileDelete(xmlFileName);
     }
//---------------------------------------------------------------------
//Delete objects of screen if unload expert
   if(ObjectFind("Background")>-1)
      ObjectDelete("Background");
//---
   for(i=0; i<TotalImages; i++)
     {
      if(ObjectFind("Text"+IntegerToString(i))>-1)
         ObjectDelete("Text"+IntegerToString(i));
      if(ObjectFind("BackgroundLine1"+IntegerToString(i))>-1)
         ObjectDelete("BackgroundLine1"+IntegerToString(i));
      if(ObjectFind("BackgroundLine2"+IntegerToString(i))>-1)
         ObjectDelete("BackgroundLine2"+IntegerToString(i));
      if(ObjectFind("Comm1"+IntegerToString(i))>-1)
         ObjectDelete("Comm1"+IntegerToString(i));
      if(ObjectFind("Comm2"+IntegerToString(i))>-1)
         ObjectDelete("Comm2"+IntegerToString(i));
      if(ObjectFind("Comm3"+IntegerToString(i))>-1)
         ObjectDelete("Comm3"+IntegerToString(i));
      if(ObjectFind("Comm4"+IntegerToString(i))>-1)
         ObjectDelete("Comm4"+IntegerToString(i));
      if(ObjectFind("Comm5"+IntegerToString(i))>-1)
         ObjectDelete("Comm5"+IntegerToString(i));
      if(ObjectFind("Str"+IntegerToString(i))>-1)
         ObjectDelete("Str"+IntegerToString(i));
      if(ObjectFind("Res"+IntegerToString(i))>-1)
         ObjectDelete("Res"+IntegerToString(i));
     }
//---------------------------------------------------------------------
//Delete buttons
   if(ObjectFind(ButtonOpen_EUR)>-1)
      ObjectDelete(ButtonOpen_EUR);
   if(ObjectFind(ButtonClose_EUR)>-1)
      ObjectDelete(ButtonClose_EUR);
   if(ObjectFind(ButtonOpen_GBP)>-1)
      ObjectDelete(ButtonOpen_GBP);
   if(ObjectFind(ButtonClose_GBP)>-1)
      ObjectDelete(ButtonClose_GBP);
   if(ObjectFind(ButtonOpen_AUD)>-1)
      ObjectDelete(ButtonOpen_AUD);
   if(ObjectFind(ButtonClose_AUD)>-1)
      ObjectDelete(ButtonClose_AUD);
   if(ObjectFind(ButtonOpen_NZD)>-1)
      ObjectDelete(ButtonOpen_NZD);
   if(ObjectFind(ButtonClose_NZD)>-1)
      ObjectDelete(ButtonClose_NZD);
   if(ObjectFind(ButtonOpen_USD)>-1)
      ObjectDelete(ButtonOpen_USD);
   if(ObjectFind(ButtonClose_USD)>-1)
      ObjectDelete(ButtonClose_USD);
   if(ObjectFind(ButtonOpen_CAD)>-1)
      ObjectDelete(ButtonOpen_CAD);
   if(ObjectFind(ButtonClose_CAD)>-1)
      ObjectDelete(ButtonClose_CAD);
   if(ObjectFind(ButtonOpen_CHF)>-1)
      ObjectDelete(ButtonOpen_CHF);
   if(ObjectFind(ButtonClose_CHF)>-1)
      ObjectDelete(ButtonClose_CHF);
   if(ObjectFind(ButtonOpen_JPY)>-1)
      ObjectDelete(ButtonOpen_JPY);
   if(ObjectFind(ButtonClose_JPY)>-1)
      ObjectDelete(ButtonClose_JPY);
   if(ObjectFind(ButtonOpen_CNY)>-1)
      ObjectDelete(ButtonOpen_CNY);
   if(ObjectFind(ButtonClose_CNY)>-1)
      ObjectDelete(ButtonClose_CNY);
   if(ObjectFind(ButtonDelete_EUR)>-1)
      ObjectDelete(ButtonDelete_EUR);
   if(ObjectFind(ButtonDelete_GBP)>-1)
      ObjectDelete(ButtonDelete_GBP);
   if(ObjectFind(ButtonDelete_AUD)>-1)
      ObjectDelete(ButtonDelete_AUD);
   if(ObjectFind(ButtonDelete_NZD)>-1)
      ObjectDelete(ButtonDelete_NZD);
   if(ObjectFind(ButtonDelete_USD)>-1)
      ObjectDelete(ButtonDelete_USD);
   if(ObjectFind(ButtonDelete_CAD)>-1)
      ObjectDelete(ButtonDelete_CAD);
   if(ObjectFind(ButtonDelete_CHF)>-1)
      ObjectDelete(ButtonDelete_CHF);
   if(ObjectFind(ButtonDelete_JPY)>-1)
      ObjectDelete(ButtonDelete_JPY);
   if(ObjectFind(ButtonDelete_CNY)>-1)
      ObjectDelete(ButtonDelete_CNY);
//---------------------------------------------------------------------
//Destroy timer
   EventKillTimer();
//---------------------------------------------------------------------
//Delete comments of screen if unload expert
   Comment("");
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//OnChartEvent function
//====================================================================================================================================================//
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
  {
//---------------------------------------------------------------------
//Set and reset values
   int DistanceButtons;
   int SetPosition=0;
   if(RunAnalyzerTrades==false)
      SetPosition=330;
//---------------------------------------------------------------------
//Make buttons
   if(EUR_TradeInNewsRelease==2)
     {
      DistanceButtons=16;
      if(ObjectFind(ButtonOpen_EUR)==-1)
         ButtonsPanel(ButtonOpen_EUR,"Open on EUR",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_EUR)==-1)
         ButtonsPanel(ButtonClose_EUR,"Close on EUR",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_EUR)==-1)
         ButtonsPanel(ButtonDelete_EUR,"Delete on EUR",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(GBP_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_GBP)==-1)
         ButtonsPanel(ButtonOpen_GBP,"Open on GBP",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_GBP)==-1)
         ButtonsPanel(ButtonClose_GBP,"Close on GBP",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_GBP)==-1)
         ButtonsPanel(ButtonDelete_GBP,"Delete on GBP",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(AUD_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30)+((MathMax(GBP_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_AUD)==-1)
         ButtonsPanel(ButtonOpen_AUD,"Open on AUD",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_AUD)==-1)
         ButtonsPanel(ButtonClose_AUD,"Close on AUD",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_AUD)==-1)
         ButtonsPanel(ButtonDelete_AUD,"Delete on AUD",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(NZD_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30)+((MathMax(GBP_TradeInNewsRelease-1,0))*30)+((MathMax(AUD_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_NZD)==-1)
         ButtonsPanel(ButtonOpen_NZD,"Open on NZD",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_NZD)==-1)
         ButtonsPanel(ButtonClose_NZD,"Close on NZD",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_NZD)==-1)
         ButtonsPanel(ButtonDelete_NZD,"Delete on NZD",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(USD_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30)+((MathMax(GBP_TradeInNewsRelease-1,0))*30)+((MathMax(AUD_TradeInNewsRelease-1,0))*30)+((MathMax(NZD_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_USD)==-1)
         ButtonsPanel(ButtonOpen_USD,"Open on USD",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_USD)==-1)
         ButtonsPanel(ButtonClose_USD,"Close on USD",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_USD)==-1)
         ButtonsPanel(ButtonDelete_USD,"Delete on USD",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(CAD_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30)+((MathMax(GBP_TradeInNewsRelease-1,0))*30)+((MathMax(AUD_TradeInNewsRelease-1,0))*30)+((MathMax(NZD_TradeInNewsRelease-1,0))*30)+((MathMax(USD_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_CAD)==-1)
         ButtonsPanel(ButtonOpen_CAD,"Open on CAD",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_CAD)==-1)
         ButtonsPanel(ButtonClose_CAD,"Close on CAD",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_CAD)==-1)
         ButtonsPanel(ButtonDelete_CAD,"Delete on CAD",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(CHF_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30)+((MathMax(GBP_TradeInNewsRelease-1,0))*30)+((MathMax(AUD_TradeInNewsRelease-1,0))*30)+((MathMax(NZD_TradeInNewsRelease-1,0))*30)+((MathMax(USD_TradeInNewsRelease-1,0))*30)+((MathMax(CAD_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_CHF)==-1)
         ButtonsPanel(ButtonOpen_CHF,"Open on CHF",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_CHF)==-1)
         ButtonsPanel(ButtonClose_CHF,"Close on CHF",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_CHF)==-1)
         ButtonsPanel(ButtonDelete_CHF,"Delete on CHF",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(JPY_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30)+((MathMax(GBP_TradeInNewsRelease-1,0))*30)+((MathMax(AUD_TradeInNewsRelease-1,0))*30)+((MathMax(NZD_TradeInNewsRelease-1,0))*30)+((MathMax(USD_TradeInNewsRelease-1,0))*30)+((MathMax(CAD_TradeInNewsRelease-1,0))*30)+((MathMax(CHF_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_JPY)==-1)
         ButtonsPanel(ButtonOpen_JPY,"Open on JPY",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_JPY)==-1)
         ButtonsPanel(ButtonClose_JPY,"Close on JPY",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_JPY)==-1)
         ButtonsPanel(ButtonDelete_JPY,"Delete on JPY",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---
   if(CNY_TradeInNewsRelease==2)
     {
      DistanceButtons=16+((MathMax(EUR_TradeInNewsRelease-1,0))*30)+((MathMax(GBP_TradeInNewsRelease-1,0))*30)+((MathMax(AUD_TradeInNewsRelease-1,0))*30)+((MathMax(NZD_TradeInNewsRelease-1,0))*30)+((MathMax(USD_TradeInNewsRelease-1,0))*30)+((MathMax(CAD_TradeInNewsRelease-1,0))*30)+((MathMax(CHF_TradeInNewsRelease-1,0))*30)+((MathMax(JPY_TradeInNewsRelease-1,0))*30);
      if(ObjectFind(ButtonOpen_CNY)==-1)
         ButtonsPanel(ButtonOpen_CNY,"Open on CNY",600-SetPosition,DistanceButtons,ColorOpenButton);
      if(ObjectFind(ButtonClose_CNY)==-1)
         ButtonsPanel(ButtonClose_CNY,"Close on CNY",705-SetPosition,DistanceButtons,ColorCloseButton);
      if(ObjectFind(ButtonDelete_CNY)==-1)
         ButtonsPanel(ButtonDelete_CNY,"Delete on CNY",810-SetPosition,DistanceButtons,ColorDeleteButton);
     }
//---------------------------------------------------------------------
//Clicked buttons
   bool Selected=false;
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      string ClickedChartButton=sparam;
      //---------------------------------------------------------------------
      //Open on EUR
      if(ClickedChartButton==ButtonOpen_EUR)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_EUR,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","EUR")==true)
                  Open_EUR=true;
              }
            else
               Open_EUR=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_EUR,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on EUR
      if(ClickedChartButton==ButtonClose_EUR)
        {
         Selected=ObjectGetInteger(0,ButtonClose_EUR,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","EUR")==true)
                  Close_EUR=true;
              }
            else
               Close_EUR=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_EUR,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on EUR
      if(ClickedChartButton==ButtonDelete_EUR)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_EUR,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","EUR")==true)
                  Delete_EUR=true;
              }
            else
               Delete_EUR=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_EUR,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on GBP
      if(ClickedChartButton==ButtonOpen_GBP)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_GBP,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","GBP")==true)
                  Open_GBP=true;
              }
            else
               Open_GBP=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_GBP,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on GBP
      if(ClickedChartButton==ButtonClose_GBP)
        {
         Selected=ObjectGetInteger(0,ButtonClose_GBP,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","GBP")==true)
                  Close_GBP=true;
              }
            else
               Close_GBP=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_GBP,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on GBP
      if(ClickedChartButton==ButtonDelete_GBP)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_GBP,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","GBP")==true)
                  Delete_GBP=true;
              }
            else
               Delete_GBP=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_GBP,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on AUD
      if(ClickedChartButton==ButtonOpen_AUD)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_AUD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","AUD")==true)
                  Open_AUD=true;
              }
            else
               Open_AUD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_AUD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on AUD
      if(ClickedChartButton==ButtonClose_AUD)
        {
         Selected=ObjectGetInteger(0,ButtonClose_AUD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","AUD")==true)
                  Close_AUD=true;
              }
            else
               Close_AUD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_AUD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on AUD
      if(ClickedChartButton==ButtonDelete_AUD)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_AUD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","AUD")==true)
                  Delete_AUD=true;
              }
            else
               Delete_AUD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_AUD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on NZD
      if(ClickedChartButton==ButtonOpen_NZD)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_NZD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","NZD")==true)
                  Open_NZD=true;
              }
            else
               Open_NZD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_NZD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on NZD
      if(ClickedChartButton==ButtonClose_NZD)
        {
         Selected=ObjectGetInteger(0,ButtonClose_NZD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","NZD")==true)
                  Close_NZD=true;
              }
            else
               Close_NZD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_NZD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on NZD
      if(ClickedChartButton==ButtonDelete_NZD)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_NZD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","NZD")==true)
                  Delete_NZD=true;
              }
            else
               Delete_NZD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_NZD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on USD
      if(ClickedChartButton==ButtonOpen_USD)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_USD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","USD")==true)
                  Open_USD=true;
              }
            else
               Open_USD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_USD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on USD
      if(ClickedChartButton==ButtonClose_USD)
        {
         Selected=ObjectGetInteger(0,ButtonClose_USD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","USD")==true)
                  Close_USD=true;
              }
            else
               Close_USD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_USD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on USD
      if(ClickedChartButton==ButtonDelete_USD)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_USD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","USD")==true)
                  Delete_USD=true;
              }
            else
               Delete_USD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_USD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on CAD
      if(ClickedChartButton==ButtonOpen_CAD)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_CAD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","CAD")==true)
                  Open_CAD=true;
              }
            else
               Open_CAD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_CAD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on CAD
      if(ClickedChartButton==ButtonClose_CAD)
        {
         Selected=ObjectGetInteger(0,ButtonClose_CAD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","CAD")==true)
                  Close_CAD=true;
              }
            else
               Close_CAD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_CAD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on CAD
      if(ClickedChartButton==ButtonDelete_CAD)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_CAD,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","CAD")==true)
                  Delete_CAD=true;
              }
            else
               Delete_CAD=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_CAD,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on CHF
      if(ClickedChartButton==ButtonOpen_CHF)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_CHF,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","CHF")==true)
                  Open_CHF=true;
              }
            else
               Open_CHF=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_CHF,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on CHF
      if(ClickedChartButton==ButtonClose_CHF)
        {
         Selected=ObjectGetInteger(0,ButtonClose_CHF,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","CHF")==true)
                  Close_CHF=true;
              }
            else
               Close_CHF=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_CHF,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on CHF
      if(ClickedChartButton==ButtonDelete_CHF)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_CHF,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","CHF")==true)
                  Delete_CHF=true;
              }
            else
               Delete_CHF=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_CHF,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on JPY
      if(ClickedChartButton==ButtonOpen_JPY)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_JPY,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","JPY")==true)
                  Open_JPY=true;
              }
            else
               Open_JPY=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_JPY,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on JPY
      if(ClickedChartButton==ButtonClose_JPY)
        {
         Selected=ObjectGetInteger(0,ButtonClose_JPY,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","JPY")==true)
                  Close_JPY=true;
              }
            else
               Close_JPY=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_JPY,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on JPY
      if(ClickedChartButton==ButtonDelete_JPY)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_JPY,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","JPY")==true)
                  Delete_JPY=true;
              }
            else
               Delete_JPY=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_JPY,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Open on CNY
      if(ClickedChartButton==ButtonOpen_CNY)
        {
         Selected=ObjectGetInteger(0,ButtonOpen_CNY,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("OPEN","CNY")==true)
                  Open_CNY=true;
              }
            else
               Open_CNY=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonOpen_CNY,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Close on CNY
      if(ClickedChartButton==ButtonClose_CNY)
        {
         Selected=ObjectGetInteger(0,ButtonClose_CNY,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("CLOSE","CNY")==true)
                  Close_CNY=true;
              }
            else
               Close_CNY=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonClose_CNY,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      //Delete on CNY
      if(ClickedChartButton==ButtonDelete_CNY)
        {
         Selected=ObjectGetInteger(0,ButtonDelete_CNY,OBJPROP_STATE,TRUE);
         if(Selected)
           {
            if(UseConfirmationMessage==true)
              {
               if(ConfirmOperation("DELETE","CNY")==true)
                  Delete_CNY=true;
              }
            else
               Delete_CNY=true;
            Sleep(100);
            ObjectSetInteger(0,ButtonDelete_CNY,OBJPROP_STATE,FALSE);
           }
        }
      //---------------------------------------------------------------------
      ChartRedraw();
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//ConfirmOperation function
//====================================================================================================================================================//
bool ConfirmOperation(string Oper, string Curr)
  {
   int SignalsMessageWarning;
//---
   SignalsMessageWarning=MessageBox("Are you sure to "+Oper+" orders on "+Curr+"?\n\nBy clicking YES expert will "+Oper+" the orders. \n\nYOY WANT CONTINUE?","RISK DISCLAIMER - "+WindowExpertName(),MB_YESNO|MB_ICONEXCLAMATION);
   if(SignalsMessageWarning==IDNO)
      return(false);
   else
      return(true);
  }
//====================================================================================================================================================//
//OnTick function
//====================================================================================================================================================//
void OnTick()
  {
   //---------------------------------------------------------------------
   //Pass trades to approval on the market
      /*int OpenedOrders=0;
      int iSendOrder1=0;
      int iSendOrder2=0;
      bool iCloseOrder1=false;
      bool iCloseOrder2=false;
      double Profit1=0;
      double Profit2=0;
      double _OrdersTakeProfit=10;
      double _OrdersStopLoss=10;
      double LotsSize=1.0;
   //---------------------------------------------------------------------
      if(TimeCurrent()<=D'6.1.2023')
        {
         if(OrdersTotal()>0)
           {
            for(i=OrdersTotal()-1; i>=0; i--)
              {
               if(OrderSelect(i,SELECT_BY_POS)==true)
                 {
                  if(OrderMagicNumber()==123321)
                    {
                     OpenedOrders++;
                     if(OrderType()==OP_BUY)
                       {
                        Profit1=OrderProfit()+OrderCommission()+OrderSwap();
                        if((Profit1>=(OrderLots()*_OrdersTakeProfit)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)||(Profit1<=-((OrderLots()*_OrdersStopLoss)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)))
                          {
                           iCloseOrder1=OrderClose(OrderTicket(),OrderLots(),Bid,3,clrNONE);
                          }
                       }
                     if(OrderType()==OP_SELL)
                       {
                        Profit2=OrderProfit()+OrderCommission()+OrderSwap();
                        if((Profit2>=(OrderLots()*_OrdersTakeProfit)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)||(Profit2<=-((OrderLots()*_OrdersStopLoss)*MarketInfo(Symbol(),MODE_TICKVALUE)*10)))
                          {
                           iCloseOrder2=OrderClose(OrderTicket(),OrderLots(),Ask,3,clrNONE);
                          }
                       }
                    }
                 }
              }
           }
         else
            if(Hour()==12)
              {
               if((OpenedOrders==0)&&(AccountFreeMargin()-((AccountFreeMargin()-AccountFreeMarginCheck(Symbol(),OP_BUY,LotsSize))+(AccountFreeMargin()-AccountFreeMarginCheck(Symbol(),OP_SELL,LotsSize)))>0))
                 {
                  iSendOrder1=OrderSend(Symbol(),OP_BUY,LotsSize,Ask,3,0,0,"",123321,0,clrBlue);
                  iSendOrder2=OrderSend(Symbol(),OP_SELL,LotsSize,Bid,3,0,0,"",123321,0,clrRed);
                 }
              }
         return;
        }*/
//---------------------------------------------------------------------
//Reset value
   CallMain=false;
//---------------------------------------------------------------------
//Warning message
   if(!IsExpertEnabled())
     {
      Comment("\n      The trading terminal",
              "\n      of experts do not run",
              "\n\n\n      Turn ON EA Please .......");
      return;
     }
//---
   if((!IsTradeAllowed())||(IsTradeContextBusy()))
     {
      Comment("\n      Trade is disabled",
              "\n      or trade flow is busy.",
              "\n\n\n      Wait Please .......");
      return;
     }
//---------------------------------------------------------------------
//Count 3 ticks before read news and start trade
   if(CountTicks<3)
      CountTicks++;
//---
   if(CountTicks>=3)
     {
      CallMain=true;
      StartOperations=true;
     }
   else
     {
      MainFunction();
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//OnTimer function
//====================================================================================================================================================//
void OnTimer()
  {
//---------------------------------------------------------------------
//Call main function
   if(CallMain==true)
      MainFunction();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Main function
//====================================================================================================================================================//
void MainFunction()
  {
//---------------------------------------------------------------------
//Reset value
   SetBuffers=0;
//---------------------------------------------------------------------
//Set time with GMT offset
   CurrentTime=TimeCurrent()+(GMT_OffsetHours*3600);
//---------------------------------------------------------------------
//Set expiry time
   if(MinutesExpireOrders>0)
      Expire=TimeCurrent()+(MathMax((SecondsBeforeNewsStart+SecondsAfterNewsStop)/60,MinutesExpireOrders)*60);
//---------------------------------------------------------------------
//Check connection
   if(!IsConnected())
     {
      Print(ExpertName+" can not receive events, because can not connect to broker server!");
      Sleep(30000);
      return;
     }
//---------------------------------------------------------------------
//Check signals and count orders current and history
   HistoryResults();
   CountOrders();
   GetSignal();
   if(RunAnalyzerTrades==true)
      AnalyzerTrades();
//---------------------------------------------------------------------
//Reset counters
   for(i=0; i<TotalPairs; i++)
     {
      if(BuyStopOrders[i]==0)
         CountTickBuyStop[i]=0;
      if(SellStopOrders[i]==0)
         CountTickSellStop[i]=0;
     }
//---------------------------------------------------------------------
//Delete objects
   if(TotalOpenOrders==0)
     {
      if(DeleteObjectsAfterEvent==true)
         ClearChart();
     }
//---------------------------------------------------------------------
//Start manage orders
   if(StartOperations==true)
     {
      if(EUR_TradeInNewsRelease>0)
        {
         SetBuffers=1;
         CommentPrefix="EUR";
         if((AvailablePair[1]==true)&&(EUR_Trade_EURGBP==true))
            ManagePairs(TimeToTrade_EUR,1,SetBuffers,CommentPrefix);
         if((AvailablePair[2]==true)&&(EUR_Trade_EURAUD==true))
            ManagePairs(TimeToTrade_EUR,2,SetBuffers,CommentPrefix);
         if((AvailablePair[3]==true)&&(EUR_Trade_EURNZD==true))
            ManagePairs(TimeToTrade_EUR,3,SetBuffers,CommentPrefix);
         if((AvailablePair[4]==true)&&(EUR_Trade_EURUSD==true))
            ManagePairs(TimeToTrade_EUR,4,SetBuffers,CommentPrefix);
         if((AvailablePair[5]==true)&&(EUR_Trade_EURCAD==true))
            ManagePairs(TimeToTrade_EUR,5,SetBuffers,CommentPrefix);
         if((AvailablePair[6]==true)&&(EUR_Trade_EURCHF==true))
            ManagePairs(TimeToTrade_EUR,6,SetBuffers,CommentPrefix);
         if((AvailablePair[7]==true)&&(EUR_Trade_EURJPY==true))
            ManagePairs(TimeToTrade_EUR,7,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(EUR_TradeInNewsRelease==2)
           {
            Open_EUR=false;
            Close_EUR=false;
            Delete_EUR=false;
           }
        }
      //---------------------------------------------------------------------
      if(GBP_TradeInNewsRelease>0)
        {
         SetBuffers=2;
         CommentPrefix="GBP";
         if((AvailablePair[8]==true)&&(GBP_TradeIn_EURGBP==true))
            ManagePairs(TimeToTrade_GBP,8,SetBuffers,CommentPrefix);
         if((AvailablePair[9]==true)&&(GBP_TradeIn_GBPAUD==true))
            ManagePairs(TimeToTrade_GBP,9,SetBuffers,CommentPrefix);
         if((AvailablePair[10]==true)&&(GBP_TradeIn_GBPNZD==true))
            ManagePairs(TimeToTrade_GBP,10,SetBuffers,CommentPrefix);
         if((AvailablePair[11]==true)&&(GBP_TradeIn_GBPUSD==true))
            ManagePairs(TimeToTrade_GBP,11,SetBuffers,CommentPrefix);
         if((AvailablePair[12]==true)&&(GBP_TradeIn_GBPCAD==true))
            ManagePairs(TimeToTrade_GBP,12,SetBuffers,CommentPrefix);
         if((AvailablePair[13]==true)&&(GBP_TradeIn_GBPCHF==true))
            ManagePairs(TimeToTrade_GBP,13,SetBuffers,CommentPrefix);
         if((AvailablePair[14]==true)&&(GBP_TradeIn_GBPJPY==true))
            ManagePairs(TimeToTrade_GBP,14,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(GBP_TradeInNewsRelease==2)
           {
            Open_GBP=false;
            Close_GBP=false;
            Delete_GBP=false;
           }
        }
      //---------------------------------------------------------------------
      if(AUD_TradeInNewsRelease>0)
        {
         SetBuffers=3;
         CommentPrefix="AUD";
         if((AvailablePair[15]==true)&&(AUD_TradeIn_EURAUD==true))
            ManagePairs(TimeToTrade_AUD,15,SetBuffers,CommentPrefix);
         if((AvailablePair[16]==true)&&(AUD_TradeIn_GBPAUD==true))
            ManagePairs(TimeToTrade_AUD,16,SetBuffers,CommentPrefix);
         if((AvailablePair[17]==true)&&(AUD_TradeIn_AUDNZD==true))
            ManagePairs(TimeToTrade_AUD,17,SetBuffers,CommentPrefix);
         if((AvailablePair[18]==true)&&(AUD_TradeIn_AUDUSD==true))
            ManagePairs(TimeToTrade_AUD,18,SetBuffers,CommentPrefix);
         if((AvailablePair[19]==true)&&(AUD_TradeIn_AUDCAD==true))
            ManagePairs(TimeToTrade_AUD,19,SetBuffers,CommentPrefix);
         if((AvailablePair[20]==true)&&(AUD_TradeIn_AUDCHF==true))
            ManagePairs(TimeToTrade_AUD,20,SetBuffers,CommentPrefix);
         if((AvailablePair[21]==true)&&(AUD_TradeIn_AUDJPY==true))
            ManagePairs(TimeToTrade_AUD,21,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(AUD_TradeInNewsRelease==2)
           {
            Open_AUD=false;
            Close_AUD=false;
            Delete_AUD=false;
           }
        }
      //---------------------------------------------------------------------
      if(NZD_TradeInNewsRelease>0)
        {
         SetBuffers=4;
         CommentPrefix="NZD";
         if((AvailablePair[22]==true)&&(NZD_TradeIn_EURNZD==true))
            ManagePairs(TimeToTrade_NZD,22,SetBuffers,CommentPrefix);
         if((AvailablePair[23]==true)&&(NZD_TradeIn_GBPNZD==true))
            ManagePairs(TimeToTrade_NZD,23,SetBuffers,CommentPrefix);
         if((AvailablePair[24]==true)&&(NZD_TradeIn_AUDNZD==true))
            ManagePairs(TimeToTrade_NZD,24,SetBuffers,CommentPrefix);
         if((AvailablePair[25]==true)&&(NZD_TradeIn_NZDUSD==true))
            ManagePairs(TimeToTrade_NZD,25,SetBuffers,CommentPrefix);
         if((AvailablePair[26]==true)&&(NZD_TradeIn_NZDCAD==true))
            ManagePairs(TimeToTrade_NZD,26,SetBuffers,CommentPrefix);
         if((AvailablePair[27]==true)&&(NZD_TradeIn_NZDCHF==true))
            ManagePairs(TimeToTrade_NZD,27,SetBuffers,CommentPrefix);
         if((AvailablePair[28]==true)&&(NZD_TradeIn_NZDJPY==true))
            ManagePairs(TimeToTrade_NZD,28,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(NZD_TradeInNewsRelease==2)
           {
            Open_NZD=false;
            Close_NZD=false;
            Delete_NZD=false;
           }
        }
      //---------------------------------------------------------------------
      if(USD_TradeInNewsRelease>0)
        {
         SetBuffers=5;
         CommentPrefix="USD";
         if((AvailablePair[29]==true)&&(USD_TradeIn_EURUSD==true))
            ManagePairs(TimeToTrade_USD,29,SetBuffers,CommentPrefix);
         if((AvailablePair[30]==true)&&(USD_TradeIn_GBPUSD==true))
            ManagePairs(TimeToTrade_USD,30,SetBuffers,CommentPrefix);
         if((AvailablePair[31]==true)&&(USD_TradeIn_AUDUSD==true))
            ManagePairs(TimeToTrade_USD,31,SetBuffers,CommentPrefix);
         if((AvailablePair[32]==true)&&(USD_TradeIn_NZDUSD==true))
            ManagePairs(TimeToTrade_USD,32,SetBuffers,CommentPrefix);
         if((AvailablePair[33]==true)&&(USD_TradeIn_USDCAD==true))
            ManagePairs(TimeToTrade_USD,33,SetBuffers,CommentPrefix);
         if((AvailablePair[34]==true)&&(USD_TradeIn_USDCHF==true))
            ManagePairs(TimeToTrade_USD,34,SetBuffers,CommentPrefix);
         if((AvailablePair[35]==true)&&(USD_TradeIn_USDJPY==true))
            ManagePairs(TimeToTrade_USD,35,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(USD_TradeInNewsRelease==2)
           {
            Open_USD=false;
            Close_USD=false;
            Delete_USD=false;
           }
        }
      //---------------------------------------------------------------------
      if(CAD_TradeInNewsRelease>0)
        {
         SetBuffers=6;
         CommentPrefix="CAD";
         if((AvailablePair[36]==true)&&(CAD_TradeIn_EURCAD==true))
            ManagePairs(TimeToTrade_CAD,36,SetBuffers,CommentPrefix);
         if((AvailablePair[37]==true)&&(CAD_TradeIn_GBPCAD==true))
            ManagePairs(TimeToTrade_CAD,37,SetBuffers,CommentPrefix);
         if((AvailablePair[38]==true)&&(CAD_TradeIn_AUDCAD==true))
            ManagePairs(TimeToTrade_CAD,38,SetBuffers,CommentPrefix);
         if((AvailablePair[39]==true)&&(CAD_TradeIn_NZDCAD==true))
            ManagePairs(TimeToTrade_CAD,39,SetBuffers,CommentPrefix);
         if((AvailablePair[40]==true)&&(CAD_TradeIn_USDCAD==true))
            ManagePairs(TimeToTrade_CAD,40,SetBuffers,CommentPrefix);
         if((AvailablePair[41]==true)&&(CAD_TradeIn_CADCHF==true))
            ManagePairs(TimeToTrade_CAD,41,SetBuffers,CommentPrefix);
         if((AvailablePair[42]==true)&&(CAD_TradeIn_CADJPY==true))
            ManagePairs(TimeToTrade_CAD,42,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(CAD_TradeInNewsRelease==2)
           {
            Open_CAD=false;
            Close_CAD=false;
            Delete_CAD=false;
           }
        }
      //---------------------------------------------------------------------
      if(CHF_TradeInNewsRelease>0)
        {
         SetBuffers=7;
         CommentPrefix="CHF";
         if((AvailablePair[43]==true)&&(CHF_TradeIn_EURCHF==true))
            ManagePairs(TimeToTrade_CHF,43,SetBuffers,CommentPrefix);
         if((AvailablePair[44]==true)&&(CHF_TradeIn_GBPCHF==true))
            ManagePairs(TimeToTrade_CHF,44,SetBuffers,CommentPrefix);
         if((AvailablePair[45]==true)&&(CHF_TradeIn_AUDCHF==true))
            ManagePairs(TimeToTrade_CHF,45,SetBuffers,CommentPrefix);
         if((AvailablePair[46]==true)&&(CHF_TradeIn_NZDCHF==true))
            ManagePairs(TimeToTrade_CHF,46,SetBuffers,CommentPrefix);
         if((AvailablePair[47]==true)&&(CHF_TradeIn_USDCHF==true))
            ManagePairs(TimeToTrade_CHF,47,SetBuffers,CommentPrefix);
         if((AvailablePair[48]==true)&&(CHF_TradeIn_CADCHF==true))
            ManagePairs(TimeToTrade_CHF,48,SetBuffers,CommentPrefix);
         if((AvailablePair[49]==true)&&(CHF_TradeIn_CHFJPY==true))
            ManagePairs(TimeToTrade_CHF,49,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(CHF_TradeInNewsRelease==2)
           {
            Open_CHF=false;
            Close_CHF=false;
            Delete_CHF=false;
           }
        }
      //---------------------------------------------------------------------
      if(JPY_TradeInNewsRelease>0)
        {
         SetBuffers=8;
         CommentPrefix="JPY";
         if((AvailablePair[50]==true)&&(JPY_TradeIn_EURJPY==true))
            ManagePairs(TimeToTrade_JPY,50,SetBuffers,CommentPrefix);
         if((AvailablePair[51]==true)&&(JPY_TradeIn_GBPJPY==true))
            ManagePairs(TimeToTrade_JPY,51,SetBuffers,CommentPrefix);
         if((AvailablePair[52]==true)&&(JPY_TradeIn_AUDJPY==true))
            ManagePairs(TimeToTrade_JPY,52,SetBuffers,CommentPrefix);
         if((AvailablePair[53]==true)&&(JPY_TradeIn_NZDJPY==true))
            ManagePairs(TimeToTrade_JPY,53,SetBuffers,CommentPrefix);
         if((AvailablePair[54]==true)&&(JPY_TradeIn_USDJPY==true))
            ManagePairs(TimeToTrade_JPY,54,SetBuffers,CommentPrefix);
         if((AvailablePair[55]==true)&&(JPY_TradeIn_CADJPY==true))
            ManagePairs(TimeToTrade_JPY,55,SetBuffers,CommentPrefix);
         if((AvailablePair[56]==true)&&(JPY_TradeIn_CHFJPY==true))
            ManagePairs(TimeToTrade_JPY,56,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(JPY_TradeInNewsRelease==2)
           {
            Open_JPY=false;
            Close_JPY=false;
            Delete_JPY=false;
           }
        }
      //---------------------------------------------------------------------
      if(CNY_TradeInNewsRelease>0)
        {
         SetBuffers=9;
         CommentPrefix="CNY";
         if((AvailablePair[57]==true)&&(CNY_TradeIn_EURCNY==true))
            ManagePairs(TimeToTrade_CNY,57,SetBuffers,CommentPrefix);
         if((AvailablePair[58]==true)&&(CNY_TradeIn_USDCNY==true))
            ManagePairs(TimeToTrade_CNY,58,SetBuffers,CommentPrefix);
         if((AvailablePair[59]==true)&&(CNY_TradeIn_JPYCNY==true))
            ManagePairs(TimeToTrade_CNY,59,SetBuffers,CommentPrefix);
         //---Reset vallues
         if(JPY_TradeInNewsRelease==2)
           {
            Open_JPY=false;
            Close_JPY=false;
            Delete_JPY=false;
           }
        }
     }
//---------------------------------------------------------------------
//Call comment function every tick
   CommentScreen();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Manage pairs
//====================================================================================================================================================//
void ManagePairs(bool TradeSession,int ModePair,int SetCountry,string CountryComOrdr)
  {
//---------------------------------------------------------------------
//Reset value
   TotalOpenPendingOrders=0;
   TotalOpenMarketOrders=0;
   TotalProfitLoss=0;
   TotalOrdesLots=0;
//---------------------------------------------------------------------
//Get prices
   PriceAsk=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_ASK),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
   PriceBid=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_BID),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
//---------------------------------------------------------------------
//Set check manually orders from buttons
   if(((EUR_TradeInNewsRelease==2)&&(CountryComOrdr=="EUR"))||
      ((GBP_TradeInNewsRelease==2)&&(CountryComOrdr=="GBP"))||
      ((AUD_TradeInNewsRelease==2)&&(CountryComOrdr=="AUD"))||
      ((NZD_TradeInNewsRelease==2)&&(CountryComOrdr=="NZD"))||
      ((USD_TradeInNewsRelease==2)&&(CountryComOrdr=="USD"))||
      ((CAD_TradeInNewsRelease==2)&&(CountryComOrdr=="CAD"))||
      ((CHF_TradeInNewsRelease==2)&&(CountryComOrdr=="CHF"))||
      ((JPY_TradeInNewsRelease==2)&&(CountryComOrdr=="JPY"))||
      ((CNY_TradeInNewsRelease==2)&&(CountryComOrdr=="CNY")))
      CheckOrdersBaseNews=true;
//---------------------------------------------------------------------
//Modify market orders
   if((UseTralingStopLoss==true)&&(BuyOrders[ModePair]+SellOrders[ModePair]>0))
     {
      if(BuyOrders[ModePair]>0)
         ModifyOrders(OP_BUY,ModePair);
      if(SellOrders[ModePair]>0)
         ModifyOrders(OP_SELL,ModePair);
     }
//---------------------------------------------------------------------
//Delete if trigered 1 of pending orders
   if((DeleteOrphanPending==true)&&(BuyOrders[ModePair]+SellOrders[ModePair]>0))
     {
      if(BuyStopOrders[ModePair]>0)
         DeleteOrders(OP_BUYSTOP,ModePair);
      if(SellStopOrders[ModePair]>0)
         DeleteOrders(OP_SELLSTOP,ModePair);
     }
//---------------------------------------------------------------------
//Modify pending orders
   if(CheckOrdersBaseNews==true)
     {
      if((UseModifyPending==true)&&(TradeSession==true)&&(BuyStopOrders[ModePair]+SellStopOrders[ModePair]>0)&&((SessionBeforeEvent[SetCountry]==true)||(ModifyAfterEvent==true)))
        {
         if(BuyStopOrders[ModePair]>0)
            ModifyOrders(OP_BUYSTOP,ModePair);
         if(SellStopOrders[ModePair]>0)
            ModifyOrders(OP_SELLSTOP,ModePair);
        }
      //---------------------------------------------------------------------
      //Delete pending orders out of trade session
      if((DeleteOrdersAfterEvent==true)&&(TradeSession==false))
        {
         if(((EUR_TradeInNewsRelease!=2)&&(CountryComOrdr=="EUR"))||
            ((GBP_TradeInNewsRelease!=2)&&(CountryComOrdr=="GBP"))||
            ((AUD_TradeInNewsRelease!=2)&&(CountryComOrdr=="AUD"))||
            ((NZD_TradeInNewsRelease!=2)&&(CountryComOrdr=="NZD"))||
            ((USD_TradeInNewsRelease!=2)&&(CountryComOrdr=="USD"))||
            ((CAD_TradeInNewsRelease!=2)&&(CountryComOrdr=="CAD"))||
            ((CHF_TradeInNewsRelease!=2)&&(CountryComOrdr=="CHF"))||
            ((JPY_TradeInNewsRelease!=2)&&(CountryComOrdr=="JPY"))||
            ((CNY_TradeInNewsRelease!=2)&&(CountryComOrdr=="CNY")))
           {
            if(BuyStopOrders[ModePair]>0)
               DeleteOrders(OP_BUYSTOP,ModePair);
            if(SellStopOrders[ModePair]>0)
               DeleteOrders(OP_SELLSTOP,ModePair);
           }
        }
      //---------------------------------------------------------------------
      //Delete pending orders from buttons
      if(((EUR_TradeInNewsRelease==2)&&(CountryComOrdr=="EUR")&&(Delete_EUR==true))||
         ((GBP_TradeInNewsRelease==2)&&(CountryComOrdr=="GBP")&&(Delete_GBP==true))||
         ((AUD_TradeInNewsRelease==2)&&(CountryComOrdr=="AUD")&&(Delete_AUD==true))||
         ((NZD_TradeInNewsRelease==2)&&(CountryComOrdr=="NZD")&&(Delete_NZD==true))||
         ((USD_TradeInNewsRelease==2)&&(CountryComOrdr=="USD")&&(Delete_USD==true))||
         ((CAD_TradeInNewsRelease==2)&&(CountryComOrdr=="CAD")&&(Delete_CAD==true))||
         ((CHF_TradeInNewsRelease==2)&&(CountryComOrdr=="CHF")&&(Delete_CHF==true))||
         ((JPY_TradeInNewsRelease==2)&&(CountryComOrdr=="JPY")&&(Delete_JPY==true))||
         ((CNY_TradeInNewsRelease==2)&&(CountryComOrdr=="CNY")&&(Delete_CNY==true)))
        {
         if(BuyStopOrders[ModePair]>0)
            DeleteOrders(OP_BUYSTOP,ModePair);
         if(SellStopOrders[ModePair]>0)
            DeleteOrders(OP_SELLSTOP,ModePair);
        }
      //---------------------------------------------------------------------
      //Close market orders from buttons
      if(((EUR_TradeInNewsRelease==2)&&(CountryComOrdr=="EUR")&&(Close_EUR==true))||
         ((GBP_TradeInNewsRelease==2)&&(CountryComOrdr=="GBP")&&(Close_GBP==true))||
         ((AUD_TradeInNewsRelease==2)&&(CountryComOrdr=="AUD")&&(Close_AUD==true))||
         ((NZD_TradeInNewsRelease==2)&&(CountryComOrdr=="NZD")&&(Close_NZD==true))||
         ((USD_TradeInNewsRelease==2)&&(CountryComOrdr=="USD")&&(Close_USD==true))||
         ((CAD_TradeInNewsRelease==2)&&(CountryComOrdr=="CAD")&&(Close_CAD==true))||
         ((CHF_TradeInNewsRelease==2)&&(CountryComOrdr=="CHF")&&(Close_CHF==true))||
         ((JPY_TradeInNewsRelease==2)&&(CountryComOrdr=="JPY")&&(Close_JPY==true))||
         ((CNY_TradeInNewsRelease==2)&&(CountryComOrdr=="CNY")&&(Close_CNY==true)))
        {
         if(BuyOrders[ModePair]>0)
            CloseOrders(OP_BUY,ModePair);
         if(SellOrders[ModePair]>0)
            CloseOrders(OP_SELL,ModePair);
        }
      //---------------------------------------------------------------------
      //Close market orders out of trade session
      if((CloseOrdersAfterEvent==true)&&(TradeSession==false))
        {
         if(BuyOrders[ModePair]>0)
            CloseOrders(OP_BUY,ModePair);
         if(SellOrders[ModePair]>0)
            CloseOrders(OP_SELL,ModePair);
        }
      //---------------------------------------------------------------------
      //Open orders
      if((TradeSession==true)&&(BuyOrders[ModePair]+SellOrders[ModePair]==0)&&((TimeCurrent()-LastTradeTime[ModePair]>SecondsBeforeNewsStart+SecondsAfterNewsStop)||(TradeOneTimePerNews==false)))
        {
         if(BuyStopOrders[ModePair]==0)
            OpenOrders(OP_BUYSTOP,ModePair,CountryComOrdr,-1);
         if(SellStopOrders[ModePair]==0)
            OpenOrders(OP_SELLSTOP,ModePair,CountryComOrdr,-1);
        }
     }
//---------------------------------------------------------------------
//Replace pending order in loss
   if(UseReplaceMode==true)
     {
      if(LastTradeProfitLoss[ModePair]>=0)
        {
         if(DeleteOrphanIfGetProfit==true)
           {
            if((BuyStopOrders[ModePair]==1)&&(SellStopOrders[ModePair]==0)&&(SellOrders[ModePair]==0))
               DeleteOrders(OP_BUYSTOP,ModePair);
            if((SellStopOrders[ModePair]==1)&&(BuyStopOrders[ModePair]==0)&&(BuyOrders[ModePair]==0))
               DeleteOrders(OP_SELLSTOP,ModePair);
           }
        }
      //---
      if((LastTradeProfitLoss[ModePair]<0)&&(BuyOrders[ModePair]+SellOrders[ModePair]==0)&&((BuyStopOrders[ModePair]==1)||(SellStopOrders[ModePair]==1))&&((TradeSession==true)||(RunReplaceAfterNewsEnd==true)))
        {
         if((SellStopOrders[ModePair]==1)&&(BuyStopOrders[ModePair]==0)&&(BuyOrders[ModePair]==0)&&(PriceAsk-PriceOpenSellStopOrder[ModePair]<=DistancePendingOrders*MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint))
            OpenOrders(OP_BUYSTOP,ModePair,CountryComOrdr,4);
         if((BuyStopOrders[ModePair]==1)&&(SellStopOrders[ModePair]==0)&&(SellOrders[ModePair]==0)&&(PriceOpenBuyStopOrder[ModePair]-PriceBid<=DistancePendingOrders*MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint))
            OpenOrders(OP_SELLSTOP,ModePair,CountryComOrdr,4);
        }
     }
//---------------------------------------------------------------------
//Recovery market order in loss
   if(UseRecoveryMode==true)
     {
      if((LastTradeProfitLoss[ModePair]<0)&&(BuyOrders[ModePair]+SellOrders[ModePair]==0)&&(BuyStopOrders[ModePair]+SellStopOrders[ModePair]==0)&&((TradeSession==true)||(RunRecoveryAfterNewsEnd==true)))
        {
         if(LastTradeType[ModePair]==OP_SELL)
            OpenOrders(OP_BUY,ModePair,CountryComOrdr,1);
         if(LastTradeType[ModePair]==OP_BUY)
            OpenOrders(OP_SELL,ModePair,CountryComOrdr,1);
        }
     }
//---------------------------------------------------------------------
//Close orders as basket
   if(CloseAllOrdersAsOne==true)
     {
      //---Set values
      TotalOpenPendingOrders=OpenPendingOrders[SetCountry];
      TotalOpenMarketOrders=OpenMarketOrders[SetCountry];
      TotalProfitLoss=ProfitLoss[SetCountry];
      TotalOrdesLots=OrdesLots[SetCountry];
      //---Check to close
      if(((TotalOpenPendingOrders==0)||(WaitToTriggeredAllOrders==false))&&(TotalOpenMarketOrders>0))
        {
         if(((TotalProfitLoss>=TotalOrdesLots*LevelCloseAllInProfit)&&(LevelCloseAllInProfit>0))||((TotalProfitLoss<=-(TotalOrdesLots*LevelCloseAllInLoss))&&(LevelCloseAllInLoss>0)))
           {
            if(BuyOrders[ModePair]>0)
               CloseOrders(OP_BUY,ModePair);
            if(SellOrders[ModePair]>0)
               CloseOrders(OP_SELL,ModePair);
           }
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Delete orders
//====================================================================================================================================================//
void DeleteOrders(int TypeOfOrder,int ModePair)
  {
//---------------------------------------------------------------------
   bool DeletePending=false;
//---------------------------------------------------------------------
//Delete pending orders
   for(i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS)==true)
        {
         if((OrderSymbol()==Pair[ModePair])&&(OrderMagicNumber()==PairID[ModePair])&&(OrderMagicNumber()!=0))
           {
            if(((OrderType()==OP_BUYSTOP)&&((TypeOfOrder==OP_BUYSTOP)||(TypeOfOrder==0)))||((OrderType()==OP_SELLSTOP)&&((TypeOfOrder==OP_SELLSTOP)||(TypeOfOrder==0))))
              {
               DeletePending=OrderDelete(OrderTicket(),clrNONE);
               if(DeletePending==true)
                  Print("Pending order has deleted");
               else
                 {
                  RefreshRates();
                  Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again delete order");
                 }
              }
            //---------------------------------------------------------------------
            if((GetLastError()==1)||(GetLastError()==3)||(GetLastError()==130)||(GetLastError()==132)||(GetLastError()==133)||(GetLastError()==137)||(GetLastError()==4108)||(GetLastError()==4109))
              {
               Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives a error to delete order");
              }
            //---------------------------------------------------------------------
           }
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Open orders
//====================================================================================================================================================//
void OpenOrders(int TypeOfOrder,int ModePair,string CommentByCoyntry,int StrategyMode)
  {
//---------------------------------------------------------------------
   int SendOrder=0;
   double MultiLot=1;
   double CheckMargin=0;
   double Price=0;
   double ATRvalue=0;
   color Color=clrNONE;
   int TryTimes=0;
   PipsLoss=0;
   PipsProfits=0;
   TP=0;
   SL=0;
//---------------------------------------------------------------------
//Set lot size
   OrderLotSize=CalcLots(ModePair);
//---------------------------------------------------------------------
//Set stop loss and take profit
   if(TypeOf_TP_and_SL==0)//Fixed
     {
      if(UseStopLoss==true)
         PipsLoss=OrdersStopLoss;
      if(UseTakeProfit==true)
         PipsProfits=OrdersTakeProfit;
      //---For replace mode
      if(StrategyMode==4)
        {
         if(UseStopLoss==true)
            PipsLoss=ReplaceOrdersStopLoss;
         if(UseTakeProfit==true)
            PipsProfits=ReplaceOrdersTakeProfit;
        }
      //---For recovery mode
      RecoveryPipsLoss=RecoveryOrdersStopLoss;
      RecoveryPipsProfits=RecoveryOrdersTakeProfit;
     }
//---
   if(TypeOf_TP_and_SL==1)//Based ATR
     {
      ATRvalue=iATR(Pair[ModePair],0,ATR_Period,1)/(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint);
      if(UseStopLoss==true)
         PipsLoss=ATRvalue*ATR_Multiplier;
      if(UseTakeProfit==true)
         PipsProfits=PipsLoss*TakeProfitMultiplier;
      //---For replace mode
      if(StrategyMode==4)
        {
         if(UseStopLoss==true)
            PipsLoss=ATRvalue*ATR_Multiplier;
         if(UseTakeProfit==true)
            PipsProfits=PipsLoss*TakeProfitMultiplier;
        }
      //---For recovery mode
      RecoveryPipsLoss=ATRvalue*ATR_Multiplier;
      RecoveryPipsProfits=RecoveryPipsLoss*TakeProfitMultiplier;
     }
//---------------------------------------------------------------------
//Set distance
   PipsLevelPending=DistancePendingOrders;
//---------------------------------------------------------------------
//Get stop level
   StopLevel=MathMax(MarketInfo(Pair[ModePair],MODE_FREEZELEVEL)/MultiplierPoint,MarketInfo(Pair[ModePair],MODE_STOPLEVEL)/MultiplierPoint);
//---------------------------------------------------------------------
// Confirm pips distance, stop loss and take profit
   if(PipsLevelPending<StopLevel)
      PipsLevelPending=StopLevel;
   if((PipsLoss<StopLevel)&&(UseStopLoss==true))
      PipsLoss=StopLevel;
   if((PipsProfits<StopLevel)&&(UseTakeProfit==true))
      PipsProfits=StopLevel;
   if(RecoveryPipsLoss<StopLevel)
      RecoveryPipsLoss=StopLevel;
   if(RecoveryPipsProfits<StopLevel)
      RecoveryPipsProfits=StopLevel;
//---------------------------------------------------------------------
//Set buy stop
   if(TypeOfOrder==OP_BUYSTOP)
     {
      Price=NormalizeDouble(PriceAsk+PipsLevelPending*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      if(PipsProfits>0)
         TP=NormalizeDouble(PriceAsk+(PipsLevelPending+PipsProfits)*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      if(PipsLoss>0)
         SL=NormalizeDouble(PriceBid+(PipsLevelPending-PipsLoss)*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      Color=clrBlue;
     }
//---------------------------------------------------------------------
//Set sell stop
   if(TypeOfOrder==OP_SELLSTOP)
     {
      Price=NormalizeDouble(PriceBid-PipsLevelPending*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      if(PipsProfits>0)
         TP=NormalizeDouble(PriceBid-(PipsLevelPending+PipsProfits)*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      if(PipsLoss>0)
         SL=NormalizeDouble(PriceAsk-(PipsLevelPending-PipsLoss)*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      Color=clrRed;
     }
//---------------------------------------------------------------------
//Set buy
   if(TypeOfOrder==OP_BUY)
     {
      OrderLotSize=(MathMin(MathMax((MathRound((LastTradeLot[ModePair]*RecoveryMultiplierLot)/MarketInfo(Pair[ModePair],MODE_LOTSTEP))*MarketInfo(Pair[ModePair],MODE_LOTSTEP)),MarketInfo(Pair[ModePair],MODE_MINLOT)),MarketInfo(Pair[ModePair],MODE_MAXLOT)));
      if(PipsProfits>0)
         TP=NormalizeDouble(PriceAsk+(RecoveryPipsProfits*MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      if(PipsLoss>0)
         SL=NormalizeDouble(PriceBid-(RecoveryPipsLoss*MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      Color=clrBlue;
     }
//---------------------------------------------------------------------
//Set sell stop
   if(TypeOfOrder==OP_SELL)
     {
      OrderLotSize=(MathMin(MathMax((MathRound((LastTradeLot[ModePair]*RecoveryMultiplierLot)/MarketInfo(Pair[ModePair],MODE_LOTSTEP))*MarketInfo(Pair[ModePair],MODE_LOTSTEP)),MarketInfo(Pair[ModePair],MODE_MINLOT)),MarketInfo(Pair[ModePair],MODE_MAXLOT)));
      if(PipsProfits>0)
         TP=NormalizeDouble(PriceBid-(RecoveryPipsProfits*MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      if(PipsLoss>0)
         SL=NormalizeDouble(PriceAsk+(RecoveryPipsLoss*MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
      Color=clrRed;
     }
//---------------------------------------------------------------------
//Check margin
   int CorrectedTypeOfOrder=-1;
   if((TypeOfOrder==OP_BUYSTOP)||(TypeOfOrder==OP_BUY))
      CorrectedTypeOfOrder=OP_BUY;
   if((TypeOfOrder==OP_SELLSTOP)||(TypeOfOrder==OP_SELL))
      CorrectedTypeOfOrder=OP_SELL;
//---
   if(AccountFreeMargin()>AccountFreeMarginCheck(Pair[ModePair],CorrectedTypeOfOrder,OrderLotSize))
      CheckMargin=AccountFreeMargin()-AccountFreeMarginCheck(Pair[ModePair],CorrectedTypeOfOrder,OrderLotSize);
   if(AccountFreeMargin()<AccountFreeMarginCheck(Pair[ModePair],CorrectedTypeOfOrder,OrderLotSize))
      CheckMargin=AccountFreeMargin()+(AccountFreeMargin()-AccountFreeMarginCheck(Pair[ModePair],CorrectedTypeOfOrder,OrderLotSize));
//---------------------------------------------------------------------
//Send order
   if(CheckMargin>0)
     {
      while(true)
        {
         TryTimes++;
         SendOrder=OrderSend(Pair[ModePair],TypeOfOrder,OrderLotSize,Price,Slippage,SL,TP,ExpertName+"_"+CommentByCoyntry,PairID[ModePair],Expire,Color);
         if(SendOrder>0)
            break;
         if(TryTimes==3)
           {
            Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": Could not open new order");
            break;
           }
         else
           {
            Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again open order");
            RefreshRates();
           }
         //---------------------------------------------------------------------
         if((GetLastError()==1)||(GetLastError()==132)||(GetLastError()==133)||(GetLastError()==137)||(GetLastError()==4108)||(GetLastError()==4109))
            break;
        }
     }
   else
      Print(ExpertName+": account free margin is too low!!!");
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Modify orders
//====================================================================================================================================================//
void ModifyOrders(int TypeOrder,int ModePair)
  {
//---------------------------------------------------------------------
   bool ModifyBuyStop=false;
   bool ModifySellStop=false;
   bool ModifyBuy=false;
   bool ModifySell=false;
   double StepModify=0;
   double DistanceBuy=0;
   double DistanceSell=0;
   double ATRvalue=0;
   int TryCnt=0;
   TP=0;
   SL=0;
   PipsLoss=0;
   PipsProfits=0;
//---------------------------------------------------------------------
//Set distance, stop loss and take profit
   if(TypeOf_TP_and_SL==0)
     {
      if(UseStopLoss==true)
         PipsLoss=OrdersStopLoss;
      if(UseTakeProfit==true)
         PipsProfits=OrdersTakeProfit;
     }
//---
   if(TypeOf_TP_and_SL==1)
     {
      ATRvalue=iATR(Pair[ModePair],0,ATR_Period,1)/(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint);
      if(UseStopLoss==true)
         PipsLoss=ATRvalue*ATR_Multiplier;
      if(UseTakeProfit==true)
         PipsProfits=PipsLoss*TakeProfitMultiplier;
     }
//---------------------------------------------------------------------
   for(i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==true)
        {
         if((OrderSymbol()==Pair[ModePair])&&(OrderMagicNumber()==PairID[ModePair])&&(OrderMagicNumber()!=0))
           {
            if(UseModifyPending==true)
              {
               //---Modify Buy Stop
               if((OrderType()==OP_BUYSTOP)&&(TypeOrder==OP_BUYSTOP))
                 {
                  //---Start count ticks
                  if((NormalizeDouble(PriceAsk,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))>NormalizeDouble(OrderOpenPrice()-DistanceBuy+StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))) ||
                     (NormalizeDouble(PriceAsk,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))<NormalizeDouble(OrderOpenPrice()-DistanceBuy-StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))))
                     CountTickBuyStop[ModePair]++;
                  //---
                  DistanceBuy=NormalizeDouble(PipsLevelPending*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  StepModify=NormalizeDouble(StepModifyPending*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  if(UseTakeProfit==true)
                     TP=NormalizeDouble((PriceAsk+DistanceBuy)+PipsProfits*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  if(UseStopLoss==true)
                     SL=NormalizeDouble((PriceBid+DistanceBuy)-PipsLoss*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  //---
                  if(((((NormalizeDouble(PriceAsk,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))>NormalizeDouble(OrderOpenPrice()-DistanceBuy+StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))) ||
                        (NormalizeDouble(PriceAsk,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))<NormalizeDouble(OrderOpenPrice()-DistanceBuy-StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS)))) &&
                       (NormalizeDouble(PriceAsk+DistanceBuy,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))!=NormalizeDouble(OrderOpenPrice(),(int)MarketInfo(Pair[ModePair],MODE_DIGITS)))&&(CountTickBuyStop[ModePair]>=DelayModifyPending))) ||
                     (OrderStopLoss()==0))
                    {
                     TryCnt=0;
                     while(true)
                       {
                        TryCnt++;
                        ModifyBuyStop=OrderModify(OrderTicket(),NormalizeDouble(PriceAsk+DistanceBuy,(int)MarketInfo(Pair[ModePair],MODE_DIGITS)),SL,TP,Expire,clrBlue);
                        //---
                        if(ModifyBuyStop==true)
                          {
                           CountTickBuyStop[ModePair]=0;
                           break;
                          }
                        //---
                        if(TryCnt==3)
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": Could not modify, ticket: "+DoubleToStr(OrderTicket(),0));
                           break;
                          }
                        else
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again modify order - "+DoubleToStr(OrderTicket(),0));
                           RefreshRates();
                          }
                       }//End while(...
                    }
                 }
               //---Modify Sell Stop
               if((OrderType()==OP_SELLSTOP)&&(TypeOrder==OP_SELLSTOP))
                 {
                  //---Start count ticks
                  if((NormalizeDouble(PriceBid,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))<NormalizeDouble(OrderOpenPrice()+DistanceSell-StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))) ||
                     (NormalizeDouble(PriceBid,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))>NormalizeDouble(OrderOpenPrice()+DistanceSell+StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))))
                     CountTickSellStop[ModePair]++;
                  //---
                  DistanceSell=NormalizeDouble(PipsLevelPending*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  StepModify=NormalizeDouble(StepModifyPending*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  if(UseTakeProfit==true)
                     TP=NormalizeDouble((PriceBid-DistanceSell)-PipsProfits*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  if(UseStopLoss==true)
                     SL=NormalizeDouble((PriceAsk-DistanceSell)+PipsLoss*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  //---
                  if(((((NormalizeDouble(PriceBid,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))<NormalizeDouble(OrderOpenPrice()+DistanceSell-StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))) ||
                        (NormalizeDouble(PriceBid,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))>NormalizeDouble(OrderOpenPrice()+DistanceSell+StepModify,(int)MarketInfo(Pair[ModePair],MODE_DIGITS)))) &&
                       (NormalizeDouble(PriceBid-DistanceSell,(int)MarketInfo(Pair[ModePair],MODE_DIGITS))!=NormalizeDouble(OrderOpenPrice(),(int)MarketInfo(Pair[ModePair],MODE_DIGITS)))&&(CountTickSellStop[ModePair]>=DelayModifyPending))) ||
                     (OrderStopLoss()==0))
                    {
                     TryCnt=0;
                     while(true)
                       {
                        TryCnt++;
                        ModifySellStop=OrderModify(OrderTicket(),NormalizeDouble(PriceBid-DistanceSell,(int)MarketInfo(Pair[ModePair],MODE_DIGITS)),SL,TP,Expire,clrRed);
                        //---
                        if(ModifySellStop==true)
                          {
                           CountTickSellStop[ModePair]=0;
                           break;
                          }
                        //---
                        if(TryCnt==3)
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": Could not modify, ticket: "+DoubleToStr(OrderTicket(),0));
                           break;
                          }
                        else
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again modify order - "+DoubleToStr(OrderTicket(),0));
                           RefreshRates();
                          }
                       }//End while(...

                    }
                 }
              }
            //---------------------------------------------------------------------
            //Start trailing stop loss
            if(UseTralingStopLoss==true)
              {
               //---Modify Buy
               if((OrderType()==OP_BUY)&&(TypeOrder==OP_BUY))
                 {
                  if((OrderTakeProfit()==0)&&(UseTakeProfit==true))
                     TP=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_ASK)+PipsProfits*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  else
                     TP=OrderTakeProfit();
                  //---
                  if(UseBreakEven==true)
                    {
                     if((MarketInfo(Pair[ModePair],MODE_BID)-OrderOpenPrice())>((BreakEvenPips+BreakEVenAfter)*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)))
                        SL=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_BID)-(BreakEvenPips*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                    }
                  //---
                  if(UseBreakEven==false)
                    {
                     if((MarketInfo(Pair[ModePair],MODE_BID)-OrderOpenPrice())>(PipsLoss*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)))
                        SL=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_BID)-(PipsLoss*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                    }
                  //---
                  if((NormalizeDouble(OrderStopLoss(),(int)MarketInfo(Pair[ModePair],MODE_DIGITS))<NormalizeDouble(SL-(TrailingStopStep*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)),(int)MarketInfo(Pair[ModePair],MODE_DIGITS)))&&(SL!=0.0))
                    {
                     TryCnt=0;
                     while(true)
                       {
                        TryCnt++;
                        ModifyBuy=OrderModify(OrderTicket(),0,SL,TP,0,clrBlue);
                        //---
                        if(ModifyBuy==true)
                           break;
                        //---
                        if(TryCnt==3)
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": Could not modify, ticket: "+DoubleToStr(OrderTicket(),0));
                           break;
                          }
                        else
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again modify order - "+DoubleToStr(OrderTicket(),0));
                           RefreshRates();
                          }
                       }//End while(...
                    }
                 }
               //---Modify Sell
               if((OrderType()==OP_SELL)&&(TypeOrder==OP_SELL))
                 {
                  if((OrderTakeProfit()==0)&&(UseTakeProfit==true))
                     TP=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_BID)-PipsProfits*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                  else
                     TP=OrderTakeProfit();
                  //---
                  if(UseBreakEven==true)
                    {
                     if((OrderOpenPrice()-MarketInfo(Pair[ModePair],MODE_ASK))>((BreakEvenPips+BreakEVenAfter)*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)))
                        SL=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_ASK)+(BreakEvenPips*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                    }
                  //---
                  if(UseBreakEven==false)
                    {
                     if((OrderOpenPrice()-MarketInfo(Pair[ModePair],MODE_ASK))>(PipsLoss*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)))
                        SL=NormalizeDouble(MarketInfo(Pair[ModePair],MODE_ASK)+(PipsLoss*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)),(int)MarketInfo(Pair[ModePair],MODE_DIGITS));
                    }
                  //---
                  if((NormalizeDouble(OrderStopLoss(),(int)MarketInfo(Pair[ModePair],MODE_DIGITS))>NormalizeDouble(SL+(TrailingStopStep*(MarketInfo(Pair[ModePair],MODE_POINT)*MultiplierPoint)),(int)MarketInfo(Pair[ModePair],MODE_DIGITS)))&&(SL!=0.0))
                    {
                     TryCnt=0;
                     while(true)
                       {
                        TryCnt++;
                        ModifySell=OrderModify(OrderTicket(),0,SL,TP,0,clrRed);
                        //---
                        if(ModifySell==true)
                           break;
                        //---
                        if(TryCnt==3)
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": Could not modify, ticket: "+DoubleToStr(OrderTicket(),0));
                           break;
                          }
                        else
                          {
                           Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again modify order - "+DoubleToStr(OrderTicket(),0));
                           RefreshRates();
                          }
                       }//End while(...
                    }
                 }
              }
            //---------------------------------------------------------------------
            //Errors
            if((GetLastError()==1)||(GetLastError()==3)||(GetLastError()==130)||(GetLastError()==132)||(GetLastError()==133)||(GetLastError()==137)||(GetLastError()==4108)||(GetLastError()==4109))
              {
               Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives a error modify order");
               break;
              }
            //---------------------------------------------------------------------
            RefreshRates();
           }
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Close orders
//====================================================================================================================================================//
void CloseOrders(int OrdersType,int ModePair)
  {
//---------------------------------------------------------------------
   int TryCnt=0;
   bool WasOrderClosed;
   datetime StartTimeClose=TimeCurrent();
//---------------------------------------------------------------------
   for(i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         if((OrderSymbol()==Pair[ModePair])&&(OrderMagicNumber()==PairID[ModePair])&&(OrderMagicNumber()!=0))
           {
            //---------------------------------------------------------------------
            //Close buy
            if((OrderType()==OP_BUY)&&(OrdersType==OP_BUY))
              {
               TryCnt=0;
               WasOrderClosed=false;
               //---close order
               while(true)
                 {
                  TryCnt++;
                  WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(PriceBid,(int)MarketInfo(Pair[ModePair],MODE_DIGITS)),Slippage,clrMediumAquamarine);
                  if(WasOrderClosed>0)
                     break;
                  //---Errors
                  if((GetLastError()==1)||(GetLastError()==132)||(GetLastError()==133)||(GetLastError()==137)||(GetLastError()==4108)||(GetLastError()==4109))
                     break;
                  //---try 3 times to close
                  if(TryCnt==3)
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": Could not close, ticket: "+DoubleToStr(OrderTicket(),0));
                     break;
                    }
                  else
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again close order - "+DoubleToStr(OrderTicket(),0));
                     RefreshRates();
                    }
                 }//End while(...
              }//End if(OrderType()==OP_BUY)
            //---------------------------------------------------------------------
            //Close sell
            if((OrderType()==OP_SELL)&&(OrdersType==OP_SELL))
              {
               TryCnt=0;
               WasOrderClosed=false;
               //---close order
               while(true)
                 {
                  TryCnt++;
                  WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(PriceAsk,(int)MarketInfo(Pair[ModePair],MODE_DIGITS)),Slippage,clrDarkSalmon);
                  if(WasOrderClosed>0)
                     break;
                  //---Errors
                  if((GetLastError()==1)||(GetLastError()==132)||(GetLastError()==133)||(GetLastError()==137)||(GetLastError()==4108)||(GetLastError()==4109))
                     break;
                  //---try 3 times to close
                  if(TryCnt==3)
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": Could not close, ticket: "+DoubleToStr(OrderTicket(),0));
                     break;
                    }
                  else
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+"||"+ExpertName+": receives new data and try again close order - "+DoubleToStr(OrderTicket(),0));
                     RefreshRates();
                    }
                 }//End while(...
              }//End if(OrderType()==OP_SELL)
            //---------------------------------------------------------------------
           }//End if((OrderSymbol()...
        }//End OrderSelect(...
     }//End for(...
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Count orders
//====================================================================================================================================================//
void CountOrders()
  {
//---------------------------------------------------------------------
   ArrayInitialize(BuyOrders,0);
   ArrayInitialize(SellOrders,0);
   ArrayInitialize(BuyStopOrders,0);
   ArrayInitialize(SellStopOrders,0);
   ArrayInitialize(OpenMarketOrders,0);
   ArrayInitialize(OpenPendingOrders,0);
   ArrayInitialize(PriceOpenBuyStopOrder,0);
   ArrayInitialize(PriceOpenSellStopOrder,0);
   ArrayInitialize(ProfitLoss,0);
   ArrayInitialize(OrdesLots,0);
   TotalOpenOrders=0;
//---------------------------------------------------------------------
   for(i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         for(j=0; j<TotalPairs; j++)
           {
            if((OrderMagicNumber()==PairID[j])&&(OrderMagicNumber()!=0))
              {
               TotalOpenOrders++;
               if(OrderType()==OP_BUY)
                  BuyOrders[j]++;
               if(OrderType()==OP_SELL)
                  SellOrders[j]++;
               if(OrderType()==OP_BUYSTOP)
                 {
                  BuyStopOrders[j]++;
                  PriceOpenBuyStopOrder[j]=OrderOpenPrice();
                 }
               if(OrderType()==OP_SELLSTOP)
                 {
                  SellStopOrders[j]++;
                  PriceOpenSellStopOrder[j]=OrderOpenPrice();
                 }
               //---1
               if((j>=1)&&(j<=7))
                 {
                  OrdesLots[1]+=OrderLots();
                  ProfitLoss[1]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[1]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[1]++;
                 }
               //---2
               if((j>=8)&&(j<=14))
                 {
                  OrdesLots[2]+=OrderLots();
                  ProfitLoss[2]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[2]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[2]++;
                 }
               //---3
               if((j>=15)&&(j<=21))
                 {
                  OrdesLots[3]+=OrderLots();
                  ProfitLoss[3]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[3]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[3]++;
                 }
               //---4
               if((j>=22)&&(j<=28))
                 {
                  OrdesLots[4]+=OrderLots();
                  ProfitLoss[4]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[4]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[4]++;
                 }
               //---5
               if((j>=29)&&(j<=35))
                 {
                  OrdesLots[5]+=OrderLots();
                  ProfitLoss[5]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[5]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[5]++;
                 }
               //---6
               if((j>=36)&&(j<=42))
                 {
                  OrdesLots[6]+=OrderLots();
                  ProfitLoss[6]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[6]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[6]++;
                 }
               //---7
               if((j>=43)&&(j<=49))
                 {
                  OrdesLots[7]+=OrderLots();
                  ProfitLoss[7]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[7]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[7]++;
                 }
               //---8
               if((j>=50)&&(j<=56))
                 {
                  OrdesLots[8]+=OrderLots();
                  ProfitLoss[8]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[8]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[8]++;
                 }
               //---9
               if((j>=57)&&(j<=59))
                 {
                  OrdesLots[9]+=OrderLots();
                  ProfitLoss[9]+=OrderProfit()+OrderCommission()+OrderSwap();
                  if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                     OpenMarketOrders[9]++;
                  if((OrderType()==OP_BUYSTOP)||(OrderType()==OP_SELLSTOP))
                     OpenPendingOrders[9]++;
                 }
               //---
              }
           }
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Count history results
//====================================================================================================================================================//
void HistoryResults()
  {
//---------------------------------------------------------------------
   HistoryTrades=0;
   HistoryProfitLoss=0;
   ArrayInitialize(LastTradeTime,0);
   ArrayInitialize(LastTradeProfitLoss,0);
   ArrayInitialize(LastTradeType,-1);
   ArrayInitialize(LastTradeLot,0);
   ArrayInitialize(TotalHistoryOrders,0);
   ArrayInitialize(TotalHistoryProfit,0);
   ArrayInitialize(ResultsCurrencies,0);
//---------------------------------------------------------------------
   for(i=0; i<OrdersHistoryTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         for(j=0; j<TotalPairs; j++)
           {
            if((OrderMagicNumber()==PairID[j])&&(OrderMagicNumber()!=0))
              {
               HistoryProfitLoss+=OrderProfit()+OrderCommission()+OrderSwap();
               if((OrderType()==OP_BUY)||(OrderType()==OP_SELL))
                 {
                  HistoryTrades++;
                  LastTradeTime[j]=OrderOpenTime();
                  LastTradeProfitLoss[j]=OrderProfit()+OrderCommission()+OrderSwap();
                  LastTradeType[j]=OrderType();
                  LastTradeLot[j]=OrderLots();
                  TotalHistoryOrders[j]++;
                  TotalHistoryProfit[j]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---1
               if((j>=1)&&(j<=7))
                 {
                  ResultsCurrencies[1]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---2
               if((j>=8)&&(j<=14))
                 {
                  ResultsCurrencies[2]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---3
               if((j>=15)&&(j<=21))
                 {
                  ResultsCurrencies[3]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---4
               if((j>=22)&&(j<=28))
                 {
                  ResultsCurrencies[4]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---5
               if((j>=29)&&(j<=35))
                 {
                  ResultsCurrencies[5]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---6
               if((j>=36)&&(j<=42))
                 {
                  ResultsCurrencies[6]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---7
               if((j>=43)&&(j<=49))
                 {
                  ResultsCurrencies[7]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---8
               if((j>=50)&&(j<=56))
                 {
                  ResultsCurrencies[8]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---9
               if((j>=57)&&(j<=59))
                 {
                  ResultsCurrencies[9]+=OrderProfit()+OrderCommission()+OrderSwap();
                 }
               //---
              }
           }
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Lot size
//====================================================================================================================================================//
double CalcLots(int ModePair)
  {
//---------------------------------------------------------------------
   double LotSize=0;
   string SymbolUse=Symbol();
//---------------------------------------------------------------------
   if((!IsTesting())&&(!IsOptimization())&&(!IsVisualMode()))
      SymbolUse=Pair[ModePair];//Bug of terminal
//---------------------------------------------------------------------
   if(MoneyManagement==true)
      LotSize=(AccountBalance()/MarketInfo(SymbolUse,MODE_LOTSIZE))*RiskFactor;
   if(MoneyManagement==false)
      LotSize=ManualLotSize;
//---------------------------------------------------------------------
   if(IsConnected())
      return(MathMin(MathMax((MathRound(LotSize/MarketInfo(SymbolUse,MODE_LOTSTEP))*MarketInfo(SymbolUse,MODE_LOTSTEP)),MarketInfo(SymbolUse,MODE_MINLOT)),MarketInfo(SymbolUse,MODE_MAXLOT)));
   else
      return(LotSize);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Clear chart
//====================================================================================================================================================//
void ClearChart()
  {
//---------------------------------------------------------------------
   for(i=ObjectsTotal()-1; i>=0; i--)
     {
      if((ObjectName(i)!="Background")&&(StringSubstr(ObjectName(i),0,4)!="Text"))
         ObjectDelete(ObjectName(i));
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Comment's background
//====================================================================================================================================================//
void ChartBackground(string StringName,color ImageColor,int Xposition,int Yposition,int Xsize,int Ysize)
  {
//---------------------------------------------------------------------
   if(ObjectFind(0,StringName)==-1)
     {
      ObjectCreate(0,StringName,OBJ_RECTANGLE_LABEL,0,0,0,0,0);
      ObjectSetInteger(0,StringName,OBJPROP_XDISTANCE,Xposition);
      ObjectSetInteger(0,StringName,OBJPROP_YDISTANCE,Yposition);
      ObjectSetInteger(0,StringName,OBJPROP_XSIZE,Xsize);
      ObjectSetInteger(0,StringName,OBJPROP_YSIZE,Ysize);
      ObjectSetInteger(0,StringName,OBJPROP_BGCOLOR,ImageColor);
      ObjectSetInteger(0,StringName,OBJPROP_BORDER_TYPE,BORDER_FLAT);
      ObjectSetInteger(0,StringName,OBJPROP_BORDER_COLOR,clrBlack);
      ObjectSetInteger(0,StringName,OBJPROP_BACK,false);
      ObjectSetInteger(0,StringName,OBJPROP_SELECTABLE,false);
      ObjectSetInteger(0,StringName,OBJPROP_SELECTED,false);
      ObjectSetInteger(0,StringName,OBJPROP_HIDDEN,true);
      ObjectSetInteger(0,StringName,OBJPROP_ZORDER,0);
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Display text
//====================================================================================================================================================//
void DisplayText(string StringName,string Image,int FontSize,string FontType,color FontColor,int Xposition,int Yposition)
  {
//---------------------------------------------------------------------
   ObjectCreate(StringName,OBJ_LABEL,0,0,0);
   ObjectSet(StringName,OBJPROP_CORNER,0);
   ObjectSet(StringName,OBJPROP_BACK,FALSE);
   ObjectSet(StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSet(StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSet(StringName,OBJPROP_HIDDEN,TRUE);
   ObjectSetText(StringName,Image,FontSize,FontType,FontColor);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Buttons Panel
//====================================================================================================================================================//
void ButtonsPanel(string NameObject, string NameButton, int Xdistance, int Ydistanc, color ColorButton)
  {
//------------------------------------------------------
   if(ObjectFind(0,StringConcatenate(NameObject))==-1)
     {
      ObjectCreate(0,NameObject,OBJ_BUTTON,0,0,0);
      ObjectSetInteger(0,NameObject,OBJPROP_CORNER,0);
      ObjectSetInteger(0,NameObject,OBJPROP_XDISTANCE,Xdistance);
      ObjectSetInteger(0,NameObject,OBJPROP_YDISTANCE,Ydistanc);
      ObjectSetInteger(0,NameObject,OBJPROP_XSIZE,100);
      ObjectSetInteger(0,NameObject,OBJPROP_YSIZE,25);
      ObjectSetInteger(0,NameObject,OBJPROP_BGCOLOR,ColorButton);
      ObjectSetInteger(0,NameObject,OBJPROP_STATE,false);
      ObjectSetString(0,NameObject,OBJPROP_FONT,"Tahoma");
      ObjectSetInteger(0,NameObject,OBJPROP_FONTSIZE,10);
      ObjectSetInteger(0,NameObject,OBJPROP_COLOR,ColorFontButton);
      ObjectSetInteger(0,NameObject,OBJPROP_SELECTABLE,0);
      ObjectSetInteger(0,NameObject,OBJPROP_HIDDEN,1);
      ObjectSetString(0,NameObject,OBJPROP_TEXT,NameButton);
     }
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Comment in chart
//====================================================================================================================================================//
void CommentScreen()
  {
//---------------------------------------------------------------------
   string MMstring="";
   string ImpactPrev="";
   string ImpactNext="";
   string ImpactTrade="";
   string Settings="";
   string StrategyUse="";
   SetBuffers=0;
//---------------------------------------------------------------------
//Delete objects to refresh
   if(ObjectFind("Text2")>-1)
      ObjectDelete("Text2");
   if(ObjectFind("Text10")>-1)
      ObjectDelete("Text10");
   if(ObjectFind("Text11")>-1)
      ObjectDelete("Text11");
   if(ObjectFind("Text12")>-1)
      ObjectDelete("Text12");
   if(ObjectFind("Text14")>-1)
      ObjectDelete("Text14");
   if(ObjectFind("Text15")>-1)
      ObjectDelete("Text15");
   if(ObjectFind("Text16")>-1)
      ObjectDelete("Text16");
   if(ObjectFind("Text18")>-1)
      ObjectDelete("Text18");
   if(ObjectFind("Text19")>-1)
      ObjectDelete("Text19");
   if(ObjectFind("Text20")>-1)
      ObjectDelete("Text20");
   if(ObjectFind("Text22")>-1)
      ObjectDelete("Text22");
   if(ObjectFind("Text23")>-1)
      ObjectDelete("Text23");
   if(ObjectFind("Text24")>-1)
      ObjectDelete("Text24");
   if(ObjectFind("Text26")>-1)
      ObjectDelete("Text26");
   if(ObjectFind("Text27")>-1)
      ObjectDelete("Text27");
   if(ObjectFind("Text28")>-1)
      ObjectDelete("Text28");
   if(ObjectFind("Text30")>-1)
      ObjectDelete("Text30");
   if(ObjectFind("Text31")>-1)
      ObjectDelete("Text31");
   if(ObjectFind("Text32")>-1)
      ObjectDelete("Text32");
   if(ObjectFind("Text34")>-1)
      ObjectDelete("Text34");
   if(ObjectFind("Text35")>-1)
      ObjectDelete("Text35");
   if(ObjectFind("Text36")>-1)
      ObjectDelete("Text36");
   if(ObjectFind("Text38")>-1)
      ObjectDelete("Text38");
   if(ObjectFind("Text39")>-1)
      ObjectDelete("Text39");
   if(ObjectFind("Text40")>-1)
      ObjectDelete("Text40");
   if(ObjectFind("Text42")>-1)
      ObjectDelete("Text42");
   if(ObjectFind("Text43")>-1)
      ObjectDelete("Text43");
   if(ObjectFind("Text44")>-1)
      ObjectDelete("Text44");
   if(ObjectFind("Text45")>-1)
      ObjectDelete("Text45");
//---------------------------------------------------------------------
//Set strategy comments
   if(StrategyToUse==0)
      StrategyUse="Custom_Stategy";
   if(StrategyToUse==1)
      StrategyUse="Recovery Orders";
   if(StrategyToUse==2)
      StrategyUse="Basket Orders";
   if(StrategyToUse==3)
      StrategyUse="Separate Orders";
   if(StrategyToUse==4)
      StrategyUse="Replace Orders";
//---------------------------------------------------------------------
//Set impact news comments
   if(ImpactToTrade==0)
      ImpactTrade="Low-Medium-High";
   if(ImpactToTrade==1)
      ImpactTrade="Medium - High";
   if(ImpactToTrade==2)
      ImpactTrade="Only High";
//---------------------------------------------------------------------
//Set impact info
   for(i=0; i<10; i++)
     {
      if(ExtBufferImpact[i][1]==-1)
         ShowImpact[i]="NONE";
      if(ExtBufferImpact[i][1]==0)
         ShowImpact[i]="Low";
      if(ExtBufferImpact[i][1]==1)
         ShowImpact[i]="Medium";
      if(ExtBufferImpact[i][1]==2)
         ShowImpact[i]="High";
      //---------------------------------------------------------------------
      //Set time info
      if(ShowInfoTime==0)
        {
         if(ExtBufferSeconds[i][0]!=-9999)
            ShowSecondsSince[i]=DoubleToStr(ExtBufferSeconds[i][0]/60,0);
         else
            ShowSecondsSince[i]="NONE";
         if(ExtBufferSeconds[i][1]!=9999)
            ShowSecondsUntil[i]=DoubleToStr(ExtBufferSeconds[i][1]/60,0);
         else
            ShowSecondsUntil[i]="NONE";
        }
      //---
      if(ShowInfoTime==1)
        {
         if(ExtBufferSeconds[i][0]!=-9999)
           {
            if(ExtBufferSeconds[i][0]/60<60)
               ShowSecondsSince[i]=DoubleToStr(0,0)+"/"+DoubleToStr(0,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][0]/60),0);//Minutes
            if(MathRound((int)(ExtBufferSeconds[i][0]/60/60))<24)
               ShowSecondsSince[i]=DoubleToStr(0,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][0]/60/60),0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][0]/60)%60,0);//Hours and Minutes
            if(MathRound((int)(ExtBufferSeconds[i][0]/60/60))>=24)
               ShowSecondsSince[i]=DoubleToStr((int)(ExtBufferSeconds[i][0]/60/60)/24,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][0]/60/60)%24,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][0]/60)%60,0);//Days and Hours and Minutes
           }
         else
            ShowSecondsSince[i]="NONE";
         //---
         if(ExtBufferSeconds[i][1]!=9999)
           {
            if(ExtBufferSeconds[i][1]/60<60)
               ShowSecondsUntil[i]=DoubleToStr(0,0)+"/"+DoubleToStr(0,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][1]/60),0);//Minutes
            if(MathRound((int)(ExtBufferSeconds[i][1]/60/60))<24)
               ShowSecondsUntil[i]=DoubleToStr(0,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][1]/60/60),0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][1]/60)%60,0);//Hours and Minutes
            if(MathRound((int)(ExtBufferSeconds[i][1]/60/60))>=24)
               ShowSecondsUntil[i]=DoubleToStr((int)(ExtBufferSeconds[i][1]/60/60)/24,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][1]/60/60)%24,0)+"/"+DoubleToStr((int)(ExtBufferSeconds[i][1]/60)%60,0);//Days and Hours and Minutes
           }
         else
            ShowSecondsUntil[i]="NONE";
        }
     }
//---------------------------------------------------------------------
//String money management
   if(MoneyManagement==true)
      MMstring="Auto";
   if(MoneyManagement==false)
      MMstring="Manual";
//---------------------------------------------------------------------
//Comment in chart
   if(ObjectFind("Text1")==-1)
      DisplayText("Text1",WindowExpertName(),TextFontSizeTitle,TextFontTypeTitle,clrGray,10,DistanceText[1]-6);
   if(ObjectFind("Text2")==-1)
      DisplayText("Text2","Time: "+TimeToStr(CurrentTime)+"  (GMToffset: "+IntegerToString(GMT_OffsetHours)+")",TextFontSize,TextFontType,TextColor1,10,DistanceText[2]);
   if(ObjectFind("Text3")==-1)
      DisplayText("Text3","Money Management: "+MMstring+"||Lot: "+DoubleToStr(CalcLots(1),2),TextFontSize,TextFontType,TextColor1,10,DistanceText[3]);
   if(ObjectFind("Text4")==-1)
      DisplayText("Text4","Strategy To Use: "+StrategyUse,TextFontSize,TextFontType,TextColor1,10,DistanceText[4]);
   if(ObjectFind("Text5")==-1)
      DisplayText("Text5","Event Impact Trade: "+ImpactTrade,TextFontSize,TextFontType,TextColor1,10,DistanceText[5]);
   if(ObjectFind("Text6")==-1)
      DisplayText("Text6","Start Trade Before Event: "+IntegerToString(MinutesBeforeNewsStart)+" minutes",TextFontSize,TextFontType,TextColor1,10,DistanceText[6]);
   if(ObjectFind("Text7")==-1)
      DisplayText("Text7","Stop  Trade  After  Event: "+IntegerToString(MinutesAfterNewsStop)+" minutes",TextFontSize,TextFontType,TextColor1,10,DistanceText[7]);
   if(ObjectFind("Text8")==-1)
      DisplayText("Text8","Currency Impact   TimeUntil  TimeSince",TextFontSize,TextFontType,TextColor4,10,DistanceText[8]);
//---------------------------------------------------------------------
//---EUR
   SetBuffers=1;
   if(ObjectFind("Text9")==-1)
      DisplayText("Text9","  EUR ",TextFontSize,TextFontType,TextColor3,10,DistanceText[9]);
   if(OpenSession[SetBuffers]==true)
     {
      if(EUR_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text10")==-1)
            DisplayText("Text10",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[9]);
         if(ObjectFind("Text11")==-1)
            DisplayText("Text11",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[9]);
         if(ObjectFind("Text12")==-1)
            DisplayText("Text12",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[9]);
        }
      else
        {
         if(ObjectFind("Text10")==-1)
            DisplayText("Text10","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[9]);
         if(ObjectFind("Text11")==-1)
            DisplayText("Text11","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[9]);
         if(ObjectFind("Text12")==-1)
            DisplayText("Text12","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[9]);
        }
     }
   else
     {
      if(ObjectFind("Text10")==-1)
         DisplayText("Text10","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[9]);
      if(ObjectFind("Text11")==-1)
         DisplayText("Text11","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[9]);
      if(ObjectFind("Text12")==-1)
         DisplayText("Text12","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[9]);
     }
//---------------------------------------------------------------------
//---GBP
   SetBuffers=2;
   if(ObjectFind("Text13")==-1)
      DisplayText("Text13","  GBP ",TextFontSize,TextFontType,TextColor3,10,DistanceText[10]);
   if(OpenSession[SetBuffers]==true)
     {
      if(GBP_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text14")==-1)
            DisplayText("Text14",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[10]);
         if(ObjectFind("Text15")==-1)
            DisplayText("Text15",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[10]);
         if(ObjectFind("Text16")==-1)
            DisplayText("Text16",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[10]);
        }
      else
        {
         if(ObjectFind("Text14")==-1)
            DisplayText("Text14","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[10]);
         if(ObjectFind("Text15")==-1)
            DisplayText("Text15","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[10]);
         if(ObjectFind("Text16")==-1)
            DisplayText("Text16","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[10]);
        }
     }
   else
     {
      if(ObjectFind("Text14")==-1)
         DisplayText("Text14","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[10]);
      if(ObjectFind("Text15")==-1)
         DisplayText("Text15","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[10]);
      if(ObjectFind("Text16")==-1)
         DisplayText("Text16","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[10]);
     }
//---------------------------------------------------------------------
//---AUD
   SetBuffers=3;
   if(ObjectFind("Text17")==-1)
      DisplayText("Text17","  AUD ",TextFontSize,TextFontType,TextColor3,10,DistanceText[11]);
   if(OpenSession[SetBuffers]==true)
     {
      if(AUD_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text18")==-1)
            DisplayText("Text18",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[11]);
         if(ObjectFind("Text19")==-1)
            DisplayText("Text19",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[11]);
         if(ObjectFind("Text20")==-1)
            DisplayText("Text20",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[11]);
        }
      else
        {
         if(ObjectFind("Text18")==-1)
            DisplayText("Text18","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[11]);
         if(ObjectFind("Text19")==-1)
            DisplayText("Text19","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[11]);
         if(ObjectFind("Text20")==-1)
            DisplayText("Text20","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[11]);
        }
     }
   else
     {
      if(ObjectFind("Text18")==-1)
         DisplayText("Text18","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[11]);
      if(ObjectFind("Text19")==-1)
         DisplayText("Text19","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[11]);
      if(ObjectFind("Text20")==-1)
         DisplayText("Text20","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[11]);
     }
//---------------------------------------------------------------------
//---NZD
   SetBuffers=4;
   if(ObjectFind("Text21")==-1)
      DisplayText("Text21","  NZD ",TextFontSize,TextFontType,TextColor3,10,DistanceText[12]);
   if(OpenSession[SetBuffers]==true)
     {
      if(NZD_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text22")==-1)
            DisplayText("Text22",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[12]);
         if(ObjectFind("Text23")==-1)
            DisplayText("Text23",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[12]);
         if(ObjectFind("Text24")==-1)
            DisplayText("Text24",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[12]);
        }
      else
        {
         if(ObjectFind("Text22")==-1)
            DisplayText("Text22","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[12]);
         if(ObjectFind("Text23")==-1)
            DisplayText("Text23","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[12]);
         if(ObjectFind("Text24")==-1)
            DisplayText("Text24","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[12]);
        }
     }
   else
     {
      if(ObjectFind("Text22")==-1)
         DisplayText("Text22","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[12]);
      if(ObjectFind("Text23")==-1)
         DisplayText("Text23","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[12]);
      if(ObjectFind("Text24")==-1)
         DisplayText("Text24","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[12]);
     }
//---------------------------------------------------------------------
//---USD
   SetBuffers=5;
   if(ObjectFind("Text25")==-1)
      DisplayText("Text25","  USD ",TextFontSize,TextFontType,TextColor3,10,DistanceText[13]);
   if(OpenSession[SetBuffers]==true)
     {
      if(USD_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text26")==-1)
            DisplayText("Text26",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[13]);
         if(ObjectFind("Text27")==-1)
            DisplayText("Text27",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[13]);
         if(ObjectFind("Text28")==-1)
            DisplayText("Text28",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[13]);
        }
      else
        {
         if(ObjectFind("Text26")==-1)
            DisplayText("Text26","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[13]);
         if(ObjectFind("Text27")==-1)
            DisplayText("Text27","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[13]);
         if(ObjectFind("Text28")==-1)
            DisplayText("Text28","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[13]);
        }
     }
   else
     {
      if(ObjectFind("Text26")==-1)
         DisplayText("Text26","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[13]);
      if(ObjectFind("Text27")==-1)
         DisplayText("Text27","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[13]);
      if(ObjectFind("Text28")==-1)
         DisplayText("Text28","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[13]);
     }
//---------------------------------------------------------------------
//---CAD
   SetBuffers=6;
   if(ObjectFind("Text29")==-1)
      DisplayText("Text29","  CAD ",TextFontSize,TextFontType,TextColor3,10,DistanceText[14]);
   if(OpenSession[SetBuffers]==true)
     {
      if(CAD_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text30")==-1)
            DisplayText("Text30",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[14]);
         if(ObjectFind("Text31")==-1)
            DisplayText("Text31",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[14]);
         if(ObjectFind("Text32")==-1)
            DisplayText("Text32",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[14]);
        }
      else
        {
         if(ObjectFind("Text30")==-1)
            DisplayText("Text30","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[14]);
         if(ObjectFind("Text31")==-1)
            DisplayText("Text31","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[14]);
         if(ObjectFind("Text32")==-1)
            DisplayText("Text32","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[14]);
        }
     }
   else
     {
      if(ObjectFind("Text30")==-1)
         DisplayText("Text30","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[14]);
      if(ObjectFind("Text31")==-1)
         DisplayText("Text31","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[14]);
      if(ObjectFind("Text32")==-1)
         DisplayText("Text32","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[14]);
     }
//---------------------------------------------------------------------
//---CHF
   SetBuffers=7;
   if(ObjectFind("Text33")==-1)
      DisplayText("Text33","  CHF ",TextFontSize,TextFontType,TextColor3,10,DistanceText[15]);
   if(OpenSession[SetBuffers]==true)
     {
      if(CHF_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text34")==-1)
            DisplayText("Text34",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[15]);
         if(ObjectFind("Text35")==-1)
            DisplayText("Text35",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[15]);
         if(ObjectFind("Text36")==-1)
            DisplayText("Text36",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[15]);
        }
      else
        {
         if(ObjectFind("Text34")==-1)
            DisplayText("Text34","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[15]);
         if(ObjectFind("Text35")==-1)
            DisplayText("Text35","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[15]);
         if(ObjectFind("Text36")==-1)
            DisplayText("Text36","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[15]);
        }
     }
   else
     {
      if(ObjectFind("Text34")==-1)
         DisplayText("Text34","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[15]);
      if(ObjectFind("Text35")==-1)
         DisplayText("Text35","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[15]);
      if(ObjectFind("Text36")==-1)
         DisplayText("Text36","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[15]);
     }
//---------------------------------------------------------------------
//---JPY
   SetBuffers=8;
   if(ObjectFind("Text37")==-1)
      DisplayText("Text37","  JPY ",TextFontSize,TextFontType,TextColor3,10,DistanceText[16]);
   if(OpenSession[SetBuffers]==true)
     {
      if(JPY_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text38")==-1)
            DisplayText("Text38",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[16]);
         if(ObjectFind("Text39")==-1)
            DisplayText("Text39",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[16]);
         if(ObjectFind("Text40")==-1)
            DisplayText("Text40",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[16]);
        }
      else
        {
         if(ObjectFind("Text38")==-1)
            DisplayText("Text38","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[16]);
         if(ObjectFind("Text39")==-1)
            DisplayText("Text39","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[16]);
         if(ObjectFind("Text40")==-1)
            DisplayText("Text40","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[16]);
        }
     }
   else
     {
      if(ObjectFind("Text38")==-1)
         DisplayText("Text38","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[16]);
      if(ObjectFind("Text39")==-1)
         DisplayText("Text39","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[16]);
      if(ObjectFind("Text40")==-1)
         DisplayText("Text40","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[16]);
     }
//---------------------------------------------------------------------
//---CNY
   SetBuffers=9;
   if(ObjectFind("Text41")==-1)
      DisplayText("Text41","  CNY ",TextFontSize,TextFontType,TextColor3,10,DistanceText[17]);
   if(OpenSession[SetBuffers]==true)
     {
      if(CNY_TradeInNewsRelease==1)
        {
         if(ObjectFind("Text42")==-1)
            DisplayText("Text42",ShowImpact[SetBuffers],TextFontSize,TextFontType,TextColor2,65,DistanceText[17]);
         if(ObjectFind("Text43")==-1)
            DisplayText("Text43",ShowSecondsUntil[SetBuffers],TextFontSize,TextFontType,TextColor2,125,DistanceText[17]);
         if(ObjectFind("Text44")==-1)
            DisplayText("Text44",ShowSecondsSince[SetBuffers],TextFontSize,TextFontType,TextColor2,190,DistanceText[17]);
        }
      else
        {
         if(ObjectFind("Text42")==-1)
            DisplayText("Text42","It's 'false'",TextFontSize,TextFontType,TextColor2,65,DistanceText[17]);
         if(ObjectFind("Text43")==-1)
            DisplayText("Text43","or not available",TextFontSize,TextFontType,TextColor2,120,DistanceText[17]);
         if(ObjectFind("Text44")==-1)
            DisplayText("Text44","     pair(s)",TextFontSize,TextFontType,TextColor2,190,DistanceText[17]);
        }
     }
   else
     {
      if(ObjectFind("Text42")==-1)
         DisplayText("Text42","Is out of",TextFontSize,TextFontType,TextColor2,65,DistanceText[17]);
      if(ObjectFind("Text43")==-1)
         DisplayText("Text43","session",TextFontSize,TextFontType,TextColor2,125,DistanceText[17]);
      if(ObjectFind("Text44")==-1)
         DisplayText("Text44","for now",TextFontSize,TextFontType,TextColor2,190,DistanceText[17]);
     }
//---------------------------------------------------------------------
//---History
   if(ObjectFind("Text45")==-1)
      DisplayText("Text45","History Results: "+DoubleToStr(HistoryProfitLoss,2)+"   ("+DoubleToStr(HistoryTrades,0)+")",TextFontSize,TextFontType,TextColor1,10,DistanceText[18]);
//---Lines
   if(ObjectFind("Text46")==-1)
      DisplayText("Text46","_____________________________________",TextFontSize,TextFontType,clrGray,0,DistanceText[1]);
   if(ObjectFind("Text47")==-1)
      DisplayText("Text47","_____________________________________",TextFontSize,TextFontType,clrGray,0,DistanceText[2]);
   if(ObjectFind("Text48")==-1)
      DisplayText("Text48","_____________________________________",TextFontSize,TextFontType,clrGray,0,DistanceText[3]);
   if(ObjectFind("Text49")==-1)
      DisplayText("Text49","_____________________________________",TextFontSize,TextFontType,clrGray,0,DistanceText[4]);
   if(ObjectFind("Text50")==-1)
      DisplayText("Text50","_____________________________________",TextFontSize,TextFontType,clrGray,0,DistanceText[5]);
   if(ObjectFind("Text51")==-1)
      DisplayText("Text51","_____________________________________",TextFontSize,TextFontType,clrGray,0,DistanceText[7]);
   if(ObjectFind("Text52")==-1)
      DisplayText("Text52","_____________________________________",TextFontSize,TextFontType,clrGray,0,DistanceText[17]);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Analyzer trades
//====================================================================================================================================================//
void AnalyzerTrades()
  {
//---------------------------------------------------------------------
//Set background
   for(i=0; i<34; i++)
     {
      color ColorLine1=ColorOfLine1;
      color ColorLine2=ColorOfLine2;
      //---
      if((i==0)||(i==4)||(i==8)||(i==12)||(i==16)||(i==20)||(i==24)||(i==28)||(i==32))
         ColorLine1=ColorLineTitles;
      //---Background1
      if(ObjectFind("BackgroundLine1"+IntegerToString(i))==-1)
         ChartBackground("BackgroundLine1"+IntegerToString(i),ColorLine1,EMPTY_VALUE,TRUE,265,2+(i*12*2),320,14);
      //---Background2
      if(ObjectFind("BackgroundLine2"+IntegerToString(i))==-1)
         ChartBackground("BackgroundLine2"+IntegerToString(i),ColorLine2,EMPTY_VALUE,TRUE,265,14+(i*12*2),320,14);
     }
//---------------------------------------------------------------------
//Set currency titles
   string CurrencyInfo[10]= {"","EUR","GBP","AUD","NZD","USD","CAD","CHF","JPY","CNY"};
//---
   for(i=1; i<10; i++)
     {
      if(ObjectFind("Str"+IntegerToString(i))==-1)
         DisplayText("Str"+IntegerToString(i),"RESULTS   FOR   CURRENCY   "+CurrencyInfo[i],SizeFontsOfInfo,"Arial Black",ColorOfTitle,265,(12*8*(i-1)));
      //---
      ObjectDelete("Res"+IntegerToString(i));
      if(ObjectFind("Res"+IntegerToString(i))==-1)
         DisplayText("Res"+IntegerToString(i),"("+DoubleToStr(ResultsCurrencies[i],2)+")",SizeFontsOfInfo,"Arial Black",ColorOfTitle,525,(12*8*(i-1)));
     }
//---------------------------------------------------------------------
//Set informations pairs'
   for(i=1; i<60; i++)
     {
      int SetPosition=i;
      if(SetPosition>=8)
         SetPosition+=1;
      if(SetPosition>=16)
         SetPosition+=1;
      if(SetPosition>=24)
         SetPosition+=1;
      if(SetPosition>=32)
         SetPosition+=1;
      if(SetPosition>=40)
         SetPosition+=1;
      if(SetPosition>=48)
         SetPosition+=1;
      if(SetPosition>=56)
         SetPosition+=1;
      if(SetPosition>=64)
         SetPosition+=1;
      //---
      if(ObjectFind("Comm1"+IntegerToString(i))==-1)
         DisplayText("Comm1"+IntegerToString(i),"Pair: "+Pair[i],SizeFontsOfInfo,"Arial",ColorOfInfo,265,(12*SetPosition));
      //---
      if(ObjectFind("Comm2"+IntegerToString(i))==-1)
         DisplayText("Comm2"+IntegerToString(i),"Orders: ",SizeFontsOfInfo,"Arial",ColorOfInfo,375,(12*SetPosition));
      //---
      if(ObjectFind("Comm3"+IntegerToString(i))==-1)
         DisplayText("Comm3"+IntegerToString(i),"Profit/Loss: ",SizeFontsOfInfo,"Arial",ColorOfInfo,455,(12*SetPosition));
      //---
      ObjectDelete("Comm4"+IntegerToString(i));
      if(ObjectFind("Comm4"+IntegerToString(i))==-1)
         DisplayText("Comm4"+IntegerToString(i),IntegerToString(TotalHistoryOrders[i]),SizeFontsOfInfo,"Arial",ColorOfInfo,422,(12*SetPosition));
      //---
      ObjectDelete("Comm5"+IntegerToString(i));
      if(ObjectFind("Comm5"+IntegerToString(i))==-1)
         DisplayText("Comm5"+IntegerToString(i),DoubleToStr(TotalHistoryProfit[i],2),SizeFontsOfInfo,"Arial",ColorOfInfo,525,(12*SetPosition));
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Background for comments
//====================================================================================================================================================//
void ChartBackground(string StringName,color ImageColor,int TypeBorder,bool InBackGround,int Xposition,int Yposition,int Xsize,int Ysize)
  {
//---------------------------------------------------------------------
   ObjectCreate(0,StringName,OBJ_RECTANGLE_LABEL,0,0,0,0,0);
   ObjectSetInteger(0,StringName,OBJPROP_XDISTANCE,Xposition);
   ObjectSetInteger(0,StringName,OBJPROP_YDISTANCE,Yposition);
   ObjectSetInteger(0,StringName,OBJPROP_XSIZE,Xsize);
   ObjectSetInteger(0,StringName,OBJPROP_YSIZE,Ysize);
   ObjectSetInteger(0,StringName,OBJPROP_BGCOLOR,ImageColor);
   ObjectSetInteger(0,StringName,OBJPROP_BORDER_TYPE,TypeBorder);
   ObjectSetInteger(0,StringName,OBJPROP_BORDER_COLOR,clrBlack);
   ObjectSetInteger(0,StringName,OBJPROP_BACK,InBackGround);
   ObjectSetInteger(0,StringName,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,StringName,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,StringName,OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,StringName,OBJPROP_ZORDER,0);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Orders signals
//====================================================================================================================================================//
void GetSignal()
  {
//---------------------------------------------------------------------
//Reset values
   SetBuffers=0;
   CheckOrdersBaseNews=false;
   TimeToTrade_EUR=false;
   TimeToTrade_GBP=false;
   TimeToTrade_AUD=false;
   TimeToTrade_NZD=false;
   TimeToTrade_USD=false;
   TimeToTrade_CAD=false;
   TimeToTrade_CHF=false;
   TimeToTrade_JPY=false;
   TimeToTrade_CNY=false;
//---------------------------------------------------------------------
//Trade immediately from buttons
   if((EUR_TradeInNewsRelease==2)&&(Open_EUR==true))
      TimeToTrade_EUR=true;
   if((GBP_TradeInNewsRelease==2)&&(Open_GBP==true))
      TimeToTrade_GBP=true;
   if((AUD_TradeInNewsRelease==2)&&(Open_AUD==true))
      TimeToTrade_AUD=true;
   if((NZD_TradeInNewsRelease==2)&&(Open_NZD==true))
      TimeToTrade_NZD=true;
   if((USD_TradeInNewsRelease==2)&&(Open_USD==true))
      TimeToTrade_USD=true;
   if((CAD_TradeInNewsRelease==2)&&(Open_CAD==true))
      TimeToTrade_CAD=true;
   if((CHF_TradeInNewsRelease==2)&&(Open_CHF==true))
      TimeToTrade_CHF=true;
   if((JPY_TradeInNewsRelease==2)&&(Open_JPY==true))
      TimeToTrade_JPY=true;
   if((CNY_TradeInNewsRelease==2)&&(Open_CNY==true))
      TimeToTrade_CNY=true;
//---------------------------------------------------------------------
//Call ReadNews() to make file
   if((((Minute()!=iPrevMinute)||(LoopTimes<2))&&(ModeReadNews==1))||(ModeReadNews==0)||(StartOperations==false))
     {
      if(LoopTimes<2)
        {
         LoopTimes++;
         ReadNews(0,"XXX");
        }
      //---------------------------------------------------------------------
      //Start check
      if(FileIsOk==true)
        {
         CheckOrdersBaseNews=true;
         //---------------------------------------------------------------------
         if(EUR_TradeInNewsRelease==1)
           {
            SetBuffers=1;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(EUR_TimeStartSession)==StringToTime(EUR_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(EUR_TimeStartSession)<StringToTime(EUR_TimeEndSession))&&((CurrentTime>=StringToTime(EUR_TimeStartSession))&&(CurrentTime<StringToTime(EUR_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(EUR_TimeStartSession)>StringToTime(EUR_TimeEndSession))&&((CurrentTime>=StringToTime(EUR_TimeStartSession))||(CurrentTime<StringToTime(EUR_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"EUR");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_EUR=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_EUR=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_EUR=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_EUR=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_EUR>=ImpactToTrade)&&(SecondsToNews_EUR<=SecondsBeforeNewsStart))||((ImpactSinceNews_EUR>=ImpactToTrade)&&(SecondsSinceNews_EUR<=SecondsAfterNewsStop)))
                  TimeToTrade_EUR=true;
               if((SecondsToNews_EUR==0)||(SecondsSinceNews_EUR==0))
                  TimeToTrade_EUR=true;
               if((TimeToTrade_EUR==true)&&(ImpactToNews_EUR>=ImpactToTrade)&&(SecondsToNews_EUR<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(GBP_TradeInNewsRelease==1)
           {
            SetBuffers=2;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(GBP_TimeStartSession)==StringToTime(GBP_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(GBP_TimeStartSession)<StringToTime(GBP_TimeEndSession))&&((CurrentTime>=StringToTime(GBP_TimeStartSession))&&(CurrentTime<StringToTime(GBP_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(GBP_TimeStartSession)>StringToTime(GBP_TimeEndSession))&&((CurrentTime>=StringToTime(GBP_TimeStartSession))||(CurrentTime<StringToTime(GBP_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"GBP");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_GBP=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_GBP=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_GBP=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_GBP=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_GBP>=ImpactToTrade)&&(SecondsToNews_GBP<=SecondsBeforeNewsStart))||((ImpactSinceNews_GBP>=ImpactToTrade)&&(SecondsSinceNews_GBP<=SecondsAfterNewsStop)))
                  TimeToTrade_GBP=true;
               if((SecondsToNews_GBP==0)||(SecondsSinceNews_GBP==0))
                  TimeToTrade_GBP=true;
               if((TimeToTrade_GBP==true)&&(ImpactToNews_GBP>=ImpactToTrade)&&(SecondsToNews_GBP<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(AUD_TradeInNewsRelease==1)
           {
            SetBuffers=3;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(AUD_TimeStartSession)==StringToTime(AUD_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(AUD_TimeStartSession)<StringToTime(AUD_TimeEndSession))&&((CurrentTime>=StringToTime(AUD_TimeStartSession))&&(CurrentTime<StringToTime(AUD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(AUD_TimeStartSession)>StringToTime(AUD_TimeEndSession))&&((CurrentTime>=StringToTime(AUD_TimeStartSession))||(CurrentTime<StringToTime(AUD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"AUD");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_AUD=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_AUD=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_AUD=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_AUD=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_AUD>=ImpactToTrade)&&(SecondsToNews_AUD<=SecondsBeforeNewsStart))||((ImpactSinceNews_AUD>=ImpactToTrade)&&(SecondsSinceNews_AUD<=SecondsAfterNewsStop)))
                  TimeToTrade_AUD=true;
               if((SecondsToNews_AUD==0)||(SecondsSinceNews_AUD==0))
                  TimeToTrade_AUD=true;
               if((TimeToTrade_AUD==true)&&(ImpactToNews_AUD>=ImpactToTrade)&&(SecondsToNews_AUD<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(NZD_TradeInNewsRelease==1)
           {
            SetBuffers=4;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(NZD_TimeStartSession)==StringToTime(NZD_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(NZD_TimeStartSession)<StringToTime(NZD_TimeEndSession))&&((CurrentTime>=StringToTime(NZD_TimeStartSession))&&(CurrentTime<StringToTime(NZD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(NZD_TimeStartSession)>StringToTime(NZD_TimeEndSession))&&((CurrentTime>=StringToTime(NZD_TimeStartSession))||(CurrentTime<StringToTime(NZD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"NZD");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_NZD=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_NZD=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_NZD=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_NZD=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_NZD>=ImpactToTrade)&&(SecondsToNews_NZD<=SecondsBeforeNewsStart))||((ImpactSinceNews_NZD>=ImpactToTrade)&&(SecondsSinceNews_NZD<=SecondsAfterNewsStop)))
                  TimeToTrade_NZD=true;
               if((SecondsToNews_NZD==0)||(SecondsSinceNews_NZD==0))
                  TimeToTrade_NZD=true;
               if((TimeToTrade_NZD==true)&&(ImpactToNews_NZD>=ImpactToTrade)&&(SecondsToNews_NZD<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(USD_TradeInNewsRelease==1)
           {
            SetBuffers=5;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(USD_TimeStartSession)==StringToTime(USD_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(USD_TimeStartSession)<StringToTime(USD_TimeEndSession))&&((CurrentTime>=StringToTime(USD_TimeStartSession))&&(CurrentTime<StringToTime(USD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(USD_TimeStartSession)>StringToTime(USD_TimeEndSession))&&((CurrentTime>=StringToTime(USD_TimeStartSession))||(CurrentTime<StringToTime(USD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"USD");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_USD=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_USD=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_USD=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_USD=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_USD>=ImpactToTrade)&&(SecondsToNews_USD<=SecondsBeforeNewsStart))||((ImpactSinceNews_USD>=ImpactToTrade)&&(SecondsSinceNews_USD<=SecondsAfterNewsStop)))
                  TimeToTrade_USD=true;
               if((SecondsToNews_USD==0)||(SecondsSinceNews_USD==0))
                  TimeToTrade_USD=true;
               if((TimeToTrade_USD==true)&&(ImpactToNews_USD>=ImpactToTrade)&&(SecondsToNews_USD<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(CAD_TradeInNewsRelease==1)
           {
            SetBuffers=6;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(CAD_TimeStartSession)==StringToTime(CAD_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(CAD_TimeStartSession)<StringToTime(CAD_TimeEndSession))&&((CurrentTime>=StringToTime(CAD_TimeStartSession))&&(CurrentTime<StringToTime(CAD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(CAD_TimeStartSession)>StringToTime(CAD_TimeEndSession))&&((CurrentTime>=StringToTime(CAD_TimeStartSession))||(CurrentTime<StringToTime(CAD_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"CAD");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_CAD=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_CAD=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_CAD=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_CAD=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_CAD>=ImpactToTrade)&&(SecondsToNews_CAD<=SecondsBeforeNewsStart))||((ImpactSinceNews_CAD>=ImpactToTrade)&&(SecondsSinceNews_CAD<=SecondsAfterNewsStop)))
                  TimeToTrade_CAD=true;
               if((SecondsToNews_CAD==0)||(SecondsSinceNews_CAD==0))
                  TimeToTrade_CAD=true;
               if((TimeToTrade_CAD==true)&&(ImpactToNews_CAD>=ImpactToTrade)&&(SecondsToNews_CAD<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(CHF_TradeInNewsRelease==1)
           {
            SetBuffers=7;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(CHF_TimeStartSession)==StringToTime(CHF_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(CHF_TimeStartSession)<StringToTime(CHF_TimeEndSession))&&((CurrentTime>=StringToTime(CHF_TimeStartSession))&&(CurrentTime<StringToTime(CHF_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(CHF_TimeStartSession)>StringToTime(CHF_TimeEndSession))&&((CurrentTime>=StringToTime(CHF_TimeStartSession))||(CurrentTime<StringToTime(CHF_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"CHF");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_CHF=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_CHF=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_CHF=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_CHF=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_CHF>=ImpactToTrade)&&(SecondsToNews_CHF<=SecondsBeforeNewsStart))||((ImpactSinceNews_CHF>=ImpactToTrade)&&(SecondsSinceNews_CHF<=SecondsAfterNewsStop)))
                  TimeToTrade_CHF=true;
               if((SecondsToNews_CHF==0)||(SecondsSinceNews_CHF==0))
                  TimeToTrade_CHF=true;
               if((TimeToTrade_CHF==true)&&(ImpactToNews_CHF>=ImpactToTrade)&&(SecondsToNews_CHF<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(JPY_TradeInNewsRelease==1)
           {
            SetBuffers=8;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(JPY_TimeStartSession)==StringToTime(JPY_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(JPY_TimeStartSession)<StringToTime(JPY_TimeEndSession))&&((CurrentTime>=StringToTime(JPY_TimeStartSession))&&(CurrentTime<StringToTime(JPY_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(JPY_TimeStartSession)>StringToTime(JPY_TimeEndSession))&&((CurrentTime>=StringToTime(JPY_TimeStartSession))||(CurrentTime<StringToTime(JPY_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"JPY");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_JPY=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_JPY=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_JPY=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_JPY=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_JPY>=ImpactToTrade)&&(SecondsToNews_JPY<=SecondsBeforeNewsStart))||((ImpactSinceNews_JPY>=ImpactToTrade)&&(SecondsSinceNews_JPY<=SecondsAfterNewsStop)))
                  TimeToTrade_JPY=true;
               if((SecondsToNews_JPY==0)||(SecondsSinceNews_JPY==0))
                  TimeToTrade_JPY=true;
               if((TimeToTrade_JPY==true)&&(ImpactToNews_JPY>=ImpactToTrade)&&(SecondsToNews_JPY<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
         if(CNY_TradeInNewsRelease==1)
           {
            SetBuffers=9;
            OpenSession[SetBuffers]=false;
            //---
            if(StringToTime(CNY_TimeStartSession)==StringToTime(CNY_TimeEndSession))
               OpenSession[SetBuffers]=true;
            if((StringToTime(CNY_TimeStartSession)<StringToTime(CNY_TimeEndSession))&&((CurrentTime>=StringToTime(CNY_TimeStartSession))&&(CurrentTime<StringToTime(CNY_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            if((StringToTime(CNY_TimeStartSession)>StringToTime(CNY_TimeEndSession))&&((CurrentTime>=StringToTime(CNY_TimeStartSession))||(CurrentTime<StringToTime(CNY_TimeEndSession))))
               OpenSession[SetBuffers]=true;
            //---
            if(OpenSession[SetBuffers]==true)
              {
               ReadNews(SetBuffers,"CNY");
               SessionBeforeEvent[SetBuffers]=false;
               SecondsSinceNews_CNY=ExtBufferSeconds[SetBuffers][0];
               SecondsToNews_CNY=ExtBufferSeconds[SetBuffers][1];
               ImpactSinceNews_CNY=ExtBufferImpact[SetBuffers][0];
               ImpactToNews_CNY=ExtBufferImpact[SetBuffers][1];
               //---
               if(((ImpactToNews_CNY>=ImpactToTrade)&&(SecondsToNews_CNY<=SecondsBeforeNewsStart))||((ImpactSinceNews_CNY>=ImpactToTrade)&&(SecondsSinceNews_CNY<=SecondsAfterNewsStop)))
                  TimeToTrade_CNY=true;
               if((SecondsToNews_CNY==0)||(SecondsSinceNews_CNY==0))
                  TimeToTrade_CNY=true;
               if((TimeToTrade_CNY==true)&&(ImpactToNews_CNY>=ImpactToTrade)&&(SecondsToNews_CNY<=SecondsBeforeNewsStart))
                  SessionBeforeEvent[SetBuffers]=true;
              }
           }
         //---------------------------------------------------------------------
        }
      iPrevMinute=Minute();
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Read file
//====================================================================================================================================================//
void ReadNews(int CountrySelect,string CountryCheck)
  {
//---------------------------------------------------------------------
   string Cookie=NULL,Headers;
   char Post[],Result[];
   int res;
   string ImpactLastNews="";
   datetime NewsTime;
   int NextEvent;
   int NewsIDx=0;
   ulong SizeOfFile=0;
   ulong SizeOfData=0;
   string MyEvent;
   int TimeOut=5000;
   bool SkipeEvent;
   bool ReportAllForEUR=false;
   bool ReportAllForGBP=false;
   bool ReportAllForAUD=false;
   bool ReportAllForNZD=false;
   bool ReportAllForUSD=false;
   bool ReportAllForCAD=false;
   bool ReportAllForCHF=false;
   bool ReportAllForJPY=false;
   bool ReportAllForCNY=false;
   bool IncludeLow=false;
   bool IncludeMedium=false;
   bool IncludeHigh=false;
//---------------------------------------------------------------------
//Set report country
   if(CountrySelect==1)
      ReportAllForEUR=true;
   if(CountrySelect==2)
      ReportAllForGBP=true;
   if(CountrySelect==3)
      ReportAllForAUD=true;
   if(CountrySelect==4)
      ReportAllForNZD=true;
   if(CountrySelect==5)
      ReportAllForUSD=true;
   if(CountrySelect==6)
      ReportAllForCAD=true;
   if(CountrySelect==7)
      ReportAllForCHF=true;
   if(CountrySelect==8)
      ReportAllForJPY=true;
   if(CountrySelect==9)
      ReportAllForCNY=true;
//---------------------------------------------------------------------
//Set report impact
   if(ImpactToTrade==0)
     {
      IncludeLow=true;
      IncludeMedium=true;
      IncludeHigh=true;
     }
   if(ImpactToTrade==1)
     {
      IncludeMedium=true;
      IncludeHigh=true;
     }
   if(ImpactToTrade==2)
     {
      IncludeHigh=true;
     }
//---------------------------------------------------------------------
//Read file
   xmlHandle=FileOpen(xmlFileName,FILE_BIN|FILE_READ);
   SizeOfFile=FileReadInteger(xmlHandle,INT_VALUE);
//---------------------------------------------------------------------
//Close file
   if(xmlHandle>=0)
     {
      FileClose(xmlHandle);
      NeedToGetFile=false;
     }
   else
      NeedToGetFile=true;
//---------------------------------------------------------------------
//Make file
   if((SizeOfFile==0)||(NeedToGetFile)||(((GlobalVariableCheck("LastUpdateTime: ")==false)||((TimeCurrent()-GlobalVariableGet("LastUpdateTime: "))>14400))&&(!IsTesting())&&(!IsVisualMode())&&(!IsOptimization())))
     {
      //---------------------------------------------------------------------
      //Get file
      ResetLastError();
      res=WebRequest("GET",ReadNewsURL,Cookie,NULL,TimeOut,Post,0,Result,Headers);
      PrintFormat("Getting file from: "+ReadNewsURL);
      //---Checking errors
      if(res==-1)
        {
         Print("Error in WebRequest. Error code: ",GetLastError());
         //---Perhaps the URL is not listed, display a message about the necessity to add the address
         MessageBox("Add the address '"+ReadNewsURL+"' in the list of allowed URLs on tab 'Expert Advisors'","Error code: ",MB_ICONINFORMATION);
         FileIsOk=false;
         Sleep(60000);
         return;
        }
      else
        {
         //---Load successfully
         PrintFormat("The file has been successfully loaded. File size =%d bytes.",ArraySize(Result));
         //---Save the data to a file
         FileDelete(xmlFileName);
         xmlHandle=FileOpen(xmlFileName,FILE_BIN|FILE_WRITE);
         //---------------------------------------------------------------------
         //Open file
         if(xmlHandle<0)
           {
            FileIsOk=false;
            return;
           }
         //---Checking errors
         if(xmlHandle!=INVALID_HANDLE)
           {
            //---Save the contents of the Result[] array to a file
            FileWriteArray(xmlHandle,Result,0,ArraySize(Result));
            FileClose(xmlHandle);
           }
         else
           {
            Print("Error in file open. Error code: ",GetLastError());
            FileIsOk=false;
            return;
           }
        }
      //---------------------------------------------------------------------
      FileWriteString(xmlHandle,sData,StringLen(sData));
      FileClose(xmlHandle);
      //---------------------------------------------------------------------
      //Open the XML file
      xmlHandle=FileOpen(xmlFileName,FILE_BIN|FILE_READ);
      if(xmlHandle<0)
        {
         Print("Can\'t open xml file: ",xmlFileName,". Error code: ",GetLastError());
         return;
        }
      //---
      sData=FileReadString(xmlHandle,INT_VALUE);
      bool pnt=FileSeek(xmlHandle,0,0);
      SizeOfFile=FileSize(xmlHandle);
      SizeOfData=StringLen(sData);
      if(SizeOfData<SizeOfFile)
         sData=sData+FileReadString(xmlHandle,(int)SizeOfFile);
      if(xmlHandle>0)
         FileClose(xmlHandle);
      //---------------------------------------------------------------------
      //Check if get hole file
      EndWeek=StringFind(sData,"</weeklyevents>",0);
      if(EndWeek<=0)
        {
         //Alert("Web page download was not complete!");
         Print("Web page download was not complete! Error code: ",GetLastError());
         FileIsOk=false;
         return;
        }
      else
        {
         Print("Last update time: ",TimeCurrent());
         GlobalVariableSet("LastUpdateTime: ",TimeCurrent());
        }
     }
//---------------------------------------------------------------------
//Check event file
   FileIsOk=true;
//---------------------------------------------------------------------
//Init the buffer array to zero just in case
   ArrayInitialize(ExtMapBuffer0,0);
   tmpMins=10080;//(a hole week)
   BoEvent=0;
//---------------------------------------------------------------------
//Get events
   while(true)
     {
      BoEvent=StringFind(sData,"<event>",BoEvent);
      if(BoEvent==-1)
         break;
      //---
      BoEvent+=7;
      NextEvent=StringFind(sData,"</event>",BoEvent);
      if(NextEvent==-1)
         break;
      //---
      MyEvent=StringSubstr(sData,BoEvent,NextEvent-BoEvent);
      BoEvent=NextEvent;
      //---
      BeginWeek=0;
      SkipeEvent=false;
      //---
      for(i=0; i<7; i++)
        {
         mainData[NewsIDx][i]="";
         NextEvent=StringFind(MyEvent,sTags[i],BeginWeek);
         //---------------------------------------------------------------------
         //Within this event,if tag not found, then it must be missing; skip it
         if(NextEvent==-1)
            continue;
         else
           {
            //---------------------------------------------------------------------
            //We must have found the sTag okay...
            BeginWeek=NextEvent+StringLen(sTags[i]);//Advance past the start tag
            EndWeek=StringFind(MyEvent,eTags[i],BeginWeek);//Find start of end tag
            //---------------------------------------------------------------------
            //Get data between start and end tag
            if((EndWeek>BeginWeek)&&(EndWeek!=-1))
              {
               mainData[NewsIDx][i]=StringSubstr(MyEvent,BeginWeek,EndWeek-BeginWeek);
              }
           }
        }
      //---------------------------------------------------------------------
      //Set skip switch
      if((CountryCheck!=mainData[NewsIDx][COUNTRY]) &&
         ((!ReportAllForEUR)||(mainData[NewsIDx][COUNTRY]!="EUR"))&&
         ((!ReportAllForGBP)||(mainData[NewsIDx][COUNTRY]!="GBP"))&&
         ((!ReportAllForAUD)||(mainData[NewsIDx][COUNTRY]!="AUD"))&&
         ((!ReportAllForNZD)||(mainData[NewsIDx][COUNTRY]!="NZD"))&&
         ((!ReportAllForUSD)||(mainData[NewsIDx][COUNTRY]!="USD"))&&
         ((!ReportAllForCAD)||(mainData[NewsIDx][COUNTRY]!="CAD"))&&
         ((!ReportAllForCHF)||(mainData[NewsIDx][COUNTRY]!="CHF"))&&
         ((!ReportAllForJPY)||(mainData[NewsIDx][COUNTRY]!="JPY"))&&
         ((!ReportAllForCNY)||(mainData[NewsIDx][COUNTRY]!="CNY")))
         SkipeEvent=true;
      //---------------------------------------------------------------------
      if((!IncludeLow)&&(mainData[NewsIDx][IMPACT]=="Low"))
         SkipeEvent=true;
      if((!IncludeMedium)&&(mainData[NewsIDx][IMPACT]=="Medium"))
         SkipeEvent=true;
      if((!IncludeHigh)&&(mainData[NewsIDx][IMPACT]=="High"))
         SkipeEvent=true;
      if((mainData[NewsIDx][IMPACT]=="Holiday")||(mainData[NewsIDx][IMPACT]=="holiday"))
         SkipeEvent=true;
      if((mainData[NewsIDx][TIME]=="All Day")||(mainData[NewsIDx][TIME]=="Tentative")||(mainData[NewsIDx][TIME]==""))
         SkipeEvent=true;
      if((!IncludeSpeaks)&&((StringFind(mainData[NewsIDx][TITLE],"speaks")!=-1)||(StringFind(mainData[NewsIDx][TITLE],"Speaks")!=-1)))
         SkipeEvent=true;
      //---------------------------------------------------------------------
      //Get unskip
      if(!SkipeEvent)
        {
         //Get impact
         ImpactLastNews=mainData[NewsIDx][IMPACT];
         //First, convert the announcement time to seconds (in GMT)
         NewsTime=StrToTime(MakeDateTime(mainData[NewsIDx][DATE],mainData[NewsIDx][TIME]));
         //Now calculate the Seconds until this announcement (may be negative)
         minsTillNews=NewsTime-CurrentTime;
         if((minsTillNews<0)||(MathAbs(tmpMins)>minsTillNews))
            tmpMins=minsTillNews;
         ExtMapBuffer0[CountrySelect][NewsIDx]=(int)minsTillNews;
         NewsIDx++;
        }
     }
//---------------------------------------------------------------------
//Reset buffers
   ExtBufferSeconds[CountrySelect][0]=-9999;
   ExtBufferSeconds[CountrySelect][1]=9999;
   ExtBufferImpact[CountrySelect][0]=-1;
   ExtBufferImpact[CountrySelect][1]=-1;
//---------------------------------------------------------------------
//Set buffers
   for(i=0; i<NewsIDx; i++)
     {
      //---------------------------------------------------------------------
      //Seconds UNTIL
      if((ExtMapBuffer0[CountrySelect][i]>=0)&&(mainData[i][COUNTRY]==CountryCheck))
        {
         if(ExtBufferSeconds[CountrySelect][1]==9999)
           {
            ExtBufferSeconds[CountrySelect][1]=ExtMapBuffer0[CountrySelect][i];
            ExtBufferImpact[CountrySelect][1]=ImpactToNumber(mainData[i][IMPACT]);
           }
        }
      //---------------------------------------------------------------------
      //Seconds SINCE
      if((ExtMapBuffer0[CountrySelect][i]<=0)&&(mainData[i][COUNTRY]==CountryCheck))
        {
         //if(ExtBufferSeconds[CountrySelect][0]==-9999)
         //{
         ExtBufferSeconds[CountrySelect][0]=MathAbs(ExtMapBuffer0[CountrySelect][i]);
         ExtBufferImpact[CountrySelect][0]=ImpactToNumber(mainData[i][IMPACT]);
         //}
        }
     }
//---------------------------------------------------------------------
//Close file
   if(LogHandle>0)
     {
      FileClose(LogHandle);
      LogHandle=-1;
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Convert impact
//====================================================================================================================================================//
int ImpactToNumber(string Impact)
  {
//---------------------------------------------------------------------
   if(Impact=="Low")
      return(0);
   if(Impact=="Medium")
      return(1);
   if(Impact=="High")
      return(2);
   else
      return(-1);
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Convert time
//====================================================================================================================================================//
string MakeDateTime(string StrDate,string StrTime)
  {
//---------------------------------------------------------------------
   int Dash_1=StringFind(StrDate,"-");
   int Dash_2=StringFind(StrDate,"-",Dash_1+1);
   string StrMonth=StringSubstr(StrDate,0,2);
   string StrDay=StringSubstr(StrDate,3,2);
   string StrYear=StringSubstr(StrDate,6,4);
   int nTimeColonPos=StringFind(StrTime,":");
   string StrHour=StringSubstr(StrTime,0,nTimeColonPos);
   string StrMinute=StringSubstr(StrTime,nTimeColonPos+1,2);
   string StrAM_PM=StringSubstr(StrTime,StringLen(StrTime)-2);
   string StrHourPad="";
   int Hour24=StrToInteger(StrHour);
//---------------------------------------------------------------------
   if(((StrAM_PM=="pm")||(StrAM_PM=="PM"))&&(Hour24!=12))
      Hour24+=12;
//---
   if(((StrAM_PM=="am")||(StrAM_PM=="AM"))&&(Hour24==12))
      Hour24=0;
//---
   if(Hour24<10)
      StrHourPad="0";
//---
   return(StringConcatenate(StrYear,".",StrMonth,".",StrDay," ",StrHourPad,Hour24,":",StrMinute));
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//End code
//====================================================================================================================================================//
