//+------------------------------------------------------------------+
//|                                               Универсал_1.64.mq4 |
//|                                                            Drknn |
//|                   02.03.2007                       drknn@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Drknn"
#property link      "drknn@mail.ru"
//+-------------------------------------------------------------------------------------------------+
//|   Советник умеет:                                                                               |
//| - Тралить ордера любого типа (как рыночные, так и отложенные)                                   |  
//| - Пипсовать                                                                                     |
//| - Ловить увеличение депозита на нужное количество процентов и, поймав его                       |
//|   сообщить пользователю, что депозит на заданное количество процентов увеличен.                 |
//| - Устанавливать те отложенные ордера, которые разрешил пользователь, если советник вдруг        |
//|   обнаружит, что ордера такого в рынке нет (например, ордер сработал - стал рыночным)           |
//| - Показывает на экране что и как у него настрено, чтоб каждый раз не лазить в настройки         |
//|                                                                                                 |
//| - Советник задумывался как универсальный трейлинг, а всё остальное добавлено лишь для удобства. |
//| - Ненужные функции можно легко отключить в окне свойств советника.                              |
//| - Если пользователь разрешил советнику устанавливать отложенный ордер какого либо типа,         |
//|   то этот ордер будет установлен от текущей цены на расстоянии _Step для ордеров данного типа.  |
//+-------------------------------------------------------------------------------------------------+
// ================ Параметры, настраиваимые пользователем ===========================
// ----- Параметры, общие для всех ордеров --------------------------------------------
extern string   t1="------ У открытых вручную MAGIC = 0 ------";
extern int      MAGIC=0;              //У ордеров открытых вручную MAGIC=0 
extern double   Lot=0.2;              //Лот для установки ордера
// ----- Разрешение установить тот или иной ордер ------------------------------------- 
extern string   t2="--- Выключатели отложенных ---";
extern bool     WaitClose=true;       //Если true, то отложенный встанет только тогда, когда закроется рыночный
                                      //иначе новый будет выставлен как только отложенный сработает
extern bool     Ustan_BuyStop=true;   //можно ли ставиь Бай-Cтоп если в рынке такого у нас нет
extern bool     Ustan_SellLimit=false;//можно ли ставиь Cелл-Лимит если в рынке такого у нас нет
extern bool     Ustan_SellStop=true;  //можно ли ставиь Cелл-Cстоп если в рынке такого у нас нет
extern bool     Ustan_BuyLimit=false; //можно ли ставиь Бай-Лимит если в рынке такого у нас нет
// ----- Параметры рыночных ордеров ---------------------------------------------
extern string   t3="--- Параметры рыночных ---";
extern int      ryn_MaxOrderov=2;     //ограничение максимального числа рыночных ордеров одного типа
extern int      ryn_TakeProfit=200;   //Тейк-Профит  рыночного ордера 
extern int      ryn_StopLoss=100;     //Стоп-Лосс  рыночного ордера
extern int      ryn_TrStop=100;       //Трейлинг-Стоп рыночного ордера. Если = 0 то тарла нет
extern int      ryn_TrStep=10;        //Шаг трала рыночного ордера
extern bool     WaitProfit=true;      // Если true, то ждать профит = значению TrailingStop и только потом начинать тралить
                                      //Иначе, трейлинговать не дожидаясь положительного профита
// ----- Параметры стоповых ордеров ---------------------------------------------
extern string   t4="--- Параметры стоповых ---";
extern int      st_Step=50;           //Расстояние в пунктах от уровня текущей цены до уорвня установки стопового ордера
extern int      st_TakeProfit=200;    //Тейк-Профит  стоповых ордеров
extern int      st_StopLoss=100;      //Стоп-Лосс  стоповых ордеров
extern int      st_TrStop=0;          //Трейлинг-Стоп стоповых ордеров. Если = 0 то тарла нет и st_TrStep не важен
extern int      st_TrStep=3;          //Шаг трала стоповых ордеров
// ----- Параметры лимитных ордеров ---------------------------------------------
extern string   t5="--- Параметры лимитных ---";
extern int      lim_Step=50;          //Расстояние в пунктах от уровня текущей цены до уорвня установки лимитного ордера
extern int      lim_TakeProfit=200;   //Тейк-Профит лимитных ордеров 
extern int      lim_StopLoss=100;     //Стоп-Лосс лимитных ордеров
extern int      lim_TrStop=0;         //Трейлинг-Стоп лимитных ордеров. Если = 0 то тарла нет и lim_TrStep не важен
extern int      lim_TrStep=3;         //Шаг трейлинга лимитных ордеров
//------ Открыть (устаовить) ордера в заданное время ----------------------------------------------------------
extern string   t6="--- Только для работы по времени ---";
extern bool     UseTime=true;         //вкл/выкл привязку к указанноу времени. Если=false, то все таймовые параметры не важны, 
                                      //а именно, параметры: Hhour, Mminute, Popravka_Hhour, TIME_Buy, TIME_Sell, TIME_BuyStop,
                                      //TIME_SellLimit, TIME_SellStop, TIME_BuyLimit.
extern int      Hhour=23;             //терминальные часы совершения сделки
extern int      Mminute=59;           //терминальные минуты совершения сделки
extern bool     TIME_Buy=false;       //вкл/выкл открываться ли на покупку в заданное время
extern bool     TIME_Sell=false;      //вкл/выкл открываться ли на продажу
extern bool     TIME_BuyStop=true;    //можно ли установить БайCтоп в заданное время
extern bool     TIME_SellLimit=false; //можно ли установить CеллЛимит в заданное время
extern bool     TIME_SellStop=true;   //можно ли установить CеллCстоп в заданное время
extern bool     TIME_BuyLimit=false;  //можно ли установить БайЛимит в заданное время
// ----- Параметры пипсовки ----------------------------------------------------
extern string   t7="--- Пипсовка ---";
extern int      PipsProfit=0;          //профит при пипсовке можно ставить 1, 2, 3, ...
extern int      Proskalz=3;            //Проскальзывание в пунктах (нужно только когда PipsProfit>0)
// -----------   Отлавливаем увеличение депозита --------------------------
extern string   t8="--- Глобальные уровни ---";
extern bool     UseGlobalLevels=true;  //отловить увеличение и уменьшение депозита заданное число процентов
                                       //если UseGlobalLevels=false, то значениt Global_TakeProfit и Global_StopLoss не важно.
extern double   Global_TakeProfit=2.0; //Глобальный Тейк-Профит (задаётся в процентах)
extern double   Global_StopLoss=2.0;   //Глобальный Стоп-Лосс (задаётся в процентах)
// ----- Прочие установки ------------------------------------------------------ 
extern string   t9="--- Прочие параметры ---";
extern bool     UseOrderSound=true;             // Использовать звуковой сигнал для установки ордеров
extern bool     UseTrailingSound=true;          // Использовать звуковой сигнал для трейлинга
extern string   NameOrderSound ="alert.wav";    // Наименование звукового файла для ордеров
extern string   NameTrallingSound ="expert.wav";// Наименование звукового файла для трейлинга
// ================== Глобальные переменные ===============================================================  
string     Comm1,Comm2,Comm3,Comm4,Comm5,Comm6,Comm7,ED,SMB;
double     PNT,NewPrice,SL,TP,Balans,Free;
int        MinLevel,i,SchBuyStop,SchSellStop,SchBuyLimit,SchSellLimit,SchSell,SchBuy,SBid,SAsk,BBid,BAsk,GTP,GSL,GLE,total;
bool       fm,Rezult,TrailBuyStop,TrailSellStop,TrailBuyLimit,TrailSellLimit,SigBuy,SigSell,NewOrder;
bool       SigTIME_Buy,SigTIME_Sell,SigTIME_BuyStop,SigTIME_SellLimit,SigTIME_SellStop,SigTIME_BuyLimit;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   if(!IsExpertEnabled())//если ложь 
   {
      Alert("Ошибка! Не нажата кнопка *Советники*"); Comment("Ошибка! Не нажата кнопка *Советники*"); return(0);
   }
   else 
   {
      Comment("Как только цена изменится, Советник начнёт работу."); Print("Как только цена изменится, Советник начнёт работу.");
   }
   SMB=Symbol();//Символ валютной пары
   PNT=MarketInfo(SMB,MODE_POINT);//Размер пункта в валюте котировки. Для текущего инструмента хранится в предопределенной переменной Point
   MinLevel=MarketInfo(SMB,MODE_STOPLEVEL);//Минимально допустимый уровень стоп-лосса/тейк-профита в пунктах
   Proverka();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   RefreshRates();
//----Первичные проверки данных
   if(!IsTradeAllowed())
     {
      Comment("Торговля запрещена в настройках терминала, либо торговый поток занят");
      return(0);
     }
   Proverka();
//----Конец первичных проверок данных
   if(ryn_TrStop>0 && ryn_TrStop>=MinLevel) Comm1="Трал рыночных - Вкл."; else Comm1="Трал рыночных - Откл.";
   if (lim_TrStop>0 && lim_TrStop>=MinLevel) Comm2="Трал лимитных - Вкл."; else Comm2="Трал лимитных - Откл.";
   if (st_TrStop>0 && st_TrStop>=MinLevel) Comm3="Трал стоповых - Вкл."; else Comm3="Трал стоповых - Откл.";
   if (PipsProfit>0) Comm4="Пипсовка - Вкл"; else Comm4="Пипсовка - Откл";
   double OtlTP=(Balans/100*Global_TakeProfit+Balans);
   double OtlSL=(Balans-Balans/100*Global_StopLoss);
   GTP=MathCeil(OtlTP);
   GSL=MathCeil(OtlSL);
   if (UseGlobalLevels)
     {
      Comm5="- - - - Глобальные уровни - - - -";
      Comm6="Глобальный Тейк-Профит = "+GTP+" $";
      Comm7="Глобальный Стоп-Лосс   = "+GSL+" $";
     }
   else 
   {
      Comm5="Глобальные уровни - Откл"; Comm6=""; Comm7="";
   }
   SchOrders();
   SMB=Symbol();
   Comment("Ордеров сейчас для ",SMB," :","\n","Buy = ",SchBuy,"       Sell = ",SchSell,"\n","BuyStop = ",SchBuyStop,
          "   SellLimit = ",SchSellLimit,"\n","SellStop = ",SchSellStop,"    BuyLimit = ",SchBuyLimit,"\n",Comm1,
          "\n",Comm2,"\n",Comm3,"\n",Comm4,"\n",Comm5,"\n",Comm6,"\n",Comm7);
   // ========================== Установка отложенных ордеров ====================================
   if(Ustan_BuyStop || Ustan_SellLimit || Ustan_SellStop || Ustan_BuyLimit) {UstanOtlozh();}
   //=============================================================================================
   //==========================  Работа по заданному времени   =======================
   if(UseTime)
     {
      SigTIME_Buy=false;  SigTIME_BuyStop=false;   SigTIME_SellStop=false;
      SigTIME_Sell=false; SigTIME_SellLimit=false; SigTIME_BuyLimit=false;
      if(Hour()==Hhour && Minute()==Mminute)//если текущие час и минута совпадают
        {
         if(TIME_Buy)
         {SigTIME_Buy=true; UstanRyn();}
         if(TIME_Sell)
         {SigTIME_Sell=true; UstanRyn();}
         if(TIME_BuyStop)
         {SigTIME_BuyStop=true; UstanOtlozh();}
         if(TIME_SellLimit)
         {SigTIME_SellLimit=true; UstanOtlozh();}
         if(TIME_SellStop)
         {SigTIME_SellStop=true; UstanOtlozh();}
         if(TIME_BuyLimit)
         {SigTIME_BuyLimit=true; UstanOtlozh();}
        }
     }
   //============== Отлавливаем увеличение депозита на Pojmat процентов =========================
   if(UseGlobalLevels)//Если разрешено отловить процент увеличения/уменьшения депозита
     {
      Balans=AccountBalance();//Баланс счёта
      Free=AccountEquity();//Текущее количество денег в статье "Средства"
      if ((Free-Balans)>=(Balans/100*Global_TakeProfit))
        {
         Print("Депозит увеличен на ",Global_TakeProfit," процентов. Суммарный профит = ",Free);
         Alert("Депозит увеличен на ",Global_TakeProfit," процентов. Суммарный профит = ",Free);
        }
      if ((Balans-Free)>=(Balans/100*Global_StopLoss))
        {
         Print("Депозит уменьшен на ",Global_StopLoss," процентов. Суммарный Стоп-Лосс = ",Free);
         Alert("Депозит уменьшен на ",Global_StopLoss," процентов. Суммарный Стоп-Лосс = ",Free);
        }
     }
   //=================Начало пипсовки==========================
   if (PipsProfit>0)
     {
      SMB=Symbol();
      for(int i=OrdersTotal()-1; i>=0; i-- )
        {//Начало цикла
         if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true)
           {//начало работы с выбранным ордером
            if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) continue;
            if(OrderType()==OP_BUY)
              {
               if(Bid>=(OrderOpenPrice()+PipsProfit*Point))
               {OrderClose(OrderTicket(),OrderLots(),Bid,Proskalz);}
              }
            if(OrderType()==OP_SELL)
              {
               if(Ask<=(OrderOpenPrice() - PipsProfit*Point))
                  OrderClose(OrderTicket(),OrderLots(),Ask,Proskalz);
              }
           } // конец работы с выбранным ордером  
        }// Конец цикла
     }
   //================Конец пипсовки ======================================
   // ================= Трейлинг Рыночных ордеров ==================================================================================
   RefreshRates(); SchOrders();  SMB=Symbol();
   if(ryn_TrStop>=MinLevel && ryn_TrStep>0 && (SchBuy>0 || SchSell>0))
     {
      for(i=0; i<OrdersTotal(); i++)
        {
         if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
           {
            if (OrderSymbol()==SMB && OrderMagicNumber()==MAGIC)
              {
               TrailingPositions();
              }
           }
        }
     }
   if(ryn_TrStop>=MinLevel && ryn_TrStep==0)
      Alert("Трейлинг невозможен - ryn_TrStep==0");
   // ===============================================================================================================================
   //============ Трейлинг отложенных ордеров =============================================================
   RefreshRates();  SchOrders();//Обновляем счётчики количества ордеров
   SMB=Symbol();
   if((st_TrStop>0 && SchBuyStop+SchSellStop>0) || (SchBuyLimit+SchSellLimit>0 && lim_TrStop>0))
     {
      TrailBuyStop=false; TrailSellStop=false; TrailBuyLimit=false; TrailSellLimit=false;
//----
      for(i=OrdersTotal()-1;i>=0;i--)
        {//Начало цикла
         if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
         if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true)
           {//начало работы с выбранным ордером
            if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC || OrderType()==OP_BUY || OrderType()==OP_SELL) continue;
            if(OrderType()==OP_BUYSTOP) // Он наверху и едет вниз
              {
               if(Ask<OrderOpenPrice()-(st_TrStop+st_TrStep)*Point)
                  TrailBuyStop=true;
              }
            if(OrderType()==OP_SELLLIMIT) // Он наверху и едет вниз
              {
               if(Bid<OrderOpenPrice()-(st_TrStop+st_TrStep)*Point)
                  TrailSellLimit=true;
              }
            if(OrderType()==OP_SELLSTOP) // Он внизу и едет вверх
              {
               if(Bid>OrderOpenPrice()+(st_TrStop+st_TrStep)*Point)
                  TrailSellStop=true;
              }
            if(OrderType()==OP_BUYLIMIT) // Он внизу и едет вверх
              {
               if(Ask>OrderOpenPrice()+(st_TrStop+st_TrStep)*Point)
                  TrailBuyLimit=true;
              }
           }//конец работы с выбранным ордером 
        }//Конец цикла
      if (TrailSellLimit || TrailBuyLimit || TrailSellStop || TrailBuyStop)   TrailingOtlozh();
     }
//----
   return(0);
  }
//+ ========================== Конец работы советника ========================================================================== +
// ___________________________________________________________________________________________
//|                                                                                           |
//|                                                                                           |
//|                       Далее идут подпрограммы (функции),                                  |
//|                  которые в случае надобности вызываются из тела советника                 |
//|                                                                                           |
//|___________________________________________________________________________________________|

//===================== Установка отложенных ордеров ===========================================================================
// Функция UstanOtlozh() устанавливает нужный отложенный ордер 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UstanOtlozh()
  {
   RefreshRates(); SchOrders(); SMB=Symbol();
   if(
      (SchSellStop==0 && (SchSell<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_SellStop && st_Step>=MinLevel) || (SigTIME_SellStop && st_Step>=MinLevel))
     )
     {
      NewPrice=Bid-st_Step*Point;
      if(st_StopLoss==0) SL=0.0000;
      else SL=NewPrice+st_StopLoss*Point;
      if(st_TakeProfit==0) TP=0.0000;
      else TP=NewPrice-st_TakeProfit*Point;
      fm=OrderSend(SMB,OP_SELLSTOP,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*Сигналим*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*Комментируем*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_SellStop=false;
         Print("Ордер SellStop установлен");
         Comment("Ордер SellStop установлен");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//Если установить не удалось
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("Ошибка № ",GLE," установки SellStop-ордера");
         Print ("Описание ошибки - ",ED);
        }
     }
   if(
      (SchBuyStop==0 && (SchBuy<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_BuyStop && st_Step>=MinLevel) || (SigTIME_BuyStop && st_Step>=MinLevel))
     )
     {
      NewPrice=Ask+st_Step*Point;
      if(st_StopLoss==0) SL=0.0000;
      else SL=NewPrice-st_StopLoss*Point;
      if(st_TakeProfit==0) TP=0.0000;
      else TP=NewPrice+st_TakeProfit*Point;
      fm=OrderSend(SMB,OP_BUYSTOP,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*Сигналим*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*Комментируем*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_BuyStop=false;
         Print("Ордер BuyStop установлен");
         Comment("Ордер BuyStop установлен");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//Если установить не удалось
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("Ошибка № ",GLE," установки BuyStop-ордера");
         Print ("Описание ошибки - ",ED);
        }
     }
   if(
      (SchBuyLimit==0 && (SchBuy<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_BuyLimit && lim_Step>=MinLevel) || (SigTIME_BuyLimit && lim_Step>=MinLevel))
     )
     {
      NewPrice=Ask-lim_Step*Point;
      if(lim_StopLoss==0) SL=0.0000;
      else SL=NewPrice-lim_StopLoss*Point;
      if(lim_TakeProfit==0) TP=0.0000;
      else TP=NewPrice+st_TakeProfit*Point;
      fm=OrderSend(SMB,OP_BUYLIMIT,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*Сигналим*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*Комментируем*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_BuyLimit=false;
         Print("Ордер BuyLimit установлен");
         Comment("Ордер BuyLimit установлен");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//Если установить не удалось
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("Ошибка № ",GLE," установки BuyLimit-ордера");
         Print ("Описание ошибки - ",ED);
        }
     }
   if(
      (SchSellLimit==0 && (SchSell<ryn_MaxOrderov || !WaitClose))
      && ((Ustan_SellLimit && lim_Step>=MinLevel) || (SigTIME_SellLimit && lim_Step>=MinLevel))
     )
     {
      NewPrice=Bid+lim_Step*Point;
      if(lim_StopLoss==0) SL=0.0000;
      else SL=NewPrice+lim_StopLoss*Point;
      if(lim_TakeProfit==0) TP=0.0000;
      else TP=NewPrice-lim_TakeProfit*Point;
      fm=OrderSend(SMB,OP_SELLLIMIT,Lot,NewPrice,3,SL,TP,NULL,MAGIC,0,CLR_NONE);
/*Сигналим*/            if(fm!=0 && fm!=-1 && UseOrderSound) PlaySound(NameOrderSound);
/*Комментируем*/        if(fm!=0 && fm!=-1)
        {
         SigTIME_SellLimit=false;
         Print("Ордер SellLimit установлен");
         Comment("Ордер SellLimit установлен");
         Sleep(5000); RefreshRates();
        }
      if(fm==0 || fm==-1)//Если установить не удалось
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("Ошибка № ",GLE," установки SellLimit-ордера");
         Print ("Описание ошибки - ",ED);
        }
     }
  }
// =============== Открытие рыночных ордеров ===============================================================
//   Функция UstanRyn() открывает рыночные ордера
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void UstanRyn()
  {
   bool NewOrderSell,NewOrderBuy; SMB=Symbol();
   int  OldTimeBuy,OldTimeSell;
   RefreshRates(); SchOrders();
   //==== Контроль времени последнего открытого ордера ============
   //  Этот блок контроля нужен для того, чтобы на одной свече открывалась только одна сделка
   //  Если в работе по заданному времени будет стоять разрешение открыть и Buy и Sell ордер,
   //  то если советник успеет за тиками, он откроет обе сделки на одной сече 
   for( i=OrdersTotal()-1; i>0; i-- )
     {//Начало цикла
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)==true)
        {//начало работы с выбранным ордером
         if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) {continue;}
         if(OrderType()==OP_BUY)
           {
            if(OrderOpenTime()>=OldTimeBuy)//если время открытия у этого ордера больше чем у последнего открытого, то...
            {OldTimeBuy=OrderOpenTime();}//запоминаем время последнего открытого Buy-ордера
           }
         if(OrderType()==OP_SELL)
           {
            if(OrderOpenTime()>=OldTimeSell)//если время открытия у этого ордера больше чем у последнего открытого, то...
            {OldTimeSell=OrderOpenTime();}//запоминаем время последнего открытого Sell-ордера
           }
        }//Конец работы с выбранным ордером
     }//Конец цикла  OldTimeBuy,OldTimeSell
   if(OldTimeBuy>=Time[0]) {NewOrderBuy=false;}
   if(OldTimeBuy<Time[0])  {NewOrderBuy=true;}
   if(OldTimeSell>=Time[0]) {NewOrderSell=false;}
   if(OldTimeSell<Time[0])  {NewOrderSell=true;}
   //==== Конец "Контроль времени последнего открытого ордера" ====  
   if (NewOrderBuy && SigTIME_Buy && SchBuy==0)
     {//Если можно открыть Buy-ордер           
      if(ryn_StopLoss==0) {SL=0.0000;}
      else {SL=Ask-ryn_StopLoss*Point;}
      if(ryn_TakeProfit==0) {TP=0.0000;}
      else {TP=Ask+ryn_TakeProfit*Point;}
      fm=OrderSend(SMB,OP_BUY,Lot,Ask,3,SL,TP,NULL,MAGIC,0,Blue);
      if(fm!=0 && fm!=-1 && UseOrderSound) {PlaySound(NameOrderSound);}
      if(fm!=0 && fm!=-1)
        {
         SigTIME_Buy=false;
         Comment("Сделка на покупку открыта");
         Print("Сделка на покупку открыта");
        }
      if (fm==-1 || fm==0)
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("Ошибка № ",GLE," установки Buy-ордера");
         Print ("Описание ошибки - ",ED);
        }
     }//Конец "Если можно открыть Buy-ордер"
   if(NewOrderSell && SigTIME_Sell && SchSell==0)
     { //Если можно открыть Sell-ордер 
      if(ryn_StopLoss==0) {SL=0.0000;}
      else {SL=Bid+ryn_StopLoss*Point;}
      if(ryn_TakeProfit==0) {TP=0.0000;}
      else {TP=Bid-ryn_TakeProfit*Point;}
      fm=OrderSend(SMB,OP_SELL,Lot,Bid,3,SL,TP,NULL,MAGIC,0,Red);
      if(fm!=0 && fm!=-1 && UseOrderSound) {PlaySound(NameOrderSound);}
      if(fm!=0 && fm!=-1)
        {
         SigTIME_Sell=false;
         Comment("Сделка на продажу открыта ");
         Print("Сделка на продажу открыта ");
        }
      if (fm==-1 || fm==0)
        {
         GLE=GetLastError();
         ED=ErrorDescription(GLE);
         Print("Ошибка № ",GLE," установки SellL-ордера");
         Print ("Описание ошибки - ",ED);
        }
     }//Конец "Если можно открыть Sell-ордер"
  }
//=========== Счётчики количества ордеров  =================================================================

//  Функция SchOrders() проходит по всем ордерам и подсчитывает, сколько сейчас имеется ордеров каждого типа.
//  Как только понадобятся значение того или иного счётчика, нужно сначала вызвать функцию SchOrders()
//  Она обновит данные и потом можно смело вызывать тот или иной счётчик.
//  SchBuyStop  - счётчик ордеров BuyStop
//  SchSellStop - счётчик ордеров SellStop
//  SchBuyLimit - счётчик ордеров BuyLimit
//  SchSellLimit  счётчик ордеров SellLimit
//  SchBuy      - счётчик Buy ордеров
//  SchSell     - счётчик Sell ордеров
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void SchOrders()
  {
   // для начала работы счётчиков мы их обнуляем
   SchBuyStop=0; SchSellStop=0; SchBuyLimit=0; SMB=Symbol();
   SchBuy=0; SchSell=0; SchSellLimit=0;
   for(i=OrdersTotal()-1;i>=0;i--)
     {//Начало цикла
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {//начало работы с выбранным ордером
         if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) continue;
         if(OrderType()==OP_BUYSTOP)
            SchBuyStop++;
         if(OrderType()==OP_SELLSTOP)
            SchSellStop++;
         if(OrderType()==OP_SELLLIMIT)
            SchSellLimit++;
         if(OrderType()==OP_BUYLIMIT)
            SchBuyLimit++;
         if(OrderType()==OP_BUY)
            SchBuy++;
         if(OrderType()==OP_SELL)
            SchSell++;
        }//конец работы с выбранным ордером
     }//Конец цикла
  }
//+------------------------------------------------------------------+
//| Сопровождение позиции простым тралом                             |
//+------------------------------------------------------------------+
void TrailingPositions()
  {
   if(OrderType()==OP_BUY)
     {
      if(!WaitProfit || (Bid-OrderOpenPrice())>ryn_TrStop*Point)
        {
         if (OrderStopLoss()<Bid-(ryn_TrStop+ryn_TrStep-1)*Point)
         {ModifyStopLoss(Bid-ryn_TrStop*Point);}
        }
     }
   if(OrderType()==OP_SELL)
     {
        if(!WaitProfit || OrderOpenPrice()-Ask>ryn_TrStop*Point) {
         if(OrderStopLoss()>Ask+(ryn_TrStop+ryn_TrStep-1)*Point || OrderStopLoss()==0)
         {ModifyStopLoss(Ask+ryn_TrStop*Point);}
        }
     }
  }
//+------------------------------------------------------------------+
//| Перенос уровня StopLoss                                          |
//| Параметры:                                                       |
//|   ldStopLoss - уровень StopLoss                                  |
//+------------------------------------------------------------------+
void ModifyStopLoss(double ldStopLoss)
  {
   fm=OrderModify(OrderTicket(),OrderOpenPrice(),ldStopLoss,OrderTakeProfit(),0,CLR_NONE);
   if(fm!=0 && fm!=-1 && UseTrailingSound) PlaySound(NameTrallingSound);
   if(fm==0 || fm==-1) {ModifError();}
  }
//======== Подтягивание отложенного ордера вслед ценой  ============================================
// Функция TrailingOtlozh() подтягивает отложенный ордер вслед за ценой
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void TrailingOtlozh()
  {
   RefreshRates(); SMB=Symbol();
   for(i=OrdersTotal()-1;i>=0;i--)//Цикл. Проходим по всем ордерам
     {//Начало цикла
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {WriteError();}
      if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
        {//начало работы с выбранным ордером
         if(OrderSymbol()!=SMB || OrderMagicNumber()!=MAGIC) {continue;}
         if(OrderType()==OP_BUYSTOP)//находится вверху, едет вниз
           {
            if(TrailBuyStop)
              {
               NewPrice=Ask+st_TrStop*Point;
               if(st_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice-st_StopLoss*Point;}
               if(st_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice+st_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1) {ModifError();}
              }
           }
         if(OrderType()==OP_SELLSTOP) // Находится внизу, едет вверх
           {
            if(TrailSellStop)
              {
               NewPrice=Bid-st_TrStop*Point;
               if(st_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice+st_StopLoss*Point;}
               if(st_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice-st_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1)  {ModifError();}
              }
           }
         if(OrderType()==OP_BUYLIMIT) // Находится внизу, едет вверх
           {
            if(TrailBuyLimit)
              {
               NewPrice=Ask-st_TrStop*Point;
               if(lim_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice-lim_StopLoss*Point;}
               if(lim_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice+lim_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1) {ModifError();}
              }
           }
         if(OrderType()==OP_SELLLIMIT)//находится вверху, едет вниз
           {
            if(TrailSellLimit)
              {
               NewPrice=Bid+st_TrStop*Point;
               if(lim_StopLoss==0) {SL=0.0000;}
               else {SL=NewPrice+lim_StopLoss*Point;}
               if(lim_TakeProfit==0) {TP=0.0000;}
               else {TP=NewPrice-lim_TakeProfit*Point;}
               fm=OrderModify(OrderTicket(),NewPrice,SL,TP,0,CLR_NONE);
               if(fm!=0 && fm!=-1 && UseTrailingSound) {PlaySound(NameTrallingSound);}
               if(fm!=0 && fm!=-1) {Sleep(5000); RefreshRates();}
               if(fm==0 || fm==-1) {ModifError();}
              }
           }
        }//конец работы с выбранным ордером
     }//Конец цикла
  }//конец функции
//========= Словестное описание ошибок =========================================================================================
// Функция возвращает не код ошибки а её словестное описание
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string ErrorDescription(int error_code)
  {
   string error_string;
//----
   switch(error_code)
     {
      //---- codes returned from trade server
      case 0:    error_string=" Нет ошибки"; break;
      case 1:   error_string=" Нет ошибки, но результат неизвестен"; break;
      case 2:   error_string=" Общая ошибка"; break;
      case 3:    error_string=" Неправильные параметры"; break;
      case 4:    error_string=" Торговый сервер занят"; break;
      case 5:    error_string=" Старая версия клиентского терминала"; break;
      case 6:    error_string=" Нет связи с торговым сервером"; break;
      case 7:    error_string=" Недостаточно прав"; break;
      case 8:    error_string=" Слишком частые запросы"; break;
      case 9:    error_string=" Недопустимая операция нарушающая функционирование сервера"; break;
      case 64:    error_string=" Счет заблокирован"; break;
      case 65:    error_string=" Неправильный номер счета"; break;
      case 128:    error_string=" Истек срок ожидания совершения сделки"; break;
      case 129:    error_string=" Неправильная цена"; break;
      case 130:    error_string=" Неправильные стопы"; break;
      case 131:    error_string=" Неправильный объем"; break;
      case 132:    error_string=" Рынок закрыт"; break;
      case 133:    error_string=" Торговля запрещена"; break;
      case 134:    error_string=" Недостаточно денег для совершения операции"; break;
      case 135:    error_string=" Цена изменилась"; break;
      case 136:    error_string=" Нет цен"; break;
      case 137:    error_string=" Брокер занят"; break;
      case 138:    error_string=" Новые цены"; break;
      case 139:    error_string=" Ордер заблокирован и уже обрабатывается"; break;
      case 140:    error_string=" Разрешена только покупка"; break;
      case 141:    error_string=" Слишком много запросов"; break;
      case 145:    error_string=" Модификация запрещена, так как ордер слишком близок к рынку"; break;
      case 146:    error_string=" Подсистема торговли занята"; break;
      case 147:    error_string=" Использование даты истечения ордера запрещено брокером"; break;
      case 148:    error_string=" Количество открытых и отложенных ордеров достигло предела, установленного брокером"; break;
      case 4000:    error_string=" Нет ошибки"; break;
      case 4001:    error_string=" Неправильный указатель функции"; break;
      case 4002:    error_string=" Индекс массива - вне диапазона"; break;
      case 4003:    error_string=" Нет памяти для стека функций"; break;
      case 4004:    error_string=" Переполнение стека после рекурсивного вызова"; break;
      case 4005:    error_string=" На стеке нет памяти для передачи параметров"; break;
      case 4006:    error_string=" Нет памяти для строкового параметра"; break;
      case 4007:    error_string=" Нет памяти для временной строки"; break;
      case 4008:    error_string=" Неинициализированная строка"; break;
      case 4009:    error_string=" Неинициализированная строка в массиве"; break;
      case 4010:    error_string=" Нет памяти для строкового массива"; break;
      case 4011:    error_string=" Слишком длинная строка"; break;
      case 4012:    error_string=" Остаток от деления на ноль"; break;
      case 4013:    error_string=" Деление на ноль"; break;
      case 4014:    error_string=" Неизвестная команда"; break;
      case 4015:    error_string=" Неправильный переход"; break;
      case 4016:    error_string=" Неинициализированный массив"; break;
      case 4017:    error_string=" Вызовы DLL не разрешены"; break;
      case 4018:    error_string=" Невозможно загрузить библиотеку"; break;
      case 4019:    error_string=" Невозможно вызвать функцию"; break;
      case 4020:    error_string=" Вызовы внешних библиотечных функций не разрешены"; break;
      case 4021:    error_string=" Недостаточно памяти для строки, возвращаемой из функции"; break;
      case 4022:    error_string=" Система занята"; break;
      case 4050:    error_string=" Неправильное количество параметров функции"; break;
      case 4051:    error_string=" Недопустимое значение параметра функции"; break;
      case 4052:    error_string=" Внутренняя ошибка строковой функции"; break;
      case 4053:    error_string=" Ошибка массива"; break;
      case 4054:    error_string=" Неправильное использование массива-таймсерии"; break;
      case 4055:    error_string=" Ошибка пользовательского индикатора"; break;
      case 4056:    error_string=" Массивы несовместимы"; break;
      case 4057:    error_string=" Ошибка обработки глобальныех переменных"; break;
      case 4058:    error_string=" Глобальная переменная не обнаружена"; break;
      case 4059:    error_string=" Функция не разрешена в тестовом режиме"; break;
      case 4060:    error_string=" Функция не подтверждена"; break;
      case 4061:    error_string=" Ошибка отправки почты"; break;
      case 4062:    error_string=" Ожидается параметр типа string"; break;
      case 4063:    error_string=" Ожидается параметр типа integer"; break;
      case 4064:    error_string=" Ожидается параметр типа double"; break;
      case 4065:    error_string=" В качестве параметра ожидается массив"; break;
      case 4066:    error_string=" Запрошенные исторические данные в состоянии обновления"; break;
      case 4067:    error_string=" Ошибка при выполнении торговой операции"; break;
      case 4099:    error_string=" Конец файла"; break;
      case 4100:    error_string=" Ошибка при работе с файлом"; break;
      case 4101:    error_string=" Неправильное имя файла"; break;
      case 4102:    error_string=" Слишком много открытых файлов"; break;
      case 4103:    error_string=" Невозможно открыть файл"; break;
      case 4104:    error_string=" Несовместимый режим доступа к файлу"; break;
      case 4105:    error_string=" Ни один ордер не выбран"; break;
      case 4106:    error_string=" Неизвестный символ"; break;
      case 4107:    error_string=" Неправильный параметр цены для торговой функции"; break;
      case 4108:    error_string=" Неверный номер тикета"; break;
      case 4109:    error_string=" Торговля не разрешена"; break;
      case 4110:    error_string=" Длинные позиции не разрешены"; break;
      case 4111:    error_string=" Короткие позиции не разрешены"; break;
      case 4200:    error_string=" Объект уже существует"; break;
      case 4201:    error_string=" Запрошено неизвестное свойство объекта"; break;
      case 4202:    error_string=" Объект не существует"; break;
      case 4203:    error_string=" Неизвестный тип объекта"; break;
      case 4204:    error_string=" Нет имени объекта"; break;
      case 4205:    error_string=" Ошибка координат объекта"; break;
      case 4206:    error_string=" Не найдено указанное подокно"; break;
      case 4207:    error_string=" Ошибка при работе с объектом"; break;
     }
//----
   return(error_string);
  }
// ======================= Вызов номера и описания ошибки выбора  ==========================================
//   Функция WriteError() пишет номер возникшей ошибки выбора и её русское описание
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WriteError()
  {
   GLE=GetLastError();
   ED=ErrorDescription(GLE);
   Print("Ошибка ", GLE, " при выборе ордера номер ",i);
   Print ("Описание ошибки - ",ED);
  }
// ======================= Ошибка модификации =====================================================
//  Функция ModifError() пишет номер возникшей ошибки модификации и её русское описание
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ModifError()
  {
   GLE=GetLastError();
   ED=ErrorDescription(GLE);
   Print("Модификация ордера № ",OrderTicket(), " вернула ошибку № ",GLE);
   Print ("Описание ошибки: ",ED);
  }
// ======================= Проверка корректности пользовательских установок =============================================
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Proverka()
  {
     if(ryn_TrStop<MinLevel && ryn_TrStop!=0) 
     {
      Comment("Ошибка! ТрейлингСтоп рыночных ордеров не может быть менее ",MinLevel);
      Print("Ошибка! ТрейлингСтоп рыночных ордеров не может быть менее ",MinLevel);
      Alert("Ошибка! ТрейлингСтоп рыночных ордеров не может быть менее ",MinLevel);
      return(0);
     }
     if(ryn_TrStop>=MinLevel && ryn_TrStep==0) 
     {
      Comment("Ошибка! Шаг тарала рыночных ордеров не может быть менее 1");
      Print("Ошибка! Шаг тарала рыночных ордеров не может быть менее 1");
      Alert("Ошибка! Шаг тарала рыночных ордеров не может быть менее 1");
      return(0);
     }
     if(ryn_TakeProfit<MinLevel && ryn_TakeProfit!=0) 
     {
      Comment("Ошибка! ryn_TakeProfit не может быть менее ",MinLevel);
      Print("Ошибка! ryn_TakeProfit не может быть менее ",MinLevel);
      Alert("Ошибка! ryn_TakeProfit не может быть менее ",MinLevel);
      return(0);
     }
     if(ryn_StopLoss<MinLevel && ryn_StopLoss!=0) 
     {
      Comment("Ошибка! ryn_StopLoss не может быть менее ",MinLevel);
      Print("Ошибка! ryn_StopLoss не может быть менее ",MinLevel);
      Alert("Ошибка! ryn_StopLoss не может быть менее ",MinLevel);
      return(0);
     }
     if(st_TakeProfit<MinLevel && st_TakeProfit!=0) 
     {
      Comment("Ошибка! st_TakeProfit не может быть менее ",MinLevel);
      Print("Ошибка! st_TakeProfit не может быть менее ",MinLevel);
      Alert("Ошибка! st_TakeProfit не может быть менее ",MinLevel);
      return(0);
     }
     if(st_StopLoss<MinLevel && st_StopLoss!=0) 
     {
      Comment("Ошибка! st_StopLoss не может быть менее ",MinLevel);
      Print("Ошибка! st_StopLoss не может быть менее ",MinLevel);
      Alert("Ошибка! st_StopLoss не может быть менее ",MinLevel);
      return(0);
     }
     if(st_TrStop<MinLevel && st_TrStop!=0) 
     {
      Comment("Ошибка! ТрейлингСтоп стоповых ордеров не может быть менее ",MinLevel);
      Print("Ошибка! ТрейлингСтоп стоповых ордеров не может быть менее ",MinLevel);
      Alert("Ошибка! ТрейлингСтоп стоповых ордеров не может быть менее ",MinLevel);
      return(0);
     }
     if(st_TrStop>=MinLevel && st_TrStep==0) 
     { 
      Comment("Ошибка! шаг тарала стоповых ордеров не может быть менее 1");
      Print("Ошибка! ТрейлингСтоп стоповых ордеров не может быть менее 1");
      Alert("Ошибка! ТрейлингСтоп стоповых ордеров не может быть менее 1");
      return(0);
     }
     if(st_Step<MinLevel)                   
     { 
      Comment("Ошибка! переменная st_Step не может быть менее ",MinLevel);
      Print("Ошибка! переменная st_Step не может быть менее ",MinLevel);
      Alert("Ошибка! переменная st_Step не может быть менее ",MinLevel);
      return(0);
     }
     if(lim_TakeProfit<MinLevel && lim_TakeProfit!=0) 
     {
      Comment("Ошибка! lim_TakeProfit не может быть менее ",MinLevel);
      Print("Ошибка! lim_TakeProfit не может быть менее ",MinLevel);
      Alert("Ошибка! lim_TakeProfit не может быть менее ",MinLevel);
      return(0);
     }
     if(lim_StopLoss<MinLevel && lim_StopLoss!=0) 
     {
      Comment("Ошибка! lim_StopLoss не может быть менее ",MinLevel);
      Print("Ошибка! lim_StopLoss не может быть менее ",MinLevel);
      Alert("Ошибка! lim_StopLoss не может быть менее ",MinLevel);
      return(0);
     }
     if(lim_TrStop<MinLevel && lim_TrStop!=0) 
     {
      Comment("Ошибка! ТрейлингСтоп лимитных ордеров не может быть менее ",MinLevel);
      Print("Ошибка! ТрейлингСтоп лимитных ордеров не может быть менее ",MinLevel);
      Alert("Ошибка! ТрейлингСтоп лимитных ордеров не может быть менее ",MinLevel);
      return(0);
     }
     if(lim_TrStop>=MinLevel && lim_TrStep==0) 
     {
      Comment("Ошибка! Шаг тарала лимитных ордеров не может быть менее 1");
      Print("Ошибка! Шаг тарала лимитных ордеров не может быть менее 1");
      Alert("Ошибка! Шаг тарала лимитных ордеров не может быть менее 1");
      return(0);
     }
     if(lim_Step<MinLevel)                  
     {
      Comment("Ошибка! переменная lim_Step не может быть менее ",MinLevel);
      Print("Ошибка! переменная lim_Step не может быть менее ",MinLevel);
      Alert("Ошибка! переменная lim_Step не может быть менее ",MinLevel);
      return(0);
     }
  }
//+------------------------------------------------------------------+