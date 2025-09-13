//+------------------------------------------------------------------+
//|                                                 MoveStopLoss.mq4 |
//|                        Copyright 2023, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Wamek TrailingStop EA-2023"
#property link      "eawamek@gmail.com"
#property version   "1.00"
#property strict
#property description "<<<< Moves the stoploss along with ask/bid at a predetermined distance >>>>>"
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

input bool AutoTrail= true;
input int Distance2Trail=300;

double When2Trail;

int OnInit()
  {
//---
  HideTestIndicators(true);
   if( !IsTradeAllowed()){
      Alert("Enable AutoTrading Please"); 
     return(0);
     }


//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   ObjectDelete("WAMEK1");

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
  ATRMaxMin();
 

 MoveStoploss();

 DrawLABEL(0,"WAMEK1",StringConcatenate("Distance2MoveStopLoss: "+DoubleToStr(When2Trail/Point(),0))+"pips", 12*50,40,DodgerBlue);
   
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//|   Move stoploss with price action                                |
//+------------------------------------------------------------------+



void MoveStoploss()
  {  
   int OrdMod;
   
   if(AutoTrail==true) When2Trail =NormalizeDouble( 0.85*AtrMax,Digits);
   else When2Trail=NormalizeDouble( Distance2Trail*Point(),Digits);

   for(int p=OrdersTotal()-1; p >=0; p--)
      if(OrderSelect(p,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderSymbol()==Symbol() )
           {
            if(OrderType()==OP_BUY)
               if(Bid> OrderOpenPrice() && OrderStopLoss()<Bid-When2Trail)
                  OrdMod= OrderModify(OrderTicket(),OrderOpenPrice(),Bid-When2Trail,OrderTakeProfit(),0,clrNONE);


            if(OrderType()==OP_SELL)
               if(Ask < OrderOpenPrice() && (OrderStopLoss()>Ask+When2Trail || OrderStopLoss()==0))
                  OrdMod= OrderModify(OrderTicket(),OrderOpenPrice(),Ask+When2Trail,OrderTakeProfit(),0,clrNONE);
           }

        }

  }




//--- ATR--------

double AtrMin,AtrMax;
void ATRMaxMin(){

 AtrMin =876532009;
 AtrMax =-876532009;
  for (int i=30; i>0; i--) {
 double Atr = iATR(Symbol(),Period(),7,i);
 
 AtrMax = MathMax(AtrMax,Atr);
 AtrMin = MathMin(AtrMin,Atr);

     }

}



//---Draw Object on the chart

void DrawLABEL(long Id, string ObjName, string Info, float X, float Y, color clr)
  {
   if(ObjectFind(Id,ObjName)==-1)
     {
      ObjectCreate(Id,ObjName, OBJ_LABEL, 0, 0, 0);
      ObjectSet(ObjName, OBJPROP_CORNER, 0);
      ObjectSet(ObjName, OBJPROP_XDISTANCE, X);
      ObjectSet(ObjName, OBJPROP_YDISTANCE, Y);
      ObjectSetInteger(Id,ObjName,OBJPROP_FONTSIZE,10); 
      ChartRedraw(Id);
     }
   ObjectSetText(ObjName,Info,12,"Arial",clr);
  }


