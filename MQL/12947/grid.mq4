//+------------------------------------------------------------------+
//|                                                         grid.mq4 |
//|                                              Роман Александрович |
//|                                           roman_machanow@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Роман Александрович"
#property link      "roman_machanow@mail.ru"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Объявление переменных                                            |
//+------------------------------------------------------------------+
input int Hag = 10;        //шаг отложенников
input double Lot  = 0.01;  //лот ордера
input int tp= 200;         //тейк профит
input double Profit_S=1;   //прибыль для начала перезапуска робота
input bool Martin=true; //включение отключение мартингейла (false=выкл)
//---
bool Poisc_Po;          //поиск открытой позиции на покупку
bool Poisc_Pr;          //поиск открытой позиции на продажу
//---
double
PriceAsk,               //цена трала на покупку
PriceBid,               //цена трала на продажу
Sredstva,               //доступные средства
LotSell,                //лот на продажу
LotBuy,                 //лот на покупку
MaxLotBay,              //максимальный лот на покупку
MaxLotSell;             //максимальный лот на продажу
//---
int
TotalOrder,//общее число ордеров
TotalBuy,
TotalSell,
TotalBuyStop,
TotalSellStop,
BuySell,
Perezapusk;
//---
string
textLots;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   TotalOrder=OrdersTotal();

   for(int i=0; i<=TotalOrder;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         int OrderTupe=OrderType();
         if(OrderTupe==OP_SELL) {TotalSell++;}
         if(OrderTupe==OP_BUY ) {TotalBuy++;}
        }
      BuySell=TotalSell+TotalBuy;
     }
//---
   Sredstva=AccountFreeMargin();
//---
   LotBuy = Lot;
   LotSell=Lot;
//---
   Perezapusk=0;
//---
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
   RefreshRates();
   ComentInform();//ВЫВОД ТОРГОВЫХ ДАННЫХ
   ModDelOtlSdelok();//МОДУЛЬ ОПРЕДЕЛИТЕЛЬ СОВЕРШЕННЫХ СДЕЛОК И ЗАКРЫТИЕ ОТЛОЖЕННИКОВ
   ProfitReturn();//МОДУЛЬ ОПРЕДЕЛИТЕЛЬ СОВЕРШЕННЫХ СДЕЛОК И ЗАКРЫТИЕ ОТЛОЖЕННИКОВ
   LotSellBuy();//РАСЧЕТ УРОВНЯ ЛОТА
//--- РАСЧЕТ ЦЕНЫ
   PriceBid=MathFloor(Bid/(Hag*Point))*(Hag*Point);//цена трала на продажу            
   PriceAsk=MathFloor(Ask/(Hag*Point))*(Hag*Point)+(Hag*Point);//цена трала на покупку
//---
   Poisc_Po = true;                                //разрешения покупки
   Poisc_Pr = true;                                //разрешение продажи
//--- ОПРЕДЕЛЕНИЕ НА РАЗРЕШЕНИЕ СДЕЛОК
   for(int i=0; i<=OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         double PriceOrder=OrderOpenPrice();
         int OrderTupe=OrderType();
         if(NormalizeDouble(PriceAsk,Digits)==NormalizeDouble(PriceOrder,Digits))
           {
            if((OrderTupe==OP_BUYSTOP) || (OrderTupe==OP_BUY) || (OrderTupe==OP_SELLSTOP) || (OrderTupe==OP_SELL))
              {
               Poisc_Po=false;
               continue;
              }
           }
        }
     }
//---
   for(int i=0; i<=OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         double PriceOrder=OrderOpenPrice();
         int OrderTupe=OrderType();
         if(NormalizeDouble(PriceBid,Digits)==NormalizeDouble(PriceOrder,Digits))
           {
            if((OrderTupe==OP_SELLSTOP) || (OrderTupe==OP_SELL) || (OrderTupe==OP_BUYSTOP) || (OrderTupe==OP_BUY))
              {
               Poisc_Pr=false;
               continue;
              }
           }
        }
     }
//--- СОВЕРШЕНИЕ СДЕЛОК
   if(Poisc_Pr)
     {
      int Error_SELLSTOP=OrderSend(Symbol(),OP_SELLSTOP,LotSell,PriceBid,3,0,PriceBid-tp*Point);
      if(Error_SELLSTOP<0)
        {
         Print("Ошибка SELLSTOP ",GetLastError());
        }
     }
//---
   if(Poisc_Po)
     {
      int Error_BUYSTOP=OrderSend(Symbol(),OP_BUYSTOP,LotBuy,PriceAsk,3,0,PriceAsk+tp*Point);
      if(Error_BUYSTOP<0)
        {
         Print("Ошибка BUYSTOP ",GetLastError());
        }
     }
  }
//+------------------------------------------------------------------+
//| ВЫВОД ТОРГОВЫХ ДАННЫХ                                            |
//+------------------------------------------------------------------+
int ComentInform()
  {
   SvodInform();
//---
   Comment("Всего ордеров   : "+IntegerToString(TotalOrder)+
           "\n ПЕРЕЗАПУСКОВ: "+IntegerToString(Perezapusk)+
           "\n SELL        : "+IntegerToString(TotalSell)+
           "\n BUY         : "+IntegerToString(TotalBuy)+
           "\n SELLSTOP    : "+IntegerToString(TotalSellStop)+
           "\n BUYSTOP     : "+IntegerToString(TotalBuyStop)+
           "\n ПРИБЫЛЬ     : "+DoubleToStr(AccountProfit(),1)+
           "\n СРЕДСТВА    : "+DoubleToStr(Sredstva,0)+textLots);
//AccountFreeMargin()
// Print("Средства счета = ",AccountEquity());
   return(0);
  }
//--- РАСЧЕТЧИК
int SvodInform()
  {
   TotalOrder=OrdersTotal();
   TotalSell=0;
   TotalBuy=0;
   TotalSellStop=0;
   TotalBuyStop=0;
//---
   for(int i=0; i<=TotalOrder;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         int OrderTupe=OrderType();
         if(OrderTupe==OP_SELL) {TotalSell++;}
         if(OrderTupe==OP_BUY ) {TotalBuy++;}
         if(OrderTupe==OP_SELLSTOP) {TotalSellStop++;}
         if(OrderTupe==OP_BUYSTOP) {TotalBuyStop++;}
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| МОДУЛЬ ОПРЕДЕЛИТЕЛЬ СОВЕРШЕННЫХ СДЕЛОК И ЗАКРЫТИЕ ОТЛОЖЕННИКОВ   |
//+------------------------------------------------------------------+
int ModDelOtlSdelok()
  {
   for(int i=0; i<=TotalOrder;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         int OrderTupe=OrderType();
         if(OrderTupe==OP_SELL) {TotalSell++;}
         if(OrderTupe==OP_BUY ) {TotalBuy++;}
        }
     }
   if(BuySell!=(TotalSell+TotalBuy))
     {
      for(int i=0; i<=TotalOrder;i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            int OrderTupe = OrderType();
            if(OrderType()==OP_BUYSTOP)
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
            if(OrderType()==OP_SELLSTOP)
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
           }
        }
      BuySell=TotalSell+TotalBuy;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| МОДУЛЬ ПЕРЕЗАПУСКА РОБОТА ПОСЛЕ ДОСТИЖЕНИЯ УРОВНЯ PROFIT         |
//+------------------------------------------------------------------+
int ProfitReturn()
  {
   if(AccountProfit()>=Profit_S)
     {
      for(int i=0; i<=OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            int OrderTupe= OrderType();
            if((OrderTupe==OP_SELLSTOP)||(OrderTupe==OP_BUYSTOP))
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_SELL)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Ask,5);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_BUY)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Bid,5);
               if(!ord_close) i--;
              }
           }
        }
      Perezapusk++;
      Sredstva=AccountFreeMargin();
      LotSell=0.01;
      LotBuy=0.01;
     }
//---
   return(0);
  }
//+-------------------------------------------------------------------------+
//| МОДУЛЬ ПЕРЕЗАПУСКА РОБОТА ПОСЛЕ ДОСТИЖЕНИЯ УРОВНЯ PROFIT_другой вариант |
//+-------------------------------------------------------------------------+
int ProfitReturn_1()
  {
   if(AccountProfit()>=Profit_S)
     {
      for(int i=0; i<=OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            int OrderTupe= OrderType();
            if((OrderTupe==OP_SELLSTOP)||(OrderTupe==OP_BUYSTOP))
              {
               bool ord_close=OrderDelete(OrderTicket(),clrNONE);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_SELL)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Ask,5);
               if(!ord_close) i--;
              }
            if(OrderTupe==OP_BUY)
              {
               bool ord_close=OrderClose(OrderTicket(),OrderLots(),Bid,5);
               if(!ord_close) i--;
              }
           }
        }
      Sredstva=AccountFreeMargin();
      LotSell=0.01;
      LotBuy=0.01;
     }
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| РАСЧЕТ УРОВНЯ ЛОТА                                               |
//+------------------------------------------------------------------+
int LotSellBuy()
  {
   if(Martin==true)
     {
      double
      ToAllLotSell=0,
      ToAllLotBuy=0;
      //---
      for(int i=0; i<=OrdersTotal();i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
           {
            double LotOrder=OrderLots();
            int OrderTupe = OrderType();
            if(OrderType()==OP_SELL) {ToAllLotSell=ToAllLotSell+LotOrder;}
            if(OrderType()==OP_BUY) {ToAllLotBuy=ToAllLotBuy+LotOrder;}
           }
        }
      //---
      if(ToAllLotSell>ToAllLotBuy){LotBuy=ToAllLotSell+Lot;}else{LotSell=ToAllLotBuy+Lot;}
      //---
      if(ToAllLotBuy>MaxLotBay){MaxLotBay=ToAllLotBuy;}
      if(ToAllLotSell>MaxLotSell){MaxLotSell=ToAllLotSell;}
      //---
      textLots=("\nВсего лотов куплено : "+DoubleToStr(ToAllLotBuy,2)+
                "\nВсего лотов продано : "+DoubleToStr(ToAllLotSell,2)+
                "\nМаксимальный лот на покупку : "+DoubleToStr(MaxLotBay,2)+
                "\nМаксимальный лот на продажу : "+DoubleToStr(MaxLotSell,2));
     }
   else
     {
      textLots="";
     }
//---
   return(0);
  }
//+------------------------------------------------------------------+
