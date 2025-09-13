//+------------------------------------------------------------------+
//|                                                     JobJobos.mq4 |
//|                                                             Ugar |
//|                                                     Ugar68@bk.ru |
//+------------------------------------------------------------------+
#property copyright "Ugar"
#property link      "Ugar68@bk.ru"
#property strict
#property version   "1.00"

//--- input parameters
extern int       SL=20;
extern int       TP=20;
extern int       Trailing=10;
extern bool      NowBarAfterProfit=true;
extern double    Lot=0.1;
extern int       Slippage=3;
extern int       Magic=120428;
//+------------------------------------------------------------------+
//Переменные на глобальном уровне
string _name;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   _name=WindowExpertName();//Имя советника
   NowBar(0,true);//инициализация функции нового бара
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
//+------------------------------------------------------------------+
//Переменные
   static double Heiken51,Heiken52;
   double Heiken151,Heiken152,Heiken301,Heiken302,Heiken601,Heiken602;
   double lot,sl=0,tp=0,op=0,oop,osl;
   int Signal=0,Spread,StopLevel,total,i,ticket,orders=0,LastHistOrder,cmd=0;
   static int Signal_;
   string SignalStr="нет",SignalStr5,SignalStr15,SignalStr30,SignalStr60,alerts;
   bool OrderSendRun=false,nowbar;
   color arrow=Blue;
//+------------------------------------------------------------------+
//Спред, минимальный стоп и проверка корректности стопов
//Спред
   Spread=(int)MarketInfo(Symbol(),MODE_SPREAD);

//Stoplevel
   StopLevel=(int)MathMax(Spread,MarketInfo(Symbol(),MODE_STOPLEVEL))+Spread+1;

//Проверка на корректность SL
   if(SL<=StopLevel)
     {
      alerts=StringConcatenate("SL дожен быть больше StopLevel ",StopLevel);
      Alert(alerts);
     }

//Проверка на корректность TP
   if(TP<=StopLevel)
     {
      alerts=StringConcatenate("TP дожен быть больше StopLevel ",StopLevel);
      Alert(alerts);
     }
//+------------------------------------------------------------------+
//Новый бар на М5
   nowbar=NowBar(5,false);
//+------------------------------------------------------------------+
//Индикаторы
//На M5 индикатор вызывается один раз за бар
   if(nowbar)
     {
      Heiken51=iCustom(NULL,5,"Heiken Ashi",2,1);
      Heiken52=iCustom(NULL,5,"Heiken Ashi",3,1);
     }

//индикатор на M15
   Heiken151=iCustom(NULL,15,"Heiken Ashi",2,0);
   Heiken152=iCustom(NULL,15,"Heiken Ashi",3,0);

//индикатор на M30
   Heiken301=iCustom(NULL,30,"Heiken Ashi",2,0);
   Heiken302=iCustom(NULL,30,"Heiken Ashi",3,0);

//индикатор на M60
   Heiken601=iCustom(NULL,60,"Heiken Ashi",2,0);
   Heiken602=iCustom(NULL,60,"Heiken Ashi",3,0);
//+------------------------------------------------------------------+
//Сигналы
   SignalStr5=signal(Heiken52,Heiken51);
   SignalStr15=signal(Heiken152,Heiken151);
   SignalStr30=signal(Heiken302,Heiken301);
   SignalStr60=signal(Heiken602,Heiken601);

//Общий сигнал
   if(Heiken51<Heiken52 && Heiken151<Heiken152 && Heiken301<Heiken302 && Heiken601<Heiken602)
     {
      Signal=1;
      SignalStr="Buy";
     }

//Общий сигнал
   if(Heiken51>Heiken52 && Heiken151>Heiken152 && Heiken301>Heiken302 && Heiken601>Heiken602)
     {
      Signal=-1;
      SignalStr="Sell";
     }

//Отображение показаний индикаторов и сигналов
   alerts=StringConcatenate("Heiken Ashi на",
                            "\n","M5   "+SignalStr5,
                            "\n","M15 ",SignalStr15,
                            "\n","M30 ",SignalStr30,
                            "\n","M60 ",SignalStr60,
                            "\n","Общий сигнал ",SignalStr);
   Comment(alerts);
//+------------------------------------------------------------------+
//проверка связи
   if(!DCOk(30))return(0);
//+------------------------------------------------------------------+
//Проверка наличия открытых ордеров и управление ими
//последний номер ордера
   total=OrdersTotal()-1;

//цикл перебора ордеров
   for(i=total; i>=0; i--)
     {
      //Выбор ордера
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("Ордер не выбран, ошибка = ",GetLastError());
         return(0);
        }

      //Если ордер чужого маджика или символа, пропустить.
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic)continue;

      //Цена открытия ордера
      oop=OrderOpenPrice();

      //Стоп ордера
      osl=OrderStopLoss();

      //Управление Buy ордерами
      if(OrderType()==OP_BUY)
        {
         //Есть ордер
         orders++;

         //Если трейлинг разрешон
         if(Trailing>0)
           {
            //стоп для трейлинга
            sl=sltp(Trailing,Bid,-1);

            //Условие для трейлинга
            if(sl>oop+0.5*Point && sl>osl+0.5*Point)
              {
               //Модификация ордера для трейлинга
               if(!OrderModify(OrderTicket(),NormalizeDouble(oop,Digits),
                  NormalizeDouble(sl,Digits),OrderTakeProfit(),0,Blue))
                 {
                  //Если ордер не модифицировался, печать ошибки и выход после паузы
                  Sleep(ErrorTime());
                 }
              }
           }
        }

      //Управление Sell ордерами
      if(OrderType()==OP_SELL)
        {
         //Есть ордер
         orders++;

         //Если трейлинг разрешон
         if(Trailing>0)
           {
            //стоп для трейлинга
            sl=sltp(Trailing,Ask,1);

            //Условие для трейлинга
            if(sl<oop-0.5*Point && sl<osl-0.5*Point)
              {
               //Модификация ордера для трейлинга
               if(!OrderModify(OrderTicket(),NormalizeDouble(oop,Digits),
                  NormalizeDouble(sl,Digits),OrderTakeProfit(),0,Red))
                 {
                  //Если ордер не модифицировался, печать ошибки и выход после паузы
                  Sleep(ErrorTime());
                 }
              }
           }
        }
     }
//+------------------------------------------------------------------+
//Если есть открытые ордера или нет сигнала, выход
//Если есть открытые ордера
   if(orders>0)
     {
      //Сброс сигнала открытия
      Signal_=0;
      return(0);
     }
//Если нет сигнала, выход
   if(Signal==0)return(0);
//+------------------------------------------------------------------+
//Поиск последнего исторического ордера и проверка его профита
//Лот по умолчанию
   lot=Lot;

//Поиск последнего исторического ордера
   LastHistOrder=LastHistotyOrder(Symbol(),Magic);

//если ордер найден
   if(LastHistOrder>=0)
     {
      //Выбор ордера
      int res=OrderSelect(LastHistOrder,SELECT_BY_TICKET);

      //Если прибыль ордера отрицательная то лот по мартингейлу
      if(OrderProfit()<0)lot=OrderLots()*2;

      //Если прибыль >0, есть сигнал, есть новый бар, нет сигнала открытия 
      //и режим NowBarAfterProfit присвоить сигналу открытия сигнал индикатора
      if(OrderProfit()>0 && Signal!=0 && nowbar && Signal_==0 && NowBarAfterProfit)
        {
         Signal_=Signal;
        }
      if(OrderProfit()<0)Signal_=Signal;
     }
//Если исторический ордер не найден или NowBarAfterProfit=false, или профит <0
//присвоить сигналу открытия сигнал индикатора
   if(LastHistOrder<0 || !NowBarAfterProfit)Signal_=Signal;
//+------------------------------------------------------------------+
//проверка связи
   if(!DCOk(30))return(0);
//+------------------------------------------------------------------+
//Открытие ордеров
//Параметры для Buy ордеров
   if(Signal_>0)
     {
      op=Ask;
      sl=sltp(SL,op,-1);
      tp=sltp(TP,op,1);
      cmd=OP_BUY;
      arrow=Blue;
      OrderSendRun=true;
     }

//Параметры для Sell ордеров
   if(Signal_<0)
     {
      op=Bid;
      sl=sltp(SL,op,1);
      tp=sltp(TP,op,-1);
      cmd=OP_SELL;
      arrow=Red;
      OrderSendRun=true;
     }

//Открытие ордера
   if(OrderSendRun)
     {
      ticket=OrderSend(Symbol(),cmd,lot,NormalizeDouble(op,Digits),Slippage,
                       NormalizeDouble(sl,Digits),NormalizeDouble(tp,Digits),_name,Magic,0,arrow);

      //Если ордер не открылся, печать ошибки и выход после паузы
      if(ticket<0)Sleep(ErrorTime());
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//Функция сравнения значений индикатора. Возвращает "белый" если для Buy направления
//"красный" для Sell направления
string signal(double fast,double slow)
  {
   string ret="нет";
   if(fast>slow)ret="белый";
   if(fast<slow)ret="красный";
   return(ret);
  }
//+------------------------------------------------------------------+
//| Функция от Ugar eMail:ugar68@bk.ru                               |
/*+------------------------------------------------------------------+
Функция нового бара. 
Возвращает true при первом вызове функции после появления нового бара заданного тайм фрейма, 
иначе false.
timeframe - таймфрейм
initialization true сброс статической переменной времени, false работа*/
bool NowBar(int timeframe,bool initialization)
  {
   bool ret=false;
   static datetime LastTime;
   datetime TimeOpenBar;
   if(initialization)LastTime=0;
   else
     {
      TimeOpenBar=iTime(NULL,timeframe,0);
      if(LastTime!=TimeOpenBar)ret=true;
      LastTime=TimeOpenBar;
     }
   return(ret);
  }
//+------------------------------------------------------------------+
//|Функция от Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
//Функция печатает ошибку исполнения приказа и возвращает время задержки на повтоное исполнение
//В случае бесполезного повтора попыток возвращает 60000.
int ErrorTime()
  {
   int err=GetLastError();
   int sec= 0,s,c;
   switch(err)
     {
      case    0: sec=0;
      break;
      case    1:
        {
         Print("Ошибка: 1 - попытка изменить уже установленные значения такими же значениями");
         sec=0;
         break;
        }
      case    2:
        {
         Print("Ошибка: 2 - Общая ошибка. Прекратить все попытки торговых операций до выяснения обстоятельств.");
         sec=60000;
         break;
        }
      case    3:
        {
         Print("Ошибка: 3 - В торговую функцию переданы неправильные параметры. Необходимо изменить логику программы.");
         sec=60000;
         break;
        }
      case    4:
        {
         Print("Ошибка: 4 - Торговый сервер занят. Можно повторить попытку через достаточно большой промежуток времени");
         sec=60000;
         break;
        }
      case    5:
        {
         Print("Ошибка: 5 - Старая версия клиентского терминала.");
         sec=60000;
         break;
        }
      case    6:
        {
         Print("Ошибка: 6 - Нет связи с торговым сервером.");
         for(c=0;c<36000;c++)
           {
            if(IsConnected())
              {
               sec=0;
               break;
              }
            Sleep(100);
           }
         if(c==36000)
           {
            sec=5000;
           }
         break;
        }
      case    8:
        {
         Print("Ошибка: 8 - Слишком частые запросы. ");
         sec=60000;
         break;
        }
      case   64:
        {
         Print("Ошибка: 64 - Счет заблокирован.");
         sec=60000;
         break;
        }
      case   65:
        {
         Print("Ошибка: 65 - Неправильный номер счета. ");
         sec=60000;
         break;
        }
      case  128:
        {
         Print("Ошибка: 128 - Истек срок ожидания совершения сделки.");
         sec=60000;
         break;
        }
      case  129:
        {
         Print("Ошибка: 129 - Неправильная цена bid или ask, возможно, ненормализованная цена.");
         sec=5000;
         break;
        }
      case  130:
        {
         Print("Ошибка: 130 - Слишком близкие стопы или неправильно рассчитанные или ненормализованные цены в стопах ");
         sec=5000;
         break;
        }
      case  131:
        {
         Print("Ошибка: 131 - Неправильный объем, ошибка в грануляции объема.");
         sec=60000;
         break;
        }
      case  132:
        {
         Print("Ошибка: 132 - Рынок закрыт.");
         sec=60000;
         break;
        }
      case  133:
        {
         Print("Ошибка: 133 - Торговля запрещена. ");
         sec=60000;
         break;
        }
      case  134:
        {
         Print("Ошибка: 134 - Недостаточно денег для совершения операции.");
         sec=60000;
         break;
        }
      case  135:
        {
         Print("Ошибка: 135 - Цена изменилась.");
         sec=0;
         break;
        }
      case  136:
        {
         Print("Ошибка: 136 - Нет цен. Брокер по какой-то причине (например, в начале сессии цен нет, неподтвержденные цены, быстрый рынок) не дал цен или отказал.");
         sec=5000;
         break;
        }
      case  138:
        {
         Print("Ошибка: 138 - Запрошенная цена устарела, либо перепутаны bid и ask.");
         sec=0;
         break;
        }
      case  139:
        {
         Print("Ошибка: 139 - Ордер заблокирован и уже обрабатывается.");
         sec=0;
         break;
        }
      case  140:
        {
         Print("Ошибка: 140 - Разрешена только покупка.");
         sec=0;
         break;
        }
      case  141:
        {
         Print("Ошибка: 141 - Слишком много запросов.");
         sec=3000;
         break;
        }
      case  142:
        {
         Print("Ошибка: 142 - Ордер поставлен в очередь. Это не ошибка, а один из кодов взаимодействия между клиентским терминалом и торговым сервером. ");
         sec=0;
         break;
        }
      case  143:
        {
         Print("Ошибка: 143 - Ордер принят дилером к исполнению. Один из кодов взаимодействия между клиентским терминалом и торговым сервером.");
         sec=0;
         break;
        }
      case  144:
        {
         Print("Ошибка: 144 - Ордер аннулирован самим клиентом при ручном подтверждении сделки. ");
         sec=30000;
         break;
        }
      case  145:
        {
         Print("Ошибка: 145 - Модификация запрещена, так как ордер слишком близок к рынку и заблокирован из-за возможного скорого исполнения.");
         sec=15000;
         break;
        }
      case  146:
        {
         Print("Ошибка: 146 - Подсистема торговли занята.");
         for(s=0;s<36000;s++)
           {
            if(!IsTradeContextBusy())
              {
               sec=0;
               break;
              }
            Sleep(100);
           }
         if(s==36000)
           {
            sec=60000;
           }
         break;
        }
      case  147:
        {
         Print("Ошибка: 147 - Использование даты истечения ордера запрещено брокером.");
         sec=60000;
         break;
        }
      case  148:
        {
         Print("Ошибка: 148 - Количество открытых и отложенных ордеров достигло предела, установленного брокером.");
         sec=60000;
         break;
        }
      case  149:
        {
         Print("Ошибка: 149 - Попытка открыть противоположную позицию к уже существующей в случае, если хеджирование запрещено.");
         sec=60000;
         break;
        }
      case  150:
        {
         Print("Ошибка: 150 - Попытка закрыть позицию по инструменту в противоречии с правилом FIFO. Сначала необходимо закрыть более ранние существующие позиции ");
         sec=60000;
         break;
        }
      default: Print("Неизвестная ошибка");
      break;
     }
   return(sec);
  }
//+------------------------------------------------------------------+
//|Функция от Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
/*Функция проверяет фозможность выполнения приказов возвращает true если всё в порядке 
если что то не в порядке пишет причину и выдерживает паузу в 0.1 секунды и повторяет проверки
Если в течении заданного количества секунд sec ситуация не нормализовалась возврвщает false
*/
bool DCOk(int sec)
  {
   bool ok=true,conn=true,trade=true;
   int s=sec*10;
   if(IsTesting() || IsOptimization())return(ok);
   for(int n=0;n<s;n++)
     {
      ok=true;
      conn=true;
      trade=true;
      if(!IsConnected())
        {
         //Print("Нет связи с сервером");
         ok=false;
         conn=false;
         Sleep(100);
         continue;
        }
      if(!IsTradeAllowed())
        {
         //Print("Торговый поток занят или советнику запрещена торговля");
         ok=false;
         trade=false;
         Sleep(100);
         continue;
        }
     }
   if(!conn)Print("Нет связи с сервером");
   if(!trade)Print("Торговый поток занят или советнику запрещена торговля");
   if(ok)RefreshRates();
   return(ok);
  }
//+------------------------------------------------------------------+
//|Функция от Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
//Функция находит последний исторический ордер и возвращает его тикет. Если не находит 
//исторических ордеров возвращает -1
/*
symb - символ, All- все символы
mag - маджик номер, -1 -любой
*/
int LastHistotyOrder(string symb,int mag)
  {
   datetime opentime=0;
   int ticket=-1;
   int hist=OrdersHistoryTotal();
   for(int p=hist-1; p>=0; p--)
     {
      if(!OrderSelect(p,SELECT_BY_POS,MODE_HISTORY))
        {
         Print("Ордер не выбран, ошибка = ",GetLastError());
        }
      if(symb!="All" && OrderSymbol()!=Symbol())continue;
      if(mag>=0 && OrderMagicNumber()!=mag)continue;
      if(opentime<OrderCloseTime())
        {
         opentime=OrderCloseTime();
         ticket=OrderTicket();
        }
     }
   return(ticket);
  }
//+------------------------------------------------------------------+
//|Функция от Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
/*Простой стоплосс тейкпрофит в пунктах
po - расстояние до уровня закрытия в пунктах, если =0 без уровня
pr - цена открытия
direct - направление уровня. 1 - вверх, -1 вниз, 0- нет
*/
double sltp(int po,double pr,int direct)
  {
   if(po==0 || direct==0)return(0);
   double step=MarketInfo(Symbol(),17);
   if(direct==1)return(MathRound((pr+po*Point)/step)*step);
   if(direct==-1)return(MathRound((pr-po*Point)/step)*step);
   return(0);
  }
//+------------------------------------------------------------------+
