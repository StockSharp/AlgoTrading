
//+------------------------------------------------------------------+
//| Include standard library                                         |
//+------------------------------------------------------------------+
#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\OrderInfo.mqh>
#include <Trade\HistoryOrderInfo.mqh>
#include <Expert\Trailing\TrailingFixedPips.mqh>
#include <Tools\Datetime.mqh>
#include <Trade\SymbolInfo.mqh>


//+------------------------------------------------------------------+
//| Variable standard library                                        |
//+------------------------------------------------------------------+
CTrade               eatrade;
CPositionInfo        eaposition;
COrderInfo           eapending;
CDateTime            eadate;
CTrailingFixedPips   eatrail;
CSymbolInfo          easymbol;

//+------------------------------------------------------------------+
//|Input parameter                                                   |
//+------------------------------------------------------------------+
input group                "News and time parameters"
input string               Starttrade              = "00:00"; // Start time format HH:mm
input string               Endtrade                = "20:00"; // End time format HH:mm
input bool                 Closetradeatendtime     = true; // Clsoe all profit at end time and end week

//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   // Close all profit at end of week
   if(Closetradeatendtime)
     {
      CloseProfitAtTime();
     }
  }
  
//+------------------------------------------------------------------+
//| Close profit at end of week                                      |
//+------------------------------------------------------------------+
void CloseProfitAtTime()
  {
   string Cat        = SymbolInfoString(_Symbol,SYMBOL_CATEGORY);

   if(Cat == "Crypto")
      return;

   MqlDateTime dt;
   datetime    curdate     = TimeCurrent(dt);

   datetime dtcheck        = StringToTime(TimeToString(TimeTradeServer(),TIME_DATE) + " " + Endtrade);

   if(dt.day_of_week == 5 && curdate >= dtcheck)
     {
      for(int i = 0; i < PositionsTotal(); i++)
        {
         if(eaposition.SelectByIndex(i))
           {
            string PosSymbol        = eaposition.Symbol();
            double Profit           = eaposition.Profit();
            ulong ticket            = eaposition.Ticket();
            double price            = eaposition.PriceOpen();
            if(PosSymbol == _Symbol && Profit > 0)
              {
               if(!eatrade.PositionClose(ticket))
               {
                  Print(__FUNCTION__ + " cannot close profit " + _Symbol + " price : " + (string)price + " profit : " + (string)Profit);
               }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+