//+------------------------------------------------------------------+
//|           Expert MACD EURUSD 1 Hour(barabashkakvn's edition).mq5 | 
//|                                                          Gabito. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Gabito."
#property link      "http://www.metaquotes.net"

#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\AccountInfo.mqh>
#include <Trade\DealInfo.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
CAccountInfo   m_account;                    // account info wrapper
CDealInfo      m_deal;                       // deals object
//--- input parameters
input ulong    MagicNumber = 23478423;       // Magic Number
input ushort   InpTrailing = 25;             // Trailing
input double   Risk=0.01;
//---
double         ExtTrailing=0.0;
double         ExtLot=0;
uchar          Pause=255;                    // pause between modifications
datetime       PrevModification=0;           // time last modifications

int    handle_iMACD;                         // variable for storing the handle of the iMACD indicator 
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   m_symbol.Name(Symbol());                     // sets symbol name
   m_trade.SetExpertMagicNumber(MagicNumber);   // sets magic number
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
   ExtTrailing=InpTrailing *digits_adjust;
   ExtLot=0.0;
//--- create handle of the indicator iMACD
   handle_iMACD=iMACD(Symbol(),Period(),5,15,3,PRICE_CLOSE);
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
//|                                                                  |
//+------------------------------------------------------------------+
void OpenBuyOrSell()
  {
   double mac1,mac2,mac3,mac4,mac5,mac6,mac7,mac8;
   mac1 = iMACDGet(MAIN_LINE,0);
   mac2 = iMACDGet(MAIN_LINE,1);
   mac3 = iMACDGet(MAIN_LINE,2);
   mac4 = iMACDGet(MAIN_LINE,3);
   mac5 = iMACDGet(SIGNAL_LINE,0);
   mac6 = iMACDGet(SIGNAL_LINE,1);
   mac7 = iMACDGet(SIGNAL_LINE,2);
   mac8 = iMACDGet(SIGNAL_LINE,3);

//--- check for long position (BUY) possibility
   if(mac8>mac7 && mac7>mac6 && mac6<mac5 && mac4>mac3 && mac3<mac2 && mac2<mac1 && mac2<-0.00020 && mac4<0 && mac1>0.00020)
     {
      if(!RefreshRates())
         return;
      double volume=LotsOptimized();
      if(volume==0)
         return;
      m_trade.Buy(volume,Symbol(),m_symbol.Bid(),0,0,"Expert MACD");
      return;
     }
//--- check for short position (SELL) possibility
   if(mac8<mac7 && mac7<mac6 && mac6>mac5 && mac4<mac3 && mac3>mac2 && mac2>mac1 && mac2>0.00020 && mac4>0 && mac1<-0.00035)
     {
      if(!RefreshRates())
         return;
      double volume=LotsOptimized();
      if(volume==0)
         return;
      m_trade.Sell(volume,Symbol(),m_symbol.Ask(),0,0,"Expert MACD");
      return;
     }
  }
//-----------------------------------+
void CloseOpBuySell()
  {
   if(!RefreshRates())
      return;
   double mac1,mac2;
   mac1 = iMACDGet(MAIN_LINE,0);
   mac2 = iMACDGet(MAIN_LINE,1);
   if(m_position.PositionType()==POSITION_TYPE_BUY)
     {
      if(mac1<mac2)
        {
         m_trade.PositionClose(m_position.Ticket());
         return;
        }
      if(ExtTrailing>0)
        {
         if(m_symbol.Bid()-m_position.PriceOpen()>Point()*ExtTrailing)
           {
            if(m_position.StopLoss()<m_symbol.Bid()-Point()*ExtTrailing)
              {
               if((long)(TimeCurrent()-PrevModification)>Pause)
                 {
                  m_trade.PositionModify(m_position.Ticket(),m_symbol.Bid()-Point()*ExtTrailing,m_position.TakeProfit());
                  PrevModification=TimeCurrent();
                  return;
                 }
              }
           }
        }
     }
   if(m_position.PositionType()==POSITION_TYPE_SELL)
     {
      if(mac1>mac2)
        {
         m_trade.PositionClose(m_position.Ticket());
         return;
        }
      if(ExtTrailing>0)
        {
         if((m_position.PriceOpen()-m_symbol.Ask())>(Point()*ExtTrailing))
           {
            if((m_position.StopLoss()>(m_symbol.Ask()+Point()*ExtTrailing)) || (m_position.StopLoss()==0))
              {
               if((long)(TimeCurrent()-PrevModification)>Pause)
                 {
                  PrevModification=TimeCurrent();
                  m_trade.PositionModify(m_position.Ticket(),m_symbol.Ask()+Point()*ExtTrailing,m_position.TakeProfit());
                  return;
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
//--- select ExtLot size
   ExtLot=NormalizeDouble(m_account.FreeMargin()*Risk/100,1);
   ExtLot=LotCheck(ExtLot);
//--- return ExtLot size
   return(ExtLot);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   int total_positions=0;

   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==MagicNumber)
            total_positions++;

   if(total_positions==0)
     {
      OpenBuyOrSell();
      return;
     }

   for(int i=PositionsTotal()-1;i>=0;i--)
     {
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==MagicNumber)
            CloseOpBuySell();
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
//| Lot Check                                                        |
//+------------------------------------------------------------------+
double LotCheck(double lots)
  {
//--- calculate maximum volume
   double volume=NormalizeDouble(lots,2);
   double stepvol=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);
   if(stepvol>0.0)
      volume=stepvol*MathFloor(volume/stepvol);
//---
   double minvol=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<minvol)
      volume=0.0;
//---
   double maxvol=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>maxvol)
      volume=maxvol;
   return(volume);
  }
//-----------------------------------+
