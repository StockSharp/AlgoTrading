//+------------------------------------------------------------------+
//|                           EMA_CROSS(barabashkakvn's edition).mq5 |
//|                                                      Coders Guru |
//|                                         http://www.forex-tsd.com |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| TODO: Add Money Management routine                               |
//+------------------------------------------------------------------+
#property copyright "Coders Guru"
#property link      "http://www.forex-tsd.com"

#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object

//---- input parameters
input bool   Reverse         = true;
input double InpTakeProfit   = 25;
input double InpStopLoss     = 105;
input double Lots            = 0.5;
input double InpTrailingStop = 20;
input int    ShortEma        = 9;
input int    LongEma         = 45;

int    handle_iMAShort;                      // variable for storing the handle of the iMA indicator 
int    handle_iMALong;                       // variable for storing the handle of the iMA indicator 
uchar  digits_adjust=-1;
double ExtTakeProfit=0.0;
double ExtStopLoss=0.0;
double ExtTrailingStop=0.0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//----
   if(Bars(Symbol(),Period())<100)
     {
      Print("bars less than 100");
      return(INIT_FAILED);
     }
//---
   m_symbol.Name(Symbol());
   m_symbol.Refresh();
   RefreshRates();
   m_trade.SetExpertMagicNumber(12345);
//--- tuning for 3 or 5 digits
   digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
     {
      digits_adjust=10;
     }
   ExtTakeProfit=InpTakeProfit*digits_adjust;
//---
   if(ExtTakeProfit<20)
     {
      Print("TakeProfit less than 20 point");
      return(INIT_FAILED);  // check ExtTakeProfit
     }//---
   ExtStopLoss=InpStopLoss*digits_adjust;
   ExtTrailingStop=InpTrailingStop*digits_adjust;
//--- create handle of the indicator iMA
   handle_iMAShort=iMA(Symbol(),Period(),ShortEma,0,MODE_EMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMAShort==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  Symbol(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iMA
   handle_iMALong=iMA(Symbol(),Period(),LongEma,0,MODE_EMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMALong==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
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
//| Calculate Crossed                                                |
//+------------------------------------------------------------------+
int Crossed(double line1,double line2)
  {
   static int last_direction=0;
   static int current_direction=0;
//--- don't work in the first load, wait for the first cross!
   static bool first_time=true;
   if(first_time==true)
     {
      first_time=false;
      //---
      if(line1>line2)
         current_direction=1;  //up
      else
         current_direction=2;  //(line1<line2) //down 

      last_direction=current_direction;
      //---
      return (0);
     }
//---
   if(line1>line2)
      current_direction=1;  //up
   else
      current_direction=2;  //(line1<line2) //down 
//---
   if(current_direction!=last_direction) //changed 
     {
      last_direction=current_direction;
      return(last_direction);
     }
   else
     {
      return(0);  //not changed
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   ulong  m_ticket=0;
   double SEma,LEma;
//---
   SEma = iMAGet(handle_iMAShort,0);
   LEma = iMAGet(handle_iMALong,0);
//---
   static int isCrossed=0;
   if(!Reverse)
      isCrossed=Crossed(LEma,SEma);
   else
      isCrossed=Crossed(SEma,LEma);
//---
   if(!RefreshRates())
      return;
//---
   int pos_total=PositionsTotal();
   if(pos_total<1)
     {
      if(isCrossed==1)
        {
         if(m_trade.Buy(Lots,Symbol(),m_symbol.Ask(),m_symbol.Ask()-ExtStopLoss*Point(),
            m_symbol.Ask()+ExtTakeProfit*Point(),"EMA_CROSS"))
           {
            m_ticket=m_trade.ResultDeal();
            Print("Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription(),
                  ", m_ticket of deal: ",m_trade.ResultDeal());
           }
         else
           {
            Print("Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription(),
                  ", m_ticket of deal: ",m_trade.ResultDeal());
           }
         return;
        }
      if(isCrossed==2)
        {
         if(m_trade.Sell(Lots,Symbol(),m_symbol.Bid(),m_symbol.Bid()+ExtStopLoss*Point(),
            m_symbol.Bid()-ExtTakeProfit*Point(),"EMA_CROSS"))
           {
            m_ticket=m_trade.ResultDeal();
            Print("Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription(),
                  ", m_ticket of deal: ",m_trade.ResultDeal());
           }
         else
           {
            Print("Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription(),
                  ", m_ticket of deal: ",m_trade.ResultDeal());
           }
         return;
        }
      return;
     }
//---
   for(int cnt=pos_total-1;cnt>=0;cnt--)
     {
      if(!m_position.SelectByIndex(cnt))
         return;
      if(m_position.Symbol()==Symbol())
        {
         if(m_position.PositionType()==POSITION_TYPE_BUY) // long position is opened
           {
            //--- check for trailing stop
            if(ExtTrailingStop>0)
              {
               if((m_symbol.Bid()-m_position.PriceOpen())>Point()*ExtTrailingStop)
                 {
                  if(m_position.StopLoss()<(m_symbol.Bid()-Point()*ExtTrailingStop))
                    {
                     m_trade.PositionModify(m_position.Ticket(),
                                            m_symbol.Bid()-Point()*ExtTrailingStop,
                                            m_position.TakeProfit());
                     return;
                    }
                 }
              }
           }
         else //--- go to short position
           {
            //--- check for trailing stop
            if(ExtTrailingStop>0)
              {
               if((m_position.PriceOpen()-m_symbol.Ask())>(Point()*ExtTrailingStop))
                 {
                  if((m_position.StopLoss()>(m_symbol.Ask()+Point()*ExtTrailingStop)) || (m_position.StopLoss()==0))
                    {
                     m_trade.PositionModify(m_position.Ticket(),
                                            m_symbol.Ask()+Point()*ExtTrailingStop,
                                            m_position.TakeProfit());
                     return;
                    }
                 }
              }
           }
        }
     }
//---
   return;
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iMA                                 |
//+------------------------------------------------------------------+
double iMAGet(const int handle,const int index)
  {
   double MA[];
   ArraySetAsSeries(MA,true);
//--- reset error code 
   ResetLastError();
//--- fill a part of the iMABuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle,0,0,index+1,MA)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iMA indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(MA[index]);
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
