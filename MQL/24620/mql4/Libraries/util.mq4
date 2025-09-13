//+------------------------------------------------------------------+
//|                                                         util.mq4 |
//|                        Copyright 2018, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property library
#property copyright "Copyright 2018, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool NewCandle(datetime lastbar)
  {
   datetime curbar=Time[0];
   if(lastbar!=curbar)
     {
      lastbar=curbar;
      return (true);
     }
   else
     {
      return(false);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void NotifySmartPhone(string Bot)
  {
   double commission=OrderCommission();
   double swap=OrderSwap();
   double profit=OrderProfit()+swap+commission;
   string longOrShort="LONG";
   int orderType= OrderType();
   if(orderType == OP_SELL|| orderType == OP_SELLLIMIT|| orderType == OP_SELLSTOP) longOrShort = "SHORT";
   string notificationString=Bot+" bot:  "+Symbol()+": "+longOrShort+" Order Closed. Profit: "+DoubleToStr(profit,Digits);
   if(IsTesting())
     {
      Print(notificationString);
        } else {
      SendNotification(notificationString);// Send notification to phone
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalculateLotSize(double risk_pct,double pips_to_sl)
  {
   double maxRisk=AccountBalance()*risk_pct/100;
   double dollarValuePerPip=NormalizeDouble(MarketInfo(Symbol(),MODE_TICKVALUE),2)*10;
   double lotSize=maxRisk/(pips_to_sl*dollarValuePerPip);
   return lotSize;
  }
//+------------------------------------------------------------------+

bool CheckVolumeValue(double volume,string &description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step,ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);
  }
//+------------------------------------------------------------------+
