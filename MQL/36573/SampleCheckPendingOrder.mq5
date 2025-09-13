//+------------------------------------------------------------------+
//|                                      SampleCheckPendingOrder.mq5 |
//|                                                          Tungman |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Tungman"
#property link      "https://www.mql5.com"
#property version   "1.00"

#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\OrderInfo.mqh>

CTrade               eatrade;
CPositionInfo        eaposition;
COrderInfo           eapending;

input double               PriceVolume             = 0.01;
input double               PriceSL                 = 300;
input double               PriceTP                 = 900;

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
   double   CurrentBid        = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   double   CurrentAsk        = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   MqlDateTime eadate;
   datetime curdatetime       = TimeCurrent(eadate);
   datetime expdate           = curdatetime + PeriodSeconds(PERIOD_D1);
   double   BuyPrice, SellPrice, SL, TP;
   bool CheckSellPending      = CheckPendingOrder("S");
   bool CheckBuyPending       = CheckPendingOrder("B");
   string d                   = "";
   double Volume              = PriceVolume;
   bool IsCheckVolume         = CheckVolumeValue(Volume, d);
   bool IsCheckMoneySell      = CheckMoneyForTrade(_Symbol,Volume,ORDER_TYPE_SELL);
   bool IsCheckMoneyBuy       = CheckMoneyForTrade(_Symbol,Volume,ORDER_TYPE_BUY);

   if(!IsCheckMoneySell)
      return;

   if(!IsCheckMoneyBuy)
      return;

   if(!IsCheckMoneySell)
      return;

   if(!IsCheckMoneyBuy)
      return;

   if(!CheckBuyPending && IsCheckVolume)
     {
      BuyPrice = NormalizeDouble(CurrentAsk, _Digits);
      SL       = NormalizeDouble(BuyPrice - (PriceSL * _Point), _Digits);
      TP       = NormalizeDouble(BuyPrice + (PriceTP * _Point), _Digits);
      eatrade.BuyStop(Volume, BuyPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "B" + _Symbol);
     }

   if(!CheckSellPending && IsCheckVolume)
     {
      SellPrice   = NormalizeDouble(CurrentBid, _Digits);
      SL          = NormalizeDouble(SellPrice + (PriceSL * _Point), _Digits);
      TP          = NormalizeDouble(SellPrice - (PriceTP * _Point), _Digits);
      eatrade.SellStop(Volume, SellPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "S" + _Symbol);
     }


  }
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|Check exists pending order
// ordertype S = SELL, B = BUY
//+------------------------------------------------------------------+
bool CheckPendingOrder(string ordertype)
  {
   bool res = false;
// Check existiing pending
   for(int i = OrdersTotal(); i >= 0; i --)
     {
      if(eapending.SelectByIndex(i))
        {
         string PenSymbol        = eapending.Symbol();
         string comment          = eapending.Comment();
         if(PenSymbol == _Symbol && comment == ordertype + _Symbol)
           {
            res = true;
            break;
           }
        }
     }
   return res;
  }
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
