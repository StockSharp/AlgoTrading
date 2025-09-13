//+------------------------------------------------------------------+
//|                                               Double trading.mq5 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

//--- input parameters
input string Commentation1= "";//Параметры ордера:
input double    Lot1=1;// Лоты для 1-го символа
input double    Lot2=1.3;// Лоты для 2-го символа
input double    StopLoss=5000;// Stop Loss
input double    TakeProfit=5000;// Take Profit
input double    Profit=20;// Требуемая прибыль в валюте депозита
enum comm 
  {
   S,     // Текущий график (ставьте более волатильный)
   };
input comm MoneyN1= S;// Символ № 1
input string Money2= "USDCHF";// Символ № 2
input string Money1_SELL_or_BUY="Auto";// Символ № 1: продавать (SELL), покупать (BUY) или робот (Auto)?
input string Money2_SELL_or_BUY="Auto";// Символ № 2: продавать (SELL), покупать (BUY) или робот (Auto)?
enum Tr 
  {
   AutoD = 0,     // Парные
   AutoM = 1,     // Зеркальные
   };
input Tr Auto= AutoM;// Какие у вас символы (для роботизированной торговли)
input int    Try=10;// Сколько раз пытаться открыть ордер
input string Commentation2= "";//Работа по расписанию:
input bool   Monday=true;// Понедельник (True - работает, False - отключен)
input bool   Tuesday=true;// Вторник (True - работает, False - отключен)
input bool   Wednesday=true;// Среда (True - работает, False - отключен)
input bool   Thursday=true;// Четверг (True - работает, False - отключен)
input bool   Friday=false;// Пятница (True - работает, False - отключен)
input string Commentation3= "";//Параметры индикатора Correlation:
input double Open_Value=-0.98;// Планка корреляции для открытия ордеров
input double Demis_Value=0.003;// Погрешность планки (+/-)
input int Depth=50;// Период подсчета
input ENUM_APPLIED_PRICE AppliedPrice=PRICE_WEIGHTED;// Цена

int  iWeek=0,Monday1=0,Tuesday2=0,Wednesday3=0,Thursday4=0,Friday5=0;
int  i=0,Correlation1=0,Deals=0;
long  TimePosition1=0,TimePosition2=0;
bool AutoF=false ,start=true, CorrelationF=0, TrendUP=0, TrendDown=0, TrendUP2=0, TrendDown2=0;
double order1=0, order2=0,ticket1=0, ticket2=0,CorrelationBuffer[1],OrderProfit1=0,OrderProfit2=0,bid=0,ask=0,point=0;
string Money1_SELLorBUY="",Money2_SELLorBUY="",Money1=Symbol(),nTypeEntry="";
datetime starttime=0;

//-----OrderSend:
double MyOrderSend(
   string   symbol,               // символ
   ENUM_ORDER_TYPE cmd,           // торговая операция
   double   volume,               // количество лотов
   double   price,                // цена
   int      slippage,             // проскальзывание
   double   stoploss,             // stop loss
   double   takeprofit,           // take profit
   string   comment=NULL,         // комментарий
   int      magic=0,              // идентификатор
   datetime expiration=0,         // срок истечения ордера
   color    arrow_color=clrNONE  // цвет
               )
  {
  //--- готовим запрос
   MqlTradeRequest request={0};
   request.action=TRADE_ACTION_DEAL;         // установка отложенного ордера
   request.magic=magic;                         // ORDER_MAGIC
   request.symbol=symbol;                       // инструмент
   request.volume=volume;                       // объем в лотах
   request.sl=stoploss;                         // Stop Loss
   request.tp=takeprofit;                       // Take Profit   
   request.type=cmd;                            // тип  ордера
   request.price=price;                         // цена для открытия
   request.deviation=slippage;                  // проскальзывание
   request.comment=comment;                     // комментарий
   request.expiration=expiration;               // срок истечения ордера
//--- отправим торговый приказ
   MqlTradeResult result={0};
   if (OrderSend(request,result))
   return(result.price);
   else
   return(0);
  }
//-----OrderSend


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
  {
//----
Money1_SELLorBUY = Money1_SELL_or_BUY;
Money2_SELLorBUY = Money2_SELL_or_BUY;
StringTrimLeft(Money1_SELLorBUY);
StringTrimLeft(Money2_SELLorBUY);
StringTrimRight(Money1_SELLorBUY);
StringTrimRight(Money2_SELLorBUY);
CorrelationBuffer[0]=0;

if (Monday==true)
{Monday1=1;
iWeek=1;}
if (Tuesday==true)
{Tuesday2=2;
iWeek=2;}
if (Wednesday==true)
{Wednesday3=3;
iWeek=3;}
if (Thursday==true)
{Thursday4=4;
iWeek=4;}
if (Friday==true)
{Friday5=5;
iWeek=5;}

if (StringSubstr(Money1_SELLorBUY,0,1)=="a" || StringSubstr(Money1_SELLorBUY,0,1)=="A" || StringSubstr(Money2_SELLorBUY,0,1)=="a" || StringSubstr(Money2_SELLorBUY,0,1)=="A")
AutoF = true;
//----
   //return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
void OnTick()
  {
  //----Function,ext:
  //Server Time:
  datetime Time1=TimeGMT();
  MqlDateTime strTime;
  TimeToStruct(Time1,strTime);
  //---

//----Robot:
// Int indicator (CorrelationBuffer[0],etc):
Correlation1=iCustom(Money1,0,"Correlation",Money2,Depth,AppliedPrice);
CopyBuffer(
   Correlation1,     // handle индикатора
   0,                // номер буфера индикатора
   0,                // откуда начнем 
   1,                // сколько копируем
   CorrelationBuffer // массив, куда будут скопированы данные
   );   
// Int indicator (CorrelationBuffer[0],etc)

///Correlation signal(CorrelationF):
if ((ticket1==0 && order1!=0) || start == true)
{
if ((ticket2==0 && order2!=0) || start == true)
{

if (CorrelationBuffer[0]<=Open_Value+Demis_Value && CorrelationBuffer[0]>=Open_Value-Demis_Value)
   CorrelationF=true;
///Correlation signal

///TRand (for Auto trading)
if (AutoF)
{
TrendUP=0;
TrendDown=0;
TrendUP2=0;
TrendDown2=0;
   if (((strTime.hour >= 19 && strTime.min >= 00) && (strTime.hour <= 23 && strTime.min <= 59)) || ((strTime.hour >= 00 && strTime.min >= 00) && (strTime.hour <= 5 && strTime.min <= 59))) // 19:00 - 6:00 (Тихоокеанская сессия AUD, NZD)
      {if (StringFind(Money1,"AUD")==0)
         TrendUP = true; 
      if (StringFind(Money1,"AUD")==3)
         TrendDown = true; 
      if (StringFind(Money1,"NZD")==0)
         TrendUP = true; 
      if (StringFind(Money1,"NZD")==3)
         TrendDown = true; 
      if (StringFind(Money2,"AUD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"AUD")==3)
         TrendDown2 = true; 
      if (StringFind(Money2,"NZD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"NZD")==3)
         TrendDown2 = true;} 
   if (((strTime.hour >= 6 && strTime.min >= 00) && (strTime.hour <= 8 && strTime.min <= 59))) // 6:00 - 9:00 (Азиатская сессия JPY)
      {if (StringFind(Money1,"JPY")==0)
         TrendUP = true; 
      if (StringFind(Money1,"JPY")==3)
         TrendDown = true;          
      if (StringFind(Money2,"JPY")==0)
         TrendUP2 = true;    
      if (StringFind(Money2,"JPY")==3)
         TrendDown2 = true;}         
   if ((strTime.hour >= 9 && strTime.min >= 00) && (strTime.hour <= 15 && strTime.min <= 59)) // 9:00 - 16:00 (Европейская сессия EUR, CHF, GBP)
      {if (StringFind(Money1,"EUR")==0)
         TrendUP = true; 
      if (StringFind(Money1,"EUR")==3)
         TrendDown = true; 
      if (StringFind(Money1,"CHF")==0)
         TrendUP = true; 
      if (StringFind(Money1,"CHF")==3)
         TrendDown = true; 
      if (StringFind(Money1,"GBP")==0)
         TrendUP = true; 
      if (StringFind(Money1,"GBP")==3)
         TrendDown = true;           
      if (StringFind(Money2,"EUR")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"EUR")==3)
         TrendDown2 = true; 
      if (StringFind(Money2,"CHF")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"CHF")==3)
         TrendDown2 = true;            
      if (StringFind(Money2,"GBP")==0)
         TrendUP2 = true;    
      if (StringFind(Money2,"GBP")==3)
         TrendDown2 = true;}           
   if ((strTime.hour >= 16 && strTime.min >= 00) && (strTime.hour <= 21 && strTime.min <= 59)) // 16:00 - 22:00 (Американская сессия USD, CAD)
      {if (StringFind(Money1,"USD")==0)
         TrendUP = true;
      if (StringFind(Money1,"USD")==3)
         TrendDown = true; 
      if (StringFind(Money1,"CAD")==0)
         TrendUP = true; 
      if (StringFind(Money1,"CAD")==3)
         TrendDown = true; 
      if (StringFind(Money2,"USD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"USD")==3)
         TrendDown2 = true; 
      if (StringFind(Money2,"CAD")==0)
         TrendUP2 = true; 
      if (StringFind(Money2,"CAD")==3)
         TrendDown2 = true;}   
   
   if (Auto==0) //Парный трейдинг
   { 
       if((TrendUP && TrendDown) || (!TrendUP && !TrendDown)) //Проверяем относиться ли валюта(ы) к торговой сессии на 1-ом графике
       {
         if(TrendUP2) // Направление тренда №2
         {
         Money2_SELLorBUY="Buy";
         Money1_SELLorBUY="Sell";
         }
         if(TrendDown2) // Направление тренда №2
         {
         Money2_SELLorBUY="Sell";
         Money1_SELLorBUY="Buy";
         } 
       } 
       else
       {
         if(TrendUP) // Направление тренда №1
         {
         Money1_SELLorBUY="Buy";
         Money2_SELLorBUY="Sell";
         }
         if(TrendDown) // Направление тренда №1
         {
         Money1_SELLorBUY="Sell";
         Money2_SELLorBUY="Buy";
         }
       }  
     }

   if (Auto==1) //Зеркальный трейдинг
   { 
       if((TrendUP && TrendDown) || (!TrendUP && !TrendDown)) //Проверяем относиться ли валюта(ы) к торговой сессии на 1-ом графике
       {
         if(TrendUP2) // Направление тренда №2
         {
         Money2_SELLorBUY="Buy";
         Money1_SELLorBUY="Buy";
         }
         if(TrendDown2) // Направление тренда №2
         {
         Money2_SELLorBUY="Sell";
         Money1_SELLorBUY="Sell";
         } 
       } 
       else
       {
         if(TrendUP) // Направление тренда №1
         {
         Money1_SELLorBUY="Buy";
         Money2_SELLorBUY="Buy";
         }
         if(TrendDown) // Направление тренда №1
         {
         Money1_SELLorBUY="Sell";
         Money2_SELLorBUY="Sell";
         }
       }  
     }
if(!TrendUP && !TrendDown && !TrendUP2 && !TrendDown2)
CorrelationF=false;
}
///TRand (for Auto trading)
}}

//OpenOrder:
if (CorrelationF==true && (Monday1==strTime.day_of_week||Tuesday2==strTime.day_of_week||Wednesday3==strTime.day_of_week||Thursday4==strTime.day_of_week||Friday5==strTime.day_of_week))
{
if (ticket1==0 && ticket2==0)
{starttime = TimeCurrent(); //Время открытия позиций
HistorySelect(starttime,TimeCurrent()); //Узнаем сколько у нас сделок до того как откроем позиции
Deals = HistoryDealsTotal();} //Узнаем сколько у нас сделок до того как откроем позиции
i=0;
  while (ticket1==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money1,SYMBOL_BID); // Запрос значения Bid
   ask   =SymbolInfoDouble(Money1,SYMBOL_ASK); // Запрос значения Ask
   point =SymbolInfoDouble(Money1,SYMBOL_POINT);//Запрос Point
   i =i+1;
   if (StringLen(Money1_SELLorBUY)==3)
   ticket1=MyOrderSend(Money1,ORDER_TYPE_BUY,Lot1,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   else
   ticket1=MyOrderSend(Money1,ORDER_TYPE_SELL,Lot1,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   }
i=0;
   while (ticket2==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money2,SYMBOL_BID); // Запрос значения Bid
   ask   =SymbolInfoDouble(Money2,SYMBOL_ASK); // Запрос значения Ask
   point =SymbolInfoDouble(Money2,SYMBOL_POINT);//Запрос Point
   i =i+1;
   if (StringLen(Money2_SELLorBUY)==3)
   ticket2=MyOrderSend(Money2,ORDER_TYPE_BUY,Lot2,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   else
   ticket2=MyOrderSend(Money2,ORDER_TYPE_SELL,Lot2,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   }
//}
//}

if (ticket1!=0 && ticket2!=0)
{
CorrelationF=false;
start=false;
order1=0;
order2=0;

//Ждем открытия позиции(й)
int NeedDeals = Deals+2; //Придаток - сколько ждать позиций
while (Deals<NeedDeals)
{
Sleep(100);
HistorySelect(starttime,TimeCurrent());
Deals = HistoryDealsTotal();
}
//Ждем открытия позиции(й)

   do //Ждем прибыль
   {
   Sleep(10);
   bid   =SymbolInfoDouble(Money1,SYMBOL_BID); // Запрос значения Bid
   ask   =SymbolInfoDouble(Money1,SYMBOL_ASK); // Запрос значения Ask
   if (StringLen(Money1_SELLorBUY)==3)
    {if (!OrderCalcProfit(ORDER_TYPE_BUY,Money1,Lot1,ticket1,bid,OrderProfit1))
    OrderProfit1=0;}
   else
    {if (!OrderCalcProfit(ORDER_TYPE_SELL,Money1,Lot1,ticket1,ask,OrderProfit1))
    OrderProfit1=0;}
   
   bid   =SymbolInfoDouble(Money2,SYMBOL_BID); // Запрос значения Bid
   ask   =SymbolInfoDouble(Money2,SYMBOL_ASK); // Запрос значения Ask
   if (StringLen(Money2_SELLorBUY)==3)
    {if (!OrderCalcProfit(ORDER_TYPE_BUY,Money2,Lot2,ticket2,bid,OrderProfit2))
    OrderProfit2=0;}
   else
    {if (!OrderCalcProfit(ORDER_TYPE_SELL,Money2,Lot2,ticket2,ask,OrderProfit2))
    OrderProfit2=0;}

      //Отслеживаем TakeProfit или StopLoss. Если такая есть ждем закрытие второй позиции.
      HistorySelect(starttime,TimeCurrent());
      ulong last_deal=HistoryDealGetTicket(HistoryDealsTotal()-1);
      string nSymbol;
      if (HistoryDealGetString(last_deal,DEAL_COMMENT,nTypeEntry) && HistoryDealGetString(last_deal,DEAL_SYMBOL,nSymbol))
         {if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money1)
         {
            i=0;
            while (1)
            {
            Sleep(100);
            i=i+1;
            HistorySelect(starttime,TimeCurrent());
            if (MathMod(i,2)==0)
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-1);
            else
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-2);
            if (HistoryDealGetString(last_deal,DEAL_COMMENT,nTypeEntry) && HistoryDealGetString(last_deal,DEAL_SYMBOL,nSymbol))
            if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money2)
            {order1 = true;
            order2 = true;
            break;}
            }
         break;
         }
         if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money2)
         {
            i=0;
            while (1)
            {
            Sleep(100);
            i=i+1;
            HistorySelect(starttime,TimeCurrent());
            if (MathMod(i,2)==0)
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-1);
            else
            last_deal=HistoryDealGetTicket(HistoryDealsTotal()-2);
            if (HistoryDealGetString(last_deal,DEAL_COMMENT,nTypeEntry) && HistoryDealGetString(last_deal,DEAL_SYMBOL,nSymbol))
            if ((StringFind(nTypeEntry,"sl") !=-1 || StringFind(nTypeEntry,"tp") != -1) && nSymbol == Money1)
            {order1 = true;
            order2 = true;
            break;}
            }
         break;
         }}
      //Отслеживаем TakeProfit или StopLoss. Если такая есть ждем закрытие второй позиции.
    
   }
   while ((OrderProfit2+OrderProfit1)<Profit); //Ждем прибыль

ticket1=0;
ticket2=0;
if (order2==0 && order1==0)
Print(" > Double trading. Прибыль: ", NormalizeDouble((OrderProfit2+OrderProfit1),2));
}
}

//TakeProfit:
i=0;
if ((OrderProfit2+OrderProfit1)>=Profit)
{
   while (order2==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money2,SYMBOL_BID); // Запрос значения Bid
   ask   =SymbolInfoDouble(Money2,SYMBOL_ASK); // Запрос значения Ask
   point =SymbolInfoDouble(Money2,SYMBOL_POINT);//Запрос Point
   if (StringLen(Money2_SELLorBUY)==3)
   order2=MyOrderSend(Money2,ORDER_TYPE_SELL,Lot2,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   else
   order2=MyOrderSend(Money2,ORDER_TYPE_BUY,Lot2,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   i=i+1;
   }
 i=0;
   while (order1==0 && Try>i)
   {
   bid   =SymbolInfoDouble(Money1,SYMBOL_BID); // Запрос значения Bid
   ask   =SymbolInfoDouble(Money1,SYMBOL_ASK); // Запрос значения Ask
   point =SymbolInfoDouble(Money1,SYMBOL_POINT);//Запрос Point
   if (StringLen(Money1_SELLorBUY)==3)
   order1=MyOrderSend(Money1,ORDER_TYPE_SELL,Lot1,bid,3,ask+StopLoss*point,bid-TakeProfit*point,"",0,0,Red);
   else
   order1=MyOrderSend(Money1,ORDER_TYPE_BUY,Lot1,ask,3,bid-StopLoss*point,ask+TakeProfit*point,"",0,0,Blue);
   i=i+1;
   }   
}
//----
  }
//+------------------------------------------------------------------+