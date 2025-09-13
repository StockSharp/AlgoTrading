//+---------------------------------------------------------+
//|              FitFul_13.mq4                              |
//|              Copyright © 2006, AS-Bisiness Corp.        |
//|              Artapov Alexandr, Sidorina Yulia           |
//+---------------------------------------------------------+
#property copyright "Copyright © 2006, AS-Bisiness Corp."
//----
extern double Y=15.5;
extern double TrailingStop=30;
extern int Period_1=PERIOD_H1;
extern int Period_3=PERIOD_M15;
extern int CloseTimeMin_1 =172800;
extern int CloseHour    =21;
extern int CloseMim_1   =50;
extern int CloseMim_2   =59;
extern int OpenMim_0   =0;
extern int OpenMim_1   =15;
extern int OpenMim_2   =30;
extern int OpenMim_3   =45;
//----
double HW,LW,CW;
double OP,R1,R2,R3,S1,S2,S3,S05,S15,S25,R05,R15,R25;
int _GetLastError=0;
//+------------------------------------------------------------------+
double MarginCalculate(string symbol,double volume)
  {
   string first   =StringSubstr(Symbol(),0,3);
   string second  =StringSubstr(Symbol(),3,3);
   string currency=AccountCurrency();
   double leverage=AccountLeverage();
   double contract=MarketInfo(Symbol(),MODE_LOTSIZE);
   double bid     =MarketInfo(Symbol(),MODE_BID);
//---- допускаем только стандартные форексные символы XXXYYY
   if(StringLen(Symbol())!=6)
     {
      Print("MarginCalculate: '",first+second,"' must be standard forex symbol XXXYYY");
      return(0.0);
     }
//---- проверка наличия данных
   if(bid<=0 || contract<=0)
     {
      Print("MarginCalculate: no market information for '",first+second,"'");
      return(0.0);
     }
//---- проверяем самые простые варианты - без кроссов
   if(first==currency)   return(contract*volume/leverage);           // USDxxx
   if(second==currency)  return(contract*bid*volume/leverage);       // xxxUSD
//---- проверяем обычные кроссы, ищем прямое преобразование через валюту депозита
   string base=currency+first;                                       // USDxxx
   if(MarketInfo(base,MODE_BID)>0) return(contract/MarketInfo(base,MODE_BID)*volume/leverage);
//---- попробуем наоборот
   base=first+currency;                                              // xxxUSD
   if(MarketInfo(base,MODE_BID)>0) return(contract*MarketInfo(base,MODE_BID)*volume/leverage);
//---- нет возможности прямого перерасчета
   Print("MarginCalculate: can not convert '",first+second,"'");
   return(0.0);
  }
//--- extern variables
extern double ExtMaxRisk=0.05;
//--- calculate current volume
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CalcVol()
  {
   double lot_min =MarketInfo(Symbol(),MODE_MINLOT);//0.1;   
   double lot_max =MarketInfo(Symbol(),MODE_MAXLOT);//5.0;   
   double lot_step=MarketInfo(Symbol(),MODE_LOTSTEP);//0.1;   
   double vol;
//----
   if(lot_min<0 || lot_max<=0.0 || lot_step<=0.0)
     {
      Print("CalculateVolume: invalid MarketInfo() results [",lot_min,",",lot_max,",",lot_step,"]");
      return(0);
     }
   if(AccountLeverage()<=0)
     {
      Print("CalculateVolume: invalid AccountLeverage() [",AccountLeverage(),"]");
      return(0);
     }
//--- basic formula
   vol=NormalizeDouble(AccountFreeMargin()*ExtMaxRisk/AccountLeverage()/10.0,2);
//--- additional calculation
   int    orders=HistoryTotal();
   int    losses=0;
   if(AccountBalance()>0)
     {
      for(int m=orders-1;m>=0;m--)
        {
         if(OrderSelect(m,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) vol=NormalizeDouble(vol-lot_step,1);
     }
//--- 
   vol=NormalizeDouble(vol/lot_step,0)*lot_step;
   if(vol<lot_min) vol=lot_min;
   if(vol>lot_max) vol=lot_max;
//---
   return(vol);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
 int start()
  {
   double bid    =MarketInfo(Symbol(),MODE_BID);
   double ask    =MarketInfo(Symbol(),MODE_ASK);
   double point  =MarketInfo(Symbol(),MODE_POINT);
   int digits    =MarketInfo(Symbol(),MODE_DIGITS);
   double slip   =MarketInfo(Symbol(),MODE_STOPLEVEL);
   //========Вычисление недельных уровней============
   double HW=iHigh(Symbol(),PERIOD_W1,1);
   double LW=iLow(Symbol(),PERIOD_W1,1);
   double CW=iClose(Symbol(),PERIOD_W1,1);
   //----
   double OP= NormalizeDouble((HW+LW+CW)/3,digits);
   double R1= NormalizeDouble(2*OP-LW,digits);
   double S1= NormalizeDouble(2*OP-HW,digits);
   double R05=NormalizeDouble((OP+R1)/2,digits);
   double S05=NormalizeDouble((OP+S1)/2,digits);
   double R2= NormalizeDouble(OP+(HW-LW),digits);
   double S2= NormalizeDouble(OP-(HW-LW),digits);
   double R15=NormalizeDouble((R1+R2)/2,digits);
   double S15=NormalizeDouble((S1+S2)/2,digits);
   double R3= NormalizeDouble(2*OP+(HW-2*LW),digits);
   double S3= NormalizeDouble(2*OP-(2*HW-LW),digits);
   double R25=NormalizeDouble((R2+R3)/2,digits);
   double S25=NormalizeDouble((S2+S3)/2,digits);
   int total=OrdersTotal(),ticket,i=0,a=0,flag=0,k=0;
//----
   if(total<1)
     {
      if(Bars<100 || IsTradeAllowed()==false)
        {Print("Недостаточно Баров - = ", BarsPerWindow());
         return;
        }
      if (AccountFreeMargin()<MarginCalculate(Symbol(),1.0))
        {
         Print("Работать нельзя -НЕТ ДЕНЕГ- = ", MarginCalculate(Symbol(),1.0));
         return;
        }
     }
   RefreshRates();
   //======Определение переменных необходимого бара=======
   double O1_b1=iOpen(Symbol(),Period_1,1);
   double C1_b1=iClose(Symbol(),Period_1,1);
   double O2_b1=iOpen(Symbol(),Period_1,2);
   double C2_b1=iClose(Symbol(),Period_1,2);
   double O1_b3=iOpen(Symbol(),Period_3,1);
   double C1_b3=iClose(Symbol(),Period_3,1);
   double L2_b3=iLow(Symbol(),Period_3,2);
   double C2_b3=iClose(Symbol(),Period_3,2);
   double L3_b3=iLow(Symbol(),Period_3,3);
   double C3_b3=iClose(Symbol(),Period_3,3);
   double H2_b3=iHigh(Symbol(),Period_3,2);
   double H3_b3=iHigh(Symbol(),Period_3,3);
   //=================================        
   if (total>0)
      while(a<=total)
        {   if (OrderSelect(a, SELECT_BY_POS)==true)
           {  if (OrderSymbol()==Symbol()) k++;
            if (k==3) {flag=1; break;}
           }
         a++;
        }
   //================================================
   if(flag==0)
     {
      if((Minute()== OpenMim_0)||(Minute()== OpenMim_1)||(Minute()== OpenMim_2)||(Minute()== OpenMim_3))
        {
         //11111111111111111111111111111111111111
         if (O1_b1<=C1_b1)
           {
            if(O2_b1<=OP && C2_b1>=OP)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S1-Y*point,digits),
               NormalizeDouble(R1+Y*point,digits),"0B",100,0,Lime);
               Print( "MagicNum=",OrderMagicNumber(), " OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1<=R05 && C2_b1>=R05)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S05-Y*point,digits),
               NormalizeDouble(R15+Y*point,digits),"1B",101,0,Lime);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1<=R1 && C2_b1>=R1)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(OP-Y*point,digits),
               NormalizeDouble(R2+Y*point,digits),"2B",102,0,Lime);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1<=R15 && C2_b1>=R15)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(R05-Y*point,digits),
               NormalizeDouble(R25+Y*point,digits),"3B",103,0,Lime);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1<=R2 && C2_b1>=R2)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(R1-Y*point,digits),
               NormalizeDouble(R3+Y*point,digits),"4B",104,0,Lime);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1<=R25 && C2_b1>=R25)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(R15-Y*point,digits),
               NormalizeDouble(R3+Y*point,digits),"5B",105,0,Lime);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1<=S1 && C2_b1>=S1)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S2-Y*point,digits),
               NormalizeDouble(OP+Y*point,digits),"7B",107,0,Lime);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1<=S05 && C2_b1>=S05)
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S15-Y*point,digits),
               NormalizeDouble(R05+Y*point,digits),"8B",108,0,Lime);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ",GetLastError() );return(-1);
              return(0);
              }
           }
         // - ПРОБОЙ (ВВЕРХУ СНИЗ)!!! 
         if (O1_b1>=C1_b1)
           {
            if(O2_b1>=OP && C2_b1<=OP)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R1+Y*point,digits),
               NormalizeDouble(S1-Y*point,digits),"0S",200,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1>=S05 && C2_b1<=S05)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R05+Y*point,digits),
               NormalizeDouble(S15-Y*point,digits),"1S",201,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1>=S1 && C2_b1<=S1)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(OP+Y*point,digits),
               NormalizeDouble(S2-Y*point,digits),"2S",202,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1>=S15 && C2_b1<=S15)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(S05+Y*point,digits),
               NormalizeDouble(S25-Y*point,digits),"3S",203,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1>=S2 && C2_b1<=S2)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(S1+Y*point,digits),
               NormalizeDouble(S3-Y*point,digits),"4S",204,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1>=S25 && C2_b1<=S25)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(S15+Y*point,digits),
               NormalizeDouble(S3-Y*point,digits),"5S",205,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1>=R1 && C2_b1<=R1)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R2+Y*point,digits),
               NormalizeDouble(OP-Y*point,digits),"7S",207,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if(O2_b1>=R05 && C2_b1<=R05)
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R15+Y*point,digits),
               NormalizeDouble(S05-Y*point,digits),"8S",208,0,Red);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
           }
         // - ОТБОЙ (СВЕРХУ ВНИЗ И ВВЕРХ)!!! 

         if (O1_b3<=C1_b3)
           {
            if((L3_b3<=OP && C3_b3>=OP)&& (L2_b3<=OP && C2_b3>=OP))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S1-Y*point,digits),
               NormalizeDouble(R1+Y*point,digits),"8B",300,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((L3_b3<=R05 && C3_b3>=R05)&& (L2_b3<=R05 && C2_b3>=R05))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S05-Y*point,digits),
               NormalizeDouble(R15+Y*point,digits),"9B",301,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((L3_b3<=R1 && C3_b3>=R1)&& (L2_b3<=R1 && C2_b3>=R1))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(OP-Y*point,digits),
               NormalizeDouble(R2+Y*point,digits),"10B",302,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((L3_b3<=R15 && C3_b3>=R15)&& (L2_b3<=R15 && C2_b3>=R15))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(R05-Y*point,digits),
               NormalizeDouble(R25+Y*point,digits),"11B",303,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((L3_b3<=R2 && C3_b3>=R2)&& (L2_b3<=R2 && C2_b3>=R2))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(R1-Y*point,digits),
               NormalizeDouble(R3+Y*point,digits),"12B",304,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((L3_b3<=R25 && C3_b3>=R25)&& (L2_b3<=R25 && C2_b3>=R25))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(R15-Y*point,digits),
               NormalizeDouble(R3+Y*point,digits),"13B",305,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((L3_b3<=S1 && C3_b3>=S1)&& (L2_b3<=S1 && C2_b3>=S1))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S2-Y*point,digits),
               NormalizeDouble(OP+Y*point,digits),"15B",307,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((L3_b3<=S05 && C3_b3>=S05)&& (L2_b3<=S05 && C2_b3>=S05))
              {
               ticket=OrderSend(Symbol(),OP_BUY,CalcVol(),ask,slip,NormalizeDouble(S15-Y*point,digits),
               NormalizeDouble(R05+Y*point,digits),"16B",308,0,PaleGreen);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
           }
         // - ОТБОЙ (СНИЗУ ВВЕРХ И ВНИЗ)!!!    
         if (O1_b3>=C1_b3)
           {
            if((H3_b3>=OP && C3_b3<=OP) && (H2_b3>=OP && C2_b3<=OP))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R1+Y*point,digits),
               NormalizeDouble(S1-Y*point,digits),"8S",400,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((H3_b3>=S05 && C3_b3<=S05) && (H2_b3>=S05 && C2_b3<=S05))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R05+Y*point,digits),
               NormalizeDouble(S15-Y*point,digits),"9S",401,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((H3_b3>=S1 && C3_b3<=S1) && (H2_b3>=S1 && C2_b3<=S1))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(OP+Y*point,digits),
               NormalizeDouble(S2-Y*point,digits),"10S",402,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((H3_b3>=S15 && C3_b3<=S15) && (H2_b3>=S15 && C2_b3<=S15))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(S05+Y*point,digits),
               NormalizeDouble(S25-Y*point,digits),"11S",403,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((H3_b3>=S2 && C3_b3<=S2) && (H2_b3>=S2 && C2_b3<=S2))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(S1+Y*point,digits),
               NormalizeDouble(S3-Y*point,digits),"12S",404,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((H3_b3>=S25 && C3_b3<=S25) && (H2_b3>=S25 && C2_b3<=S25))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),ask,slip,NormalizeDouble(S15+Y*point,digits),
               NormalizeDouble(S3-Y*point,digits),"13S",405,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((H3_b3>=R1 && C3_b3<=R1) && (H2_b3>=R1 && C2_b3<=R1))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R2+Y*point,digits),
               NormalizeDouble(OP-Y*point,digits),"15S",407,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
            if((H3_b3>=R05 && C3_b3<=R05) && (H2_b3>=R05 && C2_b3<=R05))
              {
               ticket=OrderSend(Symbol(),OP_SELL,CalcVol(),bid,slip,NormalizeDouble(R15+Y*point,digits),
               NormalizeDouble(S05-Y*point,digits),"16S",408,0,Salmon);
               Print( "MagicNum=",OrderMagicNumber()," OrderSend № ", GetLastError());return(-1);
              return(0);
              }
           }
        }
     }
//-------------------------------------------------------------
   for(i=0;i<=total;i++)
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL && OrderSymbol()==Symbol()&& IsTradeAllowed()==true)
        {
         if(OrderType()==OP_BUY)
           {
            if(CurTime()-OrderOpenTime() >CloseTimeMin_1 &&  OrderProfit( )>=0)
              {
               OrderClose(OrderTicket(),OrderLots(),bid,slip,Violet);
               Sleep(10000);continue;
              }
            if (DayOfWeek()== 5 && (Hour()==CloseHour && Minute()>=CloseMim_1 && Minute()<=CloseMim_2))
              {
               OrderClose(OrderTicket(),OrderLots(),ask,slip,Violet);
               Sleep(10000);continue;
              }
            if(TrailingStop>0)
              {
               if(bid-OrderOpenPrice()>point*TrailingStop)
                 {
                  if(OrderStopLoss()<bid-point*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss()+TrailingStop*point,OrderTakeProfit(),0,Moccasin);
                     Sleep(10000);return(0);
                    }
                 }
              }
           }
         else
           {
            if(OrderType()==OP_SELL)
              {
               if(CurTime()-OrderOpenTime() >CloseTimeMin_1 &&  OrderProfit( )>=0)
                 {
                  OrderClose(OrderTicket(),OrderLots(),ask,slip,Violet);
                  Sleep(10000);continue;
                 }
               if (DayOfWeek()== 5 && (Hour()==CloseHour && Minute()>=CloseMim_1 && Minute()<=CloseMim_2))
                 {
                  OrderClose(OrderTicket(),OrderLots(),ask,slip,Violet);
                  Sleep(10000);continue;
                 }
               if((OrderOpenPrice()-ask)>(point*TrailingStop))
                 {
                  if((OrderStopLoss()>(ask+point*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss()-TrailingStop*point,OrderTakeProfit(),0,Moccasin);
                     Sleep(10000);return(0);
                    }
                 }
              }
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+