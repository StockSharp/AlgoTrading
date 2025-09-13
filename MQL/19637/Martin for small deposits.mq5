//+------------------------------------------------------------------+
//|           Martin for small deposits(barabashkakvn's edition).mq5 |
//+------------------------------------------------------------------+
#property version   "1.001"
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
//--- input parameters
input double   InpLots           = 0.01;     // Volume
input ushort   InpTakeProfit     = 65;       // Take Profit (in pips)
input ushort   InpStep           = 15;       // Step between positions
input uchar    InpBarsSkipped=45;        // Number of bars to be skipped
input double   InpIncreaseFactor = 1.7;      // Volume increase factor
input double   InpMaxLot         = 6.0;      // Max volume
input double   InpMinProfit      = 10.0;     // Min profit for close all
//---
ulong          m_ticket;
ulong          m_magic=585631523;            // magic number
ulong          m_slippage=30;                // slippage

double         ExtTakeProfit=0.0;
double         ExtStep=0.0;

double         m_adjusted_point;             // point value adjusted for 3 or 5 points
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
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

   ExtTakeProfit=InpTakeProfit*m_adjusted_point;
   ExtStep=InpStep*m_adjusted_point;
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
//--- we work only at the time of the birth of new bar
   static datetime PrevBars=0;
   datetime time_0=iTime(m_symbol.Name(),Period(),0);
   if(time_0==PrevBars)
      return;
   PrevBars=time_0;
   if(!RefreshRates())
     {
      PrevBars=iTime(m_symbol.Name(),Period(),1);
      return;
     }

   static int counter_skipped=0;
//--- 
   int      count_buys  =0;                     double price_lowest_buy    =DBL_MAX;
   int      count_sells =0;                     double price_highest_sell  =DBL_MIN;
   datetime last_deal_in=D'1970.12.21 00:00';   double total_profit        =0.0;
   CalculateAllPositions(count_buys,price_lowest_buy,
                         count_sells,price_highest_sell,
                         last_deal_in,total_profit);
   if(last_deal_in!=D'1970.12.21 00:00')
     {
      double array_open[];
      int copied=CopyOpen(m_symbol.Name(),Period(),TimeCurrent(),last_deal_in,array_open);
      if(copied>1)
         counter_skipped=copied-1;
      int d=0;
     }
   else
      counter_skipped=0;
//---
   bool need_to_open_a_buy=false;
   bool need_to_open_a_sell=false;
   if(count_buys!=0 && count_sells!=0) // error!
     {
      CloseAllPositions();
      PrevBars=iTime(m_symbol.Name(),Period(),1);
      return;
     }
   else if(count_buys==0 && count_sells==0)
     {
      double arr_close[];
      ArraySetAsSeries(arr_close,true);
      int copied=CopyClose(m_symbol.Name(),Period(),1,15,arr_close);
      if(copied!=15)
         return;
      if(arr_close[0]<arr_close[14]) // open buy
         need_to_open_a_buy=true;
      else if(arr_close[0]>arr_close[14]) // open sell
      need_to_open_a_sell=true;
     }

   if(counter_skipped<=InpBarsSkipped && (count_buys!=0 || count_sells!=0))
     {
      //--- check profit and return
      if(total_profit>InpMinProfit)
        {
         CloseAllPositions();
         return;
        }
      return;
     }
   if(count_buys==0 || count_sells==0) // check the opening of the position "sell"
     {
      if(count_buys==0 && count_sells>0)
        {
         if(m_symbol.Bid()-price_highest_sell>ExtStep)
            need_to_open_a_sell=true;
        }
      else if(count_sells==0 && count_buys>0)
        {
         if(price_lowest_buy-m_symbol.Ask()>ExtStep)
            need_to_open_a_buy=true;
        }
     }
//--- buy
   if(need_to_open_a_buy)
     {
      double sl=0.0;
      double tp=(InpTakeProfit==0)?0.0:m_symbol.Ask()+ExtTakeProfit;
      double coef=MathPow(InpIncreaseFactor,(double)count_buys);
      double lot=LotCheck(InpLots*coef);
      if(lot!=0.0 && lot<InpMaxLot)
         OpenBuy(sl,tp,lot);
     }
//--- sell
   if(need_to_open_a_sell)
     {
      double sl=0.0;
      double tp=(InpTakeProfit==0)?0.0:m_symbol.Bid()-ExtTakeProfit;
      double coef=MathPow(InpIncreaseFactor,(double)count_sells);
      double lot=LotCheck(InpLots*coef);
      if(lot!=0.0 && lot<InpMaxLot)
         OpenSell(sl,tp,lot);
     }
//---
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &trans,
                        const MqlTradeRequest &request,
                        const MqlTradeResult &result)
  {
//---

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
//| Lot Check                                                        |
//+------------------------------------------------------------------+
double LotCheck(double lots)
  {
//--- calculate maximum volume
   double volume=NormalizeDouble(lots,2);
   double stepvol=m_symbol.LotsStep();
   if(stepvol>0.0)
      volume=stepvol*MathFloor(volume/stepvol);
//---
   double minvol=m_symbol.LotsMin();
   if(volume<minvol)
      volume=0.0;
//---
   double maxvol=m_symbol.LotsMax();
   if(volume>maxvol)
      volume=maxvol;
   return(volume);
  }
//+------------------------------------------------------------------+
//| Calculate all positions                                          |
//+------------------------------------------------------------------+
void CalculateAllPositions(int &count_buys,double &price_lowest_buy,
                           int &count_sells,double &price_highest_sell,
                           datetime  &last_deal_in,double &total_profit)
  {
   count_buys  =0;                     price_lowest_buy  =DBL_MAX;
   count_sells =0;                     price_highest_sell=DBL_MIN;
   last_deal_in=D'1970.12.21 00:00';   total_profit      =0.0;
   for(int i=PositionsTotal()-1;i>=0;i--)
      if(m_position.SelectByIndex(i)) // selects the position by index for further access to its properties
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
           {
            if(m_position.Time()>last_deal_in)
               last_deal_in=m_position.Time();
            total_profit+=m_position.Swap()+m_position.Commission()+m_position.Profit();
            if(m_position.PositionType()==POSITION_TYPE_BUY)
              {
               count_buys++;
               if(m_position.PriceOpen()<price_lowest_buy) // the lowest position of "BUY" is found
                  price_lowest_buy=m_position.PriceOpen();
               continue;
              }
            else if(m_position.PositionType()==POSITION_TYPE_SELL)
              {
               count_sells++;
               if(m_position.PriceOpen()>price_highest_sell) // the highest position of "SELL" is found
                  price_highest_sell=m_position.PriceOpen();
               continue;
              }
           }
  }
//+------------------------------------------------------------------+
//| Close all positions                                              |
//+------------------------------------------------------------------+
void CloseAllPositions()
  {
   for(int i=PositionsTotal()-1;i>=0;i--) // returns the number of current positions
      if(m_position.SelectByIndex(i))     // selects the position by index for further access to its properties
         if(m_position.Symbol()==m_symbol.Name() && m_position.Magic()==m_magic)
            m_trade.PositionClose(m_position.Ticket()); // close a position by the specified symbol
  }
//+------------------------------------------------------------------+
//| Open Buy position                                                |
//+------------------------------------------------------------------+
void OpenBuy(double sl,double tp,double lot)
  {
   sl=m_symbol.NormalizePrice(sl);
   tp=m_symbol.NormalizePrice(tp);
//--- check volume before OrderSend to avoid "not enough money" error (CTrade)
   double check_volume_lot=m_trade.CheckVolume(m_symbol.Name(),lot,m_symbol.Ask(),ORDER_TYPE_BUY);

   if(check_volume_lot!=0.0)
      if(check_volume_lot>=lot)
        {
         if(m_trade.Buy(lot,NULL,m_symbol.Ask(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("#1 Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               PrintResult(m_trade,m_symbol);
              }
            else
              {
               Print("#2 Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               PrintResult(m_trade,m_symbol);
              }
           }
         else
           {
            Print("#3 Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
            PrintResult(m_trade,m_symbol);
           }
        }
//---
  }
//+------------------------------------------------------------------+
//| Open Sell position                                               |
//+------------------------------------------------------------------+
void OpenSell(double sl,double tp,double lot)
  {
   sl=m_symbol.NormalizePrice(sl);
   tp=m_symbol.NormalizePrice(tp);
//--- check volume before OrderSend to avoid "not enough money" error (CTrade)
   double check_volume_lot=m_trade.CheckVolume(m_symbol.Name(),lot,m_symbol.Bid(),ORDER_TYPE_SELL);

   if(check_volume_lot!=0.0)
      if(check_volume_lot>=lot)
        {
         if(m_trade.Sell(lot,NULL,m_symbol.Bid(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("#1 Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               PrintResult(m_trade,m_symbol);
              }
            else
              {
               Print("#2 Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
               PrintResult(m_trade,m_symbol);
              }
           }
         else
           {
            Print("#3 Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
            PrintResult(m_trade,m_symbol);
           }
        }
//---
  }
//+------------------------------------------------------------------+
//| Print CTrade result                                              |
//+------------------------------------------------------------------+
void PrintResult(CTrade &trade,CSymbolInfo &symbol)
  {
   Print("Code of request result: "+IntegerToString(trade.ResultRetcode()));
   Print("code of request result: "+trade.ResultRetcodeDescription());
   Print("deal ticket: "+IntegerToString(trade.ResultDeal()));
   Print("order ticket: "+IntegerToString(trade.ResultOrder()));
   Print("volume of deal or order: "+DoubleToString(trade.ResultVolume(),2));
   Print("price, confirmed by broker: "+DoubleToString(trade.ResultPrice(),symbol.Digits()));
   Print("current bid price: "+DoubleToString(trade.ResultBid(),symbol.Digits()));
   Print("current ask price: "+DoubleToString(trade.ResultAsk(),symbol.Digits()));
   Print("broker comment: "+trade.ResultComment());
   DebugBreak();
  }
//+------------------------------------------------------------------+
