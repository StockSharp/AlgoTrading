//+------------------------------------------------------------------+
//|                                 SampleDetectEconomicCalendar.mq5 |
//|                                                          Tungman |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Tungman"
#property link      "https://www.mql5.com"
#property version   "1.00"

#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\OrderInfo.mqh>
#include <Trade\HistoryOrderInfo.mqh>
#include <Expert\Trailing\TrailingFixedPips.mqh>
#include <Tools\Datetime.mqh>
#include <Trade\SymbolInfo.mqh>

CTrade               eatrade;
CPositionInfo        eaposition;
COrderInfo           eapending;
CDateTime            eadate;
CTrailingFixedPips   eatrail;
CSymbolInfo          easymbol;

input bool                 Tradenews               = false; // true = Trade news time, defalt = false
input double               PriceVolume             = 0.01; // Lot
input double               PriceSL                 = 300; // Stop loss (point) (0 = not use)
input double               PriceTP                 = 900; // Take profit (point) 0 = not use)
input int                  trailingstoppoint       = 200; // Trailing stop point (can view by symbol)
input ENUM_TIMEFRAMES      Expirydate              = PERIOD_D1; // Expiry time for pending order
input bool                 UseMoneyManagement      = false;
input int                  Buydistance             = 400; // BUY - Place at point distance
input int                  Selldistance            = 400; // SELL - Place at point distance
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

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

  }
//+------------------------------------------------------------------+

// Create new struct
struct NewsData
  {
   int               NewsIndex; // News index
   datetime          ReleaseTime; // Release time
   string            NewsName;
  };

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double LotSize()
  {
   double value = 0;
   double Balance       = AccountInfoDouble(ACCOUNT_BALANCE);
   double v1            = (Balance * 0.03) / PriceSL;
   double v2            = NormalizeDouble(v1, 2);
   double v3            = v2 <= 0.01 ? SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN) : NormalizeDouble(v2, 2);
   return value         = v3 > SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX) ? SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX) : v3;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckPendingOrder(string ordertype, string suffix)
  {
   bool res = false;
// Check existiing pending
   for(int i = OrdersTotal(); i >= 0; i --)
     {
      if(eapending.SelectByIndex(i))
        {
         string PenSymbol        = eapending.Symbol();
         string comment          = eapending.Comment();
         if(PenSymbol == _Symbol && comment == ordertype + _Symbol + EnumToString(PERIOD_CURRENT) + suffix)
           {
            res = true;
            break;
           }
        }
     }
   return res;
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume, string &description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f", min_volume);
      return(false);
     }
//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f", max_volume);
      return(false);
     }
//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step, ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);
  }

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots, ENUM_ORDER_TYPE type)
  {
//--- Getting the opening price
   MqlTick mqltick;
   SymbolInfoTick(symb, mqltick);
   double price=mqltick.ask;
   if(type==ORDER_TYPE_SELL)
      price=mqltick.bid;
//--- values of the required and free margin
   double margin, free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
//--- call of the checking function
   if(!OrderCalcMargin(type, symb, lots, price, margin))
     {
      //--- something went wrong, report and return false
      Print("Error in ", __FUNCTION__, " code=", GetLastError());
      return(false);
     }
//--- if there are insufficient funds to perform the operation
   if(margin>free_margin)
     {
      //--- report the error and return false
      Print("Not enough money for ", EnumToString(type), " ", lots, " ", symb, " Error code=", GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void FuncTradeNews()
  {
   if(Tradenews == true)
     {
      string   currencybase      = SymbolInfoString(_Symbol,SYMBOL_CURRENCY_BASE);
      double   CurrentBid        = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      double   CurrentAsk        = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      double   BuyPrice, SellPrice, SL, TP;
      double   OrderVolume       = UseMoneyManagement == true ? LotSize() : NormalizeDouble(PriceVolume, 2);
      datetime curdatetime       = TimeTradeServer(eadate);
      datetime before            = curdatetime - PeriodSeconds(PERIOD_H4);
      datetime end               = curdatetime + PeriodSeconds(PERIOD_D1);
      datetime expdate           = curdatetime + PeriodSeconds(Expirydate);
      int spred                  = (int)SymbolInfoInteger(_Symbol, SYMBOL_SPREAD);
      int stop                   = (int)SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL);
      double stopprice           = NormalizeDouble((stop + spred) * _Point, _Digits);
      bool IsCheckMoneySell      = CheckMoneyForTrade(_Symbol,OrderVolume,ORDER_TYPE_SELL);
      bool IsCheckMoneyBuy       = CheckMoneyForTrade(_Symbol,OrderVolume,ORDER_TYPE_BUY);
      int Backdate               = 1;


      if(!IsCheckMoneySell)
         return;

      if(!IsCheckMoneyBuy)
         return;
         
         
      MqlCalendarValue  value[];
      MqlCalendarEvent  ev1[];

      if(currencybase != "USD")
         return;

      int x  = CalendarValueHistory(value,before,end,"US",currencybase);
      int x2 = CalendarEventByCurrency(currencybase,ev1);
      // Detect news
      bool HaveNews           = x > 0 ? true : false;
      int RedNews             = 0;

      NewsData eanewsdata[1000]; // Stored news data to struct
      int k = 0;
      for(int i = 0; i < x; i++)
        {
         ulong ev2 = value[i].event_id;
         MqlCalendarEvent ev3;
         CalendarEventById(ev2,ev3);
         if(ev3.importance == CALENDAR_IMPORTANCE_HIGH)
           {
            RedNews += 1;
            eanewsdata[k].NewsIndex    = i;
            eanewsdata[k].NewsName     = ev3.name;
            eanewsdata[k].ReleaseTime  = value[i].time;
            k++;
           }
        }


      // Wait news

      if(ArraySize(eanewsdata) > 0)
        {
         for(int i = 0; i < ArraySize(eanewsdata); i++)
           {

            datetime BeforeReleaseTime       = eanewsdata[i].ReleaseTime - PeriodSeconds(PERIOD_M5);

            // put pending order before released time
            if(TimeTradeServer() > BeforeReleaseTime && TimeTradeServer() < eanewsdata[i].ReleaseTime)
              {
               if(!CheckPendingOrder("B", "N")) // BUY NEWS
                 {
                  BuyPrice = CurrentAsk + (Buydistance * _Point);
                  SL       = NormalizeDouble(BuyPrice - (PriceSL * _Point), _Digits);
                  TP       = NormalizeDouble(BuyPrice + (PriceTP * _Point), _Digits);
                  eatrade.BuyStop(OrderVolume, BuyPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "B" + _Symbol + EnumToString(PERIOD_CURRENT) + "N");
                  ObjectCreate(0, "News up", OBJ_EVENT, 0, eanewsdata[i].ReleaseTime, BuyPrice);
                 }

               if(!CheckPendingOrder("S", "N")) // SELL NEWS
                 {
                  SellPrice   = CurrentBid - (Selldistance * _Point);
                  SL          = NormalizeDouble(SellPrice + (PriceSL * _Point), _Digits);
                  TP          = NormalizeDouble(SellPrice - (PriceTP * _Point), _Digits);
                  eatrade.SellStop(OrderVolume, SellPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "S" + _Symbol + EnumToString(PERIOD_CURRENT) + "N");
                  ObjectCreate(0, "News down", OBJ_EVENT, 0, eanewsdata[i].ReleaseTime, SellPrice);
                 }
              }
            // put pending order before released time
           }
        }

     }
  }
//+------------------------------------------------------------------+
