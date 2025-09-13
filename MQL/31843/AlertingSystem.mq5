//+------------------------------------------------------------------+
//|                                               AlertingSystem.mq5 |
//|                                    Copyright 2020, Forex Jarvis. |
//|                                            forexjarvis@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, Forex Jarvis. forexjarvis@gmail.com"
#property link      "https://www.mql5.com"
#property version   "1.00"

input double   HigherPrice;
input double   LowerPrice;

string slineid1 = "lineid1";
string slineid2 = "lineid2";

int counter = 0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {

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
   if (counter==0) {
   
      ObjectCreate(NULL, slineid1,OBJ_HLINE,0,0,HigherPrice);  //Create Hline of the vlineid1 with price HigherPrice
      ObjectCreate(NULL, slineid2,OBJ_HLINE,0,0,LowerPrice);   //Create Hline of the vlineid1 with price LowerPrice
      
      ObjectSetInteger(NULL,slineid1,OBJPROP_COLOR,C'38,166,154');   //color the vline of the vlineid
      ObjectSetInteger(NULL,slineid2,OBJPROP_COLOR,C'239,83,80');   //color the vline of the vlineid
      
      ObjectSetInteger(NULL, slineid1,OBJPROP_SELECTABLE,true);
      ObjectSetInteger(NULL, slineid2,OBJPROP_SELECTABLE,true);
      
      counter++;
   }
   
   double val1 = ObjectGetDouble(NULL,slineid1,OBJPROP_PRICE);
   double val2 = ObjectGetDouble(NULL,slineid2,OBJPROP_PRICE);
   
   ObjectSetDouble(NULL,slineid1,OBJPROP_PRICE,val1);
   ObjectSetDouble(NULL,slineid2,OBJPROP_PRICE,val2);

	if (SymbolInfoDouble(_Symbol,SYMBOL_BID) >= ObjectGetDouble(NULL,slineid1,OBJPROP_PRICE) && ObjectGetDouble(NULL,slineid1,OBJPROP_PRICE)>0.0) {
		PlaySound("alert.wav");
	}

	if (SymbolInfoDouble(_Symbol,SYMBOL_ASK) <= ObjectGetDouble(NULL,slineid2,OBJPROP_PRICE) && ObjectGetDouble(NULL,slineid2,OBJPROP_PRICE)>0.0) {
		PlaySound("alert.wav");
	}
	
   
  }
