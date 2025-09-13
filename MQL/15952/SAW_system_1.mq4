//+------------------------------------------------------------------+
//|                                                 SAW_system_1.mq4 |
//|                                              Copyright 2014, SAW |
//|                                http://saw-trade.livejournal.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, SAW"
#property link      "http://saw-trade.livejournal.com/"
#property version   "1.00"
#property description "Советник SAW_system_1 выставляет отложенные ордера из расчета волатильности"
#property description "за N дней. StopLoss всегда устанавливается на уровень противоположного ордера,"
#property description "соответственно, в параметрах указывается расстояние до стопа, там же будет стоять"
#property description "и противоположный ордер!"
#property description "Тайм-фрейм не имеет значения."
#property strict

//--- input parameters
input double   lot         = 0.01;  // Lot
input int      d           = 5;     // Amount of days (for calculating volatility)
input int      open_hour   = 7;     // Hour installation orders (terminal time)
input int      close_hour  = 10;    // Hour removal orders (terminal time)
input int      sl_rate     = 15;    // Stop-Loss (percentage of the average volatility)
input int      tp_rate     = 30;    // Take-Profit (percentage of the average volatility)
input bool     rev         = false; // Reverse positions
input bool     martin      = false; // Martingale
input double   martin_koef = 2.0;   // Multiplier

//--- global parameters
//int      dig         = 0;
bool     new_day= true;  // new trading day
int      day_week= -1;   // the current day of the week
int      d_average   = 0;     // the value of the average volatility for the selected period
int      sl          = 0;     // Stop Loss
int      tp          = 0;     // Take Profit
int      or          = 0;     // the distance from the stage to order
int      err         = 0;     // variable for counting the number of consecutive errors
bool     mod_order   = false; // orders modification flag

//--- custom functions
void v_calc();
void sl_tp_or_calc();
int send_orders();
int error();
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   day_week=DayOfWeek();   // устанавливаем текущий день недели (0 - воскресенье)

   if(Hour()>=open_hour) // если час прошел, то в этот день ордера не устанавливаем
      new_day= false;
   
   v_calc();         // РАСЧЕТ ВОЛАТИЛЬНОСТИ
   sl_tp_or_calc();  // рассчитываем стоп-лосс и тейк-профит

// расчет переменной dig   
//   if (Digits() == 5)
//      dig = 100000;
//   if (Digits() == 4)
//      dig = 10000;  
//   
//   if (Digits() == 3)
//      dig = 1000;
//   if (Digits() == 2)
//      dig = 100;

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- проверяем новый день
   if(new_day==false && day_week!=DayOfWeek())
     {
      v_calc();         // рассчитываем волатильность

      sl_tp_or_calc();  // рассчитываем стоп-лосс и тейк-профит

      //--- устанавливаем переменные
      day_week= DayOfWeek();
      new_day = true;
      mod_order=false;
     }
//-----------------------------------------------------------------------
//--- при наступлении нового дня и торгового часа, выставляем наши ордера
   if(day_week==DayOfWeek() && new_day==true && Hour()==open_hour)
     {
      while(send_orders()==666) // если ордер не установился, перезапускаем 5 раз
        {
         switch(error())
           {
            case 666: new_day=false; mod_order=true; Print("!!!!!!ПРОИЗОШЛА ОШИБКА №  ",GetLastError()); return;   // if the restart does not help, we derive a mistake and do not sell
            case   1: RefreshRates();
           }
        }
     }
//------------------------------------------------------------------------   
// если день не новый и т. д.
   if(new_day==false && day_week==DayOfWeek() && mod_order==false)
     {
      int tip1      = -1;   // тип первого ордера
      int tip2      = -1;   // тип второго ордера
      int tick1     = -1;   // номер первого ордера
      int tick2     = -1;   // номер второго ордера
      double price2 = -1.0; // цена открытия второго ордера
      double sl2    = -1.0; // стоп-лосс второго ордера
      double tp2    = -1.0; // тейк-профит второго ордера

      RefreshRates();

      // проверяем количество ордеров
      if(OrdersTotal()>2)
        {
         Print("БОЛЬШЕ 2-х ордеров на одном инструменте. Закрываем ВСЕ!!!");
         close_all_orders();
         return;
        }

      // получаем информацию по ордерам
      for(int i=0; i+1<=OrdersTotal(); i++)
        {

         if(OrderSelect(i,SELECT_BY_POS)==true)
           {
            switch(i)
              {
               case  0: tip1 = OrderType(); break;
               case  1: tip2 = OrderType(); tick2 = OrderTicket();
               price2=OrderOpenPrice(); sl2=OrderStopLoss(); tp2=OrderTakeProfit(); break;
               default: Print("Неверное количество ордеров, сбой в работе цикла!!!");
              }
           }
        }

      // проверяем время жизни отложенных ордеров
      if(tip1>1 && tip2>1 && close_hour<=Hour())
        {
         close_all_orders();
         mod_order=true;
         tip1=tip2=-1;
        }

      // меняем значения ордеров местами, если нужно
      if((tip2==0 || tip2==1) && tip1>1)
        {
         int t;

         if(OrderSelect(0,SELECT_BY_POS)==true)
           {
            tick2=OrderTicket();
            price2=OrderOpenPrice();
            sl2 = OrderStopLoss();
            tp2 = OrderTakeProfit();

            t=tip1;
            tip1 = tip2;
            tip2 = t;
           }

        }

      // модифицируем или удаляем ордера, если нужно
      if((tip1==0 || tip1==1) && tip2>1)
        {
         if(rev==false)
           {
            if(OrderDelete(tick2)==true)
               mod_order=true;
           }

         if(rev==true && martin==true)
           {
            if(OrderDelete(tick2)==true)
               if(OrderSend(Symbol(),tip2,lot*martin_koef,price2,5,sl2,tp2,NULL,0,0,clrNONE)==-1)
                  Print("ОШИБКА переустановки второго ордера, для увеличения лота №",GetLastError());

            mod_order=true;
           }

         if(rev==true && martin==false)
           {
            mod_order=true;
           }
        }
     }

// проверка на случай срабатывания тейка одного из ордеров
   if(mod_order==true && OrdersTotal()==1 && Hour()>=close_hour)
      if(OrderSelect(0,SELECT_BY_POS)==true)
         if(OrderType()>1)
            if(!OrderDelete(OrderTicket()))
               Print("ОШИБКА удаления ордера! GetLastError = " + (string)GetLastError());

// дополнительная проверка на случай отключения интернета
   if(new_day==true && day_week!=DayOfWeek())
     {
      day_week=DayOfWeek();
      mod_order=false;

      if(Hour() >= open_hour)
         new_day = false;
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void v_calc() // РАСЧЕТ ВОЛАТИЛЬНОСТИ
  {
   double h;   // максимальная цена бара
   double l;   // минимальная цена бара
   double av=0;  // сумма всей волатильности за период

                 // расчет средней волатильности за период
   for(int i=1; i<=d; i++)
     {
      h = NormalizeDouble (iHigh(NULL, PERIOD_D1, i), Digits);
      l = NormalizeDouble (iLow(NULL, PERIOD_D1, i), Digits);
      av= av+((h-l)/Point());
     }

   d_average=(int)(av/d);

   Print("d_average = ",d_average);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void sl_tp_or_calc() // РАСЧЕТ СТОПА И ПРОФИТА
  {
   sl = d_average * sl_rate / 100;
   tp = d_average * tp_rate / 100;

   or=sl/2;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int send_orders() // УСТАНОВКА ОРДЕРОВ
  {
   double price_up = -1.0; // уровень выше цены
   double price_dn = -1.0; // уровень ниже цены
   double tp_up = -1.0;    // тейк верхнего уровня
   double tp_dn = -1.0;    // тейк нижнего уровня

   RefreshRates();

// расчет уровней
   price_up = Bid + or * Point();
   price_dn = Bid - or * Point();
   tp_up    = price_up + tp * Point();
   tp_dn    = price_dn - tp * Point();

   Print ("price_up = ", price_up);
   Print ("price_dn = ", price_dn);
   Print ("tp_up = ", tp_up);
   Print ("tp_dn = ", tp_dn);

// установка ордера BUYSTOP
   if(OrderSend(Symbol(),OP_BUYSTOP,lot,price_up,5,price_dn,tp_up,NULL,0,0,clrNavy)==-1)
     {
      return(666);
     }

// установка ордера SELLSTOP
   if(OrderSend(Symbol(),OP_SELLSTOP,lot,price_dn,5,price_up,tp_dn,NULL,0,0,clrRed)==-1)
     {
      return(666);
     }

   new_day=false;
   err=0;

   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int error() // ФУНКЦИЯ ОБРАБОТКИ ОШИБОК
  {
   if(err>=5)
     {
      close_all_orders();
      err=0;
      return(666);
     }
   else
     {
      close_all_orders();
      err++;
      return(1);
     }

  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void close_all_orders() // ЗАКРЫТЬ И УДАЛИТЬ ВСЕ ОРДЕРА
  {
   int   tip=-1;        // типа выбранного ордера
   bool  cl=true;  // результат закрытия всех ордеров
   int   i= 5;          // количество попыток закрытия ордеров
   int   tick = -1;     // номер ордера

   while(OrdersTotal()>0 && i>0)
     {
      RefreshRates();

      if(OrderSelect(0,SELECT_BY_POS)==true)
        {
         tip=OrderType();
         tick=OrderTicket();

         switch(tip)
           {
            case 0: cl = OrderClose(tick, OrderLots(), Bid, 5); break;
            case 1: cl = OrderClose(tick, OrderLots(), Ask, 5); break;
            case 2: cl = OrderDelete(tick); break;
            case 3: cl = OrderDelete(tick); break;
            case 4: cl = OrderDelete(tick); break;
            case 5: cl = OrderDelete(tick); break;
           }
        }
      else
        {
         Print("ОШИБКА ВЫБОРА ОРДЕРА");
         cl=false;
        }

      if(cl==false)
         i--;

     }

   if(cl==false)
     {
      Print("ОШИБКА ЗАКРЫТИЯ ВСЕХ ОРДЕРОВ");
      return;
     }
  }
//+------------------------------------------------------------------+
