//+------------------------------------------------------------------+
//|                                                      CrossMa.mq4 |
//|                      Copyright © 2005, George-on-Don             |
//|                                       http://www.forex.aaanet.ru |
//+------------------------------------------------------------------+
#include <stdlib.mqh>
#include <stderror.mqh>
 
#define MAGICMA  20050610
 
extern double Lots               = 0.1;
extern double MaximumRisk        = 0.02;
extern double DecreaseFactor     = 3;
extern double MovingPeriod       = 12;
extern double MovingShift        = 0;
extern double MovingPeriod1      = 4;
extern double AtrPer             = 6;
extern bool   SndMl              = True ;
//+------------------------------------------------------------------+
//| Расчет открытия позиции                                          |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//---- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
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
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Проверка для открытия позиции с первым тиком нового бара.        |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   double mas;
   double maf;
   double mas_p;
   double maf_p;
   double Atr;
   int    res;
   string sHeaderLetter;
   string sBodyLetter;
//---- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//---- get Moving Average 
   mas=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,1); // динный мувинг 12
   maf=iMA(NULL,0,MovingPeriod1,MovingShift,MODE_SMA,PRICE_CLOSE,1);// короткий мувинг 4
   mas_p=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,2); // динный мувинг 12
   maf_p=iMA(NULL,0,MovingPeriod1,MovingShift,MODE_SMA,PRICE_CLOSE,2);// короткий мувинг 4
   Atr = iATR(NULL,0,AtrPer,0);
 //---- Условие продажи
   if(maf<mas && maf_p>=mas_p)  
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Ask+Atr,0,"",MAGICMA,0,Red);
       if (SndMl == True && res != -1) 
         {
         sHeaderLetter = "Operation SELL by" + Symbol()+"";
         sBodyLetter = "Order Sell by"+ Symbol() + " at " + DoubleToStr(Bid,4)+ ", and set stop/loss at " + DoubleToStr(Ask+Atr,4)+"";
         sndMessage(sHeaderLetter, sBodyLetter);
         }
      return;
     }
//---- Условие покупки
   if(maf>mas && maf_p<=mas_p)  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,Bid-Atr,0,"",MAGICMA,0,Blue);
      if ( SndMl == True && res != -1)
      { 
      sHeaderLetter = "Operation BUY at" + Symbol()+"";
      sBodyLetter = "Order Buy at"+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and set stop/loss at " + DoubleToStr(Bid-Atr,4)+"";
      sndMessage(sHeaderLetter, sBodyLetter);
      }
      return;
     }
//----
  }
//+------------------------------------------------------------------+
//| ПРоверка для закрытия открытой позиции                           |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   double mas;
   double maf;
   double mas_p;
   double maf_p;
   string sHeaderLetter;
   string sBodyLetter;
   bool rtvl;
//---- 
   if(Volume[0]>1) return;
//----  
   mas=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,1); // динный мувинг 12
   maf=iMA(NULL,0,MovingPeriod1,MovingShift,MODE_SMA,PRICE_CLOSE,1);// короткий мувинг 4
   mas_p=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,2); // динный мувинг 12
   maf_p=iMA(NULL,0,MovingPeriod1,MovingShift,MODE_SMA,PRICE_CLOSE,2);// короткий мувинг 4
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //----  
      if(OrderType()==OP_BUY)
        {
         if(maf<mas && maf_p>=mas_p) rtvl=OrderClose(OrderTicket(),OrderLots(),Bid,3,Lime);
            if ( SndMl == True && rtvl != False )
            {
            sHeaderLetter = "Operation CLOSE BUY at" + Symbol()+"";
            sBodyLetter = "Close order Buy at"+ Symbol() + " for " + DoubleToStr(Bid,4)+ ", and finish this Trade";
            sndMessage(sHeaderLetter, sBodyLetter);
            }
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if(maf>mas && maf_p<=mas_p) rtvl=OrderClose(OrderTicket(),OrderLots(),Ask,3,Lime);
         if ( SndMl == True && rtvl != False ) 
         {
         sHeaderLetter = "Operation CLOSE SELL at" + Symbol()+"";
         sBodyLetter = "Close order Sell at"+ Symbol() + " for " + DoubleToStr(Ask,4)+ ", and finish this Trade";
         sndMessage(sHeaderLetter, sBodyLetter);
         }
         break;
        }
     }
//----
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
//| Майн функция                                                     |
//+------------------------------------------------------------------+
void start()
  {
//---- check for history and trading
   if(Bars<25 || IsTradeAllowed()==false) return;
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----
  }
//+------------------------------------------------------------------+

