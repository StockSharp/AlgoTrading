//+------------------------------------------------------------------+
//|                      cheduecoglioni(barabashkakvn's edition).mq5 |
//|                                                   Gianni Esperti |
//|                                  https://www.gianniesperti.come? |
//+------------------------------------------------------------------+
#property copyright "Gianni Esperti"
#property link      "https://www.gianniesperti.come?"
#property version   "1.007"
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
#include <Trade\SymbolInfo.mqh>  
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
CSymbolInfo    m_symbol;                     // symbol info object
//--- input parameters
input double   InpLots          = 0.1;       // Lots
input ushort   InpTakeProfit    = 10;        // Take Profit (in pips)
input ushort   InpStopLoss      = 10;        // Stop Loss (in pips)
//---
ulong                m_magic=15489;          // magic number
ENUM_POSITION_TYPE   m_close_pos_type=POSITION_TYPE_BUY;
bool                 m_is_trade=true;
double               m_adjusted_point;       // point value adjusted for 3 or 5 points
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
   m_symbol.Name(Symbol());                  // sets symbol name
   RefreshRates();
   m_symbol.Refresh();
//---
   m_trade.SetExpertMagicNumber(m_magic);
//---
   if(IsFillingTypeAllowed(Symbol(),SYMBOL_FILLING_FOK))
      m_trade.SetTypeFilling(ORDER_FILLING_FOK);
   else if(IsFillingTypeAllowed(Symbol(),SYMBOL_FILLING_IOC))
      m_trade.SetTypeFilling(ORDER_FILLING_IOC);
   else
      m_trade.SetTypeFilling(ORDER_FILLING_RETURN);
//--- tuning for 3 or 5 digits
   int digits_adjust=1;
   if(m_symbol.Digits()==3 || m_symbol.Digits()==5)
      digits_adjust=10;
   m_adjusted_point=m_symbol.Point()*digits_adjust;
//---
   m_close_pos_type=POSITION_TYPE_BUY;
   m_is_trade=true;
//---
   return (INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(m_is_trade)
     {
      if(!RefreshRates())
         return;

      if(m_close_pos_type==POSITION_TYPE_SELL)
        {
         Print("Open Buy");
         OpenBuy(m_symbol.Ask()-InpStopLoss*m_adjusted_point,
                 m_symbol.Ask()+InpTakeProfit*m_adjusted_point);
        }
      else if(m_close_pos_type==POSITION_TYPE_BUY)
        {
         Print("Open Sell");
         OpenSell(m_symbol.Bid()+InpStopLoss*m_adjusted_point,
                  m_symbol.Bid()-InpTakeProfit*m_adjusted_point);
        }

     }
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &trans,
                        const MqlTradeRequest &request,
                        const MqlTradeResult &result)
  {
//--- get transaction type as enumeration value 
   ENUM_TRADE_TRANSACTION_TYPE type=trans.type;
//--- if transaction is result of addition of the transaction in history
   if(type==TRADE_TRANSACTION_DEAL_ADD)
     {
      long     deal_entry        =0;
      long     deal_type         =0;
      string   deal_symbol       ="";
      long     deal_magic        =0;
      long     deal_time         =0;
      if(HistoryDealSelect(trans.deal))
        {
         deal_entry=HistoryDealGetInteger(trans.deal,DEAL_ENTRY);
         deal_type=HistoryDealGetInteger(trans.deal,DEAL_TYPE);
         deal_symbol=HistoryDealGetString(trans.deal,DEAL_SYMBOL);
         deal_magic=HistoryDealGetInteger(trans.deal,DEAL_MAGIC);
         deal_time=HistoryDealGetInteger(trans.deal,DEAL_TIME);
        }
      else
         return;
      if(deal_symbol==m_symbol.Name() && deal_magic==m_magic)
        {
         if(deal_entry==DEAL_ENTRY_OUT)
           {
            if(deal_type==DEAL_TYPE_BUY || deal_type==DEAL_TYPE_SELL)
              {
               if(deal_type==DEAL_TYPE_BUY)
                  m_close_pos_type=POSITION_TYPE_SELL;
               else if(deal_type==DEAL_TYPE_SELL)
                  m_close_pos_type=POSITION_TYPE_BUY;
               else
                  return;
               m_is_trade=true;
              }
           }
         else if(deal_entry==DEAL_ENTRY_IN)
           {
            m_is_trade=false;
           }
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
//| Checks if the specified filling mode is allowed                  | 
//+------------------------------------------------------------------+ 
bool IsFillingTypeAllowed(string symbol,int fill_type)
  {
//--- Obtain the value of the property that describes allowed filling modes 
   int filling=(int)SymbolInfoInteger(symbol,SYMBOL_FILLING_MODE);
//--- Return true, if mode fill_type is allowed 
   return((filling & fill_type)==fill_type);
  }
//+------------------------------------------------------------------+
//| Open Buy position                                                |
//+------------------------------------------------------------------+
void OpenBuy(double sl,double tp)
  {
   sl=m_symbol.NormalizePrice(sl);
   tp=m_symbol.NormalizePrice(tp);

//--- check volume before OrderSend to avoid "not enough money" error (CTrade)
   double check_volume_lot=m_trade.CheckVolume(m_symbol.Name(),InpLots,m_symbol.Ask(),ORDER_TYPE_BUY);

   if(check_volume_lot!=0.0)
     {
      if(check_volume_lot>=InpLots)
        {
         if(m_trade.Buy(InpLots,NULL,m_symbol.Ask(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
              }
            else
              {
               Print("Buy -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
              }
           }
         else
           {
            Print("Buy -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
           }
        }
      else
        {
         m_is_trade=false;
        }
     }
   else
     {
      m_is_trade=false;
     }
//---
  }
//+------------------------------------------------------------------+
//| Open Sell position                                               |
//+------------------------------------------------------------------+
void OpenSell(double sl,double tp)
  {
   sl=m_symbol.NormalizePrice(sl);
   tp=m_symbol.NormalizePrice(tp);

//--- check volume before OrderSend to avoid "not enough money" error (CTrade)
   double check_volume_lot=m_trade.CheckVolume(m_symbol.Name(),InpLots,m_symbol.Bid(),ORDER_TYPE_SELL);

   if(check_volume_lot!=0.0)
     {
      if(check_volume_lot>=InpLots)
        {
         if(m_trade.Sell(InpLots,NULL,m_symbol.Bid(),sl,tp))
           {
            if(m_trade.ResultDeal()==0)
              {
               Print("Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
              }
            else
              {
               Print("Sell -> true. Result Retcode: ",m_trade.ResultRetcode(),
                     ", description of result: ",m_trade.ResultRetcodeDescription());
              }
           }
         else
           {
            Print("Sell -> false. Result Retcode: ",m_trade.ResultRetcode(),
                  ", description of result: ",m_trade.ResultRetcodeDescription());
           }
        }
      else
        {
         m_is_trade=false;
        }
     }
   else
     {
      m_is_trade=false;
     }
//---
  }
//+------------------------------------------------------------------+
