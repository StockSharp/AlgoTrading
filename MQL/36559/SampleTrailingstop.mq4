//+------------------------------------------------------------------+
//|                                           SampleTrailingstop.mq4 |
//|                                                          Tungman |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Tungman"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   EventSetTimer(60);

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   TrailingStop();
  }

//+------------------------------------------------------------------+
//| Trailing stop with open position                                 |
//+------------------------------------------------------------------+
void TrailingStop()
  {
   for(int i = OrdersTotal(); i >= 0; i --)
     {
      if(OrderSelect(i,SELECT_BY_POS))
        {
         double Priceopen              = NormalizeDouble(OrderOpenPrice(),_Digits);

         int freeze_level              = (int)SymbolInfoInteger(_Symbol,SYMBOL_TRADE_FREEZE_LEVEL);
         int stop_level                = (int)SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL);
         int spred                     = (int)SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);
         double trailingstoppoint      = 200;
         double PriceTP                = 1000;
         double trailpoint             = NormalizeDouble((trailingstoppoint + stop_level + spred + freeze_level) * _Point,_Digits);
         double sl,tp;
         double CurrentSL              = NormalizeDouble(OrderStopLoss(),_Digits);
         double CurrentTP              = NormalizeDouble(OrderTakeProfit(),_Digits);
         double NewSL                  = NormalizeDouble(((stop_level + spred) * _Point),_Digits);
         double NewTP                  = NormalizeDouble(((stop_level + spred) * _Point),_Digits);
         string PosSymbol              = _Symbol;
         double Posprofit              = OrderProfit();
         int   PosType                 = OrderType();
         double ValidSell             = NormalizeDouble(Ask + NewSL,_Digits);
         double ValidBuy               = NormalizeDouble(Bid - NewSL,_Digits);
         datetime expdate              = OrderExpiration();
         int ticket                    = OrderTicket();


         bool check                    = false;
         if(PosSymbol == _Symbol)
           {
            if(PosType == OP_BUY)
              {
               bool checkfreeze  = (Ask - Priceopen) > (freeze_level * _Point);
               if(CurrentSL < Priceopen || CurrentSL == 0)
                 {
                  if(Posprofit > 0 &&  Bid >= (Priceopen + trailpoint) && checkfreeze)
                    {
                     sl = NormalizeDouble(ValidBuy,_Digits); // sl must less than bid price
                     tp = NormalizeDouble(Bid + (PriceTP * _Point),_Digits);
                     check = OrderModify(OrderTicket(),Priceopen,sl,tp,0);
                    }
                 }
               else
                  if(CurrentSL > Priceopen)
                    {
                     if(Posprofit > 0 && CurrentSL <= ValidBuy && checkfreeze)
                       {
                        sl = NormalizeDouble(Bid - trailpoint,_Digits); // sl must less than bid price
                        tp = NormalizeDouble(Bid + (PriceTP * _Point),_Digits);
                        check = OrderModify(OrderTicket(),Priceopen,sl,tp,0);

                       }
                    }
              }

            if(PosType == OP_SELL)
              {

               bool checkfreeze = (Priceopen - Bid) > (freeze_level *_Point);
               if(CurrentSL > Priceopen || CurrentSL == 0)
                 {
                  if(Posprofit > 0 && Ask <= (Priceopen - trailpoint) && checkfreeze)
                    {
                     sl = NormalizeDouble(ValidSell,_Digits);
                     tp = NormalizeDouble(Ask - (PriceTP * _Point),_Digits);
                     check = OrderModify(OrderTicket(),Priceopen,sl,tp,0);
                    }
                 }
               else
                  if(CurrentSL < Priceopen)
                    {
                     if(Posprofit > 0 && CurrentSL >= ValidSell && checkfreeze)
                       {
                        sl = NormalizeDouble(Ask + trailpoint,_Digits);
                        tp = NormalizeDouble(Ask - (PriceTP * _Point),_Digits);
                        check = OrderModify(OrderTicket(),Priceopen,sl,tp,0);
                       }
                    }
              }
           }

        }
     }
  }
//+------------------------------------------------------------------+
