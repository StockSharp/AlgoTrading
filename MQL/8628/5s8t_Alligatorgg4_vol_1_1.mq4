//+------------------------------------------------------------------+
//|                                        На Alligator'е vol.1.1.mq4|
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Демёхин Виталий Евгеньевич."
#property link      "Vitalya_1983@list.ru"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

extern double MaxLot       = 0.5,         //ограничиваем размер стартового лота
              koeff        = 1.3,         //коэффициент увеличения лотов при мартингейле
              risk         = 0.04,        //риск, влияет на размер стартового лота
              shirina1     = 0.0005,      //ширина "зева" Alligator'а на открытие ордера
              shirina2     = 0.0001;      //ширина "зева" Alligator'а на закрытие ордера
              
extern bool Ruchnik = false,              //"Плавно" завершает сессию. Если надо отклюяить советник
            Vhod_Alligator= true,         //Разрешает открывать ордера Аллигатору
            Vhod_Fractals = false,        //Проверка Аллигатора Фракталом
            Vyhod_Alligator = false,      //Разрешает закрывать ордера Аллигатору
            OnlyOneOrder = true,          //Если False, включается докупка при повторе сигнала
            EnableMartingail = true,      //Мартингейл
            Trailing = true;              //ТрейлингСтоп

extern int  TP              = 80,         //TP
            SL              = 80,         //SL  :)
            TrailingStop    = 30,         //тоже понятно
            profit          = 20,         //Гарантирует прибыль при срабатывании трейлинга
            blue            = -8,         //настройки Аллигатора         
            red             = -3,
            green           = 8,          
            Fractal_bars    = 10,         //Количество баров...
            visota_fractal  = 30,         //среди которых ищется соответствующий по высоте и направлению фрактал
            spred           = 10,         //Я не ставил спред из ДЦ. Ручками. А лучше сразе 10
            Koleno          = 10,         //Количество "Колен" Мартингейла
            Zaderzhka       = 2000;       //Пауза между сделками

bool Proverka_buy,Proverka_sell,Prodazha,Pokupka,Trailing_buy,Trailing_sell,Vihod_Alligator_sell,Vihod_Alligator_buy, Fractal;
int  seychas_buy=1,seychas_sell=1, bylo_buy,bylo_sell,i; 
string text ;
double Lot, Magic_number, up, down;
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int init ()                               //объясняем как будем торговать
   {
   text = "На Alligatorе:   ";
   if (Period() == 1)      string per = "M1  ";
   if (Period() == 5)      per = "M5  ";
   if (Period() == 15)     per = "M15  ";
   if (Period() == 30)     per = "M30  ";
   if (Period() == 60)     per = "H1  ";
   if (Period() == 240)    per = "H4  ";
   if (Period() == 1440)   per = "D1  ";
   text = text + Symbol()+"  "+per;
   if (Vhod_Alligator)
         {
         text = text + "Аллигатор вкл  ";
         }
      if (Vhod_Fractals)
         {
         text = text + "Fraktals вкл  ";
         }
      if (!OnlyOneOrder)
         {
         text = text + "докупка вкл  ";
         }
      if (EnableMartingail)
         {
         text = text + "Мартингейл вкл  ";
         }
      if (Trailing)
         {
         text = text + "Trailing вкл  ";
         }
      Alert (text);
return (0);   
   }
int deinit()
  {
  text="";
  }
return (0);   

int start()
   {
   
   RefreshRates();      
   bool order_est_buy = false;      //сбрасываем счетчик открытых ордеров
   int Magic_number = Period();     //Даем Магическое число для торговли на разных ТФ
   int Orders = OrdersTotal();      //счетчик проверки ордеров
   if (Trailing==true)              
      {
      Trailing_start ();            //Запускаем трейлинг стоп
      }
   if (Vhod_Alligator== true)       // Используя настройки Аллигатора определяем момент входа в рынок....
      {
      bylo_buy = seychas_buy;       //"Сегодня также как вчера" :)
      bylo_sell = seychas_sell;
      double blue_line=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_WEIGHTED, MODE_GATORJAW, blue);
      double red_line=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_WEIGHTED, MODE_GATORTEETH , red);
      double green_line=iAlligator(NULL, 0, 13, 8, 8, 5, 5, 3, MODE_SMMA, PRICE_WEIGHTED, MODE_GATORLIPS , green);
      double  PriceHigh= High[0];
      if (green_line>blue_line+shirina1)        // сигналы входа
      seychas_buy=1;                
      if (blue_line>green_line+shirina1)
      seychas_sell=1;
      if (green_line+shirina2<red_line)         // сигналы выхода
      seychas_buy=0;
      if (blue_line+shirina2<red_line)
      seychas_sell=0;
      }
   if (Vhod_Fractals)                           //Проверяем фракталом
      {
      Fractal();           
      }
   if (!Proverka_buy ()||OnlyOneOrder == false) //Если открытых ордеров нет...
      {
      for (i = OrdersTotal() ; i>=0; i--)       //Интересуемся...
         {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);  //Есть ли у нас отложенные?
         if(OrderType() == OP_BUYLIMIT && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
            {
            OrderDelete(OrderTicket());         //Закрываем их 
            RefreshRates ();
            Sleep (Zaderzhka);                  // Отдохнем
            }
         }
      if (Ruchnik == false && (seychas_buy > bylo_buy||Vhod_Alligator== false))// Если Аллигатор дал команду или не мешает...
         {
         if(up>0||Vhod_Fractals == false)       //И фрактал дает добро
            {       
            Pokupka();                          //Открываем длинную
            }
         }
      }
   if (!Proverka_sell()||OnlyOneOrder == false) //тоже самое, только для коротких
      {
      for (i = OrdersTotal() ; i>=0; i--)
         {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if(OrderType() == OP_SELLLIMIT && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
            {
            OrderDelete(OrderTicket());
            RefreshRates ();
            Sleep (Zaderzhka);
            }
         }
      if (Ruchnik == false && (seychas_sell > bylo_sell||Vhod_Alligator== false))
         {
         if(down>0||Vhod_Fractals == false)
            {
            Prodazha();
            }
         }
      }
   if  (Vyhod_Alligator == true)          //Если Аллигатор дал команду на закрытие
      {
      if (seychas_buy < bylo_buy&&Vihod_Alligator_buy == true) 
         {
         for (i = 0 ; i<=Orders; i++)     //ищем открытые ордера 
            {
            OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_BUY && OrderSymbol() ==Symbol() &&OrderMagicNumber()==Magic_number)  
               OrderClose(OrderTicket() ,OrderLots(),Bid,3,Blue); //И сносим их
            }
         }
      if (seychas_sell < bylo_sell&&Vihod_Alligator_sell == true)
         {
         Orders=OrdersTotal();
         for (i = 0 ; i<=Orders; i++)
            {
            OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_SELL&& OrderSymbol() ==Symbol() &&OrderMagicNumber() == Magic_number)  
               OrderClose(OrderTicket() ,OrderLots(),Ask,3,Blue);
            }
         }
      }
   return(0);
   }

//+------------------------------------------------------------------+
// Проверка наличия открытых ордеров
bool Proverka_buy()
   {
   bool Otkryt_orders_buy = false; //флаг "открытых ордеров нет"
   for (i = OrdersTotal() ; i>=0; i--)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      int Magic_number= Period();
      if(OrderType() == OP_BUY && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)
      Otkryt_orders_buy = true; //Попался!!!
      }
   if (Otkryt_orders_buy==true)
      return(true);
   else 
      {
      Vihod_Alligator_buy = true;
      return (false);
      }
   }
//--------------------------

bool Proverka_sell()
   {
   bool Otkryt_orders_sell = false;
   for (i = OrdersTotal() ; i>=0; i--)
      {
      Magic_number= Period();
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() == OP_SELL && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
      Otkryt_orders_sell= true;
      }
   if (Otkryt_orders_sell==true)
      return(true);
   else 
      {
      Vihod_Alligator_sell = true;
      return (false);
      }
   }
    
    
    
    
    
    
// Выставление ордеров на продажу
bool Prodazha()
   {
   Lot = AccountFreeMargin()/1000*risk; //Берем процент от маржи
   if (Lot >MaxLot) Lot = MaxLot;       //выравниваем его
   if (Lot <0.01) Lot = 0.01;
   if (EnableMartingail == true) TP=SL/koeff; //Если мартингейл вкл, то ТП не оптимизируем
   Magic_number=Period();
   Sleep (Zaderzhka);
   RefreshRates ();
   if (TP == 0)
      OrderSend(Symbol(),OP_SELL,Lot,Bid,1,Ask + SL*Point,0,text,Magic_number);
   else
      OrderSend(Symbol(),OP_SELL,Lot,Bid,1,Ask + SL*Point,Bid - TP*Point,text,Magic_number);
   if (EnableMartingail == true)     //Если мы используем мартингейл
      {
      for (i=1; i<=Koleno; i++)
         {
         Lot = Lot*2*koeff; //Коэффициент 1.5 даст увеличение следующего лота в 3 раза
         Sleep (Zaderzhka);
         RefreshRates ();
         OrderSend(Symbol(),OP_SELLLIMIT,Lot,Ask+(SL*Point*(i)-spred*Point),1,Ask + SL*Point*(i+1),Ask+TP*Point*(i-1),text,Magic_number);
         }
      }
   }


// Выставление ордеров на покупку
bool Pokupka()
   {
   double Lot = AccountFreeMargin()/1000*risk;
   if (Lot >MaxLot) Lot = MaxLot;
   if (Lot <0.01) Lot = 0.01;
   if (EnableMartingail == true) TP=SL/koeff;
   Magic_number=Period();
   Sleep (Zaderzhka);
   RefreshRates ();
   if (TP == 0)
      OrderSend(Symbol(),OP_BUY,Lot,Ask,1,Bid - SL*Point,0,text,Magic_number);
   else
      OrderSend(Symbol(),OP_BUY,Lot,Ask,1,Bid - SL*Point,Ask + TP*Point,text,Magic_number);
   if (EnableMartingail == true)
      {
      for (i=1; i<=Koleno; i++)
         {
         Lot = Lot*2*koeff;
         Sleep (Zaderzhka);
         RefreshRates ();
         OrderSend(Symbol(),OP_BUYLIMIT,Lot,Bid - (SL*Point*(i)-spred*Point),1,Bid - SL*Point*(i+1),Bid - TP*Point*(i-1),text,Magic_number);
         
         }
      }
   }


int Trailing_start ()
   {
   for (i = OrdersTotal() ; i>=0; i--) //ищем открытые ордера
      {
      RefreshRates();
      Magic_number= Period();
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() == OP_BUY && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
         {
         if (TP==0)
            double TrailingStopKoeff  = TrailingStop;
         else
            {
            //Коэффициент Трейлинга дает эффект прижимания SL к TP при приближении цены к TP
            TrailingStopKoeff = (OrderTakeProfit() - OrderOpenPrice())/(OrderTakeProfit () - Bid)*TrailingStop;
            //Если он больше заложенного в параметрах, то он становится ему равным
            if (TrailingStopKoeff >TrailingStop) TrailingStopKoeff  = TrailingStop; 
            }
         
         //Если SL двльше трейлинга, то SL = трейлингу
         if(Bid-OrderOpenPrice()>profit&&Bid>Point*TrailingStopKoeff +OrderOpenPrice()&&OrderStopLoss()<Bid-Point*TrailingStopKoeff )
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStopKoeff,OrderTakeProfit(),text,Blue);
            Sleep (Zaderzhka);
            Vihod_Alligator_buy = false;
            }
         }
      if(OrderType() == OP_SELL && OrderSymbol () ==Symbol() && OrderMagicNumber() == Magic_number)  
         {
         
         TrailingStopKoeff = (OrderOpenPrice()-OrderTakeProfit())/(Ask - OrderTakeProfit ())*TrailingStop;
         if (TrailingStopKoeff >TrailingStop) TrailingStopKoeff  = TrailingStop;
         if(OrderOpenPrice()-Ask>profit&&OrderOpenPrice()-Ask>Point*TrailingStopKoeff&&OrderStopLoss()>Ask+Point*TrailingStopKoeff )
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStopKoeff,OrderTakeProfit(),text,Blue);
            Sleep (Zaderzhka);
            Vihod_Alligator_sell = false;
            }
         }
      }
   return (0);
   }
   
   
double Fractal ()
   {
   up =0;         //Сброс сигналов
   down = 0;
   for (i=Fractal_bars;i>=3;i--)
      {
      double up_prov =iFractals(NULL, 0, MODE_UPPER, i); //В наших барах должен быть
      if (up_prov >up)                                   //фрактал
         up = up_prov;
      double down_prov=iFractals(NULL, 0, MODE_LOWER, i);
      if (down_prov>down)
         down = down_prov;
      }
   if (up<Ask+visota_fractal*Point)                      //И этот фрактал должен быть заметен
   up = 0;
   if (down<Bid-visota_fractal*Point)
   down = 0;
   }