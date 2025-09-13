
//+------------------------------------------------------------------+
//|                                  RandomT.mq4
//|                        Copyright 2010, K Lam
//|                                        http://FxKillU.com
//+------------------------------------------------------------------+
// This is for EURUSD H1 at PRange 5~4 NextMin=2 DollarCut=90~100(1%) StopLoss=280~320

// This is for GBPUSD H1 at spread 10(4) minlot=0.01 Risk Control can get more
// This is for USDCHF H1 at spread 12(4) minlot=0.01 Risk Control can get more
// This is for EURAUD H1 at spread 12(4) minlot=0.01 Risk Control can get more

#property copyright "Copyright 2010, K Lam"
#property link      "http://FxKillU.com"
#define MagicNo 20070424
#define Version  20100730
#define NA 99

extern string Name_Expert= "RandomT";
extern string OWN="Copyright 2010, K LAM";

extern int BarWatch=12;
extern int Tshift=2;

extern bool TrailingProfit = true; //false; //
//extern int StopLoss=280;//450; //700; //50 fail 70 should be set at daily high low!
extern bool AutoStopLevel  = false; //true; //AutoStopLevel on StopLevel = Auto true; //
extern int StartStopLevel  = 4; //12 18 normal high low Range! 4 is min suggect at 8~10  
extern double MinProfit=0.4; //0.3 double
extern int StopLevel=10;


int Z1,Z2,F1,F2,a1,b1;
string text1;
string text2;
string text3;
string text4;
string text5;

int init()
{
   text1="\n";
   text2="\n";
   text3="\n";
   text4="\n";
   text5="\n";

}

int start()
{     

int op;
int t=0;
int i;
double Lot;
double xLow,xHigh;
int ticketno;
   
   text1="\n  XSymbol= "+Symbol();
   
   //cal for display   
   LoadIndicatorVal();
   
   //display Comment
   Comment(text1,text2,text3,text4,text5);
   
   //TrailingStop
   if(TrailingProfit) TrailingStop(MagicNo);
   
   Lot =NormalizeDouble(AccountFreeMargin()/100000.0,1);
//   CheckOrderToClose();   
   // Order Sig Show BuySellSig()
   if(BuySellSig()==OP_BUY && (CheckOrder()==99 || CheckOrder()==OP_SELL)) {
      CloseAll(Symbol(),MagicNo); //OrderClose(OrderTicket(),OrderLots(),Ask,7,White);
      ticketno= OrderSend(Symbol(), OP_BUY, Lot, Ask, 5,0, 0, "", MagicNo, 0, Green);
      if(ticketno == -1) {//error check
         if(GetLastError()==130)
            Print("error 130 OP_BUY Ask =",Ask);
         }
      }
      
   if(BuySellSig()==OP_SELL && (CheckOrder()==99 || CheckOrder()==OP_BUY)) {
      CloseAll(Symbol(),MagicNo); //OrderClose(OrderTicket(),OrderLots(),Bid,7,White);
      ticketno= OrderSend(Symbol(), OP_SELL, Lot, Bid, 5, 0,0, "", MagicNo, 0, Red);
      if(ticketno == -1) {//error check
         if(GetLastError()==130)
            Print("error 130 OP_BUY Bid =",Bid);
         }      
      }

    return (0);
}

//+------------------------------------------------------------------+ 
// Function

double GetSlippage(string XSymbol)
{
   double bid   =MarketInfo(XSymbol,MODE_BID);
   double ask   =MarketInfo(XSymbol,MODE_ASK);
   double point =MarketInfo(XSymbol,MODE_POINT);
   return((ask-bid)/point);
}

void LoadIndicatorVal()
{
string MoreX;

      Z1=iCustom(NULL, 0,"ZigZag",BarWatch,5,3,1,Tshift)/Point;//HighMapBuffer
      Z2=iCustom(NULL, 0,"ZigZag",BarWatch,5,3,2,Tshift)/Point;//LowMapBuffer
      
      F1=iFractals(NULL, 0, MODE_UPPER, Tshift)/Point;
      F2=iFractals(NULL, 0, MODE_LOWER, Tshift)/Point;
      
      a1=iMACD(NULL,0, 12,26,9,PRICE_CLOSE, MODE_MAIN,Tshift)/Point;
      b1=iMACD(NULL,0, 12,26,9,PRICE_CLOSE, MODE_SIGNAL,Tshift)/Point;
      
      if(a1>b1) MoreX=" >>>>";
      if(a1<b1) MoreX=" <<<<";
      
      text2="\n  Z1="+DoubleToStr(NormalizeDouble(Z1,0),0)
      +"  Z2="+DoubleToStr(NormalizeDouble(Z2,0),0)
      +"  F1="+DoubleToStr(NormalizeDouble(F1,0),0)
      +"  F2="+DoubleToStr(NormalizeDouble(F2,0),0)
      +"  a1="+DoubleToStr(NormalizeDouble(a1,0),0)
      +MoreX
      +" b1="+DoubleToStr(NormalizeDouble(b1,0),0);
      
}

int CheckOrder()
{
   int i;
   int ShowOrder;
   for(i=0;i<OrdersTotal(); i++) {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) continue;
      if(OrderSymbol()!=Symbol()) continue;
      if(OrderMagicNumber()!=MagicNo) continue;
      ShowOrder=OrderType();
      if(ShowOrder!=OP_BUY && ShowOrder!=OP_SELL) continue;
      else return(ShowOrder);
      }
   return(99);//no order 
}

int BuySellSig()
{
   if (Z1==F1 && F1!=0 && a1>b1) {
      text3="\n  Sig=OP_SELL";
      return(OP_SELL);
      }
      
   if (Z2==F2 && F2!=0 && a1<b1) {
      text3="\n  Sig=OP_BUY";
      return(OP_BUY);
      }
      
   text3="\n";
   return(99);
}

void CloseAll(string SymbolToClose,int MagNo)
{//at Symbol
   bool result;
   int cmd,error;
   int cnt,retry;
   int CloseNo;
   double sp,price;
   string XSymbol;
   double LastBalance,LastEquity,LastProfit;
   
   LastBalance=AccountBalance()+AccountCredit();
   LastEquity=AccountEquity();
   LastProfit=AccountProfit();

   for (cnt=10;IsTradeContextBusy();cnt--) {
   cnt+=2;
   Sleep(10);
    //Print("Trade context is busy. Please wait");
   }
    
   CloseNo=OrdersTotal();
   if(CloseNo==0) return(0);

   for(cnt=OrdersTotal();cnt > 0;cnt--) {//last to frist -1
      if(OrderSelect(cnt-1,SELECT_BY_POS, MODE_TRADES)==true) {
      
      for(retry=0;retry < 10;retry++) {
         if((SymbolToClose=="All") || 
         (OrderSymbol()==SymbolToClose && MagNo == 0) ||
         (OrderSymbol()==SymbolToClose && OrderMagicNumber() == MagNo)) {
         
            cmd=OrderType();
            //RefreshRates();
            XSymbol = OrderSymbol();
            sp = GetSlippage(XSymbol);
            
            if(cmd==OP_BUY) price=MarketInfo(XSymbol,MODE_BID);// Ask;
            if(cmd==OP_SELL) price=MarketInfo(XSymbol,MODE_ASK);// Bid; // not the chart price!!
            
            if(cmd==OP_BUY || cmd==OP_SELL) {
               result=OrderClose(OrderTicket(),OrderLots(),price,sp,CLR_NONE);//CLR_NONEArrowsColor
               if(!result) {
                  error=GetLastError();
                  Print("LastError = ",error," price=",price," Slippage=",sp);
                  } else break;
               if(error==129 || error==135 || error==146) Sleep(100);// RefreshRates(); 138
            }
            
            if(cmd!=OP_BUY && cmd!=OP_SELL) {
               result=OrderDelete(OrderTicket());
               }
         }//if
      } //for retry
      } else Print( "Error when order select ", GetLastError());//OrderSelect
   }//for
      
   CloseNo=CloseNo-OrdersTotal();   //CloseNo-=OrdersTotal();   
   if(!IsTesting()) Print(CloseNo," Closed, ",//SymbolToClose,"=",MagNo,
   " B4 Profit=",LastProfit," B4 Bal=", LastBalance," B4 Eq=",LastEquity,
   " Equity: ",AccountEquity()," Balance: ", AccountBalance()+AccountCredit());
   //" B4 Balance: ", LastBalance," B4 Equity: ",LastEquity," B4 Last Profit ",LastProfit," Equity: ",AccountEquity();   
}

bool TrailingStop(int MagicKey)      //¸òÂÜ¤îÄ¹
{
   bool  result=true;
   int cnt,err,ReOrderType;
   int TOrderOpenPrice,TCurrentStopLoss,TTargetStopLoss=0;
   double CurrentStopLoss,TargetStopLoss=0;
   double Calprofit;
   
   //Modify Set all Order at same sl tp TrailingStop
   for(cnt=OrdersTotal()-1;cnt>=0;cnt--) {//while(count>=0) { // while always delay much! do not use
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)==false) continue;
      if(OrderSymbol() ==Symbol() && OrderMagicNumber() == MagicKey) {
         ReOrderType=OrderType();
         if(ReOrderType!=OP_BUY && ReOrderType!=OP_SELL) continue; //step out
         //if(OrderComment()==MarkLock) continue; //step out
         
         Calprofit=OrderProfit()+OrderCommission()+OrderSwap();
         if(Calprofit > MinProfit) {// make sure you in profit!  //CurrentStopLoss = OrderStopLoss();
            CurrentStopLoss = NormalizeDouble(OrderStopLoss(), Digits);//should be error,when ==0
            TCurrentStopLoss = NormalizeDouble(CurrentStopLoss/Point, 0);
            TOrderOpenPrice =  NormalizeDouble(OrderOpenPrice()/Point, 0);
            text4 = "\n";
            
            if(ReOrderType==OP_BUY) {//TargetStopLoss = Bid -StopLevel*Point;
               TargetStopLoss = NormalizeDouble(Bid -StopLevel*Point, Digits);
               TTargetStopLoss= NormalizeDouble(TargetStopLoss/Point,0);
               
               if(TCurrentStopLoss == TTargetStopLoss) continue; //step out
               if((TCurrentStopLoss < TTargetStopLoss && TOrderOpenPrice < TTargetStopLoss) || (TCurrentStopLoss == 0)) {
               //need add mini point get profit!
               
                  result=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TargetStopLoss, Digits)
                  ,OrderTakeProfit(),0,Tomato);
                  
                  text4 = "\n OP_BUY Order Ticket="+DoubleToStr(OrderTicket(),0)
                  +" modified. CurrentStopLoss="+DoubleToStr(TCurrentStopLoss,0)
                  +" to  TargetStopLoss="+DoubleToStr(TTargetStopLoss,0);
                  }
               }
               
            if(ReOrderType==OP_SELL) { //TargetStopLoss = Ask +StopLevel*Point;
               TargetStopLoss = NormalizeDouble(Ask +StopLevel*Point, Digits);
               TTargetStopLoss= NormalizeDouble(TargetStopLoss/Point,0);
               
               if(TCurrentStopLoss == TTargetStopLoss) continue; //step out
               if((TCurrentStopLoss > TTargetStopLoss && TOrderOpenPrice > TTargetStopLoss) || (TCurrentStopLoss == 0)) {
               
                  result=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(TargetStopLoss, Digits)
                  ,OrderTakeProfit(),0,Tomato);
                  
                  text4 = "\n OP_SELL Order Ticket="+DoubleToStr(OrderTicket(),0)
                  +" modified. CurrentStopLoss="+DoubleToStr(TCurrentStopLoss,0)
                  +" to  TargetStopLoss="+DoubleToStr(TTargetStopLoss,0);
                  }
               }
            
            if(!result) {
               err=GetLastError();            
               Print("LastError = ",err," StopLoss=",TargetStopLoss);//return(false);
               if(err==130) StopLevel++;
                //130 Invalid stops. //136 Off quotes. // 135 Price changed.
               if(StopLevel > 25) Print("StopLevel Error = ",err," StopLevel > 25, DO NOT RUN at ",Symbol() ,"Pair... StopLoss=",TargetStopLoss);//return(false);
               }

            //else Print("Ticket ",OrderTicket()," modified. TargetStopLoss=",TargetStopLoss);            //   else OrderPrint();

         }//Calprofit > MinProfit)
      }//OrderMagicNumber() == MagicNo
   }//for(

   return(true);
}

//+------------------------------------------------------------------------------------+