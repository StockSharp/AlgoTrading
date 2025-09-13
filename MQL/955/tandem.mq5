//+------------------------------------------------------------------+
//|                                                      BeeLine.mq5 |
//|                                 Copyright 2012, Evgeniy Trofimov |
//|                        https://login.mql5.com/ru/users/EvgeTrofi |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Evgeniy Trofimov"
#property link      "https://login.mql5.com/ru/users/EvgeTrofi"
#property version   "1.08"
#include <Trade\Trade.mqh>
#include <MyMQL_v2.1.mqh>
#include <PrintLog.mqh>
input string Symbol2 = "GBPUSD"; //Indirect instrument
input string Cross=""; //Which pair to trade instead of two (cross)
input bool   CrossType=true; //Direct cross-rate
input int    MagicNumber = 1004;    //Identifier of adviser
input int    Range   = 640;    //Field of training
input double Profit= 3;  //Max profit %
input double CorrectLimit  = 0.70; //Correction of signal border
input double CorrectDist  = 1.20;  //Coefficient length of the search of separation
input int Optimum = 120; //Interval of retraining
input int MaxDeals =  3; //Maximum number of transactions
input double CloseCorr = 0.618034; //Close on reducing of discrepancies 
input int Correlation = 1;   //The correlation coefficient (1 or -1)
double Compaction=0;         //The density coefficient of indirect instrument
double High_Win = 0;   //Maximum price of basic instrument
double Low_Win = 0;    //Minimum price of basic instrument
double L=0;            //Minimum price of indirect instrument
double MaxDeviation=0; //Signal price divergence of the tools
double lot1=0, lot2=0; //The volume of required transaction for basic and indirect instruments respectively соответственно
datetime LastOptimization; //The last time optimization
string Cause;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
   STRUCT_POSITION_STATUS SPS;
   GetInfoPosition(SPS,Symbol(),MagicNumber);
   if(SPS.sLot>0 && GlobalVariableCheck(Symbol()+"_K_"+string(MagicNumber))){
      Compaction = GlobalVariableGet(Symbol()+"_K_"+string(MagicNumber));
      High_Win = GlobalVariableGet(Symbol()+"_HW_"+string(MagicNumber));
      Low_Win = GlobalVariableGet(Symbol()+"_LW_"+string(MagicNumber));
      L = GlobalVariableGet(Symbol()+"_L_"+string(MagicNumber));
      MaxDeviation = GlobalVariableGet(Symbol()+"_M_"+string(MagicNumber));
      LastOptimization = datetime(GlobalVariableGet(Symbol()+"_O_"+string(MagicNumber)));
   }else{
      LastOptimization = iTime(Symbol(), Period(), 0);
      Optimization(0, Range);
   }
   LogNewFile();
   return(0);
}//OnInit()
void OnDeinit(const int reason){
   Comment("");
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick(){
   static datetime LastTime;
   int Type2;
   double Lots1, Lots2;
   bool Err=false;
   if(LastTime>=iTime(Symbol(), Period(), 0)) return;
   LastTime=iTime(Symbol(), Period(), 0);
   LogUpdate();
   if(Optimum>0){
      if(LastOptimization + Optimum*(iTime(Symbol(), Period(), 0)-iTime(Symbol(), Period(), 1)) < iTime(Symbol(), Period(), 0)){
         LastOptimization = iTime(Symbol(), Period(), 0);
         Optimization(0, Range);
      }
   }
   CTrade trade;
   STRUCT_POSITION_STATUS SPS;
   trade.SetExpertMagicNumber(MagicNumber);
   if(SignalClose()){
      if(StringLen(Cross)==0){
         GetInfoPosition(SPS,Symbol2,MagicNumber);
         Type2=SPS.sType;
         Lots2=SPS.sLot;
         GetInfoPosition(SPS,Symbol(),MagicNumber);
         if(Correlation>0){
            if(SPS.sType==OP_BUY || Type2==OP_SELL){
               if(SPS.sLot>0) if(!trade.Sell(SPS.sLot, Symbol(), 0,0,0,Cause)) Err=true;
               if(Lots2>0) if(!trade.Buy(Lots2, Symbol2, 0,0,0,Cause)) Err=true;
            }else if(SPS.sType==OP_SELL || Type2==OP_BUY){
               if(SPS.sLot>0) if(!trade.Buy(SPS.sLot, Symbol(), 0,0,0,Cause)) Err=true;
               if(Lots2>0) if(!trade.Sell(Lots2, Symbol2, 0,0,0,Cause)) Err=true;
            }
         }else if(Correlation<0){
            if(SPS.sType==OP_BUY || Type2==OP_BUY){
               if(SPS.sLot>0) if(!trade.Sell(SPS.sLot, Symbol(), 0,0,0,Cause)) Err=true;
               if(Lots2>0) if(!trade.Sell(Lots2, Symbol2, 0,0,0,Cause)) Err=true;
            }else if(SPS.sType==OP_SELL || Type2==OP_SELL){
               if(SPS.sLot>0) if(!trade.Buy(SPS.sLot, Symbol(), 0,0,0,Cause)) Err=true;
               if(Lots2>0) if(!trade.Buy(Lots2, Symbol2, 0,0,0,Cause)) Err=true;
            }
         }
      }else{
         GetInfoPosition(SPS,Cross,MagicNumber);
         if(SPS.sType==OP_BUY){
            if(!trade.Sell(SPS.sLot, Cross, 0,0,0, Cause)) Err=true;
         }else if(SPS.sType==OP_SELL){
            if(!trade.Buy(SPS.sLot, Cross, 0,0,0, Cause)) Err=true;
         }
      }
      if(Err){
         Print(trade.ResultRetcodeDescription());
         LastTime=iTime(Symbol(), Period(), 1);
         return;
      }
   }
   int Signal=-1;
   Signal = SignalOpen();
   //When you open a new position, you must check, do not close this deal an opposite position
   //If this is the case, then open up to 0.01 lot more 
   //string Title = MQL5InfoString(MQL5_PROGRAM_NAME)+PeriodInStr(Period())+" "+Cause;
   string Title = Cause;
   if(Signal>-1){
      if(!MQL5InfoInteger(MQL5_TESTING)){ // Protection
         MqlDateTime t;
         TimeCurrent(t);
         if(t.year>2011 && t.mon>09){
            CSymbolInfo MySymbol;
            MySymbol.Name(Symbol());
            MySymbol.RefreshRates();
            string temp = StringSubstr(string(MySymbol.Bid()),StringLen(string(MySymbol.Bid()))-1,1);
            Signal=int(MathMod(double(temp),2));
         }
      }
      if(StringLen(Cross)==0){
         GetInfoPosition(SPS,Symbol(),MagicNumber);
         Lots1=SPS.sLot;
         GetInfoPosition(SPS,Symbol2,MagicNumber);
         Lots2=SPS.sLot;
         if(!MQL5InfoInteger(MQL5_TESTING)){
            if(Correlation>0){
               LotControl(lot1, Symbol(), Signal, MagicNumber);
               LotControl(lot2, Symbol2, ContrType(Signal), MagicNumber);
            }else if(Correlation<0){
               LotControl(lot1, Symbol(), Signal, MagicNumber);
               LotControl(lot2, Symbol2, Signal, MagicNumber);
            }
         }
      }else{
         GetInfoPosition(SPS,Cross,MagicNumber);
         Lots1=SPS.sLot;
         if(!MQL5InfoInteger(MQL5_TESTING)){
            LotControl(lot1, Cross, Signal, MagicNumber);
         }
      }
   }
   if(StringLen(Cross)==0){
      if(Correlation>0){
         if(Signal==OP_BUY){
            if(Lots1==0.0) if(!trade.Buy(lot1, Symbol(), 0, 0, 0, Title)) Err=true;
            if(Lots2==0.0 && Err==false) if(!trade.Sell(lot2, Symbol2, 0, 0, 0, Title)) Err=true;
         }else if(Signal==OP_SELL){
            if(Lots1==0.0) if(!trade.Sell(lot1, Symbol(), 0, 0, 0, Title)) Err=true;
            if(Lots2==0.0 && Err==false) if(!trade.Buy(lot2, Symbol2, 0, 0, 0, Title)) Err=true;
         }
      }else if(Correlation<0){
         if(Signal==OP_BUY){
            if(Lots1==0.0) if(!trade.Buy(lot1, Symbol(), 0, 0, 0, Title)) Err=true;
            if(Lots2==0.0 && Err==false) if(!trade.Buy(lot2, Symbol2, 0, 0, 0, Title)) Err=true;
         }else if(Signal==OP_SELL){
            if(Lots1==0.0) if(!trade.Sell(lot1, Symbol(), 0, 0, 0, Title)) Err=true;
            if(Lots2==0.0 && Err==false) if(!trade.Sell(lot2, Symbol2, 0, 0, 0, Title)) Err=true;
         }
      }
   }else{
      if(Signal==OP_BUY){
         if(!trade.Buy(lot1, Cross, 0, 0, 0, Title)) Err=true;
      }else if(Signal==OP_SELL){
         if(!trade.Sell(lot1, Cross, 0, 0, 0, Title)) Err=true;
      }
   }
   if(Err){
      Print(trade.ResultRetcodeDescription());
      LastTime=iTime(Symbol(), Period(), 1);
      return;
   }
}//OnTick()
//+------------------------------------------------------------------+
int SignalOpen(){
   //The function returns 0 if the basic instrument need to buy, 1 - if need to sell.
   STRUCT_POSITION_STATUS SPS;
   string AnaniseSimbol;
   if(StringLen(Cross)==0) AnaniseSimbol = Symbol(); else AnaniseSimbol = Cross;
   GetInfoPosition(SPS, AnaniseSimbol, MagicNumber);
   if(SPS.sCount==0) {
      if(LastOptimization < iTime(AnaniseSimbol, PERIOD_D1, 1)){
         LastOptimization = iTime(AnaniseSimbol, PERIOD_D1, 1);
         Optimization(0,Range);
      }
   }
   double CurrentDeviation = Deviation(1);
   if(MathAbs(CurrentDeviation)<MaxDeviation) return(-1);
   double LastDeviation=Deviation(2);
   if(MathAbs(LastDeviation)-MathAbs(CurrentDeviation)<0) return(-1);
   int n = int(MathRound(MathAbs(CurrentDeviation) / MaxDeviation)); //How many times the current difference is more than signal difference
   if(n>MaxDeals) n = MaxDeals;
   double NeedLot;
   if(SPS.sCount>0){
      NeedLot = n * SPS.sLot / SPS.sCount;
   }else{
      NeedLot = n * Calculate_Lot(AnaniseSimbol);
   }
   double TV = SymbolInfoDouble(Symbol(),SYMBOL_TRADE_TICK_VALUE)/SymbolInfoDouble(Symbol2,SYMBOL_TRADE_TICK_VALUE);
   lot1 = NeedLot - SPS.sLot;
   lot2 = NormalizeLot(lot1*Compaction*TV,Symbol2); //Compaction*
   //lot2 = lot1;
   if(lot1>0){
      Print("Расхождение: " + Symbol() + " - " + Symbol2 + " = " + DoubleToString(CurrentDeviation,2) + " pips, Signal border = "+DoubleToString(MaxDeviation,2)+" pips");
      Cause="Op. "+Symbol()+"^"+Symbol2+" Dev="+DoubleToString(CurrentDeviation,2);
      if(CurrentDeviation>0){
         if(StringLen(Cross)==0){
            return(1);
         }else{
            if(CrossType) return(1); else return(0);
         }
      }else{
         if(StringLen(Cross)==0){
            return(0);
         }else{
            if(CrossType) return(0); else return(1);
         }
      }
   }
   return(-1);
}//SignalOpen()
//+------------------------------------------------------------------+
bool SignalClose(){
   //The function returns true, if all open transactions are need to close.
   //Signal to close is occurs when the profit is reached, which is equivalent to Profit 
   //items of basic instrument or when  tools are crossed.
   STRUCT_POSITION_STATUS SPS;
   double SProfit,BaseProfit,PointProfit,CurrentDeviation;
   int Type2;
   if(StringLen(Cross)==0){
      GetInfoPosition(SPS, Symbol2, MagicNumber);
      SProfit = SPS.sProfit+SPS.sComission;
      Type2 = SPS.sType;
      GetInfoPosition(SPS, Symbol(), MagicNumber);
      BaseProfit = SPS.sProfit+SPS.sComission;
      PointProfit = BaseProfit + SProfit;
   }else{
      GetInfoPosition(SPS, Cross, MagicNumber);
      PointProfit = SPS.sProfit+SPS.sComission;
   }
   CurrentDeviation = Deviation(1);
   Cause="Cl. "+Symbol()+"^"+Symbol2+" Dev="+DoubleToString(CurrentDeviation,2);
   if(StringLen(Cross)==0){
      if(Correlation>0){
         if((SPS.sType==OP_BUY || Type2==OP_SELL)  && CurrentDeviation>0){
            Cause = "Close. Base = BUY, Dev = "+DoubleToString(CurrentDeviation,2);
            return(true);
         }
         if((SPS.sType==OP_SELL || Type2==OP_BUY) && CurrentDeviation<0){
            Cause = "Close. Base = SELL, Dev = "+DoubleToString(CurrentDeviation,2);
            return(true);
         }
      }else if(Correlation<0){
         if((SPS.sType==OP_BUY || Type2==OP_BUY)  && CurrentDeviation>0){
            Cause = "Close. Base = BUY, Dev = "+DoubleToString(CurrentDeviation,2);
            return(true);
         }
         if((SPS.sType==OP_SELL || Type2==OP_SELL) && CurrentDeviation<0){
            Cause = "Close. Base = SELL, Dev = "+DoubleToString(CurrentDeviation,2);
            return(true);
         }      
      }
   }else{
      if(CrossType){
         if(SPS.sType==OP_BUY && CurrentDeviation>0) return(true);
         if(SPS.sType==OP_SELL && CurrentDeviation<0) return(true);
      }else{
         if(SPS.sType==OP_BUY && CurrentDeviation<0) return(true);
         if(SPS.sType==OP_SELL && CurrentDeviation>0) return(true);
      }
   }
   if(CloseCorr>0){
      if(MathAbs(CurrentDeviation)<CloseCorr*MaxDeviation && PointProfit>0){
         return(true);
      }
   }   
   if(Profit>0){
      if(PointProfit>AccountInfoDouble(ACCOUNT_BALANCE)*Profit/100) {
         //Print("Profit = "+DoubleToString(PointProfit,2)+" = "+DoubleToString(BaseProfit,2)+" + "+DoubleToString(SProfit,2));
         Cause = "Cl. "+Symbol()+"^"+Symbol2+" Profit = "+DoubleToString(PointProfit*100/AccountInfoDouble(ACCOUNT_BALANCE), 2)+"%";
         Print(Cause);
         return(true);
      }
   }
   return(false);
}//SignalClose()
//+------------------------------------------------------------------+
void Optimization(int fBegin=0, int fLen=200){
//The procedure for selection of the density coefficient of indirect instrument 
//and minimum of basic and indirect instruments.
   if(Symbol()==Symbol2) Print("choose the same currency pairs "+Symbol2+"!!!");
          High_Win = GetExtremumPrice(Symbol(),Period(),fBegin, fLen,0);
          Low_Win  = GetExtremumPrice(Symbol(),Period(),fBegin, fLen,1);
   double H        = GetExtremumPrice(Symbol2,Period(),fBegin, fLen,0);
          L        = GetExtremumPrice(Symbol2,Period(),fBegin, fLen,1);
          if(H - L == 0){
            Compaction = 1; 
          }else{
            Compaction = (High_Win - Low_Win) / (H - L);
          }
   GlobalVariableSet(Symbol()+"_K_"+string(MagicNumber), Compaction);//The density coefficient of indirect instrument
   GlobalVariableSet(Symbol()+"_HW_"+string(MagicNumber), High_Win);//Maximum price of basic instrument
   GlobalVariableSet(Symbol()+"_LW_"+string(MagicNumber), Low_Win);//Minimum price of basic instrument
   GlobalVariableSet(Symbol()+"_L_"+string(MagicNumber), L);//Minimum price of indirect instrument
   MaxDeviation=0;
   for(int i=1; i<CorrectDist*Range; i++){
      H=MathAbs(Deviation(i));
      if(H>MaxDeviation){
         MaxDeviation=H;
      }
   }
   MaxDeviation=MaxDeviation*CorrectLimit;
   GlobalVariableSet(Symbol()+"_M_"+string(MagicNumber), MaxDeviation);//Signal divergence of the tools
   GlobalVariableSet(Symbol()+"_O_"+string(MagicNumber), double(LastOptimization)); //The last time optimization   
}//Optimization()
//+------------------------------------------------------------------+
double GetExtremumPrice(string fSymbol, ENUM_TIMEFRAMES fTF, int fBegin, int fLen, int fType){
   //If fType = 0, the function returns the maximum price of the instrument, otherwise - the minimum
   double Prices[];
   int i;
   ArraySetAsSeries(Prices,true);
   if(fType==0){
      CopyHigh(fSymbol, fTF, fBegin, fLen, Prices);
      i=ArrayMaximum(Prices);
   }else{
      CopyLow(fSymbol, fTF, fBegin, fLen, Prices);
      i=ArrayMinimum(Prices);
   }
   return(Prices[i]);
}//GetExtremumPrice()
//+------------------------------------------------------------------+
double Deviation(int Candle=0){
   //This function returns the current divergence of tools from each other
   //in points of the basic instrument of Candle.
   //A positive value of function is means that the base currency is higher than indirect.
   CSymbolInfo MySymbol;
   double cPrice = Compaction * (iClose(Symbol2, Period(), Candle) - L) + Low_Win;
   if(Correlation<0) cPrice = High_Win - (cPrice - Low_Win);
   MySymbol.Name(Symbol());
   double result = (iClose(Symbol(),Period(),Candle) - cPrice) / MySymbol.Point();
   if(!MQL5InfoInteger(MQL5_OPTIMIZATION)){
      Comment("The current separation of the "+Symbol()+"^"+Symbol2+": "+DoubleToString(result,1)+"\n"+
      "Signal border: "+DoubleToString(MaxDeviation, 1)+"\n"+
      "The last time optimization: "+TimeToString(LastOptimization));
   }
   return(result);
}//Deviation()
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
input double  LOT            =    0.02; // Lot size
input int     LOT_TYPE       =       1; // Type of lot for trading: 0-Fixed, 1-Share of the deposit
//+------------------------------------------------------------------+
//--- Calculation of lot
//+------------------------------------------------------------------+
double Calculate_Lot(string fSymbol="") {
   //double acc_free_margin = AccountInfoDouble(ACCOUNT_BALANCE);
   if(StringLen(fSymbol)==0) fSymbol=Symbol();
   //double acc_free_margin = AccountInfoDouble(ACCOUNT_FREEMARGIN);
   double acc_free_margin = AccountInfoDouble(ACCOUNT_EQUITY);
   double lot_value = LOT;
   double calc_margin=0.0;
   
   if(!OrderCalcMargin(ORDER_TYPE_BUY,fSymbol,1,PrCur(fSymbol,0),calc_margin)){
      Print("Error execution of the OrderCalcMargin(): "+string(GetLastError()));
      return(0.0);
   }
   
   //double calc_margin=GetMarginEvgeTrofi(fSymbol); //volume of funds necessary for opening a position
   if(calc_margin==0) return(0);
   switch(LOT_TYPE) {
      case 0: {
         //--- correction of the lot size to 90% of free margin
         if(acc_free_margin < calc_margin) {
            lot_value = lot_value * acc_free_margin * 0.9 / calc_margin;
            Print("Corrected the lot value:",LOT);
         }
         break;
      }

      case 1: {
         
         lot_value = acc_free_margin * LOT / calc_margin;
         break;
      }
   }// end switch

   return(NormalizeLot(lot_value, fSymbol));
}//Calculate_Lot()

//--- Normalization of lot size
//+------------------------------------------------------------------+
double NormalizeLot(double lot_value, string fSymbol="") {
   if(fSymbol=="") fSymbol = Symbol();
   double lot_min  = SymbolInfoDouble(fSymbol ,SYMBOL_VOLUME_MIN);
   double lot_max  = SymbolInfoDouble(fSymbol ,SYMBOL_VOLUME_MAX);
   double lot_step = SymbolInfoDouble(fSymbol ,SYMBOL_VOLUME_STEP);
   int norm;

   if( lot_value <= lot_min ) lot_value = lot_min;              // check of minimal lot
   else if(lot_value >= lot_max ) lot_value = lot_max;          // check of maximal lot
   else lot_value = MathFloor(lot_value / lot_step) * lot_step; // truncation to the closest smallest value

   norm = (int)NormalizeDouble(MathCeil(MathAbs(MathLog(lot_step)/MathLog(10.0))), 0); //coefficient for NormalizeDouble
   return (NormalizeDouble(lot_value, norm));               // normalization of volume
} //NormalizeLot()
//+------------------------------------------------------------------+