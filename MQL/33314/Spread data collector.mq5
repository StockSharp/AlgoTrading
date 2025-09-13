//+------------------------------------------------------------------+
//|                                        Spread data collector.mq5 |
//|                                     Copyright 2021, alipoormomen |
//|                       https://www.mql5.com/en/users/alipoormomen |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, alipoormomen."
#property link      "https://www.mql5.com/en/users/alipoormomen"
#property version   "1.00"
int spread_Less_than_10=0;
int spread_10_20=0;
int spread_20_30=0;
int spread_30_40=0;
int spread_40_50=0;
int spread_More_than_50=0;

int con_year=0;

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

   datetime date1= TimeCurrent();
   MqlDateTime str1;
   TimeToStruct(date1,str1);
   Print("year==",str1.year,"***********************************************************************************");
   Print("spread_10==",spread_Less_than_10);
   Print("spread_10_20==",spread_10_20);
   Print("spread_20_30==",spread_20_30);
   Print("spread_30_40==",spread_30_40);
   Print("spread_40_50==",spread_40_50);
   Print("spread_50==",spread_More_than_50);



  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- Returns the last known time
   datetime date1= TimeCurrent();
   MqlDateTime str1;
   TimeToStruct(date1,str1);
//---This is a structure for storing the latest prices
   MqlTick last_tick;
   SymbolInfoTick(Symbol(),last_tick);
 
   
   if(con_year != str1.year)
     {
     //--When the year changed print data
      if(con_year!=0)
        {
         Print("year==",con_year,"***********************************************************************************");
         Print("spread_Less_than_10==",spread_Less_than_10);
         Print("spread_10_20==",spread_10_20);
         Print("spread_20_30==",spread_20_30);
         Print("spread_30_40==",spread_30_40);
         Print("spread_40_50==",spread_40_50);
         Print("spread_More_than_50==",spread_More_than_50);
        }
    //--When the year changed initialisation
      con_year=str1.year;
      spread_Less_than_10=0;
      spread_10_20=0;
      spread_20_30=0;
      spread_30_40=0;
      spread_40_50=0;
      spread_More_than_50=0;

     }
//---Spread data collector
   if(last_tick.ask-last_tick.bid<10*_Point)
      spread_Less_than_10++;
   if(last_tick.ask-last_tick.bid<20*_Point && last_tick.ask-last_tick.bid>10*_Point)
      spread_10_20++;
   if(last_tick.ask-last_tick.bid<30*_Point && last_tick.ask-last_tick.bid>20*_Point)
      spread_20_30++;
   if(last_tick.ask-last_tick.bid<40*_Point && last_tick.ask-last_tick.bid>30*_Point)
      spread_30_40++;
   if(last_tick.ask-last_tick.bid<50*_Point && last_tick.ask-last_tick.bid>40*_Point)
      spread_40_50++;
   if(last_tick.ask-last_tick.bid>50*_Point)
      spread_More_than_50++;
  }
//+------------------------------------------------------------------+
