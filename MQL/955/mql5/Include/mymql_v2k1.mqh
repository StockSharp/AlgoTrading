//+------------------------------------------------------------------+
//|                                                        MyMQL.mqh |
//|                                 Copyright 2012, Evgeniy Trofimov |
//|                        https://login.mql5.com/ru/users/EvgeTrofi |
//+------------------------------------------------------------------+
#include <Trade\SymbolInfo.mqh> 
struct STRUCT_POSITION_STATUS{
   string sSymbol;
   long sMagic;
   double sLot;
   int sType;
   int sCount;
   double sOpenPrice;
   double sOpenPriceLast;
   datetime sOpenTime;
   int sPoints;
   double sProfit;
   double sSwap;
   double sComission;
};

#define OP_BUY 0           //Buy 
#define OP_SELL 1          //Sell 
//+------------------------------------------------------------------+
 int iBarShift(string symbol, ENUM_TIMEFRAMES timeframe, datetime time, bool exact=false) {
   //Searching a bar by time. The function returns shift of the bar, 
   //to whom the specified time belongs. If for the special 
   //time the bar is missing (the "hole" in the story), the function returns, 
   //depending on the parameter "exact", -1 or shift of nearest bar.
   //Parameters:
   //symbol   -   Symbolic name of the tool. NULL means current symbol. 
   //timeframe   -   Period. Can be one of the chart periods. 0 means the current chart period. 
   //time   -   The value of time to search. 
   //exact   -   The return value if the bar is not found. FALSE - iBarShift returns the nearest. TRUE - iBarShift returns -1. 
   if(time<0) return(-1);
   datetime Arr[], time0;
   CopyTime(symbol,timeframe,0,1,Arr);
   time0=Arr[0];//Time of opening zero bar
   if(CopyTime(symbol, timeframe, time0, time, Arr)>0){
      datetime temptime = iTime(symbol, timeframe, ArraySize(Arr)-1);
      if(Arr[0]==temptime && temptime<=time){
         //Print("Guessed!");
         return(ArraySize(Arr)-1);
      }else{
         if(exact){
            //Print("Not found!");
            return(-1);
         }else{
            //Print("Nearest!");
            return(ArraySize(Arr)-1);
         }
      }
   }else{
      return(-1);
   }
 }//iBarShift()
//+------------------------------------------------------------------+
datetime iTime(string symbol, ENUM_TIMEFRAMES tf, int index) {
   if(index < 0) return(-1);
   datetime Arr[];
   if(CopyTime(symbol, tf, index, 1, Arr)>0)
        return(Arr[0]);
   else return(-1);
}//iTime()
//+------------------------------------------------------------------+
double iClose(string symbol, ENUM_TIMEFRAMES timeframe, int index){
   if(index < 0) return(-1);
   double Arr[];
   if(CopyClose(symbol,timeframe, index, 1, Arr)>0) 
        return(Arr[0]);
   else return(-1);
}//iClose()
//+------------------------------------------------------------------+
double iOpen(string symbol, ENUM_TIMEFRAMES timeframe, int index){
   if(index < 0) return(-1);
   double Arr[];
   if(CopyOpen(symbol,timeframe, index, 1, Arr)>0) 
        return(Arr[0]);
   else return(-1);
}//iOpen()
//+------------------------------------------------------------------+
double iHigh(string symbol, ENUM_TIMEFRAMES timeframe, int index){
   if(index < 0) return(-1);
   double Arr[];
   if(CopyHigh(symbol,timeframe, index, 1, Arr)>0) 
        return(Arr[0]);
   else return(-1);
}//iHigh()
//+------------------------------------------------------------------+
double iLow(string symbol, ENUM_TIMEFRAMES timeframe, int index){
   if(index < 0) return(-1);
   double Arr[];
   if(CopyLow(symbol,timeframe, index, 1, Arr)>0) 
        return(Arr[0]);
   else return(-1);
}//iLow()

//+------------------------------------------------------------------+
void GetInfoPosition(STRUCT_POSITION_STATUS &SPS, string fSymbol="", int fMagic=0, int fType=-1){
   ENUM_DEAL_TYPE DealType;
   double fLot=0.0;
   int myPoints;
   string sym;
   ulong ticket;
   int Count=0;
   int TotalHistory;
   CSymbolInfo MySymbol;
   SPS.sPoints = 0;
   SPS.sProfit = 0;
   SPS.sComission = 0;
   SPS.sSwap = 0;
   int total = PositionsTotal();
   for (int i = 0; i < total; i++) {
      sym=PositionGetSymbol(i);
      if(sym == fSymbol || fSymbol=="") {
         SPS.sSymbol = sym;
         MySymbol.Name(sym);
         HistorySelectByPosition(PositionGetInteger(POSITION_IDENTIFIER));
         TotalHistory = HistoryDealsTotal();
         for(int j=0; j<TotalHistory; j++){
            ticket=HistoryDealGetTicket(j);
            if(HistoryDealGetInteger(ticket, DEAL_MAGIC) == fMagic || fMagic == 0) {
               SPS.sMagic = HistoryDealGetInteger(ticket, DEAL_MAGIC);
               
               DealType = (ENUM_DEAL_TYPE)(HistoryDealGetInteger(ticket, DEAL_TYPE));
               if(DealType==fType || fType==-1){
                  if(HistoryDealGetInteger(ticket, DEAL_ENTRY)==DEAL_ENTRY_IN){
                     if(Count==0){
                        SPS.sOpenPrice=HistoryDealGetDouble(ticket,DEAL_PRICE);
                        SPS.sOpenTime=datetime(HistoryDealGetInteger(ticket,DEAL_TIME));
                     }
                     SPS.sOpenPriceLast=HistoryDealGetDouble(ticket,DEAL_PRICE);
                     Count++;
                  }
                  MySymbol.RefreshRates();
                  if(DealType==DEAL_TYPE_BUY){ //Buy
                     fLot = fLot + HistoryDealGetDouble(ticket,DEAL_VOLUME);
                     myPoints = int(MathRound((MySymbol.Bid() - HistoryDealGetDouble(ticket,DEAL_PRICE)) / MySymbol.Point()));
                     SPS.sSwap = SPS.sSwap + GetSwap(sym, HistoryDealGetInteger(ticket,DEAL_TIME), TimeCurrent(), 0);
                  }else if(DealType==DEAL_TYPE_SELL){ //Sell
                     fLot = fLot - HistoryDealGetDouble(ticket,DEAL_VOLUME);
                     myPoints = int(MathRound((HistoryDealGetDouble(ticket,DEAL_PRICE)- MySymbol.Ask()) / MySymbol.Point()));
                     SPS.sSwap = SPS.sSwap + GetSwap(sym, HistoryDealGetInteger(ticket,DEAL_TIME), TimeCurrent(), 1);
                  }
                  MySymbol.Refresh();
                  SPS.sPoints = SPS.sPoints + myPoints;
                  SPS.sProfit = SPS.sProfit + myPoints * HistoryDealGetDouble(ticket,DEAL_VOLUME) * MySymbol.TickValue();
               }
            }
         }//Next j
         SPS.sComission = SPS.sComission + PositionGetDouble(POSITION_COMMISSION) * MathAbs(fLot) / PositionGetDouble(POSITION_VOLUME);          
      }
   }//Next i
   if(fLot>0){
      SPS.sType = 0;
   }else if(fLot<0){
      SPS.sType = 1;
   }else SPS.sType=-1;
   SPS.sLot=MathAbs(fLot);
   SPS.sCount=Count;
}//GetInfoPosition()
//+------------------------------------------------------------------+
double GetSwap(string fSymbol, datetime OpenTime, datetime CloseTime, int fType){
   //Function returns swap by instrument, over the specified time range
   CSymbolInfo MySym;
   MySym.Name(fSymbol);
   double MySwap;
   if(fType==OP_BUY){
      MySwap = MySym.SwapLong();
   }else{
      MySwap = MySym.SwapShort();
   }
   MqlDateTime Open, Close;
   TimeToStruct(OpenTime, Open);
   TimeToStruct(CloseTime, Close);
   int FirstDay, SecondDay;
   FirstDay = Open.day_of_year;
   SecondDay = Close.day_of_year;
   if(Open.year<Close.year) SecondDay = SecondDay + int(365.242199)*(Close.year-Open.year);
   return(MySwap*(SecondDay-FirstDay));
}//GetSwap()
//+------------------------------------------------------------------+
double GetMarginEvgeTrofi(string fSymbol, int fType=0, string USD="USD"){
   double res;
   int i;
   string name;
   
   if(fType==OP_BUY){
      res = SymbolInfoDouble(fSymbol, SYMBOL_MARGIN_LONG);
   }else{
      res = SymbolInfoDouble(fSymbol, SYMBOL_MARGIN_SHORT);
   }
   res = res * SymbolInfoDouble(fSymbol,SYMBOL_TRADE_CONTRACT_SIZE)/AccountInfoInteger(ACCOUNT_LEVERAGE);
//--- get account currency
   string account_currency=AccountInfoString(ACCOUNT_CURRENCY);

//--- margin currency   
   string margin_currency=SymbolInfoString(fSymbol, SYMBOL_CURRENCY_MARGIN);
   
//--- if margin currency and account currency are equal
   if(margin_currency==account_currency) {
      //--- just return the contract value
      return(res);
   }
   //Print(DoubleToString(res,2)+" "+margin_currency);
   if(margin_currency!=USD){
      //If the currency of margin is not dollars, then translate it into dollars
      for(i=0; i<SymbolsTotal(true); i++){
         name = SymbolName(i,true);
         if(SymbolInfoString(name, SYMBOL_CURRENCY_PROFIT) == margin_currency &&
            SymbolInfoString(name, SYMBOL_CURRENCY_BASE) == USD){
            res = res / PrCur(name, fType);
            margin_currency = USD;
            break;
         }else if(SymbolInfoString(name, SYMBOL_CURRENCY_PROFIT) == USD &&
                  SymbolInfoString(name, SYMBOL_CURRENCY_BASE) == margin_currency){
            res = res * PrCur(name, fType);
            margin_currency = USD;
            break;
         }
         
      }//Next i
   }
   
   if(margin_currency!=USD){
      Print(__FUNCTION__,"  Can't find calculation currency for symbol combination "+fSymbol);
      Print("In the 'Market watch' must already be mapped pair, in which participates "+ margin_currency+ " and USD.");
      return(NULL);
   }
   
   //Now that the margin calculated in dollars, it can be translated into any currency of deposit!
   
   if(margin_currency==account_currency) return(res);
   //Print(DoubleToString(res,2)+" "+margin_currency);
   for(i=0; i<SymbolsTotal(true); i++){
      name = SymbolName(i,true);
      if(SymbolInfoString(name, SYMBOL_CURRENCY_PROFIT) == margin_currency &&
         SymbolInfoString(name, SYMBOL_CURRENCY_BASE) == account_currency){
         res = res / PrCur(name, fType);
         margin_currency = account_currency;
         break;
      }else if(SymbolInfoString(name, SYMBOL_CURRENCY_PROFIT) == account_currency &&
               SymbolInfoString(name, SYMBOL_CURRENCY_BASE) == margin_currency){
         res = res * PrCur(name, fType);
         margin_currency = account_currency;
         break;
      }
   }//Next i
   
   if(margin_currency==account_currency) return(res);

   Print(__FUNCTION__,"  Could not convert the currency "+margin_currency+" to "+account_currency+"! Требуется включить необходимую валютную пару в списке 'Обзор рынка'");
   return(NULL);

}//GetMarginEvgeTrofi()
//+------------------------------------------------------------------+
void LotControl(double& fLot, string fSymbol, int fType, int fMagicNumber){
   //Lot adjustment procedure to avoid conflicts with other trading strategies.
   //This function is applied before the opening of the next position. In fLot entered the calculated value of volume
   //new position on the instrument fSymbol.. If the specified instrument is now open opposite position,
   //then must be corrected "fLot" to "x.xx", where х.хх - is the minimal step of the lot (usually 0.01).
   //FType - the type of position you want to open (0 = OP_BUY, 1 = OP_SELL)
   //fMagicNumber - identifier of trading system
   STRUCT_POSITION_STATUS SPS, mySPS;
   GetInfoPosition(mySPS,fSymbol, fMagicNumber, ContrType(fType));
   GetInfoPosition(SPS,fSymbol, 0, ContrType(fType));
   double ForeignLot = SPS.sLot - mySPS.sLot; //Another lot that has the opposite direction
   if(fLot==ForeignLot){
      fLot=fLot+SymbolInfoDouble(fSymbol, SYMBOL_VOLUME_STEP);
   }
}//LotControl()
//+------------------------------------------------------------------+
int ContrType(int fType){
   if(fType==OP_BUY) return(OP_SELL);
   if(fType==OP_SELL) return(OP_BUY);
   return(-1);
}
//+------------------------------------------------------------------+
double PrCur(string fSymbol, int fType){
   //Returns the price of symbol (Ask - to buy, Bid - to sell)
   MqlTick tick;
   SymbolInfoTick(fSymbol, tick);
   if(fType == OP_BUY){
      return(tick.ask);
   }else{
      return(tick.bid);
   }
}//PrCur()
//+------------------------------------------------------------------+

string PeriodInStr(int per){
   switch(per){
   case PERIOD_M1:
      return("M1");
   case PERIOD_M2:
      return("M2");
   case PERIOD_M3:
      return("M3");
   case PERIOD_M4:
      return("M4");      
   case PERIOD_M5:
      return("M5");
   case PERIOD_M6:
      return("M6");
   case PERIOD_M10:
      return("M10");
   case PERIOD_M12:
      return("M12");
   case PERIOD_M15:
      return("M15");
   case PERIOD_M20:
      return("M20");
   case PERIOD_M30:
      return("M30");
   case PERIOD_H1:
      return("H1");
   case PERIOD_H2:
      return("H2");
   case PERIOD_H3:
      return("H3");
   case PERIOD_H4:
      return("H4");
   case PERIOD_H6:
      return("H6");
   case PERIOD_H8:
      return("H8");
   case PERIOD_H12:
      return("H12");
   case PERIOD_D1:
      return("D1");
   case PERIOD_W1:
      return("W1");
   case PERIOD_MN1:
      return("MN1");
   default:
      return("NOPERIOD");
   }
}//PeriodInStr()
//+------------------------------------------------------------------+
double iMAOnArray(double &array[], int total, int period, int ma_shift, int ma_method, int shift){
   double buf[],arr[];
   if(total==0) total=ArraySize(array);
   if(total>0 && total<=period) return(0);
   if(shift>total-period-ma_shift) return(0);
   switch(ma_method)
     {
      case MODE_SMA :
        {
         total=ArrayCopy(arr,array,0,shift+ma_shift,period);
         if(ArrayResize(buf,total)<0) return(0);
         double sum=0;
         int    i,pos=total-1;
         for(i=1;i<period;i++,pos--)
            sum+=arr[pos];
         while(pos>=0)
           {
            sum+=arr[pos];
            buf[pos]=sum/period;
            sum-=arr[pos+period-1];
            pos--;
           }
         return(buf[0]);
        }
      case MODE_EMA :
        {
         if(ArrayResize(buf,total)<0) return(0);
         double pr=2.0/(period+1);
         int    pos=total-2;
         while(pos>=0)
           {
            if(pos==total-2) buf[pos+1]=array[pos+1];
            buf[pos]=array[pos]*pr+buf[pos+1]*(1-pr);
            pos--;
           }
         return(buf[shift+ma_shift]);
        }
      case MODE_SMMA :
        {
         if(ArrayResize(buf,total)<0) return(0);
         double sum=0;
         int    i,k,pos;
         pos=total-period;
         while(pos>=0)
           {
            if(pos==total-period)
              {
               for(i=0,k=pos;i<period;i++,k++)
                 {
                  sum+=array[k];
                  buf[k]=0;
                 }
              }
            else sum=buf[pos+1]*(period-1)+array[pos];
            buf[pos]=sum/period;
            pos--;
           }
         return(buf[shift+ma_shift]);
        }
      case MODE_LWMA :
        {
         if(ArrayResize(buf,total)<0) return(0);
         double sum=0.0,lsum=0.0;
         double price;
         int    i,weight=0,pos=total-1;
         for(i=1;i<=period;i++,pos--)
           {
            price=array[pos];
            sum+=price*i;
            lsum+=price;
            weight+=i;
           }
         pos++;
         i=pos+period;
         while(pos>=0)
           {
            buf[pos]=sum/weight;
            if(pos==0) break;
            pos--;
            i--;
            price=array[pos];
            sum=sum-lsum+price*period;
            lsum-=array[i];
            lsum+=price;
           }
         return(buf[shift+ma_shift]);
        }
      default: return(0);
     }
   return(0);
  }