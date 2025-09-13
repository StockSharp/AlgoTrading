//+------------------------------------------------------------------+
//|                                                    GurooEx01.mq5 |
//|     Copyright © 2010, Marketing Dreams Ltd. All Rights Reserved. |
//|                     http://community.trading-gurus.com/threads/9 |
//|                                                                  |
//| GuruTrader™ example 1                                            |
//| Object Oriented MQL5 Version 1.0                                 |
//|                                                                  |
//| A quick port of our MQL4 robot example 1 to MQL5 for use with    |
//| the public beta test version of MetaTrader 5. It is written      |
//| using MetaQuotes object oriented framework for MQL5.             |
//|                                                                  |
//| Uses a random number generator to simulate tossing a coin.       |
//| It can be amazingly profitable given that it uses random entries!|                                                                  |
//|                                                                  |
//| Wealth Warning! This expert is for educational purposes only.    |
//| It should NEVER be used on a live account. Past performance is   |
//| in no way indicative of future results!                          |
//+------------------------------------------------------------------+
#property copyright   "Copyright © 2010, Marketing Dreams Ltd."
#property link        "http://community.trading-gurus.com/threads/9"
#property version     "1.00"
#property description "An example of an OOP random entry system"

//---- include object oriented framework
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\AccountInfo.mqh>
#include <Indicators\Indicators.mqh>

//---- input parameters
input  int     Magic=12345;
input  int     Slippage=30;
input  int     ProfitTarget=1000;
input  int     StopLoss=10000;
input  double  Lots=0.1;
input  bool    ReallyRandom=true;
//+------------------------------------------------------------------+
//| MA crossover example expert class                                |
//+------------------------------------------------------------------+
class CGuruEx01
  {
private:
   int               Dig;
   double            Points;
   bool              Initialized;
   bool              Running;
   ulong             OrderNumber;
   double            GetSize();

protected:
   string            m_Pair;                    // Currency pair to trade 
   CTrade            m_Trade;                   // Trading object
   CSymbolInfo       m_Symbol;                  // Symbol info object
   CPositionInfo     m_Position;                // Position info object
   void              InitSystem();
   bool              CheckEntry();
   bool              CheckExit();

public:
                     CGuruEx01();               // Constructor
                    ~CGuruEx01() { Deinit(); }  // Destructor
   bool              Init(string Pair);
   void              Deinit();
   bool              Validated();
   bool              Execute();
  };

CGuruEx01 GuruEx01;
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CGuruEx01::CGuruEx01()
  {
   Initialized=false;
  }
//+------------------------------------------------------------------+
//| Performs system initialisation                                   |
//+------------------------------------------------------------------+
bool CGuruEx01::Init(string Pair)
  {
   m_Pair=Pair;
   m_Symbol.Name(m_Pair);                // Symbol
   m_Trade.SetExpertMagicNumber(Magic);  // Magic number

   Dig=m_Symbol.Digits();
   Points=m_Symbol.Point();
   m_Trade.SetDeviationInPoints(Slippage);

   Print("Digits = ",Dig,", Points = ",DoubleToString(Points,Dig));

   if(ReallyRandom)
      MathSrand(int(TimeLocal())); // Initialize random number generator
   else
      MathSrand(1);           // Same every time

   Initialized=true;

   return(true);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
CGuruEx01::Deinit()
  {
   Initialized=false;

   Print("DeInitialized OK");
  }
//+------------------------------------------------------------------+
//| Returns trade size based on money management system (if any!)    |
//+------------------------------------------------------------------+
double CGuruEx01::GetSize()
  {
   return(Lots);
  }
//+------------------------------------------------------------------+
//| Checks if everything initialized successfully                    |
//+------------------------------------------------------------------+
bool CGuruEx01::Validated()
  {
   return(Initialized);
  }
//+------------------------------------------------------------------+
//| Performs system reinitialisation                                 |
//+------------------------------------------------------------------+
void CGuruEx01::InitSystem()
  {
   Running=false;
   Initialized=true;
  }
//+------------------------------------------------------------------+
//| Performs system logic. Called on every tick                      |
//+------------------------------------------------------------------+
bool CGuruEx01::Execute()
  {
   if(Running) 
     {                   // Are we in a trade at the moment?
      if(CheckExit() > 0) 
        {        // Yes - Last trade complete?
         Initialized = false;       // Yes - Indicate we need to reinitialise
         InitSystem();              //  and start all over again!
        }
     }
   else 
     {
      if(CheckEntry()>0) 
        {       // Entered a trade?
         Running=true;            // Yes - Indicate that we're in a trade
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//| Checks for entry to a trade                                      |
//+------------------------------------------------------------------+
bool CGuruEx01::CheckEntry()
  {
   int CoinToss;

   m_Symbol.RefreshRates();
   CoinToss=MathRand();
   Print("CoinToss = ",CoinToss,", Bid = ",m_Symbol.Bid(),", Ask = ",m_Symbol.Ask());

   if(CoinToss<16384) 
     {               // Coin came up heads, so GO LONG!
      if(m_Trade.PositionOpen(Symbol(),ORDER_TYPE_BUY,GetSize(),m_Symbol.Ask(),
         m_Symbol.Bid() - (Points * StopLoss),
         m_Symbol.Ask()+(Points*ProfitTarget))) 
        {
         OrderNumber=m_Trade.ResultOrder();
         Print("Entered LONG at ",TimeToString(TimeCurrent(),TIME_DATE|TIME_SECONDS));
         return(true);
        }
      else 
        {
         OrderNumber=0;
        }
     }
   else  
     {                                // Coin came up tails, so GO SHORT!
      if(m_Trade.PositionOpen(Symbol(),ORDER_TYPE_SELL,GetSize(),m_Symbol.Bid(),
         m_Symbol.Ask() + (Points * StopLoss),
         m_Symbol.Bid() -(Points*ProfitTarget))) 
        {
         OrderNumber=m_Trade.ResultOrder();
         Print("Entered SHORT at ",TimeToString(TimeCurrent(),TIME_DATE|TIME_SECONDS));
         return(true);
        }
      else 
        {
         OrderNumber=0;
        }
     }

   return(false);
  }
//+------------------------------------------------------------------+
//| Checks for exit from a trade                                     |
//+------------------------------------------------------------------+
bool CGuruEx01::CheckExit()
  {
   double PositionSize;

   if(m_Position.Select(m_Pair)!=true) 
     {
      Print("Position flat at ",TimeToString(TimeCurrent(),TIME_DATE|TIME_SECONDS));
      return(true);
     }
   else 
     {
      m_Position.InfoDouble(POSITION_VOLUME,PositionSize);
      if(PositionSize<=0) 
        {
         Print("Position closed at ",TimeToString(TimeCurrent(),TIME_DATE|TIME_SECONDS));
         return(true);
        }
      else 
        {
         return(false);
        }
     }
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE)==ACCOUNT_TRADE_MODE_REAL) 
     {
      MessageBox("Wealth Warning! This expert is for educational purposes only."+
                 " It should NEVER be used on a live account."+
                 " Past performance is in no way indicative of future results!");
      Print("Initialization Failure");
      return(-1);
     }

   if(!GuruEx01.Init(Symbol())) 
     {
      GuruEx01.Deinit();
      return(-1);
     }

   Print("Initialized OK");

   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   GuruEx01.Deinit();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(!GuruEx01.Validated()) 
     {
      return;
     }

   GuruEx01.Execute();
  }
//+------------------------------------------------------------------+
