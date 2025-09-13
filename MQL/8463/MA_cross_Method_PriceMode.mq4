//============================================================================================
//
//
//
//
//
//============================================================================================
extern int    MA1_Period=3;                              // Период 1-й МА
extern int    MA2_Period=13;                             // Период 2-й МА
extern int    MA1_Method=0;                              // Метод вычисления МА1 (SMA=0,EMA=1,SMMA=2,LWMA=3)
extern int    MA2_Method=3;                              // Метод вычисления МА2 (SMA=0,EMA=1,SMMA=2,LWMA=3)
extern int    MA1_Price=0;                               // Метод вычисления цены МА1 
extern int    MA2_Price=4;                               // Метод вычисления цены МА2
extern int    MA1_Shift=0;                               // Временной сдвиг МА1
extern int    MA2_Shift=0;                               // Временной сдвиг МА2
extern double Lot = 0.1;                                 // Фиксированный лот
extern int    slippage = 0;                              // Отклонение цены для рыночных ордеров 
int New_Bar;                                             // 0/1 Факт образования нового бара
int Time_0;                                              // Время начала нового бара
int PosOpen;                                             // Направление пересечения
int PosClose;                                            // Направление пересечения
int total;                                               // Количество открытых ордеров
double MA1_0;                                            // Текущее значение 1-й МА (розов)
double MA1_1;                                            // Предыдущее значение 1-й МА (розов)
double MA2_0;                                            // Текущее значение 2-й МА (голубая)
double MA2_1;                                            // Предыдущее значение 2-й МА (голубая)
int orderBuy;                                            // 1 = факт налиия ордера Buy
int orderSell;                                           // 1 = факт налиия ордера Sell 
//============================================================================================
int init()  
   {

   }  
//============================================================================================
int start()  
   {
   orderBuy=0;
   orderSell=0; 
   double price;  
   int openOrders=0;
   int total=OrdersTotal();                                  // Общее количество ордеров
   for(int i=total-1;i>=0;i--)
      {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)     // Выбираем ордер
         {
         if(OrderType()==OP_BUY)                             // Если стоит ордер на покупку
            {
            orderBuy=1;
            if(CrossPositionClose()==1)                      // Закрывем ордер, если удовлетворяет
               {                                             // условию CrossPositionClose()=1
               price=MarketInfo(Symbol(),MODE_BID);
               OrderClose(OrderTicket(),OrderLots(),price,slippage,CLR_NONE);
               }
            }
         if(OrderType()==OP_SELL)                            // Если стоит ордер на покупку
            {
            orderSell=1;
            if(CrossPositionClose()==2)                      // Закрывем ордер, если удовлетворяет
               {                                             // условию CrossPositionClose()=2
               price=MarketInfo(Symbol(),MODE_ASK);
               OrderClose(OrderTicket(),OrderLots(),price,slippage,CLR_NONE);
               }
            }
         }
      }
   
   New_Bar=0;                                                // Для начала обнулимся
   if (Time_0 != Time[0])                                    // Если уже другое время начала бара
      {
      New_Bar= 1;                                            // А вот и новый бар
      Time_0 = Time[0];                                      // Запомним время начала нового бара
      } 
   
   MA1_0=iMA(NULL,0, MA1_Period, MA1_Shift,MAMethod(MA1_Method), MAPrice(MA1_Price), 0);    // Текущее    значение 1-й МА
   MA1_1=iMA(NULL,0, MA1_Period, MA1_Shift,MAMethod(MA1_Method), MAPrice(MA1_Price), 1);    // Предыдущее значение 1-й МА
   MA2_0=iMA(NULL,0, MA2_Period, MA2_Shift,MAMethod(MA2_Method), MAPrice(MA2_Price), 0);    // Текущее    значение 2-й МА
   MA2_1=iMA(NULL,0, MA2_Period, MA2_Shift,MAMethod(MA2_Method), MAPrice(MA2_Price), 1);    // Предыдущее значение 2-й МА
   
   if (CrossPositionOpen()==1 && New_Bar==1)                 // Движение снизу вверх = откр. Buy
      {
      OpenBuy();
      }      
   if (CrossPositionOpen()==2 && New_Bar==1)                 // Движение сверху вниз = откр. Sell
      {
      OpenSell();
      }    
   return;
   }  
//============================================================================================
int CrossPositionOpen()
   {
   PosOpen=0;                                                 // Вот где собака зарыта!!:)
   if ((MA1_1<=MA2_0 && MA1_0>MA2_0) || (MA1_1<MA2_0 && MA1_0>=MA2_0))   // Пересечение снизу вверх  
      {
      PosOpen=1;
      }                  
   if ((MA1_1>=MA2_0 && MA1_0<MA2_0) || (MA1_1>MA2_0 && MA1_0<=MA2_0))   // Пересечение сверху вниз
      {
      PosOpen=2;
      }             
   return(PosOpen);                                          // Возвращаем направление пересечен.
   }
//============================================================================================
int CrossPositionClose()
   {
   PosClose=0;                                                // Вот где собака зарыта!!:)
   if ((MA1_1>=MA2_0 && MA1_0<MA2_0) || (MA1_1>MA2_0 && MA1_0<=MA2_0))   // Пересечение сверху вниз        {
      {
      PosClose=1;
      }                  
   if ((MA1_1<=MA2_0 && MA1_0>MA2_0) || (MA1_1<MA2_0 && MA1_0>=MA2_0))   // Пересечение снизу вверх
      {
      PosClose=2;
      }             
   return(PosClose);                                          // Возвращаем направление пересечен.
   }
//============================================================================================
int OpenBuy() 
   {
   if (total==1)
      {
      OrderSelect(0, SELECT_BY_POS,MODE_TRADES);              // Выделим ордер
      if (OrderType()==OP_BUY) return;                        // Если он buy, то не открываемся
      }
   OrderSend(Symbol(),OP_BUY, Lot, Ask, slippage, 0, 0, "Buy: MA_cross_Method_PriceMode", 1, 0, CLR_NONE);// Открываемся
   return;
   }
//============================================================================================
int OpenSell() 
   {
   if (total==1)
      {
      OrderSelect(0, SELECT_BY_POS,MODE_TRADES);              // Выделим ордер
      if (OrderType()==OP_SELL) return;                       // Если он sell, то не открываемся
      }
   OrderSend(Symbol(),OP_SELL, Lot, Bid, slippage, 0, 0, "Sell: MA_cross_Method_PriceMode", 2, 0, CLR_NONE);
   return;
   }
//============================================================================================
int MAMethod(int MA_Method)
   {
      switch(MA_Method)
        {
         case 0: return(0);                                   // Возвращает MODE_SMA=0
         case 1: return(1);                                   // Возвращает MODE_EMA=1
         case 2: return(2);                                   // Возвращает MODE_SMMA=2
         case 3: return(3);                                   // Возвращает MODE_LWMA=3
        }
   }
//============================================================================================
int MAPrice(int MA_Price)
   {
      switch(MA_Price)
        {
         case 0: return(PRICE_CLOSE);                         // Возвращает PRICE_CLOSE=0        
         case 1: return(PRICE_OPEN);                          // Возвращает PRICE_OPEN=1
         case 2: return(PRICE_HIGH);                          // Возвращает PRICE_HIGH=2
         case 3: return(PRICE_LOW);                           // Возвращает PRICE_LOW=3
         case 4: return(PRICE_MEDIAN);                        // Возвращает PRICE_MEDIAN=4
         case 5: return(PRICE_TYPICAL);                       // Возвращает PRICE_TYPICAL=5
         case 6: return(PRICE_WEIGHTED);                      // Возвращает PRICE_WEIGHTED=6
        }
   }
//============================================================================================

