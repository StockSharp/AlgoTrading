//+------------------------------------------------------------------+
//|                                                   FurureMACD.mq4 |
//|                     Copyright © 2008, Демёхин Виталий Евгеньевич |
//|                                             vitalya_1983@list.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Демёхин Виталий Евгеньевич"
#property link      "vitalya_1983@list.ru"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
extern int  Kol_vo_sovpadenii =  5,
            tocnost           =  10,
            analiz_bars       =  8,
            poisk_fractals    =  4,
            minTP             =  30;
extern bool TrailingStop      =  false,
            Ruchnik           =  false,
            dokupka           =  true;

extern double  Stat_Take_Profit  =  0.5,
               zabyvaemost    =  1.5;
bool Proverka_buy,Proverka_sell;
string nash_grafik;
static datetime New_time;
int i,magic_number;
int init()
  {
//----
   magic_number = Period();
   nash_grafik = Symbol() +"_"+ Period ();
      New_time=Time[0];
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   nash_grafik = NULL;
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
//----------------------------------------------составление последовательности баров
   if (TrailingStop)
      Trailing_start ();
   if (New_time!=Time[0])
   {
      int history [1000];
      
      for (int i=analiz_bars+poisk_fractals;i>poisk_fractals;i--)
         {
         history [i] = NormalizeDouble((tocnost*(iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,i)-iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,i)))/(100*Point),0);
         }
      
   //--------------------------------открываем или создаем файл с данной последовательностью
      
      for (i=analiz_bars+poisk_fractals;i>poisk_fractals;i--)
         {
         string posledovatelnost =posledovatelnost+history [i]+"_";
         }
      posledovatelnost = posledovatelnost +".csv";
      int nash_file=FileOpen ("FutureMACD_"+nash_grafik+"/"+posledovatelnost, FILE_CSV|FILE_READ,',');
      if (nash_file<0)
         {
         bool new_file=true;
         }
      else 
         {
         new_file=false;
         FileClose (nash_file);
         }
      nash_file=FileOpen ("FutureMACD_"+nash_grafik+"/"+posledovatelnost, FILE_CSV|FILE_READ|FILE_WRITE,',');
   
   //---------------------------------------------------------определяем среди них фракталы
      double MaxHighPik = High [poisk_fractals]; //Начинаем сравнивать бары
      double MaxLowPik = Low [poisk_fractals];  
      for (i=poisk_fractals; i>=1; i--)
         {
         if (MaxHighPik < High [i])
            {
            MaxHighPik = High [i];     
            }
         if (MaxLowPik > Low [i])
            {
            MaxLowPik = Low [i];       
            }
         }
         
      double Fractal_Up = (MaxHighPik - Open [poisk_fractals])/Point;
      double Fractal_Down = (Open [poisk_fractals]-MaxLowPik)/Point;
   //-------------------------------------------------редактируем наш файл
      double setka [1] [1];
   /*
         |кол-во обращений |Тейкпрофит |
         |                 |           |
   ------------------------------------|
   Buy   |        0        |     0     |
   ------------------------------------|
   Sell  |        0        |     0     |
   */ if (!new_file)
         {
         FileSeek (nash_file, 0,SEEK_SET);
         int Buy_Kol_vo_obraschenii    = FileReadNumber (nash_file);
         int Buy_Take_Profit           = FileReadNumber (nash_file);
         int Sell_Kol_vo_obraschenii   = FileReadNumber (nash_file);
         int Sell_Take_Profit          = FileReadNumber (nash_file);
         }
      if (new_file)
         {
         Buy_Kol_vo_obraschenii        = 0;
         Buy_Take_Profit               = 0;
         Sell_Kol_vo_obraschenii       = 0;
         Sell_Take_Profit              = 0;
         }
   //----------------------------------------------------Работа с данными
      if (Fractal_Up>=Fractal_Down)
         {
         Buy_Kol_vo_obraschenii++;
         Buy_Take_Profit=NormalizeDouble((Buy_Take_Profit+Fractal_Up*zabyvaemost)/(1+zabyvaemost),0);
         Sell_Take_Profit=NormalizeDouble((Sell_Take_Profit-Fractal_Up*zabyvaemost)/(1+zabyvaemost),0);
         }
      if (Fractal_Down>=Fractal_Up)
         {
         Sell_Kol_vo_obraschenii++;
         Sell_Take_Profit=NormalizeDouble((Sell_Take_Profit+Fractal_Down*zabyvaemost)/(1+zabyvaemost),0);
         Buy_Take_Profit=NormalizeDouble((Buy_Take_Profit-Fractal_Down*zabyvaemost)/(1+zabyvaemost),0);
         }
//-------------------------------------------------------------------запись в файл
      FileSeek (nash_file,0,SEEK_SET);
      FileWrite (nash_file,
                           Buy_Kol_vo_obraschenii,
                           Buy_Take_Profit);
                        
      FileWrite (nash_file,
                           Sell_Kol_vo_obraschenii,
                           Sell_Take_Profit);
      FileClose (nash_file);
      
         
//------------------------------------------------------------------ищем совпадения в графике         
      for (i=analiz_bars+poisk_fractals;i>poisk_fractals;i--)
         {
         history [i] = NormalizeDouble((tocnost*(iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,i)-iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,i)))/(100*Point),0);
         }
      
//---------------------------------------------------открываем файл с данной последовательностью
      
      
      for (i=analiz_bars;i>=1;i--)
         {
         string nasha_posledovatelnost=nasha_posledovatelnost+history [i]+"_";
         }
      nasha_posledovatelnost= nasha_posledovatelnost +".csv";
      nash_file=FileOpen ("FutureMACD_"+nash_grafik+"/"+nasha_posledovatelnost, FILE_CSV|FILE_READ,',');
      if (nash_file>0)
         {
         FileClose (nash_file);
         nash_file=FileOpen ("FutureMACD_"+nash_grafik+"/"+posledovatelnost, FILE_CSV|FILE_READ|FILE_WRITE,',');
         FileSeek (nash_file, 0,SEEK_SET);
         Buy_Kol_vo_obraschenii    = FileReadNumber (nash_file);
         Buy_Take_Profit           = FileReadNumber (nash_file);
         Sell_Kol_vo_obraschenii   = FileReadNumber (nash_file);
         Sell_Take_Profit          = FileReadNumber (nash_file);
         FileClose (nash_file);
         if (!Proverka_buy ()||(dokupka&&New_time!= Time [0])) 
            {
            if (!Ruchnik&& Buy_Take_Profit>=Sell_Take_Profit)// Если Аллигатор дал команду или не мешает...
               {
               double TP = NormalizeDouble ((Buy_Take_Profit*Stat_Take_Profit)*Point,Digits);
               for (i=OrdersTotal();i>=1;i--)
                  {
                  OrderSelect (i-1,SELECT_BY_POS,MODE_TRADES);
                  if (OrderType () == OP_SELL&&OrderSymbol() == Symbol()&& OrderMagicNumber () == magic_number)
                     {
                     OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);
                     }
                  }
               if (Buy_Kol_vo_obraschenii>Kol_vo_sovpadenii&&Buy_Take_Profit>minTP)
                  {
                  OrderSend (Symbol(),OP_BUY, 0.1,Ask,3,Bid-Buy_Take_Profit*Point,Bid+TP,0,magic_number);
                  }
               }
            }
         if (!Proverka_sell()||(dokupka&&New_time != Time[0])) //Если открытых ордеров нет...
            {
            if (!Ruchnik&& Buy_Take_Profit<=Sell_Take_Profit)
               {
               for (i=OrdersTotal();i>=1;i--)
                  {
                  OrderSelect (i-1,SELECT_BY_POS,MODE_TRADES);
                  if (OrderType () == OP_BUY&&OrderSymbol() == Symbol()&& OrderMagicNumber () == magic_number)
                     {
                     OrderClose(OrderTicket(),OrderLots(),Bid,3,Blue);
                     }
                  }
               if (Sell_Kol_vo_obraschenii>Kol_vo_sovpadenii&&Sell_Take_Profit>minTP)
                  {
                  TP = NormalizeDouble ((Sell_Take_Profit*Stat_Take_Profit)*Point,Digits);
                  OrderSend (Symbol(),OP_SELL, 0.1,Bid,3,Ask+Sell_Take_Profit*Point,Ask-TP,0,magic_number);
                  }
               }
            }
         }
      }
//----
   return(0);
  }
//+------------------------------------------------------------------+
   bool Proverka_buy()
   {
   bool Otkryt_orders_buy = false; //флаг "открытых ордеров нет"
   for (i = OrdersTotal(); i>=1; i--)
      {
      OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() == OP_BUY && OrderSymbol () ==Symbol() && OrderMagicNumber() == magic_number)
      Otkryt_orders_buy = true; //Попался!!!
      }
   if (Otkryt_orders_buy==true)
      return(true);
   else 
      {
      return (false);
      }
   }
//--------------------------

bool Proverka_sell()
   {
   bool Otkryt_orders_sell = false;
   for (i = OrdersTotal() ; i>=1; i--)
      {
      
      OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() == OP_SELL && OrderSymbol () ==Symbol() && OrderMagicNumber() == magic_number)  
      Otkryt_orders_sell= true;
      }
   if (Otkryt_orders_sell==true)
      return(true);
   else 
      {
      return (false);
      }
   }

//------------------------------------------------------------
int Trailing_start ()
   {
   
   for (i = OrdersTotal(); i>=1; i--)
      {
      RefreshRates();
      OrderSelect(i-1, SELECT_BY_POS, MODE_TRADES);
      
      if(OrderType() == OP_BUY && OrderSymbol () ==Symbol() && OrderMagicNumber() == magic_number)
         {
         int Trailing = NormalizeDouble ((OrderTakeProfit()-OrderOpenPrice())/4,0);
         if(Bid>Point*Trailing +OrderOpenPrice()&&OrderStopLoss()<Bid-Point*Trailing)
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*Trailing,OrderTakeProfit(),0,0);
            Sleep (1000);
            }
         }
      if(OrderType() == OP_SELL && OrderSymbol () ==Symbol() && OrderMagicNumber() == magic_number)
         {
          Trailing = NormalizeDouble ((OrderOpenPrice()-OrderTakeProfit())/4,0);
         if(OrderOpenPrice()-Ask>Point*Trailing&&OrderStopLoss()>Ask+Point*Trailing)
            {
            OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*Trailing,OrderTakeProfit(),0,0);
            Sleep (1000);
            }
         }  
      }
   return (0);
   }