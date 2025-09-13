//+------------------------------------------------------------------+
//|                                             gazonkos expert.mq4  |
//|                                                    1H   EUR/USD  |
//|                                                    Smirnov Pavel |
//|                                                 www.autoforex.ru |
//+------------------------------------------------------------------+

#property copyright "Smirnov Pavel"
#property link      "www.autoforex.ru"

extern int magic = 12345;
extern int TakeProfit = 16; // Уровень тейкпрофит в пунктах
extern int Otkat = 16;// Величина отката в пунктах
extern int StopLoss = 40; // уровень стоплосс в пунктах

extern int t1=3;
extern int t2=2;
extern int delta=40;

extern double lot = 0.1;// Размер позиции
extern int active_trades=1;//Максимальное количество одновременно открытых ордеров

int STATE=0;
int Trade=0;
double maxprice=0.0;
double minprice=10000.0;
int ticket;
bool cantrade=true;
int LastTradeTime=0;
int LastSignalTime=0;

int OpenLong(double volume=0.1)
{
  int slippage=10;
  string comment="gazonkos expert (Long)";
  color arrow_color=Blue;

  ticket=OrderSend(Symbol(),OP_BUY,volume,Ask,slippage,Ask-StopLoss*Point,
                      Ask+TakeProfit*Point,comment,magic,0,arrow_color);
  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
    {
      Print("Buy order opened : ",OrderOpenPrice());
      return(0);
    }  
  }
  else 
  {
    Print("Error opening Buy order : ",GetLastError()); 
    return(-1);
  }
}
  
int OpenShort(double volume=0.1)
{
  int slippage=10;
  string comment="gazonkos expert (Short)";
  color arrow_color=Red;

  ticket=OrderSend(Symbol(),OP_SELL,volume,Bid,slippage,Bid+StopLoss*Point,
                      Bid-TakeProfit*Point,comment,magic,0,arrow_color);
  if(ticket>0)
  {
    if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
      {
        Print("Sell order opened : ",OrderOpenPrice());
        return(0);
      }  
  }
  else 
  {
    Print("Error opening Sell order : ",GetLastError()); 
    return(-1);
  }
}

int OrdersTotalMagic(int MagicValue)//функция возвращает количество открытых ордеров с magic = MagicValue
{
   int j=0;
   int i;
   for (i=0;i<OrdersTotal();i++)//Производим просмотр среди всех открытых ордеров
   {
     if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))//Выбираем по-порядку ордера
     {
        if (OrderMagicNumber()==MagicValue) j++; //Подсчитываем только те у котроых нужный magic   
     }
     else 
     {    
         Print("gazonkos expert: OrderSelect() в OrdersTotalMagic() вернул ошибку - ",GetLastError());    
         return(-1);
     }
   }   
   return(j);//Возвращаем количество подсчитанных ордеров с magic = MagicValue.  
}

int init()
{
  return(0);
}

int deinit()
{   
  return(0);
}

int start()
{

// STATE = 0  Ждем сигнала к началу работы советника  ------------------------------------------------------------

   if (STATE==0)
   {
      bool cantrade=true;
      if(TimeHour(TimeCurrent())==LastTradeTime) cantrade=false;//запрещаем торговать пока не наступит новый час после последней 
                                                                //открытой сделки (чтобы избежать множественных открываний сделок на одном и том же часовом баре)     
      if(OrdersTotalMagic(magic)>=active_trades) cantrade=false;// проверяем на допустимое количество открытых ордеров
      if(cantrade) // если не было ни одного запрета на открытие сделок, то переходим к ожиданию сигналов системы на открытие ордеров
         STATE=1;
   }

// STATE = 1  Ждем импульса (движения) цены ----------------------------------------------------------------------

   if (STATE==1)
   {
      if((Close[t2]-Close[t1])>delta*Point)// сигнал для входа в длинную позицию
      {
         Trade = 1; //идентификатор позиции, для которой получен сигнал на открытие  "-1" - короткая позиция, "1"-длинная
         maxprice=Bid;// запоминаем текущее положение цены (необходимо для определения отката в STATE=2)
         LastSignalTime=TimeHour(TimeCurrent());//Запоминаем время получения сигнала
         STATE = 2; // перейти в следующее состояние
      }
      
      if((Close[t1]-Close[t2])>delta*Point)// сигнал для входа в короткую позицию
      {
         Trade = -1; // идентификатор позиции, для которой получен сигнал на открытие  "-1" - короткая позиция, "1"-длинная
         minprice=Bid;// запоминаем текущее положение цены (необходимо для определения отката в STATE=2)
         LastSignalTime=TimeHour(TimeCurrent());//Запоминаем время получения сигнала
         STATE = 2; // перейти в следующее состояние
      }
   }
   
// STATE = 2 - Ждем отката цены   -------------------------------------------------------------------------------- 

   if (STATE==2)
   {
      if(LastSignalTime!=TimeHour(TimeCurrent()))//Если на баре на котором получен сигнал не произошло отката,то переходим в состояние STATE=0
      {   
         STATE=0;
         return(0);         
      }
      if(Trade==1)// ожидаем отката для длинной позиции
      {
         if(Bid>maxprice) maxprice=Bid;//если цена пошла еще выше, то меняем значение maxprice на текущее значение цены
         if(Bid<(maxprice-Otkat*Point))// проверяем наличие отката цены после импульса 
            STATE=3;//если произошел откат на величину Otkat, то переходим в состояние открытия длинной позиции
      }
      
      if(Trade==-1)// ожидаем отката для короткой позиции
      {
         if(Bid<minprice) minprice=Bid;//если цена пошла еще ниже, то меняем значение minprice на текущее значение цены
         if(Bid>(minprice+Otkat*Point))// проверяем наличие отката цены после импульса
            STATE=3;//если произошел откат на величину Otkat, то переходим в состояние открытия короткой позиции
      }
   }  

// STATE = 3 - открываем позиции согласно переменной Trade ("-1" - короткую, "1" - длинную)   -------------------- 
  
   if(STATE==3)
   {
      if(Trade==1)// открываем длинную позицию
      {
         OpenLong(lot);// открываем длинную позицию
         LastTradeTime=TimeHour(TimeCurrent());//запоминаем время совершения последней сделки
         STATE=0; //переходим в состояние ожидания
      }
      if(Trade==-1)// открываем короткую позицию
      {
         OpenShort(lot);// открываем короткую позицию  
         LastTradeTime=TimeHour(TimeCurrent());//запоминаем время совершения последней сделки
         STATE=0; //переходим в состояние ожидания
      }   
   }  
  return(0);
}