//+------------------------------------------------------------------+
//|                           DoubleUp2(barabashkakvn's edition).mq5 |
//|                                                  The # one Lotfy |
//|                                             hmmlotfy@hotmail.com |
//+------------------------------------------------------------------+
#property copyright "The # one Lotfy"
#property link      "hmmlotfy@hotmail.com"

#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\AccountInfo.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
CAccountInfo   m_account;                    // account info wrapper
//--- input parameters
input int      Inp_ma_period_CCI          =  8;    // averaging period iCCI
input int      Inp_fast_ema_period_MACD   =  13;   // period for Fast average calculation MACD
input int      Inp_slow_ema_period_MACD   =  33;   // period for Slow average calculation MACD
//--- parameters
double         m_cci                      =  0.0;  //
double         m_macd                     =  0.0;  //
int            m_buy_total                =  0;    //
int            m_sell_total               =  0;    //
double         m_ext_lot                  =  0.1;  //
int            m_pos                      =  0;    //
double         m_buy_sell_level           =  230;  // Buy Sell Level
//---
int            handle_iCCI;                        // variable for storing the handle of the iCCI indicator 
int            handle_iMACD;                       // variable for storing the handle of the iMACD indicator 
ulong          m_magic                    =  343;  // magic number
//---
#define MAIN_LINE 0
#define SIGNAL_LINE 1
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   m_symbol.Name(Symbol());                  // sets symbol name
   m_trade.SetExpertMagicNumber(m_magic);    // sets magic number
   RefreshRates();

//--- create handle of the indicator iCCI
   handle_iCCI=iCCI(Symbol(),Period(),Inp_ma_period_CCI,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iCCI==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iCCI indicator for the symbol %s/%s, error code %d",
                  Symbol(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }

//--- create handle of the indicator iMACD
   handle_iMACD=iMACD(Symbol(),Period(),Inp_fast_ema_period_MACD,Inp_slow_ema_period_MACD,2,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMACD==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMACD indicator for the symbol %s/%s, error code %d",
                  Symbol(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//----
   m_ext_lot=NormalizeDouble(m_account.Balance()/50001.0,2);
   if(m_ext_lot<0.1)
      m_ext_lot=0.1;

   m_cci=iCCIGet(0);
   m_macd=iMACDGet(MAIN_LINE,0)*1000000;

   m_buy_total=0;
   m_sell_total=0;

   for(int cnt=PositionsTotal()-1;cnt>=0;cnt--)// ???close all orders
     {
      if(!m_position.SelectByIndex(cnt))
         continue;
      if(m_position.Symbol()!=Symbol())
         continue;
      if(m_position.Magic()!=m_magic)
         continue;
      if(m_position.PositionType()==POSITION_TYPE_BUY)
         m_buy_total++;
      else
         m_sell_total++;
     }
   if(m_cci>m_buy_sell_level && m_macd>m_buy_sell_level)
     {
      sell();
     }
   else if(m_cci<-m_buy_sell_level && m_macd<-m_buy_sell_level)
     {
      buy();
     }
   close();
//---
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void buy()
  {
   closeAll(POSITION_TYPE_SELL);
   if(!RefreshRates())
      return;
   if(m_buy_total==0)
      m_trade.Buy(m_ext_lot*MathPow(2,m_pos),Symbol(),
                  m_symbol.Ask(),0.0,0.0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void sell()
  {
   closeAll(POSITION_TYPE_BUY);
   if(!RefreshRates())
      return;
   if(m_sell_total==0)
      m_trade.Sell(m_ext_lot*MathPow(2,m_pos),Symbol(),
                   m_symbol.Bid(),0.0,0.0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void closeAll(ENUM_POSITION_TYPE pos_type)
  {
   for(int cnt=PositionsTotal()-1;cnt>=0;cnt--)// close all orders
     {
      if(!m_position.SelectByIndex(cnt))
         continue;
      if(m_position.Symbol()!=Symbol())
         continue;
      if(m_position.Magic()!=m_magic)
         continue;
      if(m_position.PositionType()==pos_type)
        {
         if(m_position.Profit()<0)
            m_pos++;
         else if(m_position.Profit()>0)
            m_pos=0;
         m_trade.PositionClose(m_position.Ticket());
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void close()
  {
   for(int cnt=OrdersTotal()-1;cnt>=0;cnt--)// close all orders
     {
      if(!m_position.SelectByIndex(cnt))
         continue;
      if(m_position.Symbol()!=Symbol())
         continue;
      if(m_position.Magic()!=m_magic)
         continue;
      if(m_position.Profit()>0 && MathAbs(m_position.PriceOpen()-m_position.PriceCurrent())>120*Point()) ///
        {
         m_pos+=2;
         Print("Profit=",DoubleToString(m_position.Profit(),2),
               " and PriceOpen(",DoubleToString(m_position.PriceOpen(),Digits()),")",
               " - PriceCurrent(",DoubleToString(m_position.PriceCurrent(),Digits()),")",
               " > ",DoubleToString(120*Point(),Digits()));
         m_trade.PositionClose(m_position.Ticket());
        }
     }
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
//| Get value of buffers for the iCCI                                |
//+------------------------------------------------------------------+
double iCCIGet(const int index)
  {
   double CCI[];
   ArraySetAsSeries(CCI,true);
//--- reset error code 
   ResetLastError();
//--- fill a part of the iCCIBuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iCCI,0,0,index+1,CCI)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iCCI indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(CCI[index]);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iMACD                               |
//|  the buffer numbers are the following:                           |
//|   0 - MAIN_LINE, 1 - SIGNAL_LINE                                 |
//+------------------------------------------------------------------+
double iMACDGet(const int buffer,const int index)
  {
   double MACD[];
   ArraySetAsSeries(MACD,true);
//--- reset error code 
   ResetLastError();
//--- fill a part of the iMACDBuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iMACD,buffer,0,index+1,MACD)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iMACD indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(MACD[index]);
  }
//+------------------------------------------------------------------+
