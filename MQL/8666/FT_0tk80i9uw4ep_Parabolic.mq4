//+---------------------------------------------------------------------------0+|
//|                                                        Скальп Parabolic.mq4 |
//|                            Разработано для 39 выпуска журнала  FORTRADER.RU |
//|                                                                             |
//| Описание стратегии:         http://www.forexsystems.ru/showthread.php?t=5495|
//| Описание некоторых функций: http://fxnow.ru/blog/programming_mql4/          |
//| Контакты:                   yuriy@fortrader.ru                              |
//+----------------------------------------------------------------------------+|
//|4-150
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

//контроль времени открытия. 
extern int timecontrol=0; //1 - включено, 0 - выключено.
extern int starttime = 7; 
extern int stoptime = 17; 

//размер в пунктах после которого стоп переносим в безубыток
extern int BBUSize=0;
//размер трейлинга 
extern int TrailingStop=0;
//Начальный обьем сделки
extern int TrailingShag=5;
//настройки быстрого параболика
extern double stepfast=0.02; 
extern double maximumfast=0.2;
//настройки медленного параболика 
extern double stepslow=0.005; 
extern double maximumslow=0.05;
//количество баров для поиска минимумов максимумов
extern int barsearch=3;
//отступ от минимума/максимума для рассчета стоплоссов
extern int otstup=100;
//уровень входа по фибоначчи
extern double Ur1=50;
//уровень профита по фибоначчи
extern double Ur2=161;


int nummodb,nummods;
int bars;
int err1;
int start()
  { //if(err1<0){Print("start() завершил работу с ошибкой");return(-1);}
    if(bars!=Bars && ((timecontrol(starttime,stoptime)!=1 && timecontrol==1) ||timecontrol==0))
    {bars=Bars;
     err1=ScalpParabolicPattern();
     }
 
 if(TrailingStop>0){err1=TrailingStop();}
 if(BBUSize>0){err1=BBU();}

   return(0);
  }
  

//+------------------------------------------------------------------+
int sarmax,sarmin,countbars,countbarv;
int ScalpParabolicPattern()
   {int err;double op,sl,tp,max,min,Ur50,Ur116;
   //загрузим параболики
   double sarslow=iSAR(NULL,0,stepslow ,maximumslow,1);
   double fastsar=iSAR(NULL,0,stepfast,maximumfast,1);
   double fastsarlast=iSAR(NULL,0,stepfast,maximumfast,1);



   if(fastsar>Bid && sarslow<Bid){sarmax=1;}
   if(sarslow>Bid){sarmax=0;}

   if(fastsar<Bid && sarslow>Bid){sarmin=1;}
   if(sarslow<Bid){sarmin=0;}


   
   if(sarslow<Bid && fastsar<Bid && sarmax==1 )//если медленный параболик направлен вверх и произошел пробой параболика который двигался вниз
   {sarmax=0;
    min=MaximumMinimum(0,barsearch);   //найдем текущий минимум
    max=High[1];
   
    Ur50 =GetFiboUr(max,min,Ur1/100);//получим 50% значение фибо
    Ur116 =GetFiboUr(max,min,Ur2/100); //получим 116% значение фибо
 
   op=Ur50;  sl=min-otstup*Point;  tp=Ur116;
    if((Ask-op)<5*Point || (Ask-sl)<5*Point || (tp-Ask)<5*Point){return(0);}
   err=OrderSend(Symbol(),OP_BUYLIMIT,0.1,NormalizeDouble(op,Digits),3,sl,tp,"FORTRADER.RU",0,0,Red);
   if(err<0){Print("ScalpParabolicPattern()-  Ошибка установки отложенных ордеров OP_BUYLIMIT.  op "+op+" sl "+sl+" tp "+tp+" "+GetLastError());return(-1);}
   nummodb=0;
   }
   
   if(sarslow>Bid && fastsar>Bid && sarmin==1 )//если медленный параболик направлен вверх и произошел пробой параболика который двигался вниз
   {sarmin=0;
    max=MaximumMinimum(1,barsearch);   //найдем текущий минимум
    min=Low[1];
   
    Ur50 =GetFiboUr(min,max,Ur1/100);//получим 50% значение фибо
    Ur116 =GetFiboUr(min,max,Ur2/100); //получим 116% значение фибо
 
   op=Ur50;  sl=max+otstup*Point;  tp=Ur116;
   if((op-Ask)<5*Point || (sl-Ask)<5*Point || (Ask-tp)<5*Point){return(0);}
   err=OrderSend(Symbol(),OP_SELLLIMIT,0.1,NormalizeDouble(op,Digits),3,sl,tp,"FORTRADER.RU",0,0,Red);
   if(err<0){Print("ScalpParabolicPattern()-  Ошибка установки отложенных ордеров OP_SELLLIMIT.  op "+op+" sl "+sl+" tp "+tp+" "+GetLastError());return(-1);}
   nummods=0;
   
   }
   
   
   if(fastsar>Bid && ChLimitOrder(1)>0)
   {
   err=deletelimitorder(1);
   }
 
   if(fastsar<Bid && ChLimitOrder(0)>0)
   {
   err=deletelimitorder(0);
   }
 
return(0);
}

int deletelimitorder(int type)
{int i;int err;
  for(  i=1; i<=OrdersTotal(); i++)         
   {
      if(OrderSelect(i-1,SELECT_BY_POS)==true) 
       {                                   
         if(OrderType()==OP_BUYLIMIT && OrderSymbol()==Symbol() && type==1)
         { 
         err=OrderDelete(OrderTicket());
         }
          if(OrderType()==OP_SELLLIMIT && OrderSymbol()==Symbol() && type==0)
         { 
          err=OrderDelete(OrderTicket());
         }
       }
    }
return(err);
}

int ChLimitOrder(int type)
{int i;
    for(  i=1; i<=OrdersTotal(); i++)         
   {
      if(OrderSelect(i-1,SELECT_BY_POS)==true) 
       {                                   
         if(OrderType()==OP_BUYLIMIT && OrderSymbol()==Symbol() && type==1)
         { 
          return(1);
         }
         if(OrderType()==OP_SELLLIMIT && OrderSymbol()==Symbol() && type==0)
         { 
          return(1);
         }
       }
    }
return(0);
}


double MaximumMinimum(int type,int barsearch)
{//описание функции http://fxnow.ru/blog/programming_mql4/3.html
 int x=0,stop=0;double minmax;
  
  if(type==0)
   {
   while(stop==0)
    {
    minmax =Low[iLowest(NULL,0,MODE_LOW,barsearch,x)];
    if(minmax>Low[iLowest(NULL,0,MODE_LOW,barsearch,x+barsearch)])
     {
      minmax =Low[iLowest(NULL,0,MODE_LOW,barsearch,x+barsearch)];
      x=x+barsearch;
     }
     else {stop=1;return(minmax);}
    }//while(stop
   }//if(type
   
   if(type==1)
   {
   while(stop==0)
    {
    minmax =High[iHighest(NULL,0,MODE_HIGH,barsearch,x)];
    if(minmax<High[iHighest(NULL,0,MODE_HIGH,barsearch,x+barsearch)])
      {
       minmax =High[iHighest(NULL,0,MODE_HIGH,barsearch,x+barsearch)];
       x=x+barsearch;
       }
       else{stop=1;return(minmax);}
     }// while(sto
   }//if(type
return(0);          
}

double GetFiboUr(double high, double low, double ur)
{//описание функции http://fxnow.ru/blog/programming_mql4/4.html
  int digits = MarketInfo(Symbol(),MODE_DIGITS);                             
  double Fibo = NormalizeDouble(low + (high - low)*ur, digits); return(Fibo);
return(0); 
}


int  TrailingStop()
{//описание функции http://fxnow.ru/blog/programming_mql4/1.html
int i;bool err;double lastbid;

   for( i=1; i<=OrdersTotal(); i++)         
   {
      if(OrderSelect(i-1,SELECT_BY_POS)==true) 
       {   
        if(TrailingStop>0 && OrderType()==OP_BUY && OrderSymbol()==Symbol())  
        {                
         if(Bid-OrderOpenPrice()>=TrailingStop*Point && TrailingStop>0 && (Bid-Point*TrailingStop)>OrderStopLoss())
          {
           if(((Bid-Point*TrailingStop)-OrderStopLoss())>=TrailingShag*Point)
           {
            Print("ТРЕЙЛИМ");
            err=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
            if(err==false){return(-1);}
           }//if(Bid>=OrderStopLoss()
          }//if(Bid-OrderOpenPrice()
         }//if(BBUSize>0
        }//if(OrderSelect(i
            
       if(OrderSelect(i-1,SELECT_BY_POS)==true) 
       {
        if(OrderType()==OP_SELL && OrderSymbol()==Symbol() )  
        {        
         if(OrderOpenPrice()-Ask>=TrailingStop*Point && TrailingStop>0 && OrderStopLoss()>(Ask+TrailingStop*Point))
          {
           if((OrderStopLoss()-(Ask+TrailingStop*Point))>TrailingShag*Point)
           {
            Print("ТРЕЙЛИМ");
            err=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TrailingStop*Point,OrderTakeProfit(),0,Green);
            if(err==false){return(-1);}
           }//if(Ask<=OrderStopLoss()
          }//if(OrderOpenPrice()
         }//if(BBUSize>0 
       }// if(OrderSelect
    }// for( i=1;
return(0);
}
int  BBU()
{//описание функции http://fxnow.ru/blog/programming_mql4/2.html
int i;bool err;
   for( i=1; i<=OrdersTotal(); i++)         
   {
      if(OrderSelect(i-1,SELECT_BY_POS)==true) 
       {   
        if(BBUSize>0 && OrderType()==OP_BUY && OrderSymbol()==Symbol() && OrderStopLoss()<OrderOpenPrice())  
        {                
         if(Bid-OrderOpenPrice()>=BBUSize*Point && BBUSize>0 )
          {
           Print("ПОРА В БезуБыток");
           err=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+1*Point,OrderTakeProfit(),0,Green);
           if(err==false){return(-1);}
          }//if(Bid-OrderOpenPrice()
         }//if(BBUSize>0
        }//if(OrderSelect(i
            
       if(OrderSelect(i-1,SELECT_BY_POS)==true) 
       {
        if(BBUSize>0 && OrderType()==OP_SELL && OrderSymbol()==Symbol() && (OrderStopLoss()>OrderOpenPrice() || OrderStopLoss()==0))  
        {        
         if(OrderOpenPrice()-Ask>=BBUSize*Point && BBUSize>0)
          {
           Print("ПОРА В БезуБыток");
           err=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-1*Point,OrderTakeProfit(),0,Green);
           if(err==false){return(-1);}
          }//if(OrderOpenPrice()
         }//if(BBUSize>0 
       }// if(OrderSelect
    }// for( i=1;
return(0);
}

int timecontrol(int starttime, int stoptime)
{//описание функции http://fxnow.ru/blog/programming_mql4/5.html
   if (Hour()>=starttime &&  Hour()<=stoptime) 
      { 
      return(0);
      }  
return(1);
}

