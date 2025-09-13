//+------------------------------------------------------------------+
//|                                                 MACFibo v1.1.mq4 |
//|                                                     Dinesh Yadav |
//|                                              dineshydv@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Dinesh Yadav"
#property link      "dineshydv@gmail.com"
// workcopy
// Based on MAC-Fibo trading system

#include <stderror.mqh>
#include <stdlib.mqh>

//#property indicator_chart_window
extern string Trade_Time="+++Trade Time Parameters+++";
extern int StartHour=0; // London open 7GMT;close 15GMT 
extern int EndHour=16;  // NY open=12GMT;close 20GMT 
extern bool FridayTrade=true; // Trade on Fridays
extern bool MondayTrade=true; // Trade on Mondays

extern string MA_Params="+++MA Parameters+++";
extern string MA_Mode_Types="0=SMA; 1=EMA; 2=SMMA; 3=LWMA;";
extern int Fast_MA=5;
extern int Fast_Mode=1;
//extern int MidMA=8;
extern int Slow_MA=20;
extern int Slow_Mode=0;
extern int Mid_MA=8;
extern int Mid_Mode=0;
extern string Trade_Params="+++Trade Parameters+++";
extern double Lots=0.1;
extern int Min_TP=10; // Minimum take profit
extern int Max_SL=50; // Max Stop loss
extern int MagicNum=12345;
extern int MaxOpnPos=10;
extern color Long_Entry_Clr=Blue;
extern color Shrt_Entry_Clr=Red;

extern bool Close_at_fastXmid=true;

double myPoint=1, slippage=3, mySlip, myMin_TP, myMax_SL;
int sft=0; 
double MA5_now, MA8_now, MA20_now, MA5_prv, MA8_prv, MA20_prv, MA5_aft, MA8_aft, MA20_aft;
double POINT_A, POINT_B, P_2000, P_1618, P_1250, P_0786, P_0618, P_0500, P_0382;
string TRADE;
int bars_snc_dn_strtd=0,bars_snc_up_strtd=0;
datetime barTime=0;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
   if(Digits==3||Digits==5) myPoint=10;
   mySlip=(slippage*myPoint);
   myMin_TP=((Min_TP*myPoint)*Point);
   myMax_SL=((Max_SL*myPoint)*Point);
   return(0);
}
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit() { return(0); }


//+------------------------------------------------------------------+
//| Check Trading Time function                                      |
//+------------------------------------------------------------------+
// TradeTime
bool TradeTime()
{
   bool TradeDay, TradeHour;

   // Check Monday Friday Trading
   if(DayOfWeek()==1 && MondayTrade==false) TradeDay=false; else
   if(DayOfWeek()==5 && FridayTrade==false) TradeDay=false;
   else TradeDay=true;   

   // Check Market Hour
   if( TimeHour(TimeCurrent()) >= StartHour && TimeHour(TimeCurrent()) < EndHour ) TradeHour=true; else TradeHour=false;
   // Trade condition
   if( TradeDay==true && TradeHour==true ) return(true); else return(false);
}  // End: TradeTime


//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{
if(!IsTradeAllowed()) { Print("Allow Live Trading First."); return(0); }
if(IsTradeContextBusy()) { Print("Trade context is busy. Try again"); return(0); }

   int OpenPosCnt=0, BUYPosCnt=0, SELLPosCnt=0;
   for(int cnt=0; cnt<OrdersTotal(); cnt++) 
   {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if ( OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNum ) 
      {
         if(OrderType()==OP_BUY) BUYPosCnt=BUYPosCnt+1;
         if(OrderType()==OP_SELL) SELLPosCnt=SELLPosCnt+1;
      }
   } 
   OpenPosCnt=BUYPosCnt+SELLPosCnt;


if(NewBar())
{
   MA5_now = iMA(NULL, 0, Fast_MA, 0, Fast_Mode, PRICE_CLOSE, sft+1); 
   MA5_prv = iMA(NULL, 0, Fast_MA, 0, Fast_Mode, PRICE_CLOSE, sft+2); 
   MA5_aft = iMA(NULL, 0, Fast_MA, 0, Fast_Mode, PRICE_CLOSE, sft); 

   MA8_now = iMA(NULL, 0, Mid_MA, 0, Mid_Mode, PRICE_CLOSE, sft+1); 
   MA8_prv = iMA(NULL, 0, Mid_MA, 0, Mid_Mode, PRICE_CLOSE, sft+2); 
   MA8_aft = iMA(NULL, 0, Mid_MA, 0, Mid_Mode, PRICE_CLOSE, sft); 

   MA20_now = iMA(NULL, 0, Slow_MA, 0, Slow_Mode, PRICE_CLOSE, sft+1); 
   MA20_prv = iMA(NULL, 0, Slow_MA, 0, Slow_Mode, PRICE_CLOSE, sft+2); 
   MA20_aft = iMA(NULL, 0, Slow_MA, 0, Slow_Mode, PRICE_CLOSE, sft); 
   
   POINT_AB();
   if( TradeTime()==true && OpenPosCnt<MaxOpnPos && TRADE != "NA" ) { Open_Pos(); }
   if( OpenPosCnt>0 && Close_at_fastXmid==true ) { Close_fastXmid(); }

}


   return(0);
} // END: start


//+------------------------------------------------------------------+
//| Check Open possibilities function                                |
//+------------------------------------------------------------------+
// POINT_AB
void POINT_AB()
{
   string POINT_A_BAR_OPN, POINT_B_BAR_OPN;

   // 5EMA cross above 20SMA UP
   if((MA5_now > MA20_now) && (MA5_prv < MA20_prv) && (MA5_aft > MA20_aft)) 
   { 
      TRADE="BUY";
      int low_bar_shft=iLowest(NULL,0,MODE_LOW,bars_snc_dn_strtd,sft+1);
      POINT_A=Close[sft+1]; // close of the bar which confirm MA cross
      POINT_B=Low[low_bar_shft];
      Print("BUY Trade. POINT_A: ",POINT_A," POINT_B: ", POINT_B);
Set_fibo();
      // Draw POINT_A, POINT_B and connecting line
      POINT_A_BAR_OPN=StringConcatenate( TimeHour(Time[sft+1]),":",TimeMinute(Time[sft+1]) );
      POINT_B_BAR_OPN=StringConcatenate( TimeHour(Time[low_bar_shft]),":",TimeMinute(Time[low_bar_shft]) );
      DrawObjects("MACFIBO-A "+POINT_A_BAR_OPN, sft+2, POINT_A, sft, POINT_A, Long_Entry_Clr, 3 );
      DrawObjects("MACFIBO-B "+POINT_B_BAR_OPN, low_bar_shft+1, POINT_B, low_bar_shft-1, POINT_B, Long_Entry_Clr, 3 );
      DrawObjects("MACFIBO-Line "+POINT_A_BAR_OPN+"-"+POINT_B_BAR_OPN, low_bar_shft, POINT_B, sft+1, POINT_A, Long_Entry_Clr, 3 );
   }
   // 5EMA cross below 20SMA DN
   else if ((MA5_now < MA20_now) && (MA5_prv > MA20_prv) && (MA5_aft < MA20_aft)) 
   { 
      TRADE="SELL";
      Print("MA Cross DN : bars_snc_up_strtd ", bars_snc_up_strtd);
      int High_bar_shft=iHighest(NULL,0,MODE_HIGH,bars_snc_up_strtd,sft+1);
      POINT_A=Close[sft+1]; // close of the bar which confirm MA cross
      POINT_B=High[High_bar_shft];
      Print("SELL Trade. POINT_A: ",POINT_A," POINT_B: ", POINT_B);
Set_fibo();
      // Draw POINT_A, POINT_B anc connecting line
      POINT_A_BAR_OPN=StringConcatenate( TimeHour(Time[sft+1]),":",TimeMinute(Time[sft+1]) );
      POINT_B_BAR_OPN=StringConcatenate( TimeHour(Time[High_bar_shft]),":",TimeMinute(Time[High_bar_shft]) );
      DrawObjects("MACFIBO-A "+POINT_A_BAR_OPN, sft+2, POINT_A, sft, POINT_A, Shrt_Entry_Clr, 3 );
      DrawObjects("MACFIBO-B "+POINT_B_BAR_OPN, High_bar_shft+1, POINT_B, High_bar_shft-1, POINT_B, Shrt_Entry_Clr, 3 );
      DrawObjects("MACFIBO-Line "+POINT_A_BAR_OPN+"-"+POINT_B_BAR_OPN, High_bar_shft, POINT_B, sft+1, POINT_A, Shrt_Entry_Clr, 3 );
   }
   else { TRADE="NA"; }

   if( MA5_now < MA20_now ) // Trend is DN
   { bars_snc_up_strtd=0; bars_snc_dn_strtd++; }
   else if( MA5_now > MA20_now ) // Trend is UP
   { bars_snc_up_strtd++; bars_snc_dn_strtd=0; }

}  // End: POINT_AB


//+------------------------------------------------------------------+
//| Set fibo levels function                                         |
//+------------------------------------------------------------------+
// Set_fibo
void Set_fibo()
{
//POINT_A is 0 level. POINT_B is 100 level.
double diff;

   if(TRADE=="BUY")
   {
      diff=(POINT_A-POINT_B);
      P_2000=POINT_B+diff*2; P_1618=POINT_B+diff*1.618; P_1250=POINT_B+diff*1.250; 
      P_0786=POINT_B+diff*0.786; P_0618=POINT_B+diff*0.618; P_0500=POINT_B+diff*0.50; 
      P_0382=POINT_B+diff*0.382;
   }

   if(TRADE=="SELL")
   {
      diff=(POINT_B-POINT_A);
      P_2000=POINT_B-diff*2; P_1618=POINT_B-diff*1.618; P_1250=POINT_B-diff*1.250; 
      P_0786=POINT_B-diff*0.786; P_0618=POINT_B-diff*0.618; P_0500=POINT_B-diff*0.50; 
      P_0382=POINT_B-diff*0.382;
   }
   Print("Fibo 200%=",P_2000," : 1.618%=",P_1618," : 0.786%=",
      P_0786," : 0.618%=",P_0618," : 0.500%=",P_0500," : 0.382%=",P_0382);


}  // End: Set_fibo


//+------------------------------------------------------------------+
//| Get TP function                                          |
//+------------------------------------------------------------------+
double get_TP()
{
double Day_high[], Day_low[], TP_NEW;
int prv_day_shft;
int cur_day_shft = iBarShift(NULL, PERIOD_D1, iTime(NULL, 0, 0));
double prv_day_high=iHigh( NULL, PERIOD_D1, cur_day_shft+1) ;
double prv_day_low=iLow( NULL, PERIOD_D1, cur_day_shft+1) ;


if(TRADE=="BUY")
{
   if(prv_day_high>P_1618) { TP_NEW=P_1618; } else 
   if(prv_day_high>P_1250) { TP_NEW=P_1250; }
   else if(prv_day_high>POINT_A) { TP_NEW=prv_day_high; }
   else { TP_NEW=P_1618; }
   TP_NEW=MathMax( (Ask+myMin_TP), TP_NEW);
   Print("New Take Profit: ",TP_NEW);
}
else if(TRADE=="SELL")
{
   if(prv_day_low<P_1618) { TP_NEW=P_1618; } else 
   if(prv_day_low<P_1250) { TP_NEW=P_1250; }
   else if(prv_day_low<POINT_A) { TP_NEW=prv_day_low; }
   else { TP_NEW=P_1618; }
   TP_NEW=MathMin( (Bid-myMin_TP), TP_NEW);
   Print("New Take Profit: ",TP_NEW);
}

return(TP_NEW);
} // END: get_TP()


//+------------------------------------------------------------------+
//| Get SL function                                          |
//+------------------------------------------------------------------+
double get_SL() 
{ 
double SL_NEW;
if(TRADE=="BUY")
{
   SL_NEW=MathMax( (Bid-myMax_SL), P_0382);
   Print("New Stop Loss: ",SL_NEW);
}
else if(TRADE=="SELL")
{
   SL_NEW=MathMin( (Ask+myMax_SL), P_0382);
   Print("New Stop Loss: ",SL_NEW);
}

return(SL_NEW); 
} // END: get_SL()

//+------------------------------------------------------------------+
//| Open positions function                                          |
//+------------------------------------------------------------------+
// Open_Pos
void Open_Pos()
{
   double SL=0, TP=0, price;
   int cmd;
   string cmt;
   color clr;

   if(AccountFreeMargin()<(1000*Lots))
   {  Print("We have no money. Free Margin = ", AccountFreeMargin());
      return(0);  
   }
   // Open long position
   if( TRADE=="BUY" ) 
   { 
      RefreshRates();
      cmd=OP_BUY; price=Ask; 
      SL=get_SL(); TP=get_TP(); 
      cmt=(DoubleToStr(POINT_A,5)+","+DoubleToStr(POINT_B,5) ); // "BUY"
      clr=Green;
      Print("Placing BUY at: ",price," SL: ", SL ," TP: ",TP );
   }
   // Open short position 
   else if( TRADE=="SELL" ) 
   { 
      RefreshRates();
      cmd=OP_SELL; price=Bid; 
      SL=get_SL(); TP=get_TP(); 
      cmt=(DoubleToStr(POINT_A,5)+","+DoubleToStr(POINT_B,5) ); // "SELL"
      clr=Red;
      Print("Placing SELL at: ",price," SL: ", SL ," TP: ",TP );
   }
   Print("Comment is: ",cmt);
   int ticket=OrderSend(Symbol(),cmd,Get_lot(),price,mySlip,SL,TP,cmt,MagicNum,0,clr);
   if(ticket<0) { Print("OrderSend Error #",GetLastError()," Desc: ",ErrorDescription(GetLastError()) ); }
   
}  // End: Open_Pos


//+------------------------------------------------------------------+
//| Find fastXmid MA function                                        |
//+------------------------------------------------------------------+
// Close_fastXmid
void Close_fastXmid()
{
   bool cmd_out; double price;
   string To_close;
   // 5EMA cross above 8SMA UP. Close all short positions
   if((MA5_now > MA8_now) && (MA5_prv < MA8_prv) && (MA5_aft > MA8_aft)) 
   { To_close="SELL"; Print("5EMA cross 8SMA up. Time to close short positions."); }
   // 5EMA cross below 8SMA DN. Close all long positions
   else if ((MA5_now < MA8_now) && (MA5_prv > MA8_prv) && (MA5_aft < MA8_aft)) 
   { To_close="BUY"; Print("5EMA cross 8SMA down. Time to close long positions."); }
   else { To_close="NA"; }

   for(int i = OrdersTotal()-1 ; i >= 0 ; i--)
   {
      OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderMagicNumber() == MagicNum && OrderSymbol()==Symbol())
      {
         if(To_close=="BUY" && OrderType()==OP_BUY && OrderProfit()<0 )
         {
            price=NormalizeDouble( MarketInfo(OrderSymbol(),MODE_BID), MarketInfo(OrderSymbol(),MODE_DIGITS));
            cmd_out=OrderClose(OrderTicket(),OrderLots(),price,mySlip,CLR_NONE);
            Print("Closing ",OrderSymbol()," BUY at ",price);
            if(!cmd_out) { Print("OrderClose BUY error #",GetLastError()," Desc: ",ErrorDescription(GetLastError()) ); }
         }
         if(To_close=="SELL" && OrderType()==OP_SELL && OrderProfit()<0 )
         {
            price=NormalizeDouble( MarketInfo(OrderSymbol(),MODE_ASK), MarketInfo(OrderSymbol(),MODE_DIGITS));
            cmd_out=OrderClose(OrderTicket(),OrderLots(),price,mySlip, CLR_NONE);
            Print("Closing ",OrderSymbol()," SELL at ",price);
            if(!cmd_out) { Print("OrderClose SELL error #",GetLastError()," Desc: ",ErrorDescription(GetLastError()) ); }
         }
      }
   }
   return(0);
}  // END: Close_fastXmid


//+------------------------------------------------------------------+
//| Check NewBar function                                            |
//+------------------------------------------------------------------+
// NewBar
bool NewBar()
{
   if( barTime < Time[0] ) 
   {  // we have a new bar opened
      barTime = Time[0];
      return(true);
   }
   return (false);
} // END: NewBar


void DrawObjects(string ObjName, int shft_bgn, double Price_Bgn, int shft_end, double Price_End, color clr, int wdth) 
{
   ObjectDelete(ObjName);
   ObjectCreate(ObjName, OBJ_TREND, 0, Time[shft_bgn], Price_Bgn, Time[shft_end], Price_End ); 
   ObjectSet(ObjName, OBJPROP_WIDTH, wdth);
   ObjectSet(ObjName,OBJPROP_RAY,false);
   ObjectSet(ObjName,OBJPROP_STYLE,STYLE_SOLID);
   ObjectSet(ObjName,OBJPROP_COLOR,clr);
}


//+------------------------------------------------------------------+
//| Get Lot function                                            |
//+------------------------------------------------------------------+
double Get_lot() 
{
   return(Lots);
} //Get_lot()


//+------------------------------------------------------------------+