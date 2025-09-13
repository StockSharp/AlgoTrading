//+------------------------------------------------------------------+
//| Auto SL-TP                                             |
//+------------------------------------------------------------------+

/*
   This utility EA places Stoploss & TakeProfit if there are positions without them.
*/

extern string  About = "<<< Automatic SL and TP manager >>>";

extern bool    Set_StopLoss = true;
extern bool    Set_TakeProfit = true;

// StopLoss
extern string  SLExp = "<<< StopLoss >>>";
extern string  StopLossExp = "1=Fixed Pips  2=ATR Multiple";
extern int     StopLoss_Method = 1 ;
extern int     Fixed_SL = 5;
extern double  StopLoss_ATR = 0.7 ;

// TakeProfit
extern string  TPExp = "<<< TakeProfit >>>";
extern string  TakeProfitExp = "1=Fixed Pips  2=ATR Multiple";
extern int     TakeProfit_Method = 1 ;
extern int     Fixed_TP = 10;
extern double  TakeProfit_ATR = 1.8 ;

// ATR Setting
extern string  ATRExp = "<<< ATR Setting >>>";
extern string  TF_note = "0=Chart 1=M1 2=M5 3=M15 4=M30 5=1H 6=4H 7=D1 8=W1 9=MN";
extern int     ATR_TimeFrame = 7;
extern int     ATR_Period = 30;

// Misc
extern string  AboutMisc = "<<< Miscellaneous >>>";
extern bool    Alert_On = false ;

string NAME = "Auto SL-TP Setter v1";

static int     ATR_TF;

//+------------------------------------------------------------------+
//| init                                                            
//+------------------------------------------------------------------+
int init()
{
   ATR_TF = TF_Selector(ATR_TimeFrame);

   return(0);
}

//+------------------------------------------------------------------+
//| TF Selector                                                      |
//+------------------------------------------------------------------+
int TF_Selector(int TIMEFRAME)
{
   int TF;

   // Indicator Time Frame
   switch(TIMEFRAME) // 0=Chart 1=M1 2=M5 3=M15 4=M30 5=1H 6=4H 7=D1 8=W1 9=MN
   {
      case 0 : TF = 0; break; 
      case 1 : TF = 1; break; 
      case 2 : TF = 5; break; 
      case 3 : TF = 15; break; 
      case 4 : TF = 30; break; 
      case 5 : TF = 60; break; 
      case 6 : TF = 240; break; 
      case 7 : TF = 1440; break; 
      case 8 : TF = 10080; break; 
      case 9 : TF = 43200; break; 
      default: TF = 0; break; 
   }

   return(TF);
}

//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
int start()
{
   

   if(Set_StopLoss) SetStopLoss();
   if(Set_TakeProfit) SetTakeProfit();
   
   return(0);
}

//+------------------------------------------------------------------+
//| SL_Decision                                                      |
//+------------------------------------------------------------------+
int SL_Decision(string SYMBOL)
{
   if(StopLoss_Method>2 || StopLoss_Method<1) StopLoss_Method = 1; // error correction
   
   double SL = Fixed_SL * DeciQuoteAdjuster(SYMBOL) ;
   if(StopLoss_Method == 2) SL = (ATR(SYMBOL) * StopLoss_ATR) / MarketInfo(SYMBOL,MODE_POINT) ;
   
   int Stoploss = SL;

   return(Stoploss);
}

//+------------------------------------------------------------------+
//| TP_Decision                                                      |
//+------------------------------------------------------------------+
int TP_Decision(string SYMBOL)
{
   if(TakeProfit_Method>2 || TakeProfit_Method<1) TakeProfit_Method = 2; // error correction
   
   double TP = Fixed_TP * DeciQuoteAdjuster(SYMBOL) ;
   if(TakeProfit_Method == 2) TP = (ATR(SYMBOL) * TakeProfit_ATR) / MarketInfo(SYMBOL,MODE_POINT) ;
   
   int TakeProfit = TP;

   return(TakeProfit);
}

//+------------------------------------------------------------------+
//| ATR                                                              |
//+------------------------------------------------------------------+
double ATR(string SYMBOL)
{
   double ATR = iATR(SYMBOL,ATR_TF,ATR_Period,1);
   return(ATR);
}

//+------------------------------------------------------------------+
//| SetStopLoss                                                      |
//+------------------------------------------------------------------+
void SetStopLoss()
{
   int i,Type; bool selected, success;

   for(i=0; i<OrdersTotal(); i++)
   {
      selected = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(selected && (OrderType() == OP_BUY || OrderType() == OP_SELL) && OrderStopLoss() < 1 * MarketInfo(OrderSymbol(),MODE_POINT))
      {
         Type = OrderType();
         success = false;
         
         if(Type == OP_BUY) success = OrderModify(OrderTicket(), OrderOpenPrice(),OrderOpenPrice() - SL_Decision(OrderSymbol()) * MarketInfo(OrderSymbol(),MODE_POINT), OrderTakeProfit(), 0, CLR_NONE);
         if(Type == OP_SELL) success = OrderModify(OrderTicket(), OrderOpenPrice(),OrderOpenPrice() + SL_Decision(OrderSymbol()) * MarketInfo(OrderSymbol(),MODE_POINT), OrderTakeProfit(), 0, CLR_NONE);

         if(success && Alert_On) Alert("Stoploss for "+OrderSymbol()+" has been placed automatically.") ;
         if (!success) Print("Error code = " + GetLastError());
      }
   }
} // end of SetStopLoss

//+------------------------------------------------------------------+
//| SetStopLoss                                                      |
//+------------------------------------------------------------------+
void SetTakeProfit()
{
   int i,Type; bool selected, success;

   for(i=0; i<OrdersTotal(); i++)
   {
      selected = OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(selected && (OrderType() == OP_BUY || OrderType() == OP_SELL) && OrderTakeProfit() < 1 * MarketInfo(OrderSymbol(),MODE_POINT))
      {
         Type = OrderType();
         success = false;
         
         if(Type == OP_BUY) success = OrderModify(OrderTicket(), OrderOpenPrice(), OrderStopLoss(), OrderOpenPrice() + TP_Decision(OrderSymbol()) * MarketInfo(OrderSymbol(),MODE_POINT), 0, CLR_NONE);
         if(Type == OP_SELL) success = OrderModify(OrderTicket(), OrderOpenPrice(), OrderStopLoss(), OrderOpenPrice() - TP_Decision(OrderSymbol()) * MarketInfo(OrderSymbol(),MODE_POINT), 0, CLR_NONE);

         if(success && Alert_On) Alert("TakeProfit for "+OrderSymbol()+" has been placed automatically.") ;
         if (!success) Print("Error code = " + GetLastError());
      }
   }
} // end of SetStopLoss

//+------------------------------------------------------------------+
//| Deci Quote Adjuster                                              | 
//+------------------------------------------------------------------+
int DeciQuoteAdjuster(string SYMBOL)
{   
   int DQADJ = 1;
   int DIGITS = MarketInfo(SYMBOL,MODE_DIGITS);
   if(DIGITS == 5 || DIGITS == 3 ) DQADJ = 10;

   return(DQADJ);
}

