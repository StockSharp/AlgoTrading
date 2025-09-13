//+------------------------------------------------------------------+
//|                                                  DT-RSI-EXP1.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "klot"
#property link      "klot@mail.ru"
#define MAGICRSI  54321
//---- input parameters
extern int       PeriodRSI=47;
extern int       TimeFrameRSI=15;
//------------------------------
extern double TakeProfit=76;
extern double StopLoss=26;
extern double Lots=0.1;
extern double TrailingStop=0;
//----
double BufferRSIUP[500];
double BufferRSIDW[500];
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
//---- Контроль открытых позиций -----
   int ticket,cnt;
   bool sell=false; bool buy=false;
   if (OrdersTotal()>0)
     {
      for(cnt=0;cnt<OrdersTotal();cnt++)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderType()==OP_SELL &&   // check for opened position 
            OrderSymbol()==Symbol())  // check for symbol  
            sell=true;
         if(OrderType()==OP_BUY &&    // check for opened position 
            OrderSymbol()==Symbol())  // check for symbol  
            buy=true;
        }
     }
//----
   double rsi1,rsi2,rsi3,vol1,vol2,vol3,vol4;
   int i,k,pos1,pos2,pos3,pos4;
   for(i=0; i<500; i++)
     {
      rsi1=iRSI(NULL,TimeFrameRSI,PeriodRSI,PRICE_CLOSE,i+1);
      rsi2=iRSI(NULL,TimeFrameRSI,PeriodRSI,PRICE_CLOSE,i+2);
      rsi3=iRSI(NULL,TimeFrameRSI,PeriodRSI,PRICE_CLOSE,i+3);
//----
      if  (rsi1<rsi2 && rsi2>=rsi3) BufferRSIUP[i+2]=rsi2; // Верхушки. RSI поворачивает вниз
      if  (rsi1>rsi2 && rsi2<=rsi3) BufferRSIDW[i+2]=rsi2; // Донышки. RSI поворачивает вверх
     }
//------ Поиск верхушек ------
   k=0;
   for(i=0; i<500; i++)
     {
      if (BufferRSIUP[i]>40 && k==0) { vol1=BufferRSIUP[i]; pos1=i; k++;}
      if (BufferRSIUP[i]>60 && BufferRSIUP[i]>vol1 && k!=0) { vol2=BufferRSIUP[i]; pos2=i; k++;}
      if (k>1) break;
     }
//----- Поиск минимумов ниже 40
   // Если между верхушками есть минимум ниже 40, то линию тренда не строим
   for(i=0; i<pos2; i++)
     {
      if(BufferRSIDW[i]!=0 && BufferRSIDW[i]<40) {vol1=0; vol2=0;}
     }
//----- Поиск донышек ------
   k=0;
   for(i=0; i<500; i++)
     {
      if (BufferRSIDW[i]<60 && BufferRSIDW[i]!=0 && k==0) { vol3=BufferRSIDW[i]; pos3=i; k++;}
      if (BufferRSIDW[i]!=0 && BufferRSIDW[i]<40 && BufferRSIDW[i]<vol3 && k!=0) { vol4=BufferRSIDW[i]; pos4=i; k++;}
      if (k>1) break;
     }
//----- Поиск максимумов выше 60
   // Если между донышками есть максимум больше 60, тогда линию тренда не строим
   for(i=0; i<pos4; i++)
     {
      if(BufferRSIUP[i]!=0 && BufferRSIUP[i]>60) {vol3=0; vol4=0;}
     }
//---------------------------------------------------------------------------------------------------------------+
   // ---- Сигналы для входов Buy и Sell
   double volDW, volDW1, volUP, volUP1;
   bool sellDW=false,buyUP=false;
   double rftl0_240, rsi0;
//----
   rftl0_240=iCustom(NULL,240,"RFTL",0,1);
   rsi0=iRSI(NULL,TimeFrameRSI,PeriodRSI,PRICE_CLOSE,0);
   rsi1=iRSI(NULL,TimeFrameRSI,PeriodRSI,PRICE_CLOSE,1);
   rsi2=iRSI(NULL,TimeFrameRSI,PeriodRSI,PRICE_CLOSE,2);
   rsi3=iRSI(NULL,TimeFrameRSI,PeriodRSI,PRICE_CLOSE,3);
//------------ рассчет продолжения линии тренда для Sell --------------------
   if (vol3!=0 && vol4!=0)
     {
      volDW=vol3+(pos3*(vol3-vol4)/(pos4-pos3));      // Значение линии тренда на текущем баре
      volDW1=vol3+((pos3-1)*(vol3-vol4)/(pos4-pos3)); // Значение линии тренда на прошлом баре
     }
   if (volDW!=0 && rsi1<volDW && rsi2>volDW1 && rftl0_240>Close[1] && // rsi1<rsi2 && rsi2>rsi3 &&
          /* rsi2<60 && */ rsi2>50 && rsi0>47 && pos2>pos4) sellDW=true; else sellDW=false; // Сигнал Sell
//------------ рассчет продолжения линии тренда для Buy --------------------
   if (vol1!=0 && vol2!=0)
     {
      volUP=vol1+(pos1*(vol1-vol2)/(pos2-pos1));      // Значение линии тренда на текущем баре
      volUP1=vol1+((pos1-1)*(vol1-vol2)/(pos2-pos1)); // Значение линии тренда на прошлом баре
     }
   if (volUP!=0 && rsi1>volUP && rsi2<volUP1 && rftl0_240<Close[1] && // rsi1>rsi2 && rsi2<rsi3 &&
          rsi2<50 && rsi0<55 && /* rsi2>40 && */ pos4>pos2) buyUP=true; else buyUP=false; // Сигнал Buy
//------------
   Comment(   " vol1 = ",vol1, "  pos1 = ",pos1,"\n",
              " vol2 = ",vol2, "  pos2 = ",pos2,"\n",
              " vol3 = ",vol3, "  pos3 = ",pos3,"\n",
              " vol4 = ",vol4, "  pos4 = ",pos4,"\n",
              "  VOLUP = ", volUP, "  VOLUP1 = ", volUP1, "\n",
              "  VOLDW = ", volDW, "  VOLDW1 = ", volDW1, "\n",
              "  RSI1 = ",rsi1, "  RSI2 = ",rsi2, "  rftl0_240 = ",rftl0_240);
//----
   for(i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICRSI || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if(rsi0>70)
           {
            OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
            return(0);
           }
         // check for trailing stop
         if(TrailingStop>0)
           {
            if(Bid-OrderOpenPrice()>Point*TrailingStop)
              {
               if(OrderStopLoss()<Bid-Point*TrailingStop)
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
                  return(0);
                 }
              }
           }
        }
      if(OrderType()==OP_SELL)
        {
         if(rsi0<30)
           {
            OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
            return(0);
           }
         // check for trailing stop
         if(TrailingStop>0)
           {
            if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
              {
               if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                 {
                  OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
                  return(0);
                 }
              }
           }
        }
     }
//----
   if (buyUP==true && buy==false)
     {
      ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-StopLoss*Point,Ask+TakeProfit*Point,"DT-RSI",MAGICRSI,0,Aquamarine);
      if(ticket>0)
        {
         if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
        }
      else Print("Error opening BUY order : ",GetLastError());
      return(0);
     }
   if (sellDW==true && sell==false)
     {
      ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+StopLoss*Point,Bid-TakeProfit*Point,"DT-RSI",MAGICRSI,0,Red);
      if(ticket>0)
        {
         if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
        }
      else Print("Error opening BUY order : ",GetLastError());
      return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+

