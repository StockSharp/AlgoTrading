//+------------------------------------------------------------------+
//|                                            CoensioTrader1V06.mq5 |
//|                                         © Copyright 2013 Coesnio |
//|                                           http://www.coensio.com |
//|                                                                  |
//| Notes:                                                           |
//| * Please do not change the network functions                     |
//| * If desired change only trading strategy functions              |
//| * For more information/questions visit www.coensio.com/forum     |
//+------------------------------------------------------------------+
#property copyright "© Coensio"
#property link      "http://www.Coensio.com"
//#property version   "0.6"

#include "coensiotrader1v06.mqh" 
#include <Trade\Trade.mqh>
#include <ChartObjects\ChartObjectsTxtControls.mqh>

//Web connection functions
#import "wininet.dll"
int InternetAttemptConnect(int x);
int InternetOpenW(string sAgent,int lAccessType,
                  string sProxyName="",string sProxyBypass="",
                  int lFlags=0);
int InternetOpenUrlW(int hInternetSession,string sUrl,
                     string sHeaders="",int lHeadersLength=0,
                     int lFlags=0,int lContext=0);
int InternetReadFile(int hFile,uchar &sBuffer[],int lNumBytesToRead,
                     int &lNumberOfBytesRead[]);
int HttpQueryInfoW(int hRequest,int dwInfoLevel,
                   uchar &lpvBuffer[],int &lpdwBufferLength,int &lpdwIndex);
int InternetCloseHandle(int hInet);
#import

//MQL5 Classes
CTrade cTrade;
//Define EA name
string EaName="CoensioTrader1V06";

//General variable settings
input string      GeneralEASettings    ="$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$";
input string      UserName             ="coensio"; //Specify your username here
input ENUM_TIMEFRAMES TimeFrame        =PERIOD_H1; //TimeFrame: Base time frame
input int         NumOfCurrencies      =1;         //NumOfCurrencies: 1 to 6, if "1" then single chart mode is used
input int         MarketWatchTimer     =60;        //MarketWatchTimer: EA loop-timer in seconds
input double      EquityTakeProfit     =1.2;       //EquityTakeProfit: TakeProfit if Equity>EquityTakeProfit*Balance
input double      RiskMax              =0;         //RiskMax: Maximum equity risk in %, if "0" then LotSize is used
input double      LotSize              =0;         //LotSize: Lot size used if RiskMax="0" 
input double      LotBalanceDivider    =1000000;   //LotBalanceDivider: Used if RiskMax="0" && LotSize="0"
input int         TrailingStopLossStep =10;        //TrailingStopLossStep: In pips, if "0" then trailing is disabled
input bool        CloseOnSignal        =false;     //CloseOnSignal: Closes active trade on new signal
input int         Slip                 =3;         //Slip: Slippage in pips     

                                                   //Currency settings
input string      CurrencySettings     ="$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$";
string            CurrencyPair1        ="EURUSD";
input string      CurrencyPair1Info="CHART";   //CurrencyPair1: Defined by EA's chart e.g. EURUSD
input string      CurrencyPair2        ="GBPUSD";
input string      CurrencyPair3        ="AUDUSD";
input string      CurrencyPair4        ="NZDUSD";
input string      CurrencyPair5        ="USDCAD";
input string      CurrencyPair6        ="USDJPY";
//Optimization settings
input string     OptimizationSettings="$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$";
//CurencyPair1
input int         TakeProfitPips1      =250;       //TakeProfit1: 0 to 500 in pips
input int         StopLossPips1        =100;       //StopLoss1: 0 to 500 in pips
input int         BPeriod1             =30;        //BPeriod1: 10 to 50 
input int         BShift1              =1;         //BShift1: 1  
input double      BDeviation1          =1.5;       //BDeviation1: 1 to 3
input ENUM_TIMEFRAMES DemaTimeFrame1   =PERIOD_D1; //DemaTimeFrame1: e.g.: D1
input int         DemaPeriod1          =20;        //DemaPeriod1: 10 to 50
                                                   //CurencyPair2
input int         TakeProfitPips2      =300;       //TakeProfit2: 0 to 500 in pips
input int         StopLossPips2        =200;       //StopLoss2: 0 to 500 in pips
input int         BPeriod2             =40;        //BPeriod2: 10 to 50 
input int         BShift2              =1;         //BShift2: 1  
input double      BDeviation2          =2;         //BDeviation2: 1 to 3
input ENUM_TIMEFRAMES DemaTimeFrame2   =PERIOD_D1; //DemaTimeFrame2: e.g.: D1
input int         DemaPeriod2          =40;        //DemaPeriod2: 10 to 50
                                                   //CurencyPair3
input int         TakeProfitPips3      =100;       //TakeProfit3: 0 to 500 in pips
input int         StopLossPips3        =150;       //StopLoss3: 0 to 500 in pips
input int         BPeriod3             =10;        //BPeriod3: 10 to 50 
input int         BShift3              =1;         //BShift3: 1  
input double      BDeviation3          =2;         //BDeviation3: 1 to 3
input ENUM_TIMEFRAMES DemaTimeFrame3   =PERIOD_D1; //DemaTimeFrame3: e.g.: D1
input int         DemaPeriod3          =20;        //DemaPeriod3: 10 to 50         
                                                   //CurencyPair4
input int         TakeProfitPips4      =200;       //TakeProfit4: 0 to 500 in pips
input int         StopLossPips4        =100;       //StopLoss4: 0 to 500 in pips
input int         BPeriod4             =40;        //BPeriod4: 10 to 50 
input int         BShift4              =1;         //BShift4: 1  
input double      BDeviation4          =2;         //BDeviation4: 1 to 3
input ENUM_TIMEFRAMES DemaTimeFrame4   =PERIOD_D1; //DemaTimeFrame4: e.g.: D1
input int         DemaPeriod4          =40;        //DemaPeriod4: 10 to 50          
                                                   //CurencyPair5
input int         TakeProfitPips5      =100;       //TakeProfit5: 0 to 500 in pips
input int         StopLossPips5        =150;       //StopLoss5: 0 to 500 in pips
input int         BPeriod5             =50;        //BPeriod5: 10 to 50 
input int         BShift5              =1;         //BShift5: 1  
input double      BDeviation5          =1.5;       //BDeviation5: 1 to 3
input ENUM_TIMEFRAMES DemaTimeFrame5   =PERIOD_D1; //DemaTimeFrame5: e.g.: D1
input int         DemaPeriod5          =10;        //DemaPeriod5: 10 to 50  
                                                   //CurencyPair6
input int         TakeProfitPips6      =300;       //TakeProfit6: 0 to 500 in pips
input int         StopLossPips6        =200;       //StopLoss6: 0 to 500 in pips
input int         BPeriod6             =30;        //BPeriod6: 10 to 50 
input int         BShift6              =1;         //BShift6: 1  
input double      BDeviation6          =1;         //BDeviation6: 1 to 3
input ENUM_TIMEFRAMES DemaTimeFrame6   =PERIOD_D1; //DemaTimeFrame6: e.g.: D1
input int         DemaPeriod6          =10;        //DemaPeriod6: 10 to 50           

                                                   //Internal variables
int               n                    =0;
static int        BarCnt               =0;
bool              MarketDirection      =false;
double            iMABuffer1[];
double            iMABuffer2[];
double            iMABuffer3[];
double            iMABuffer4[];
static double     ask1                 =0;
static double     ask2                 =0;
static double     ask3                 =0;
static double     ask4                 =0;
static double     ask5                 =0;
static double     ask6                 =0;
MqlTick           LastTick1;
MqlTick           LastTick2;
MqlTick           LastTick3;
MqlTick           LastTick4;
MqlTick           LastTick5;
MqlTick           LastTick6;
//EA verification
string            URL="http://www.coensio.com/sts/"; //Coensio status directory URL
string            AccessCode           ="SE34535FER345346";
string            CoensioMsg           ="Welcome to CoensioTrader1 project. Visit www.coensio.com";
string            CoensioCode          ="";
//Optimization data mining
string            OptimizationPasses   ="";
string            LogFile              ="";
string            InstallFile          ="";
double            IndexToLog           =10;       //Optimization results with larger CoensioIndex than IndexToLog will be reported 
double            DrawDownToLog        =30;       //Optimization results with lower Equity drawdown than DrawDownToLog will be reported 
double            GrowthToLog          =10;       //Optimization results with larger Growth than GrowthToLog will be reported 
int               TopPasses            =25;       //Number of best case passes that are reported to coensio server
double            StatisticValues[9];             //Array for testing parameters
string            ParameterValues[];              //Array of parameter values
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_STATS
  {
   C_DEPOSIT              =0, //Initial deposit
   C_TRADES               =1, //Total trades
   C_PROFIT               =2, //Profit 
   C_PROFIT_FACTOR        =3, //Profit factor
   C_BALANCE_DD           =4, //Max. balance drawdown
   C_EQUITYDD_PERCENT     =5, //Max. equity drawdown in %
   C_GROWTH               =6, //Growth in %  
   C_INDEX                =7  //Coeniso Index = profit factor * growth factor/drawdown^2
  };
//Coensio Class
class CCoensioClass
  {
private:
   int               MagicNr;
   double            Slippage;
   double            Pip;
   int               BarCntInt;
   bool              BuySig1;
   bool              BuySig2;
   bool              SellSig1;
   bool              SellSig2;
   bool              LongTrading;
   bool              ShortTrading;
   string            symbol;                        //Currency pair to trade 
   ENUM_TIMEFRAMES   timeframe;                     //Timeframe
   double            EntryBars;
   double            Lots;
   int               Ticket;
   double            TP;
   double            SL;
   MqlTick           LastTick;
   MqlRates          Mrate[];
   int               BBHandle;                      //Bollinger Bands handle
   int               DemaHandle;                    //DEMA handle
   double            BBUp[],BBLow[],BBMidle[];      //Dynamic arrays for numerical values of Bollinger Bands
   double            DemaVal[];                     //Dynamic array for numerical values of Moving Average 
   double            EMA1[];
   double            EMA2[];
   double            EMA3[];
   int               StopLoss;
   int               TakeProfit;
   //Functions   
   void              BarLogic();
   void              ExitLogic();
   bool              CheckOpenOrders();
   bool              OpenBuy(string SignalComment);
   bool              OpenSell(string SignalComment);
   bool              CloseBuy();
   bool              CloseSell();
   void              GetLotSize(int Pips);
   bool              MoveSL();
public:
   void              TickLogic();
   bool              Init(string SYMBOL,ENUM_TIMEFRAMES TIMEFRAME);
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCoensioClass::Init(string SYMBOL,ENUM_TIMEFRAMES TIMEFRAME)
  {
   symbol=SYMBOL;
   timeframe=TIMEFRAME;
   Lots=LotSize;
   Slippage=Slip;
   StopLoss=StopLossPips1;
   TakeProfit=TakeProfitPips1;
//Determine Pip and Slippage value
   if(SymbolInfoInteger(symbol, SYMBOL_DIGITS)==2 || SymbolInfoInteger(symbol, SYMBOL_DIGITS)==4) { Pip=1*SymbolInfoDouble(symbol, SYMBOL_POINT); Slippage=1*Slippage;}
   if(SymbolInfoInteger(symbol, SYMBOL_DIGITS)==3 || SymbolInfoInteger(symbol, SYMBOL_DIGITS)==5) { Pip=10*SymbolInfoDouble(symbol, SYMBOL_POINT); Slippage=10*Slippage;}
   if(SymbolInfoInteger(symbol,SYMBOL_DIGITS)==6) { Pip=100*SymbolInfoDouble(symbol,SYMBOL_POINT); Slippage=100*Slippage;}
//Generate unique magic number
   MathSrand(TimeCurrent());
   MagicNr=MathAbs(MagicNr*MathRand()*timeframe*StringGetChar(symbol,1)*StringGetChar(symbol,3));
   MagicNr=StrToDouble(StringSubstr(DoubleToStr(MagicNr,0),1,6));

   cTrade.SetExpertMagicNumber(MagicNr);
   cTrade.SetDeviationInPoints(Slippage);

   Print(symbol," is initialized.");
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CCoensioClass::BarLogic()
  {
   BBHandle=iBands(symbol,timeframe,BPeriod1,BShift1,BDeviation1,PRICE_CLOSE);
   DemaHandle=iDEMA(symbol,DemaTimeFrame1,DemaPeriod1,0,PRICE_CLOSE);

   if(NumOfCurrencies>=2)
     {
      if(symbol==CurrencyPair1)
        {
         TakeProfit=TakeProfitPips1;
         StopLoss=StopLossPips1;
         BBHandle=iBands(symbol,timeframe,BPeriod1,BShift1,BDeviation1,PRICE_CLOSE);
         DemaHandle=iDEMA(symbol,DemaTimeFrame1,DemaPeriod1,0,PRICE_CLOSE);
        }
      if(symbol==CurrencyPair2)
        {
         TakeProfit=TakeProfitPips2;
         StopLoss=StopLossPips2;
         BBHandle=iBands(symbol,timeframe,BPeriod2,BShift2,BDeviation2,PRICE_CLOSE);
         DemaHandle=iDEMA(symbol,DemaTimeFrame2,DemaPeriod2,0,PRICE_CLOSE);
        }
      if(symbol==CurrencyPair3)
        {
         TakeProfit=TakeProfitPips3;
         StopLoss=StopLossPips3;
         BBHandle=iBands(symbol,timeframe,BPeriod3,BShift3,BDeviation3,PRICE_CLOSE);
         DemaHandle=iDEMA(symbol,DemaTimeFrame3,DemaPeriod3,0,PRICE_CLOSE);
        }
      if(symbol==CurrencyPair4)
        {
         TakeProfit=TakeProfitPips4;
         StopLoss=StopLossPips4;
         BBHandle=iBands(symbol,timeframe,BPeriod4,BShift4,BDeviation4,PRICE_CLOSE);
         DemaHandle=iDEMA(symbol,DemaTimeFrame4,DemaPeriod4,0,PRICE_CLOSE);
        }
      if(symbol==CurrencyPair5)
        {
         TakeProfit=TakeProfitPips5;
         StopLoss=StopLossPips5;
         BBHandle=iBands(symbol,timeframe,BPeriod5,BShift5,BDeviation5,PRICE_CLOSE);
         DemaHandle=iDEMA(symbol,DemaTimeFrame5,DemaPeriod5,0,PRICE_CLOSE);
        }
      if(symbol==CurrencyPair6)
        {
         TakeProfit=TakeProfitPips6;
         StopLoss=StopLossPips6;
         BBHandle=iBands(symbol,timeframe,BPeriod6,BShift6,BDeviation6,PRICE_CLOSE);
         DemaHandle=iDEMA(symbol,DemaTimeFrame6,DemaPeriod6,0,PRICE_CLOSE);
        }
     }

//Define Arrays
   ArraySetAsSeries(Mrate,true);
   ArraySetAsSeries(DemaVal,true);
   ArraySetAsSeries(BBUp,true);
   ArraySetAsSeries(BBLow,true);
   ArraySetAsSeries(BBMidle,true);
//Copy indicator data to buffers 
   if(CopyRates(symbol,timeframe,0,4,Mrate)<0) return;
   if(CopyBuffer(BBHandle,0,0,3,BBMidle)<0 || 
      CopyBuffer(BBHandle,1,0,3,BBUp)<0 || 
      CopyBuffer(BBHandle,2,0,3,BBLow)<0) return;
   if(CopyBuffer(DemaHandle,0,0,3,DemaVal)<0) return;

   CopyBuffer(iMA(symbol,PERIOD_H4,21,0,MODE_SMA,PRICE_CLOSE),0,0,10,EMA1);
   CopyBuffer(iMA(symbol,PERIOD_H4,50,0,MODE_SMA,PRICE_CLOSE),0,0,10,EMA2);
   CopyBuffer(iMA(symbol,PERIOD_H4,100,0,MODE_SMA,PRICE_CLOSE),0,0,10,EMA3);

   if(symbol==Symbol())//Say what??
     {
      if(DemaVal[0]>DemaVal[1] && DemaVal[1]>DemaVal[2])
         MarketDirection=true;  //Up trend
      if(DemaVal[0]<DemaVal[1] && DemaVal[1]<DemaVal[2])
         MarketDirection=false; //Down trend
     }
   if(Mrate[1].open < BBLow[1] && Mrate[1].close > BBLow[1])
      if(Mrate[1].low > Mrate[2].low) //Higher low
         if(Mrate[2].low < Mrate[3].low) //Lower low
            if(DemaVal[0]>DemaVal[1] && DemaVal[1]>DemaVal[2])
               BuySig1=true;

   if(Mrate[1].open > BBUp[1] && Mrate[1].close < BBUp[1])
      if(Mrate[1].high < Mrate[2].high) //Lower high
         if(Mrate[2].high > Mrate[3].high) //Higher high
            if(DemaVal[0]<DemaVal[1] && DemaVal[1]<DemaVal[2])
               SellSig1=true;

//Draw market trend direction
   if(!MQL5InfoInteger(MQL5_TESTING) || IsVisualMode())
      if(symbol==Symbol())
         DrawTrend(MarketDirection);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CCoensioClass::TickLogic()
  {
//Reset entry signals
   BuySig1=false;
   BuySig2=false;
   SellSig1=false;
   SellSig2=false;

//Check for new bar
   if(BarCntInt!=iBars(symbol,timeframe))
     {
      BarLogic();
      BarCntInt=iBars(symbol,timeframe);
     }

//Process exit signals
   if(CloseOnSignal) ExitLogic(); //Close current trades when new signal is detected

                                  //Process entry signals
   if(BuySig1) OpenBuy("BS1");
   if(BuySig2) OpenBuy("BS2");
   if(SellSig1) OpenSell("SS1");
   if(SellSig2) OpenSell("SS2");

//Take Equity profit!
   if(AccountInfoDouble(ACCOUNT_EQUITY)>AccountInfoDouble(ACCOUNT_BALANCE)*EquityTakeProfit)
     {
      int i=PositionsTotal()-1;
      while(i>=0)
        {
         if(cTrade.PositionClose(PositionGetSymbol(i))) i--;
        }
     }

//Check for open trades: define LongTrading and ShortTrading flags and handle trailing stoploss
   if(CheckOpenOrders() && TrailingStopLossStep!=0)
     {
      MoveSL();
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CCoensioClass::ExitLogic()
  {
//Process exit signals
   if(BuySig1 || BuySig2) CloseSell();
   if(SellSig1 || SellSig2) CloseBuy();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCoensioClass::CheckOpenOrders()
  {
//Search for open trades
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_MAGIC)==MagicNr)
        {
         return(true);
        }
     }
//Else, if no open trades are found
   LongTrading=false;
   ShortTrading=false;
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCoensioClass::MoveSL()
  {
   SymbolInfoTick(symbol,LastTick);
   double CurrentSL=0;
   double CurrentTP=0;
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_MAGIC)==MagicNr)
        {
         if(PositionGetInteger(POSITION_TYPE)==OP_BUY)
           {
            CurrentSL=PositionGetDouble(POSITION_SL);
            CurrentTP=PositionGetDouble(POSITION_TP);
            if((LastTick.bid-CurrentSL)>(StopLoss+TrailingStopLossStep)*Pip)
               cTrade.PositionModify(symbol,(CurrentSL+TrailingStopLossStep*Pip),CurrentTP);
            return(true);
           }
         if(PositionGetInteger(POSITION_TYPE)==OP_SELL)
           {
            CurrentSL=PositionGetDouble(POSITION_SL);
            CurrentTP=PositionGetDouble(POSITION_TP);
            if((CurrentSL-LastTick.ask)>(StopLoss+TrailingStopLossStep)*Pip)
               cTrade.PositionModify(symbol,(CurrentSL-TrailingStopLossStep*Pip),CurrentTP);
            return(true);
           }
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCoensioClass::OpenBuy(string SignalComment)
  {
   SymbolInfoTick(symbol,LastTick);
   Ticket=false;
   SL=0;
   SL= LastTick.ask-(StopLoss*Pip);
   TP=0;
   if(TakeProfit!=0) TP=LastTick.ask+(TakeProfit*Pip);
   GetLotSize(StopLoss);

   Ticket=cTrade.PositionOpen(symbol,OP_BUY,Lots,LastTick.ask,NormalizeDouble(SL,SymbolInfoInteger(symbol,SYMBOL_DIGITS)),NormalizeDouble(TP,SymbolInfoInteger(symbol,SYMBOL_DIGITS)),SignalComment);
   if(!Ticket)
     {
      Print("Buy Error: ",GetLastError()," ",symbol);
      Print("Failed to open order! Error:",cTrade.CheckResultRetcodeDescription());
      return(false);
     }
   if(Ticket)
     {
      EntryBars=iBars(symbol,timeframe);
      BuySig1=false;
      BuySig2=false;
      LongTrading=true;
      ShortTrading=false;
      Print(symbol,": OpenBuy, Lots: ",Lots);
      DrawText(DoubleToString(LastTick.ask,5),SignalComment,TimeCurrent(),iLow(symbol,timeframe,0)-30*Pip);
      UpArrow(DoubleToString(iLow(symbol,timeframe,0),5),TimeCurrent(),iLow(symbol,timeframe,0)-5*Pip,Blue);
      return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCoensioClass::OpenSell(string SignalComment)
  {
   SymbolInfoTick(symbol,LastTick);
   Ticket=false;
   SL=0;
   SL= LastTick.bid+(StopLoss*Pip);
   TP=0;
   if(TakeProfit!=0) TP=LastTick.bid-(TakeProfit*Pip);
   GetLotSize(StopLoss);

   Ticket=cTrade.PositionOpen(symbol,OP_SELL,Lots,LastTick.bid,NormalizeDouble(SL,SymbolInfoInteger(symbol,SYMBOL_DIGITS)),NormalizeDouble(TP,SymbolInfoInteger(symbol,SYMBOL_DIGITS)),SignalComment);
   if(!Ticket)
     {
      Print("Sell Error:  ",GetLastError()," ",symbol);
      Print("Failed to open order! Error:",cTrade.CheckResultRetcodeDescription());
      return(false);
     }
   if(Ticket)
     {
      EntryBars=iBars(symbol,timeframe);
      SellSig1=false;
      SellSig2=false;
      ShortTrading= true;
      LongTrading = false;
      Print(symbol,": OpenSell, Lots: ",Lots);
      DrawText(DoubleToString(LastTick.bid,5),SignalComment,TimeCurrent(),iHigh(symbol,timeframe,0)+50*Pip);
      DownArrow(DoubleToString(iHigh(symbol,timeframe,0),5),TimeCurrent(),iHigh(symbol,timeframe,0)+40*Pip,Red);
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCoensioClass::CloseBuy()
  {
//################## MQL5 #######################
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_MAGIC)==MagicNr)
        {
         if(PositionGetInteger(POSITION_TYPE)==OP_BUY)
           {
            cTrade.PositionClose(symbol);
            Comment("Buy closed!");
            LongTrading=false;
            return(true);
           }
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CCoensioClass::CloseSell()
  {
//################## MQL5 #######################
   if(PositionSelect(symbol))
     {
      if(PositionGetInteger(POSITION_MAGIC)==MagicNr)
        {
         if(PositionGetInteger(POSITION_TYPE)==OP_SELL)
           {
            cTrade.PositionClose(symbol);
            Comment("Sell closed!");
            ShortTrading=false;
            return(true);
           }
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CCoensioClass::GetLotSize(int Pips)
  {
//Proportional or fixed risk management
   if(RiskMax>0) Lots=NormalizeDouble(((RiskMax/100)*AccountInfoDouble(ACCOUNT_FREEMARGIN)/Pips)/(Pip/SymbolInfoDouble(symbol,SYMBOL_POINT)),2);
   if(RiskMax==0 && LotSize==0) Lots=NormalizeDouble((AccountInfoDouble(ACCOUNT_EQUITY)/LotBalanceDivider),2);
   if(Lots<MarketInfo(symbol, MODE_MINLOT)) Lots=MarketInfo(symbol, MODE_MINLOT);
   if(Lots>MarketInfo(symbol, MODE_MAXLOT)) Lots=MarketInfo(symbol, MODE_MAXLOT);
   Lots=NormalizeDouble(Lots,2);
   return;
  }

//Load class
CCoensioClass CCoensioClass;
//Define object name for each trading currency pair
CCoensioClass TradeObject1,TradeObject2,TradeObject3,TradeObject4,TradeObject5,TradeObject6;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//Perform EA checks
   if(!EACheck())
      return(INIT_FAILED);

//Set EA market watch timer interval
   EventSetTimer(MarketWatchTimer); //in seconds
                                    //EventSetMillisecondTimer(100); //in miliseconds

//Initialize currency pairs
   CurrencyPair1=Symbol();
   if(NumOfCurrencies>=1) TradeObject1.Init(CurrencyPair1, TimeFrame);
   if(NumOfCurrencies>=2) TradeObject2.Init(CurrencyPair2, TimeFrame);
   if(NumOfCurrencies>=3) TradeObject3.Init(CurrencyPair3, TimeFrame);
   if(NumOfCurrencies>=4) TradeObject4.Init(CurrencyPair4, TimeFrame);
   if(NumOfCurrencies>=5) TradeObject5.Init(CurrencyPair5, TimeFrame);
   if(NumOfCurrencies>=6) TradeObject6.Init(CurrencyPair6, TimeFrame);

//Clean chart
   for(int i=ObjectsTotal(0)-1; i>-1; i--)
     {
      ObjectDelete(0,ObjectName(0,i));
     }

//Set coensio colors
   ChartColorSet();
//Display info labels
   DisplayInfo();
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//Just exit
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(IsVisualMode())
     {
      DisplayInfo();
      MoveTrend();
     }
//To avoid tick stagnation the EA control is replaced to OnTimer function!   
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
//Process each object in trade class.
   if(NumOfCurrencies>=1) SymbolInfoTick(CurrencyPair1,LastTick1);
   if(NumOfCurrencies>=2) SymbolInfoTick(CurrencyPair2,LastTick2);
   if(NumOfCurrencies>=3) SymbolInfoTick(CurrencyPair3,LastTick3);
   if(NumOfCurrencies>=4) SymbolInfoTick(CurrencyPair4,LastTick4);
   if(NumOfCurrencies>=5) SymbolInfoTick(CurrencyPair5,LastTick5);
   if(NumOfCurrencies>=6) SymbolInfoTick(CurrencyPair6,LastTick6);

   if(NumOfCurrencies>=1) if(LastTick1.ask!=ask1){ TradeObject1.TickLogic(); ask1=LastTick1.ask; }
   if(NumOfCurrencies>=2) if(LastTick2.ask!=ask2){ TradeObject2.TickLogic(); ask2=LastTick2.ask; }
   if(NumOfCurrencies>=3) if(LastTick3.ask!=ask3){ TradeObject3.TickLogic(); ask3=LastTick3.ask; }
   if(NumOfCurrencies>=4) if(LastTick4.ask!=ask4){ TradeObject4.TickLogic(); ask4=LastTick4.ask; }
   if(NumOfCurrencies>=5) if(LastTick5.ask!=ask5){ TradeObject5.TickLogic(); ask5=LastTick5.ask; }
   if(NumOfCurrencies>=6) if(LastTick6.ask!=ask6){ TradeObject6.TickLogic(); ask6=LastTick6.ask; }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool EACheck()
  {
//Check EA name
   if(StringCompare(WindowExpertName(),EaName)!=0)
     {
      MessageBox("Copyright protection enabled!","Info");
      Print("Copyright protection enabled!");
      return(false);
     }
//Check user name
   if(StringCompare(UserName,"coensio")==0)
     {
      MessageBox("You are not coensio, specify your nick name in UserName parameter!","Info");
      Print("You are not coensio, specify your nick name in UserName parameter!");
      return(false);
     }
//Check TimeFrame
   if(Period()!=TimeFrame)
     {
      MessageBox("Chart timeframe not equal to TimeFrame setting!","Info");
      Print("Chart timeframe not equal to TimeFrame setting!");
      return(false);
     }
   if(TerminalInfoInteger(TERMINAL_DLLS_ALLOWED)==false)
     {
      MessageBox("Allow DLL import in options of the client terminal.","Info");
      Print("Allow DLL import in options of the client terminal.");
      return(false);
     }
//Disclaimer
   if(IsVisualMode())
     {
      if(MessageBox("Trading financial instruments like CFD's, always involves a possible risk of losing the initial deposit. "+
         "The hypothetical performance trading results, that are presented on www.coensio.com website are "+
         "generally prepared with the benefit of hindsight. Furthermore, this EA version is still in beta phase "+
         "and is meant for educational purposes only. You agree and understand the risks involved. You understand "+
         "and acknowledge, that there is a very high degree of risk involved in automated trading."
         ,"Disclaimer",MB_YESNO)==IDNO) return(false);
     }
//Check user installation
   if(!MQL5InfoInteger(MQL5_TESTING))
     {
      InstallFile=WindowExpertName()+".ins";
      if(!FileIsExist(InstallFile))
        {
         HttpPush(URL+WindowExpertName()+".php?results=0&testerpasses=0&users=1");
         int FileHandle=FileOpen(InstallFile,FILE_READ|FILE_WRITE|FILE_CSV);
         FileWrite(FileHandle,"1");
         FileClose(FileHandle);
        }
     }
//Identify EA version
   int VerificationResults=ReadInternetMsg(URL+WindowExpertName()+".msg");
   if(VerificationResults==0)
     {
      Print(WindowExpertName()," EA is connected!");
     }
   else if(VerificationResults==-1)
     {
      MessageBox("Sorry, this beta version of EA has been disabled! Visit: www.coensio.com","Info");
      Print("Sorry, this beta version of EA has been disabled! Visit: www.coensio.com");
      return(false);
     }
   else
     {
      MessageBox("Cannot connect to coensio server, check your internet connection!","Info");
      Print("Cannot connect to coensio server, check your internet connection!");
      return(false);
     }
   return(true);
  }
//Tester section
void OnTesterInit()
  {
   Print("Start Of Optimization");
   EACheck();
//Clean log file
   LogFile=WindowExpertName()+".log";
   if(FileIsExist(LogFile,0))
     {
      UploadResults(LogFile);
      FileDelete(LogFile);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double OnTester()
  {
   GetTestStatistics(StatisticValues);
   FrameAdd("Statistics",1,0,StatisticValues);
   return(0.0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTesterPass()
  {
   string Name ="";  // Public name/frame label
   ulong  Pass =0;   // Number of the optimization pass at which the frame is added
   long   Id   =0;   // Public id of the frame
   double Val  =0.0; // Single numerical value of the frame
   int ParametersCount;
   string Parameters;
   string OptimizationPassesStr;

//Get statistics   
   FrameNext(Pass,Name,Id,Val,StatisticValues);
   FrameInputs(Pass,ParameterValues,ParametersCount);
   OptimizationPassesStr=IntegerToString(Pass,1);
   if(StatisticValues[C_INDEX]>=IndexToLog && StatisticValues[C_EQUITYDD_PERCENT]<DrawDownToLog && StatisticValues[C_GROWTH]>GrowthToLog)
     {
      Print("OptimizationPass["+OptimizationPassesStr+"] PF[",DoubleToString(StatisticValues[C_PROFIT_FACTOR],2),"] GR[",DoubleToString(StatisticValues[C_GROWTH],2),"] EQDD[",DoubleToString(StatisticValues[C_EQUITYDD_PERCENT],2),"] CI[",DoubleToString(StatisticValues[C_INDEX],3),"] BDD[",DoubleToString(StatisticValues[C_BALANCE_DD],0),"]");
      string Results;
      int RiskMaxSet=1;
      int LotSizeSet=1;
      for(int i=0; i<ParametersCount; i++)
        {
         if(StringFind(ParameterValues[i],"RiskMax=0",0)>-1) RiskMaxSet=0;
         if(StringFind(ParameterValues[i],"LotSize=0",0)>-1) LotSizeSet=0;
        }
      if((RiskMaxSet==1) || ((RiskMaxSet==0) && (LotSizeSet==0)))
        {
         Results="InitialDeposit: "+DoubleToString(StatisticValues[C_DEPOSIT],0)+
                 " Profit: "+DoubleToString(StatisticValues[C_PROFIT],0)+
                 " Growth: "+DoubleToString(StatisticValues[C_GROWTH],2)+"%"+
                 " ProfitFactor: "+DoubleToString(StatisticValues[C_PROFIT_FACTOR],2)+
                 " EquityDrawdown: "+DoubleToString(StatisticValues[C_EQUITYDD_PERCENT],2)+"%"+
                 " Trades: "+DoubleToString(StatisticValues[C_TRADES],0)+
                 " CoensioIndex: "+DoubleToString(StatisticValues[C_INDEX],3)+
                 " BalanceDrawDown: "+DoubleToString(StatisticValues[C_BALANCE_DD],0)+
                 " Proportional";
        }
      if((RiskMaxSet==0) && (LotSizeSet==1))
        {
         Results="InitialDeposit: "+DoubleToString(StatisticValues[C_DEPOSIT],0)+
                 " Profit: "+DoubleToString(StatisticValues[C_PROFIT],0)+
                 " Growth: "+DoubleToString(StatisticValues[C_GROWTH],2)+"%"+
                 " ProfitFactor: "+DoubleToString(StatisticValues[C_PROFIT_FACTOR],2)+
                 " EquityDrawdown: "+DoubleToString(StatisticValues[C_EQUITYDD_PERCENT],2)+"%"+
                 " Trades: "+DoubleToString(StatisticValues[C_TRADES],0)+
                 " CoensioIndex: "+DoubleToString(StatisticValues[C_INDEX],3)+
                 " BalanceDrawDown: "+DoubleToString(StatisticValues[C_BALANCE_DD],0)+
                 " Fixed";
        }
      for(int i=0; i<ParametersCount; i++)
        {
         Parameters+=OptimizationPassesStr+" "+ParameterValues[i]+" "+DoubleToString(StatisticValues[C_INDEX],3)+"\n";
        }
      //Update results LogFile
      int FileHandle=FileOpen(LogFile,FILE_READ|FILE_WRITE|FILE_CSV);
      if(FileHandle==-1) Print("Log file is locked! Restart Terminal!");
      FileSeek(FileHandle,0,SEEK_END);
      FileWrite(FileHandle,OptimizationPassesStr+" "+Results+" "+UserName);
      FileSeek(FileHandle,0,SEEK_END);
      FileWrite(FileHandle,Parameters);
      FileClose(FileHandle);
     }
   OptimizationPasses=IntegerToString(Pass+1,1);
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTesterDeinit()
  {
//Update passes to server
   Print("OptimizationPasses: ",OptimizationPasses);
   HttpPush(URL+WindowExpertName()+".php?results=0&testerpasses="+OptimizationPasses+"&users=0");
   if(FileIsExist(LogFile,0))
     {
      UploadResults(LogFile);
      FileDelete(LogFile);
     }
   Print("End Of Optimization");
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UploadResults(string File)
  {
//TODO: CLEAN THIS MESS!
//Seek Top 25 results
   int n=0;
   int i=0;
   int NumOfPasses=0;
   string ArrayExploded[];
   double CoensioIndex;
   double CoensioIndexList[];
   double CoensioIndexListOK[];
   double TopTenPasses[];
   string TopTenString="";
   string FileLine;
   string TestId;
   ArrayResize(TopTenPasses,TopPasses);
//Generate unique id number (Timebased)
   TestId=DoubleToStr(StringToDouble(TimeLocal()),0);

   if(FileIsExist(File,0))
     {
      Print("Interesting optimization results are found!");
      Print("Sharing top ten results with coensio server...it can take a while");
      //Read logfile   
      int FileHandle=FileOpen(File,FILE_READ|FILE_WRITE|FILE_CSV);
      if(FileHandle==-1) Print("Log file is locked! Restart Terminal!");
      //Create CoensioIndex list  
      while(!FileIsEnding(FileHandle))
        {
         FileLine=FileReadString(FileHandle);
         if(StringFind(FileLine,"CoensioIndex",0)>-1)
           {
            StringSplit(FileLine,StringGetCharacter(" ",0),ArrayExploded);
            CoensioIndex=StringToDouble(ArrayExploded[14]);
            ArrayResize(CoensioIndexList,n+1);
            CoensioIndexList[n]=CoensioIndex;
            n++;
           }
        }
      //Sort CoensioIndex list in descending order
      //Series arrays are sorted in descending order!
      ArraySetAsSeries(CoensioIndexList,true);
      ArraySort(CoensioIndexList);

      //Find all unique index numbers. TODO: Improve this mess!
      n=0;
      for(i=1; i<ArraySize(CoensioIndexList); i++)
        {
         if(i==1)
           {
            ArrayResize(CoensioIndexListOK,n+1);
            CoensioIndexListOK[n]=CoensioIndexList[i-1];
            n++;
           }
         if(CoensioIndexList[i]!=CoensioIndexList[i-1])
           {
            ArrayResize(CoensioIndexListOK,n+1);
            CoensioIndexListOK[n]=CoensioIndexList[i];
            n++;
           }
        }
      //Fixing problem with only 1 result
      if(ArraySize(CoensioIndexList)==1)
        {
         ArrayResize(CoensioIndexListOK,n+1);
         CoensioIndexListOK[0]=CoensioIndexList[0];
        }
      if(ArraySize(CoensioIndexListOK)>=TopPasses) NumOfPasses=TopPasses;
      else NumOfPasses=ArraySize(CoensioIndexListOK);

      //Find TopTen passes
      FileSeek(FileHandle,-1,0);
      while(!FileIsEnding(FileHandle))
        {
         FileLine=FileReadString(FileHandle);
         if(StringFind(FileLine,"CoensioIndex",0)>-1)
           {
            StringSplit(FileLine,StringGetCharacter(" ",0),ArrayExploded);
            CoensioIndex=StringToDouble(ArrayExploded[14]);
            for(i=0; i<NumOfPasses; i++)
              {
               if(CoensioIndex==CoensioIndexListOK[i]) TopTenPasses[i]=ArrayExploded[0];
              }
           }
        }
      string TopTenStringInfo="";
      for(i=0; i<NumOfPasses; i++)
        {
         TopTenString+="#"+DoubleToString(TopTenPasses[i],0)+"#";
         TopTenStringInfo+=DoubleToString(TopTenPasses[i],0)+" ";
        }
      Print("Interesting passes (Top25): ",TopTenStringInfo);
      //Upload logfile results   
      FileSeek(FileHandle,-1,0);
      while(!FileIsEnding(FileHandle))
        {
         FileLine=FileReadString(FileHandle);
         if(StringLen(FileLine)!=0)
           {
            StringSplit(FileLine,StringGetCharacter(" ",0),ArrayExploded);
            string StrToFind="#"+ArrayExploded[0]+"#";
            if(StringFind(TopTenString,StrToFind,0)>-1)
              {
               StringReplace(FileLine,"CHART",Symbol());
               StringReplace(FileLine,"CurrencyPair1Info","CurrencyPair1");
               HttpPush(URL+WindowExpertName()+".php?results="+TestId+FileLine+"&testerpasses=0$users=0");
               Print(FileLine);
               Sleep(500);
              }
           }
        }
      FileClose(FileHandle);
     }
   Print("Results uploaded!");
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void GetTestStatistics(double &StatsArray[])
  {
   StatsArray[0]=TesterStatistics(STAT_INITIAL_DEPOSIT);
   StatsArray[1]=TesterStatistics(STAT_TRADES);                //Number of executed trades
   StatsArray[2]=TesterStatistics(STAT_PROFIT);                //Net profit upon completion of testing
   StatsArray[3]=TesterStatistics(STAT_PROFIT_FACTOR);         //Profit factor – the STAT_GROSS_PROFIT/STAT_GROSS_LOSS ratio 
   StatsArray[4]=TesterStatistics(STAT_BALANCE_DD);            //Max. balance drawdown
   StatsArray[5]=TesterStatistics(STAT_EQUITYDD_PERCENT);      //Max. equity drawdown in %
   StatsArray[6]=100*(TesterStatistics(STAT_PROFIT)/TesterStatistics(STAT_INITIAL_DEPOSIT)); //Growth in %
   StatsArray[7]=StatsArray[3]*(StatsArray[6]/MathPow(StatsArray[5],2));  //Coeniso Index = profit factor * growth factor/drawdown^2
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void HttpPush(string addr)
  {
   InternetAttemptConnect(0);
   int hInternetSession=InternetOpenW("Microsoft Internet Explorer",0,"","",0);
   int hURL=InternetOpenUrlW(hInternetSession,addr,"",0,0,0);
//Http access
   int BufLen=2048;
   int Ind=0;
   uchar Buf0[2048];
   int iRes;
   iRes=HttpQueryInfoW(hURL,HTTP_QUERY_CONTENT_LENGTH,Buf0,BufLen,Ind);
   InternetCloseHandle(hInternetSession);
   return;
  }
//EA version identification
int ReadInternetMsg(string Address)
  {
//Print(Address);
   InternetAttemptConnect(0);
   int hInternetSession=InternetOpenW("Microsoft Internet Explorer",0,"","",0); //Nobody likes IE!
   int hURL=InternetOpenUrlW(hInternetSession,Address,"",0,FLAG_RELOAD|FLAG_PRAGMA_NOCACHE,0);
//Http read
   int BufLen=2048;
   int Ind=0;
   uchar Buf0[2048];
   string MsgStr="";
   int iRes;
   iRes=HttpQueryInfoW(hURL,HTTP_QUERY_CONTENT_LENGTH,Buf0,BufLen,Ind);

   for(int k=0;k<BufLen;k++) { MsgStr=MsgStr+CharToString(Buf0[k]);}
//Process data
   int dwBytesRead[1];
   uchar buffer[1024];
   CoensioMsg="";
   CoensioCode="";

   while(InternetReadFile(hURL,buffer,1024,dwBytesRead))
     {
      if(dwBytesRead[0]==0) break;
      for(int i=0; i<StrToInteger(MsgStr); i++)
        {
         if(i<16) CoensioCode=CoensioCode+CharToString(buffer[i]);
         if(i>16) CoensioMsg=CoensioMsg+CharToString(buffer[i]);
        }
     }
   InternetCloseHandle(hInternetSession);
   InternetCloseHandle(hURL);
   if(StringCompare(CoensioCode,AccessCode,false)==0)
     {
      Print(CoensioMsg);
      return(0);
     }
   else
     {
      Print("Invalid verification code ",CoensioCode);
      return(-1);
     }
   return(-2);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DisplayInfo()
  {
   int Hoffset1 = 5;
   int Hoffset2 = 60;

//Display labels
   if(ObjectFind("Motd")<0) DrawObjectLabel("Motd",CoensioMsg,300,0,Yellow,8);
   if(ObjectFind("Name")<0) DrawObjectLabel("Name",EaName,Hoffset1,20,Yellow,8);
   if(ObjectFind("Info")<0) DrawObjectLabel("Info","© Copyright 2013 www.Coensio.com",Hoffset1,30,Yellow,8);
//Right side info  
   if(ObjectFind("Balance")<0) DrawObjectLabel("Balance","Balance: ",Hoffset1,40,Yellow,8);
   if(ObjectFind("BalanceVal")>=0) ObjectDelete(0,"BalanceVal");
   if(ObjectFind("BalanceVal")<0)DrawObjectLabel("BalanceVal",DoubleToString(AccountBalance(),1),Hoffset2,40,Yellow,8);
   if(ObjectFind("Floating")<0) DrawObjectLabel("Floating","Floating: ",Hoffset1,50,Yellow,8);
   if(ObjectFind("FloatingVal")>=0) ObjectDelete(0,"FloatingVal");
   if(ObjectFind("FloatingVal")<0) DrawObjectLabel("FloatingVal",DoubleToString(AccountEquity()-AccountBalance(),1),Hoffset2,50,Yellow,8);
   return;
  }
//+------------------------------------------------------------------+
