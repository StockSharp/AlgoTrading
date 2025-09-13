//+------------------------------------------------------------------+
//|                                                                  |
//|                 Copyright © 1999-2007, MetaQuotes Software Corp. |
//|                                         http://www.metaquotes.ru |
//+------------------------------------------------------------------+
#property copyright "AD"
#property link      ""
//----
double Lots=1;
// Есть ли открытые советником ордера? Если есть, то: или закрываем, или запрещаем открывать новый
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckOrders(int Type)
  {
   int ticket,i;
   bool Result;
//----
   Result=True;
   if(OrdersTotal()!=0)
     {
      for(i=0;i<OrdersTotal();i++)
        {
         ticket=OrderSelect(i,SELECT_BY_POS);
         if(OrderMagicNumber()==553)
           {
            if(OrderType()==Type)
              {
               if(Type==OP_BUY)
                 {
                  if(OrderClose(OrderTicket(),OrderLots(),Bid,10)==False)
                     Result=False;
                 }
               if(Type==OP_SELL)
                 {
                  if(OrderClose(OrderTicket(),OrderLots(),Ask,20)==False)
                     Result=False;
                 }
              }
            else Result=False;
           }
        }
     }
   return(Result);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double MA, MAPrev;
   int ticket;
//----
   MA=iMA("GBPUSD",15,1,0,MODE_EMA,PRICE_TYPICAL,0);
   MAPrev=iMA("GBPUSD",15,1,0,MODE_EMA,PRICE_TYPICAL,1);
   // Открытие вверх
   if(Open[0]<MA-Point && Ask<MA)
      if(CheckOrders(OP_SELL)==True && MAPrev<MA)
        {
         Lots=NormalizeDouble(AccountFreeMargin()/10000, 1);
         if(Lots>5)
            Lots=5;
         ticket=OrderSend("GBPUSD",OP_BUY,Lots,Ask,10,0,0,NULL,553);
         if(ticket<0)
            Print("Не удалось открыть ордер BUY. Ошибка N", GetLastError());
        }
   // Открытие вниз
   if(Open[0]>MA+Point && Bid>MA)
      if(CheckOrders(OP_BUY)==True && MAPrev>MA)
        {
         Lots=NormalizeDouble(AccountFreeMargin()/10000, 1);
         if(Lots>5)
            Lots=5;
         ticket=OrderSend("GBPUSD",OP_SELL,Lots,Bid,10,0,0,NULL,553);
         if(ticket<0)
            Print("Не удалось открыть ордер SELL. Ошибка N", GetLastError());
        }
//----
   return(0);
  }
//+------------------------------------------------------------------+