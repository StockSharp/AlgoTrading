//+------------------------------------------------------------------+
//|                                               FT_TIME_BIGDOG.mq4 |
//|                            FORTRADER.RU, Юрий, ftyuriy@gmail.com |
//|               http://www.fortrader.ru, Время + каналы + трейлинг |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU, Юрий, ftyuriy@gmail.com"
#property link      "http://FORTRADER.RU, TIME"
/*Разработано для 52 выпуска журнала FORTRADER.Ru. Система по стратегии большая собака. 
Обсуждение: http://forexsystems.ru/torgovaia-strategiia-big-dog-t8929.html?t=8929
Архив журнала: http://www.fortrader.ru/arhiv.php
52 выпуск: http://www.fortrader.ru/
*/

extern int starttime = 14; 
extern int stoptime  = 16; 
extern int maxpoint  = 50; 

extern int TP=50;

extern int mn=1;

extern double Lots=0.1;

extern int orderlimit=20;

int start()
  {
   OpenPattern();
   return(0);
  }

int day,buy,sell,stop;  
int OpenPattern()
{double op,sl,tp;int err;


   //Если новый день то удаляем ордера.
   if(day!=DayOfWeek()){_DeleteOrder(1);_DeleteOrder(0);}
   
   double high= GetHighLow_Time(starttime,stoptime,0);
   double low = GetHighLow_Time(starttime,stoptime,1);
   
   if(OrdersTotal()<1){buy=0;sell=0;stop=0;}
   
   if(MathAbs(high-low)<maxpoint*Point*mn && StrToInteger(TimeToStr(Time[1],TIME_MINUTES))>=stoptime && stop==0)
   {stop=1;
      if(sell==0 && (high-Ask)>orderlimit*Point*mn)
      {day=DayOfWeek();sell=1;
      op=high;  sl=low;tp=high+TP*Point*mn;
      err=OrderSend(Symbol(),OP_BUYSTOP,Lots,NormalizeDouble(op,Digits),3,sl,tp,"FORTRADER.RU",0,0,Red);
      if(err<0){Print("FT_TIME_BIGDOG()-  Ошибка установки отложенных ордеров OP_SELLLIMIT.  op "+op+" sl "+sl+" tp "+tp+" "+GetLastError());return(-1);}
      }
      
      if(buy==0 && (Bid-low)>orderlimit*Point*mn)
      {day=DayOfWeek();buy=1;
      op=low;  sl=high;  tp=low-TP*Point*mn;
      err=OrderSend(Symbol(),OP_SELLSTOP,Lots,NormalizeDouble(op,Digits),3,sl,tp,"FORTRADER.RU",0,0,Red);
      if(err<0){Print("FT_TIME_BIGDOG()-  Ошибка установки отложенных ордеров OP_BUYLIMIT.  op "+op+" sl "+sl+" tp "+tp+" "+GetLastError());return(-1);}
      }
   }
return(0);
}

//удаляет отложенные стоп ордера
int _DeleteOrder(int type)
{
   for( int i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                                       
     if(OrderType()==OP_SELLSTOP && OrderSymbol()==Symbol() && type==0)
     {
      OrderDelete(OrderTicket()); 
     }//if
  
    if(OrderType()==OP_BUYSTOP && OrderSymbol()==Symbol() && type==1)
     {
      OrderDelete(OrderTicket()); 
     }//if
    }//if
   }
   return(0);
}  

double GetHighLow_Time(int starttime,int stoptime, int type)
{double ret;int i;

if(type==0)
{
   if(StrToInteger(TimeToStr(Time[1],TIME_MINUTES))>=stoptime)
   {
      for( i=0;i<200;i++)
      {
         if(StrToInteger(TimeToStr(iTime(Symbol(),Period(),i),TIME_MINUTES))<=stoptime)
         {
            if(High[i]>ret)
            {ret=High[i];}
            if(StrToInteger(TimeToStr(iTime(Symbol(),Period(),i),TIME_MINUTES))<=starttime)
            {return(ret);}
         }
      }    
   }
}

if(type==1)
{
   if(StrToInteger(TimeToStr(Time[1],TIME_MINUTES))>=stoptime)
   {
      for( i=0;i<200;i++)
      {
         if(StrToInteger(TimeToStr(iTime(Symbol(),Period(),i),TIME_MINUTES))<=stoptime)
         {
            if(Low[i]<ret || ret==0){ret=Low[i];}
            if(StrToInteger(TimeToStr(iTime(Symbol(),Period(),i),TIME_MINUTES))<=starttime){return(ret);}
         }
      }    
   }
}

return(ret);
}