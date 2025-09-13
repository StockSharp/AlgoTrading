//+------------------------------------------------------------------+ 
//|                         FxDownloader  2006                       | 
//|                         KENZO                                    | 
//|                                                                  | 
//+------------------------------------------------------------------+ 
//---- input parameters 
extern double    Lots=1.0;
extern int       ReInvest=1;
extern int       ReadHistory=1; // Чтение истории (1) или перезапись истории (0) 
extern double    Probab=0.8;    // требуемая вероятность выигрыша 
extern int       dstop=25;      // шаг изменения стопа в пачке стопов 
extern int       Nstop=1;       // число стопов в пачке стопов  НЕ БОЛЬШЕ 3 
extern int       delta=1;       // шаг изменения шага в векторе 
extern int       Nidelt=20;     // число шагов по изменению шага в векторе НЕ БОЛЬШЕ 30 
extern int       NN=10;         // размер вектора  НЕ БОЛЬШЕ 12 
extern double    forg=1.05;     // скорость забывания результатов обучения 
extern bool ReplaceStops=true;
extern double lTrailingStop =510;
extern double sTrailingStop =510;
extern string _Parameters_b_Lots="Параметры модуля расчёта лота";
extern int LotsWayChoice =1;      // Способ выбора рабочего лота: 
                                  //  0-фиксированный, 
                                  //  1-процент от депозита, 
                                  //  2-фракционно-пропорциональный, 
                                  //  3-фракционно-фиксированный, 
extern int LotsPercent    =30;    // Процент от депозита 
extern int LotsDeltaDepo  =500;   // Коэффициент приращения депозита 
extern int LotsDepoForOne =500;   // Размер депозита для одного минилота 
extern int LotsMax        =10000; // Максимальное количество лотов 
//----
int mn,Ncomb,izm,ii,metka,file1,idelt,i,istop,Take0,Stop0,TrailingStop,total,ticket;
int buy_open,sell_open,buy_close,sell_close,ik,nb,ns,iii,ReWriteHistory;
double time0,prob,prob0,Balance0,lotsi,OpenPriceBuy,OpenPriceSell,LastBuyOpen,LastSellOpen;
string FileName="sss";
int delt[30],sr[50,30],sd[5000,30,3],stop[3],nsd[5000,30,3];
double P[12,30],pt[30],cen[5000,30,3],rost[5000,30,3],spad[5000,30,3],LastSd[5000,30,3];
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int init() 
  { //не очень хорошая инициализация. Не рекомендуется часто выключать систему 
     for(idelt=1;idelt<=Nidelt;idelt++)
     {
      delt[idelt]=delta*idelt;
        for(i=1;i<=NN-1;i++){
         P[i,idelt]=Close[i-1];
        }
     }
  return(0);}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit() {return(0);}
//+------------------------------------------------------------------+ 
//| expert start function                                            | 
//+------------------------------------------------------------------+ 
  int start() 
  {
   double MidLot;
   //  Защита от бесплатного распространения в Интернет 
   //  if(iii==0){Alert("Демо-версия программы работает только на Тестере");iii=1;} 
   //  if(IsTesting()==false)return(0); 
   buy_open=0;sell_open=0;buy_close=0;sell_close=0;
//----
   if(ReadHistory==0)ReWriteHistory=1;
   if(Bars<205||AccountFreeMargin()<10)return(0);
     for(idelt=1;idelt<=Nidelt;idelt++)
     {
      delt[idelt]=delta*idelt;
      //формирование векторов цен с шагом delta*idelt 
        if(MathAbs(Ask-P[1,idelt])>(delt[idelt]-0.5)*Point) 
        {
         for(i=1;i<=NN-1;i++){ P[NN+1-i,idelt]=P[NN-i,idelt];  
         }
         P[1,idelt]=Ask;
        }
      //при изменении вектора проводим обработку новой информации 
        if(MathAbs(P[1,idelt]-pt[idelt])>0.5*Point)
        {
         //строим вектор парных сравнений 
           for(i=1;i<=NN-1;i++)
           {
            if(P[i,idelt]>P[i+1,idelt]){sr[i,idelt]=1;} else {sr[i,idelt]=0;}
           }
         //вычисляем номер текущей комбинации 
         Ncomb=0;  mn=1;
         for(i=1;i<=NN-1;i++){ Ncomb=Ncomb+mn*sr[i,idelt]; mn=2*mn;    }
         //открываем фиктивную позицию с меткой комбинации, если такая позиция еще не открыта 
         //открытие проводим по всей пачке стопов c порогом по частоте сделок в каждом канале 
           for(istop=1;istop<=Nstop;istop++)
           {
            if(sd[Ncomb,idelt,istop]==0 && CurTime()-LastSd[Ncomb,idelt,istop]>2*Period()*60)
              {
               sd[Ncomb,idelt,istop]=1; cen[Ncomb,idelt,istop]=Ask;
               LastSd[Ncomb,idelt,istop]=CurTime();
              }
           }
         //проверяем все прежние фиктивные позиции на предмет закрытия 
           for(istop=1;istop<=Nstop;istop++){ stop[istop]=dstop*istop;
              for(i=0;i<=mn-1;i++)
              {
                 if(sd[i,idelt,istop]==1)
                 {
                  if(Ask-cen[i,idelt,istop]>stop[istop]*Point)
                    {
                     rost[i,idelt,istop]=rost[i,idelt,istop]/forg+1;
                     spad[i,idelt,istop]=spad[i,idelt,istop]/forg;
                     sd[i,idelt,istop]=0;
                     nsd[i,idelt,istop]=nsd[i,idelt,istop]+1;
                    }
                  if(cen[i,idelt,istop]-Ask>stop[istop]*Point)
                    {
                     spad[i,idelt,istop]=spad[i,idelt,istop]/forg+1;
                     rost[i,idelt,istop]=rost[i,idelt,istop]/forg;
                     sd[i,idelt,istop]=0;
                     nsd[i,idelt,istop]=nsd[i,idelt,istop]+1;
                    }
                 }
              }
           }
         //даем приказ на открытие реальной позиции, если статистика текущей комбинации благоприятна 
           for(istop=1;istop<=Nstop;istop++)
           {
            stop[istop]=dstop*istop;
            prob=rost[Ncomb,idelt,istop]/(rost[Ncomb,idelt,istop]+spad[Ncomb,idelt,istop]+0.0001);
            if(prob>Probab && nsd[Ncomb,idelt,istop]>10 && CurTime()-LastBuyOpen>2*Period()*60)
              {
               Take0=stop[istop]; Stop0=stop[istop];
               buy_open=1;
                 if(OrdersTotal()>0)
                 {//закрытие сильным сигналом 
                    for(i=0;i<OrdersTotal();i++)
                    {
                     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
                       if (OrderSymbol()==Symbol()) 
                       {  
                       if(OrderType()==OP_SELL)
                         {
                           FileName="FDlast_sell"+Symbol()+Period();
                           file1=FileOpen(FileName,FILE_CSV|FILE_READ);
                           prob0=FileReadNumber(file1); if(prob>(prob0+0.05))sell_close=1;
                           FileClose(file1);
                          }
                       }
                    }
                 }
               FileName="FDlast_buy"+Symbol()+Period();
               file1=FileOpen(FileName,FILE_CSV|FILE_WRITE);
               FileWrite(file1,prob); FileClose(file1);
              }
            //фиксация прибыли слабым сигналом 
              if(OrdersTotal()>0)
              {
                 for(i=0; i<OrdersTotal();i++)
                 {
                  OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
                    if (OrderSymbol()==Symbol()) 
                    {
                     if(OrderType()==OP_SELL)
                       {
                        if(prob>0.6 && nsd[Ncomb,idelt,istop]>10 &&
                          (OrderOpenPrice()-Ask)>(dstop/2)*Point) sell_close=1;
                       }
                    }
                 }
              }
            prob=spad[Ncomb,idelt,istop]/(rost[Ncomb,idelt,istop]+spad[Ncomb,idelt,istop]+0.0001);
            if(prob>Probab && nsd[Ncomb,idelt,istop]>10 && CurTime()-LastSellOpen>2*Period()*60)
              {
               Take0=stop[istop]; Stop0=stop[istop];
               sell_open=1;
                 if(OrdersTotal()>0)
                 {
                    for(i=0;i<OrdersTotal();i++)
                    {
                     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
                       if (OrderSymbol()==Symbol()) 
                       {
                        if(OrderType()==OP_BUY)
                         {
                           FileName="FDlast_buy"+Symbol()+Period();
                           file1=FileOpen(FileName,FILE_CSV|FILE_READ);
                           prob0=FileReadNumber(file1); if(prob>(prob0+0.05))buy_close=1;
                           FileClose(file1);
                         }
                      }
                   }
                }
               FileName="FDlast_sell"+Symbol()+Period();
               file1=FileOpen(FileName,FILE_CSV|FILE_WRITE);
               FileWrite(file1,prob); FileClose(file1);
              }
            //фиксация прибыли слабым сигналом 
              if(OrdersTotal()>0)
              {
                 for(i=0;i<OrdersTotal();i++)
                 {
                  OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
                    if (OrderSymbol()==Symbol()) 
                    {
                     if(OrderType()==OP_BUY)
                      {
                        if(prob>0.6 && nsd[Ncomb,idelt,istop]>10 &&
                        (Bid-OrderOpenPrice())>(dstop/2)*Point)buy_close=1;
                      }
                   }
                }
             }
          }
          izm=izm+1;
        }
          pt[idelt]=P[1,idelt];
     }
   //Считывание информации из файла 
     if(ii==0)
     {
      FileName="FD_"+Symbol(); ii=1;
      file1=FileOpen(FileName,FILE_CSV|FILE_READ);
      metka=FileReadNumber(file1);time0=FileReadNumber(file1);
        if(metka==1 && ReadHistory==1 && ReWriteHistory!=1)
        {
         //*/                   for(idelt=1;idelt<=Nidelt;idelt++){for(i=1;i<=NN;i++){ 
         //*/                   P[i,idelt]=FileReadNumber(file1);   }    }      
           for(istop=1;istop<=Nstop;istop++)
           {
              for(idelt=1;idelt<=Nidelt;idelt++)
              {
                 for(i=0;i<=mn-1;i++)
                 {
                  rost[i,idelt,istop]=FileReadNumber(file1);
                  spad[i,idelt,istop]=FileReadNumber(file1);
                  nsd[i,idelt,istop]=FileReadNumber(file1);
                 }
              }
           }
        }
      FileClose(file1);
     }
   //Запись информации в файл через 100 изменений вектора 
   //Возможность накрутки статистики при частом тестировании исключена 
     if(izm>10 && (CurTime()>=time0 || ReWriteHistory==1))
     {
      izm=0; FileName="FD_"+Symbol();
      file1=FileOpen(FileName,FILE_CSV|FILE_WRITE);
      FileWrite(file1,"1");FileWrite(file1,CurTime());
      //*/          for(idelt=1;idelt<=Nidelt;idelt++){for(i=1;i<=NN;i++){ 
      //*/          FileWrite(file1,P[i,idelt]);   }    }            
        for(istop=1;istop<=Nstop;istop++)
        {
           for(idelt=1;idelt<=Nidelt;idelt++)
           {
              for(i=0;i<=mn-1;i++)
              {
               FileWrite(file1,rost[i,idelt,istop]);
               FileWrite(file1,spad[i,idelt,istop]);
               FileWrite(file1,nsd[i,idelt,istop]);
              }
           }
        }
      FileClose(file1);
     }
   //БЛОК ИСПОЛНЕНИЯ ПРИКАЗОВ 
   if(ik==0){Balance0=AccountBalance();ik=1;}
   if(ReInvest==1)lotsi=Lots*AccountBalance()/Balance0; else lotsi=Lots;
   if(lotsi<0.1){lotsi=0.1;}
     if(lotsi>=0.1 && lotsi<0.2)lotsi=0.1; else 
     {
      if(lotsi>=0.2 && lotsi<0.5)lotsi=0.2; else 
     {
           if(lotsi>=0.5 && lotsi<1)lotsi=0.5; else 
           {
            if(lotsi>=1 && lotsi<2)lotsi=1; else 
           {
                 if(lotsi>=2 && lotsi<3)lotsi=2; else 
                 {
                  if(lotsi>=3 && lotsi<4)lotsi=3; else 
                 {
                       if(lotsi>=4 && lotsi<5)lotsi=4; else 
                       {
                        if(lotsi>=5 && lotsi<6)lotsi=5; else 
                       {
                             if(lotsi>=6 && lotsi<7)lotsi=6; else 
                             {
                              if(lotsi>=7 && lotsi<8)lotsi=7; else 
                             {
                                   if(lotsi>=8 && lotsi<9)lotsi=8; else 
                                   {
                                    if(lotsi>=9 && lotsi<15)lotsi=9; else 
                                   {
                                         if(lotsi>=15 && lotsi<20)lotsi=15; else 
                                         {
                                          if(lotsi>=20 && lotsi<25)lotsi=20; else 
                                         {
                                               if(lotsi>=25 && lotsi<30)lotsi=25; else 
                                               {
                                                if(lotsi>=30 && lotsi<35)lotsi=30; else 
                                               {
                                                     if(lotsi>=35 && lotsi<40)lotsi=35; else 
                                                     {
                                                      if(lotsi>=40 && lotsi<45)lotsi=40; else 
                                                     {
                                                           if(lotsi>=45 && lotsi<50)lotsi=45; else 
                                                           {
                                                            if(lotsi>=50 && lotsi<55)lotsi=50; else 
                                                           {
                                                                 if(lotsi>=55 && lotsi<60)lotsi=55; else 
                                                                 {
                                                                  if(lotsi>=60 && lotsi<65)lotsi=60; else 
                                                                 {
                                                                       if(lotsi>=65 && lotsi<70)lotsi=65; else 
                                                                       {
                                                                        if(lotsi>=70 && lotsi<75)lotsi=70; else 
                                                                       {
                                                                             if(lotsi>=75 && lotsi<80)lotsi=75; else 
                                                                             {
                                                                              if(lotsi>=80 && lotsi<85)lotsi=80; else 
                                                                             {
                                                                                   if(lotsi>=85 && lotsi<90)lotsi=85; else 
                                                                                   {
                                                                                      if(lotsi>=90 && lotsi<95)lotsi=90; else 
                                                                                        {
                                                                                         if(lotsi>=95 && lotsi<100)lotsi=95; else 
                                                                                        {
                                                                                            if(lotsi>=100)lotsi=lotsi;/*100*/
                                                                                        }
                                                                                     }
                                                                                  }
                                                                               }
                                                                            }
                                                                         }
                                                                      }
                                                                   }
                                                                }
                                                             }
                                                          }
                                                       }
                                                    }
                                                 }
                                              }
                                           }
                                        }
                                     }
                                  }
                               }
                            }
                         }
                      }
                   }
                }
             }
          }
       }
    }
   MidLot=GetSizeLot();
   lotsi=NormalizeDouble(MidLot,1);
   total=OrdersTotal();
   //исполняем приказы на открытие    
     if(total==0) 
     {
        if(buy_open==1 && MathAbs(OpenPriceBuy-Ask)>10*Point)
        {
         Print("Trying to open BUY price: ",Ask," Stop ",Ask-Stop0*Point," TP ",Point,Ask+(Take0*Point));
         ticket=OrderSend(Symbol(),OP_BUY,lotsi,Ask,3,Ask-Stop0*Point,Ask+Take0*Point,"AT",16384,0,Blue);
         return(0);
        }
        if(sell_open==1 && MathAbs(OpenPriceSell-Bid)>10*Point)
        {
         Print("Trying to open SELL price: ",Bid," Stop ",Bid+Stop0*Point," TP ",Bid-Take0*Point);
         ticket=OrderSend(Symbol(),OP_SELL,lotsi,Bid,3,Bid+Stop0*Point,Bid-Take0*Point,"AT",16384,0,Red);
         return(0);
        }
     }
     if(total==1) 
     {
        OrderSelect(0, SELECT_BY_POS, MODE_TRADES); if (OrderSymbol()==Symbol()) 
        {
           if(OrderType()==OP_SELL)
           {
              if(buy_open==1 && MathAbs(OpenPriceSell-Bid)>10*Point)
              {
               ticket=OrderSend(Symbol(),OP_BUY,lotsi,Ask,3,Ask-Stop0*Point,Ask+Take0*Point,"AT",16384,0,Orange);
               return(0);
              }
           }
           if(OrderType()==OP_BUY)
           {
              if(sell_open==1 && MathAbs(OpenPriceBuy-Ask)>10*Point)
              {
               ticket=OrderSend(Symbol(),OP_SELL,lotsi,Bid,3,Bid+Stop0*Point,Bid-Take0*Point,"AT",16384,0,Red);
               return(0);
              }
           }
        }
     }
   //смещение стопов при повторных приказах на открытие позиции того же типа 
     if(total>0)
     {
        for(i=0;i<total;i++) 
        {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES); if (OrderSymbol()==Symbol()) 
           {
              if(OrderType()==OP_BUY && buy_open==1 && Bid-OrderStopLoss()>Stop0*Point)
              {
               OrderModify(OrderTicket(),OrderOpenPrice(),
               Bid-Stop0*Point,Bid+Take0*Point,0,Orange);  return(0);
              }
              if(OrderType()==OP_SELL && sell_open==1 && OrderStopLoss()-Ask>Stop0*Point)
              {
               OrderModify(OrderTicket(),OrderOpenPrice(),
              Ask+Stop0*Point*TrailingStop,Ask-Take0*Point,0,Red); return(0);
              }
           }
        }
     }
   //снимаем приказы на открытие, если нужные позиции уже открыты 
     if(total>0)
     {
        for(i=0;i<total;i++) 
        {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES); if (OrderSymbol()==Symbol()) 
           {
              if(OrderType()==OP_BUY){buy_open=0;   LastBuyOpen=CurTime();
              OpenPriceBuy=OrderOpenPrice();
              }
              if(OrderType()==OP_SELL){sell_open=0; LastSellOpen=CurTime();
              OpenPriceSell=OrderOpenPrice();
              }
           }
        }
     }
   //исполняем приказы на закрытие    
     if(total>0)
     {
        for(i=0;i<total;i++) 
        {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES); if (OrderSymbol()==Symbol()) 
           {
              if(buy_close==1 && OrderType()==OP_BUY)
              {
              OrderClose(OrderTicket(),OrderLots(),Bid,3,Orange);   return(0);  
              }
              if(sell_close==1 && OrderType()==OP_SELL)
              {
              OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);   return(0);  
              }
           }
        }
     }
   //снимаем приказы на закрытие, если нужные позиции уже закрыты 
   nb=0;ns=0;
     if(total>0)
     {
        for(i=0;i<total;i++) 
        {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES); if (OrderSymbol()==Symbol()) 
           {
            if(OrderType()==OP_BUY)nb=1; if(OrderType()==OP_SELL)ns=1;
           }
        }
     }   if(nb==0)buy_close=0;  if(ns==0)sell_close=0;
   //ДВИЖЕНИЕ СТОПОВ 
     if(OrdersTotal()>0)
     {
        for(i=0;i<OrdersTotal();i++) 
        {
           OrderSelect(i, SELECT_BY_POS, MODE_TRADES); if (OrderSymbol()==Symbol()) 
           {
              if(OrderType()==OP_BUY)
              {
                 if(TrailingStop>10)
                 {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                     OrderModify(OrderTicket(),OrderOpenPrice(),
                     Bid-Point*TrailingStop,Bid+Point*TrailingStop,0,Orange);
                 }
              }
              if(OrderType()==OP_SELL)
              {
                 if(TrailingStop>10)
                 {
                  if(OrderStopLoss()>Ask+Point*TrailingStop)
                     OrderModify(OrderTicket(),OrderOpenPrice(),
                    Ask+Point*TrailingStop,Ask-Point*TrailingStop,0,Red);
                 }
              }
           }
        }
     }
//----
  return(0); 
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  double GetSizeLot() 
  {
   double dLot;
   if (LotsWayChoice==0) dLot=lotsi;
   // фиксированный процент от депозита 
     if (LotsWayChoice==1) 
     {
      dLot=MathCeil(AccountFreeMargin()/10000*LotsPercent)/10;
     }
   // фракционно-пропорциональный 
     if (LotsWayChoice==2) 
     {
      int k=LotsDepoForOne;
        for(double i=2; i<=LotsMax; i++) 
        {
         k=k+i*LotsDeltaDepo;
           if (k>AccountFreeMargin()) 
           {
            dLot=(i-1)/10; break;
           }
        }
     }
   // фракционно-фиксированный 
     if (LotsWayChoice==3) 
     {
      dLot=MathCeil((AccountFreeMargin()-LotsDepoForOne)/LotsDeltaDepo)/10;
     }
   if (dLot<0.1)  dLot=0.1;
   if (dLot>LotsMax) dLot=LotsMax;
//----
   return(dLot);
  }
//+------------------------------------------------------------------+