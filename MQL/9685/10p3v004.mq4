//+------------------------------------------------------------------+
//| 10points 3.mq4  10p3v004.mq4
//| Copyright 2008, enhanced K LAM
//| Copyright 2008, K LAM
//| http://FxKillU.com
//+------------------------------------------------------------------+

#property copyright "Copyright 2008, enhanced K LAM"
#property link "http://FxKillU.com"

#define MAGICMA  20070424
#define Version  20081204

extern string Name_Expert= "10p3v004";
extern string OWN="Copyright 2008, K LAM";

extern int    MagicNumber=20070424;  // Magic number for the orders placed
extern int    MagicBuy=200704241;
extern int    MagicSELL=200704242;

int MagicNo[3] = {20070424,200704241,200704242};
int Magic0=0,MagicB=1,MagicS=2;

extern double TakeProfit = 40;
extern double Lots = 0.01;
extern double InitialStop = 0;
extern double TrailingStop = 20;

extern int Pips=15;
extern int MaxTrades=9;//6;
extern int OrderstoProtect=3;//5;
extern int SecureProfit=8;

extern int AccountProtection=1;
extern int ReverseCondition=0;

//extern double EURUSDPipValue=10;
//extern double GBPUSDPipValue=10;
//extern double USDCHFPipValue=10;
//extern double USDJPYPipValue=9.715;

extern int mm=0;
extern int risk=12;
extern int AccountisNormal=0;

int OpenOrders=0, cnt=0;
int slippage=5;
double sl=0, tp=0;
double BuyPrice=0, SellPrice=0;
double lotsi=0, mylotsi=0;

int mode=0, myOrderType=0;
bool ContinueOpening=True;
double LastPrice=0;
int PreviousOpenOrders=0;
double Profit=0;
int LastTicket=0, LastType=0;

double LastClosePrice=0, LastLots=0;
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
color ArrowsColor=Orange; //Green;        //orders arrows color

double TmpMark=0;
double MaxDrawdown=0,MaxLots=0;
double LowFreeMargin=0,LowAccountEquity=0;

datetime StartDate,EndDate;

//+------------------------------------------------------------------+
//| expert initialization function |
//+------------------------------------------------------------------+
int init()
{
//----
   StartDate = TimeCurrent();
   XSymbol=Symbol();
   miniLot=MarketInfo(XSymbol,MODE_MINLOT); // mini order at the lots pre Set
   maxLot =MarketInfo(XSymbol,MODE_MAXLOT); // mini order at the lots pre Set
   PipValue=MarketInfo(Symbol(),MODE_TICKVALUE);
   MaxDrawdown=0;

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
//----
return(0);
}
//+------------------------------------------------------------------+
//| expert start function |
//+------------------------------------------------------------------+
int start()
{
//---- 

if (AccountisNormal==1) {
      if (mm!=0) { lotsi=MathCeil(AccountBalance()*risk/10000); }
            else { lotsi=Lots; }
                  } else { // then is mini AccountisNormal!=1
                  if (mm!=0) { lotsi=MathCeil(AccountBalance()*risk/10000)/10; }
                  else { lotsi=Lots; }
                  }

if (lotsi < miniLot) lotsi=miniLot;
if (lotsi > maxLot) lotsi=maxLot;
if (lotsi > 100) lotsi=100;

      OpenOrders=0;
      for(cnt=0;cnt<OrdersTotal();cnt++) {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNo[0]) { //if (OrderSymbol()==Symbol()) {
            OpenOrders++;  }
         } 

//if (Symbol()=="EURUSD") { PipValue=EURUSDPipValue; }
//if (Symbol()=="GBPUSD") { PipValue=GBPUSDPipValue; }
//if (Symbol()=="USDJPY") { PipValue=USDJPYPipValue; }
//if (Symbol()=="USDCHF") { PipValue=USDCHFPipValue; }
//if (PipValue==0) { PipValue=MarketInfo(Symbol(),MODE_TICKVALUE); } //=5;
int upmin = Minute();
if (upmin == 0 || upmin == 30) 
PipValue=MarketInfo(Symbol(),MODE_TICKVALUE); //update each Minute

      if (PreviousOpenOrders>OpenOrders) { 
         for(cnt=OrdersTotal(); cnt>=0; cnt--) {
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            mode=OrderType();
            if (OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNo[0]) {
               if (mode==OP_BUY) { OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage,Blue); }
               if (mode==OP_SELL) { OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage,Red); }
               return(0);
               }
         }// for
      }

   PreviousOpenOrders=OpenOrders;
   
      if (OpenOrders>=MaxTrades) {ContinueOpening=False;}
      else {ContinueOpening=True;}

      if (LastPrice==0) {
         for(cnt=0;cnt<OrdersTotal();cnt++) { 
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            mode=OrderType();
            if (OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNo[0]) {
               LastPrice=OrderOpenPrice();
               if (mode==OP_BUY) { myOrderType=OP_BUY; }
               if (mode==OP_SELL) { myOrderType=OP_SELL; }
            }
         }
      }

      if (OpenOrders<1) {
         myOrderType=99;
         //if (iMACD(14,26,9,MODE_MAIN,0)>0 and iMACD(14,26,9,MODE_MAIN,0)>iMACD(14,26,9,MODE_MAIN ,1)) then OrderType=2;
         if (iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,0)>
             iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,1)) { myOrderType=OP_BUY; }
         //if (iMACD(14,26,9,MODE_MAIN,0)<0 and iMACD(14,26,9,MODE_MAIN,0)<iMACD(14,26,9,MODE_MAIN ,1)) then OrderType=1;
         if (iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,0)<
             iMACD(Symbol(),0,14,26,9,PRICE_CLOSE,MODE_MAIN,1)) { myOrderType=OP_SELL; }

         if (ReverseCondition==1) {
            if (myOrderType==OP_SELL) { myOrderType=OP_BUY; }
            else { if (myOrderType==OP_BUY) { myOrderType=OP_SELL; } }
         }
      }
      
      // if we have opened positions we take care of them
      if (TrailingStop>0) {
      for(cnt=OrdersTotal();cnt>=0;cnt--) {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNo[0]) {
            
            if (OrderType()==OP_SELL) { 
               if (OrderOpenPrice()-Ask>=(TrailingStop+Pips)*Point) { 
                  if (OrderStopLoss()>(Ask+Point*TrailingStop)) {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderClosePrice()-TakeProfit*Point-TrailingStop*Point,800,Purple);
                     return(0);
                  }
               }
            }
            
            if (OrderType()==OP_BUY) {
               if (Bid-OrderOpenPrice()>=(TrailingStop+Pips)*Point) {
                  if (OrderStopLoss()<(Bid-Point*TrailingStop)) {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderClosePrice()+TakeProfit*Point+TrailingStop*Point,800,Yellow);
                     return(0);
                  }
               }
            }

         }
      } //for
      } //(TrailingStop>0) 

      Profit=0;TmpMark=0;
      LastTicket=0;
      LastType=0;
      LastClosePrice=0;
      LastLots=0; 

      for(cnt=0;cnt<OrdersTotal();cnt++) {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if (OrderSymbol()==Symbol() && OrderMagicNumber() == MagicNo[0]) {
            LastTicket=OrderTicket();
            LastType=OrderType();  //if (OrderType()==OP_BUY) { LastType=OP_BUY; }  if (OrderType()==OP_SELL) { LastType=OP_SELL; }
            LastClosePrice=OrderClosePrice();
            LastLots=OrderLots();
            TmpMark+=OrderProfit(); //Profit+=OrderProfit();
            
            if (LastType==OP_BUY) { //Profit=Profit+(Ord(cnt,VAL_CLOSEPRICE)-Ord(cnt,VAL_OPENPRICE))*PipValue*Ord(cnt,VAL_LOTS);
               if (OrderClosePrice()<OrderOpenPrice()) { Profit=Profit-(OrderOpenPrice()-OrderClosePrice())*OrderLots()/Point; }
               if (OrderClosePrice()>OrderOpenPrice()) { Profit=Profit+(OrderClosePrice()-OrderOpenPrice())*OrderLots()/Point; }
            }
            if (LastType==OP_SELL) {//Profit=Profit+(Ord(cnt,VAL_OPENPRICE)-Ord(cnt,VAL_CLOSEPRICE))*PipValue*Ord(cnt,VAL_LOTS );
               if (OrderClosePrice()>OrderOpenPrice()) { Profit=Profit-(OrderClosePrice()-OrderOpenPrice())*OrderLots()/Point; }
               if (OrderClosePrice()<OrderOpenPrice()) { Profit=Profit+(OrderOpenPrice()-OrderClosePrice())*OrderLots()/Point; }
            }
            //Print(Symbol,":",Profit,",",LastLots);
         }
      }//for
      
      //take a record
      if(TmpMark < 0 && MaxDrawdown>TmpMark) { //should be -
         MaxDrawdown=TmpMark;
         //Print("Wow....New Drawdown Record at $",MaxDrawdown);
         }      
      
      Profit=Profit*PipValue;
      text2="Profit: $"+DoubleToStr(Profit,2)+"~"+TmpMark+" +/-  OpenOrders="+OpenOrders;
      
      if (OpenOrders>=OrderstoProtect && AccountProtection==1) {//Print(Symbol,":",Profit);
         if (Profit>=SecureProfit) {
            //OrderClose(LastTicket,LastLots,LastClosePrice,slippage,Yellow);
            CloseAll(XSymbol,MagicNo[0]);
            ContinueOpening=False;
            return(0);
         }
      }

      if (!IsTesting()) {
         if (myOrderType==99) { text="No conditions to open trades"; }
         else { text=" "; }
         
         if (myOrderType==99) { myOrderText="BUY Vs SELL???"; }
         if (myOrderType==OP_BUY) { myOrderText="OP_BUY"; }
         if (myOrderType==OP_SELL) { myOrderText="OP_SELL"; }

         Comment(XSymbol," Ver=",Version,
         "\nLastPrice=",LastPrice," Previous open orders=",PreviousOpenOrders,
         "\nContinue opening=",ContinueOpening," OrderType=",myOrderText,
         "\nMax Orders=",MaxTrades ," Protect Order=",OrderstoProtect,
         "\nMagicNo=",MagicNo[myOrderType]," MaxDrawdown=",MaxDrawdown,
         "\n",text2,"\nLots=",lotsi ,"\n",text,
         "\nPipValue=",PipValue);
      }
      
      if (myOrderType==OP_SELL && ContinueOpening) { 
         if ((Bid-LastPrice)>=Pips*Point || OpenOrders<1) { 
            SellPrice=Bid;
            LastPrice=0;
            
            if (TakeProfit==0) { tp=0; }
            else { tp=SellPrice-TakeProfit*Point; }
            if (InitialStop==0) { sl=0; }
            else { sl=SellPrice+InitialStop*Point; }
            
            if (OpenOrders!=0) {
               mylotsi=lotsi; 
               if (MaxTrades>12) mylotsi=NormalizeDouble(lotsi*MathPow(1.5,OpenOrders),2);
               else mylotsi=NormalizeDouble(lotsi*MathPow(2,OpenOrders),2);
            } else { mylotsi=lotsi; } //OpenOrders==0
            
            if (mylotsi>100) { mylotsi=100; }
            if (mylotsi>maxLot) mylotsi=maxLot;
            mylotsi-=MathMod(mylotsi,MarketInfo(Symbol(),MODE_LOTSTEP));// fix after 0.01
            if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
            
            ticketno=OrderSend(Symbol(),OP_SELL,mylotsi,SellPrice,slippage,sl,tp,Name_Expert+Version,MagicNo[0],0,Red);
            if(ticketno == -1) { if (GetLastError()==131) Print("131 mylotsi=",mylotsi);}

            //else if (OpenOrders>=OrderstoProtect) ReboundSet(MagicNo[0]);//else {OrderComplete=1;}
            return(0);
         }
      }

      if (myOrderType==OP_BUY && ContinueOpening) {
         if ((LastPrice-Ask)>=Pips*Point || OpenOrders<1) {
            BuyPrice=Ask;
            LastPrice=0;
            
            if (TakeProfit==0) { tp=0; }
            else { tp=BuyPrice+TakeProfit*Point; } 
            if (InitialStop==0) { sl=0; }
            else { sl=BuyPrice-InitialStop*Point; }
            
            if (OpenOrders!=0) {
               mylotsi=lotsi;
               if (MaxTrades>12) mylotsi=NormalizeDouble(lotsi*MathPow(1.5,OpenOrders),2);
               else mylotsi=NormalizeDouble(lotsi*MathPow(2,OpenOrders),2);
            } else { mylotsi=lotsi; } //OpenOrders==0

            if (mylotsi>100) { mylotsi=100; }
            if (mylotsi>maxLot) mylotsi=maxLot;
            mylotsi-=MathMod(mylotsi,MarketInfo(Symbol(),MODE_LOTSTEP));// fix after 0.01
            
            if(AccountFreeMarginCheck(XSymbol,OP_BUY,mylotsi)<=0 || GetLastError()==134) return;
            
            ticketno=OrderSend(Symbol(),OP_BUY,mylotsi,BuyPrice,slippage,sl,tp,Name_Expert+Version,MagicNo[0],0,Blue);
            if(ticketno == -1) { if (GetLastError()==131) Print("131 mylotsi=",mylotsi);}
            
            //else if (OpenOrders>=OrderstoProtect) ReboundSet(MagicNo[0]);//else {OrderComplete=1;}
            return(0);
         }
      }

//----
return(0);
}

void CloseAll(string SymbolToClose,int MagicNo) //at Symbol
{
int cnt;
int CloseNo;

   CloseNo=OrdersTotal();
   if(CloseNo==0) return(0);
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--) {//last to frist
      OrderSelect(cnt,SELECT_BY_POS, MODE_TRADES);
      if((SymbolToClose=="All") || 
         (OrderSymbol()==SymbolToClose && MagicNo == 0) ||
         (OrderSymbol()==SymbolToClose && OrderMagicNumber() == MagicNo)) {
         
             if(OrderType()==OP_BUY)
               OrderClose(OrderTicket(),OrderLots(),Bid,slippage,ArrowsColor);
             if(OrderType()==OP_SELL)
               OrderClose(OrderTicket(),OrderLots(),Ask,slippage,ArrowsColor);
         }
      }
   CloseNo=CloseNo-OrdersTotal();   //CloseNo-=OrdersTotal();   
   if(!IsTesting()) Print(CloseNo," Orders Closed");
}

void ReboundSet(int MagicNo)
{// if (OpenOrders>=OrderstoProtect) ReboundSet();

bool result;
int cnt,err;
double tpx,slx;

      Sleep(1000);//---- wait for 1 seconds
      RefreshRates();
      
     //Rebound Modify Set all Order at same sl tp
 if(myOrderType==OP_BUY || myOrderType==OP_SELL) {
   //count=OrdersTotal()-1;  //while(count>=0) { // while always delay much! do not use
   for(cnt=OrdersTotal();cnt>=0;cnt--) {
     if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)==false) break;
	  if(OrderSymbol() == XSymbol && OrderMagicNumber() == MagicNo) // && Reversed==False) 
	    {  
	    if (myOrderType==OP_BUY) tpx= OrderOpenPrice()+(SecureProfit*Point);
	    //slx = OrderOpenPrice()-TrailingStop*Point;
	    if (myOrderType==OP_SELL) tpx= OrderOpenPrice()-(SecureProfit*Point);
	    
	    //if (OrderOpenPrice()-Ask>=(TrailingStop+Pips)*Point) && (OrderStopLoss()>(Ask+Point*TrailingStop)) {
       //slx = OrderOpenPrice()+TrailingStop*Point;}
                  
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

   }//for
}

}
//+------------------------------------------------------------------+