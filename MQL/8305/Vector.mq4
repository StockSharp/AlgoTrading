//+------------------------------------------------------------------+
//|                                                    Векторный.mq4 |
//|                               Copyright ©BFE 2006 Software Corp. |
//|                                                BFE2006@yandex.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright ©BFE 2006 Software Corp."
#property link      "BFE2006@yandex.ru"

extern int     MAGIC=0;
extern double LotsPercent=1;       //процент для рассчета лота
extern double PrcProfit=0.5;      //процент для рассчета профита
extern double PrcLose=30;          //прцент фиксации убытков
extern double PeriodMA=PERIOD_M15;  //период МА
double        Lot;
double        Free,Balans,MaxL,MinL;
double        SB_openEU,SB_stopEU,SB_openGU,SB_stopGU,SB_openUC,SB_stopUC,SB_openUJ,SB_stopUJ;
double        L,H,sr,Pips_Profit,PR,ST;
double        ML,PNT,x,NP,Price,l;
double        m1,m2,m3,m4,m5,m6,m7,m8,m9;
string        SMB,EU,GU,UC,UJ,OBCH_TREND;
bool          fm;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
 //------------ Проверяем вкл. советника 
  if(!IsExpertEnabled())//если ложь 
   {Alert("Ошибка! Не нажата кнопка *Советники*");}
   else 
   {Comment("Как только цена изменится, Советник начнёт работу.");}  
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
//---------- Запоминаем основные параметры
SMB=Symbol();                           //Символ валютной пары  
Balans=AccountBalance();                //Баланс
Free=AccountEquity();                   //Свободные средства
MaxL=MarketInfo(SMB,MODE_MAXLOT)/10;    //Максимальный допустимый размер лота c учетом увелечения
MinL=MarketInfo(SMB,MODE_MINLOT);       //Минимальный возможный лот
//==========   
//---------- Считаем открытые и отложенные ордера
for(int i=OrdersTotal(); i>=0; i--)
  {
  if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
    {
    if(OrderSymbol()=="EURUSD")
      {
      if(OrderType()==OP_BUY     || OrderType()==OP_SELL)    {SB_openEU++;}
      }
    if(OrderSymbol()=="GBPUSD")
      {
      if(OrderType()==OP_BUY     || OrderType()==OP_SELL)    {SB_openGU++;}
      }
    if(OrderSymbol()=="USDCHF")
      {
      if(OrderType()==OP_BUY     || OrderType()==OP_SELL)    {SB_openUC++;}
      }
    if(OrderSymbol()=="USDJPY")
      {
      if(OrderType()==OP_BUY     || OrderType()==OP_SELL)    {SB_openUJ++;}
      }
    }
  }
 
//========== 
//---------- Рассчитываем дополнительные параметры
m4=iMA("EURUSD",PeriodMA,3,8,MODE_SMMA,PRICE_MEDIAN,0);
m5=iMA("GBPUSD",PeriodMA,3,8,MODE_SMMA,PRICE_MEDIAN,0);
m6=iMA("USDCHF",PeriodMA,3,8,MODE_SMMA,PRICE_MEDIAN,0);
m1=iMA("USDJPY",PeriodMA,3,8,MODE_SMMA,PRICE_MEDIAN,0);

m7=iMA("EURUSD",PeriodMA,7,8,MODE_SMMA,PRICE_MEDIAN,0);
m8=iMA("GBPUSD",PeriodMA,7,8,MODE_SMMA,PRICE_MEDIAN,0);
m9=iMA("USDCHF",PeriodMA,7,8,MODE_SMMA,PRICE_MEDIAN,0);
m2=iMA("USDJPY",PeriodMA,7,8,MODE_SMMA,PRICE_MEDIAN,0);

if(m4-m7>0){EU="BUY";}if(m4-m7<0){EU="SELL";}
if(m5-m8>0){GU="BUY";}if(m5-m8<0){GU="SELL";}
if(m6-m9>0){UC="BUY";}if(m6-m9<0){UC="SELL";}
if(m1-m2>0){UJ="BUY";}if(m6-m9<0){UJ="SELL";}
if((m4+m5+m6+m1)-(m7+m8+m9+m2)>0){OBCH_TREND="BUY";}
if((m4+m5+m6+m1)-(m7+m8+m9+m2)<0){OBCH_TREND="SELL";}
for(int s=1;s<=50;s++)
  {
  L=L+iLow(SMB,PERIOD_H4,1);  //сумма минимальных цен баров
  H=H+iHigh(SMB,PERIOD_H4,1); //сумма максимальных цен баров
  }
//--- Рассчет среднего разброса цен с учетом валютных пар   
if(SMB=="EURUSD" || SMB=="GBPUSD" || SMB=="USDCHF" || SMB=="AUDUSD"
|| SMB=="USDCAD" || SMB=="EURCHF" || SMB=="NZDUSD")                 {sr=((H-L)/s)*10000;}
if(SMB=="USDJPY" || SMB=="EURJPY")                                  {sr=((H-L)/s)*100;}
sr=(NormalizeDouble((sr),0));
Pips_Profit=sr/5;  Pips_Profit=(NormalizeDouble((Pips_Profit),0));
if(Pips_Profit>13)  {Pips_Profit=13;}                 //размер пипсовки

//==========
//---------- Принимаем решение в зависимости от колличества ордеров
if(SB_openEU==0){Open_Oder1();}  
if(SB_openGU==0){Open_Oder2();}
if(SB_openUC==0){Open_Oder3();}
if(SB_openUJ==0){Open_Oder4();}

if(SB_openEU==1){Pips();}
if(SB_openGU==1){Pips();}
if(SB_openUC==1){Pips();}
if(SB_openUJ==1){Pips();}

Comment("Кол-во ордеров: ",SB_openEU+SB_openGU+SB_openUC+SB_openUJ,
"\n","Пипсовка: ",Pips_Profit,"\n",
"EURUSD: ",EU,"\n","GBPUSD: ",GU,"\n","USDCHF: ",UC,"\n","USDJPY: ",UJ,"\n",
"Общий тренд для USD: ",OBCH_TREND,"\n","Глобальный профит: ",PR,"\n",
"Глобальный стоп: ",ST);

SB_openEU=0;SB_stopEU=0;SB_openGU=0;SB_stopGU=0;SB_openUC=0;SB_stopUC=0;SB_openUJ=0;SB_stopUJ=0;
sr=0; H=0;L=0;
return(0);
}

//---------- Открываем первый ордер
void Open_Oder1()
{
//--- Расчитываем лот
Lot = (AccountFreeMargin() / 10000*LotsPercent/4) / 10;
Lot=(NormalizeDouble((Lot),2));
   if(Lot<MinL){Lot=MinL;} 
   if(Lot>MaxL){Lot=MaxL;}

  if((m4+m5+m6+m1)-(m7+m8+m9+m2)>0)                                                             //BUY
  {
  if(m4>m7 && SB_openEU==0){fm=OrderSend("EURUSD",OP_BUY,Lot,Ask,3,0,0,NULL,MAGIC,0,MediumBlue);}
  }
  if((m4+m5+m6+m1)-(m7+m8+m9+m2)<0)                                                             //SELL
  {
  if(m4<m7 && SB_openEU==0){fm=OrderSend("EURUSD",OP_SELL,Lot,Bid,3,0,0,NULL,MAGIC,0,Gold);}
  }
}
//========== 
void Open_Oder2()
{
//--- Расчитываем лот
Lot = (AccountFreeMargin() / 10000*LotsPercent/4) / 10;
Lot=(NormalizeDouble((Lot),2));
   if(Lot<MinL){Lot=MinL;} 
   if(Lot>MaxL){Lot=MaxL;}
   
    if((m4+m5+m6+m1)-(m7+m8+m9+m2)>0)
    {   
    if(m5>m8 && SB_openGU==0){fm=OrderSend("GBPUSD",OP_BUY,Lot,Ask,3,0,0,NULL,MAGIC,0,MediumBlue);}
    }
    if((m4+m5+m6+m1)-(m7+m8+m9+m2)<0)                                                             //SELL
    {
    if(m5<m8 && SB_openGU==0){fm=OrderSend("GBPUSD",OP_SELL,Lot,Bid,3,0,0,NULL,MAGIC,0,Gold);}
    }
}
//========== 
void Open_Oder3()
{
//--- Расчитываем лот
Lot = (AccountFreeMargin() / 10000*LotsPercent/4) / 10;
Lot=(NormalizeDouble((Lot),2));
   if(Lot<MinL){Lot=MinL;} 
   if(Lot>MaxL){Lot=MaxL;}
   
    if((m4+m5+m6+m1)-(m7+m8+m9+m2)>0)
    {
    if(m6>m9 && SB_openUC==0){fm=OrderSend("USDCHF",OP_BUY,Lot,Ask,3,0,0,NULL,MAGIC,0,MediumBlue);} 
    }  
    if((m4+m5+m6+m1)-(m7+m8+m9+m2)<0)                                                             //SELL
    {
    if(m6<m9 && SB_openUC==0){fm=OrderSend("USDCHF",OP_SELL,Lot,Bid,3,0,0,NULL,MAGIC,0,Gold);}
    }
}
//==========
void Open_Oder4()
{
//--- Расчитываем лот
Lot = (AccountFreeMargin() / 10000*LotsPercent/4) / 10;
Lot=(NormalizeDouble((Lot),2));
   if(Lot<MinL){Lot=MinL;} 
   if(Lot>MaxL){Lot=MaxL;}
   
    if((m4+m5+m6+m1)-(m7+m8+m9+m2)>0)
    {
    if(m1>m2 && SB_openUJ==0){fm=OrderSend("USDJPY",OP_BUY,Lot,Ask,3,0,0,NULL,MAGIC,0,MediumBlue);} 
    }  
    if((m4+m5+m6+m1)-(m7+m8+m9+m2)<0)                                                             //SELL
    {
    if(m1<m2 && SB_openUJ==0){fm=OrderSend("USDJPY",OP_SELL,Lot,Bid,3,0,0,NULL,MAGIC,0,Gold);}
    }
}
//---------- Если один рыночный ордер - пипсуем
void Pips()
{
for(int e=OrdersTotal();e>=0;e--)
  {
  if (OrderSelect(e,SELECT_BY_POS,MODE_TRADES)==true)
    {
    if(OrderSymbol()=="EURUSD")
      {
      if(OrderType()==OP_BUY && (OrderOpenPrice()+Pips_Profit*Point)<Bid)
      {OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);}
      if(OrderType()==OP_SELL && (OrderOpenPrice()-Pips_Profit*Point)>Ask)
      {OrderClose(OrderTicket(),OrderLots(),Ask,3,Red);}
      }
    if(OrderSymbol()=="GBPUSD")
      {
      if(OrderType()==OP_BUY && (OrderOpenPrice()+Pips_Profit*Point)<Bid)
      {OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);}
      if(OrderType()==OP_SELL && (OrderOpenPrice()-Pips_Profit*Point)>Ask)
      {OrderClose(OrderTicket(),OrderLots(),Ask,3,Red);}
      }
    if(OrderSymbol()=="USDCHF")
      {
      if(OrderType()==OP_BUY && (OrderOpenPrice()+Pips_Profit*Point)<Bid)
      {OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);}
      if(OrderType()==OP_SELL && (OrderOpenPrice()-Pips_Profit*Point)>Ask)
      {OrderClose(OrderTicket(),OrderLots(),Ask,3,Red);}
      }
    if(OrderSymbol()=="USDJPY")
      {
      if(OrderType()==OP_BUY && (OrderOpenPrice()+Pips_Profit*Point)<Bid)
      {OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);}
      if(OrderType()==OP_SELL && (OrderOpenPrice()-Pips_Profit*Point)>Ask)
      {OrderClose(OrderTicket(),OrderLots(),Ask,3,Red);}
      }
    }
  }
  {//--- контроль прибыли
  PR=AccountBalance()+(Balans/100*PrcProfit);
  PR=(NormalizeDouble((PR),2));
  if ((Free-Balans)>=(Balans/100*PrcProfit))
    {
    for (int b=OrdersTotal();b>=0; b--)
      {
      if (OrderSelect(b,SELECT_BY_POS,MODE_TRADES)==true)
        {
        if(OrderType()==OP_BUY) {OrderClose(OrderTicket(),OrderLots(),Bid,3);}
        if(OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),Ask,3);}
        }
      }
    }//--- контроль убытков 
  ST=AccountBalance()-(Balans/100*PrcLose);
  ST=(NormalizeDouble((ST),2));
  if((Free-Balans)<=(-(Balans/100*PrcLose)))
    {
    for (int bd=OrdersTotal();bd>=0; bd--)
      {
      if (OrderSelect(bd,SELECT_BY_POS,MODE_TRADES)==true)
        {
        if(OrderType()==OP_BUY) {OrderClose(OrderTicket(),OrderLots(),Bid,3);}
        if(OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),Ask,3);}
        }
      }
    }     
 } 
//==========
}

//+------------------------------------------------------------------+