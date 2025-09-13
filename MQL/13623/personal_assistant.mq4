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
input bool Allow_pen_orders=true; // Allows placement of pending orders
input bool Set_SL = false; // Allows input of StopLoss for active orders before execution
input bool Set_TP = false; // Allows input of TakeProfit for active orders before execution
input bool Display_legend=false; // Allows display of action commands

input double LotSize=0.01;
input int slippage=2;

input int text_size=10;
input color text_color=clrBlack;
input int right_edge_shift = 380;
input int upper_edge_shift = 15;
//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
string EA_name="PERSONAL_ASSISTANT";
string global_Volume = "VOLUME_" + IntegerToString(ID);
string global_Record = "RECORD_" + IntegerToString(ID);
string global_Price="PRICE_"+IntegerToString(ID);
string global_StopLoss="SL_"+IntegerToString(ID);
string global_TakeProfit="TP_"+IntegerToString(ID);
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(AccountInfoDouble(ACCOUNT_BALANCE)<5)
      return(INIT_FAILED);

   GlobalVariableSet(global_Volume,LotSize);
   GlobalVariableSet(global_Record,-1.0);

   GlobalVariableSet(global_Price,0.0);
   GlobalVariableSet(global_StopLoss,0.0);
   GlobalVariableSet(global_TakeProfit,0.0);

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   for(int z=1; z<=24; z++)
      ObjectDelete(ChartID(),EA_name+"_"+(string)z);

   GlobalVariableDel(global_Volume);
   GlobalVariableDel(global_Record);

   GlobalVariableDel(global_Price);
   GlobalVariableDel(global_StopLoss);
   GlobalVariableDel(global_TakeProfit);
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
   EA_volume=0;

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

            EA_profit=EA_profit+(OrderProfit()+OrderCommission()+OrderSwap());

            if(OrderTakeProfit()!=0)
              {
               EA_takeProfit=EA_takeProfit+NormalizeDouble(MathRound((MathAbs(OrderTakeProfit()-OrderOpenPrice())/_Point))*MarketInfo(_Symbol,MODE_TICKVALUE)*OrderLots(),3);
               tp_counter++;
              }

            if(OrderStopLoss()!=0)
              {
               EA_StopLoss=EA_StopLoss+NormalizeDouble(MathRound((MathAbs(OrderStopLoss()-OrderOpenPrice())/_Point))*MarketInfo(_Symbol,MODE_TICKVALUE)*OrderLots(),3);
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
   text="TakeProfit (sum) = "+DoubleToString(EA_takeProfit,3)+" set for ("+IntegerToString(tp_counter)+"/"+IntegerToString(open_position_counter)+") - Re: "+
        DoubleToString(((EA_takeProfit/AccountInfoDouble(ACCOUNT_BALANCE))*100),2)+" %";

   createObject(11,OBJ_LABEL,0,x,y+upper_edge_shift*10,text);

   if(sl_counter==0)
      text="StopLoss (sum) = 0.00";
   else
     {
      text="StopLoss (sum) = "+DoubleToString(EA_StopLoss,3)+" set for ("+IntegerToString(sl_counter)+"/"+IntegerToString(open_position_counter)+") - Ri: "+
           DoubleToString(((EA_StopLoss/AccountInfoDouble(ACCOUNT_BALANCE))*100),2)+" %";
     }
   createObject(12,OBJ_LABEL,0,x,y+upper_edge_shift*11,text);

   if(Display_legend)
     {
      // LEGEND
      text="ACTION LEGEND:";
      createObject(13,OBJ_LABEL,0,x,y+upper_edge_shift*15,text);

      text="* Press 1 to open BUY position!";
      createObject(14,OBJ_LABEL,0,x,y+upper_edge_shift*16,text);

      text="* Press 2 to open SELL position!";
      createObject(15,OBJ_LABEL,0,x,y+upper_edge_shift*17,text);

      text="* Press 3 to close positions!";
      createObject(16,OBJ_LABEL,0,x,y+upper_edge_shift*18,text);

      text="* Press 4 to increase volume!";
      createObject(17,OBJ_LABEL,0,x,y+upper_edge_shift*19,text);

      text="* Press 5 to decrease volume!";
      createObject(18,OBJ_LABEL,0,x,y+upper_edge_shift*20,text);

      text="* Press 6 to open BUY STOP position!";
      createObject(19,OBJ_LABEL,0,x,y+upper_edge_shift*21,text);

      text="* Press 7 to open SELL STOP position!";
      createObject(20,OBJ_LABEL,0,x,y+upper_edge_shift*22,text);

      text="* Press 8 to open BUY LIMIT position!";
      createObject(21,OBJ_LABEL,0,x,y+upper_edge_shift*23,text);

      text="* Press 9 to open SELL LIMIT position!";
      createObject(22,OBJ_LABEL,0,x,y+upper_edge_shift*24,text);

      text="* Press 0 to ABORT action in process!";
      createObject(23,OBJ_LABEL,0,x,y+upper_edge_shift*25,text);
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
//|                                                                  |
//+------------------------------------------------------------------+
void createObjectPT(int st_ID,ENUM_OBJECT obj,int window,datetime x,double y,string txt="")
  {
   ObjectCreate(ChartID(),EA_name+"_"+IntegerToString(st_ID),obj,window,x,y);
   ObjectSetText(EA_name+"_"+IntegerToString(st_ID),txt,16,"Arial",clrRed);
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
         button 1 -> open BUY order manually
         button 2 -> open SELL order manually
         button 3 -> CLOSE BUY or SELL order manually   
         button 4 -> increase current Lot volume (Lots in increments of 0.01)
         button 5 -> decrease current Lot volume (Lots in increments of 0.01) 
         
         button 6 -> set PENDING BUY STOP order manually
         button 7 -> set PENDING SELL STOP order manually
         button 8 -> set PENDING BUY LIMIT order manually
         button 9 -> set PENDING SELL LIMIT order manually
   */
   if(id==CHARTEVENT_KEYDOWN && (GlobalVariableGet(global_Record)==-1.0))
     {
      // *********************************************************************************************************************
      // button 1
      if(lparam==49 || lparam==97)
        {
         if(!Set_SL && !Set_TP)
            if(OrderSend(_Symbol,OP_BUY,GlobalVariableGet(global_Volume),MarketInfo(_Symbol,MODE_ASK),slippage,0,0,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
               Print("Order BUY successfully opened for ",EA_name,"_",ID);
         else
            Print("Order BUY unsuccessfully opened for ",EA_name,"_",ID);
         else
           {
            GlobalVariableSet(global_Record,lparam);
            if(Set_SL)
               createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input SL");
            else
               createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input TP");
           }
        }
      // *********************************************************************************************************************
      // button 2 
      if(lparam==50 || lparam==98)
        {
         if(!Set_SL && !Set_TP)
            if(OrderSend(_Symbol,OP_SELL,GlobalVariableGet(global_Volume),MarketInfo(_Symbol,MODE_BID),slippage,0,0,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
               Print("Order SELL successfully opened for ",EA_name,"_",ID);
         else
            Print("Order SELL unsuccessfully opened for ",EA_name,"_",ID);
         else
           {
            GlobalVariableSet(global_Record,lparam);
            if(Set_SL)
               createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input SL");
            else
               createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input TP");
           }
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
         if(GlobalVariableGet(global_Volume)>=MarketInfo(_Symbol,MODE_MAXLOT))
            Alert("Top limit for order volume reached!");
         else
           {
            GlobalVariableSet(global_Volume,GlobalVariableGet(global_Volume)+MarketInfo(_Symbol,MODE_LOTSTEP));

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
         if(GlobalVariableGet(global_Volume)<=MarketInfo(_Symbol,MODE_MINLOT))
            Alert("Volume is at minimum, it can not be decreased!");
         else
           {
            GlobalVariableSet(global_Volume,GlobalVariableGet(global_Volume)-MarketInfo(_Symbol,MODE_LOTSTEP));

            text="Lot amount = "+DoubleToString(GlobalVariableGet(global_Volume),2);
            createObject(4,OBJ_LABEL,0,x,y+upper_edge_shift*3,text);

            text="Tick value = "+DoubleToString(MarketInfo(_Symbol,MODE_TICKVALUE)*GlobalVariableGet(global_Volume),3)+" "+AccountInfoString(ACCOUNT_CURRENCY);
            createObject(5,OBJ_LABEL,0,x,y+upper_edge_shift*4,text);

            text="Margin required = "+DoubleToString(MarketInfo(_Symbol,MODE_MARGINREQUIRED)*GlobalVariableGet(global_Volume),3);
            createObject(6,OBJ_LABEL,0,x,y+upper_edge_shift*5,text);
           }
        }
      // *********************************************************************************************************************
      // button 6, button 7, button 8, button 9
      if((lparam>=54 && lparam<=57) || (lparam>=102 && lparam<=105))
         if(Allow_pen_orders)
           {
            GlobalVariableSet(global_Record,lparam);
            createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input PRICE");
           }
      else
         Alert("Pending orders not allowed!");
     }
// RESET SELECTED ACTION (button 0)
   if(id==CHARTEVENT_KEYDOWN)
      if(GlobalVariableGet(global_Record)!=-1.0)
         if(lparam==48 || lparam==96)
           {
            GlobalVariableSet(global_Record,-1.0);
            glDelete();
            ObjectDelete(ChartID(),EA_name+"_"+(string)24);
           }
// MOUSE ON CHART EVENT -> LOCATION of MOUSE CLICK
   if(id==CHARTEVENT_CLICK && (GlobalVariableGet(global_Record)!=-1.0))
     {
      double action=GlobalVariableGet(global_Record);

      int window=0;
      double price=0,
      SL = 0,
      TP = 0;
      datetime time=0;

      ChartXYToTimePrice(ChartID(),(int)lparam,(int)dparam,window,time,price);

      price=NormalizeDouble(price,_Digits);

      // determine what inputs are required for ACTIVE or PENDING TRADES
      // if active trade, and Set_SL TRUE, first set SL, then if Set_TP TRUE, set TP
      if(action==49.0 || action==50 || action==97.0 || action==98.0)
        {
         if(Set_SL && Set_TP)
           {
            if(GlobalVariableGet(global_TakeProfit)==0 && GlobalVariableGet(global_StopLoss)!=0)
              {
               GlobalVariableSet(global_TakeProfit,price);
               GlobalVariableSet(global_Record,-1.0);
              }

            if(GlobalVariableGet(global_StopLoss)==0)
              {
               GlobalVariableSet(global_StopLoss,price);
               createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input TP");
              }
           }

         if(!Set_TP && Set_SL)
            if(GlobalVariableGet(global_StopLoss)==0)
               GlobalVariableSet(global_StopLoss,price);

         if(Set_TP && !Set_SL)
            if(GlobalVariableGet(global_TakeProfit)==0)
               GlobalVariableSet(global_TakeProfit,price);
        }

      if((action>=54 && action<=57) || (action>=102 && action<=105))
         if(GlobalVariableGet(global_Price)==0)
           {
            GlobalVariableSet(global_Price,price);
            createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input SL");
           }
      else if(GlobalVariableGet(global_StopLoss)==0)
        {
         GlobalVariableSet(global_StopLoss,price);
         createObjectPT(24,OBJ_TEXT,0,Time[0]+_Period*60*20,Bid,"Input TP");
        }
      else if(GlobalVariableGet(global_TakeProfit)==0)
        {
         GlobalVariableSet(global_TakeProfit,price);
         GlobalVariableSet(global_Record,-1.0);
        }

      if(action==49.0 || action==50 || action==97.0 || action==98.0)
        {
         if(Set_SL && !Set_TP)
            GlobalVariableSet(global_Record,-1.0);

         if(!Set_SL && Set_TP)
            GlobalVariableSet(global_Record,-1.0);
        }
      SL = GlobalVariableGet(global_StopLoss);
      TP = GlobalVariableGet(global_TakeProfit);
      price=GlobalVariableGet(global_Price);
      // *********************************************************************************************************************
      // BUY (button 1) 
      if((action==49 || action==97) && GlobalVariableGet(global_Record)==-1.0)
        {
         ObjectDelete(ChartID(),EA_name+"_"+(string)24);

         if(((Set_SL && SL<MarketInfo(_Symbol,MODE_ASK)) || !Set_SL) && ((Set_TP && TP>MarketInfo(_Symbol,MODE_ASK)) || !Set_TP))
            if(OrderSend(_Symbol,OP_BUY,GlobalVariableGet(global_Volume),MarketInfo(_Symbol,MODE_ASK),slippage,SL,TP,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
               Print("Order BUY successfully opened for ",EA_name,"_",ID);
         else
            Print("Order BUY unsuccessfully opened for ",EA_name,"_",ID);
         else
            Alert("StopLoss or TakeProfit for BUY order is incorrect!");

         glDelete();
        }
      // *********************************************************************************************************************
      // SELL (button 2) 
      if((action==50 || action==98) && GlobalVariableGet(global_Record)==-1.0)
        {
         ObjectDelete(ChartID(),EA_name+"_"+(string)24);

         if(((Set_SL && SL>MarketInfo(_Symbol,MODE_BID)) || !Set_SL) && ((Set_TP && TP<MarketInfo(_Symbol,MODE_BID)) || !Set_TP))
            if(OrderSend(_Symbol,OP_SELL,GlobalVariableGet(global_Volume),MarketInfo(_Symbol,MODE_BID),slippage,SL,TP,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
               Print("Order SELL successfully opened for ",EA_name,"_",ID);
         else
            Print("Order SELL unsuccessfully opened for ",EA_name,"_",ID);
         else
            Alert("StopLoss or TakeProfit for SELL order is incorrect!");

         glDelete();
        }
      // *********************************************************************************************************************
      // BUY STOP (button 6)
      if((action==54.0 || action==102.0) && GlobalVariableGet(global_Record)==-1.0)
        {
         ObjectDelete(ChartID(),EA_name+"_"+(string)24);

         if(price>MarketInfo(_Symbol,MODE_ASK))
            if(SL<price && TP>price)
               if(OrderSend(_Symbol,OP_BUYSTOP,GlobalVariableGet(global_Volume),price,slippage,SL,TP,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
                 {
                  Print("Order BUY STOP successfully opened for ",EA_name,"_",ID);
                  Alert("Pending order BUY STOP set at price = ",price," with SL = ",SL," and TP = ",TP);
                 }
         else
           {
            Print("Order BUY STOP unsuccessfully opened for ",EA_name,"_",ID);
            Alert("ERROR at setting Pending order BUY STOP!");
           }
         else
            Alert("StopLoss or TakeProfit for BUY STOP order is incorrect!");
         else
            Alert("The Price level set for BUY STOP order is outside allowed range!");

         glDelete();
        }
      // *********************************************************************************************************************
      // SELL STOP (button 7)
      if((action==55.0 || action==103.0) && GlobalVariableGet(global_Record)==-1.0)
        {
         ObjectDelete(ChartID(),EA_name+"_"+(string)24);

         if(price<MarketInfo(_Symbol,MODE_BID))
            if(SL>price && TP<price)
               if(OrderSend(_Symbol,OP_SELLSTOP,GlobalVariableGet(global_Volume),price,slippage,SL,TP,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
                 {
                  Print("Order SELL STOP successfully opened for ",EA_name,"_",ID);
                  Alert("Pending order SELL STOP set at price = ",price," with SL = ",SL," and TP = ",TP);
                 }
         else
           {
            Print("Order SELL STOP unsuccessfully opened for ",EA_name,"_",ID);
            Alert("ERROR at setting Pending order SELL STOP!");
           }
         else
            Alert("StopLoss or TakeProfit for SELL STOP order is incorrect!");
         else
            Alert("The Price level set for SELL STOP order is outside allowed range!");

         glDelete();
        }
      // *********************************************************************************************************************
      // BUY LIMIT (button 8)
      if((action==56.0 || action==104.0) && GlobalVariableGet(global_Record)==-1.0)
        {
         ObjectDelete(ChartID(),EA_name+"_"+(string)24);

         if(price<MarketInfo(_Symbol,MODE_ASK))
            if(SL<price && TP>price)
               if(OrderSend(_Symbol,OP_BUYLIMIT,GlobalVariableGet(global_Volume),price,slippage,SL,TP,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
                 {
                  Print("Order BUY LIMIT successfully opened for ",EA_name,"_",ID);
                  Alert("Pending order BUY LIMIT set at price = ",price," with SL = ",SL," and TP = ",TP);
                 }
         else
           {
            Print("Order BUY LIMIT unsuccessfully opened for ",EA_name,"_",ID);
            Alert("ERROR at setting Pending order BUY LIMIT!");
           }
         else
            Alert("StopLoss or TakeProfit for BUY LIMIT order is incorrect!");
         else
            Alert("The Price level set for BUY LIMIT order is outside allowed range!");

         glDelete();
        }
      // *********************************************************************************************************************
      // SELL LIMIT (button 9)
      if((action==57.0 || action==105.0) && GlobalVariableGet(global_Record)==-1.0)
        {
         ObjectDelete(ChartID(),EA_name+"_"+(string)24);

         if(price>MarketInfo(_Symbol,MODE_BID))
            if(SL>price && TP<price)
               if(OrderSend(_Symbol,OP_SELLLIMIT,GlobalVariableGet(global_Volume),price,slippage,SL,TP,EA_name+"  "+IntegerToString(ID),ID,0,clrNONE)>0)
                 {
                  Print("Order SELL LIMIT successfully opened for ",EA_name,"_",ID);
                  Alert("Pending order SELL LIMIT set at price = ",price," with SL = ",SL," and TP = ",TP);
                 }
         else
           {
            Print("Order SELL LIMIT unsuccessfully opened for ",EA_name,"_",ID);
            Alert("ERROR at setting Pending order SELL LIMIT!");
           }
         else
            Alert("StopLoss or TakeProfit for SELL LIMIT order is incorrect!");
         else
            Alert("The Price level set for SELL LIMIT order is outside allowed range!");

         glDelete();
        }
     }
  }
//+------------------------------------------------------------------+
//| Custom delete global varables function                           |
//+------------------------------------------------------------------+
void glDelete()
  {
   GlobalVariableSet(global_Price,0.0);
   GlobalVariableSet(global_StopLoss,0.0);
   GlobalVariableSet(global_TakeProfit,0.0);
  }
//+------------------------------------------------------------------+
