//+---------------------------------------------------------------------+
//|           Wave Power EA1.mq4
//|                              Copyright 2008, Regeneration by K
//|                                              http://FxKillU.com
//+---------------------------------------------------------------------+

///Please, do not sell this EA because its FREE
#property copyright "Copyright 2008, K LAM Wave Power EA V1.0A"
#property link      "http://FxKillU.com"
//#property show_inputs

#define MAGICMA  20070424
#define Version  20090203

extern string Name_Expert= "Wave Power EA";
extern string OWN="Copyright 2008, K LAM";

//m30 no break M30 10000 Masterforex
// MasterForex Mini AC Trade 0.1 X100 AC

extern string  sq="--WAVE SETTING--";
extern int    OpenOrdersBasedOn=16; //8 for eurusd Method to decide if we start long or short

//extern bool ExitWithSTOCH= False; // true;//
extern string sb="--TRADE SETTING--";
extern double Lots = 0.1;// 0.1       // We start with this lots number
extern int    TakeProfit = 32;//32;  // Profit Goal for the latest order opened
extern double multiply = 2; //2.0; 
extern int    MaxTrades = 12; //15;        // Maximum number of orders to open
extern int    Pips = 24; //24; //22;             // Distance in Pips from one order to another
extern int    StopLoss = 0;  // StopLoss
//extern int    TrailingStop = 0;// Pips to trail the StopLoss
extern bool   MyMoneyProfitTarget= False; //true;
extern double My_Money_Profit_Target= 100000; //at hold AC 50; a day

extern string MM="--Account Management--"; // (from order 2, not from first order
//extern string accounttypes="0 if Normal Lots, 1 for mini lots, 2 for micro lots";
//extern int    AccountType=1;  // 0 if Normal Lots, 1 for mini lots, 2 for micro lots
extern string riskset="risk to calculate the lots size (0 Mini order)";
extern int    risk=0;             // risk to calculate the lots size(0 for Mini lots)
extern string magicnumber="--MAgic No--";
extern int    MagicNumber=20070424;  // Magic number for the orders placed

extern string  so="--CUTLOSS SETTING--";
extern bool SecureProfitProtection = False; // True;
extern string OTP="Number of orders to enable the account protection";
extern int OrderstoProtect=4;//3;   //after ==2 then next the3 order Number of orders to enable the account protection
extern string SP="profit more than Rebound Point we close the orders";
//extern double StepPips = 8;//72; //20;     // If profit made is bigger than ReboundProfit we close the orders
extern double ReboundProfit2 = 18;
extern double ReboundProfit1 = 28;//72; //20;     // If profit made is bigger than ReboundProfit we close the orders
//on test 28
extern bool LossProtection =False; // True; //for test
extern double TTLoss= 0;//72; //20;     // If profit made is bigger than ReboundProfit we close the orders

extern string  ASP="if one will check profit from all symbols, if cero only this symbol";
extern bool AllSymbolsProtect=False; // if one will check profit from all symbols, if cero only this symbol
extern string  EP="if true, then the expert will protect the account equity to the percent specified";
extern bool EquityProtection=False; //true; if true, then the expert will protect the account equity to the percent specified
extern string  AEP="percent of the account to protect on a set of trades";
extern int AccountEquityPercentProtection=70; // percent of the account to protect on a set of trades
extern string  AMP="if true, then the expert will use money protection to the USD specified";
//extern bool AccountMoneyProtection=False;
//extern double AccountMoneyProtectionValue=10000.00;

extern bool TradeOnFriday = true; //False; //

extern string  OtherSetting="--Others Setting--";
extern int OrdersTimeAlive=0; //604800;  //5day  in seconds 86400 for 1 Day //48*60*60;// 2 day

//extern string  reverse="if one the desition to go long/short will be reversed";
extern bool ReverseCondition=False;  // if one the desition to go long/short will be reversed
//extern string  limitorder="if true, instead open market orders it will open limit orders ";

extern bool SetLimitOrders=False; // if true, instead open market orders it will open limit orders 
extern bool HoldMulitSymbol=False; //true;

color ArrowsColorModify=Purple; //orders Modify color
color ArrowsColorUp=Aqua;       //orders UP color
color ArrowsColorDown=Red;      //orders DOWN color
color ArrowsColor=Orange; //Green;        //orders arrows color

bool ContinueOpening=True;
bool OSymbolInStore = false;

string OSymbol = "GBPUSD";
string XSymbol = "GBPUSD";

datetime LastOrderOpenTime=0,CurrOrderOpenTime=0;

string StrOrderDirect = "Buy | Sell";

int anythings=2404;// for range no use
int PreviousOpenOrders=0;
int OpenOrders=0;
int MarketOpenOrders=0, LimitOpenOrders=0;
int slippage=5;
int myOrderType=0;
int OrderTypeDirect=99;

double miniLot=0,maxLot=0;
double BuyPrice=0, SellPrice=0, LastPrice=0;
double lotsi=0, mylotsi=0;

double Profit=0;
double CurLot=0;

//int LastTicket=0, LastType=0;
//double LastClosePrice=0;
//double LastLots=0;
double PipValue=0;
double indexAOhigh=0;
double indexAOlow=0;

double TmpMark=0;
double MaxDrawdown=0,MaxLots=0;

double LowFreeMargin=0,LowAccountEquity=0,MaxBalance=0;
datetime StartDate,EndDate;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
double indexAO;  
int count;
int AOtimeframe=30;

double indexRate=0.7;

   StartDate = TimeCurrent();
   XSymbol=Symbol();
   LowAccountEquity = AccountEquity();
   LowFreeMargin = AccountFreeMargin();
   MaxBalance = AccountBalance();
//---- 
   miniLot=MarketInfo(XSymbol,MODE_MINLOT); // mini order at the lots pre Set
   maxLot=MarketInfo(XSymbol,MODE_MAXLOT); // mini order at the lots pre Set
   
	if (risk==0) lotsi=miniLot;
//	else lotsi=miniLot*risk;
	else lotsi=MathCeil(AccountBalance()*risk/10000)*miniLot;	
	lotsi =Lots;
		
   Print("AC Min Lot=",miniLot," Max Lot=",maxLot," Start Real Lot=",lotsi);
//----
// cal the high poind
if(OpenOrdersBasedOn==10) { //iBars(Symbol(),60)
   for(count=0;count < Bars;count++) {
      indexAO=iAO(XSymbol,AOtimeframe, count);
      if(indexAO > indexAOhigh) indexAOhigh = indexAO;
      if(indexAO < indexAOlow) indexAOlow = indexAO;
      }
      Print("30 Bars =",iBars(XSymbol,30)," 60 Bars =",iBars(XSymbol,60));
      Print("High=",indexAOhigh," Low=",indexAOlow," Last=",indexAO);
}

indexAOhigh = indexAOhigh*indexRate;
indexAOlow = indexAOlow*indexRate;

   return(0);
  }
  
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   DeleteAllObjects();
   //miniLot=MarketInfo(XSymbol,MODE_MINLOT); // mini order at the lots pre Set
   
   EndDate = TimeCurrent();
	if (risk==0) lotsi=miniLot;
//	else lotsi=miniLot*risk;
	else lotsi=MathCeil(AccountBalance()*risk/10000)*miniLot;
	lotsi =Lots;	
	
   //write to log file
 	//int handle = FileOpen( StringConcatenate( Name_Expert, XSymbol,".csv" ), FILE_CSV | FILE_WRITE,',');
   int Onefile = FileOpen( StringConcatenate( "WP EA(", AccountServer(), ").csv"), FILE_CSV | FILE_READ | FILE_WRITE,',');
   if ( Onefile < 0 ) { Print( "Write File Fail!! ", GetLastError(), "Error NO" ); }
   	
	FileSeek(Onefile, 0, SEEK_END);
   int fsize=FileSize(Onefile);
   
   if (fsize == 0) //Print("my_table.dat size is ", size, " bytes");
   FileWrite(	Onefile,
	            "Symbol",
	            "Max Drawdown",
	            "Low Equity",
	            "Low FreeMargin",
	            "Max Lot Make",
	            "Total Trade",
	            "Account Balance",
	            "Max Balance",
	            "Start",
	            "End"
	            );
	            
	//FileSeek(Onefile, 0, SEEK_END);
	FileWrite(	Onefile,
	            XSymbol,
	            MaxDrawdown,
	            NormalizeDouble(LowAccountEquity,2),
	            NormalizeDouble(LowFreeMargin,2),
	            MaxLots,
	            OrdersHistoryTotal(),
	            NormalizeDouble(AccountBalance(),2),
	            NormalizeDouble(MaxBalance,2),
	            TimeToStr(StartDate,TIME_DATE|TIME_MINUTES),
	            TimeToStr(EndDate,TIME_DATE|TIME_SECONDS)
	            );
	FileClose( Onefile );


if (false) {
   int handle = FileOpen( StringConcatenate( "WP EA(", XSymbol, ").csv"), FILE_CSV | FILE_WRITE,',');
	if ( handle < 0 ) { Print( "Write File Fail!! ", GetLastError(), "Error NO" ); }

	FileWrite(	handle,
	            "Symbol",
	            "Max Drawdown",
	            "Low Equity",
	            "Low FreeMargin",
	            "Max Lot Make",
	            "Total Trade",
	            "Account Balance",
	            "Max Balance",
	            "Start",
	            "End"
	            );

	FileWrite(	handle,
	            XSymbol,
	            MaxDrawdown,
	            NormalizeDouble(LowAccountEquity,2),
	            NormalizeDouble(LowFreeMargin,2),
	            MaxLots,
	            OrdersHistoryTotal(),
	            NormalizeDouble(AccountBalance(),2),
	            NormalizeDouble(MaxBalance,2),
	            TimeToStr(StartDate,TIME_DATE|TIME_MINUTES),
	            TimeToStr(EndDate,TIME_DATE|TIME_SECONDS)
	            );

	FileClose( handle ); }
	

   Print("AC Min Lot=",miniLot," Max Lot=",maxLot," End Real Lot=",lotsi);
   Print("Wow....New Drawdown Record at $",MaxDrawdown," Low Equity",LowAccountEquity," Low FreeMargin",LowFreeMargin,"Max Lot",MaxLots,"Account Balance",AccountBalance());
   
if(OpenOrdersBasedOn==10) { //iBars(Symbol(),60)
   Print(XSymbol," 30M Bars =",iBars(XSymbol,30)," 60M Bars =",iBars(XSymbol,60));
   Print(XSymbol,"High=",indexAOhigh," Low=",indexAOlow);
   }
   //Print("MathCeil 101.3=",MathCeil(101.3));
//----
   return(0);
  }
  
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  
//Symbol()
//----
   int cnt=0;
   string text="";
   OSymbolInStore = false;
   
   slippage =(Ask-Bid)/Point; //slippage =NormalizeDouble((Ask-Bid)/Point);
   PipValue=MarketInfo(XSymbol,MODE_TICKVALUE); 
   if (PipValue==0) PipValue=5;
      
      TmpMark = AccountEquity();
      if(TmpMark < LowAccountEquity) {LowAccountEquity=TmpMark;}

      TmpMark = AccountFreeMargin();
      if(TmpMark < LowFreeMargin) {LowFreeMargin=TmpMark;}
      
      TmpMark = AccountBalance();
      if(TmpMark > MaxBalance) {MaxBalance=TmpMark;}
         

   //miniLot=MarketInfo(XSymbol,MODE_MINLOT); // mini order at the lots pre Set
   //lotsi=MathCeil(AccountBalance()*risk/10000)/100;*miniLot
	if (risk==0) lotsi=miniLot;
	else lotsi=MathCeil(AccountBalance()*risk/10000)*miniLot;
	
	lotsi =Lots;
	
   if(lotsi <= miniLot) lotsi = miniLot;
   if(lotsi >= maxLot) lotsi = maxLot;


   //take a record
   TmpMark=AccountProfit();
   if(TmpMark < 0 && MaxDrawdown>TmpMark) { //should be -
         MaxDrawdown=TmpMark;
         //Print("Wow....New Drawdown Record at $",MaxDrawdown);
         }

   OpenOrders=0;
   MarketOpenOrders=0;
   LimitOpenOrders=0;
   CurrOrderOpenTime=0;
   LastOrderOpenTime=0;
   
   for(cnt=0;cnt<OrdersTotal();cnt++)
   {
     if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
     {if(OrderSymbol()!=XSymbol) {OSymbolInStore = true; OSymbol = OrderSymbol();}
	   if (OrderSymbol()==XSymbol && OrderMagicNumber() == MagicNumber)
	   {
	     TmpMark+=OrderProfit(); //Profit+=OrderProfit();
//        ProfitPips+=(OrderProfit()/LastLots)/PipValue;
	  	  if (OrderType()==OP_BUY || OrderType()==OP_SELL) 
	  	  {
	  	   MarketOpenOrders++;
	  	   if(OrderOpenTime()!=0) CurrOrderOpenTime=OrderOpenTime();
	  	   if(CurrOrderOpenTime > LastOrderOpenTime) LastOrderOpenTime=CurrOrderOpenTime;
	  	  }
	  	  if (OrderType()==OP_SELLLIMIT || OrderType()==OP_BUYLIMIT) LimitOpenOrders++;
	  	  OpenOrders++;
	   }
	  }
   }
   
         if(TmpMark < 0 && MaxDrawdown>TmpMark) { //should be -
         MaxDrawdown=TmpMark;
         //Print("Wow....New Drawdown Record at $",MaxDrawdown);
         }
         
   // close order for activate 
   if (PreviousOpenOrders>OpenOrders)
   {
    CloseAll(XSymbol,0); // 0 for all M_no
    DeleteQ(XSymbol,0); //delete the queue order  "All"
   }

   // Orders Time alive protection
   if (OrdersTimeAlive>0 && LastOrderOpenTime!=0 && (TimeCurrent() - LastOrderOpenTime)>OrdersTimeAlive) // 172800 for 2 day
   {
    text = text + "\nClosing all orders because OrdersTimeAlive protection.";
    Print(TimeCurrent()," - ",LastOrderOpenTime," > ",OrdersTimeAlive);
    Print("Closing all orders because OrdersTimeAlive protection.");
    CloseAll(XSymbol,0);
    DeleteQ(XSymbol,0);
    
    PreviousOpenOrders=OpenOrders+1;
    ContinueOpening=False;
    return(0);
   }   
   
   // Account equity protection 
   if (EquityProtection && AccountEquity()<=AccountBalance()*AccountEquityPercentProtection/100) 
	 {
	 text = text + "\nEquityProtection activated. Closing all orders.";
	 Print("EquityProtection activated. Over ",AccountEquityPercentProtection,"%. Closing all orders. Balance: ",AccountBalance()," Equity: ", AccountEquity());
	 CloseAll(XSymbol,0);
    DeleteQ(XSymbol,0);
	 PreviousOpenOrders=OpenOrders+1;
	 ContinueOpening=False;
	 return(0);
   }
   
   //set my profit Stop trading
   if (MyMoneyProfitTarget && AccountBalance() > My_Money_Profit_Target)
   {
    text = text + "\nMoneyProfitTarget reached. Closing all orders..";
    Print("MoneyProfitTarget reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity(),"Last Profit ",AccountProfit());
    CloseAll(XSymbol,0);
    DeleteQ(XSymbol,0);
    
    PreviousOpenOrders=OpenOrders+1;
    ContinueOpening=False;
    return(0);
   }
    
  // SecureProfit protection,,, Modified to make easy to understand 
  if((SecureProfitProtection && MarketOpenOrders>=OrderstoProtect) ||
    (LossProtection && MarketOpenOrders>=MaxTrades))
   {
	  Profit=0;
	  CurLot=0;
	  //count the order no
     for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
      {
       if ((OrderSymbol()==XSymbol && OrderMagicNumber()==MagicNumber) || AllSymbolsProtect)
        {
        Profit=Profit+OrderProfit()+OrderSwap();
        CurLot=CurLot+OrderLots();
        //Print("Rebound Profit+CurLot=",ReboundProfitProfit,"+",CurLot);
        }
      }//for
     }

	  //Change the Last order at the stoploss --Protection Profit>= ReboundProfit*CurLot	  
	  if(Profit>= ReboundProfit1*CurLot && SecureProfitProtection && MarketOpenOrders>=OrderstoProtect)
	    {
	     text = text + "\nRebound Profit Reach, Closing orders.";
	     Print("Rebound Profit Reach, Closing Balance: ",AccountBalance()," Equity: ", AccountEquity(),
	     "Rebound Over ",ReboundProfit1,"*",CurLot,"=",ReboundProfit1*CurLot," Profit: ",Profit);
	 CloseAll(XSymbol,0);
    DeleteQ(XSymbol,0);
    
	     PreviousOpenOrders=OpenOrders+1;
		  ContinueOpening=False;
		  return(0);
		 }
   
	  // StopLoss!
	  if(Profit<= -(TTLoss*CurLot) && LossProtection && MarketOpenOrders>=MaxTrades)
	  	 { //stop loss
	     text = text + "\nLoss Reach, Closing orders.";
	     Print("Loss Reach, Closing Balance: ",AccountBalance()," Equity: ", AccountEquity(),
	     "Loss Over ",TTLoss,"*",CurLot,"=",TTLoss*CurLot," Profit: ",Profit);
	     
    CloseAll(XSymbol,0);
    DeleteQ(XSymbol,0);
    
	     PreviousOpenOrders=OpenOrders+1;
	     ContinueOpening=False;
	  	  return(0);	  
	  }
}

   // if dont trade at fridays then we close all
   if (!TradeOnFriday && DayOfWeek()==5)
   {
    text = text +"\nStop Open New orders because TradeOnFriday protection.";
    Print("Stop Open New orders because TradeOnFriday protection.");
    // Close queue at the Open order, not here
   }

   OpenOrders=0;
   MarketOpenOrders=0;
   LimitOpenOrders=0;
   for(cnt=0;cnt<OrdersTotal();cnt++)   
   {
     if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
     {if(OrderSymbol()!=XSymbol) {OSymbolInStore = true; OSymbol = OrderSymbol();}
	   if (OrderSymbol()==XSymbol && OrderMagicNumber() == MagicNumber)
	   {				
	  	  if (OrderType()==OP_BUY || OrderType()==OP_SELL) 
	  	  {
	  	   MarketOpenOrders++;
	  	   LastOrderOpenTime=OrderOpenTime();
	  	  }
	  	  if (OrderType()==OP_SELLLIMIT || OrderType()==OP_BUYLIMIT) LimitOpenOrders++;
	  	  OpenOrders++;
	   }
	  }
   }
      //need count one Time
   PreviousOpenOrders=OpenOrders;
   
   if (OpenOrders>=MaxTrades) ContinueOpening=False;
      else ContinueOpening=True;

   if (LastPrice==0) 
   {
	  for(cnt=0;cnt<OrdersTotal();cnt++)
	  {	
	    if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
        {if(OrderSymbol()!=XSymbol) {OSymbolInStore = true; OSymbol = OrderSymbol();}
		  if (OrderSymbol()==XSymbol && OrderMagicNumber() == MagicNumber) 
		  {
			LastPrice=OrderOpenPrice();
			myOrderType=OrderType();
		  }
		 }
	  }
   }//get the last info od order
   
//  OrderTypeDirect=99;

  switch (OpenOrdersBasedOn)
  {
     case 1: {if (MADirected()==OP_BUY) OrderTypeDirect=OP_BUY;
              if (MADirected()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
     }
       break;
     case 2: {if (EMADirected()==OP_BUY) OrderTypeDirect=OP_BUY;
              if (EMADirected()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
     }
       break;
     case 3: {if (SEMADirected()==OP_BUY) OrderTypeDirect=OP_BUY;
              if (SEMADirected()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
     }
       break;
     case 4: {if (StochDirected()==OP_BUY) OrderTypeDirect=OP_BUY;
              if (StochDirected()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
     //OrderTypeDirect=StochDirected(); 
     }
       break;
     case 5: {if (SMADirected()==OP_BUY) OrderTypeDirect=OP_BUY;
              if (SMADirected()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
     }
       break;
     case 6: {//if (MA5Directed()==OP_BUY) OrderTypeDirect=OP_BUY;
              //if (MA5Directed()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
              OrderTypeDirect=MA5Directed();
     }
       break;
     case 7: {if (StepMAStochDirected()==OP_BUY) OrderTypeDirect=OP_BUY;
              if (StepMAStochDirected()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
              //OrderTypeDirect=StepMAStochDirected();
     }
       break;
     case 8: {//if (StepMAStochDirected()==OP_BUY) OrderTypeDirect=OP_BUY;
              //if (StepMAStochDirected()==OP_SELL) OrderTypeDirect=OP_SELL; //else not change
                     OrderTypeDirect=IMA_RSIDirected();
     }
       break;
     case 9: {OrderTypeDirect=CCIDirected();
     }
     
       break;
     case 10: {OrderTypeDirect=AODirected();
     }
       break;            
     case 16:
       OrderTypeDirect=StochDirected();
       break;
     case 17:
       OrderTypeDirect=MACD();
       break;
     default: OrderTypeDirect=99;
       break;
  }


   if (OpenOrders<1) 
   {
     myOrderType=OrderTypeDirect;
	  if (ReverseCondition)
	  {
	  	  if (myOrderType==OP_BUY) myOrderType=OP_SELL;
		  else if(myOrderType==OP_SELL) myOrderType=OP_BUY;
	  }
   }   
   
   if (ReverseCondition)
   {
    if (OrderTypeDirect==OP_BUY) OrderTypeDirect=OP_SELL;
    else if (OrderTypeDirect==OP_SELL) OrderTypeDirect=OP_BUY;
   }
   
      if(!IsTesting()) {
      //if(true) {
             StrOrderDirect = "Watch......type="+OpenOrdersBasedOn;
             if (OrderTypeDirect==OP_BUY) StrOrderDirect = "Buy.. Type="+OpenOrdersBasedOn;
             if (OrderTypeDirect==OP_SELL) StrOrderDirect = "SELL.. Type="+OpenOrdersBasedOn;
             if (OrderTypeDirect==99 && OpenOrders<1) text=text + "\nNo conditions to open trades";
             if (OSymbolInStore && HoldMulitSymbol) text=text + "\nHold trading! Other Symbol in Store "+OSymbol;             
             Comment(XSymbol," Ver=",Version,"\nLast Price=",LastPrice," Previous Open\'s=",PreviousOpenOrders,
             " Continue Opening=",ContinueOpening,"\nOrder Direct=",StrOrderDirect,"\nLOT=",lotsi,text,
             "\nSPREAD=",MarketInfo(XSymbol,MODE_SPREAD));
      }
      
      if(AccountFreeMargin() < 1) return(0);
      if(OSymbolInStore && HoldMulitSymbol)  return(0); // for mulitcurrent

      if (OpenOrders<1) OpenMarketOrders();
      else
         if (SetLimitOrders) OpenLimitOrders();
         else OpenMarketOrders();

//----
   return(0);
  }
//+------------------------------------------------------------------+


void DrawDirect(int goto)
{
if(IsTesting()) return;
   if(goto==OP_BUY)
     {
     anythings++;
     ObjectCreate(DoubleToStr(anythings,0), OBJ_ARROW, 0, iTime(XSymbol,0,0), Bid);
     ObjectSet(DoubleToStr(anythings,0),OBJPROP_ARROWCODE,SYMBOL_ARROWUP);
     ObjectSet(DoubleToStr(anythings,0),OBJPROP_COLOR,Blue);
     }
   if(goto==OP_SELL)
     {
     anythings++;
     ObjectCreate(DoubleToStr(anythings,0), OBJ_ARROW, 0, iTime(XSymbol,0,0), Ask);
     ObjectSet(DoubleToStr(anythings,0),OBJPROP_ARROWCODE,SYMBOL_ARROWDOWN);
     ObjectSet(DoubleToStr(anythings,0),OBJPROP_COLOR,Red);
     }
   if(goto==99)
     {
     anythings++;
     ObjectCreate(DoubleToStr(anythings,0), OBJ_ARROW, 0, iTime(XSymbol,0,0), Ask);
     ObjectSet(DoubleToStr(anythings,0),OBJPROP_ARROWCODE,3);
     ObjectSet(DoubleToStr(anythings,0),OBJPROP_COLOR,Red);
     }
}

void OpenMarketOrders()
{         
   bool result;
   int count=0,OrderComplete=0;
   int err;
   double slx,tpx;
   double ticketno=-1;
   
if (!TradeOnFriday && DayOfWeek()==5)
	  {
	    Comment("TradeOnfriday is False, Not Open Order!!!");
	    return(0);
	  }
	  
//if (OpenOrders >= 4) { //reverse                  }
                     
      if (myOrderType==OP_SELL && ContinueOpening) 
      {	      
	     if ((Bid-LastPrice)>=Pips*Point || OpenOrders<1) 
	     {  
	     if (OpenOrders >= 5) { //reverse
	     BuyPrice=Ask;
		    LastPrice=0;
		    if (TakeProfit>0) {
		          tpx=BuyPrice+TakeProfit*Point;
		          if (OpenOrders == OrderstoProtect-1) tpx=BuyPrice+ReboundProfit1*Point;
		          if (OpenOrders > OrderstoProtect-1) tpx=BuyPrice+ReboundProfit2*Point;
		          //Print("tpx=",tpx);
		          }
		       else tpx=0;
		    if (StopLoss>0) slx=BuyPrice-StopLoss*Point;
		       else slx=0;
		    if (OpenOrders!=0) 
		       mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2);
		       else mylotsi=lotsi;
		    if (mylotsi>100) mylotsi=100;
		    
		    if(mylotsi <= miniLot) mylotsi = miniLot;
		    if(mylotsi >= maxLot) mylotsi = maxLot;		    
          if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
		    
		    ticketno=OrderSend(XSymbol,OP_BUY,mylotsi,BuyPrice,slippage,slx,tpx,Name_Expert+Version,MagicNumber,0,ArrowsColorUp);
		    if(ticketno != -1) OrderComplete=1;
		    if (mylotsi>MaxLots) MaxLots=mylotsi; //mark a record

                     } else {
		    SellPrice=Bid;
		    LastPrice=0;
		    if (TakeProfit>0) {
		          tpx=SellPrice-TakeProfit*Point;
		          if (OpenOrders == OrderstoProtect-1) tpx=SellPrice-ReboundProfit1*Point;
		          if (OpenOrders > OrderstoProtect-1) tpx=SellPrice-ReboundProfit2*Point;
	             //Print("tpx=",tpx);
	             }
		       else tpx=0;
		    if (StopLoss>0) slx=SellPrice+StopLoss*Point;
		       else slx=0;
		    if (OpenOrders!=0) 
		       mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2);
		       else mylotsi=lotsi;
		    if(mylotsi>100) mylotsi=100;
		    
		    if(mylotsi <= miniLot) mylotsi = miniLot;
		    if(mylotsi >= maxLot) mylotsi = maxLot;
          if(AccountFreeMarginCheck(XSymbol,OP_SELL,mylotsi)<=0 || GetLastError()==134) return;

		    ticketno=OrderSend(XSymbol,OP_SELL,mylotsi,SellPrice,slippage,slx,tpx,Name_Expert+Version,MagicNumber,0,ArrowsColorDown);
		    if(ticketno != -1) OrderComplete=1;
		    if (mylotsi>MaxLots) MaxLots=mylotsi; //mark a record
		    } //else
	     }
      }
      
      if (myOrderType==OP_BUY && ContinueOpening)
      {
	     if ((LastPrice-Ask)>=Pips*Point || OpenOrders<1) 
	     { // buy more,price low then before
	     if (OpenOrders >= 5) { //reverse
		    SellPrice=Bid;
		    LastPrice=0;
		    if (TakeProfit>0) {
		          tpx=SellPrice-TakeProfit*Point;
		          if (OpenOrders == OrderstoProtect-1) tpx=SellPrice-ReboundProfit1*Point;
		          if (OpenOrders > OrderstoProtect-1) tpx=SellPrice-ReboundProfit2*Point;
	             //Print("tpx=",tpx);
	             }
		       else tpx=0;
		    if (StopLoss>0) slx=SellPrice+StopLoss*Point;
		       else slx=0;
		    if (OpenOrders!=0) 
		       mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2);
		       else mylotsi=lotsi;
		    if(mylotsi>100) mylotsi=100;
		    
		    if(mylotsi <= miniLot) mylotsi = miniLot;
		    if(mylotsi >= maxLot) mylotsi = maxLot;
          if(AccountFreeMarginCheck(XSymbol,OP_SELL,mylotsi)<=0 || GetLastError()==134) return;

		    ticketno=OrderSend(XSymbol,OP_SELL,mylotsi,SellPrice,slippage,slx,tpx,Name_Expert+Version,MagicNumber,0,ArrowsColorDown);
		    if(ticketno != -1) OrderComplete=1;
		    if (mylotsi>MaxLots) MaxLots=mylotsi; //mark a record
		    
                     } else {
		    BuyPrice=Ask;
		    LastPrice=0;
		    if (TakeProfit>0) {
		          tpx=BuyPrice+TakeProfit*Point;
		          if (OpenOrders == OrderstoProtect-1) tpx=BuyPrice+ReboundProfit1*Point;
		          if (OpenOrders > OrderstoProtect-1) tpx=BuyPrice+ReboundProfit2*Point;
		          //Print("tpx=",tpx);
		          }
		       else tpx=0;
		    if (StopLoss>0) slx=BuyPrice-StopLoss*Point;
		       else slx=0;
		    if (OpenOrders!=0) 
		       mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2);
		       else mylotsi=lotsi;
		    if (mylotsi>100) mylotsi=100;
		    
		    if(mylotsi <= miniLot) mylotsi = miniLot;
		    if(mylotsi >= maxLot) mylotsi = maxLot;		    
          if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
		    
		    ticketno=OrderSend(XSymbol,OP_BUY,mylotsi,BuyPrice,slippage,slx,tpx,Name_Expert+Version,MagicNumber,0,ArrowsColorUp);
		    if(ticketno != -1) OrderComplete=1;
		    if (mylotsi>MaxLots) MaxLots=mylotsi; //mark a record
		    }
	     }
      }
      Sleep(1000);//---- wait for 1 seconds
      RefreshRates();
      
if(OrderComplete==0) return(0);
     //Rebound Modify Set all Order at same sl tp
 if(ContinueOpening) {
 if(myOrderType==OP_BUY || myOrderType==OP_SELL) {
         
   //count=OrdersTotal()-1;  //while(count>=0) { // while always delay much! do not use
   for(count=OrdersTotal()-1;count>=0;count--) {
     if(OrderSelect(count,SELECT_BY_POS,MODE_TRADES)==false) break;
	  if(OrderSymbol() == XSymbol && OrderMagicNumber() == MagicNumber) // && Reversed==False) 
	    { 
         //if((tpx==OrderTakeProfit()) && (slx==OrderStopLoss())) continue;
         if(NormalizeDouble(tpx, Digits)==NormalizeDouble(OrderTakeProfit(), Digits) 
         && NormalizeDouble(slx, Digits)==NormalizeDouble(OrderStopLoss(), Digits)) continue;
         else {
            if(slx==0)
            result=OrderModify(OrderTicket(),OrderOpenPrice(),0,tpx,0,ArrowsColorModify);
            else
            result=OrderModify(OrderTicket(),OrderOpenPrice(),slx,tpx,0,ArrowsColorModify);


            if(!result) //|| GetLastError()!=1) 
              { err=GetLastError();
                if(!IsTesting()) Print("LastError = ",err); 
                }
                if(!IsTesting()) Print("Ticket ",OrderTicket()," modified.");            //   else OrderPrint();
                         //         } //ERR_NO_RESULT 1 No error returned, but the result is unknown 
            }
	  }
//     count--;
   }
}
}

}

void OpenLimitOrders()
{
double slx,tpx;
               
      if (myOrderType==OP_SELL && ContinueOpening) 
      {	
	     //if ((Bid-LastPrice)>=Pips*Point || OpenOrders<1) {		
		    //SellPrice=Bid;				
		    SellPrice = LastPrice+Pips*Point;
		    LastPrice=0;
		    if (TakeProfit>0) tpx=SellPrice-TakeProfit*Point;
		       else tpx=0;
		    if (StopLoss>0) slx=SellPrice+StopLoss*Point;
		       else slx=0;
		    if (OpenOrders!=0) 
		       mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2);
		       else mylotsi=lotsi;
		    if (mylotsi>100) mylotsi=100;

		    if(mylotsi <= miniLot) mylotsi = miniLot;
		    if(mylotsi >= maxLot) mylotsi = maxLot;		    
          if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
          		    
		    OrderSend(XSymbol,OP_SELLLIMIT,mylotsi,SellPrice,slippage,slx,tpx,Name_Expert+Version,MagicNumber,0,ArrowsColor);		    		    
		    return(0);
	     //}
      }
      
      if (myOrderType==OP_BUY && ContinueOpening) 
      {
	     //if ((LastPrice-Ask)>=Pips*Point || OpenOrders<1) {		
		    //BuyPrice=Ask;
		    BuyPrice=LastPrice-Pips*Point; //low then before Order
		    LastPrice=0;
		    if (TakeProfit>0) tpx=BuyPrice+TakeProfit*Point;
		       else tpx=0;
		    if (StopLoss>0) slx=BuyPrice-StopLoss*Point;
		       else slx=0;
		    if (OpenOrders!=0) 
		       mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2);
		       else mylotsi=lotsi;
		    if (mylotsi>100) mylotsi=100;
		    
		    if(mylotsi <= miniLot) mylotsi = miniLot;
		    if(mylotsi >= maxLot) mylotsi = maxLot;		    
          if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
          		    
		    OrderSend(XSymbol,OP_BUYLIMIT,mylotsi,BuyPrice,slippage,slx,tpx,Name_Expert+Version,MagicNumber,0,ArrowsColor);		    
		    return(0);
	     //}
      }   

}

void DeleteAllObjects()
{
 int    obj_total=ObjectsTotal();
 string name;
 for(int i=0;i<obj_total;i++)
 {
  name=ObjectName(i);
  if (name!="")
   ObjectDelete(name);
 }
 ObjectDelete("FLP_txt");
 ObjectDelete("P_txt");
}

void CloseAll(string SymbolToClose,int MagicNo) //at Symbol
{
bool   result;
int cmd,error;
int cnt;
int CloseNo;

   CloseNo=OrdersTotal();
   if(CloseNo==0) return(0);
   
   for(cnt=OrdersTotal();cnt > 0;cnt--) {//last to frist -1
      if(OrderSelect(cnt-1,SELECT_BY_POS, MODE_TRADES)==true) {
      if((SymbolToClose=="All") || 
         (OrderSymbol()==SymbolToClose && MagicNo == 0) ||
         (OrderSymbol()==SymbolToClose && OrderMagicNumber() == MagicNo)) {
            while(true) {
            cmd=OrderType();
             if(cmd==OP_BUY)
               result=OrderClose(OrderTicket(),OrderLots(),Bid,slippage,ArrowsColor);
             if(cmd==OP_SELL)
               result=OrderClose(OrderTicket(),OrderLots(),Ask,slippage,ArrowsColor);

               if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
               else error=0;
               if(error==129 || error==135) RefreshRates();
               else break;
            }//while
         }//if
      } else Print( "Error when order select ", GetLastError());//OrderSelect
      }//for
   CloseNo=CloseNo-OrdersTotal();   //CloseNo-=OrdersTotal();   
   if(!IsTesting()) Print(CloseNo," Orders Closed");
}

void DeleteQ(string SymbolToClose,int MagicNo) //at Symbol
{
int cnt;
int CloseNo,mode;

	  //delete the queue order
   CloseNo=OrdersTotal();
   if(CloseNo==0) return(0);
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--) //last to frist
      {
      OrderSelect(cnt,SELECT_BY_POS, MODE_TRADES);
      mode=OrderType();
      if((SymbolToClose=="All") || 
         (OrderSymbol()==SymbolToClose && MagicNo == 0) ||
         (OrderSymbol()==SymbolToClose && OrderMagicNumber() == MagicNo)) {
             if(mode==OP_SELLLIMIT || mode==OP_BUYLIMIT || mode==OP_BUYSTOP || mode==OP_SELLSTOP)
               OrderDelete(OrderTicket());
             }
      }
   CloseNo=CloseNo-OrdersTotal();
   if(!IsTesting()) Print(CloseNo," Queue Orders Removed");
}

int StochDirected()
{
int    K_Period = 14;
int    D_Period = 3;
int    Slow_Period = 3;
int    Stoch_TF = 60;
int    Ma_TF = 60; // 1 Hour
int    shift=1;
int    H_level = 92; // 97;
int    L_level = 8; //3;

int stochMAmode = 3; //0;
int mamode;
int SDirect=99;

  switch (stochMAmode) {
     case 0: mamode=MODE_SMA;
       break; 	
     case 1: mamode=MODE_EMA;
       break; 
     case 2: mamode=MODE_SMMA;
       break; 
     case 3: mamode=MODE_LWMA;
       break;        
     default: mamode=MODE_SMA;
       break;
  }

 //---- lot setting and modifications
    	double stom1=iStochastic(XSymbol,Stoch_TF,K_Period,D_Period,Slow_Period,mamode,0,MODE_MAIN,shift);
   	double stom2=iStochastic(XSymbol,Stoch_TF,K_Period,D_Period,Slow_Period,mamode,0,MODE_MAIN,shift+1);
   	double stos1=iStochastic(XSymbol,Stoch_TF,K_Period,D_Period,Slow_Period,mamode,0,MODE_SIGNAL,shift);
   	double stos2=iStochastic(XSymbol,Stoch_TF,K_Period,D_Period,Slow_Period,mamode,0,MODE_SIGNAL,shift+1);

      double madirect=iMA(XSymbol,Ma_TF,72,0,MODE_SMA,PRICE_TYPICAL,0); // 72 3 day 144 x Ma_TF 144H 6 day!!

//Sell order
      if(stom1<stos1 && stom2>=stos2 && stom2>H_level)
        {
//        madirect=iMA(XSymbol,Stoch_TF,96,0,MODE_SMA,PRICE_HIGH,0);  //4 day       
//        if (Bid<madirect) {
        SDirect=OP_SELL;
        DrawDirect(OP_SELL); }
//        }

//buy order
      if(stom1>stos1 && stom2<=stos2 && stom2<L_level)
        { 
//        madirect=iMA(XSymbol,Stoch_TF,96,0,MODE_SMA,PRICE_LOW,0);
//        	  if (Ask>madirect) {

        SDirect=OP_BUY;
        DrawDirect(OP_BUY); }
//        }
    
return(SDirect);
}

int MADirected()
{
int   per_SMA5 = 15;
int   per_SMA20 = 20;
int   per_SMA40 = 25;
int   per_SMA60 = 50;
int   MaDirect=99;
double SMA5, SMA20, SMA40, SMA60, SMA40_prew;

  SMA5 = iMA(XSymbol,PERIOD_H1,per_SMA5,0,MODE_SMA,PRICE_MEDIAN,1);
  SMA20 = iMA(XSymbol,PERIOD_H1,per_SMA20,0,MODE_SMA,PRICE_MEDIAN,1);
  
  SMA40_prew = iMA(XSymbol,PERIOD_H1,per_SMA40,0,MODE_SMA,PRICE_MEDIAN,2);
  SMA40 = iMA(XSymbol,PERIOD_H1,per_SMA40,0,MODE_SMA,PRICE_MEDIAN,1);
  SMA60 = iMA(XSymbol,PERIOD_H1,per_SMA60,0,MODE_SMA,PRICE_MEDIAN,1);

  if(SMA5>SMA20 && SMA20>SMA40 && (SMA40-SMA60)>=0.0001 && SMA40_prew<=SMA60)  
     { MaDirect=OP_BUY;
       DrawDirect(OP_BUY);
     }

  if(SMA5<SMA20 && SMA20<SMA40 && (SMA60-SMA40)>=0.0001 && SMA40_prew>=SMA60) 
     { MaDirect=OP_SELL;
       DrawDirect(OP_SELL);
     }

return(MaDirect);
}

int EMADirected()
{
int    EDirect    =99; 
int    ShortEma   =19;
int    LongEma    =110;                         // Minimum diference between EMA's that allow opening
double SEma, LEma,EMA1,EMA2,LWMA1,LWMA2;

  SEma = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_EMA,PRICE_CLOSE,0);
  LEma = iMA(XSymbol,PERIOD_H1,LongEma,0,MODE_EMA,PRICE_CLOSE,0);
  
  EMA1  = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_EMA,PRICE_CLOSE,1);
  EMA2  = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_EMA,PRICE_CLOSE,2);
  LWMA1 = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_LWMA,PRICE_CLOSE,1);
  LWMA2 = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_LWMA,PRICE_CLOSE,2); 

  if(LWMA1 > EMA1 && LWMA1 > LWMA2 && EMA1 > EMA2)
     { EDirect=OP_BUY;
       DrawDirect(OP_BUY);
     }

  if(LWMA1 < EMA1 && LWMA1 < LWMA2 && EMA1 < EMA2)
     { EDirect=OP_SELL;
       DrawDirect(OP_SELL);
     }

return(EDirect);
}

int SEMADirected()
{
int    SEDirect   =99; 
int    ShortEma   =19;
int    LongEma    =110;                         // Minimum diference between EMA's that allow opening
double SEma, LEma,EMA1,EMA2,LWMA1,LWMA2;
double S1M, S1S, S2M, S2S, S3M, S3S, S4M, S4S;
bool   UseEMAFilter=true;
bool   Buy,Sell;
  SEma = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_EMA,PRICE_CLOSE,0);
  LEma = iMA(XSymbol,PERIOD_H1,LongEma,0,MODE_EMA,PRICE_CLOSE,0);
  
  EMA1  = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_EMA,PRICE_CLOSE,1);
  EMA2  = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_EMA,PRICE_CLOSE,2);
  LWMA1 = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_LWMA,PRICE_CLOSE,1);
  LWMA2 = iMA(XSymbol,PERIOD_H1,ShortEma,0,MODE_LWMA,PRICE_CLOSE,2); 

  Sell=   LWMA1 < EMA1 && LWMA1 < LWMA2 && EMA1 < EMA2 ; 
  Buy=    LWMA1 > EMA1 && LWMA1 > LWMA2 && EMA1 > EMA2 ;
  if (!UseEMAFilter) {
   Buy = true;
   Sell = true;
   }
  
   S1M=iStochastic(XSymbol,PERIOD_M5,5,3,3,MODE_SMA,0,MODE_MAIN,0);
   S1S=iStochastic(XSymbol,PERIOD_M5,5,3,3,MODE_SMA,0,MODE_SIGNAL,0);
   S2M=iStochastic(XSymbol,PERIOD_M30,5,3,3,MODE_SMA,0,MODE_MAIN,0);
   S2S=iStochastic(XSymbol,PERIOD_M30,5,3,3,MODE_SMA,0,MODE_SIGNAL,0);
   S3M=iStochastic(XSymbol,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_MAIN,0);
   S3S=iStochastic(XSymbol,PERIOD_H1,5,3,3,MODE_SMA,0,MODE_SIGNAL,0);
   S4M=iStochastic(XSymbol,PERIOD_H1,14,3,3,MODE_SMA,0,MODE_MAIN,0);
   S4S=iStochastic(XSymbol,PERIOD_H1,14,3,3,MODE_SMA,0,MODE_SIGNAL,0);
   
   if(S1M>S1S && S2M>S2S && S3M>S3S && S4M>S4S && Buy)
     { SEDirect=OP_BUY;
       DrawDirect(OP_BUY);
     }

   if(S1M<S1S && S2M<S2S && S3M<S3S && S4M<S4S && Sell)  
     { SEDirect=OP_SELL;
       DrawDirect(OP_SELL);
     }
     
return(SEDirect);
}

int SMADirected()
{
int    SMADirect  =99; 
int    TimeF      =60;
double ima13,ima21,ima5,IndexMa;

 ima13 =iMA(XSymbol,TimeF,13,0,MODE_SMA,PRICE_CLOSE,0);
 ima21 =iMA(XSymbol,TimeF,21,0,MODE_SMA,PRICE_CLOSE,0);
 ima5  =iMA(XSymbol,TimeF,5,0,MODE_SMMA,PRICE_MEDIAN,0);
 IndexMa=(ima13-ima21)*10000;
 Comment("Index Ma =",IndexMa);
 
   if(IndexMa > 1)
     { SMADirect=OP_BUY;
       DrawDirect(OP_BUY);
     }

   if(IndexMa < -1)
     { SMADirect=OP_SELL;
       DrawDirect(OP_SELL);
     }

return(SMADirect);
}

int MA5Directed()
{ //6
int    MA5Direct  =99; 
int    TimeF      =60;
double ima5,IndexMa;

 ima5  =iMA(XSymbol,TimeF,5,0,MODE_SMMA,PRICE_MEDIAN,0);
 IndexMa=(Low[0]-ima5)*100;
 
 //((High[0]+Low[0])/2);
 //-ima5)*10000;
 //Comment("\n\n\n\n\nIndex Ma =",IndexMa);
 
//   if(IndexMa > ima5) //1)
//     {  MA5Direct=OP_BUY;
//        DrawDirect(OP_BUY);
//     }

//   if(IndexMa < ima5) //-1)
//     {  MA5Direct=OP_SELL;
//        DrawDirect(OP_SELL);
//     }
     
   if(IndexMa < -1)
     {  MA5Direct=OP_BUY;
        DrawDirect(OP_BUY);
     }

   if(IndexMa > 1)
     {  MA5Direct=OP_SELL;
        DrawDirect(OP_SELL);
     }


return(MA5Direct);
}

int StepMAStochDirected()
{//7

//---- input parameters
int PeriodWATR=10;
double Kwatr=1.0000;
int HighLow=0;
int NumberOfBarsToCalculate = 500;
//---- indicator buffers
double LineMinBuffer[];
double LineMidBuffer[];

int    StepDirect  =99;
//int    TimeF      =60;
double IndexMa;

      //string short_name;
      //short_name = "Max bars to count: |"+(Bars-1)+"| ";
      //IndicatorShortName(short_name);

   int      i,shift,TrendMin,TrendMax,TrendMid;
   double   SminMin0,SmaxMin0,SminMin1,SmaxMin1,SumRange,dK,WATR0,WATRmax,WATRmin,WATRmid;
   double   SminMax0,SmaxMax0,SminMax1,SmaxMax1,SminMid0,SmaxMid0,SminMid1,SmaxMid1;
   double   linemin,linemax,linemid,Stoch1,Stoch2,bsmin,bsmax;
   
   double prev_y = 0,prev_b = 0;
   	
   for(shift=NumberOfBarsToCalculate-1;shift>=0;shift--)
   {
	SumRange=0;
	for (i=PeriodWATR-1;i>=0;i--)
	    { 
       dK = 1+1.0*(PeriodWATR-i)/PeriodWATR;
       SumRange+= dK*MathAbs(High[i+shift]-Low[i+shift]);
       }
	WATR0 = SumRange/PeriodWATR;
	
	WATRmax=MathMax(WATR0,WATRmax);
	if (shift==NumberOfBarsToCalculate-1-PeriodWATR) WATRmin=WATR0;
	WATRmin=MathMin(WATR0,WATRmin);
	
	int StepSizeMin=MathRound(Kwatr*WATRmin/Point);
	int StepSizeMax=MathRound(Kwatr*WATRmax/Point);
	int StepSizeMid=MathRound(Kwatr*0.5*(WATRmax+WATRmin)/Point);
		
	if (HighLow>0)
	  {
	  SmaxMin0=Low[shift]+2*StepSizeMin*Point;
	  SminMin0=High[shift]-2*StepSizeMin*Point;
	  
	  SmaxMax0=Low[shift]+2*StepSizeMax*Point;
	  SminMax0=High[shift]-2*StepSizeMax*Point;
	  
	  SmaxMid0=Low[shift]+2*StepSizeMid*Point;
	  SminMid0=High[shift]-2*StepSizeMid*Point;
	  
	  if(Close[shift]>SmaxMin1) TrendMin=1; 
	  if(Close[shift]<SminMin1) TrendMin=-1;
	  
	  if(Close[shift]>SmaxMax1) TrendMax=1; 
	  if(Close[shift]<SminMax1) TrendMax=-1;
	  
	  if(Close[shift]>SmaxMid1) TrendMid=1; 
	  if(Close[shift]<SminMid1) TrendMid=-1;
	  }
	 
	if (HighLow == 0)
	  {
	  SmaxMin0=Close[shift]+2*StepSizeMin*Point;
	  SminMin0=Close[shift]-2*StepSizeMin*Point;
	  
	  SmaxMax0=Close[shift]+2*StepSizeMax*Point;
	  SminMax0=Close[shift]-2*StepSizeMax*Point;
	  
	  SmaxMid0=Close[shift]+2*StepSizeMid*Point;
	  SminMid0=Close[shift]-2*StepSizeMid*Point;
	  
	  if(Close[shift]>SmaxMin1) TrendMin=1; 
	  if(Close[shift]<SminMin1) TrendMin=-1;
	  
	  if(Close[shift]>SmaxMax1) TrendMax=1; 
	  if(Close[shift]<SminMax1) TrendMax=-1;
	  
	  if(Close[shift]>SmaxMid1) TrendMid=1; 
	  if(Close[shift]<SminMid1) TrendMid=-1;
	  }
	 	
	  if(TrendMin>0 && SminMin0<SminMin1) SminMin0=SminMin1;
	  if(TrendMin<0 && SmaxMin0>SmaxMin1) SmaxMin0=SmaxMin1;
		
	  if(TrendMax>0 && SminMax0<SminMax1) SminMax0=SminMax1;
	  if(TrendMax<0 && SmaxMax0>SmaxMax1) SmaxMax0=SmaxMax1;
	  
	  if(TrendMid>0 && SminMid0<SminMid1) SminMid0=SminMid1;
	  if(TrendMid<0 && SmaxMid0>SmaxMid1) SmaxMid0=SmaxMid1;
	  
	  
	  if (TrendMin>0) linemin=SminMin0+StepSizeMin*Point;
	  if (TrendMin<0) linemin=SmaxMin0-StepSizeMin*Point;
	  
	  if (TrendMax>0) linemax=SminMax0+StepSizeMax*Point;
	  if (TrendMax<0) linemax=SmaxMax0-StepSizeMax*Point;
	  
	  if (TrendMid>0) linemid=SminMid0+StepSizeMid*Point;
	  if (TrendMid<0) linemid=SmaxMid0-StepSizeMid*Point;
	  
	  bsmin=linemax-StepSizeMax*Point;
	  bsmax=linemax+StepSizeMax*Point;
	  Stoch1=NormalizeDouble((linemin-bsmin)/(bsmax-bsmin),6);
	  Stoch2=NormalizeDouble((linemid-bsmin)/(bsmax-bsmin),6);
	  prev_y = (Stoch1 - Stoch2);
	  if(prev_y<0.0){
	     LineMinBuffer[shift] = prev_y;
	     LineMidBuffer[shift] = 0;
	     }
	  else if(prev_y>0.0){
	     LineMidBuffer[shift] = prev_y;
	     LineMinBuffer[shift] = 0;
	     }
	  
	  //prev_y = Stoch1;
	  //prev_b = Stoch2;	  
	  
	  SminMin1=SminMin0;
	  SmaxMin1=SmaxMin0;
	  
	  SminMax1=SminMax0;
	  SmaxMax1=SmaxMax0;
	  
	  SminMid1=SminMid0;
	  SmaxMid1=SmaxMid0;
	 }
	 
	 IndexMa = prev_y*10;
	 
	    if(IndexMa > 2) {
	        StepDirect=OP_BUY;
	        DrawDirect(OP_BUY);
	        }
	       else { if(IndexMa < -2) {
	        StepDirect=OP_SELL;
	        DrawDirect(OP_SELL);
	        }
	          else {
	            StepDirect=99;
	            DrawDirect(99);
	          }
	        }

	return(StepDirect);
}


int IMA_RSIDirected()
{//8

//---- input parameters

int    RSIDirect  =99; 

//---- input parameters
int RSIOMA          = 3;
int RSIOMA_MODE     = MODE_EMA;
int RSIOMA_PRICE    = PRICE_CLOSE;

int Ma_RSIOMA       = 21,Ma_RSIOMA_MODE  = MODE_EMA;

//int BuyTrigger      = 70;
//int SellTrigger     = 30;
//string short_name;

int MainTrendLong   = 50;
int MainTrendShort  = 50;
double RSIBuffer[];
double PosBuffer[];
double NegBuffer[];

double bdn[],bup[];
double sdn[],sup[];

double marsioma[];

int i;
int MAFastPeriod=3;
int MAFastShift=0;
int MAFastMethod=MODE_SMA;
int MAFastPrice=PRICE_CLOSE;

int MASlowPeriod=8;
int MASlowShift=0;
int MASlowMethod=MODE_SMA;
int MASlowPrice=PRICE_CLOSE;
int rete;

double rel,rel1,negative,positive;

double macd = iMACD(XSymbol,0,5,13,1,PRICE_CLOSE,MODE_MAIN,0);
int    counted_bars=IndicatorCounted();

  double fast1=iMA(XSymbol,0,MAFastPeriod,MAFastShift,MAFastMethod,MAFastPrice,1);
  double fast2=iMA(XSymbol,0,MAFastPeriod,MAFastShift,MAFastMethod,MAFastPrice,2);
  double slow1=iMA(XSymbol,0,MASlowPeriod,MASlowShift,MASlowMethod,MASlowPrice,1);
  double slow2=iMA(XSymbol,0,MASlowPeriod,MASlowShift,MASlowMethod,MASlowPrice,2);

  if (fast1>slow1&&fast2<slow2) rete = OP_BUY;
  if (fast1<slow1&&fast2>slow2) rete = OP_SELL;
   
//----
   if(Bars<=RSIOMA) return(99);
//---- initial zero
   if(counted_bars<1)
      for(i=1;i<=RSIOMA;i++) RSIBuffer[Bars-i]=0.0;
//----
   i=Bars-RSIOMA-1;
   
   int ma = i;
   
   if(counted_bars>=RSIOMA) i=Bars-counted_bars-1;
   
   while(i>=0)
     {
      double sumn=0.0,sump=0.0;
      if(i==Bars-RSIOMA-1)
        {
         int k=Bars-2;
         
         //---- initial accumulation
         while(k>=i)
           {
            
            double cma = iMA(XSymbol,0,RSIOMA,0,RSIOMA_MODE,RSIOMA_PRICE,k);
            double pma = iMA(XSymbol,0,RSIOMA,0,RSIOMA_MODE,RSIOMA_PRICE,k+1);
            
            rel=cma-pma;
            
            if(rel>0) sump+=rel;
            else      sumn-=rel;
            k--;
           }
         positive=sump/RSIOMA;
         negative=sumn/RSIOMA;
        }
      else
        {
         //---- smoothed moving average
         double ccma = iMA(XSymbol,0,RSIOMA,0,RSIOMA_MODE,RSIOMA_PRICE,i);
         double ppma = iMA(XSymbol,0,RSIOMA,0,RSIOMA_MODE,RSIOMA_PRICE,i+1);

         rel=ccma-ppma;
         
         if(rel>0) sump=rel;
         else      sumn=-rel;
         positive=(PosBuffer[i+1]*(RSIOMA-1)+sump)/RSIOMA;
         negative=(NegBuffer[i+1]*(RSIOMA-1)+sumn)/RSIOMA;
        }
         //    Comment(rel);
      PosBuffer[i]=positive;
      NegBuffer[i]=negative;
      if(negative==0.0) RSIBuffer[i]=0.0;
      else
      {
          RSIBuffer[i]=100.0-100.0/(1+positive/negative);
          
          bdn[i] = 0;
          bup[i] = 0;
          sdn[i] = 0;
          sup[i] = 0;
          
          if(RSIBuffer[i]>MainTrendLong)
          bup[i] = -10;
          
          if(RSIBuffer[i]<MainTrendShort)
          bdn[i] = -10;
          
          if(RSIBuffer[i]<30 && RSIBuffer[i]>RSIBuffer[i+1])
          sup[i] = -10;
          
          if(RSIBuffer[i]>70 && RSIBuffer[i]<RSIBuffer[i+1])
          sdn[i] = -10;
      }
      i--;
     }
     
     //while(ma>=0) Slow
   //count=OrdersTotal()-1;  //while(count>=0) { // while always delay much! do not use
   for(;ma>=0;ma--)     
     {
         marsioma[ma] = iMAOnArray(RSIBuffer,0,Ma_RSIOMA,0,Ma_RSIOMA_MODE,ma); 
     //    ma--;
     }

//double ma34 = iMA(XSymbol,PERIOD_M1,2,0,MODE_EMA,PRICE_CLOSE,0);
//double ma89 = iMA(XSymbol,PERIOD_M1,5,0,MODE_EMA,PRICE_CLOSE,0);
//double sarCurrent  = iSAR(XSymbol,PERIOD_M5,0.009,0.2,0);
//double sarPrevious = iSAR(XSymbol,PERIOD_M5,0.009,0.2,1);
string tripo;

if (rel > 0 && macd > 0) tripo = "BUY";
if (rel < 0 && macd < 0) tripo = "SELL";


   RSIDirect = 99;
   if (rel > 0 && rete == OP_BUY) RSIDirect = OP_BUY;
   if (rel < 0 && rete == OP_SELL) RSIDirect = OP_SELL;
 
	return(RSIDirect);
}

int CCIDirected()
{//9
//---- input parameters
int CCIDirect=99;
int CCIPeriod1=15;//day
int CCIPeriod2=20;//day
int CCItimeframe=60;

double indexCCI1=iCCI(XSymbol,CCItimeframe,CCIPeriod1,PRICE_TYPICAL,0);
double indexCCI2=iCCI(XSymbol,CCItimeframe,CCIPeriod2,PRICE_TYPICAL,1);

  if (indexCCI1 < -120) CCIDirect = OP_BUY;
  if (indexCCI1 > 120) CCIDirect = OP_SELL;
 
	return(CCIDirect);
}

int AODirected()
{//10
//---- input parameters
int AODirect=99;
int AOtimeframe=30; //30;
//double indexAO=0.014;

double indexAO0=iAO(XSymbol, AOtimeframe, 0);
double indexAO1=iAO(XSymbol, AOtimeframe, 1);
double indexAO2=iAO(XSymbol, AOtimeframe, 2);
double indexAO3=iAO(XSymbol, AOtimeframe, 3);

//  if (indexAO0 > 0 && indexAO1 < 0 && indexAO2 < 0 && indexAO3 < 0)
//      if(indexAO0 > indexAO1 && indexAO1 > indexAO2 && indexAO2 > indexAO3)
if (indexAO0 < indexAOlow)
         AODirect = OP_BUY;
         
//  if (indexAO0 < 0 && indexAO1 > 0 && indexAO2 > 0 && indexAO3 > 0)
//      if(indexAO0 < indexAO1 && indexAO1 < indexAO2 && indexAO2 < indexAO3)
if (indexAO0 > indexAOhigh)
        AODirect = OP_SELL;
 
	return(AODirect);
}

int MACD()
{
   int MACDDirect=99;      
   //if (iMACD(14,26,9,MODE_MAIN,0)>0 and iMACD(14,26,9,MODE_MAIN,0)>iMACD(14,26,9,MODE_MAIN ,1)) then OrderType=2;
   if (iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,0)>
       iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,1)) { MACDDirect=OP_BUY; }
      //if (iMACD(14,26,9,MODE_MAIN,0)<0 and iMACD(14,26,9,MODE_MAIN,0)<iMACD(14,26,9,MODE_MAIN ,1)) then OrderType=1;
   if (iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,0)<
       iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,1)) { MACDDirect=OP_SELL; }

	return(MACDDirect);
}