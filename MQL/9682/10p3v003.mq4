//+------------------------------------------------------------------+
//|                                                     10point3.mq4 |
//|                              Copyright © 2005, Alejandro Galindo |
//|                                   Revised version from davidke20 |
//|                 Settings by Marcel Corzo - marcelcorzo@yahoo.com |

//+------------------------------------------------------------------+

#property copyright "Copyright 2008, enhanced K LAM"
#property link "http://FxKillU.com"

extern int    Magic              = 772188;
extern double TakeProfit         = 45;
extern double Lots               = 0.01;
extern double InitialStop        = 0; //300
extern double TrailingStop       = 45;
extern int    MaxTrades          = 10;
extern double Multiplier         = 2;
extern int    Pips               = 30;
extern int    OrderstoProtect    = 5;
extern bool   Money_management   = false;
extern int    AccountType        = 2;        //0: Standard account(NorthFinance,MiG,Alpari) 1: Normal account(FXLQ,FXDD) 2:InterbankFX's NANO Account
extern double risk               = 0.5;
extern bool   ReverseSignal      = false;
extern int    Fast_EMA           = 12;
extern int    Slow_EMA           = 26;
extern int    Signal_SMA         = 9;
extern int    Shift              = 1;
extern int    TradingRange       = 0;

extern bool    UseTimeFilter=false;
extern int     StopTrade = 18;
extern int     StartTrade = 19;

datetime TimeStamp=0;
int  OpenOrders=0, cnt=0;
int  slippage=5;
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
double PipValue=0;
string text="", text2="",text3="",text4="";

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

   double MinLots = NormalizeDouble((MarketInfo(Symbol(), MODE_MINLOT)),2);
   double MaxLots = NormalizeDouble((MarketInfo(Symbol(), MODE_MAXLOT)),2);
   double LotSizeValue = NormalizeDouble((MarketInfo(Symbol(), MODE_LOTSIZE)),0);
   double PipValue=MarketInfo(Symbol(),MODE_TICKVALUE);

   if(Money_management)
   {
      switch(AccountType)
      {
         case 0: lotsi=NormalizeDouble(MathCeil((risk*AccountEquity())/10000)/10,1); break;
         case 1: lotsi=NormalizeDouble((risk*AccountEquity())/100000,2); break;
         case 2: lotsi=NormalizeDouble((risk*AccountEquity())/1000,2); break;
         default: lotsi=NormalizeDouble(MathCeil((risk*AccountEquity())/10000)/10,1); break;
      }
   }
   else
   {
      lotsi=Lots;
   }
   
   if(lotsi<MinLots) {lotsi=MinLots;}
   if(lotsi>MaxLots) {lotsi=MaxLots;}
   
   OpenOrders=0;
   for(cnt=0;cnt<OrdersTotal();cnt++) {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  if (OrderSymbol()==Symbol() && OrderMagicNumber() == Magic) {				
	     OpenOrders++;
	  }
   }// for
   
   if (PreviousOpenOrders>OpenOrders) { //close all in Magic
	  for(cnt=OrdersTotal();cnt>=0;cnt--) {
	     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  	  mode=OrderType();
		  if (OrderSymbol()==Symbol() && OrderMagicNumber() == Magic) {
			if (mode==OP_BUY) { OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage,Blue); }
			if (mode==OP_SELL) { OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage,Red); }
			return(0);
		 }
	  }
   }

   PreviousOpenOrders=OpenOrders;
   if (OpenOrders>=MaxTrades) {
	  ContinueOpening=False;
   } else {
	  ContinueOpening=True;
   }

   if (LastPrice==0) {// load last price and the myordertype
	  for(cnt=0;cnt<OrdersTotal();cnt++) {	
	    OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
		 mode=OrderType();	
		 if (OrderSymbol()==Symbol() && OrderMagicNumber() == Magic) {
			LastPrice=OrderOpenPrice();
			if (mode==OP_BUY || mode==OP_SELL) {
			   myOrderType=mode; } // other type not Set
		 }
	  }
   }

   if (OpenOrders<1) {
     if (UseTimeFilter)
     {//check trading time
        if (Hour()>StopTrade && Hour()<StartTrade) {
        Comment("No trading, danger time zone!");
        return(0);}//End of trading time check
     }

	     myOrderType=MACD();

     	  if (ReverseSignal) {
     	      if (myOrderType==OP_SELL) { myOrderType=OP_BUY; }
     	      else { if (myOrderType==OP_BUY) { myOrderType=OP_SELL; } }
	     }
   }//if (OpenOrders<1) {

   // if we have opened positions we take care of them
   for(cnt=OrdersTotal();cnt>=0;cnt--) {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  if (OrderSymbol() == Symbol() && OrderMagicNumber() == Magic) {

	  	  if (OrderType()==OP_SELL) {
	  	  	  if (TrailingStop>0) {
				  if (OrderOpenPrice()-Ask>=(TrailingStop*Point+Pips*Point)) {						
					 if (OrderStopLoss()>(Ask+Point*TrailingStop)) {
					    OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderClosePrice()-TakeProfit*Point-TrailingStop*Point,800,Purple);
	  					 return(0);	  					
	  				 }
	  			  }
			  }
	  	  }
   
	  	  if (OrderType()==OP_BUY) {
	  		 if (TrailingStop>0) {
			   if (Bid-OrderOpenPrice()>=(TrailingStop*Point+Pips*Point)) {
					if (OrderStopLoss()<(Bid-Point*TrailingStop)) {
					   OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderClosePrice()+TakeProfit*Point+TrailingStop*Point,800,Yellow);
                  return(0);
					}
  				}
			 }
	  	  }
	  	  
   	}
   }//for
   
   Profit=0;
   LastTicket=0;
   LastType=0;
	LastClosePrice=0;
	LastLots=0;	
	for(cnt=0;cnt<OrdersTotal();cnt++) {
	  OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  if (OrderSymbol()==Symbol() && OrderMagicNumber() == Magic) {
	  	   LastTicket=OrderTicket();
	  	   
			if (OrderType()==OP_BUY || OrderType()==OP_SELL) { LastType=OrderType(); }
			
			LastClosePrice=OrderClosePrice();
			LastLots=OrderLots();
			//Profit+=OrderProfit();
			
			if (LastType==OP_BUY) {
				//Profit=Profit+(Ord(cnt,VAL_CLOSEPRICE)-Ord(cnt,VAL_OPENPRICE))*PipValue*Ord(cnt,VAL_LOTS);				
				if (OrderClosePrice()<OrderOpenPrice()) 
					{ Profit=Profit-(OrderOpenPrice()-OrderClosePrice())*OrderLots()/Point; }
				if (OrderClosePrice()>OrderOpenPrice()) 
					{ Profit=Profit+(OrderClosePrice()-OrderOpenPrice())*OrderLots()/Point; }
			}
			
			if (LastType==OP_SELL) {
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
   if (OpenOrders>=OrderstoProtect)
   {	    
	     //Print(Symbol,":",Profit);
	     if ((Profit>=(AccountBalance()*(risk/100) && Money_management)) || (Profit>=(lotsi*(LotSizeValue/100)) && !Money_management))
	     {
	        OrderClose(LastTicket,LastLots,LastClosePrice,slippage,Yellow);		 
	        ContinueOpening=False;
	        return(0);
	     }
   }

      if (!IsTesting()) 
      {
	     if (myOrderType==99) { text="No conditions to open trades"; }
	     else { text="                         "; }
	     Comment("Buy range: ",text3,"\nSellRange: ",text4,"\n","LastPrice=",LastPrice," Previous open orders=",PreviousOpenOrders,"\nContinue opening=",ContinueOpening," OrderType=",myOrderType,"\n",text2,"\nLots=",lotsi,"\n",text,
	     "\nProperty of Alejandro Gallindo","\nhttp://elcactus.com\nhttp://www.forex-tsd.com");
      }

      if (myOrderType==OP_SELL && ContinueOpening) 
      {	
	     if ((Bid-LastPrice)>=Pips*Point || OpenOrders<1) 
	     {		
		    SellPrice=Bid;				
		    LastPrice=0;
		    if (TakeProfit==0) { tp=0; }
		    else { tp=SellPrice-TakeProfit*Point; }	
		    if (InitialStop==0) { sl=0; }
		    else { sl=NormalizeDouble(SellPrice+InitialStop*Point + (MaxTrades-OpenOrders)*Pips*Point, Digits);  }
		    if (OpenOrders!=0) 
		    {
			      mylotsi=lotsi;			
			      for(cnt=1;cnt<=OpenOrders;cnt++)
			      {
				     if (MaxTrades>12) { mylotsi=NormalizeDouble(mylotsi*1.5,2); }
				     else { mylotsi=NormalizeDouble(mylotsi*Multiplier,2); }
			      }
		    } else { mylotsi=lotsi; }
		    if (mylotsi>100) { mylotsi=100; }
		    OrderSend(Symbol(),OP_SELL,mylotsi,SellPrice,slippage,sl,tp,"10p3-Sell",Magic,0,Red);		    		    
		    return(0);
	     }
      }
      
      if (myOrderType==OP_BUY && ContinueOpening) 
      {
	     if ((LastPrice-Ask)>=Pips*Point || OpenOrders<1) 
	     {		
		    BuyPrice=Ask;
		    LastPrice=0;
		    if (TakeProfit==0) { tp=0; }
		    else { tp=BuyPrice+TakeProfit*Point; }	
		    if (InitialStop==0)  { sl=0; }
		    else { sl=NormalizeDouble(BuyPrice-InitialStop*Point - (MaxTrades-OpenOrders)*Pips*Point, Digits); }
		    if (OpenOrders!=0) {
			   mylotsi=lotsi;			
			   for(cnt=1;cnt<=OpenOrders;cnt++)
			   {
				  if (MaxTrades>12) { mylotsi=NormalizeDouble(mylotsi*1.5,2); }
				  else { mylotsi=NormalizeDouble(mylotsi*Multiplier,2); }
			   }
		    } else { mylotsi=lotsi; }
		    if (mylotsi>100) { mylotsi=100; }
		    OrderSend(Symbol(),OP_BUY,mylotsi,BuyPrice,slippage,sl,tp,"10p3-Buy",Magic,0,Blue);		    
		    return(0);
	     }
      }   

//----
   return(0);
  }
//+------------------------------------------------------------------+

int MACD()
{
   myOrderType=99;

   double MACDMainCurr=iMACD(NULL,0,Fast_EMA,Slow_EMA,Signal_SMA,PRICE_CLOSE,MODE_MAIN,Shift);
   double MACDSigCurr=iMACD(NULL,0,Fast_EMA,Slow_EMA,Signal_SMA,PRICE_CLOSE,MODE_SIGNAL,Shift);
   double MACDMainPre=iMACD(NULL,0,Fast_EMA,Slow_EMA,Signal_SMA,PRICE_CLOSE,MODE_MAIN,Shift+1);
   double MACDSigPre=iMACD(NULL,0,Fast_EMA,Slow_EMA,Signal_SMA,PRICE_CLOSE,MODE_SIGNAL,Shift+1);

   double SellRange=TradingRange*Point;
   double BuyRange=(TradingRange-(TradingRange*2))*Point;

   if(MACDMainCurr>MACDSigCurr && MACDMainPre<MACDSigPre && MACDSigPre<BuyRange && MACDMainCurr<0 && TimeStamp!=iTime(Symbol(),0,0))
    {myOrderType=OP_BUY; TimeStamp=iTime(Symbol(),0,0);}
   if(MACDMainCurr<MACDSigCurr && MACDMainPre>MACDSigPre && MACDSigPre>SellRange && MACDMainCurr>0 && TimeStamp!=iTime(Symbol(),0,0))
    {myOrderType=OP_SELL; TimeStamp=iTime(Symbol(),0,0);}
   
   text3=DoubleToStr(SellRange,Digits);
   text4=DoubleToStr(BuyRange,Digits);

return(myOrderType);
}

