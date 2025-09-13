//+------------------------------------------------------------------+
//|                                                 TradeChannel.mq4 |
//|                                  Copyright © 2005, George-on-Don |
//|                                       http://www.forex.aaanet.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, George-on-Don"
#property link      "http://www.forex.aaanet.ru"

#include <stdlib.mqh>
#include <stderror.mqh>

#define MAGICMA  20050610

extern double    Lots=0.1;            // Размер лота
extern bool      SndMl=true;          // Флаг для отправки информации на е-майл
extern bool      isFloatLots = true;  // Флаг для расчета величины лота 
extern double    DcF = 3;             // Фактор оптимизации 
extern double    MaxR = 0.02;         // Максимальный допустимый риск
extern int       pATR=4;              // Период АТР
extern int       rChannel = 20;       // период канала
extern double    Trailing = 30;

//--------- global variables
double Atr;
double Resist;
double ResistPrev;
double Support;
double SupportPrev;
double Pivot;
//------------------   

int CalculateCurrentOrders(string symbol)
  {
   int buys=0;
   int sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) 
         break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  
            buys++;
         if(OrderType()==OP_SELL) 
           sells++;
        }
     }
//---- return orders volume
      if(buys>0) 
         return(buys);
      else
        return(-sells);
  }

//+------------------------------------------------------------------+
//| Расчет оптимальной на взгляд автора величины лота                |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   if (isFloatLots == true)          // если флаг true то проводится оптимизация величины лота, иначе лот неизменен
     {  
     	int orders=HistoryTotal();  // history orders total
   	int losses=0;               // number of losses orders without a break
//---- select lot size
	   lot=NormalizeDouble(AccountFreeMargin()*MaxR/1000.0,1);
//---- calcuulate number of losses orders without a break
	   if(DcF>0)
     	  {  
      	for(int i=orders-1;i>=0;i--)
        	  {
	         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) 
	           { 
	           	Print("Ошибка в истории!"); 
	           	break;
	           }
         	if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) 
         	  continue;
	         if(OrderProfit()>0) break;
         	if(OrderProfit()<0) losses++;
        	  }
      	if(losses>1) lot=NormalizeDouble(lot-lot*losses/DcF,1);
        }
     }  
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }

//-------------------------------------------------------------------+
// Вычисляем параметры канала                                        |
//-------------------------------------------------------------------+
void defPcChannel() 
  {
   Resist=High[Highest(NULL,0,MODE_HIGH,rChannel,1)]; // up channel   
   ResistPrev=High[Highest(NULL,0,MODE_HIGH,rChannel,2)];   
   Support=Low[Lowest(NULL,0,MODE_LOW,rChannel,1)];
   SupportPrev=Low[Lowest(NULL,0,MODE_LOW,rChannel,2)];
   Pivot = (Resist+Support+Close[1])/3;
  }

//-------------------------------------------------------------------+
// Определяем выполняются ли условия для открытия длинной            |
// позиции "на отбой"                                                |
//-------------------------------------------------------------------+
bool isOpenBuy()
  {
// определяем параметры канала
   defPcChannel() ;     
// условия открытия длинной позиции
   if ( High[1] >= Resist && Resist == ResistPrev) // касание ценой верхненй границы канала и формирование каналом "потолка" 
     return (true); 
   if ( Close [1] < Resist && Resist == ResistPrev && Close [1] > Pivot) // закрытие свечи в верхней половине канала и формирование каналом "потолка" 
     return(true); 

     return(false);
  }

//-------------------------------------------------------------------+
// Определяем выполняются ли условия для открытия короткой           |
// позиции "на отбой"                                                |
//-------------------------------------------------------------------+
bool isOpenSell()
  {
   defPcChannel();
   if (Low[1] <= Support && Support==SupportPrev ) // касание ценой нижней границы и формирование "пола"
     return (true);

   if (Close [1] > Support && Support==SupportPrev && Close [1] < Pivot ) // закрытие свечи внижней области канала и формирование "пола"
     return (true);

     return (false);
  }

//-------------------------------------------------------------------+
// Проверяем возможно ли вообще открытие позиций                     |
//-------------------------------------------------------------------+
void CheckForOpen()
  {
   int    res;
   string sHeaderLetter;
   string sBodyLetter;
//---- начинаем торговлю только с первым тиком нового бара
   if(Volume[0]>1) 
   return;
//---- 
   Atr = iATR(NULL,0,pATR,1);

//---- Условие продажи
   if (isOpenBuy() == True)
     {    
      Print("Lots = ",LotsOptimized());
      res=OrderSend(Symbol(),OP_SELL, LotsOptimized(),Bid,3, Resist+Atr,0,"",MAGICMA,0,Red);
       if (SndMl == True && res != -1) 
         {
         sHeaderLetter = "Operation SELL by " + Symbol()+"";
         sBodyLetter = "Order Sell by "+ Symbol() + " at " + DoubleToStr(Bid,4)+ ", and set stop/loss at " + DoubleToStr(Resist+Atr,4)+"";
         sndMessage(sHeaderLetter, sBodyLetter);
         }
      return;
     }

//---- Условие покупки
    if (isOpenSell() == true)
      {
       res=OrderSend(Symbol(),OP_BUY, LotsOptimized() ,Ask,3,Support-Atr,0,"order",MAGICMA,0,Blue);
       if ( SndMl == True && res != -1)
         { 
          sHeaderLetter = "Operation BUY at " + Symbol()+"";
          sBodyLetter = "Order Buy at "+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and set stop/loss at " + DoubleToStr(Support-Atr,4)+"";
          sndMessage(sHeaderLetter, sBodyLetter);
         }
       return;
      }
   return;    
  }  

//-------------------------------------------------------------------+
// Проверям возможность и условия для для закрытия позиций           |
//-------------------------------------------------------------------+
bool isCloseSell()
  {
   defPcChannel();
   if (Low[1] <= Support && Support==SupportPrev ) // касание ценой нижней границы и формирование "пола"
        return (true);
   return (false);
  }

bool isCloseBuy()
  {
   defPcChannel();
   
   if ( High[1] >= Resist && Resist == ResistPrev) // касание ценой верхненй границы канала и формирование каналом "потолка" 
     return (true); 

   return (false);
  }

void CheckForClose()
  {
   string sHeaderLetter;
   string sBodyLetter;
   bool CloseOrd;

   if(Volume[0]>1) return;

   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)
        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol())
        continue;
      
//----  
      if(OrderType()==OP_BUY)
        {
         if (isCloseBuy() == true )        
           {
            CloseOrd=OrderClose(OrderTicket(),OrderLots(),Bid,3,Lime);
            if ( SndMl == True && CloseOrd == True)
              {
               sHeaderLetter = "Operation CLOSE BUY at" + Symbol()+"";
               sBodyLetter = "Close order Buy at "+ Symbol() + " for " + DoubleToStr(Bid,4)+ ", and finish this Trade";
               sndMessage(sHeaderLetter, sBodyLetter);
              }
            break;
           }                                            
         else 
           {
// проверим трейлинг стоп 
            if(Trailing>0)  
              {                 
               if(Bid-OrderOpenPrice()>Point*Trailing)
                 {
                  if(OrderStopLoss()<Bid-Point*Trailing)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*Trailing,OrderTakeProfit(),0,Green);
                     return ;
                    }
                 }
              }
           }
        }
        
      if(OrderType()==OP_SELL)
        {
           
         if (isCloseSell() == true)       
           {
            CloseOrd=OrderClose(OrderTicket(),OrderLots(),Ask,3,Lime);
            if ( SndMl == True && CloseOrd == True) 
              {
               sHeaderLetter = "Operation CLOSE SELL at" + Symbol()+"";
               sBodyLetter = "Close order Sell at "+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and finish this Trade";
               sndMessage(sHeaderLetter, sBodyLetter);
              }
            break;
           }
         else 
           {
// проверим на трейлинг стоп 
            if(Trailing>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*Trailing))
                 {
                  if((OrderStopLoss()>(Ask+Point*Trailing)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*Trailing,OrderTakeProfit(),0,Red);
                     return;
                    }
                 }
              }
           }
        }
     }
  }  
//-------------------------------------------------------------------+
// Функция отправки собщения об открытии или закрытии позиции        |
//-------------------------------------------------------------------+
void sndMessage(string HeaderLetter, string BodyLetter)
  {
   int RetVal;
   SendMail( HeaderLetter, BodyLetter );
   RetVal = GetLastError();
   if (RetVal!= ERR_NO_MQLERROR) 
     Print ("Ошибка, сообщение не отправлено: ", ErrorDescription(RetVal));
   return;      
  }

//+------------------------------------------------------------------+
//| Expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if(Bars<25 || IsTradeAllowed()==false) 
     return (0);
   if (AccountFreeMargin()<(100*Point*Lots))
     {
      Print("Стоп! Недостаточно средств для продолжения торговли. Свободная маржа = ", AccountFreeMargin());
      return(0);  
     }
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();

   return(0);
  }
//+------------------------------------------------------------------+