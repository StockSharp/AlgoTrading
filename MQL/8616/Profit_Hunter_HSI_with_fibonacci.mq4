//+------------------------------------------------------------------+
//|                                                   lOT HUNTER.mq4 |
//|                                                Copyright © 2008, |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008,Risdyanto "
#property link      ""

extern int  numBars = 3;
extern int      lot = 1;
extern int maPeriod = 5;
extern int timeFrame =1;
double support;
double resist;
string trendType,trendType2;
string Signal;

extern int daysBackForHigh = 1;
extern int daysBackForLow = 1;
//---- buffers
double Rates[][6];
//----
double fib000,
       fib236,
       fib146,
       fib382,
       fib50,
       fib618,
       fib764,
       fib91,
       fib100,
       fib1618,
       fib2618,
       fib4236,
       range,
       prevRange,
       high,
       low;
bool objectsExist, highFirst;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
    ObjectCreate("lineSupport",OBJ_HLINE,0,0,0);
    ObjectSet("lineSupport",OBJPROP_COLOR,Red);
    ObjectSet("lineSupport",OBJPROP_WIDTH,1);
    ObjectSet("lineSupport",OBJPROP_STYLE,STYLE_DASHDOT);
    
    ObjectCreate("lineResist",OBJ_HLINE,0,0,0);
    ObjectSet("lineResist",OBJPROP_COLOR,Blue);
    ObjectSet("lineResist",OBJPROP_WIDTH,1);
    ObjectSet("lineResist",OBJPROP_STYLE,STYLE_DASHDOT);
    
    ObjectCreate("lblTrendType",OBJ_LABEL,0,0,0,0,0);
    ObjectSet("lblTrendType",OBJPROP_XDISTANCE,50);
    ObjectSet("lblTrendType",OBJPROP_YDISTANCE,20);
    ObjectSetText("lblTrendType","TrendType",14,"Tahoma",Red);
   
    ObjectCreate("lblTrendType2",OBJ_LABEL,0,0,0,0,0);
    ObjectSet("lblTrendType2",OBJPROP_XDISTANCE,50);
    ObjectSet("lblTrendType2",OBJPROP_YDISTANCE,50);
    ObjectSetText("lblTrendType2","Signal",14,"Tahoma",Red);
   
    ObjectCreate("Aris",OBJ_LABEL,0,0,0,0,0);
    ObjectSet("Aris",OBJPROP_XDISTANCE,50 );
    ObjectSet("Aris",OBJPROP_YDISTANCE,100);
    ObjectSetText( "Aris", " Risdyanto Trading Expert "  , 12, "Arial", Blue );
 
 
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   ObjectsDeleteAll();
   ObjectDelete("fib000");
   ObjectDelete("fib000_label");
   ObjectDelete("fib236");
   ObjectDelete("fib236_label");
   ObjectDelete("fib382");
   ObjectDelete("fib382_label");
   ObjectDelete("fib50");
   ObjectDelete("fib50_label");
   ObjectDelete("fib618");
   ObjectDelete("fib618_label");
   ObjectDelete("fib100");
   ObjectDelete("fib100_label");
   ObjectDelete("fib1618");
   ObjectDelete("fib1618_label");
   ObjectDelete("fib2618");
   ObjectDelete("fib2618_label");
   ObjectDelete("fib4236");
   ObjectDelete("fib4236_label");
    // ObjectDelete("Aris");
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
  MarketAnalize();
  if(OrdersTotal()==0)
   CheckForOpen();
  else
   CheckForClose();
//----
   return(0);
  }

void MarketAnalize()

{
// support = 100000;
 //resist  = 0;
  for(int k = 1;k<=numBars;k++)
  { 
  
  // if (support < iLow(Symbol(),timeFrame,k))
     support = iLow (Symbol(),timeFrame,k);
   //  if (resist > iHigh(Symbol(),timeFrame,k))
     resist  = iHigh(Symbol(),timeFrame,k);
  }   
 
  ObjectSet("lineSupport",OBJPROP_PRICE1,support);
  ObjectSet("lineResist" ,OBJPROP_PRICE1, resist);
  
//--------------------------------------------------------------------------------  
  double ma = iMA(Symbol(),timeFrame,maPeriod,0,MODE_EMA,PRICE_CLOSE,0);
  
  if(Ask>ma)
  {
    trendType = "Naik";
  }
  if(Bid<ma)
  {
    trendType = "Turun";
  } 

  ObjectSetText("lblTrendType",trendType);
 
 int i = 0;
   prevRange = 0;
   objectsExist = false;
   ArrayCopyRates(Rates, Symbol(), PERIOD_D1);   
   high = Rates[daysBackForHigh][3];
   low = Rates[daysBackForLow][2];
   range = high - low;
//----
   while(true) 
     {
       if(High[i] == high) 
         {
           highFirst = true;
           break;
         }
       else 
           if(Low[i] == low) 
             {
               highFirst = false;
               break;
             }
       i++;
     }
   // Delete Objects if necessary
   if(prevRange != range) 
     {
       ObjectDelete("fib000");
       ObjectDelete("fib000_label");
       ObjectDelete("fib146");
       ObjectDelete("fib146_label");
       ObjectDelete("fib236");
       ObjectDelete("fib236_label");
   //    ObjectDelete("fib382");
   //    ObjectDelete("fib382_label");
  //     ObjectDelete("fib50");
  ///     ObjectDelete("fib50_label");
   //    ObjectDelete("fib618");
   //    ObjectDelete("fib618_label");
       ObjectDelete("fib764");
       ObjectDelete("fib764_label");
       ObjectDelete("fib91");
       ObjectDelete("fib91_label");
       ObjectDelete("fib100");
       ObjectDelete("fib100_label");
       ObjectDelete("fib1618");
       ObjectDelete("fib1618_label");
       ObjectDelete("fib2618");
       ObjectDelete("fib2618_label");
       ObjectDelete("fib4236");
       ObjectDelete("fib4236_label");
       objectsExist = false;
       prevRange = range;
     }
//----
   if(highFirst == true) 
     {
       fib000 = low;
       fib146 = (range * 0.09) + low;
       fib236 = (range * 0.236) + low;
       fib382 = (range * 0.382) + low;
       fib50 = (high + low) / 2;
       fib618 = (range * 0.618) + low;
       fib764 = (range * 0.764) + low;
       fib91 = (range * 0.91) + low;
       fib100 = high;
       fib1618 = (range * 0.618) + high;
       fib2618 = (range * 0.618) + (high + range);
       fib4236 = (range * 0.236) + high + (range * 3);
     }
   else 
       if(highFirst == false) 
         {
           fib000 = high;
           fib146 = high - (range * 0.09);
           fib236 = high - (range * 0.236);
           fib382 = high - (range * 0.382);
           fib50  = (high + low) / 2;
           fib618 = high - (range * 0.618);
           fib764 = high - (range * 0.764);
           fib91 = high - (range * 0.91);
           fib100 = low;
           fib1618 = low - (range * 0.618);
           fib2618 = (low - range) - (range * 0.618);// + (high + range);
           fib4236 = low - (range * 3) - (range * 0.236);// + high + (range * 3);
         }
//----   
   if(objectsExist == false) 
     {
       ObjectCreate("fib000", OBJ_HLINE, 0, Time[40], fib000);
       ObjectSet("fib000", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib000", OBJPROP_COLOR, Red);
       ObjectCreate("fib000_label", OBJ_TEXT, 0, Time[0], fib000);
       ObjectSetText("fib000_label","                             0.0", 8, "Times", Black);
       //----
       ObjectCreate("fib146", OBJ_HLINE, 0, Time[40], fib146);
       ObjectSet("fib146", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib146", OBJPROP_COLOR, Red);
       ObjectCreate("fib146_label", OBJ_TEXT, 0, Time[0], fib146);
       ObjectSetText("fib146_label","                             14.6", 8, "Times", Black);
       //----
       ObjectCreate("fib236", OBJ_HLINE, 0, Time[40], fib236);
       ObjectSet("fib236", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib236", OBJPROP_COLOR, Red);
       ObjectCreate("fib236_label", OBJ_TEXT, 0, Time[0], fib236);
       ObjectSetText("fib236_label","                             23.6", 8, "Times", Black);
       //----
   //    ObjectCreate("fib382", OBJ_HLINE, 0, Time[40], fib382);
   //    ObjectSet("fib382", OBJPROP_STYLE, STYLE_DASHDOTDOT);
   //    ObjectSet("fib382", OBJPROP_COLOR, Red);
   //    ObjectCreate("fib382_label", OBJ_TEXT, 0, Time[0], fib382);
   //    ObjectSetText("fib382_label","                             38.2", 8, "Times", Black);
       //----
   //    ObjectCreate("fib50", OBJ_HLINE, 0, Time[40], fib50);
   //    ObjectSet("fib50", OBJPROP_STYLE, STYLE_DASHDOTDOT);
   //    ObjectSet("fib50", OBJPROP_COLOR, Red);
   //    ObjectCreate("fib50_label", OBJ_TEXT, 0, Time[0], fib50);
   //    ObjectSetText("fib50_label","                             50.0", 8, "Times", Black);
       //----
     //  ObjectCreate("fib618", OBJ_HLINE, 0, Time[40], fib618);
    //   ObjectSet("fib618", OBJPROP_STYLE, STYLE_DASHDOTDOT);
    //   ObjectSet("fib618", OBJPROP_COLOR, Red);
    //   ObjectCreate("fib618_label", OBJ_TEXT, 0, Time[0], fib618);
    //   ObjectSetText("fib618_label","                             61.8", 8, "Times", Black);
       //----
       ObjectCreate("fib764", OBJ_HLINE, 0, Time[40], fib764);
       ObjectSet("fib764", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib764", OBJPROP_COLOR, Red);
       ObjectCreate("fib764_label", OBJ_TEXT, 0, Time[0], fib764);
       ObjectSetText("fib764_label","                             76.4", 8, "Times", Black);
       //----
       ObjectCreate("fib91", OBJ_HLINE, 0, Time[40], fib91);
       ObjectSet("fib91", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib91", OBJPROP_COLOR, Red);
       ObjectCreate("fib91_label", OBJ_TEXT, 0, Time[0], fib91);
       ObjectSetText("fib91_label","                             91", 8, "Times", Black);
       //----
       ObjectCreate("fib100", OBJ_HLINE, 0, Time[40], fib100);
       ObjectSet("fib100", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib100", OBJPROP_COLOR, Red);
       ObjectCreate("fib100_label", OBJ_TEXT, 0, Time[0], fib100);
       ObjectSetText("fib100_label","                             100.0", 8, "Times", Black);
       //----
       ObjectCreate("fib1618", OBJ_HLINE, 0, Time[40], fib1618);
       ObjectSet("fib1618", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib1618", OBJPROP_COLOR, Red);
       ObjectCreate("fib1618_label", OBJ_TEXT, 0, Time[0], fib1618);
       ObjectSetText("fib1618_label","                             161.8", 8, "Times", Black);
       //----
       ObjectCreate("fib2618", OBJ_HLINE, 0, Time[40], fib2618);
       ObjectSet("fib2618", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib2618", OBJPROP_COLOR, Red);
       ObjectCreate("fib2618_label", OBJ_TEXT, 0, Time[0], fib2618);
       ObjectSetText("fib2618_label","                             261.8", 8, "Times", Black);
       //----
       ObjectCreate("fib4236", OBJ_HLINE, 0, Time[40], fib4236);
       ObjectSet("fib4236", OBJPROP_STYLE, STYLE_DASHDOTDOT);
       ObjectSet("fib4236", OBJPROP_COLOR, Red);
       ObjectCreate("fib4236_label", OBJ_TEXT, 0, Time[0], fib4236);
       ObjectSetText("fib4236_label","                             423.6", 8, "Times", Black);
     
   
     if(highFirst == true)   
     {if  (Ask < fib236 )   {Signal = "Reverse-Buy";}
      else if (Bid >fib764) {Signal = "Reverse-Sell";}
      else if ((Ask && Bid > fib236) && (Ask&&Bid < fib764)) {Signal = "Trading-Area";}
      else if (Bid > fib91) {Signal = "Continuation";}
      else if (Ask < fib146) {Signal = "Continuation";}}
     
     else 
       if(highFirst == false)  
     {if (Bid > fib236 )    {Signal = "Reverse-Sell";}
       else if (Ask < fib764 ) {Signal = "Reverse_Buy";}
       else if ((Ask && Bid < fib236) && (Ask&&Bid > fib764)) {Signal = "Trading-Area";}
       else if (Ask < fib91) {Signal = "Continuation";}
       else if (Bid > fib146) {Signal = "Continuation";}}
     
       
        ObjectSetText("lblTrendType2",Signal);
}
  }


//------------------------------------------------------------------------
void CheckForOpen()
{
   if((trendType=="Naik") && (Signal=="Trading-Area"))
  { if(Ask>resist) OrderSend(Symbol(), OP_BUY,lot,Ask,3,support,0);}
  
   if((trendType=="Naik") && (Signal=="Reverse-Sell") && (highFirst == false))
  { if(Ask<resist) OrderSend(Symbol(), OP_SELL,lot,Bid,3,fib146,0);}
  
  if((trendType=="Naik") && (Signal=="Reverse-Buy")&& (highFirst == false))
  { if(Bid<resist) OrderSend(Symbol(), OP_BUY,lot,Ask,3,fib91,0);}
 
  if((trendType=="Turun") && (Signal=="Trading-Area"))
  { if(Bid < support)  OrderSend(Symbol(),OP_SELL,lot,Bid,3,resist,0); }
  
  if((trendType=="Turun") && (Signal=="Reverse-Sell") && (highFirst == true))
  { if(Bid<resist) OrderSend(Symbol(), OP_SELL,lot,Bid,3,fib91,0);}
  
  if((trendType=="Turun") && (Signal=="Reverse-Buy")&& (highFirst == true))
  { if(Bid<resist) OrderSend(Symbol(), OP_BUY,lot,Ask,3,fib146,0);}
  
}

//------------------------------------------------------------------
void CheckForClose()
{
   if(OrderProfit()<0)
 {
   if(OrderType()==OP_BUY)
   {
     if(Bid<support) OrderClose(OrderTicket(),OrderLots(),Bid,3);
   }
   if(OrderType()==OP_SELL)
   { 
      if(Ask>resist)  OrderClose(OrderTicket(),OrderLots(),Ask,3);
   }
 }
 
OrderSelect(0,SELECT_BY_POS);
 if(OrderProfit()>0)
 {
   if(OrderType()==OP_BUY)
   {
     if(Bid<support) OrderClose(OrderTicket(),OrderLots(),Bid,3);
   }
   if(OrderType()==OP_SELL)
   { 
      if(Ask>resist)  OrderClose(OrderTicket(),OrderLots(),Ask,3);
   }
 }
if(OrderType()==OP_BUY)
 {
    //if((Ask>OrderOpenPrice()+Point*20)&&(OrderStopLoss()<(OrderOpenPrice()+Point*15)))
  //  OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*15,OrderTakeProfit(),0,Blue);
   // if((Ask>OrderOpenPrice()+Point*30)&&(OrderStopLoss()<(OrderOpenPrice()+Point*25)))
  //  OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*25,OrderTakeProfit(),0,Blue);
 //   if((Ask>OrderOpenPrice()+Point*40)&&(OrderStopLoss()<(OrderOpenPrice()+Point*35)))
 //   OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*35,OrderTakeProfit(),0,Blue);    
    if((Ask>OrderOpenPrice()+Point*60)&&(OrderStopLoss()<(OrderOpenPrice()+Point*55)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*55,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*80)&&(OrderStopLoss()<(OrderOpenPrice()+Point*75)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*75,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*100)&&(OrderStopLoss()<(OrderOpenPrice()+Point*95)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*95,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*120)&&(OrderStopLoss()<(OrderOpenPrice()+Point*115)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*115,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*140)&&(OrderStopLoss()<(OrderOpenPrice()+Point*135)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*135,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*160)&&(OrderStopLoss()<(OrderOpenPrice()+Point*155)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*155,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*180)&&(OrderStopLoss()<(OrderOpenPrice()+Point*175)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*175,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*200)&&(OrderStopLoss()<(OrderOpenPrice()+Point*195)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*195,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*220)&&(OrderStopLoss()<(OrderOpenPrice()+Point*210)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*210,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*240)&&(OrderStopLoss()<(OrderOpenPrice()+Point*230)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*230,OrderTakeProfit(),0,Blue);
    if((Ask>OrderOpenPrice()+Point*260)&&(OrderStopLoss()<(OrderOpenPrice()+Point*250)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*250,OrderTakeProfit(),0,Blue);       
 }
 if(OrderType()==OP_SELL)
 {
  // if((Bid<OrderOpenPrice()-Point*20)&&(OrderStopLoss()>(OrderOpenPrice()-Point*15)))
//    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*15,OrderTakeProfit(),0,Blue);   
 //  if((Bid<OrderOpenPrice()-Point*30)&&(OrderStopLoss()>(OrderOpenPrice()-Point*25)))
 //   OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*25,OrderTakeProfit(),0,Blue);   
//   if((Bid<OrderOpenPrice()-Point*40)&&(OrderStopLoss()>(OrderOpenPrice()-Point*35)))
//    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*35,OrderTakeProfit(),0,Blue);     
   if((Bid<OrderOpenPrice()-Point*60)&&(OrderStopLoss()>(OrderOpenPrice()-Point*55)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*55,OrderTakeProfit(),0,Blue);     
   if((Bid<OrderOpenPrice()-Point*80)&&(OrderStopLoss()>(OrderOpenPrice()-Point*75)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*75,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*100)&&(OrderStopLoss()>(OrderOpenPrice()-Point*95)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*95,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*120)&&(OrderStopLoss()>(OrderOpenPrice()-Point*115)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*115,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*140)&&(OrderStopLoss()>(OrderOpenPrice()-Point*135)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*135,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*160)&&(OrderStopLoss()>(OrderOpenPrice()-Point*155)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*155,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*180)&&(OrderStopLoss()>(OrderOpenPrice()-Point*175)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*175,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*200)&&(OrderStopLoss()>(OrderOpenPrice()-Point*195)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*195,OrderTakeProfit(),0,Blue);
    if((Bid<OrderOpenPrice()-Point*220)&&(OrderStopLoss()>(OrderOpenPrice()-Point*210)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*210,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*240)&&(OrderStopLoss()>(OrderOpenPrice()-Point*230)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*230,OrderTakeProfit(),0,Blue);
   if((Bid<OrderOpenPrice()-Point*260)&&(OrderStopLoss()>(OrderOpenPrice()-Point*250)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*250,OrderTakeProfit(),0,Blue);
  }
}
//+------------------------------------------------------------------+