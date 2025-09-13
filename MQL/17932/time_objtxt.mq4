//+------------------------------------------------------------------+
//|                                                  time_objtxt.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
int index;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   //index = ArrayMaximum(High,30,0);
   
   //bool ret = ObjectCreate(0,"My Text",OBJ_TEXT,0,D'2015.06.30 11:53:24',High[index]);
   //if(ret==true)
   //Print("Object created");
   //else
   //Print("Object not created");
   for(int i=0;i<=ObjectsTotal();i++)
   {
    int objtype = ObjectType(ObjectName(i));
    if(objtype==OBJ_TEXT)
    {
     datetime d1 = ObjectGetInteger(0,ObjectName(i),OBJPROP_TIME1);
     printf("The time of object %s is %s",ObjectName(i),TimeToString(d1,TIME_DATE|TIME_SECONDS));
    }
   }
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
