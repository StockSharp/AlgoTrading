//+------------------------------------------------------------------+
//| 10points 3.mq4  10p3v005.mq4
//| Copyright 2008, enhanced K LAM
//| Copyright 2008, K LAM
//| http://FxKillU.com
//+------------------------------------------------------------------+

#property copyright "Copyright 2008, enhanced K LAM"
#property link "http://FxKillU.com"

#define MAGICMA  20070424
#define Version  20090303

extern string Name_Expert= "10p3v005";
extern string OWN="Copyright 2008, K LAM";

int MagicNo[3] = {200704240,200704241,200704242}; //extern int    MagicNumber=20070424;  // Magic number for the orders placed
int Magic0=0,MagicB=1,MagicS=2; //OP_BUY 0 Buying position. OP_SELL 1 Selling position. 
extern int Bside=1;
extern int Sside=1;  
extern int OpenHour      = 0;
extern int CloseHour     = 0;

extern double TakeProfit = 8; //40
extern double Lots = 0.1;
extern double InitialStop = 0;
extern double TrailingStop = 20;
//extern double StopLoss = 5000;  // StopLoss
extern double multiply = 2; //2.0; 

extern int Pips=15; // 15
extern int MaxTrades=1;
extern int OrderstoProtect=3;
extern int SecureProfit=300; //30

extern int mm=0;
extern int risk=12;
extern int AccountisNormal=0;

extern bool   StopLossProtection=false; // 
extern double StopLoss = 200;  // StopLoss

extern bool   MyMoneyStartProtection=false; // true;// 
extern double My_Money_Start_Protection= 1000; //at hold AC 50; a day

extern bool   MyMoneyProfitTarget=  false; //true;//
extern double My_Money_Profit_Target= 1300; //at hold AC 50; a day

extern int ReboundLock=1;
extern int AccountProtection=1;
extern int ReverseCondition=0;

extern int Logrecord = 1;
int OpenOrders=0, cnt=0;
int slippage=5;
double sl=0, tp=0;
double BuyPrice=0, SellPrice=0;
double lotsi=0, mylotsi=0;

int PreviousOpenOrders=0;
int LastTicket=0, LastType=0;
int mode=0, myOrderType=0;
bool ContinueOpening=True;

double LastPrice=0;
double Profit=0,ProfitPips=0;
double LastClosePrice=0, LastLots=0, StockLots=0;
double Pivot=0;
double PipValue=0;
double miniLot=0,maxLot=0;

//string OSymbol = "GBPUSD";
string XSymbol = "GBPUSD";

string text="", text2="";
string myOrderText="BUY | SELL";

double ticketno=-1; // for ordersend

color ArrowsColorModify=Purple; //orders Modify color
color ArrowsColorUp=Aqua;       //orders UP color
color ArrowsColorDown=Red;      //orders DOWN color
color ArrowsColor=White;  //Orange; //Green;        //orders arrows color

double TmpMark=0;
double MaxDrawdown=0,MaxLots=0;
double LowFreeMargin=0,LowAccountEquity=0,MaxBalance=0;

datetime StartDate,EndDate;

int MinuteToStop = 55;

//+------------------------------------------------------------------+
//| expert initialization function |
//+------------------------------------------------------------------+
int init()
{
//----
   StartDate = TimeCurrent();
   XSymbol=Symbol();
   LowAccountEquity = AccountEquity();
   LowFreeMargin = AccountFreeMargin();
   MaxBalance = AccountBalance();

   miniLot=MarketInfo(XSymbol,MODE_MINLOT); // mini order at the lots pre Set
   maxLot =MarketInfo(XSymbol,MODE_MAXLOT); // mini order at the lots pre Set
   PipValue=MarketInfo(XSymbol,MODE_TICKVALUE);
   MaxDrawdown=0;
   //Magic0=0,MagicB=1,MagicS=2;
   if (Bside==1) Magic0=MagicB;
   if (Sside==1) Magic0=MagicS;
   
   if (Bside==1 && Sside==1) Magic0=0;

//----
return(0);
}
//+------------------------------------------------------------------+
//| expert deinitialization function |
//+------------------------------------------------------------------+
int deinit()
{
//---- 
   EndDate = TimeCurrent();
   //record Lot to File
   
      //write to log file
if (Logrecord ==1) { 	
   int Onefile = FileOpen( StringConcatenate( Name_Expert,"(", AccountServer(), ").csv"), FILE_CSV | FILE_READ | FILE_WRITE,',');
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
}

//----
return(0);
}


//+------------------------------------------------------------------+
//| expert start function |
//+------------------------------------------------------------------+
int start()
{
//---- 
   if (!IsTradeTime() && !IsTradeTimeA()) {
   if (OrdersTotal() !=0) CloseAll(XSymbol,MagicNo[Magic0]);
   return;
   }
   if (DayOfWeek()==5) {
   if (OrdersTotal() !=0) CloseAll(XSymbol,MagicNo[Magic0]);
   return;
   }

double imacd1,imacd2;
         imacd1=iMACD(XSymbol,0,14,26,9,PRICE_CLOSE,MODE_MAIN,0);
         imacd2=iMACD(XSymbol,0,14,26,9,PRICE_CLOSE,MODE_MAIN,1);

PipValue=MarketInfo(Symbol(),MODE_TICKVALUE); //update each Minute
slippage =(Ask-Bid)/Point; //slippage =NormalizeDouble((Ask-Bid)/Point);

      TmpMark = AccountEquity();
      if(TmpMark < LowAccountEquity) {LowAccountEquity=TmpMark;}

      TmpMark = AccountFreeMargin();
      if(TmpMark < LowFreeMargin) {LowFreeMargin=TmpMark;}
      
      TmpMark = AccountBalance();
      if(TmpMark > MaxBalance) {MaxBalance=TmpMark;}

   if (mm!=0) {
      if (AccountisNormal==1) {lotsi=MathCeil(AccountBalance()*risk/10000);}
      else {lotsi=MathCeil(AccountBalance()*risk/10000)/10; }// then is mini AccountisNormal!=1
   }
   else { lotsi=Lots; } //then no Money Management
   
   if (lotsi < miniLot) lotsi=miniLot;
   if (lotsi > maxLot) lotsi=maxLot;
   if (lotsi > 100) lotsi=100;

      OpenOrders=0;
      for(cnt=0;cnt<OrdersTotal();cnt++) {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()==XSymbol && OrderMagicNumber() == MagicNo[Magic0]) { //if (OrderSymbol()==Symbol()) {
            OpenOrders++;  }
         } 

      if (PreviousOpenOrders>OpenOrders && OrdersTotal() != 0) {
      Print("OpenOrders reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity()," Last Profit ",AccountProfit());
      // X CloseAll(XSymbol,MagicNo[Magic0]);
      Comment("Close All Order: ",AccountBalance());
      PreviousOpenOrders=OpenOrders;
      }
      
      //if (AccountEquity() > AccountBalance() * 1.05) {
//      Print("Green Profit reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity(),"Last Profit ",AccountProfit());
//      Comment("Profit Equity 1.05 reached: ",AccountBalance());
//      CloseAll("All",0);
      //}
      
//      if (AccountProfit() > 30) {//if (MyMoneyProfitTarget && AccountEquity() > My_Money_Profit_Target)
//      Print("Green Profit reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity(),"Last Profit ",AccountProfit());
//      CloseAll("All",0); //CloseAll(XSymbol,MagicNo[Magic0]); //PreviousOpenOrders=OpenOrders+1;
//      }
//   if (MyMoneyProfitTarget && AccountEquity() > My_Money_Profit_Target) {
 
   if (StopLossProtection && AccountProfit() < -StopLoss) {
    text = text + "\nStop Loss reached. Closing all orders..";
    Print("Stop Loss reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity()," Last Profit ",AccountProfit());
    
    //X CloseAll("All",0); //CloseAll(XSymbol,MagicNo[Magic0]); //PreviousOpenOrders=OpenOrders+1;
    Comment("Stop Loss reached: ",My_Money_Profit_Target,"  AccountBalance: ",AccountBalance());
    }

   //set my profit Stop trading
   if (MyMoneyProfitTarget && AccountEquity() > My_Money_Profit_Target) {
    if (OrdersTotal() == 0) return(0);
    if (AccountEquity() > My_Money_Profit_Target+15) {
    if (OrdersTotal() == 0) return(0);
    text = text + "\nMoneyProfitTarget reached. Closing all orders..";
    Print("MoneyProfitTarget reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity()," Last Profit ",AccountProfit());
    
    //X CloseAll("All",0); //CloseAll(XSymbol,MagicNo[Magic0]); //PreviousOpenOrders=OpenOrders+1;
    //DeleteQ(XSymbol,MagicNo[Magic0]);
    Comment("Money Profit Target reached: ",My_Money_Profit_Target,"  AccountBalance: ",AccountBalance());
    
    ContinueOpening=False;
    return(0);
    }
   }
   
   if (MyMoneyStartProtection && AccountBalance() < My_Money_Start_Protection) {
    if (OrdersTotal() == 0) return(0);   
    text = text + "\nStart_Protection reached. Closing all orders..";
    Print("Start_Protection reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity()," Last Profit ",AccountProfit());
    
    //X CloseAll("All",0); //CloseAll(XSymbol,MagicNo[Magic0]); //PreviousOpenOrders=OpenOrders+1;
    //DeleteQ(XSymbol,MagicNo[Magic0]);
    Comment("Start_Protection reached: ",My_Money_Start_Protection,"  AccountBalance: ",AccountBalance());

    ContinueOpening=False;
    return(0);
   }
   
   PreviousOpenOrders=OpenOrders;
   
      if (OpenOrders>=MaxTrades) {ContinueOpening=False;}
      else {ContinueOpening=True;}

      if (LastPrice==0) {
         for(cnt=0;cnt<OrdersTotal();cnt++) { 
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            mode=OrderType();
            if (OrderSymbol()==XSymbol && OrderMagicNumber() == MagicNo[Magic0]) {
               LastPrice=OrderOpenPrice();
               if (mode==OP_BUY) { myOrderType=OP_BUY; }
               if (mode==OP_SELL) { myOrderType=OP_SELL; }
            }
         }
      }

      if (true) { //OpenOrders<1) {
         myOrderType=99;
         //if (iMACD(14,26,9,MODE_MAIN,0)>0 and iMACD(14,26,9,MODE_MAIN,0)>iMACD(14,26,9,MODE_MAIN,1)) then OrderType=2;
	      //if (iMACD(14,26,9,MODE_MAIN,0)<0 and iMACD(14,26,9,MODE_MAIN,0)<iMACD(14,26,9,MODE_MAIN,1)) then OrderType=1;
	      //if (iMACD(NULL,0,14,26,9,PRICE_CLOSE,MODE_MAIN,0)>iMACD(NULL,0,14,26,9,PRICE_CLOSE,MODE_MAIN,1)) { myOrderType=2; }	  
	      //if (iMACD(NULL,0,14,26,9,PRICE_CLOSE,MODE_MAIN,0)<iMACD(NULL,0,14,26,9,PRICE_CLOSE,MODE_MAIN,1)) { myOrderType=1; }

         imacd1=iMACD(XSymbol,0,14,26,9,PRICE_CLOSE,MODE_MAIN,0);
         imacd2=iMACD(XSymbol,0,14,26,9,PRICE_CLOSE,MODE_MAIN,1);
//         imacd1=iMACD("EURUSD",0,14,26,9,PRICE_CLOSE,MODE_MAIN,0);
//         imacd2=iMACD("EURUSD",0,14,26,9,PRICE_CLOSE,MODE_MAIN,1);
         if (imacd1>imacd2 && Bside) { myOrderType=OP_BUY; }
         if (imacd1<imacd2 && Sside) { myOrderType=OP_SELL; }
             
         if (ReverseCondition==1) {
            if (myOrderType==OP_SELL) { myOrderType=OP_BUY; }
            else { if (myOrderType==OP_BUY) { myOrderType=OP_SELL; } }
         }
      }
      
      // if we have opened positions we take care of them
      if (TrailingStop>0) {
      for(cnt=OrdersTotal();cnt>=0;cnt--) {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNo[Magic0]) {
            
            if (OrderType()==OP_SELL) { 
               if (OrderOpenPrice()-Ask>=(TrailingStop+Pips)*Point) { 
                  if (OrderStopLoss()>(Ask+Point*TrailingStop)) {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderClosePrice()-TakeProfit*Point-TrailingStop*Point,0,Purple);//800
                     return(0);
                  }
               }
            }
            
            if (OrderType()==OP_BUY) {
               if (Bid-OrderOpenPrice()>=(TrailingStop+Pips)*Point) {
                  if (OrderStopLoss()<(Bid-Point*TrailingStop)) {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderClosePrice()+TakeProfit*Point+TrailingStop*Point,0,Yellow);//800
                     return(0);
                  }
               }
            }

         }
      } //for
      } //(TrailingStop>0) 

      Profit=0;TmpMark=0;ProfitPips=0;StockLots=0;
      LastTicket=0;
      LastType=0;
      LastClosePrice=0;
      LastLots=0; 

      for(cnt=0;cnt<OrdersTotal();cnt++) {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()==XSymbol && OrderMagicNumber() == MagicNo[Magic0]) {
            LastTicket=OrderTicket();
            LastType=OrderType();  //if (OrderType()==OP_BUY) { LastType=OP_BUY; }  if (OrderType()==OP_SELL) { LastType=OP_SELL; }
            LastClosePrice=OrderClosePrice();
            LastLots=OrderLots();
            StockLots+=LastLots;
            TmpMark+=OrderProfit(); //Profit+=OrderProfit();
            ProfitPips+=(OrderProfit()/LastLots)/PipValue;
         }
      }//for

      Profit=TmpMark; //Profit=Profit*PipValue;
      text2="Profit: $"+DoubleToStr(Profit,2)+" Pips="+DoubleToStr(ProfitPips,2)+"+/-\nStockLots="+DoubleToStr(StockLots,2); //  OpenOrders="+OpenOrders+" 

      //take a record
      if(TmpMark < 0 && MaxDrawdown>TmpMark) { //should be -
         MaxDrawdown=TmpMark;//Print("Wow....New Drawdown Record at $",MaxDrawdown);
         }

      if (OpenOrders>=OrderstoProtect && AccountProtection==1) {//Print(Symbol,":",Profit);
         //StopLoss = AccountBalance() *0.3;
         //else if (OpenOrders=MaxTrades && Profit < -StopLoss) { //Stop Loss  CloseAll(XSymbol,MagicNo[Magic0]);}
         if (Profit>SecureProfit) {
            Print("SecureProfit reached. Closing all orders... Balance: ",AccountBalance()," Equity: ",AccountEquity(),"Last Profit ",AccountProfit());
            //X CloseAll(XSymbol,MagicNo[Magic0]); //OrderClose(LastTicket,LastLots,LastClosePrice,slippage,Yellow);
            ContinueOpening=False;
            return(0);
         } 
      }

      if (true) { //(!IsTesting()) {
         if (myOrderType==99) { text="No conditions to open trades"; }
         else { text=" "; }
         
         if (myOrderType==99) { myOrderText="BUY Vs SELL???"; }
         if (myOrderType==OP_BUY) { myOrderText="OP_BUY"; }
         if (myOrderType==OP_SELL) { myOrderText="OP_SELL"; }

         Comment(XSymbol," Ver=",Version,
         "\nLastPrice=",LastPrice," Previous open orders=",PreviousOpenOrders,
         "\nContinue opening=",ContinueOpening," OrderType=",myOrderText,
         "\nMax Orders=",MaxTrades ," Protect Order=",OrderstoProtect,
         "\nMagicNo=",MagicNo[Magic0]," MaxDrawdown=",MaxDrawdown,
         "\n",text2,"\nLots=",lotsi ,"\n",text,
         "\nSPREAD=",MarketInfo(XSymbol,MODE_SPREAD),//"~",Ask-Bid,
         "\nPipValue=",PipValue,
         "\nmacd == ",imacd1," >< ",imacd2,
         "  Rate== ",imacd1/imacd1," : ",imacd2/imacd1);
      }
      
double madirect;

      if (myOrderType==OP_SELL && ContinueOpening) {
         //madirect=iMA(XSymbol,60,48,0,MODE_SMA,PRICE_HIGH,0);  //4 day       
         //if (Bid<madirect == false) return; //{ //sell
         
         SellPrice=Bid;
         if ((SellPrice-LastPrice)>=Pips*Point || OpenOrders<1) { //Bid - LastPrice
            LastPrice=0;
            
            if (TakeProfit==0) { tp=0; }
            else { tp=SellPrice-TakeProfit*Point; }
            if (InitialStop==0) { sl=0; }
            else { sl=SellPrice+InitialStop*Point; }
            
            if (OpenOrders!=0) { //mylotsi=lotsi*multiply*OpenOrders; 
               if (MaxTrades>12) mylotsi=NormalizeDouble(lotsi*MathPow(1.5,OpenOrders),1); //2
               else mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),1);
            } else { mylotsi=lotsi; } //OpenOrders==0
            
            if (mylotsi>100) { mylotsi=100; }
            if (mylotsi>maxLot) mylotsi=maxLot;
            mylotsi-=MathMod(mylotsi,MarketInfo(XSymbol,MODE_LOTSTEP));// fix after 0.01
            if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
            Print("MathPow(multiply,OpenOrders)=",MathPow(multiply,OpenOrders),"mylotsi=",mylotsi,"=",NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2));
            
            ticketno=OrderSend(XSymbol,OP_SELL,mylotsi,SellPrice,slippage,sl,tp,Name_Expert+Version,MagicNo[Magic0],0,Red);
            if(ticketno == -1) { if (GetLastError()==131) Print("error 131 mylotsi=",mylotsi);}
            //else if (OpenOrders>=OrderstoProtect && AccountProtection==1 && ReboundLock == 1) ReboundSet(MagicNo[Magic0]);//else {OrderComplete=1;}
            if (mylotsi>MaxLots) MaxLots=mylotsi; //mark a record
            return(0);
         }
      }

      if (myOrderType==OP_BUY && ContinueOpening) {
         //madirect=iMA(XSymbol,60,48,0,MODE_SMA,PRICE_LOW,0);  //4 day
        	//if (Ask>madirect == false) return;
        	
         BuyPrice=Ask;
         if ((LastPrice-BuyPrice)>=Pips*Point || OpenOrders<1) { //BuyPrice=Ask;
            LastPrice=0;
            
            if (TakeProfit==0) { tp=0; }
            else { tp=BuyPrice+TakeProfit*Point; } 
            if (InitialStop==0) { sl=0; }
            else { sl=BuyPrice-InitialStop*Point; }
            
            if (OpenOrders!=0) { //mylotsi=lotsi*multiply*OpenOrders;
               if (MaxTrades>12) mylotsi=NormalizeDouble(lotsi*MathPow(1.5,OpenOrders),1); //2
               else mylotsi=NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),1);
            } else { mylotsi=lotsi; } //OpenOrders==0

            if (mylotsi>100) { mylotsi=100; }
            if (mylotsi>maxLot) mylotsi=maxLot;
            mylotsi-=MathMod(mylotsi,MarketInfo(XSymbol,MODE_LOTSTEP));// fix after 0.01
            
            if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
            Print("MathPow(multiply,OpenOrders)=",MathPow(multiply,OpenOrders),"mylotsi=",NormalizeDouble(lotsi*MathPow(multiply,OpenOrders),2));            
            
            ticketno=OrderSend(XSymbol,OP_BUY,mylotsi,BuyPrice,slippage,sl,tp,Name_Expert+Version,MagicNo[Magic0],0,Blue);
            if(ticketno == -1) { if (GetLastError()==131) Print("131 mylotsi=",mylotsi);}
            //else if (OpenOrders>=OrderstoProtect && AccountProtection==1 && ReboundLock == 1) ReboundSet(MagicNo[Magic0]);//else {OrderComplete=1;}            
		      if (mylotsi>MaxLots) MaxLots=mylotsi; //mark a record
            return(0);
         }
      }
 if (OpenOrders>=OrderstoProtect && AccountProtection==1 && ReboundLock == 1) ReboundSet(MagicNo[Magic0]);
//----
return(0);
}

bool IsTradeTime() {
   if (OpenHour < CloseHour && TimeHour(TimeCurrent()) < OpenHour || TimeHour(TimeCurrent()) >= CloseHour) return (FALSE);
   if (OpenHour > CloseHour && (TimeHour(TimeCurrent()) < OpenHour && TimeHour(TimeCurrent()) >= CloseHour)) return (FALSE);
   if (OpenHour == 0) CloseHour = 24;
   if (Hour() == CloseHour - 1 && Minute() >= MinuteToStop) return (FALSE);
   return (TRUE);
}


bool IsTradeTimeA() {
   if (DayOfWeek() == 5 && Hour()>=CloseHour) return (FALSE);
   return (TRUE);
}

void CloseAll(string SymbolToClose,int MagNo) //at Symbol
{
bool   result;
int timeout=0;
int cmd,error;
int cnt;
int CloseNo;
double TAsk,TBid;

   for (cnt=10;IsTradeContextBusy();cnt--) {
   cnt+=2;
   Sleep(10);
    //Print("Trade context is busy. Please wait");
    }
    
   CloseNo=OrdersTotal();
   if(CloseNo==0) return(0);
      TAsk=Ask; // for the price change feq ... so lock the target Ask & Bid
      TBid=Bid;
   
   for(cnt=OrdersTotal();cnt > 0;cnt--) {//last to frist -1
      if(OrderSelect(cnt-1,SELECT_BY_POS, MODE_TRADES)==true) {
      if((SymbolToClose=="All") || 
         (OrderSymbol()==SymbolToClose && MagNo == 0) ||
         (OrderSymbol()==SymbolToClose && OrderMagicNumber() == MagNo)) {
            cmd=OrderType();
            while(true) { // RefreshRates();
             if(cmd==OP_BUY)
               result=OrderClose(OrderTicket(),OrderLots(),TAsk,slippage,ArrowsColor);
             if(cmd==OP_SELL)
               result=OrderClose(OrderTicket(),OrderLots(),TBid,slippage,ArrowsColor);

               if(result!=TRUE) { error=GetLastError(); Print("LastError = ",error); }
               else error=0;
               if(error==129) {timeout++; Sleep(100); if (timeout>10) break;}
               if(error==129 || error==135 || error==146) continue;// RefreshRates(); 138
               else break;
            }//while
         }//if
      } else Print( "Error when order select ", GetLastError());//OrderSelect
      if (timeout>10) break;
      }//for
   CloseNo=CloseNo-OrdersTotal();   //CloseNo-=OrdersTotal();   
   if(!IsTesting()) Print(CloseNo," Orders Closed, ",SymbolToClose,"=",MagNo," Balance: ", AccountBalance()," Equity: ",AccountEquity(),"Last Profit ",AccountProfit());
}

void ReboundSet(int MagicNoset)
{// if (OpenOrders>=OrderstoProtect) ReboundSet();

bool     result;
int      cnt,err;
int      ReOrderType;
double   btxprice=1000,stxprice=0;
double   tpx,slx;

      Sleep(100);//---- wait for 1/10 seconds
      RefreshRates();
      
     //Get the Last price
   for(cnt=OrdersTotal();cnt>0;cnt--) {//while(count>=0) { // while always delay much! do not use
      if(OrderSelect(cnt-1,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol() == XSymbol && OrderMagicNumber() == MagicNoset) {// && Reversed==False) 
         ReOrderType=OrderType();
         if (ReOrderType != OP_BUY && ReOrderType !=OP_SELL) continue;
         if (ReOrderType==OP_BUY) {
         if (OrderOpenPrice() < btxprice) btxprice=OrderOpenPrice();}
         if (ReOrderType==OP_SELL) {
         if (OrderOpenPrice() > stxprice) stxprice=OrderOpenPrice();}
      }
   }//for
      
     //Rebound Modify Set all Order at same sl tp
   for(cnt=OrdersTotal();cnt>0;cnt--) {//while(count>=0) { // while always delay much! do not use
      if(OrderSelect(cnt-1,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol() == XSymbol && OrderMagicNumber() == MagicNoset) {// && Reversed==False)
         ReOrderType=OrderType();
         if (ReOrderType==OP_BUY || ReOrderType==OP_SELL) {
            if (ReOrderType==OP_BUY) {
               tpx= btxprice+(Pips*Point);// SecureProfit*Point);PipsTakeProfit
               slx= btxprice-(TrailingStop*Point);}
            if (ReOrderType==OP_SELL) {
               tpx= stxprice-(Pips*Point);//TakeProfit
               slx= stxprice+(TrailingStop*Point);} //if (OrderOpenPrice()-Ask>=(TrailingStop+Pips)*Point) && (OrderStopLoss()>(Ask+Point*TrailingStop)) {
               
            tpx=NormalizeDouble(tpx, Digits);
            slx=NormalizeDouble(slx, Digits);
            
            if (NormalizeDouble(tpx, Digits)==NormalizeDouble(OrderTakeProfit(), Digits) 
             && NormalizeDouble(slx, Digits)==NormalizeDouble(OrderStopLoss(), Digits)) continue;
            else {
            
               if(slx==0) result=OrderModify(OrderTicket(),OrderOpenPrice(),0,tpx,0,ArrowsColorModify);
               else result=OrderModify(OrderTicket(),OrderOpenPrice(),0,tpx,0,ArrowsColorModify);//slx

            if(IsTesting()) { //!
            if(!result) {//|| GetLastError()!=1) 
              err=GetLastError();
              Print("LastError = ",err," tpx=",tpx," slx=",slx);}
            else Print("Ticket ",OrderTicket()," modified.");            //   else OrderPrint();
            }//IsTesting()
                         //         } //ERR_NO_RESULT 1 No error returned, but the result is unknown 
            }//else
	     }//if
      }
   }//for
}
//+------------------------------------------------------------------+