//+------------------------------------------------------------------+
//|                                                       MA2CCI.mq4 |
//|                                  Copyright © 2005, George-on-Don |
//|                                       http://www.forex.aaanet.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, George-on-Don"
#property link      "http://www.forex.aaanet.ru"


#include <stdlib.mqh>
#include <stderror.mqh>

#define MAGICMA  20050610

//---- input parameters
extern int       FMa=4; // быстрый мувинг
extern int       SMa=8; // медленный мувинг
extern int       PCCi=4; // период CCI
extern int       pATR=4; //Период ATR для стоп/лосса
extern double    Lots=0.1; // лот
extern bool      SndMl=true; // флаг для отправки информации на е-майл
extern double    DcF = 3;// Фактор оптимизации 
extern double    MaxR = 0.02; // Максимальный риск
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
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
  
void CheckForOpen()
{
   double mas;
   double maf;
   double mas_p;
   double maf_p;
   double Atr;
   double icc;
   double icc_p;
   int    res;
   string sHeaderLetter;
   string sBodyLetter;
//---- начинаем торговлю только с первым тиком нового бара
   if(Volume[0]>1) return;
//---- определяем Moving Average 
   mas=iMA(NULL,0,SMa,0,MODE_SMA,PRICE_CLOSE,1); // динный мувинг 1 период назад
   maf=iMA(NULL,0,FMa,0,MODE_SMA,PRICE_CLOSE,1);// короткий мувинг 1 период назад
   mas_p=iMA(NULL,0,SMa,0,MODE_SMA,PRICE_CLOSE,2); // динный мувинг 2 периода назад
   maf_p=iMA(NULL,0,FMa,0,MODE_SMA,PRICE_CLOSE,2);// короткий мувинг 2 периода назад
   Atr = iATR(NULL,0,pATR,0);
   icc = iCCI(NULL,0,PCCi,PRICE_CLOSE,1);// CCI 1 период назад
   icc_p = iCCI(NULL,0,PCCi,PRICE_CLOSE,2);// CCI 2 периода назад
 //---- Условие продажи
   if ( (maf<mas && maf_p>=mas_p)&&(icc<0 && icc_p >=0 )) 
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Ask+Atr,0,"",MAGICMA,0,Red);
       if (SndMl == True && res != -1) 
         {
         sHeaderLetter = "Operation SELL by " + Symbol()+"";
         sBodyLetter = "Order Sell by "+ Symbol() + " at " + DoubleToStr(Bid,4)+ ", and set stop/loss at " + DoubleToStr(Ask+Atr,4)+"";
         sndMessage(sHeaderLetter, sBodyLetter);
         }
      return;
     }
//---- Условие покупки
   if ((maf>mas && maf_p<=mas_p)&& (icc > 0 && icc_p <=0 ))  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,Bid-Atr,0,"",MAGICMA,0,Blue);
      if ( SndMl == True && res != -1)
      { 
      sHeaderLetter = "Operation BUY at " + Symbol()+"";
      sBodyLetter = "Order Buy at "+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and set stop/loss at " + DoubleToStr(Bid-Atr,4)+"";
      sndMessage(sHeaderLetter, sBodyLetter);
      }
      return;
     }
}  

void CheckForClose()
{
double mas;
   double maf;
   double mas_p;
   double maf_p;
   string sHeaderLetter;
   string sBodyLetter;
   bool CloseOrd;
//---- 
   if(Volume[0]>1) return;
//----  
   mas=iMA(NULL,0,SMa,0,MODE_SMA,PRICE_CLOSE,1); // динный мувинг 1 период назад
   maf=iMA(NULL,0,FMa,0,MODE_SMA,PRICE_CLOSE,1);// короткий мувинг 1 период назад
   mas_p=iMA(NULL,0,SMa,0,MODE_SMA,PRICE_CLOSE,2); // динный мувинг 2 периода назад
   maf_p=iMA(NULL,0,FMa,0,MODE_SMA,PRICE_CLOSE,2);// короткий мувинг 2 периода назад
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //----  
      if(OrderType()==OP_BUY)
        {
         if(maf<mas && maf_p>=mas_p) CloseOrd=OrderClose(OrderTicket(),OrderLots(),Bid,3,Lime);
            if ( SndMl == True && CloseOrd == True)
            {
            sHeaderLetter = "Operation CLOSE BUY at" + Symbol()+"";
            sBodyLetter = "Close order Buy at "+ Symbol() + " for " + DoubleToStr(Bid,4)+ ", and finish this Trade";
            sndMessage(sHeaderLetter, sBodyLetter);
            }
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if(maf>mas && maf_p<=mas_p) OrderClose(OrderTicket(),OrderLots(),Ask,3,Lime);
         if ( SndMl == True && CloseOrd == True) 
         {
         sHeaderLetter = "Operation CLOSE SELL at" + Symbol()+"";
         sBodyLetter = "Close order Sell at "+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and finish this Trade";
         sndMessage(sHeaderLetter, sBodyLetter);
         }
         break;
        }
     }
//----
}  

//+------------------------------------------------------------------+
//| Расчет оптимальной величины лота                                 |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*MaxR/1000.0,1);
//---- calcuulate number of losses orders without a break
   if(DcF>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Ошибка в истории!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DcF,1);
     }
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
}

//--------------------------------------------------------------------
// функция отправки ссобщения об отрытии или закрытии позиции
//--------------------------------------------------------------------
void sndMessage(string HeaderLetter, string BodyLetter)
{
   int RetVal;
   SendMail( HeaderLetter, BodyLetter );
   RetVal = GetLastError();
   if (RetVal!= ERR_NO_MQLERROR) Print ("Ошибка, сообщение не отправлено: ", ErrorDescription(RetVal));
}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   if(Bars<25 || IsTradeAllowed()==false) return;
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----
   return(0);
  }
//+------------------------------------------------------------------+