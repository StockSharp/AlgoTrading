/////////////////////////////////////////////////////////////////////////
//                                                                     //                              
// Универсальный эксперт, для долгосрочной, портфельной торговли.      //
// Метод использования средних в данном эксперте, показывает прибыль   //
// на многих инструментах.                                             //
//                                                                     //
// ВНИМАНИЕ!                                                           //
// Рекомендуемый тайм фрейм для forex (День).                          //
// Рекомендуемый тайм фрейм для CFD и Futures (15 - 30 минут)          //
// Тестировать и оптимизировать по тикам (требуется минутная история). //
// Если минутной истории нет, тестировать входы Long и Short по        //
// отдельности. (Метод "по ценам открытия".)                           //
//                                                       Olek          //
/////////////////////////////////////////////////////////////////////////

extern int    MovingPeriod       = 23; //период сглаживания "оптимизируется"
extern int    Magic_№            = 1; // для каждого инструмента, должен быть свой номер
extern double MaximumRisk        = 0.05;// МаниМенеджмент
extern double Lots               = 0.1; // МаниМенеджмент
extern int    DecreaseFactor     = 0;   // МаниМенеджмент
double EMA1 = 0;
double LWMA1 =0;
double EMA2  =0;
double LWMA2 =0;


//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==Magic_№)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//----
   if(buys>0) return(buys);
   else       return(-sells);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     
   int    losses=0;                  
//---- 
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1); //для forex
//   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/(10*Close[1]),1); //для CFD и Futures
//---- 
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
//---- 
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
  
//---- 
  
   int    ticket;
   bool   Sell=0, Buy=0;
   
//----
  
   static datetime prevtime1=0;
   if(prevtime1 == Time[0]) return(0);
   prevtime1 = Time[0];
   
//----  
   
   EMA1=iMA(0,0,MovingPeriod,0,MODE_EMA,PRICE_CLOSE,1);
   EMA2=iMA(0,0,MovingPeriod,0,MODE_EMA,PRICE_CLOSE,2);
   LWMA1=iMA(0,0,MovingPeriod,0,MODE_LWMA,PRICE_CLOSE,1);
   LWMA2=iMA(0,0,MovingPeriod,0,MODE_LWMA,PRICE_CLOSE,2); 
   
//----  

   Sell=   LWMA1 < EMA1 && LWMA1 < LWMA2 && EMA1 < EMA2; 
   Buy=    LWMA1 > EMA1 && LWMA1 > LWMA2 && EMA1 > EMA2;
   
//----  
   
   if(Sell ==1 )
     {
      ticket=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"",Magic_№,0,Red);
      if(ticket>0)
        {
          if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) { Print("SELL order opened : ",OrderOpenPrice());}
        }
      else Print("Error opening SELL order : ",GetLastError()); 
      return(0); 
     }

   if(Buy == 1)  
     {
      ticket=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"",Magic_№,0,Blue);
      if(ticket>0)
        {
          if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))  { Print("BUY order opened : ",OrderOpenPrice());}
        }
      else Print("Error opening BUY order : ",GetLastError()); 
      return(0); 
     }
//----
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
  
//----
  
   
   bool   Sell=0, Buy=0;
   
//----
  
   static datetime prevtime2=0;
   if(prevtime2 == Time[0]) return(0);
   prevtime2 = Time[0];

//----   
   
   EMA1=iMA(0,0,MovingPeriod,0,MODE_EMA,PRICE_CLOSE,1);
   LWMA1=iMA(0,0,MovingPeriod,0,MODE_LWMA,PRICE_CLOSE,1);
   
//----   
    
   Sell=   LWMA1 < EMA1 ;
   Buy=    LWMA1 > EMA1 ;
   
//----

  for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=Magic_№ || OrderSymbol()!=Symbol()) continue;
      if(OrderType()==OP_BUY)
        {
          if (Sell==1)
             {
              OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);
              return(0);
             }
        }    
              
      if(OrderType()==OP_SELL)
        {
           if (Buy==1)
             {
              OrderClose(OrderTicket(),OrderLots(),Ask,3,Blue);
              return(0);
             }
        }
     }
//----
  }
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----
  }
//+------------------------------------------------------------------+