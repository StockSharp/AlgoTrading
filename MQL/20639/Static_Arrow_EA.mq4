//+------------------------------------------------------------------+
//|                                              Static_Arrow_EA.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

string objname = "";
datetime time;
double price;
int x;



//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
//---
      
   if((Period()!=PERIOD_W1) || (Symbol()!="EURUSD"))
   {
      ChartSetSymbolPeriod(0,"EURUSD",PERIOD_W1);
   }
   ChartSetInteger(0,CHART_AUTOSCROLL,false);  
   ChartSetInteger(0,CHART_SHIFT,false);
   ChartSetInteger(0,CHART_SCALE,0); 
   ChartNavigate(0,CHART_END,0);
  
   
   int XX = 1300;
   int YY = 300;

   for(int i=0;i<40;i++)
   {  
      
      ChartXYToTimePrice(0,XX,YY,x,time,price);
      objname = StringConcatenate("Arrow ",i);
        
      ObjectCreate(0,objname,OBJ_ARROW,0,0,0);
      ObjectSet(objname,OBJPROP_TIME1,time);
      ObjectSet(objname,OBJPROP_PRICE1,price);
      ObjectSetInteger(0,objname,OBJPROP_COLOR,clrBlue);
      ObjectSetInteger(0,objname,OBJPROP_STYLE,STYLE_SOLID);
      ObjectSetInteger(0,objname,OBJPROP_WIDTH,5);
      
      XX = XX-33;
          
   }   
      
   EventSetMillisecondTimer(10);
   
//---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   EventKillTimer();
   ObjectsDeleteAll();   
}
  
  
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
//---
   
}
//+------------------------------------------------------------------+
void OnTimer()
{  

   ChartSetInteger(0,CHART_AUTOSCROLL,false);  
   ChartSetInteger(0,CHART_SHIFT,false);
   ChartSetInteger(0,CHART_SCALE,0);
   ChartNavigate(0,CHART_END,0);
  
   for(int i=0;i<40;i++)
   {
      objname = StringConcatenate("Arrow ",i);
      time = (datetime)ObjectGet(objname,OBJPROP_TIME1);
      price = ObjectGet(objname,OBJPROP_PRICE1);
      ObjectSet(objname,OBJPROP_TIME1,time);
      ObjectSet(objname,OBJPROP_PRICE1,price);   
   }
     
} 


