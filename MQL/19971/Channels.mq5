//+------------------------------------------------------------------+
//|                            Channels(barabashkakvn's edition).mq5 |
//|                        Copyright 2018, MetaQuotes Software Corp. |
//|                                           http://wmua.ru/slesar/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, MetaQuotes Software Corp."
#property link      "http://wmua.ru/slesar/"
#property version   "1.000"
//---
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
//+------------------------------------------------------------------+
//| Enum hours                                                       |
//+------------------------------------------------------------------+
enum ENUM_HOURS
  {
   hour_00  =0,   // 00
   hour_01  =1,   // 01
   hour_02  =2,   // 02
   hour_03  =3,   // 03
   hour_04  =4,   // 04
   hour_05  =5,   // 05
   hour_06  =6,   // 06
   hour_07  =7,   // 07
   hour_08  =8,   // 08
   hour_09  =9,   // 09
   hour_10  =10,  // 10
   hour_11  =11,  // 11
   hour_12  =12,  // 12
   hour_13  =13,  // 13
   hour_14  =14,  // 14
   hour_15  =15,  // 15
   hour_16  =16,  // 16
   hour_17  =17,  // 17
   hour_18  =18,  // 18
   hour_19  =19,  // 19
   hour_20  =20,  // 20
   hour_21  =21,  // 21
   hour_22  =22,  // 22
   hour_23  =23,  // 23
  };
//--- input parameters
input double   InpLots=0.1;      // Lots
input ushort   InpStopLossBuy       = 0;     // Stop Loss BUY (in pips)
input ushort   InpStopLossSell      = 0;     // Stop Loss SELL (in pips)
input ushort   InpTakeProfitBuy     = 0;     // Take Profit BUY (in pips)
input ushort   InpTakeProfitSell    = 0;     // Take Profit SELL (in pips)
input ushort   InpTrailingStopBuy   = 30;    // Trailing Stop BUY (in pips)
input ushort   InpTrailingStopSell  = 30;    // Trailing Stop SELL (in pips)
ushort         InpTrailingStep      = 1;     // Trailing Step (in pips)
input bool     InpUseHours          = false; // Use trade hours
input ENUM_HOURS InpFrom=hour_00;            // From hour
input ENUM_HOURS InpTo=hour_23;              // To hour
input ulong    m_magic=43915489;             // magic number
//---
ulong          m_slippage=10;                // slippage

double ExtStopLossBuy=0.0;
double ExtStopLossSell=0.0;
double ExtTakeProfitBuy=0.0;
double ExtTakeProfitSell=0.0;
double ExtTrailingStopBuy=0.0;
double ExtTrailingStopSell=0.0;
double ExtTrailingStep=0.0;

int    handle_iMA_H1_002_close;              // variable for storing the handle of the iMA indicator 
int    handle_iMA_H1_002_open;               // variable for storing the handle of the iMA indicator 
int    handle_iMA_H1_220_close;              // variable for storing the handle of the iMA indicator 
int    handle_iEnvelopes_H1_220_0_3;         // variable for storing the handle of the iEnvelopes indicator 
int    handle_iEnvelopes_H1_220_0_7;         // variable for storing the handle of the iEnvelopes indicator 
int    handle_iEnvelopes_H1_220_1_0;         // variable for storing the handle of the iEnvelopes indicator 

double m_adjusted_point;                     // point value adjusted for 3 or 5 points
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(!m_symbol.Name(Symbol())) // sets symbol name
      return(INIT_FAILED);
   RefreshRates();

   string err_text="";
   if(!CheckVolumeValue(InpLots,err_text))
     {
      Print(err_text);
      return(INIT_PARAMETERS_INCORRECT);
     }
//---
   m_trade.SetExpertMagicNumber(m_magic);
//---
   if(IsFillingTypeAllowed(SYMBOL_FILLING_FOK))
      m_trade.SetTypeFilling(ORDER_FILLING_FOK);
   else if(IsFillingTypeAllowed(SYMBOL_FILLING_IOC))
      m_trade.SetTypeFilling(ORDER_FILLING_IOC);
   else
      m_trade.SetTypeFilling(ORDER_FILLING_RETURN);
//---
   m_trade.SetDeviationInPoints(m_slippage);
//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;
   m_adjusted_point=m_symbol.Point()*digits_adjust;

   ExtStopLossBuy       = InpStopLossBuy*m_adjusted_point;
   ExtStopLossSell      = InpStopLossSell*m_adjusted_point;
   ExtTakeProfitBuy     = InpTakeProfitBuy*m_adjusted_point;
   ExtTakeProfitSell    = InpTakeProfitSell*m_adjusted_point;
   ExtTrailingStopBuy   = InpTrailingStopBuy*m_adjusted_point;
   ExtTrailingStopSell  = InpTrailingStopSell*m_adjusted_point;
   ExtTrailingStep      = InpTrailingStep*m_adjusted_point;
//--- create handle of the indicator iMA
   handle_iMA_H1_002_close=iMA(m_symbol.Name(),PERIOD_H1,2,0,MODE_EMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMA_H1_002_close==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iMA
   handle_iMA_H1_002_open=iMA(m_symbol.Name(),PERIOD_H1,2,0,MODE_EMA,PRICE_OPEN);
//--- if the handle is not created 
   if(handle_iMA_H1_002_open==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iMA
   handle_iMA_H1_220_close=iMA(m_symbol.Name(),PERIOD_H1,220,0,MODE_EMA,PRICE_CLOSE);
//--- if the handle is not created 
   if(handle_iMA_H1_220_close==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iMA indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(Period()),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iEnvelopes
   handle_iEnvelopes_H1_220_0_3=iEnvelopes(m_symbol.Name(),PERIOD_H1,220,0,MODE_EMA,PRICE_CLOSE,0.3);
//--- if the handle is not created 
   if(handle_iEnvelopes_H1_220_0_3==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iEnvelopes indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(PERIOD_D1),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iEnvelopes
   handle_iEnvelopes_H1_220_0_7=iEnvelopes(m_symbol.Name(),PERIOD_H1,220,0,MODE_EMA,PRICE_CLOSE,0.7);
//--- if the handle is not created 
   if(handle_iEnvelopes_H1_220_0_7==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iEnvelopes indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(PERIOD_D1),
                  GetLastError());
      //--- the indicator is stopped early 
      return(INIT_FAILED);
     }
//--- create handle of the indicator iEnvelopes
   handle_iEnvelopes_H1_220_1_0=iEnvelopes(m_symbol.Name(),PERIOD_H1,220,0,MODE_EMA,PRICE_CLOSE,1.0);
//--- if the handle is not created 
   if(handle_iEnvelopes_H1_220_1_0==INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code 
      PrintFormat("Failed to create handle of the iEnvelopes indicator for the symbol %s/%s, error code %d",
                  m_symbol.Name(),
                  EnumToString(PERIOD_D1),
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
//---
   MqlDateTime str1;
   TimeToStruct(TimeCurrent(),str1);

   if(InpUseHours)
     {
      if(!(str1.hour>=InpFrom && str1.hour<=InpTo))
        {
         Comment("Time for trade has not come else!");
         return;
        }
     }
   if(Bars(m_symbol.Name(),Period())<100)
     {
      Comment("bars less than 100");
      return;
     }
   else
      Comment("");

   double MA_H1_002_close_p0=iMAGet(handle_iMA_H1_002_close,0);
   double MA_H1_002_close_p1=iMAGet(handle_iMA_H1_002_close,1);
   double MA_H1_002_open_p0=iMAGet(handle_iMA_H1_002_open,0);
   double MA_H1_002_open_p1=iMAGet(handle_iMA_H1_002_open,1);
   double MA_H1_220_close_p0=iMAGet(handle_iMA_H1_220_close,0);

   double Env_H1_220_0_3_upper_p0=iEnvelopesGet(handle_iEnvelopes_H1_220_0_3,UPPER_LINE,0);
   double Env_H1_220_0_3_lower_p0=iEnvelopesGet(handle_iEnvelopes_H1_220_0_3,LOWER_LINE,0);
   double Env_H1_220_0_7_upper_p0=iEnvelopesGet(handle_iEnvelopes_H1_220_0_7,UPPER_LINE,0);
   double Env_H1_220_0_7_lower_p0=iEnvelopesGet(handle_iEnvelopes_H1_220_0_7,LOWER_LINE,0);
   double Env_H1_220_1_0_upper_p0=iEnvelopesGet(handle_iEnvelopes_H1_220_1_0,UPPER_LINE,0);
   double Env_H1_220_1_0_lower_p0=iEnvelopesGet(handle_iEnvelopes_H1_220_1_0,LOWER_LINE,0);

   bool lFlagBuyOpen=false;
   bool lFlagSellOpen=false;
//---
   lFlagBuyOpen=((MA_H1_002_close_p0>Env_H1_220_1_0_lower_p0 && MA_H1_002_close_p1<Env_H1_220_1_0_lower_p0) || 
                 (MA_H1_002_close_p0>Env_H1_220_0_7_lower_p0 && MA_H1_002_close_p1<Env_H1_220_0_7_lower_p0) || (MA_H1_002_close_p0<Env_H1_220_0_3_lower_p0
                 && MA_H1_002_close_p1<Env_H1_220_0_3_lower_p0) || (MA_H1_002_close_p0>MA_H1_220_close_p0 && MA_H1_002_close_p1<MA_H1_220_close_p0) || 
                 (MA_H1_002_close_p0>Env_H1_220_0_3_upper_p0 && MA_H1_002_close_p1<Env_H1_220_0_3_upper_p0) || 
                 (MA_H1_002_close_p0>Env_H1_220_0_7_upper_p0 && MA_H1_002_close_p1<Env_H1_220_0_7_upper_p0));
   lFlagSellOpen=((MA_H1_002_open_p0<Env_H1_220_1_0_upper_p0 && MA_H1_002_open_p1>Env_H1_220_1_0_upper_p0)
                  || (MA_H1_002_open_p0<Env_H1_220_0_7_upper_p0 && MA_H1_002_open_p1>Env_H1_220_0_7_upper_p0) || 
                  (MA_H1_002_open_p0<Env_H1_220_0_3_upper_p0 && MA_H1_002_open_p1>Env_H1_220_0_3_upper_p0) || (MA_H1_002_open_p0<MA_H1_220_close_p0 && 
                  MA_H1_002_open_p1>MA_H1_220_close_p0) || (MA_H1_002_open_p0<Env_H1_220_0_3_lower_p0 && MA_H1_002_open_p1>Env_H1_220_0_3_lower_p0) || 
                  (MA_H1_002_open_p0<Env_H1_220_0_7_lower_p0 && MA_H1_002_open_p1>Env_H1_220_0_7_lower_p0));
//---
   if(InpTrailingStopBuy==0 && InpTrailingStopSell==0)
      return;
   int total=0;
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            total++;
            if(m_position.PositionType()==POSITION_TYPE_BUY)
              {
               if(InpTrailingStopBuy!=0)
                 {
                  if(m_position.PriceCurrent()-m_position.PriceOpen()>ExtTrailingStopBuy+ExtTrailingStep)
                     if(m_position.StopLoss()<m_position.PriceCurrent()-(ExtTrailingStopBuy+ExtTrailingStep))
                       {
                        if(!m_trade.PositionModify(m_position.Ticket(),
                           m_symbol.NormalizePrice(m_position.PriceCurrent()-ExtTrailingStopBuy),
                           m_position.TakeProfit()))
                           Print("Modify ",m_position.Ticket(),
                                 " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                                 ", description of result: ",m_trade.ResultRetcodeDescription());
                        continue;
                       }
                 }
              }

            if(m_position.PositionType()==POSITION_TYPE_SELL)
              {
               if(InpTrailingStopSell!=0)
                 {
                  if(m_position.PriceOpen()-m_position.PriceCurrent()>ExtTrailingStopSell+ExtTrailingStep)
                     if((m_position.StopLoss()>(m_position.PriceCurrent()+(ExtTrailingStopSell+ExtTrailingStep))) || 
                        (m_position.StopLoss()==0))
                       {
                        if(!m_trade.PositionModify(m_position.Ticket(),
                           m_symbol.NormalizePrice(m_position.PriceCurrent()+ExtTrailingStopSell),
                           m_position.TakeProfit()))
                           Print("Modify ",m_position.Ticket(),
                                 " Position -> false. Result Retcode: ",m_trade.ResultRetcode(),
                                 ", description of result: ",m_trade.ResultRetcodeDescription());
                       }
                 }
              }
           }
//---
   if(total==0)
     {
      if(lFlagBuyOpen)
        {
         if(!RefreshRates())
            return;
         double sl=0.0;
         double tp=0.0;
         if(InpStopLossBuy!=0)
            sl=m_symbol.NormalizePrice(m_symbol.Ask()-ExtStopLossBuy);
         if(InpTakeProfitBuy!=0)
            tp=m_symbol.NormalizePrice(m_symbol.Ask()+ExtTakeProfitBuy);
         if(m_trade.Buy(InpLots,m_symbol.Name(),m_symbol.Ask(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("#1 Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
            else
              {
               Print("#2 Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
           }
         else
           {
            Print("#3 Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
            //PrintResult(m_trade,m_symbol);
           }
         return;
        }
      if(lFlagSellOpen)
        {
         if(!RefreshRates())
            return;
         double sl=0.0;
         double tp=0.0;
         if(InpStopLossSell!=0)
            sl=m_symbol.NormalizePrice(m_symbol.Bid()+ExtStopLossSell);
         if(InpTakeProfitSell!=0)
            tp=m_symbol.NormalizePrice(m_symbol.Bid()-ExtTakeProfitSell);
         if(m_trade.Sell(InpLots,m_symbol.Name(),m_symbol.Bid(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("#1 Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
            else
              {
               Print("#2 Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               //PrintResult(m_trade,m_symbol);
              }
           }
         else
           {
            Print("#3 Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
            //PrintResult(m_trade,m_symbol);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Refreshes the symbol quotes data                                 |
//+------------------------------------------------------------------+
bool RefreshRates(void)
  {
//--- refresh rates
   if(!m_symbol.RefreshRates())
     {
      Print("RefreshRates error");
      return(false);
     }
//--- protection against the return value of "zero"
   if(m_symbol.Ask()==0 || m_symbol.Bid()==0)
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume,string &error_description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=m_symbol.LotsMin();
   if(volume<min_volume)
     {
      error_description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }
//--- maximal allowed volume of trade operations
   double max_volume=m_symbol.LotsMax();
   if(volume>max_volume)
     {
      error_description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }
//--- get minimal step of volume changing
   double volume_step=m_symbol.LotsStep();
   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      error_description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                                     volume_step,ratio*volume_step);
      return(false);
     }
   error_description="Correct volume value";
   return(true);
  }
//+------------------------------------------------------------------+ 
//| Checks if the specified filling mode is allowed                  | 
//+------------------------------------------------------------------+ 
bool IsFillingTypeAllowed(int fill_type)
  {
//--- Obtain the value of the property that describes allowed filling modes 
   int filling=m_symbol.TradeFillFlags();
//--- Return true, if mode fill_type is allowed 
   return((filling & fill_type)==fill_type);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iMA                                 |
//+------------------------------------------------------------------+
double iMAGet(int handle_iMA,const int index)
  {
   double MA[1];
//--- reset error code 
   ResetLastError();
//--- fill a part of the iMABuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iMA,0,index,1,MA)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iMA indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(MA[0]);
  }
//+------------------------------------------------------------------+
//| Get value of buffers for the iEnvelopes                          |
//|  the buffer numbers are the following:                           |
//|   0 - UPPER_LINE, 1 - LOWER_LINE                                 |
//+------------------------------------------------------------------+
double iEnvelopesGet(int handle_iEnvelopes,const int buffer,const int index)
  {
   double Envelopes[1];
//ArraySetAsSeries(Bands,true);
//--- reset error code 
   ResetLastError();
//--- fill a part of the iEnvelopesBuffer array with values from the indicator buffer that has 0 index 
   if(CopyBuffer(handle_iEnvelopes,buffer,index,1,Envelopes)<0)
     {
      //--- if the copying fails, tell the error code 
      PrintFormat("Failed to copy data from the iBands indicator, error code %d",GetLastError());
      //--- quit with zero result - it means that the indicator is considered as not calculated 
      return(0.0);
     }
   return(Envelopes[0]);
  }
//+------------------------------------------------------------------+
