//+------------------------------------------------------------------+
//|                          ADX System(barabashkakvn's edition).mq5 |
//|                                                           System |
//|                                                   work_a@ukr.net |
//+------------------------------------------------------------------+
#property copyright "System"
#property link      "work_a@ukr.net"
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
#include <Trade\AccountInfo.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
CAccountInfo   m_account;                    // account info wrapper
input ushort   InpTakeProfit     = 15;       // TakeProfit
input double   Lots              = 1;        // Lot
input ushort   InpTrailingStop   = 20;       // TrailingStop
input ushort   InpStopLoss       = 100;      // StopLoss
//---
ulong          m_magic           = 16384;
double         ExtTakeProfit     = 0.0;
double         ExtTrailingStop   = 0.0;
double         ExtStopLoss       = 0.0;
int            handle_iADX;                  // variable for storing the handle of the iADX indicator 
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//SetMarginMode();
//if(!IsHedging())
//  {
//   Print("Hedging only!");
//   return(INIT_FAILED);
//  }
//---
   m_symbol.Name(Symbol());                  // sets symbol name
   if(!RefreshRates())
     {
      Print("Error RefreshRates. Bid=",DoubleToString(m_symbol.Bid(),Digits()),
            ", Ask=",DoubleToString(m_symbol.Ask(),Digits()));
      return(INIT_FAILED);
     }
//---
   m_trade.SetExpertMagicNumber(m_magic);    // sets magic number
//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;

   ExtTakeProfit     = InpTakeProfit   * digits_adjust;
   ExtTrailingStop   = InpTrailingStop * digits_adjust;
   ExtStopLoss       = InpStopLoss     * digits_adjust;

//--- create handle of the indicator iADX
   handle_iADX=iADX(Symbol(),Period(),14);
//--- if the handle is not created 
   if(handle_iADX==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iADX indicator for the symbol %s/%s, error code %d",
                  Symbol(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }

   if(Bars(Symbol(),Period())<100)
     {
      Print("bars less than 100");
      return(INIT_FAILED);
     }
   if(ExtTakeProfit<10)
     {
      Print("ExtTakeProfit less than 10");
      return(INIT_FAILED);  // check ExtTakeProfit
     }
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   double ADXP,ADXC,ADXDIPP;
   double ADXDIPC,ADXDIMP,ADXDIMC;

   ADXP = iADXGet(MAIN_LINE, 2);
   ADXC = iADXGet(MAIN_LINE, 1);
   ADXDIPP = iADXGet(PLUSDI_LINE, 2);
   ADXDIPC = iADXGet(PLUSDI_LINE, 1);
   ADXDIMP = iADXGet(MINUSDI_LINE, 2);
   ADXDIMC = iADXGet(MINUSDI_LINE, 1);

   int total=0;
   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of open positions
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==m_magic)
            total++;

   if(total==0)
     {
      //--- no opened positions identified
      if(m_account.FreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ",m_account.FreeMargin());
         return;
        }
      //--- check for long position (BUY) possibility
      if((ADXP<ADXC) && (ADXDIPP<ADXP) && (ADXDIPC>ADXC))
        {
         if(!RefreshRates())
            return;

         if(m_trade.Buy(Lots,m_symbol.Name(),m_symbol.Ask(),m_symbol.Ask()-ExtStopLoss*Point(),
            m_symbol.Ask()+ExtTakeProfit*Point(),"adx sample"))
           {
            Print("BUY order opened : ",m_trade.ResultPrice());
           }
         else
           {
            Print("Error opening BUY. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription(),
                  ", ticket of deal: ",m_trade.ResultDeal());
            return;
           }
        }
      //--- check for short position (SELL) possibility
      if((ADXP<ADXC) && (ADXDIMP<ADXP) && (ADXDIMC>ADXC))
        {
         if(!RefreshRates())
            return;

         if(m_trade.Sell(Lots,m_symbol.Name(),m_symbol.Bid(),m_symbol.Bid()+ExtStopLoss*Point(),
            m_symbol.Bid()-ExtTakeProfit*Point(),"adx sample"))
           {
            Print("SELL order opened : ",m_trade.ResultPrice());
           }
         else
            Print("Error opening Sell. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription(),
                  ", ticket of deal: ",m_trade.ResultDeal());
         return;
        }
      return;
     }

   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of open positions
      if(m_position.SelectByIndex(i))
         if(m_position.Symbol()==Symbol() && m_position.Magic()==m_magic)
           {
            if(m_position.PositionType()==POSITION_TYPE_BUY) // long position is opened
              {
               if(ADXP>ADXC && ADXDIPP>ADXP && ADXDIPC<ADXC)
                 {
                  m_trade.PositionClose(m_position.Ticket()); // close position
                  return; // exit
                 }
               if(ExtTrailingStop>0)
                 {
                  if(!RefreshRates())
                     return;

                  if(m_symbol.Bid()-m_position.PriceOpen()>Point()*ExtTrailingStop)
                    {
                     if(m_position.StopLoss()<m_symbol.Bid()-Point()*ExtTrailingStop)
                       {
                        m_trade.PositionModify(m_position.Ticket(),m_symbol.Bid()-Point()*ExtTrailingStop,
                                               m_position.TakeProfit());
                        return;
                       }
                    }
                 }
              }
            else
              {
               if(ADXP>ADXC && ADXDIMP>ADXP && ADXDIMC<ADXC)
                 {
                  m_trade.PositionClose(m_position.Ticket()); // close position
                  return; // exit
                 }
               if(ExtTrailingStop>0)
                 {
                  if(!RefreshRates())
                     return;

                  if((m_position.PriceOpen()-m_symbol.Ask())>(Point()*ExtTrailingStop))
                    {
                     if((m_position.StopLoss()>(m_symbol.Ask()+Point()*ExtTrailingStop)) || 
                        (m_position.StopLoss()==0))
                       {
                        m_trade.PositionModify(m_position.Ticket(),m_symbol.Ask()+Point()*ExtTrailingStop,
                                               m_position.TakeProfit());
                        return;
                       }
                    }
                 }
              }
           }
   return;
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
//| Get value of buffers for the iADX                                |
//|  the buffer numbers are the following:                           |
//|   0 - iADXBuffer, 1 - DI_plusBuffer, 2 - DI_minusBuffer          |
//+------------------------------------------------------------------+
double iADXGet(const int buffer,const int index)
  {
   double ADX[];
   ArraySetAsSeries(ADX,true);
//--- reset error code 
   ResetLastError();
//--- fill a part of the iADXBuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iADX,buffer,0,index+1,ADX)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iADX indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(ADX[index]);
  }
//+------------------------------------------------------------------+
