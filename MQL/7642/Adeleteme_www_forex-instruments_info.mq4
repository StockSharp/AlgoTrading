//+------------------------------------------------------------------+
//|                                                 L3_H3_Expert.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
extern bool ShowComments=false;
extern int  SundayOpenHour =0, NewYorkCloseHour=22, NewYorkFridayCloseHour=21;//IBFX_SunOpenHour=23 FXDD=0
extern int  AsianPivotHour=23; // IBFX Time 0  = 4pm (PST) or 7pm (EST) 22 = 2pm (PST) or 5pm (EST)
extern int  LondonPivotHour=6; //IBFX 8 = 12am (PST-Midnight) or 3am (EST)
extern int  NewYorkPivotHour=13;
//----
extern double Lots=1.0;
extern double StopLoss=32;//33;
extern double TakeProfit=150;//150;
extern double TrailingStop=0;
extern double Slippage=0;
//----
int buyMagicNum, sellMagicNum;
double yesterday_high=0, yesterday_low=0, yesterday_close=0, yesterday_open=0;
double P=0, M0=0, M1=0, M2=0,M3=0, M4=0, M5=0;
double S1=0, S2=0, S3=0, S4=0;
double R1=0, R2=0, R3=0, R4=0;
double L3, H3, D3=0.2750;
int buyNum, sellNum, buyDay=0, sellDay=0;
bool closeBuys=false, closeSells=false;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   //CALCULATE PIVOTS
   //AsianPivotHour=23 LondonPivotHour=6 NewYorkPivotHour=13 
   //SundayOpenHour =0 NewYorkCloseHour=23, NewYorkFridayCloseHour=22;
   int i, cnt, ticket, counted_bars=Bars, startPivotHour, stopPivotHour, pivotCloseTime, pivotOpenTime, barTime;
   double yesterday_high, yesterday_low, yesterday_close;
   bool pivotBuy=false, pivotSell=false, buyOK=true, sellOK=true, startPivots=false, pivotsCompleted=false;
   //startPivotHour=23;
   if (Hour()>=AsianPivotHour && Hour() < LondonPivotHour)
   {buyMagicNum=11; sellMagicNum=12; startPivotHour=AsianPivotHour;  stopPivotHour=AsianPivotHour;}
   //{buyMagicNum=Day()+11; sellMagicNum=Day()+12; startPivotHour = AsianPivotHour;  stopPivotHour = AsianPivotHour;}
   if (Hour() < LondonPivotHour)
   {buyMagicNum=11; sellMagicNum=12; startPivotHour=AsianPivotHour;  stopPivotHour=AsianPivotHour;}
   //{buyMagicNum=Day()+11; sellMagicNum=Day()+12; startPivotHour = AsianPivotHour;  stopPivotHour = AsianPivotHour;}
   if (Hour()>=NewYorkCloseHour)
   {buyMagicNum=Day()+11; sellMagicNum=Day()+12; startPivotHour=AsianPivotHour;  stopPivotHour=AsianPivotHour;}
   if (Hour()>=LondonPivotHour  && Hour() < NewYorkPivotHour)
   {buyMagicNum=21; sellMagicNum=22; startPivotHour=LondonPivotHour;  stopPivotHour=LondonPivotHour;}
   //{buyMagicNum=Day()+21; sellMagicNum=Day()+22; startPivotHour = LondonPivotHour;  stopPivotHour = LondonPivotHour;}
   if (Hour()>=NewYorkPivotHour  && Hour()<=NewYorkCloseHour)
   {buyMagicNum=31; sellMagicNum=32; startPivotHour=NewYorkPivotHour;  stopPivotHour=NewYorkPivotHour;}
   //{buyMagicNum=Day()+31; sellMagicNum=Day()+32; startPivotHour = NewYorkPivotHour;  stopPivotHour = NewYorkPivotHour;}   
   if (DayOfWeek()==1 && Hour() < LondonPivotHour)
   {buyMagicNum=11; sellMagicNum=12; startPivotHour=SundayOpenHour;  stopPivotHour=SundayOpenHour;}
   //buyMagicNum=buyMagicNum+DayOfYear(); 
   //sellMagicNum=sellMagicNum+DayOfYear();
/*
startPivotHour = LondonPivotHour;  
stopPivotHour = LondonPivotHour;
if (Hour() >=4) 
{
startPivotHour = Hour()-4;  
stopPivotHour = Hour()-4;
}
else 
{
startPivotHour = Hour()-0;  
stopPivotHour = Hour()-0;
}
buyMagicNum=DayOfYear()+21; 
sellMagicNum=DayOfYear()+22;
buyMagicNum=Hour()+21; 
sellMagicNum=Hour()+22;
*/
   startPivots=false;
   //
   for(i=0; i<counted_bars; i++)
     {
      barTime=TimeHour(iTime(NULL,PERIOD_H1,i));
      if (startPivots==false && barTime==startPivotHour)
        {
         startPivots=True;
         pivotCloseTime=iTime(NULL,PERIOD_H1,i);
         yesterday_high=iHigh(NULL,PERIOD_H1,i+1);//High[i+1];
         yesterday_low =iLow(NULL,PERIOD_H1,i+1);//Low[i+1];
         yesterday_close=iOpen(NULL,PERIOD_H1,i);//Open[i];
        }
      if (startPivots==True)
        {
         yesterday_high=MathMax(yesterday_high, iHigh(NULL,PERIOD_H1,i+1));
         yesterday_low =MathMin(yesterday_low, iLow(NULL,PERIOD_H1,i+1));
         barTime=TimeHour(iTime(NULL,PERIOD_H1,i+1));
         if (barTime==stopPivotHour)
           {
            pivotsCompleted=True;
            startPivots=False;
            yesterday_open=iOpen(NULL,PERIOD_H1,i+1);
            pivotOpenTime=iTime(NULL,PERIOD_H1,i+1);
            break;
           }
        }
     }
   P=(yesterday_high + yesterday_low + yesterday_close)/3;
   R1=(2*P)-yesterday_low;
   S1=(2*P)-yesterday_high;
   R2=P-S1+R1;
   S2=P-R1+S1;
   R3=(2*P)+(yesterday_high-(2*yesterday_low));
   R4=(3*P)+(yesterday_high-(3*yesterday_low));
   S3=(2*P)-((2* yesterday_high)-yesterday_low);
   S4=(3*P)-((3* yesterday_high)-yesterday_low);
   M0=(S2+S3)/2;
   M1=(S1+S2)/2;
   M2=(P+S1)/2;
   M3=(P+R1)/2;
   M4=(R1+R2)/2;
   M5=(R2+R3)/2;
   L3=yesterday_close - ((yesterday_high - yesterday_low)*(D3));
   H3=((yesterday_high - yesterday_low)* D3) + yesterday_close;
/*
int    AllOrders=HistoryTotal();     // history orders total 
     for(int i=AllOrders-1;i>=0;i--)
       {
        if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false)  break; 
        if(OrderSymbol()!=Symbol() || OrderType()>OP_BUY) continue;
if (b.ticket==0 && condition="Bullish" && b==0)   
{  ticket=.............}
}
*/
   int OpenBuys=0, OpenSells=0;
   //
   bool sellLimit=true, sellStop=true, buyLimit=true, buyStop=true;
   // MANAGE OPEN ORDERS //Check for open orders && Magic Numbers
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol()==Symbol())//check for opened position and symbol
        {
         if (OrderType()==OP_BUY && OrderMagicNumber()==buyMagicNum) buyStop=false;//buyOK=false;
         if (OrderType()==OP_BUY && OrderMagicNumber()==buyMagicNum) buyLimit=false;//buyOK=false;
         if (OrderType()==OP_BUYSTOP && OrderMagicNumber()==buyMagicNum) buyStop=false;//buyOK=false;   
         if (OrderType()==OP_BUYLIMIT && OrderMagicNumber()==buyMagicNum) buyLimit=false;//buyOK=false;
         if (OrderType()==OP_BUYSTOP && OrderMagicNumber()!=buyMagicNum) OrderDelete(OrderTicket());
         //if (OrderType()==OP_BUYLIMIT && OrderMagicNumber()!=buyMagicNum+2) OrderDelete(OrderTicket()); 
         if (OrderType()==OP_SELL && OrderMagicNumber()==sellMagicNum) sellStop=false;//sellOK=false;   
         if (OrderType()==OP_SELL && OrderMagicNumber()==sellMagicNum) sellLimit=false;//sellOK=false;  
         if (OrderType()==OP_SELLSTOP && OrderMagicNumber()==sellMagicNum) sellStop=false;//sellOK=false;   
         if (OrderType()==OP_SELLLIMIT && OrderMagicNumber()==sellMagicNum) sellLimit=false;//sellOK=false;
         if (OrderType()==OP_SELLSTOP && OrderMagicNumber()!=sellMagicNum) OrderDelete(OrderTicket());
         //if (OrderType()==OP_SELLLIMIT && OrderMagicNumber()!=sellMagicNum+2) OrderDelete(OrderTicket());
         if (OrderType()==OP_BUY || OrderType()==OP_BUYLIMIT || OrderType()==OP_BUYSTOP) OpenBuys++;
         if (OrderType()==OP_SELL || OrderType()==OP_SELLLIMIT || OrderType()==OP_SELLSTOP) OpenSells++;
/*
   if (OrderType()==OP_BUYSTOP && OrderMagicNumber()==firstStartTime) buyOK=false;
   if (OrderType()==OP_SELLSTOP && OrderMagicNumber()==firstLowStartTime) sellOK=false;
   if (OrderType()==OP_BUYSTOP && imaBuy==false) OrderDelete(OrderTicket());  
   if (OrderType()==OP_BUYSTOP && OrderOpenPrice() != buyPrice)       
      {OrderModify(OrderTicket(),buyPrice,buyPrice-StopLoss*Point,OrderTakeProfit(),0,CLR_NONE);}
   if (OrderType()==OP_SELLSTOP && imaSell==false) OrderDelete(OrderTicket());  
   if (OrderType()==OP_SELLSTOP && OrderOpenPrice() != sellPrice)
      {OrderModify(OrderTicket(),sellPrice,sellPrice+StopLoss*Point,OrderTakeProfit(),0,CLR_NONE);}
   if (OrderType()==OP_BUY) OpenBuys++;
   if (OrderType()==OP_SELL) OpenSells++;
   if (OrderType()==OP_BUYSTOP) OpenBuys++;
   if (OrderType()==OP_SELLSTOP) OpenSells++;
*/
        }
     }
/*
for(cnt=0;cnt<OrdersTotal();cnt++)
{
OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
if (OrderSymbol()==Symbol())//check for opened position and symbol
   {
   if (OrderType()==OP_BUY && buyDay==Day()) closeSells=true;//buyOK=false;
   if (OrderType()==OP_BUYSTOP || OrderType()==OP_BUYLIMIT && closeBuys==true) OrderDelete(OrderTicket()); 
   if (OrderType()==OP_SELL && sellDay==Day()) closeBuys=true; 
   if (OrderType()==OP_SELLSTOP || OrderType()==OP_SELLLIMIT && closeSells==true) OrderDelete(OrderTicket());   
   }
}

if (buyDay !=Day()) {closeBuys=false; closeSells=false;}
if (sellDay !=Day()) {closeBuys=false; closeSells=false;}
*/
   bool M1_M3=false, M2_M4=false;
   //
   if (yesterday_close > yesterday_open) M2_M4=true; else if (yesterday_close < yesterday_open) M1_M3=true;
      else {M2_M4=false; M1_M3=false;}
   double buyPrice, sellPrice, BuyTakeProfit, SellTakeProfit, SellStopLoss, BuyStopLoss;
/*
buyPrice=NormalizeDouble(M2,Digits);
BuyTakeProfit=NormalizeDouble(M3,Digits);
//BuyTakeProfit=NormalizeDouble(M2+60*Point,Digits);
sellPrice=NormalizeDouble(M3,Digits);
SellTakeProfit=NormalizeDouble(M2,Digits);
//SellTakeProfit=NormalizeDouble(M3-60*Point,Digits);
*/
   double imaFast, imaSlow, imaFast2, imaSlow2;
   bool buySig=false, sellSig=false;
   imaFast=iMA(NULL, PERIOD_M5, 5, 0, MODE_SMMA, PRICE_CLOSE, 1);
   imaSlow=iMA(NULL, PERIOD_M5, 20, 0, MODE_SMMA, PRICE_CLOSE, 1);
   imaFast2=iMA(NULL, PERIOD_M5, 5, 0, MODE_SMMA, PRICE_CLOSE, 2);
   imaSlow2=iMA(NULL, PERIOD_M5, 20, 0, MODE_SMMA, PRICE_CLOSE, 2);
/*
imaFast=iMA(NULL, PERIOD_H1, 5, 0, MODE_SMMA, PRICE_CLOSE, 1);
imaSlow=iMA(NULL, PERIOD_H1, 20, 0, MODE_SMMA, PRICE_CLOSE, 1);
imaFast2=iMA(NULL, PERIOD_H1, 5, 0, MODE_SMMA, PRICE_CLOSE, 2);
imaSlow2=iMA(NULL, PERIOD_H1, 20, 0, MODE_SMMA, PRICE_CLOSE, 2);
*/
/*
imaFast=iMA(NULL, PERIOD_M5, 20, 0, MODE_SMMA, PRICE_CLOSE, 1);
imaSlow=iMA(NULL, PERIOD_M5, 80, 0, MODE_SMMA, PRICE_CLOSE, 1);
imaFast2=iMA(NULL, PERIOD_M5, 20, 0, MODE_SMMA, PRICE_CLOSE, 2);
imaSlow2=iMA(NULL, PERIOD_M5, 80, 0, MODE_SMMA, PRICE_CLOSE, 2);
*/
   M1_M3=false;
   M2_M4=false;
   if (imaFast>imaSlow && imaFast2<=imaSlow2) M2_M4=true;//{buySig=true; sellSig=false;} //buySig=true; //
   if (imaFast<imaSlow && imaFast2>=imaSlow2) M1_M3=true;//{sellSig=true; buySig=false;} //sellSig=true;//
   //if (M1_M3==true) {
   //buyOK=false;
   sellPrice=NormalizeDouble(yesterday_high,Digits);
   SellTakeProfit=NormalizeDouble(P,Digits);//sellPrice-20*Point;
   SellStopLoss=NormalizeDouble(yesterday_high+16*Point,Digits);
   //SellTakeProfit=NormalizeDouble(H3-150*Point,Digits);
   buyPrice=NormalizeDouble(yesterday_low+2*Point,Digits);
   BuyTakeProfit=NormalizeDouble(P,Digits);//buyPrice+20*Point;
   BuyStopLoss=NormalizeDouble(yesterday_low-16*Point,Digits);
   //BuyTakeProfit=NormalizeDouble(M2+60*Point,Digits);
   //}
/*
if (M2_M4==true) {
//sellOK=false;
buyPrice=NormalizeDouble(L3,Digits);
BuyTakeProfit=NormalizeDouble(M4,Digits);//buyPrice+20*Point;
//BuyTakeProfit=NormalizeDouble(L3+150*Point,Digits);//buyPrice+20*Point;
//BuyTakeProfit=NormalizeDouble(M2+60*Point,Digits);
sellPrice=NormalizeDouble(H3,Digits);
SellTakeProfit=NormalizeDouble(M1,Digits);//sellPrice-20*Point;//
}
*/
/*
buyPrice=NormalizeDouble(L3,Digits);
BuyTakeProfit=NormalizeDouble(L3+70*Point,Digits);
//BuyTakeProfit=NormalizeDouble(M2+60*Point,Digits);
sellPrice=NormalizeDouble(H3,Digits);
SellTakeProfit=NormalizeDouble(H3-70*Point,Digits);
//SellTakeProfit=NormalizeDouble(M3-60*Point,Digits);
*/
   //Place Orders
   //if (closeBuys==false) {
   //if (Hour()<21) {
   //buyStop=false;
   if (buyStop)  // && imaBuy==true)//(OpenBuys==0 && buyOK==true && buySig==true)// && OpenBuys == 0)
     {
      //if (Ask<buyPrice) ticket=OrderSend(Symbol(),OP_BUYLIMIT,Lots,buyPrice,3,buyPrice-StopLoss*Point,BuyTakeProfit,"M2 Buy Limit",buyMagicNum+2,0,CLR_NONE);//White);
      //if (Ask<buyPrice) ticket=OrderSend(Symbol(),OP_BUYSTOP,Lots,NormalizeDouble(P,Digits),3,NormalizeDouble(P,Digits)-StopLoss*Point,NormalizeDouble(M4,Digits),"M2 Buy Stop",buyMagicNum+1,0,CLR_NONE);//White);
      if (Ask<yesterday_low) ticket=OrderSend(Symbol(),OP_BUYSTOP,Lots,buyPrice,Slippage,BuyStopLoss,BuyTakeProfit,"Pivot Buy Stop",buyMagicNum,0,CLR_NONE);//White);
      //ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,Ask+BuyTakeProfit*Point,"TD_Trendline Buy",imaBuyTime,0,White);//firstStartTime,0,White);
      if(ticket>0)
        {
         if (OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
           {
            Print("Pivot Buy order opened : ",OrderOpenPrice());
            buyDay=Day();//buyTime=firstStartTime;
           }
         else
           {
            Print("Error opening Pivot Buy order : ",GetLastError());
            return(0);
           }
        }
     }
   if (buyLimit)// && imaBuy==true)//(OpenBuys==0 && buyOK==true && buySig==true)// && OpenBuys == 0)
     {
      if (Ask>yesterday_low) ticket=OrderSend(Symbol(),OP_BUYLIMIT,Lots,buyPrice,Slippage,BuyStopLoss,BuyTakeProfit,"Pivot Buy Limit",buyMagicNum,0,CLR_NONE);//White);
      //if (Ask<buyPrice) ticket=OrderSend(Symbol(),OP_BUYSTOP,Lots,buyPrice,3,buyPrice-StopLoss*Point,BuyTakeProfit,"M2 Buy Stop",buyMagicNum,0,CLR_NONE);//White);
      //ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*Point,Ask+BuyTakeProfit*Point,"TD_Trendline Buy",imaBuyTime,0,White);//firstStartTime,0,White);
      if(ticket>0)
        {
         if (OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
           {
            Print("Pivot Buy order opened : ",OrderOpenPrice());
            buyDay=Day();//buyTime=firstStartTime;
           }
         else
           {
            Print("Error opening Pivot Buy order : ",GetLastError());
            return(0);
           }
        }

     }
   //} //END if (closeBuys=false) {
   //if (closeSells==false) {
   //sellStop=false;
   if (sellStop)// sellOK==true && imaSell==true)// && sellSig==true)// && OpenSells == 0) 
     {
      //if (Bid<M3) ticket=OrderSend(Symbol(),OP_SELLLIMIT,Lots,sellPrice,3,sellPrice+StopLoss*Point,SellTakeProfit,"M3 Sell Limit",sellMagicNum+2,0,CLR_NONE);//Red);
      //if (Bid>sellPrice) ticket=OrderSend(Symbol(),OP_SELLSTOP,Lots,NormalizeDouble(P,Digits),3,NormalizeDouble(P,Digits)+StopLoss*Point,NormalizeDouble(M1,Digits),"M3 Sell Stop",sellMagicNum+1,0,CLR_NONE);//Red);
      if (Bid>yesterday_high) ticket=OrderSend(Symbol(),OP_SELLSTOP,Lots,sellPrice,Slippage,SellStopLoss,SellTakeProfit,"Pivot Sell Stop",sellMagicNum,0,CLR_NONE);//Red);
      //ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*Point,Bid-SellTakeProfit*Point,"TD_Trendline Sell",imaSellTime,0,White);//firstLowStartTime,0,Red);
      if(ticket>0)
        {
         if (OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
           {
            Print("Pivot Sell order opened : ",OrderOpenPrice());
            sellDay=Day();//sellTime=firstLowStartTime;
           }
         else
           {
            Print("Error opening Pivot Sell order : ",GetLastError());
            return(0);
           }
        }
     }
   if (sellLimit)// sellOK==true && imaSell==true)// && sellSig==true)// && OpenSells == 0) 
     {
      //if (Bid<sellPrice) ticket=OrderSend(Symbol(),OP_SELLLIMIT,Lots,NormalizeDouble(P,Digits),3,NormalizeDouble(P,Digits)+StopLoss*Point,NormalizeDouble(M1,Digits),"M3 Sell Limit",sellMagicNum+2,0,CLR_NONE);//Red);
      if (Bid<yesterday_high) ticket=OrderSend(Symbol(),OP_SELLLIMIT,Lots,sellPrice,Slippage,SellStopLoss,SellTakeProfit,"Pivot Sell Limit",sellMagicNum,0,CLR_NONE);//Red);
      //if (Bid>M3) ticket=OrderSend(Symbol(),OP_SELLSTOP,Lots,sellPrice,3,sellPrice+StopLoss*Point,SellTakeProfit,"M3 Sell Stop",sellMagicNum,0,CLR_NONE);//Red);
      //ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*Point,Bid-SellTakeProfit*Point,"TD_Trendline Sell",imaSellTime,0,White);//firstLowStartTime,0,Red);
      if(ticket>0)
        {
         if (OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
           {
            Print("Pivot Sell order opened : ",OrderOpenPrice());
            sellDay=Day();//sellTime=firstLowStartTime;
           }
         else
           {
            Print("Error opening Pivot Sell order : ",GetLastError());
            return(0);
           }
        }
     }
   //} //END if (closeSells=false) {    
   //} //END if (Hour()<21)
   if (ShowComments==true) Comment(" P = ",P, " M4 = ",M4," yesterday_high = ",yesterday_high, " yesterday_low = ", yesterday_low,
      " yesterday_close = ",yesterday_close, " startPivotHour = ",startPivotHour, " stopPivotHour = ",stopPivotHour,
      "\n","buyStop = ",buyStop, " buyLimit = ",buyLimit,
      "\n","sellStop = ",sellStop, " sellLimit = ",sellLimit, " M1_M3 = ", M1_M3," M2_M4 = ",M2_M4);
/*
for(cnt=0;cnt<OrdersTotal();cnt++)
{
OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
if (OrderSymbol()==Symbol())//check for opened position and symbol
   {
   //if (OrderType()==OP_BUY) OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);
   //if (OrderType()==OP_SELL) OrderClose(OrderTicket(),OrderLots(),Ask,3,Red);
   if (M1_M3==true && OrderType()==OP_BUYSTOP || OrderType()==OP_BUYLIMIT) OrderDelete(OrderTicket());  
   if (M2_M4==true && OrderType()==OP_SELLSTOP || OrderType()==OP_SELLLIMIT) OrderDelete(OrderTicket()); 
   }
}    
*/
   //CLOSE && MODIFY ORDERS
   //int CloseOrdersHour=22;
   //if (DayOfWeek()==5) CloseOrdersHour=21;
   //if (Hour()== CloseOrdersHour) {//18)// && Hour()<=19)  GBPUSD use 22 others 18?
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol()==Symbol())//check for opened position and symbol
        {
         if (OrderType()==OP_BUYSTOP && OrderMagicNumber()!=buyMagicNum) OrderDelete(OrderTicket());
         if (OrderType()==OP_SELLSTOP && OrderMagicNumber()!=sellMagicNum) OrderDelete(OrderTicket());
         if (OrderType()==OP_BUYLIMIT && OrderMagicNumber()!=buyMagicNum) OrderDelete(OrderTicket());
         if (OrderType()==OP_SELLLIMIT && OrderMagicNumber()!=sellMagicNum) OrderDelete(OrderTicket());
         //if (OrderType()==OP_BUY && OrderStopLoss() != BuyStopLoss)
         //OrderModify(OrderTicket(), OrderOpenPrice(), BuyStopLoss, BuyTakeProfit, 0, CLR_NONE);    
         //    OrderModify(OrderTicket(), OrderOpenPrice(), BuyStopLoss, OrderTakeProfit(), 0, CLR_NONE);  
         // if (OrderType()==OP_SELL && OrderStopLoss() != SellStopLoss)
         //OrderModify(OrderTicket(), OrderOpenPrice(), SellStopLoss, SellTakeProfit, 0, CLR_NONE);           
         //   OrderModify(OrderTicket(), OrderOpenPrice(), SellStopLoss, OrderTakeProfit(), 0, CLR_NONE); 
   /*  
   if (OrderType()==OP_BUY) OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);
   if (OrderType()==OP_SELL) OrderClose(OrderTicket(),OrderLots(),Ask,3,Red);
   if (OrderType()==OP_BUYSTOP || OrderType()==OP_BUYLIMIT) OrderDelete(OrderTicket());  
   if (OrderType()==OP_SELLSTOP || OrderType()==OP_SELLLIMIT) OrderDelete(OrderTicket());
   */
        }
     }
   //}    
//----
   return(0);
  }
//+------------------------------------------------------------------+