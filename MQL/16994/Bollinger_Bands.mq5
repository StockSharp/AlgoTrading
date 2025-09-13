//+------------------------------------------------------------------+
//|                     Bollinger Bands(barabashkakvn's edition).mq5 |
//+------------------------------------------------------------------+
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\AccountInfo.mqh>
#include <Trade\DealInfo.mqh>
#include <Trade\OrderInfo.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
CAccountInfo   m_account;                    // account info wrapper
CDealInfo      m_deal;                       // deals object
COrderInfo     m_order;                      // pending orders object 
#property copyright "xoiper"
#property link      "kin.city@yahoo.com"
//--- Profit/Loss
input ushort InpProfitMade = 3;     // how much money do you expect to make
input ushort InpLossLimit  = 20;    // how much loss can you tolorate
//--- Bollinger Bands
input int    BB_period=4;           // Bollinger period
input int    BB_deviation=2;        // Bollinger deviation
//--- Other
input ushort InpBDistance  = 3;     // plus how much
input double InpLots       = 1.0;   // how many lots to trade at a time 
input bool   LotIncrease=true;  // grow lots based on balance = true
//--- Non-external flag settings
bool   logging=false;              // log data or not
bool   logerrs=false;              // log errors or not
bool   logtick=false;              // log tick data while orders open (or not)
bool   OneOrderOnly=true;          // one order at a time or not
//--- Naming and numbering
ulong  m_magic=200607121116;           // allows multiple experts to trade on same account
string TradeComment="_bolltrade_v01.txt";   // comment so multiple EAs can be seen in Account History
double StartingBalance=1000;                   // lot size control if LotIncrease == true
//--- Bar handling
datetime bartime=0;                      // used to determine when a bar has moved
int      bartick=0;                      // number of times bars have moved
//--- Trade control
bool   TradeAllowed=true;                // used to manage trades
//--- Min/Max tracking and tick logging
int    maxOrders;                        // statistic for maximum numbers or orders open at one time
double maxEquity;                        // statistic for maximum equity level
double minEquity;                        // statistic for minimum equity level
double maxOEquity;                       // statistic for maximum equity level per order
double minOEquity;                       // statistic for minimum equity level per order 
double EquityPos=0;                      // statistic for number of ticks order was positive
double EquityNeg=0;                      // statistic for number of ticks order was negative
double EquityZer=0;                      // statistic for number of ticks order was zero
//---
double ExtProfitMade=0.0;
double ExtLossLimit=0.0;
double ExtBDistance=0.0;
double ExtLots=0.0;
int    handle_iBands;                        // variable for storing the handle of the iBands indicator 
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create handle of the indicator iBands
   handle_iBands=iBands(Symbol(),Period(),BB_period,0,BB_deviation,PRICE_OPEN);
//--- if the handle is not created 
   if(handle_iBands==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iBands indicator for the symbol %s/%s, error code %d",
                  Symbol(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//---
   m_symbol.Name(Symbol());                  // sets symbol name
   m_trade.SetExpertMagicNumber(m_magic);    // sets magic number
   if(!RefreshRates())
     {
      Print("Error RefreshRates. Bid=",DoubleToString(m_symbol.Bid(),Digits()),
            ", Ask=",DoubleToString(m_symbol.Ask(),Digits()));
      return(INIT_FAILED);
     }
//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;

   ExtProfitMade  =InpProfitMade*digits_adjust;
   ExtLossLimit   =InpLossLimit*digits_adjust;
   ExtBDistance   =InpBDistance*digits_adjust;
   ExtLots        =InpLots;

   if(LotIncrease)
     {
      StartingBalance=m_account.Balance()/ExtLots;
      logwrite(TradeComment,"LotIncrease ACTIVE Account balance="+DoubleToString(m_account.Balance(),2)+
               " ExtLots="+DoubleToString(ExtLots,2)+
               " StartingBalance="+DoubleToString(StartingBalance,2));
     }
   else
     {
      logwrite(TradeComment,"LotIncrease NOT ACTIVE Account balance="+DoubleToString(m_account.Balance(),2)+
               " ExtLots="+DoubleToString(ExtLots,2));
     }
   logwrite(TradeComment,"Init Complete");
   Comment(" ");
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
// always indicate deinit statistics
   logwrite(TradeComment,"MAX number of orders "+IntegerToString(maxOrders));
   logwrite(TradeComment,"MAX equity           "+DoubleToString(maxEquity,2));
   logwrite(TradeComment,"MIN equity           "+DoubleToString(minEquity,2));
// so you can see stats in journal
   Print("MAX number of orders "+IntegerToString(maxOrders));
   Print("MAX equity           "+DoubleToString(maxEquity,2));
   Print("MIN equity           "+DoubleToString(minEquity,2));
   logwrite(TradeComment,"DE-Init Complete");
   Comment(" ");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   int      gle=0;
   ulong    ticket=0;
   int      PositionsPerSymbol=0;
   int      PositionsBUY=0;
   int      PositionsSELL=0;
// stoploss and takeprofit and close control
   double SL=0;
   double TP=0;
   double CurrentProfit=0;
   double CurrentBasket=0;
// direction control
   bool BUYme=false;
   bool SELLme=false;
// bar counting
   if(bartime!=iTime(0))
     {
      bartime=iTime(0);
      bartick++;
      TradeAllowed=true;
     }
// Lot increasement based on AccountBalance when expert is started
// this will trade 1.0, then 1.1, then 1.2 etc as account balance grows
// or 0.9 then 0.8 then 0.7 as account balance shrinks 
   if(LotIncrease)
     {
      ExtLots=NormalizeDouble(m_account.Balance()/StartingBalance,1);
      if(ExtLots>500.0) ExtLots=500.0;
     }
   PositionsPerSymbol=0;
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==m_magic)
           {
            PositionsPerSymbol++;
            if(m_position.PositionType()==POSITION_TYPE_BUY)
               PositionsBUY++;
            else
               PositionsSELL++;
           }
//--- keep some statistics
   if(PositionsPerSymbol>maxOrders)
      maxOrders=PositionsPerSymbol;
//+-----------------------------+
//| Insert your indicator here  |
//| And set either BUYme or     |
//| SELLme true to place orders |
//+-----------------------------+
   double bup=iBandsGet(UPPER_BAND,0);
   double bdn=iBandsGet(LOWER_BAND,0);
//
   if(iClose(0)>bup+(ExtBDistance*Point()))
      SELLme=true;
   if(iClose(0)<bdn-(ExtBDistance*Point()))
      BUYme=true;
//+------------+
//| End Insert |
//+------------+
//ENTRY LONG (buy, m_symbol.Ask()) 
   if((OneOrderOnly && PositionsPerSymbol==0 && BUYme) || (!OneOrderOnly && TradeAllowed && BUYme))
     {
      if(!RefreshRates())
         return;
      if(ExtLossLimit==0)
         SL=0;
      else
         SL=m_symbol.Ask()-((ExtLossLimit+10)*Point());
      if(ExtProfitMade==0)
         TP=0;
      else
         TP=m_symbol.Ask()+((ExtProfitMade+10)*Point());

      ticket=0;
      if(m_trade.Buy(ExtLots,Symbol(),m_symbol.Ask(),SL,TP,TradeComment))
         ticket=m_trade.ResultDeal();
      if(ticket>0)
        {
         if(logging)
            logwrite(TradeComment,"BUY Ticket="+IntegerToString(ticket)+
                     " m_symbol.Ask()="+DoubleToString(m_symbol.Ask(),Digits())+
                     " ExtLots="+DoubleToString(ExtLots,2)+
                     " SL="+DoubleToString(SL,Digits())+
                     " TP="+DoubleToString(TP,Digits()));
         maxOEquity=0;
         minOEquity=0;
         EquityPos=0;
         EquityNeg=0;
         EquityZer=0;
         TradeAllowed=false;
         return;
        }
      else
        {
         if(logerrs)
            logwrite(TradeComment,"-----ERROR-----  opening BUY order :"+IntegerToString(m_trade.ResultRetcode())+
                     " "+m_trade.ResultRetcodeDescription());
        }
     }//BUYme
//ENTRY SHORT (sell, m_symbol.Bid())
   if((OneOrderOnly && PositionsPerSymbol==0 && SELLme) || (!OneOrderOnly && TradeAllowed && SELLme))
     {
      if(!RefreshRates())
         return;
      if(ExtLossLimit==0)
         SL=0;
      else
         SL=m_symbol.Bid()+((ExtLossLimit+10)*Point());
      if(ExtProfitMade==0)
         TP=0;
      else
         TP=m_symbol.Bid()-((ExtProfitMade+10)*Point());

      ticket=0;
      if(m_trade.Sell(ExtLots,Symbol(),m_symbol.Bid(),SL,TP,TradeComment))
         ticket=m_trade.ResultDeal();
      if(ticket>0)
        {
         if(logging)
            logwrite(TradeComment,"SELL Ticket="+IntegerToString(ticket)+
                     " m_symbol.Bid()="+DoubleToString(m_symbol.Bid(),Digits())+
                     " ExtLots="+DoubleToString(ExtLots,2)+
                     " SL="+DoubleToString(SL,Digits())+
                     " TP="+DoubleToString(TP,Digits()));
         maxOEquity=0;
         minOEquity=0;
         EquityPos=0;
         EquityNeg=0;
         EquityZer=0;
         TradeAllowed=false;
         return;
        }
      else
        {
         if(logerrs)
            logwrite(TradeComment,"-----ERROR-----  opening SELL order :"+IntegerToString(m_trade.ResultRetcode())+
                     " "+m_trade.ResultRetcodeDescription());
        }
     }//SELLme
//--- accumulate statistics
   CurrentBasket=m_account.Equity()-m_account.Balance();
   if(CurrentBasket>maxEquity)
     {
      maxEquity=CurrentBasket;
      maxOEquity=CurrentBasket;
     }
   if(CurrentBasket<minEquity)
     {
      minEquity=CurrentBasket;
      minOEquity=CurrentBasket;
     }
   if(CurrentBasket>0)
      EquityPos++;
   if(CurrentBasket<0)
      EquityNeg++;
   if(CurrentBasket==0)
      EquityZer++;
//--- Order Management
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY)
              {
               if(!RefreshRates())
                  return;
               CurrentProfit=m_symbol.Bid()-m_position.PriceOpen();
               if(logtick)
                  logwrite(TradeComment,"BUY  CurrentProfit="+DoubleToString(CurrentProfit/Point(),Digits())+
                           " CurrentBasket="+DoubleToString(CurrentBasket/Point(),Digits()));
               //--- Did we make a profit
               if(ExtProfitMade>0 && CurrentProfit>=(ExtProfitMade*Point()))
                 {
                  if(m_trade.PositionClose(m_position.Ticket()))
                    {
                     if(logging)
                       {
                        logwrite(TradeComment,"CLOSE BUY PROFIT Ticket="+IntegerToString(m_position.Ticket())+
                                 " SL="+DoubleToString(SL,Digits())+
                                 " TP="+DoubleToString(TP,Digits()));
                        logwrite(TradeComment,"MAX order equity "+DoubleToString(maxOEquity,2));
                        logwrite(TradeComment,"MIN order equity "+DoubleToString(minOEquity,2));
                        logwrite(TradeComment,"order equity positive ticks ="+DoubleToString(EquityPos,2));
                        logwrite(TradeComment,"order equity negative ticks ="+DoubleToString(EquityNeg,2));
                        logwrite(TradeComment,"order equity   zero   ticks ="+DoubleToString(EquityZer,2));
                        break;
                       }
                    }
                  else
                    {
                     if(logerrs)
                        logwrite(TradeComment,"-----ERROR----- CLOSE BUY PROFIT m_symbol.Bid()="+DoubleToString(m_symbol.Bid(),Digits())+
                                 ", "+IntegerToString(m_trade.ResultRetcode())+" "+m_trade.ResultRetcodeDescription());
                    }
                 }//if
               //--- Did we take a loss
               if(ExtLossLimit>0 && CurrentProfit<=(ExtLossLimit*(-1)*Point()))
                 {
                  if(m_trade.PositionClose(m_position.Ticket()))
                    {
                     if(logging)
                       {
                        logwrite(TradeComment,"CLOSE BUY LOSS Ticket="+IntegerToString(m_position.Ticket())+
                                 " SL="+DoubleToString(SL,Digits())+
                                 " TP="+DoubleToString(TP,Digits()));
                        logwrite(TradeComment,"MAX order equity "+DoubleToString(maxOEquity,2));
                        logwrite(TradeComment,"MIN order equity "+DoubleToString(minOEquity,2));
                        logwrite(TradeComment,"order equity positive ticks ="+DoubleToString(EquityPos,2));
                        logwrite(TradeComment,"order equity negative ticks ="+DoubleToString(EquityNeg,2));
                        logwrite(TradeComment,"order equity   zero   ticks ="+DoubleToString(EquityZer,2));
                        break;
                       }
                    }
                  else
                    {
                     if(logerrs)
                        logwrite(TradeComment,"-----ERROR----- CLOSE BUY LOSS m_symbol.Bid()="+DoubleToString(m_symbol.Bid(),Digits())+
                                 ", "+IntegerToString(m_trade.ResultRetcode())+" "+m_trade.ResultRetcodeDescription());
                    }
                 }//if
              } // if BUY
            if(m_position.PositionType()==POSITION_TYPE_SELL)
              {
               if(!RefreshRates())
                  return;
               CurrentProfit=m_position.PriceOpen()-m_symbol.Ask();
               if(logtick)
                  logwrite(TradeComment,"SELL CurrentProfit="+DoubleToString(CurrentProfit/Point(),Digits())+
                           " CurrentBasket="+DoubleToString(CurrentBasket/Point(),Digits()));
               //--- Did we make a profit
               if(ExtProfitMade>0 && CurrentProfit>=(ExtProfitMade*Point()))
                 {
                  if(m_trade.PositionClose(m_position.Ticket()))
                    {
                     if(logging)
                       {
                        logwrite(TradeComment,"CLOSE SELL PROFIT Ticket="+IntegerToString(m_position.Ticket())+
                                 " SL="+DoubleToString(SL,Digits())+
                                 " TP="+DoubleToString(TP,Digits()));
                        logwrite(TradeComment,"MAX order equity "+DoubleToString(maxOEquity,2));
                        logwrite(TradeComment,"MIN order equity "+DoubleToString(minOEquity,2));
                        logwrite(TradeComment,"order equity positive ticks ="+DoubleToString(EquityPos,2));
                        logwrite(TradeComment,"order equity negative ticks ="+DoubleToString(EquityNeg,2));
                        logwrite(TradeComment,"order equity   zero   ticks ="+DoubleToString(EquityZer,2));
                        break;
                       }
                    }
                  else
                    {
                     if(logerrs)
                        logwrite(TradeComment,"-----ERROR----- CLOSE SELL PROFIT m_symbol.Ask()="+DoubleToString(m_symbol.Ask(),Digits())+
                                 ", "+IntegerToString(m_trade.ResultRetcode())+" "+m_trade.ResultRetcodeDescription());
                    }
                 }//if
               //--- Did we take a loss
               if(ExtLossLimit>0 && CurrentProfit<=(ExtLossLimit*(-1)*Point()))
                 {
                  if(m_trade.PositionClose(m_position.Ticket()))
                    {
                     if(logging)
                       {
                        logwrite(TradeComment,"CLOSE SELL LOSS Ticket="+IntegerToString(m_position.Ticket())+
                                 " SL="+DoubleToString(SL,Digits())+
                                 " TP="+DoubleToString(TP,Digits()));
                        logwrite(TradeComment,"MAX order equity "+DoubleToString(maxOEquity,2));
                        logwrite(TradeComment,"MIN order equity "+DoubleToString(minOEquity,2));
                        logwrite(TradeComment,"order equity positive ticks ="+DoubleToString(EquityPos,2));
                        logwrite(TradeComment,"order equity negative ticks ="+DoubleToString(EquityNeg,2));
                        logwrite(TradeComment,"order equity   zero   ticks ="+DoubleToString(EquityZer,2));
                        break;
                       }
                    }
                  else
                    {
                     if(logerrs)
                        logwrite(TradeComment,"-----ERROR----- CLOSE SELL LOSS m_symbol.Ask()="+DoubleToString(m_symbol.Ask(),Digits())+
                                 ", "+IntegerToString(m_trade.ResultRetcode())+" "+m_trade.ResultRetcodeDescription());
                    }
                 }//if
              } //if SELL
           } // if(OrderSymbol)
  } // start()
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void logwrite(string filename,string mydata)
  {
   int myhandle;
   myhandle=FileOpen(Symbol()+"_"+filename,FILE_CSV|FILE_WRITE|FILE_READ,";");
   if(myhandle!=INVALID_HANDLE)
     {
      FileSeek(myhandle,0,SEEK_END);
      FileWrite(myhandle,mydata+" "+TimeToString(TimeCurrent()));
      FileClose(myhandle);
     }
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iBands                              |
//|  the buffer numbers are the following:                           |
//|   0 - BASE_LINE, 1 - UPPER_BAND, 2 - LOWER_BAND                  |
//+------------------------------------------------------------------+
double iBandsGet(const int buffer,const int index)
  {
   double Bands[];
   ArraySetAsSeries(Bands,true);
//--- reset error code 
   ResetLastError();
//--- fill a part of the iStochasticBuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iBands,buffer,0,index+1,Bands)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iBands indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(Bands[index]);
  }
//+------------------------------------------------------------------+ 
//| Get Close for specified bar index                                | 
//+------------------------------------------------------------------+ 
double iClose(const int index,string symbol=NULL,ENUM_TIMEFRAMES timeframe=PERIOD_CURRENT)
  {
   if(symbol==NULL)
      symbol=Symbol();
   if(timeframe==0)
      timeframe=Period();
   double Close[];
   double close=0;
   ArraySetAsSeries(Close,true);
   int copied=CopyClose(symbol,timeframe,index,1,Close);
   if(copied>0) close=Close[0];
   return(close);
  }
//+------------------------------------------------------------------+ 
//| Get Time for specified bar index                                 | 
//+------------------------------------------------------------------+ 
datetime iTime(const int index,string symbol=NULL,ENUM_TIMEFRAMES timeframe=PERIOD_CURRENT)
  {
   if(symbol==NULL)
      symbol=Symbol();
   if(timeframe==0)
      timeframe=Period();
   datetime Time[];
   datetime time=0;
   ArraySetAsSeries(Time,true);
   int copied=CopyTime(symbol,timeframe,index,1,Time);
   if(copied>0) time=Time[0];
   return(time);
  }
//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool RefreshRates()
  {
//--- refresh rates
   if(!m_symbol.RefreshRates())
      return(false);
//--- protection against the return value of "zero"
   if(m_symbol.Ask()==0 || m_symbol.Bid()==0)
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
