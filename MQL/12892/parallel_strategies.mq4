//+------------------------------------------------------------------+
//|                                               Parallel Strategies|
//|                                    Copyright 2015, Rodolphe Ahmad|
//|                            https://www.mql5.com/en/users/rodoboss|
//+------------------------------------------------------------------+
#property copyright   "Rodolphe Ahmad  © 2015"
#property link        "mql5.com/en/users/rodoboss"
#property version     "1.00"
#property strict
//---
#import "stdlib.ex4"
string ErrorDescription(int e);
#import
//+------------------------------------------------------------------+
//| Define Area                                                      |
//+------------------------------------------------------------------+
#define USE true
#define DONTUSE false
#define Nothing false
#define Anything true
#define FLIPFLOP false
#define Me "Me"
#define Child "Child"
#define ChildChild "ChildChild"
#define ChildChildChild "ChildChildChild"
#define Parent "Parent"
#define ParentParent "ParentParent"
#define ParentParentParent "ParentParentParent"
#define high    "High"
#define low     "Low"
#define open    "Open"
#define close   "Close"
#define volume  "Volume"
#define bullish "Bullish"
#define bearish "Bearish"
#define A "A"
#define B "B"
#define C "C"
#define D "D"
#define M1   PERIOD_M1
#define M5   PERIOD_M5
#define M15  PERIOD_M15
#define M30  PERIOD_M30
#define H1   PERIOD_H1
#define H4   PERIOD_H4
#define D1   PERIOD_D1
#define W1   PERIOD_W1
#define MN   PERIOD_MN1
//+------------------------------------------------------------------+
//| Functions and procedures and sequences                           |
//+------------------------------------------------------------------+
void in(string a) {}
//+------------------------------------------------------------------+
//| Price Action Procedures                                          |
//+------------------------------------------------------------------+
int pToPeriod(string P)
  {
   in("1 5 15 30 60 240 1440 10080 43200");
//---
   if(P=="1" || P=="5" || P=="15" || P=="30" || P=="60" || P=="240" || P=="1440" || P=="10080" || P=="43200")
     {
      return (int)P;
     }
   if(P==Me)
     {
      return Period();
     }
   if(P==Parent)
     {
      //---
      if(Period()==M1)
        {
         return M5;
        }
      if(Period()==M5)
        {
         return M15;
        }
      if(Period()==M15)
        {
         return M30;
        }
      if(Period()==M30)
        {
         return H1;
        }
      if(Period()==H1)
        {
         return H4;
        }
      if(Period()==H4)
        {
         return D1;
        }
      if(Period()==D1)
        {
         return W1;
        }
      if(Period()==W1)
        {
         return MN;
        }
      if(Period()==MN)
        {
         return MN;
        }
     }
   if(P==ParentParent)
     {

      if(Period()==M1)
        {
         return M15;
        }
      if(Period()==M5)
        {
         return M30;
        }
      if(Period()==M15)
        {
         return H1;
        }
      if(Period()==M30)
        {
         return H4;
        }
      if(Period()==H1)
        {
         return D1;
        }
      if(Period()==H4)
        {
         return W1;
        }
      if(Period()==D1)
        {
         return MN;
        }
      if(Period()==W1)
        {
         return MN;
        }
      if(Period()==MN)
        {
         return MN;
        }
     }
   if(P==ParentParentParent)
     {
      if(Period()==M1)
        {
         return M30;
        }
      if(Period()==M5)
        {
         return H1;
        }
      if(Period()==M15)
        {
         return H4;
        }
      if(Period()==M30)
        {
         return D1;
        }
      if(Period()==H1)
        {
         return W1;
        }
      if(Period()==H4)
        {
         return MN;
        }
      if(Period()==D1)
        {
         return MN;
        }
      if(Period()==W1)
        {
         return MN;
        }
      if(Period()==MN)
        {
         return MN;
        }
     }
   if(P==Child)
     {
      if(Period()==M1)
        {
         return M1;
        }
      if(Period()==M5)
        {
         return M1;
        }
      if(Period()==M15)
        {
         return M5;
        }
      if(Period()==M30)
        {
         return M15;
        }
      if(Period()==H1)
        {
         return M30;
        }
      if(Period()==H4)
        {
         return H1;
        }
      if(Period()==D1)
        {
         return H4;
        }
      if(Period()==W1)
        {
         return D1;
        }
      if(Period()==MN)
        {
         return W1;
        }
     }
   if(P==ChildChild)
     {
      if(Period()==M1)
        {
         return M1;
        }
      if(Period()==M5)
        {
         return M1;
        }
      if(Period()==M15)
        {
         return M1;
        }
      if(Period()==M30)
        {
         return M5;
        }
      if(Period()==H1)
        {
         return M15;
        }
      if(Period()==H4)
        {
         return M30;
        }
      if(Period()==D1)
        {
         return H1;
        }
      if(Period()==W1)
        {
         return H4;
        }
      if(Period()==MN)
        {
         return D1;
        }
     }
   if(P==ChildChildChild)
     {
      if(Period()==M1)
        {
         return M1;
        }
      if(Period()==M5)
        {
         return M1;
        }
      if(Period()==M15)
        {
         return M1;
        }
      if(Period()==M30)
        {
         return M1;
        }
      if(Period()==H1)
        {
         return M5;
        }
      if(Period()==H4)
        {
         return M15;
        }
      if(Period()==D1)
        {
         return M30;
        }
      if(Period()==W1)
        {
         return H1;
        }
      if(Period()==MN)
        {
         return H4;
        }
     }
//---
   return Period();
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool barIsBullish(int i,string P)
  {
   return ( getBarClose(i,P) >= getBarOpen(i,P));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool barIsBearish(int i,string P)
  {
   return ( getBarClose(i,P) < getBarOpen(i,P));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double    getBarVolume(int i,string P)
  {
   return  (double)iVolume(Symbol(),pToPeriod(P),i);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double    getBarHigh(int i,string P)
  {
   return (double)iHigh(Symbol(),pToPeriod(P),i);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double    getBarLow(int i,string P)
  {
   return(double)iLow(Symbol(),pToPeriod(P),i);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double    getBarOpen(int i,string P)
  {
   return (double)iOpen(Symbol(),pToPeriod(P),i);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double    getBarClose(int i,string P)
  {
//  iClose(Sym,1,0))
   return (double)iClose(Symbol(),pToPeriod(P),i);
  }
//+------------------------------------------------------------------+
//| On new bar function     IsBarClosed(0, true)                     |
//+------------------------------------------------------------------+
bool IsBarClosed(int timeframe,bool reset)
  {
   static datetime lastbartime;
   if(timeframe==-1)
     {
      if(reset)
         lastbartime=0;
      else
         lastbartime=iTime(NULL,timeframe,0);
      return(true);
     }
   if(iTime(NULL,timeframe,0)==lastbartime) // wait for new bar
      return(false);
   if(reset)
      lastbartime=iTime(NULL,timeframe,0);
   return(true);
  }
//+------------------------------------------------------------------+
//| Class EA                                                         |
//+------------------------------------------------------------------+
class EA
  {
public:
   bool              flipme;
   double            maxspread;
   bool              M;
   int               Ms,Mf;
   bool              T;
   int               Ts,Tf;
   bool              W;
   int               Ws,Wf;
   bool              TH;
   int               THs,THf;
   bool              F;
   int               Fs,Ff;
   double
   StopLoss,
   TakeProfit,
   Lots,
   TrailingStop;
   int
   Slippage,
   Magic;
   string
   Sym;
   bool              TL;
   bool              TS;
   bool              TCL;
   bool              TCS;
   int               LongTicket;
   int               ShortTicket;
   int               PreviousTime;
   bool              RManagement;
   double            RPercent;
   void              EA();
   void             ~EA();
   double            PipsToDecimal(double Pips);
   int               Buy();
   int               Sell();
   void              CloseBuy(int Ticket);
   void              CloseSell(int Ticket);
   void              DoLongTrailingStop(int Ticket);
   void              DoShortTrailingStop(int Ticket);
   void              Trade(bool a,bool b,bool c,bool d);
   bool              TimeDayOk();
   double            CalculateLots();
   int               CountOrders(int Type);
   void              Create(double ALots,
                            int ASlippage,
                            int AMagic,bool flipme,
                            bool rm,double rp,
                            string APSymbol,
                            double AStopLoss,
                            double ATakeProfit,
                            double ATrailingStop,
                            double mxspread,
                            bool M__,
                            int Ms__,int Mf__,
                            bool T__,
                            int Ts__,int Tf__,
                            bool W__,
                            int Ws__,int Wf__,
                            bool TH__,
                            int THs__,int THf__,
                            bool F__,
                            int Fs__,int Ff__);
  };
//+------------------------------------------------------------------+
//| EA Constructor                                                   |
//+------------------------------------------------------------------+
void EA::EA()
  {
  }
//+------------------------------------------------------------------+
//| EA Destructor                                                    |
//+------------------------------------------------------------------+
void EA::~EA()
  {
  }
//+------------------------------------------------------------------+
//| EA Function to count orders                                      |
//+------------------------------------------------------------------+
int   EA::CountOrders(int Type)
  {
   int _CountOrd=0;
   for(int i=0;i<OrdersTotal();i++)
     {
      int o=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Sym && OrderType()==Type && OrderMagicNumber()==Magic)
        {
         _CountOrd++;
        }
     }
   return(_CountOrd);
  }
//+------------------------------------------------------------------+
//| Function to bind day with time with trade                        |
//+------------------------------------------------------------------+
bool  EA::TimeDayOk()
  {
//+------------------------------------------------------------------+
//| Monday                                                           |
//+------------------------------------------------------------------+
   if(( DayOfWeek()==MONDAY && M))
     {
      int Start_Hour=Ms;
      int Finish_Hour=Mf;
      double Current_Time=TimeHour(TimeCurrent());
      if(Start_Hour==0) Start_Hour=24; if(Finish_Hour==0) Finish_Hour=24; if(Current_Time==0) Current_Time=24;
      if(Start_Hour<Finish_Hour)
         if( (Current_Time < Start_Hour) || (Current_Time >= Finish_Hour) ) return(false);
      if(Start_Hour>Finish_Hour)
         if( (Current_Time < Start_Hour) && (Current_Time >= Finish_Hour) ) return(false);
      return(true);
     }
//+------------------------------------------------------------------+
//| Tuesday                                                          |
//+------------------------------------------------------------------+
   if(( DayOfWeek()==TUESDAY && T))
     {
      int Start_Hour=Ts;
      int Finish_Hour=Tf;
      double Current_Time=TimeHour(TimeCurrent());
      if(Start_Hour==0) Start_Hour=24; if(Finish_Hour==0) Finish_Hour=24; if(Current_Time==0) Current_Time=24;
      if(Start_Hour<Finish_Hour)
         if( (Current_Time < Start_Hour) || (Current_Time >= Finish_Hour) ) return(false);
      if(Start_Hour>Finish_Hour)
         if( (Current_Time < Start_Hour) && (Current_Time >= Finish_Hour) ) return(false);
      return(true);
     }
//+------------------------------------------------------------------+
//| Wednesday                                                        |
//+------------------------------------------------------------------+
   if(( DayOfWeek()==WEDNESDAY && W))
     {
      int Start_Hour=Ws;
      int Finish_Hour=Wf;
      double Current_Time=TimeHour(TimeCurrent());
      if(Start_Hour==0) Start_Hour=24; if(Finish_Hour==0) Finish_Hour=24; if(Current_Time==0) Current_Time=24;
      if(Start_Hour<Finish_Hour)
         if( (Current_Time < Start_Hour) || (Current_Time >= Finish_Hour) ) return(false);
      if(Start_Hour>Finish_Hour)
         if( (Current_Time < Start_Hour) && (Current_Time >= Finish_Hour) ) return(false);
      return(true);
     }
//+------------------------------------------------------------------+
//| Thursday                                                         |
//+------------------------------------------------------------------+
   if(( DayOfWeek()==THURSDAY && TH))
     {
      int Start_Hour=THs;
      int Finish_Hour=THf;
      double Current_Time=TimeHour(TimeCurrent());
      if(Start_Hour==0) Start_Hour=24; if(Finish_Hour==0) Finish_Hour=24; if(Current_Time==0) Current_Time=24;
      if(Start_Hour<Finish_Hour)
         if( (Current_Time < Start_Hour) || (Current_Time >= Finish_Hour) ) return(false);
      if(Start_Hour>Finish_Hour)
         if( (Current_Time < Start_Hour) && (Current_Time >= Finish_Hour) ) return(false);
      return(true);
     }
//+------------------------------------------------------------------+
//| Friday                                                           |
//+------------------------------------------------------------------+
   if(( DayOfWeek()==FRIDAY && F))
     {
      int Start_Hour=Fs;
      int Finish_Hour=Ff;
      double Current_Time=TimeHour(TimeCurrent());
      if(Start_Hour==0) Start_Hour=24; if(Finish_Hour==0) Finish_Hour=24; if(Current_Time==0) Current_Time=24;
      if(Start_Hour<Finish_Hour)
         if( (Current_Time < Start_Hour) || (Current_Time >= Finish_Hour) ) return(false);
      if(Start_Hour>Finish_Hour)
         if( (Current_Time < Start_Hour) && (Current_Time >= Finish_Hour) ) return(false);
      return(true);
     }
//very important
   return false;
  }
//+------------------------------------------------------------------+
//| How did ea trade ?? function                                     |
//+------------------------------------------------------------------+
void EA::Trade(bool TL1,bool TS1,bool TCL1,bool TCS1)
  {
   TL  =  TL1;
   TS  =  TS1;
   TCL =  TCL1;
   TCS =  TCS1;
   if(OrderSelect(LongTicket,SELECT_BY_TICKET))
     {
      if(OrderCloseTime()!=0)
        {
         LongTicket=-1;
        }
     }
   if(OrderSelect(ShortTicket,SELECT_BY_TICKET))
     {
      if(OrderCloseTime()!=0)
        {
         ShortTicket=-1;
        }
     }
   if(TCL)
     {
      if(LongTicket>-1)
        {
         CloseBuy(LongTicket);
         LongTicket=-1;
        }
     }
   if(TCS)
     {
      if(ShortTicket>-1)
        {
         CloseSell(ShortTicket);
         ShortTicket=-1;
        }
     }
   if(TL)
     {
      if(LongTicket==-1)
        {
         if(ShortTicket>-1 && flipme)
           {
            CloseSell(ShortTicket);
            ShortTicket=-1;
           }
         if(TimeDayOk() && MarketInfo(Sym,MODE_SPREAD)<=maxspread)
            LongTicket=Buy();
        }
     }
   if(TS)
     {
      if(ShortTicket==-1)
        {
         if(LongTicket>-1 && flipme)
           {
            CloseBuy(LongTicket);
            LongTicket=-1;
           }
         if(TimeDayOk() && MarketInfo(Sym,MODE_SPREAD)<=maxspread)
            ShortTicket=Sell();
        }
     }
   if(LongTicket>-1)
     {
      DoLongTrailingStop(LongTicket);
     }
   if(ShortTicket>-1)
     {
      DoShortTrailingStop(ShortTicket);
     }
  }
//+------------------------------------------------------------------+
//| EA create the "constructor"                                      |
//+------------------------------------------------------------------+
void EA::Create(double ALots,
                int ASlippage,
                int AMagic,bool flipflop,
                bool rm,double rp,
                string APSymbol,
                double AStopLoss,
                double ATakeProfit,
                double ATrailingStop,
                double mxspread,
                bool M__,
                int Ms__,int Mf__,
                bool T__,
                int Ts__,int Tf__,
                bool W__,
                int Ws__,int Wf__,
                bool TH__,
                int THs__,int THf__,
                bool F__,
                int Fs__,int Ff__)
  {
   Lots=ALots;
   Slippage=ASlippage;
   Magic=AMagic;
   Sym=APSymbol;
   StopLoss=(AStopLoss>0)?PipsToDecimal(AStopLoss):0;
   TakeProfit=(ATakeProfit>0)?PipsToDecimal(ATakeProfit):0;
   TrailingStop=(ATrailingStop>0)?PipsToDecimal(ATrailingStop):0;
   TL = false;
   TS = false;
   TCL = false;
   TCS = false;
   LongTicket=-1;
   ShortTicket=-1;
   PreviousTime=0;
   M=M__;
   Ms = Ms__;
   Mf = Mf__;
   T=T__;
   Ts = Ts__;
   Tf = Tf__;
   W=W__;
   Ws = Ws__;
   Wf = Wf__;
   TH = TH__;
   THs = THs__;
   THf = THf__;
   F=F__;
   Fs = Fs__;
   Ff = Ff__;
   maxspread=mxspread;
   RManagement=rm;
   RPercent=rp;
   flipme=flipflop;
  }
//+------------------------------------------------------------------+
//| Pips to decimal for all digits broker                            |
//+------------------------------------------------------------------+
double EA::PipsToDecimal(double Pips)
  {
   double ThePoint=SymbolInfoDouble(Sym,SYMBOL_POINT);
   if(ThePoint==0.0001 || ThePoint==0.00001)
     {
      return Pips * 0.0001;
     }
   else if(ThePoint==0.01 || ThePoint==0.001)
     {
      return Pips * 0.01;
     }
   else
     {
      return 0;
     }
  }
//+------------------------------------------------------------------+
//| EA calculate lots                                                |
//+------------------------------------------------------------------+
double EA::CalculateLots()
  {
//+------------------------------------------------------------------+
//| Return default lot size if no money management                   |
//+------------------------------------------------------------------+
   if(!RManagement)
     {
      if(Lots<MarketInfo(Sym,MODE_MINLOT))
        { return MarketInfo(Sym,MODE_MINLOT); }

      if(Lots>MarketInfo(Sym,MODE_MAXLOT))
        { return MarketInfo(Sym,MODE_MAXLOT); }
      return Lots;
     }
//+------------------------------------------------------------------+
//| Calcute Money Management With Risk                               |
//+------------------------------------------------------------------+
   else
     {
      double lottoreturn=0;
      double MinLots=MarketInfo(Sym,MODE_MINLOT);
      double MaxLots=MarketInfo(Sym,MODE_MAXLOT);
      lottoreturn=AccountFreeMargin()/100000*RPercent;
      lottoreturn=MathMin(MaxLots,MathMax(MinLots,lottoreturn));
      if(MinLots<0.1)lottoreturn=NormalizeDouble(lottoreturn,2);
      else
        {
         if(MinLots<1)lottoreturn=NormalizeDouble(lottoreturn,1);
         else lottoreturn=NormalizeDouble(lottoreturn,0);
        }
      if(lottoreturn<MinLots)Lots=MinLots;
      if(lottoreturn>MaxLots)Lots=MaxLots;
      return(lottoreturn);
     }
   return Lots;
  }
//+------------------------------------------------------------------+
//| Buy                                                              |
//+------------------------------------------------------------------+
int EA::Buy()
  {
   PreviousTime=(int)TimeMinute(TimeLocal());
   double SL = (StopLoss>0)?MarketInfo(Sym,MODE_BID)-StopLoss:0;
   double TP = (TakeProfit>0)?MarketInfo(Sym,MODE_ASK)+TakeProfit:0;
   while(IsTradeContextBusy()) Sleep(1000);
   RefreshRates();
   return OrderSend(
                    Sym,
                    OP_BUY,
                    CalculateLots(),
                    MarketInfo(Sym,MODE_ASK),
                    Slippage,
                    SL,
                    TP,
                    NULL /*"Long Position" */,
                    Magic
                    );
  }
//+------------------------------------------------------------------+
//| Sell                                                             |
//+------------------------------------------------------------------+
int EA::Sell()
  {
   PreviousTime=(int)TimeMinute(TimeLocal());
   double SL = (StopLoss>0)?MarketInfo(Sym,MODE_ASK)+StopLoss:0;
   double TP = (TakeProfit>0)?MarketInfo(Sym,MODE_BID)-TakeProfit:0;
   while(IsTradeContextBusy()) Sleep(1000);
   RefreshRates();
   return OrderSend(
                    Sym,
                    OP_SELL,
                    CalculateLots(),
                    MarketInfo(Sym,MODE_BID),
                    Slippage,
                    SL,
                    TP,
                    NULL /*"Short Position" */,
                    Magic
                    );
  }
//+------------------------------------------------------------------+
//| Close buy                                                        |
//+------------------------------------------------------------------+
void EA::CloseBuy(int Ticket)
  {
   if(OrderSelect(Ticket,SELECT_BY_TICKET))
      int o0=OrderClose(Ticket,OrderLots(),MarketInfo(Sym,MODE_BID),Slippage);
  }
//+------------------------------------------------------------------+
//| Close sell                                                       |
//+------------------------------------------------------------------+
void EA::CloseSell(int Ticket)
  {
   if(OrderSelect(Ticket,SELECT_BY_TICKET))
      int o0=OrderClose(Ticket,OrderLots(),MarketInfo(Sym,MODE_ASK),Slippage);
  }
//+------------------------------------------------------------------+
//| Long trailing stop                                               |
//+------------------------------------------------------------------+
void EA::DoLongTrailingStop(int Ticket)
  {
   if(TrailingStop>0)
     {
      RefreshRates();
      int o20=OrderSelect(Ticket,SELECT_BY_TICKET);
      if(MarketInfo(Sym,MODE_BID)-OrderOpenPrice()>TrailingStop)
        {
         if(MarketInfo(Sym,MODE_BID)-TrailingStop>OrderStopLoss() || OrderStopLoss()==0)
           {
            int o0=OrderModify(OrderTicket(),
                               OrderOpenPrice(),
                               MarketInfo(Sym,MODE_BID)-TrailingStop,
                               OrderTakeProfit(),
                               0
                               );
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Short trailing stop                                              |
//+------------------------------------------------------------------+
void EA::DoShortTrailingStop(int Ticket)
  {
   if(TrailingStop>0)
     {
      RefreshRates();
      int o0=OrderSelect(Ticket,SELECT_BY_TICKET);
      if(OrderOpenPrice()-MarketInfo(Sym,MODE_ASK)>TrailingStop)
        {
         if(OrderStopLoss()>MarketInfo(Sym,MODE_ASK)+TrailingStop || OrderStopLoss()==0)
           {
            int o02=OrderModify(OrderTicket(),
                                OrderOpenPrice(),
                                MarketInfo(Sym,MODE_ASK)+TrailingStop,
                                OrderTakeProfit(),
                                0
                                );
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Extern Variables                                                 |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Expert Info                                                      |
//+------------------------------------------------------------------+
extern string expertinfo="  Parallel Strategies  EA  ";  // ===========[ <EXPERT INFO> ]=========
extern string expdeveloper="https://www.mql5.com/en/users/rodoboss"; //By
//+------------------------------------------------------------------+
//| Expert Magic Numbers                                             |
//+------------------------------------------------------------------+
extern string magicnumbers="===========[ <MAGIC NUMBERS> ]=========";  // ===========[ <MAGIC NUMBERS> ]=========
extern int    Magic1 = 11;        // Magic Number Strategy 1
extern int    Magic2 = 12;        // Magic Number Strategy 2
extern int    Magic3 = 13;        // Magic Number Strategy 3
extern int    Magic4 = 14;        // Magic Number Strategy 4
extern string Strategieslist="===========[ <Strategies> ]=========";  // ===========[ <List Strategies> ]=========
extern bool usemovingcrossea=true;  // USE moving cross Strategy ?
input ENUM_MA_METHOD mamethodes=  MODE_EMA; // Fast Moving Average Methode
input ENUM_MA_METHOD mamethodes2= MODE_SMA; //Slow Moving Average Methode
extern bool usemacdbreakout = true; // USE Macd breakout  Strategy ?
extern bool useheikenahsiea = true; // USE Heiken Ashi Strategy ?
extern bool useheikenashiandmacdea=true; // USE Heiken and MACD Strategy ?
//+------------------------------------------------------------------+
//| Expert Money Managemnet                                          |
//+------------------------------------------------------------------+
extern string mm="===========[ <Money Management> ]=========";  // ===========[ <Money Management> ]=========
extern double exLots=0.01;  // Lot Size
extern  bool   RiskMM=false; // ---------------------Risk Management
extern  double RiskPercent=5;// ------------------   Risk Percentage
//+------------------------------------------------------------------+
//| Order management                                                 |
//+------------------------------------------------------------------+
extern string tm="===========[ <Order  Management> ]========="; // ===========[ <Order Management> ]=========
extern  double exStopLoss=00.0;    // Stop Loss
extern  double exTakeProfit=00.0;  // Take Profit
extern  double exTrailingStop=10.0;   // Trailing Stop 
extern  double exmaxSpread=30.0;    //[Max Spread]  
//+------------------------------------------------------------------+
//| Week days agenda                                                 |
//+------------------------------------------------------------------+
extern string WeekDays="===========[ <WEEK DAYS> ]========="; // ===========[ <WEEK DAYS> ]=========
extern string exemple = " ------- Hour settings: set to 24 for no limitations ";
extern bool   Monday=true;              //Monday Trade
extern int    Monday_Start_Hour =24;    //Monday Start Hour
extern int    Monday_Finish_Hour=24;    //Monday Finish Hour
extern bool   Tuesday=true;             //Tuesday Trade
extern int    Tuesday_Start_Hour =24;   //Tuesday Start Hour
extern int    Tuesday_Finish_Hour=24;   //Tuesday Finish Hour
extern bool   Wednesday=true;           //Wednesday Trade
extern int    Wednesday_Start_Hour =24; //Wednesday Start Hour
extern int    Wednesday_Finish_Hour=24; //Wednesday Finish Hour
extern bool   Thursday=true;            //Thursday Trade
extern int    Thursday_Start_Hour =24;  //Thursday Start Hour
extern int    Thursday_Finish_Hour=24;  //Thursday Finish Hour
extern bool   Friday=true;              //Friday Trade
extern int    Friday_Start_Hour =24;    //Friday Start Hour
extern int    Friday_Finish_Hour=24;    //Friday Finish Hour
bool   exflipflop=false;// Close buy to sell && vice versa
//+------------------------------------------------------------------+
//| Instance of Parallele Cheers :)                                  |
//+------------------------------------------------------------------+
EA movingcrossEA,MACDbreakout,heinkenAshiEa,HeinkenAndMacd;
//+------------------------------------------------------------------+
//| Init Funciton                                                    |
//+------------------------------------------------------------------+
bool condbuy,condsell,condstopbuy,condstopsell=false;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   int exSlippage=(Digits==5 || Digits==3)?20:2;   //  Maximum Slippage  
   string str="";
   str+="Expert Name : "+(string) WindowExpertName()+"\n";
   Comment(str);
   movingcrossEA.Create(exLots,exSlippage,Magic1,exflipflop,RiskMM,RiskPercent,Symbol(),exStopLoss,exTakeProfit,exTrailingStop,exmaxSpread,Monday,Monday_Start_Hour,Monday_Finish_Hour,Tuesday,Tuesday_Start_Hour,Tuesday_Finish_Hour,Wednesday,Wednesday_Start_Hour,Wednesday_Finish_Hour,Thursday,Thursday_Start_Hour,Thursday_Finish_Hour,Friday,Friday_Start_Hour,Friday_Finish_Hour);
   MACDbreakout.Create(exLots,exSlippage,Magic2,exflipflop,RiskMM,RiskPercent,Symbol(),exStopLoss,exTakeProfit,exTrailingStop,exmaxSpread,Monday,Monday_Start_Hour,Monday_Finish_Hour,Tuesday,Tuesday_Start_Hour,Tuesday_Finish_Hour,Wednesday,Wednesday_Start_Hour,Wednesday_Finish_Hour,Thursday,Thursday_Start_Hour,Thursday_Finish_Hour,Friday,Friday_Start_Hour,Friday_Finish_Hour);
   heinkenAshiEa.Create(exLots,exSlippage,Magic3,true,RiskMM,RiskPercent,Symbol(),exStopLoss,exTakeProfit,exTrailingStop,exmaxSpread,Monday,Monday_Start_Hour,Monday_Finish_Hour,Tuesday,Tuesday_Start_Hour,Tuesday_Finish_Hour,Wednesday,Wednesday_Start_Hour,Wednesday_Finish_Hour,Thursday,Thursday_Start_Hour,Thursday_Finish_Hour,Friday,Friday_Start_Hour,Friday_Finish_Hour);
   HeinkenAndMacd.Create(exLots,exSlippage,Magic4,true,RiskMM,RiskPercent,Symbol(),exStopLoss,exTakeProfit,exTrailingStop,exmaxSpread,Monday,Monday_Start_Hour,Monday_Finish_Hour,Tuesday,Tuesday_Start_Hour,Tuesday_Finish_Hour,Wednesday,Wednesday_Start_Hour,Wednesday_Finish_Hour,Thursday,Thursday_Start_Hour,Thursday_Finish_Hour,Friday,Friday_Start_Hour,Friday_Finish_Hour);
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| OnDeinit                                                         |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//| ON Tick                                                          |
//+------------------------------------------------------------------+
void OnTick()
  {
   initmovingcrossEA(usemovingcrossea);
   initMACDBREAKOUT(usemacdbreakout);
   initHeikenAshiEa(useheikenahsiea);
   initHeikenAndMacd(useheikenashiandmacdea);
  }
//+------------------------------------------------------------------+
//| Init all instance for parallel EAs...                            |
//+------------------------------------------------------------------+
void initmovingcrossEA(bool a)
  {
   if(a==USE)
     {
      condbuy=getFasMa(0)>getSlowMa(0) && getFasMa(1)<getSlowMa(0);
      condsell=getFasMa(0)<getSlowMa(0) && getFasMa(1)>getSlowMa(0);
      condstopbuy=getFasMa(0)<getSlowMa(0);
      condstopsell=getFasMa(0)>getSlowMa(0);
      movingcrossEA.Trade(
                          (condbuy),
                          (condsell),
                          (condstopbuy),
                          (condstopsell)
                          );
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void  initMACDBREAKOUT(bool a)
  {
   if(a==USE)
     {
      condbuy=CLOSE(1)>rhigh() && macd_main(0)>macd_signal(0);
      condsell=CLOSE(1)<rlow() && macd_main(0)<macd_signal(0);
      condstopbuy=macd_main(0)<macd_signal(0) && macd_main(1)>macd_signal(1);
      condstopsell=macd_main(0)>macd_signal(0) && macd_main(1)<macd_signal(1);
      MACDbreakout.Trade(
                         (condbuy),
                         (condsell),
                         (condstopbuy),
                         (condstopsell)
                         );
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void  initHeikenAshiEa(bool a)
  {
   if(a==USE)
     {
      condbuy=hat(1)==OP_BUY && hat(2)==OP_BUY && hat(2)!=hat(3);
      condsell=hat(1)==OP_SELL && hat(2)==OP_SELL && hat(2)!=hat(3);
      condstopbuy=FLIPFLOP;
      condstopsell=FLIPFLOP;
      heinkenAshiEa.Trade(
                          //    && heinkenAshiEa.CountOrders(OP_SELL)==0
                          // && heinkenAshiEa.CountOrders(OP_SELL)==0
                          //  && heinkenAshiEa.CountOrders(OP_BUY)==0
                          (condbuy),
                          (condsell),
                          (condstopbuy),
                          (condstopsell)
                          );
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void  initHeikenAndMacd(bool a)
  {
   if(a==USE)
     {
      HeinkenAndMacd.Trade(
                           ( hat(1) == OP_BUY  && hat(1) != hat(2) &&CLOSE(1) > rhigh()   && macd_main(0) > macd_signal(0)&& HeinkenAndMacd.CountOrders(OP_SELL)==0  ),
                           ( hat(1) == OP_SELL && hat(1) != hat(2) &&CLOSE(1) < rlow()    && macd_main(0) < macd_signal(0)&& HeinkenAndMacd.CountOrders(OP_BUY)==0 ),
                           (Nothing),
                           (Nothing)
                           );
     }
  }
//+------------------------------------------------------------------+
//| SPECIAL PARAMS for movingcrossEA                                 |
//+------------------------------------------------------------------+
double getFasMa(int Shift)
  {
   return iMA(Symbol(), 0, 12, 0, mamethodes, PRICE_CLOSE, Shift);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double getSlowMa(int Shift)
  {
   return iMA(Symbol(), 0, 36, 0, mamethodes2, PRICE_CLOSE, Shift);
  }
//+------------------------------------------------------------------+
//| End special params for moving cross                              |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Special Params for MACD BREAKOUTS EA                             |
//+------------------------------------------------------------------+
double CLOSE(int shift)
  {
   return iClose(Symbol(),0, shift);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double rhigh()
  {
   int BreakoutPeriod=5;
   return iHigh(Symbol(), Period(), iHighest(Symbol(), Period(), MODE_HIGH, BreakoutPeriod, 2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double rlow()
  {
   int BreakoutPeriod=5;
   return   iLow(Symbol(), Period(), iLowest(Symbol(), Period(), MODE_LOW, BreakoutPeriod, 2));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double macd_main(int shift)
  {
   int    MACD_FastEMA             = 12;
   int    MACD_SlowEMA             = 26;
   int    MACD_SignalPeriod        = 9;
   return iMACD(NULL, 0, MACD_FastEMA, MACD_SlowEMA, MACD_SignalPeriod, PRICE_CLOSE, MODE_MAIN,shift);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double macd_signal(int shift)
  {
   int    MACD_FastEMA             = 12;
   int    MACD_SlowEMA             = 26;
   int    MACD_SignalPeriod        = 9;
   return iMACD(NULL, 0, MACD_FastEMA, MACD_SlowEMA, MACD_SignalPeriod, PRICE_CLOSE, MODE_SIGNAL, shift);
  }
//+------------------------------------------------------------------+
//| End special params for MACD breakout EA                          |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Specials params for HEINKEN ASHI                                 |
//+------------------------------------------------------------------+
int GetHeikenAshiTrend(int shift)
  {
   string indiname="Heiken_Ashi_Smoothed";
   int    MaMetod                  = 2;
   int    MaPeriod                 = 6;
   int    MaMetod2                 = 3;
   int    MaPeriod2                = 2;
   double _open  = iCustom(Symbol(), 0,indiname, MaMetod, MaPeriod, MaMetod2, MaPeriod2, 4, shift);
   double _close = iCustom(Symbol(), 0,indiname, MaMetod, MaPeriod, MaMetod2, MaPeriod2, 5, shift);
   if(_close > _open) return(OP_BUY);
   return(OP_SELL);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double hat(int a)
  {
   return GetHeikenAshiTrend(a);
  }
//+------------------------------------------------------------------+
//| End special params for HEINKEN AHSI                              |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double FasMa(int Shift,string Sym)
  {
   return iMA(Sym, 0, 12, 0, MODE_EMA, PRICE_CLOSE, Shift);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double SlowMa(int Shift,string Sym)
  {
   return iMA(Sym, 0, 36, 0, MODE_SMA, PRICE_CLOSE, Shift);
  }
//+------------------------------------------------------------------+
