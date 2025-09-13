//+------------------------------------------------------------------+
//|                                               Bull vs Medved.mq4 |
//|                       Copyright © 2008, Andrey Kuzmenko (Foxbat) |
//|                                          mailto:foxbat-b@mail.ru |
//+------------------------------------------------------------------+
//| Некоторые "общеполезные" функции кода, трейлинг стоп, например,  |
//| были любезно позаимствованы из других экспертов.                 |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Andrey Kuzmenko (Foxbat)"
#property link      "foxbat-b@mail.ru"
//----
#define MAGIC 612453
//----
extern double Lots = 0.10;       // размер лота
extern double CandleSize =75;    // размер тела свечи
extern double k_sl=0.8;          // Уровень StopLoss определяется как CandleSize*k_sl
extern double k_tp=0.8;          // Уровень TakeProfit определяется как CandleSize*k_tp
extern double popravka_up=16;    // отступ от текущей цены для отложенного ордера Buylimit
extern double popravka_down=20;  // отступ от текущей цены для отложенного ордера Selllimit
extern bool flag = true;
extern string        StartTime="0:05";// Время старта по гринвичу
extern string        StartTime1="4:05";
extern string        StartTime2="8:05";
extern string        StartTime3="12:05";
extern string        StartTime4="16:05";
extern string        StartTime5="20:05";
datetime             TimeStart;
datetime             TimeStart1;
datetime             TimeStart2;
datetime             TimeStart3;
datetime             TimeStart4;
datetime             TimeStart5;
bool                 trade=false;
bool                 trade1=false;
//----
double Limit = 400;
color clOpenBuy = Blue;
color clCloseBuy = Aqua;
color clOpenSell = Red;
color clCloseSell = Violet;
color clModiBuy = Blue;
color clModiSell = Red;
string Name_Expert = "Bull vs Medved";
int Slippage = 0;
bool UseSound = True;
string NameFileSound = "alert.wav";
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsBull()
  {
   if
   ((Close[3]>Open[2])&&
    (Close[2]-Open[2] >= 10*Point)&&
    (Close[1]-Open[1] >= CandleSize*Point) )
       return(true);
   else 
       return(false);
  }
  
bool IsBadBull()
  {
   if
   ((Close[3]-Open[3] >= 10*Point)&&
    (Close[2]-Open[2] >= 10*Point)&&
    (Close[1]-Open[1] >= CandleSize*Point) )
       return(true);
   else 
       return(false);
  }
bool IsCoolBull()
  {
   if
   ((Open[2]-Close[2] >= 20*Point)&&
    (Close[2]<=Open[1])&&
    (Close[1]>Open[2])&&
    (Close[1]-Open[1] >= 0.4*CandleSize*Point) )
       return(true);
   else 
       return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsBear()
  {
   if((Open[1] - Close[1] >= CandleSize*Point)) 
       return(true);
   else 
       return(false);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
{
   

// Переводим время из строчной величины StartTime во временнУю TimeStart
   TimeStart=StrToTime(StartTime);
   TimeStart1=StrToTime(StartTime1);
   TimeStart2=StrToTime(StartTime2);
   TimeStart3=StrToTime(StartTime3);
   TimeStart4=StrToTime(StartTime4);
   TimeStart5=StrToTime(StartTime5);
   // Если текущее время меньше стартового или больше его на 5 минут, то выходим и ничего не делаем.
   // Но предварительно делаем переменную trade ложной. Просто сбрасываем информацию о том, что уже открывались.
   if(CurTime()<TimeStart || CurTime()>TimeStart+300 &&
      CurTime()<TimeStart1 || CurTime()>TimeStart1+300 &&
      CurTime()<TimeStart2 || CurTime()>TimeStart2+300 &&
      CurTime()<TimeStart3|| CurTime()>TimeStart3+300 &&
      CurTime()<TimeStart4|| CurTime()>TimeStart4+300 && 
      CurTime()<TimeStart5 || CurTime()>TimeStart5+300 )
    { trade=false; return(0); }
 
   if(trade) return(0);
   
//Проверим нет ли у нас отложенных ордеров, которые  висят и бездействуют 4 часа. Если таковые есть удалим их.   
   {
    int total =OrdersTotal();
    int ticket=OrderTicket();
   {      
   for(int i=0;i<=total;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS))
       {if(OrderSymbol()==Symbol())
          {if(OrderType()>1)
             {if(TimeCurrent()-OrderOpenTime()>230*60)
                    {   
       
                    ticket=OrderTicket();    
                    OrderDelete(OrderTicket());
                    }
             }       
          }          
       }
    break;                
     }
   }
 
  {
  
   if(AccountFreeMargin() < (1000*Lots))
     {
       Print("We have no money. Free Margin = ", AccountFreeMargin());
       return(0);
     }
   if(!ExistPositions())
     {
       
       if(IsBull() == true && IsBadBull() == false)
         {
           OpenBuy();
           return(0);
         }
       
     }
     
   if(!ExistPositions())
     {
       
       if(IsCoolBull() == true )
         {
           OpenBuy();
           return(0);
         }
       
     }  
     
   if(!ExistPositions())
     {
       
       if(IsBear() == true)
         {   
           OpenSell();
           return(0);
         }
       
     }
   
   return (0);
  }
 
}

 }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool ExistPositions() 
  {
	  for(int i = 0; i < OrdersTotal(); i++) 
	    {
		     if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) 
		       {
			        if(OrderSymbol() == Symbol() && OrderMagicNumber() == MAGIC) 
			          {
				           return(True);
			          }
		       } 
	    } 
	  return(false);
  } 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+   
  
void OpenBuy()
{
  { 
   double ldLot, ldStop, ldTake; 
   string lsComm; 
   int i = 1;
   int orders = HistoryTotal(); 
   ldLot = Lots;
   ldStop = GetStopLossBuy(); 
   ldTake = GetTakeProfitBuy(); 
   lsComm = GetCommentForOrder();
   OrderSend(Symbol(), OP_BUYLIMIT, ldLot, Ask-popravka_up*Point, Slippage,ldStop, ldTake, lsComm, MAGIC,0, clOpenBuy); 
   if(UseSound) 
       PlaySound(NameFileSound);
  } 
 } 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpenSell() 
  { 
   double ldLot, ldStop, ldTake; 
   string lsComm;
   int i = 1;
   int orders = HistoryTotal(); 
   ldLot = Lots;
   ldStop = GetStopLossSell(); 
   ldTake = GetTakeProfitSell(); 
   lsComm = GetCommentForOrder();
   OrderSend(Symbol(), OP_SELLLIMIT, ldLot, Bid+popravka_down*Point, Slippage, ldStop, ldTake, lsComm, MAGIC,0, clOpenSell); 
   if(UseSound) 
       PlaySound(NameFileSound);        
  } 

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string GetCommentForOrder() 
  { 	
    return(Name_Expert); 
  } 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetStopLossBuy() 
  {
  int sl;
  sl=MathRound((Close[1]-Open[1])*10000*k_sl);  
  return(Bid - sl*Point);
  } 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetStopLossSell() 
  { 
  int sl;
  sl=MathRound((Open[1]-Close[1])*10000*k_sl);
  	
  return(Ask + sl*Point); 
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitBuy() 
  { 
  int tp;
   tp=MathRound((Close[1]-Open[1])*10000*k_tp);
    return(Ask + tp*Point); 
  } 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double GetTakeProfitSell() 
  { 
   int tp;
   tp=MathRound((Open[1]-Close[1])*10000*k_tp);  	
   return(Bid - tp*Point); 
  } 
//----
return(0);
//+------------------------------------------------------------------+



