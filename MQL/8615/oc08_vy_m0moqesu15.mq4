

bool New_Bar=false;                // Факт нового бара  
                                
extern double StopLoss   =100;     // SL для открываемого ордера
extern double TakeProfit =100;     // ТР для открываемого ордера

extern double MAp         =12;     // MA период
extern double MAs         =0;      // MA смещение
extern double MAm         =2;      // МА метод


extern double Lots       =0.1;     // Жестко заданное количество лотов
extern double Prots      =0.02;    // Процент свободных средств


//--------------------------------------------------------------- 1 --
int start()
  {
   int
   Total,                           // Количество ордеров в окне 
   Tip=-1,                          // Тип выбран. ордера (B=0,S=1)
   Ticket;                          // Номер ордера
   double
   MA,                              // Значен. МА
   Lot,                             // Колич. лотов в выбран.ордере
   Lts,                             // Колич. лотов в открыв.ордере
   Min_Lot,                         // Минимальное количество лотов
   Step,                            // Шаг изменения размера лота
   Free,                            // Текущие свободные средства
   One_Lot,                         // Стоимость одного лота
   Price,                           // Цена выбранного ордера
   SL,                              // SL выбранного ордера 
   TP;                              // TP выбранного ордера
   bool
   Cls_B=false,                     // Критерий для закрытия  Buy
   Cls_S=false,                     // Критерий для закрытия  Sell
   Opn_B=false,                     // Критерий для открытия  Buy
   Opn_S=false;                     // Критерий для открытия  Sell
//--------------------------------------------------------------- 2 --
   // Учёт ордеров
   
   for(int i=1; i<=OrdersTotal(); i++)          // Цикл перебора ордер
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // Если есть следующий
        {                                       // Анализ ордеров:     
         Ticket=OrderTicket();                  // Номер выбранн. орд.
         Tip   =OrderType();                    // Тип выбранного орд.
         Price =OrderOpenPrice();               // Цена выбранн. орд.
         SL    =OrderStopLoss();                // SL выбранного орд.
         TP    =OrderTakeProfit();              // TP выбранного орд.
         Lot   =OrderLots();                    // Количество лотов
        }
     }
//--------------------------------------------------------------- 3 --
   // Торговые критерии
   
   static datetime New_Time=0;                  // Время текущего бара   
   New_Bar=false;                               // Нового бара нет   
   if(New_Time!=Time[0])                        // Сравниваем время     
   {       
   New_Time=Time[0];                            // Теперь время такое      
   New_Bar=true;                                // Поймался новый бар     
   }
   
   MA=iMA(NULL,0,MAp,MAs,MAm,MODE_MAIN,0);      // Задаем МА и параметры
   
   if (Close[0]>MA && New_Bar==true)            // Если цена закрытия больше МА и образовался новый бар...
     {                                          // 
      Opn_B=true;                               // ...Критерий откр. Buy
      Cls_S=true;                               // Критерий закр. Sell
     }
   if (Close[0]<MA && New_Bar==true)            // Если цена закрытия меньше МА и образовался новый бар...
     {                                          // 
      Opn_S=true;                               // ...Критерий откр. Sell
      Cls_B=true;                               // Критерий закр. Buy
     }
//--------------------------------------------------------------- 4 --
   // Закрытие ордеров
   
   while(true)                                  // Цикл закрытия орд.
     {
      if (Tip==0 && Cls_B==true)                // Открыт ордер Buy и есть критерий закр 
        {                                       // 
         OrderClose(Ticket,Lot,Bid,2);          // Закрытие Buy
         return;                                // Выход из start()
        }

      if (Tip==1 && Cls_S==true)                // Открыт ордер Sell и есть критерий закр 
        {                                       //        
         OrderClose(Ticket,Lot,Ask,2);          // Закрытие Sell
         return;                                // Выход из start()
        }
      break;                                    // Выход из while
     }
//--------------------------------------------------------------- 5 --
   // Стоимость ордеров (если лот указан как "0")
   
   RefreshRates();                                // Обновление данных
   Min_Lot=MarketInfo(NULL,MODE_MINLOT);          // Миним. колич. лотов 
   Free   =AccountFreeMargin();                   // Свободн средства
   One_Lot=MarketInfo(NULL,MODE_MARGINREQUIRED);  // Стоимость 1 лота
   Step   =MarketInfo(NULL,MODE_LOTSTEP);         // Шаг изменен размера

   if (Lots > 0)                                  // Если заданы лоты,то 
      Lts =Lots;                                  // с ними и работаем 
   else                                           // % свободных средств
      Lts=MathFloor(Free*Prots/One_Lot/Step)*Step;// Для открытия

   if(Lts < Min_Lot) Lts=Min_Lot;                 // Не меньше минимальн
   if (Lts*One_Lot > Free)                        // Лот дороже свободн.
     {
      Alert(" Не хватает денег на ", Lts," лотов");
      return;                                     // Выход из start()
     }
//--------------------------------------------------------------- 6 --
   // Открытие ордеров
   
   while(true)                                             // Цикл закрытия орд.
     {
      if (OrdersTotal()==0 && Opn_B==true)                 // Открытых орд. нет +
        {                                                  // критерий откр. Buy
         SL=Bid - StopLoss*Point;                          // Вычисление SL откр.
         TP=Bid + TakeProfit*Point;                        // Вычисление TP откр.
         OrderSend(Symbol(),OP_BUY,Lts,Ask,2,SL,TP);       //Открытие Buy
         return;                                           // Выход из start()
        }
      
      if (OrdersTotal()==0 && Opn_S==true)                  // Открытых орд. нет +
        {                                                   // критерий откр. Sell
         SL=Ask + StopLoss*Point;                           // Вычисление SL откр.
         TP=Ask - TakeProfit*Point;                         // Вычисление TP откр.
         OrderSend(Symbol(),OP_SELL,Lts,Bid,2,SL,TP);       //Открытие Sel
         return;                                            // Выход из start()
        }
      break;                                                // Выход из while
     }
//--------------------------------------------------------------- 9 --
   return;                                           // Выход из start()
  }


