//+------------------------------------------------------------------+
//|                                                     RSI_Test.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


extern double RiskPercentage = 10;     // риск
extern int    TrailingStop   = 50;     // трейлинг стоп
extern int    MaxOrders      =  1;     // максимальное количество ордеров, если 0 - не контролируется
extern int    BuyOp          = 12;     // сигнал на покупку
extern int    SellOp         = 88;     // сигнал на продажу
extern int    magicnumber    = 777;
extern int    Test           = 14;     // период RSI
int expertBars;
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
extern int SetHour   = 00;             //Час старта оптимизации 
extern int SetMinute = 05;             //Минута старта оптимизации 
int    TestDay     = 3;                      //Количество дней для оптимизации 
int    TimeOut     = 12;                     //Время ожидания окончания оптимизации в минутах
string NameMTS     = "rsi_test";        //Имя советника
string NameFileSet = "rsi_test.set";             //Имя Set файла с установками
string PuthTester  = "D:\Forex\Metatrader1";//Путь к тестеру
//--- Последовательность фильтрации
int    Gross_Profit   = 1;                   //Сортировка по Максимальной прибыли
int    Profit_Factor  = 2;                   //Сортировка по Максимальной прибыльности
int    Expected_Payoff= 3;                   //Сортировка по Максимальному матожиданию
//--имена переменных для оптимизации
string Per1 = "BuyOp";
string Per2 = "SellOp";
string Per3 = "Test";
string Per4 = "";
bool StartTest=false;
datetime TimeStart;
//--- Подключение библиотеки автооптимизатора
#include <auto_optimization.mqh>

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
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


   double margin = MarketInfo(Symbol(), MODE_MARGINREQUIRED);
   double minLot = MarketInfo(Symbol(), MODE_MINLOT);
   double maxLot = MarketInfo(Symbol(), MODE_MAXLOT);
   double step =   MarketInfo(Symbol(), MODE_LOTSTEP);
   double account = AccountFreeMargin();
   
   double percentage = account*RiskPercentage/100;
   
   double Lots = MathRound(percentage/margin/step)*step;
   
   if(Lots < minLot)
   {
      Lots = minLot;
   }
   
   if(Lots > maxLot)
   {
      Lots = maxLot;
   }
   
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   if(!IsTesting() && !IsOptimization()){                //При тестировании и оптимизации не запускать
      if(TimeHour(TimeCurrent())==SetHour){                //Сравнение текущего часа с установленным для запуска
        if(!StartTest){                                 //Защита от повторного запуска
            if(TimeMinute(TimeCurrent())>SetMinute-1){     //Сравнение диапазона минут с установленной для запуска минутой
               if(TimeMinute(TimeCurrent())<SetMinute+1){  //диапазон нужен в случае если по каким-то причинам долго нет нового тика
                  TimeStart   =TimeLocal();
                  StartTest   =true;                     //Флаг запуска тестера
                  Tester(TestDay,NameMTS,NameFileSet,PuthTester,TimeOut,Gross_Profit,Profit_Factor,Expected_Payoff,Per1,Per2,Per3,Per4);
   }}}}
   BuyOp     =GlobalVariableGet(Per1);
   SellOp    =GlobalVariableGet(Per2);
   Test      =GlobalVariableGet(Per3);
//   TrailingStop=GlobalVariableGet(Per4);
   }
   if(StartTest){                                        //Если флаг запуска тестера установлен
       if(TimeLocal()-TimeStart > TimeOut*60){            //Если с момента запуска прошло больше установленного времени ожидания тестирования
       StartTest = false;                                //Обнулим флаг
   }}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 
    if(!StartTest){Comment("BuyOp ",BuyOp,"  | SellOp ",SellOp,"  | Test ",Test);}   
   
   
   int i=0;  
   int total = OrdersTotal();   
   for(i = 0; i <= total; i++) 
     {
      if(TrailingStop>0)  
       {                 
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == magicnumber) 
         {
         TrailingStairs(OrderTicket(),TrailingStop,TrailingStop);
         }
       }
      }

   if ((total < MaxOrders || MaxOrders == 0)) 
     {
      if ((iRSI(NULL,0,Test,PRICE_CLOSE,0) < BuyOp) && (iRSI(NULL,0,Test,PRICE_CLOSE,0) > iRSI(NULL,0,Test,PRICE_CLOSE,1)))
       {
        if (Open[0]>Open[1])
          {OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"RSI_Buy",magicnumber,0,Green);}
       }
      if ((iRSI(NULL,0,Test,PRICE_CLOSE,0) > SellOp) && (iRSI(NULL,0,Test,PRICE_CLOSE,0) < iRSI(NULL,0,Test,PRICE_CLOSE,1)))
       {
        if (Open[0]<Open[1])
          {OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"RSI_Sell",magicnumber,0,Red);}
       }
     } 
//----
   return(0);
  }
//+------------------------------------------------------------------+

void TrailingStairs(int ticket,int trldistance,int trlstep)
   { 
   
   double nextstair; // ближайшее значение курса, при котором будем менять стоплосс
 
   // проверяем переданные значения
   if ((trldistance<MarketInfo(Symbol(),MODE_STOPLEVEL)) || (trlstep<1) || (trldistance<trlstep) || (ticket==0) || (!OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)))
      {
      Print("Трейлинг функцией TrailingStairs() невозможен из-за некорректности значений переданных ей аргументов.");
      return(0);
      } 
   
   // если длинная позиция (OP_BUY)
   if (OrderType()==OP_BUY)
      {
      // расчитываем, при каком значении курса следует скорректировать стоплосс
      // если стоплосс ниже открытия или равен 0 (не выставлен), то ближайший уровень = курс открытия + trldistance + спрэд
      if ((OrderStopLoss()==0) || (OrderStopLoss()<OrderOpenPrice()))
      nextstair = OrderOpenPrice() + trldistance*Point;
         
      // иначе ближайший уровень = текущий стоплосс + trldistance + trlstep + спрэд
      else
      nextstair = OrderStopLoss() + trldistance*Point;
 
      // если текущий курс (Bid) >= nextstair и новый стоплосс точно лучше текущего, корректируем последний
      if (Bid>=nextstair)
         {
         if ((OrderStopLoss()==0) || (OrderStopLoss()<OrderOpenPrice()) && (OrderOpenPrice() + trlstep*Point<Bid-MarketInfo(Symbol(),MODE_STOPLEVEL)*Point)) 
            {
            if (!OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice() + trlstep*Point,OrderTakeProfit(),OrderExpiration()))
            Print("Не удалось модифицировать стоплосс ордера №",OrderTicket(),". Ошибка: ",GetLastError());
            }
         }
 /*     else
         {
         if (!OrderModify(ticket,OrderOpenPrice(),OrderStopLoss() + trlstep*Point,OrderTakeProfit(),OrderExpiration()))
         Print("Не удалось модифицировать стоплосс ордера №",OrderTicket(),". Ошибка: ",GetLastError());
         }*/
      }
      
   // если короткая позиция (OP_SELL)
   if (OrderType()==OP_SELL)
      { 
      // расчитываем, при каком значении курса следует скорректировать стоплосс
      // если стоплосс ниже открытия или равен 0 (не выставлен), то ближайший уровень = курс открытия + trldistance + спрэд
      if ((OrderStopLoss()==0) || (OrderStopLoss()>OrderOpenPrice()))
      nextstair = OrderOpenPrice() - (trldistance + MarketInfo(Symbol(),MODE_SPREAD))*Point;
      
      // иначе ближайший уровень = текущий стоплосс + trldistance + trlstep + спрэд
      else
      nextstair = OrderStopLoss() - (trldistance + MarketInfo(Symbol(),MODE_SPREAD))*Point;
       
      // если текущий курс (Аск) >= nextstair и новый стоплосс точно лучше текущего, корректируем последний
      if (Ask<=nextstair)
         {
         if ((OrderStopLoss()==0) || (OrderStopLoss()>OrderOpenPrice()) && (OrderOpenPrice() - (trlstep + MarketInfo(Symbol(),MODE_SPREAD))*Point>Ask+MarketInfo(Symbol(),MODE_STOPLEVEL)*Point))
            {
            if (!OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice() - (trlstep + MarketInfo(Symbol(),MODE_SPREAD))*Point,OrderTakeProfit(),OrderExpiration()))
            Print("Не удалось модифицировать стоплосс ордера №",OrderTicket(),". Ошибка: ",GetLastError());
            }
         }
 /*     else
         {
         if (!OrderModify(ticket,OrderOpenPrice(),OrderStopLoss()- (trlstep + MarketInfo(Symbol(),MODE_SPREAD))*Point,OrderTakeProfit(),OrderExpiration()))
         Print("Не удалось модифицировать стоплосс ордера №",OrderTicket(),". Ошибка: ",GetLastError());
         }*/
      }      
   }

