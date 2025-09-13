//+------------------------------------------------------------------+
//|    Рекомендуется для 4H 1D                 dealers 7.49 MACD.mq4 |
//|                                         Copyright © 2006, Alex_N |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Alex_N"
#property link "asd-01@bk.ru"

extern int    MAGIC=12457814;
extern double TakeProfit = 30;         // Уровень тейк профита в пунктах 
extern double Lots = 0.1;              // Колличество лотов начальное
extern double InitialStop = 90;        // Уровень установки стоп ордера
extern double TrailingStop = 15;       // Уровень трейлинг стопа

extern int MaxTrades=5;                // Максимальное колличество ордеров
extern int Pips=4;                     // Интервал между ордерами
extern int SecureProfit=50;            // По замыслу создателей это защищаемый профит в баксах,
                                       // т.е. когда суммарный профит по всем ордерам достигает 
                                       // SecureProfit баксов, то один последний ордер закрывается.
extern int AccountProtection=1;        // Вот если этот параметр 1, то ордер и закрывается 
                                       // при достижении профита суммарного. А если нет, то нет.
extern int OrderstoProtect=3;          // а это количество якобы незакрываемых ордеров, но
                                       // из-за ошибок в программе там так сделано. Ордера закрываются если:
                                       // OpenOrders>=(MaxTrades-OrderstoProtect)
                                       // OpenOrders - количество открытых на данный момент ордеров всего.
                                       // MaxTrades - задано выше, пусть 5.
                                       // OrderstoProtect - пусть 3.
                                       // Таким образом ордера начинают закрываться если их больше или равно 2.
extern int ReverseCondition=0;         // Это такая фича, при изменении которой на 1 ордера начнут 
                                       // открываться против правил. Т.е. при 0 ордера открываются по правилам,
                                       // а при 1 в обратную сторону. Так иногда делают программеры не уверенные 
                                       // в своих программах. Типа раз сливает советник, давай развернём его
                                       // и пусть продаёт когда нужно покупать и наоборот.
extern int MACD_fast_ema_period=14;    // Первый параметр индикатора MACD
extern int MACD_slow_ema_period=26;    // Второй параметр индикатора MACD
extern int mm=0;                       // Способ управления мани менеджмент ММ=0 торговля виксированным лотом ММ=1 расчет по риску
extern int slippage=2;                 // Проскальзывание
extern int risk=2;                     // Риск при ММ=1
extern int MaxLots=5;                  // Максимально возможное колличкство лотов в позиции
extern int AccountisNormal=0;          // 0- у брокера разрешены дробные лоты. 1-запрещены.
extern double Doble=1.5;               // Множитель позиций каждая следующяя позиция умножается на Doble
extern double USDCHFPipValue=10.5;     // Цена пункта символа
extern double USDCADPipValue=10.4;     // Цена пункта символа
extern double USDJPYPipValue=9.2;      // Цена пункта символа
extern double EURJPYPipValue=9.8;      // Цена пункта символа
extern double EURUSDPipValue=10.3;     // Цена пункта символа
extern double GBPUSDPipValue=10;       // Цена пункта символа
extern double AUDUSDPipValue=9.9;      // Цена пункта символа
extern double NZDUSDPipValue=8.9;      // Цена пункта символа 

int  OpenOrders=0, cnt=0;
double sl=0, tp=0;
double BuyPrice=0, SellPrice=0;
double lotsi=0, mylotsi=0;
int mode=0, myOrderType=0;
bool ContinueOpening=True;
double LastPrice=0;
int  PreviousOpenOrders=0;
double Profit=0;
int LastTicket=0, LastType=0;
double LastClosePrice=0, LastLots=0;
double Pivot=0;
double PipValue=0;
string text="", text2="";

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
   if (AccountisNormal==1)
   {
	  if (mm!=0) { lotsi=MathCeil(AccountBalance()*risk/10000); }
		else { lotsi=Lots; }
   } else {  // then is mini
    if (mm!=0) { lotsi=MathCeil(AccountBalance()*risk/10000)/10; }
		else { lotsi=Lots; }
   }

   if (lotsi>MaxLots){ lotsi=MaxLots; }
   
   OpenOrders=0;
   for(cnt=0;cnt<OrdersTotal();cnt++)   
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC)
	  {				
	  	  OpenOrders++;
	  }
   }     
   
   if (OpenOrders<1) 
   if (Symbol()=="USDCHF") { PipValue=USDCHFPipValue; }
   if (Symbol()=="USDCAD") { PipValue=USDCADPipValue; }
   if (Symbol()=="USDJPY") { PipValue=USDJPYPipValue; }
   if (Symbol()=="EURJPY") { PipValue=EURJPYPipValue; }
   if (Symbol()=="EURUSD") { PipValue=EURUSDPipValue; }
   if (Symbol()=="GBPUSD") { PipValue=GBPUSDPipValue; }
   if (Symbol()=="AUDUSD") { PipValue=AUDUSDPipValue; }
   if (Symbol()=="NZDUSD") { PipValue=NZDUSDPipValue; }
   if (PipValue==0) { PipValue=5; }
   
   if (PreviousOpenOrders>OpenOrders) 
   {	  
	  for(cnt=OrdersTotal();cnt>=0;cnt--)
	  {
	     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
		  if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
		  {
	  	   mode=OrderType();
			if (mode==OP_BUY) { OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage,Blue); }
			if (mode==OP_SELL) { OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage,Red); }
			return(0);
		 }
	  }
   }

   PreviousOpenOrders=OpenOrders;
   if (OpenOrders>=MaxTrades) 
   {
	  ContinueOpening=False;
   } else {
	  ContinueOpening=True;
   }

   if (LastPrice==0) 
   {
	  for(cnt=0;cnt<OrdersTotal();cnt++)
	  {	
	    OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
		 if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
		 {
		   mode=OrderType();	
			LastPrice=OrderOpenPrice();
			if (mode==OP_BUY) { myOrderType=2; }
			if (mode==OP_SELL) { myOrderType=1;	}
		 }
	  }
   }

   if (OpenOrders<1) 
   {
	  myOrderType=3;
	  if (iMACD(NULL,0,MACD_fast_ema_period,MACD_slow_ema_period,1,PRICE_CLOSE,MODE_MAIN,0)>iMACD
	  (NULL,0,MACD_fast_ema_period,MACD_slow_ema_period,1,PRICE_CLOSE,MODE_MAIN,1)) { myOrderType=2; }
	  if (iMACD(NULL,0,MACD_fast_ema_period,MACD_slow_ema_period,1,PRICE_CLOSE,MODE_MAIN,0)
	  <iMACD(NULL,0,MACD_fast_ema_period,MACD_slow_ema_period,1,PRICE_CLOSE,MODE_MAIN,1)) { myOrderType=1; }
	  if (ReverseCondition==1)
	  {
	  	  if (myOrderType==1) { myOrderType=2; }
		  else { if (myOrderType==2) { myOrderType=1; } }
	  }
   }

   // if we have opened positions we take care of them
   for(cnt=OrdersTotal();cnt>=0;cnt--)
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  if (OrderSymbol() == Symbol() && OrderMagicNumber()==MAGIC) 
	  {
	  	  if (OrderType()==OP_SELL) 
	  	  {
	  	  	  if (TrailingStop>0) 
			  {
				  if (OrderOpenPrice()-Ask>=(TrailingStop+Pips)*Point) 
				  {
					 if (OrderStopLoss()>(Ask+Point*TrailingStop))
					 {
					    OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,
					    OrderClosePrice()-TakeProfit*Point-TrailingStop*Point,800,Purple);
	  					 return(0);	  					
	  				 }
	  			  }
			  }
	  	  }
   
	  	  if (OrderType()==OP_BUY)
	  	  {
	  		 if (TrailingStop>0) 
	  		 {
			   if (Bid-OrderOpenPrice()>=(TrailingStop+Pips)*Point) 
				{
					if (OrderStopLoss()<(Bid-Point*TrailingStop)) 
					{
					   OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,
					   OrderClosePrice()+TakeProfit*Point+TrailingStop*Point,800,Yellow);
                  return(0);
					}
  				}
			 }
	  	  }
   	}
   }
   
   Profit=0;
   LastTicket=0;
   LastType=0;
	LastClosePrice=0;
	LastLots=0;	
	for(cnt=0;cnt<OrdersTotal();cnt++)
	{
	  OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  if (OrderSymbol()==Symbol() && OrderMagicNumber()==MAGIC) 
	  {
	  	   LastTicket=OrderTicket();
			if (OrderType()==OP_BUY) { LastType=OP_BUY; }
			if (OrderType()==OP_SELL) { LastType=OP_SELL; }
			LastClosePrice=OrderClosePrice();
			LastLots=OrderLots();
			if (LastType==OP_BUY) 
			{
				//Profit=Profit+(Ord(cnt,VAL_CLOSEPRICE)-Ord(cnt,VAL_OPENPRICE))*PipValue*Ord(cnt,VAL_LOTS);				
				if (OrderClosePrice()<OrderOpenPrice())
					{ Profit=Profit-(OrderOpenPrice()-OrderClosePrice())*OrderLots()/Point; }
				if (OrderClosePrice()>OrderOpenPrice())
					{ Profit=Profit+(OrderClosePrice()-OrderOpenPrice())*OrderLots()/Point; }
			}
			if (LastType==OP_SELL) 
			{
				//Profit=Profit+(Ord(cnt,VAL_OPENPRICE)-Ord(cnt,VAL_CLOSEPRICE))*PipValue*Ord(cnt,VAL_LOTS);
				if (OrderClosePrice()>OrderOpenPrice()) 
					{ Profit=Profit-(OrderClosePrice()-OrderOpenPrice())*OrderLots()/Point; }
				if (OrderClosePrice()<OrderOpenPrice()) 
					{ Profit=Profit+(OrderOpenPrice()-OrderClosePrice())*OrderLots()/Point; }
			}
			//Print(Symbol,":",Profit,",",LastLots);
	  }
   }
	
	Profit=Profit*PipValue;
	text2="Profit: $"+DoubleToStr(Profit,2)+" +/-";
   if (OpenOrders>=(MaxTrades-OrderstoProtect) && AccountProtection==1) 
   {	    
	     //Print(Symbol,":",Profit);
	     if (Profit>=SecureProfit) 
	     {
	        OrderClose(LastTicket,LastLots,LastClosePrice,slippage,Yellow);		 
	        ContinueOpening=False;
	        return(0);
	     }
   }

      if (!IsTesting()) 
      {
	     if (myOrderType==3) { text="No conditions to open trades"; }
	     else { text="                         "; }
	     Comment("LastPrice=",LastPrice," Previous open orders=",PreviousOpenOrders,
	     "\nContinue opening=",ContinueOpening," OrderType=",myOrderType,"\n",text2,"\nLots=",lotsi,"\n",text);
      }

      if (myOrderType==1 && ContinueOpening) 
      {	
	     if ((Bid-LastPrice)>=Pips*Point || OpenOrders<1) 
	     {		
		    SellPrice=Bid;				
		    LastPrice=0;
		    if (TakeProfit==0) { tp=0; }
		    else { tp=SellPrice-TakeProfit*Point; }	
		    if (InitialStop==0) { sl=0; }
		    else { sl=SellPrice+InitialStop*Point;  }
		    if (OpenOrders!=0) 
		    {
			      mylotsi=lotsi;			
			      for(cnt=1;cnt<=OpenOrders;cnt++)
			      {
				     if (MaxTrades>MaxTrades) { mylotsi=NormalizeDouble(mylotsi*Doble,1); }
				     else { mylotsi=NormalizeDouble(mylotsi*Doble,1); }
			      }
		    } else { mylotsi=lotsi; }
		    if (mylotsi>MaxLots) { mylotsi=MaxLots; }
		    OrderSend(Symbol(),OP_SELL,mylotsi,SellPrice,slippage,sl,tp,NULL,MAGIC,0,Red);		    		    
		    return(0);
	     }
      }
      
      if (myOrderType==2 && ContinueOpening) 
      {
	     if ((LastPrice-Ask)>=Pips*Point || OpenOrders<1) 
	     {		
		    BuyPrice=Ask;
		    LastPrice=0;
		    if (TakeProfit==0) { tp=0; }
		    else { tp=BuyPrice+TakeProfit*Point; }	
		    if (InitialStop==0)  { sl=0; }
		    else { sl=BuyPrice-InitialStop*Point; }
		    if (OpenOrders!=0) {
			   mylotsi=lotsi;			
			   for(cnt=1;cnt<=OpenOrders;cnt++)
			   {
				  if (MaxTrades>MaxTrades) { mylotsi=NormalizeDouble(mylotsi*Doble,1); }
				  else { mylotsi=NormalizeDouble(mylotsi*Doble,1); }
			   }
		    } else { mylotsi=lotsi; }
		    if (mylotsi>MaxLots) { mylotsi=MaxLots; }
		    OrderSend(Symbol(),OP_BUY,mylotsi,BuyPrice,slippage,sl,tp,NULL,MAGIC,0,Blue);		    
		    return(0);
	     }
      }   

//----
   return(0);
  }
//+------------------------------------------------------------------+