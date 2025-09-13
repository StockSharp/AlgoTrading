//+------------------------------------------------------------------+
//|                                           PERSONAL_ASSISTANT.mq4 |
//|                                                    INFINITE LOOP |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "INFINITE LOOP"
#property link      ""
#property version   "1.00"
#property strict
#property description "Personal assistant is there to provide you with crucial information for making investment decisions and to execute your orders!"
//+------------------------------------------------------------------+
//| User input variables                                             |
//+------------------------------------------------------------------+
input int ID=3900;
input bool Display_legend=true;
input double LotSize=0.01;
input int slippage=2;

input int text_size=10;
input color text_color=clrBlack;
input int right_edge_shift = 350;
input int upper_edge_shift = 15;
//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
string EA_name="PERSONAL_ASSISTANT";
string global_Volume="VOLUME";
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   GlobalVariableSet(global_Volume,LotSize);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   for(int z=1; z<=20; z++)
      ObjectDelete(ChartID(),EA_name+"_"+(string)z);

   GlobalVariableDel(global_Volume);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   int x=0,
   y=0,
   open_position_counter=0,
   sl_counter = 0,
   tp_counter = 0;

   double EA_profit=0,
   EA_takeProfit=0,
   EA_StopLoss=0,
   EA_volume=0,
   EA_sl_volume = 0,
   EA_tp_volume = 0;

   string text="";
//********************************************************************************************************************************
// Control over opened positions
   for(int pos=0; pos<OrdersTotal(); pos++)
     {
      // select every order by position
      if(OrderSelect(pos,SELECT_BY_POS,MODE_TRADES))
        {
         // check if order was set by this EA 
         if(OrderMagicNumber()==ID)
           {
            open_position_counter++;

            EA_profit=EA_profit+OrderProfit();

            if(OrderTakeProfit()!=0)
              {
               EA_takeProfit= EA_takeProfit+MathAbs(OrderTakeProfit()-OrderOpenPrice())/_Point;
               EA_tp_volume = EA_tp_volume+OrderLots();
               tp_counter++;
              }
            if(OrderStopLoss()!=0)
              {
               EA_StopLoss=EA_StopLoss+MathAbs(OrderStopLoss()-OrderOpenPrice())/_Point;
               EA_sl_volume=EA_sl_volume+OrderLots();
               sl_counter++;
              }
            EA_volume=EA_volume+OrderLots();
           }
        }
     }
//********************************************************************************************************************************
// Data display
   x = (int)(ChartGetInteger(ChartID(),CHART_WIDTH_IN_PIXELS,0) - right_edge_shift);
   y = upper_edge_shift;

   text="EA id = "+EA_name+"  "+(string)ID;
   createObject(1,OBJ_LABEL,0,x,y,text);

   text="Type & Period = "+_Symbol+" "+IntegerToString(_Period);
   createObject(2,OBJ_LABEL,0,x,y+upper_edge_shift,text);

   text="Leverage = "+IntegerToString(AccountLeverage());
   createObject(3,OBJ_LABEL,0,x,y+upper_edge_shift*2,text);

   text="Lot amount = "+DoubleToString(GlobalVariableGet(global_Volume),2);
   createObject(4,OBJ_LABEL,0,x,y+upper_edge_shift*3,text);

   text="Tick value = "+DoubleToString(MarketInfo(_Symbol,MODE_TICKVALUE)*GlobalVariableGet(global_Volume),3)+" "+AccountInfoString(ACCOUNT_CURRENCY);
   createObject(5,OBJ_LABEL,0,x,y+upper_edge_shift*4,text);

   text="Margin required = "+DoubleToString(MarketInfo(_Symbol,MODE_MARGINREQUIRED)*GlobalVariableGet(global_Volume),3);
   createObject(6,OBJ_LABEL,0,x,y+upper_edge_shift*5,text);

   text="Spread = "+DoubleToString(MarketInfo(_Symbol,MODE_SPREAD),2);
   createObject(7,OBJ_LABEL,0,x,y+upper_edge_shift*6,text);

   text="***************************************************";
   createObject(8,OBJ_LABEL,0,x,y+upper_edge_shift*7,text);

   text="Profit/Loss (sum) = "+DoubleToString(EA_profit,2);
   createObject(9,OBJ_LABEL,0,x,y+upper_edge_shift*8,text);

   text="Positions opened by EA = "+IntegerToString(open_position_counter);
   createObject(10,OBJ_LABEL,0,x,y+upper_edge_shift*9,text);

   if(tp_counter==0)
      text="TakeProfit (sum) = 0.00";
   else
   text="TakeProfit (sum) = "+DoubleToString(EA_takeProfit *(MarketInfo(_Symbol,MODE_TICKVALUE) *(EA_tp_volume/tp_counter)),2)+" set for "+
        IntegerToString(tp_counter)+"/"+IntegerToString(open_position_counter)+" orders.";

   createObject(11,OBJ_LABEL,0,x,y+upper_edge_shift*10,text);

   if(sl_counter==0)
      text="StopLoss (sum) = 0.00";
   else
     {
      text="StopLoss (sum) = "+DoubleToString(EA_StopLoss *(MarketInfo(_Symbol,MODE_TICKVALUE) *(EA_sl_volume/sl_counter)),2)+" set for "+
           IntegerToString(sl_counter)+"/"+IntegerToString(open_position_counter)+" orders.";
     }
   createObject(12,OBJ_LABEL,0,x,y+upper_edge_shift*11,text);

   if(Display_legend)
     {
      // LEGEND
      text="ACTION LEGEND:";
      createObject(13,OBJ_LABEL,0,x,y+upper_edge_shift*15,text);

      text="* Press 1 to open long position!";
      createObject(14,OBJ_LABEL,0,x,y+upper_edge_shift*16,text);

      text="* Press 2 to open short position!";
      createObject(15,OBJ_LABEL,0,x,y+upper_edge_shift*17,text);

      text="* Press 3 to close positions!";
      createObject(16,OBJ_LABEL,0,x,y+upper_edge_shift*18,text);

      text="* Press 4 to increase volume!";
      createObject(17,OBJ_LABEL,0,x,y+upper_edge_shift*19,text);

      text="* Press 5 to decrease volume!";
      createObject(18,OBJ_LABEL,0,x,y+upper_edge_shift*20,text);
     }
  }
//+------------------------------------------------------------------+
//| Custom create object function                                    |
//+------------------------------------------------------------------+
void createObject(int st_ID,ENUM_OBJECT obj,int window,int x,int y,string txt="")
  {
   ObjectCreate(EA_name+"_"+IntegerToString(st_ID),obj,window,0,0);
   ObjectSet(EA_name+"_"+IntegerToString(st_ID),OBJPROP_XDISTANCE,x);
   ObjectSet(EA_name+"_"+IntegerToString(st_ID),OBJPROP_YDISTANCE,y);
   ObjectSetText(EA_name+"_"+IntegerToString(st_ID),txt,text_size,"Arial",text_color);
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   int x=0,
   y=0;

   string text="";

   x = (int)(ChartGetInteger(ChartID(),CHART_WIDTH_IN_PIXELS,0) - right_edge_shift);
   y = upper_edge_shift;
/*
         button 1 -> open BUY position manually
         button 2 -> open SELL position manually
         button 3 -> CLOSE BUY or SELL position manually   
         button 4 -> increase current Lot volume (Lots in increments of 0.01)
         button 5 -> decrease current Lot volume (Lots in increments of 0.01) 
   */
   if(id==CHARTEVENT_KEYDOWN)
     {
      // *********************************************************************************************************************
      // button 1
      if(lparam==49 || lparam==97)
        {
         if(OrderSend(_Symbol,OP_BUY,GlobalVariableGet(global_Volume),MarketInfo(_Symbol,MODE_ASK),slippage,0,0,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
            Print("Order BUY successfully opened for ",EA_name,"_",ID);
         else
            Print("Order BUY unsuccessfully opened for ",EA_name,"_",ID);
        }
      // *********************************************************************************************************************
      // button 2 
      if(lparam==50 || lparam==98)
        {

         if(OrderSend(_Symbol,OP_SELL,GlobalVariableGet(global_Volume),MarketInfo(_Symbol,MODE_BID),slippage,0,0,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
            Print("Order SELL successfully opened for ",EA_name,"_",ID);
         else
            Print("Order SELL unsuccessfully opened for ",EA_name,"_",ID);
        }
      // *********************************************************************************************************************
      // button 3 
      if(lparam==51 || lparam==99)
        {
         for(int pos=0; pos<OrdersTotal(); pos++)
            if(OrderSelect(pos,SELECT_BY_POS,MODE_TRADES))
               if(OrderMagicNumber()==ID)
                 {
                  if(OrderType()==OP_BUY)
                     if(OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),slippage,clrNONE))
                        Print(OrderTicket()," closed successfully for ",EA_name,"_",ID);
                  else
                     Print(OrderTicket()," closed unsuccessfully for ",EA_name,"_",ID);

                  if(OrderType()==OP_SELL)
                     if(OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),slippage,clrNONE))
                        Print(OrderTicket()," closed successfully for ",EA_name,"_",ID);
                  else
                     Print(OrderTicket()," closed unsuccessfully for ",EA_name,"_",ID);
                 }
        }
      // *********************************************************************************************************************
      // button 4 
      if(lparam==52 || lparam==100)
        {
         if(GlobalVariableGet(global_Volume)>=8.00)
            Alert("Coution, extreme volume!");
         else
           {
            GlobalVariableSet(global_Volume,GlobalVariableGet(global_Volume)+0.01);

            text="Lot amount = "+DoubleToString(GlobalVariableGet(global_Volume),2);
            createObject(4,OBJ_LABEL,0,x,y+upper_edge_shift*3,text);

            text="Tick value = "+DoubleToString(MarketInfo(_Symbol,MODE_TICKVALUE)*GlobalVariableGet(global_Volume),3)+" "+AccountInfoString(ACCOUNT_CURRENCY);
            createObject(5,OBJ_LABEL,0,x,y+upper_edge_shift*4,text);

            text="Margin required = "+DoubleToString(MarketInfo(_Symbol,MODE_MARGINREQUIRED)*GlobalVariableGet(global_Volume),3);
            createObject(6,OBJ_LABEL,0,x,y+upper_edge_shift*5,text);
           }
        }
      // *********************************************************************************************************************
      // button 5 
      if(lparam==53 || lparam==101)
        {

         if(GlobalVariableGet(global_Volume)<=0.01)
            Alert("Volume is at minimum, it can not be decreased!");
         else
           {
            GlobalVariableSet(global_Volume,GlobalVariableGet(global_Volume)-0.01);

            text="Lot amount = "+DoubleToString(GlobalVariableGet(global_Volume),2);
            createObject(4,OBJ_LABEL,0,x,y+upper_edge_shift*3,text);

            text="Tick value = "+DoubleToString(MarketInfo(_Symbol,MODE_TICKVALUE)*GlobalVariableGet(global_Volume),3)+" "+AccountInfoString(ACCOUNT_CURRENCY);
            createObject(5,OBJ_LABEL,0,x,y+upper_edge_shift*4,text);

            text="Margin required = "+DoubleToString(MarketInfo(_Symbol,MODE_MARGINREQUIRED)*GlobalVariableGet(global_Volume),3);
            createObject(6,OBJ_LABEL,0,x,y+upper_edge_shift*5,text);
           }
        }
     }
  }
//+------------------------------------------------------------------+
