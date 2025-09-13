#property copyright "trevone"
#property link "http://www.mql4.com/users/trevone" 

/* 
*   @desc: External variables declaration
*/
extern string TradeComment = "MPM"; 
extern int MAGIC = 677885;
extern int StartHour = 10;
extern int SignalStartHour = 0;
extern int SignalEndHour = 23; 
extern string SignalSettings = "-----------------------------------------------------------------------------------------------";
extern int Signal = 0;
extern string MarginSettings = "-----------------------------------------------------------------------------------------------";
extern double MarginUsage = 0.1;     
extern string TradeSettings = "-----------------------------------------------------------------------------------------------";
extern double ProfitPerLot = 300; 
extern double BreakEvenPerLot = 1;
extern double InitiateLossPerLot = 600;
extern double LossPerLot = 150;
extern string TimeSettings = "-----------------------------------------------------------------------------------------------";
extern int RunTime = 28800; 
extern int GraceTime = 3600;
extern string OtherSettings = "-----------------------------------------------------------------------------------------------";
extern double StopRatio = 0.05;
extern int ProgressiveCandles = 3;
extern double ProgressiveSize = 0.6;
extern double BreakSize = 2;    
extern string IndicatorSettings = "-----------------------------------------------------------------------------------------------";
extern int ATRPeriod = 14; 
extern double ReverseRatio = 5; 
extern double InitiateReversePerLot = 400;
extern double ReverseProfitPerLot = 500;


/* 
*   @desc: Double variables declaration 
*/  

double spread, slippage, pipPoints, lotSize, eATRCur, stopLevel,
marginRequirement, highestAccountBalance, openLots, openBuyLots, openSellLots; 
 
double BaseLotSize = 0.01;
double DynamicSlippage = 1; 
double startTradingTime = 0;   
double dailyMovement = 0;  
    
/* 
*   @desc: Global interger variables declaration 
*/ 

int r, digits, totalTrades, lastCloseTime; 
 
int openType = -1;
int totalDailyTrades = 0; 
int totalStops = 0;
int totalLongCandles = 0;
int totalShortCandles = 0; 
int LotPrecision = 2;
int MaxDailyTrades = 1; 

int totalTargets = 0;
int totalBreakeven = 0;
int totalRunners = 0;
int totalDailyRunners = 0;
int totalReverse = 0;
int totalLosers = 0;

int symbolHistory;
int totalHistory = 100;

int ATRShift = 0;   
int ATRTimeFrame = 0;

int QueryHistory = 7;
  
/* 
*   @desc: Global boolean variables declaration 
*/ 

bool longSignal = false;
bool shortSignal = false; 
bool breakeven = false; 
bool takeALoss = false;
 
/* 
*   @desc: Global string variables declaration 
*/ 

string display = ""; 

string debug = "";
 
void init(){

   /* 
   *   @desc: Init some variables before start 
   */
   
   highestAccountBalance = AccountBalance();  
   setPipPoint();
   
}

void deinit(){ 
   Print( 
      "totalTargets: ", totalTargets, 
      " totalRunners: ", totalRunners, 
      " totalDailyRunners: ", totalDailyRunners, 
      " totalBreakeven: ", totalBreakeven, 
      " totalLosers: ", totalLosers,
      " totalReverse: ", totalReverse 
   );
}

void closeAll( string type = "none" ){   

   /* 
   *   @desc: Closes all open positions 
   */ 

   for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() ){   
         RefreshRates();
         if( OrderType() == OP_BUY ){ 
            r = OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            if( !r ) Print( "914 Error in closeAll() OP_BUY. Error code=", GetLastError() );
            else {
               Print( "813 Closed OP_BUY in closeAll() from ", type );
               if( type == "manageBreakeven()" ) totalBreakeven++; 
               else if( type == "manageLoss()" ) totalLosers++;
               else if( type == "manageRunners()" ) totalDailyRunners++;
               else if( type == "manageProfitTime()" ) totalTargets++; 
            }  
         } else if( OrderType() == OP_SELL ) {
            r = OrderClose( OrderTicket(), OrderLots(), Ask, slippage ); 
            if( !r ) Print( "904 Error in closeAll() OP_SELL. Error code=", GetLastError() );
            else {
               Print( "810 Closed OP_SELL in closeAll() from ", type );
               if( type == "manageBreakeven()" ) totalBreakeven++; 
               else if( type == "manageLoss()" ) totalLosers++;
               else if( type == "manageRunners()" ) totalDailyRunners++;
               else if( type == "manageProfitTime()" ) totalTargets++; 
            }
         } 
      }
   } 
   
}  

void setPipPoint(){

   /* 
   *   @desc: Sets the pipPoints for the current Symbol 
   */

   digits = MarketInfo( Symbol(), MODE_DIGITS );
   pipPoints = 0.01;
   if( digits == 3 || digits == 2 ) pipPoints = 0.01;
   else if( digits == 5 || digits == 4 ) pipPoints = 0.0001;
   
   if( Symbol() == "XAUUSD" || Symbol() == "XAGUSD" || Symbol() == "XAUUSDMini" || Symbol() == "XAGUSDMini" ) 
      stopLevel = MarketInfo( Symbol(), MODE_STOPLEVEL );
   else stopLevel = MarketInfo( Symbol(), MODE_STOPLEVEL ) / 10;
   
} 

double marginCalculate( string symbol, double volume ){  

   /* 
   *   @desc: Returns the margin required to open 0.01 lots 
   */

   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
   
} 

void lotSize(){  

   /* 
   *   @desc: Calculates the lotSize based on account information 
   */  
   
   spread = ( Ask - Bid ) / pipPoints;
   slippage = NormalizeDouble( ( spread / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize );  
   lotSize = NormalizeDouble( ( AccountFreeMargin() * MarginUsage / marginRequirement ) * BaseLotSize, LotPrecision ); 
   if( LotPrecision == 2 && lotSize < 0.01 ) lotSize = 0.01;
   else if( LotPrecision == 1 && lotSize < 0.1 ) lotSize = 0.1; 
   if( lotSize > 999 ) lotSize = 999; 
   if( AccountBalance() > highestAccountBalance ) highestAccountBalance = AccountBalance(); 
   
}

void preparePositions() { 

   /* 
   *   @desc: Prepares all the trade information before the main logic
   */
   
   totalTrades = 0;
   startTradingTime = 999999999999;
   openType = -1;
   totalStops = 0;
   openLots = 0;
   openBuyLots = 0;
   openSellLots = 0;
   for( int i = 0; i < OrdersTotal(); i++ ) { 
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;     
      if( OrderOpenTime() < startTradingTime ) startTradingTime = OrderOpenTime();
      if( OrderType() == OP_BUY ) {
         openType = OP_BUY;
         openBuyLots = openBuyLots + OrderLots();
      } else if( OrderType() == OP_SELL ) {
         openType = OP_SELL;
         openSellLots = openSellLots + OrderLots();
      }
      if( OrderStopLoss() != 0 ) totalStops++; 
      openLots = MathAbs( openBuyLots - openSellLots );
      totalTrades++;
   }   
   
   if( totalTrades == 0 ){   
      breakeven = false;
      takeALoss = false;
   }
    
   if( takeALoss == false && AccountProfit() < openLots * -InitiateLossPerLot ){ 
      takeALoss = true;
   } 
   
}  

void manageRunners(){
   if( Hour() == StartHour && Minute() == 0 ){
      totalDailyTrades = 0;
      if( AccountEquity() / AccountBalance() > 1 ) 
         closeAll( "manageRunners()" ); 
   }
}
 

void prepareIndicators(){ 

   /* 
   *   @desc: Creates the generic indicators for the signals
   */
   HideTestIndicators( true );
   eATRCur = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift ); 
    
} 
   
void prepareSignal(){

   /* 
   *   @desc: Generates a long or short signal
   */

   longSignal = false;
   shortSignal = false; 
   
   if( Signal == 0 ){
      totalLongCandles = 0;
      totalShortCandles = 0;
      
      dailyMovement = ( iClose( NULL, PERIOD_D1, 0 ) - iOpen( NULL, PERIOD_D1, 0 ) ) / pipPoints;
      
      for( int i = 1; i < ProgressiveCandles + 1; i++ ){
         if( Open[i] < Close[i] && MathAbs( Open[i] - Close[i] ) < ( ProgressiveSize * eATRCur ) ){
            totalLongCandles++;
         }
         if( Open[i] > Close[i] && MathAbs( Open[i] - Close[i] ) < ( ProgressiveSize * eATRCur ) ){
            totalShortCandles++;
         }
      } 
      
      if( TimeCurrent() - startTradingTime > Period() * 60 ){
         if( !breakeven && openType == OP_BUY && Close[1] < Open[1]  && MathAbs( Open[1] - Close[1] ) > ( BreakSize * eATRCur ) ){
            breakeven = true;
         } else if( !breakeven && openType == OP_SELL && Close[1] > Open[1] && MathAbs( Open[1] - Close[1] ) > ( BreakSize * eATRCur ) ){
            breakeven = true;
         }
      }
    
      if( dailyMovement > 0 && totalLongCandles == ProgressiveCandles ){
         longSignal = true;
         shortSignal = false;
      } else if( dailyMovement < 0 && totalShortCandles == ProgressiveCandles ){
         longSignal = false;
         shortSignal = true; 
      }
   } 
    
}

void openPosition(){

   /* 
   *   @desc: Opens a postion if there is a signal
   */
   
   if( ( ( SignalStartHour < SignalEndHour && Hour() >= SignalStartHour && Hour() < SignalEndHour ) || ( SignalStartHour > SignalEndHour && ( ( Hour() <= SignalEndHour && Hour() >= 0 ) || ( Hour() <= 23 && Hour() >= SignalStartHour ) ) ) ) ){
      
      if( totalTrades == 0 && totalDailyTrades < MaxDailyTrades && TimeCurrent() - lastCloseTime > GraceTime ){ 
         if( longSignal ){
            if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
            r = OrderSend( Symbol(), OP_BUY, lotSize, Ask, slippage, 0, 0, TradeComment, MAGIC );
            if( !r ) Print( "814 Error in openPosition() OP_BUY. Error code=", GetLastError() );
            else Print( "843 Opened OP_BUY in openPosition() ", lotSize, " lots @ ", Ask );  
         } else if( shortSignal ){  
            if( AccountFreeMarginCheck( Symbol(), OP_SELL, lotSize ) <= 0 || GetLastError() == 134 ) return;
            r = OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, TradeComment, MAGIC );
            if( !r ) Print( "811 Error in openPosition() OP_SELL. Error code=", GetLastError() );
            else Print( "843 Opened OP_SELL in openPosition() ", lotSize, " lots @ ", Bid ); 
         } 
      }
   }

   
}  
 

void display(){

   /* 
   *   @desc: Write information on the chart
   */

   display = "";  
   display = StringConcatenate( display ,"leverage: ", AccountLeverage(), "\n" );  
   display = StringConcatenate( display ,"lotSize: ", lotSize, "\n" );   
   display = StringConcatenate( display ,"openLots: ", DoubleToStr( openLots, 2 ), "\n" ); 
   display = StringConcatenate( display ,"highestAccountBalance: ", highestAccountBalance, "\n" );      
   if( GraceTime - ( TimeCurrent() - lastCloseTime ) > 0 )
      display = StringConcatenate( display ,"GraceTime: ", GraceTime - ( TimeCurrent() - lastCloseTime ), "\n" );
   
   
   Comment( display ); 
   
}
 

void manageProfitTime(){
   double profit, stop, stopPrice;
   
   /*
   if( totalStops > 0 && openType == OP_BUY && Close[1] < Open[1]  && MathAbs( Open[1] - Close[1] ) > ( BreakSize * eATRCur ) ){
      closeAll( "manageProfitTime()" );
   } else if( totalStops > 0 && openType == OP_SELL && Close[1] > Open[1] && MathAbs( Open[1] - Close[1] ) > ( BreakSize * eATRCur ) ){
      closeAll( "manageProfitTime()" );
   }
   */
   if( AccountProfit() > openLots * ProfitPerLot ){
      
      if( TimeCurrent() - startTradingTime > RunTime && totalStops == 0 ){
         closeAll( "manageProfitTime()" ); 
         totalDailyTrades++; 
      } else {
         for( int i = 0; i < OrdersTotal(); i++ ) { 
            if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
            if( OrderType() == OP_BUY ){
               profit = MathAbs( OrderOpenPrice() - Bid );
               stop = NormalizeDouble( profit * StopRatio, digits );
               stopPrice = OrderOpenPrice() + stop;
               if( OrderStopLoss() == 0 && Bid > stopPrice + ( stopLevel * pipPoints ) && stopPrice > OrderOpenPrice() && OrderStopLoss() != stopPrice ){
                  r = OrderModify( OrderTicket(), OrderOpenPrice(), stopPrice, 0, 0, 0 );
                  if( !r ) Print( "211 Error in manageProfitTime() OP_BUY. Error code=", GetLastError() );
                  else {
                     Print( "243 Modified OP_BUY in manageProfitTime()" ); 
                     totalRunners++;
                  }
               }
            } else if( OrderType() == OP_SELL ){
               profit = MathAbs( OrderOpenPrice() - Ask );
               stop = NormalizeDouble( profit * StopRatio, digits );
               stopPrice = OrderOpenPrice() + stop;
               if( OrderStopLoss() == 0 && Ask < stopPrice - ( stopLevel * pipPoints ) && stopPrice < OrderOpenPrice() && OrderStopLoss() != stopPrice ){
                  r = OrderModify( OrderTicket(), OrderOpenPrice(), stopPrice, 0, 0, 0 );
                  if( !r ) Print( "271 Error in manageProfitTime() OP_SELL. Error code=", GetLastError() );
                  else {
                     Print( "233 Modified OP_SELL in manageProfitTime()" );
                     totalRunners++;
                  }
               }
            }
         }
      }
   }
} 

void manageBreakeven(){  
   if( breakeven && AccountProfit() > openLots * BreakEvenPerLot && totalStops == 0 ){
      closeAll( "manageBreakeven()" ); 
   } 
}

void manageLoss(){  
   if( takeALoss && AccountProfit() > openLots * -LossPerLot ) {
      closeAll( "manageLoss()" ); 
   } 
} 
  
void prepareHistory(){
   symbolHistory = 0; 
   lastCloseTime = 0; 
   for( int iPos = OrdersHistoryTotal() - 1; iPos > ( OrdersHistoryTotal() - 1 ) - totalHistory; iPos-- ){
      r = OrderSelect( iPos, SELECT_BY_POS, MODE_HISTORY );
      double QueryHistoryDouble = ( double ) QueryHistory;
      if( symbolHistory >= QueryHistoryDouble ) break;
      if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() ){
         if( OrderCloseTime() > lastCloseTime ) lastCloseTime = OrderCloseTime();
         symbolHistory = symbolHistory + 1;
      }
   }   
}


void openReverse(){
   double reverseLots;
   if( totalTrades == 1 && AccountProfit() < openLots * -InitiateReversePerLot ){
      if( openType == OP_BUY ){
         reverseLots = ReverseRatio * openBuyLots;
         if( AccountFreeMarginCheck( Symbol(), OP_SELL, reverseLots ) <= 0 || GetLastError() == 134 ) {
            Print( "133 Failed Reverse OP_SELL in openReverse() Lots: ", reverseLots, " AccountFreeMargin: ", AccountFreeMargin() );
            return;
         }
         r = OrderSend( Symbol(), OP_SELL, reverseLots, Bid, slippage, 0, 0, TradeComment, MAGIC );
         if( !r ) Print( "179 Error in OrderSend openReverse() OP_SELL. Error code=", GetLastError() );
         else {
            Print( "114 OrderSend OP_SELL in openReverse()" );
            totalReverse++;
         }
      } else if( openType == OP_SELL ){
         reverseLots = ReverseRatio * openSellLots;
         if( AccountFreeMarginCheck( Symbol(), OP_BUY, reverseLots ) <= 0 || GetLastError() == 134 ) {
            Print( "133 Failed Reverse OP_SELL in openReverse() Lots: ", reverseLots, " AccountFreeMargin: ", AccountFreeMargin() );
            return;
         }
         r = OrderSend( Symbol(), OP_BUY, reverseLots, Ask, slippage, 0, 0, TradeComment, MAGIC );
         if( !r ) Print( "161 Error in OrderSend openReverse() OP_BUY. Error code=", GetLastError() );
         else {
            Print( "117 OrderSend OP_BUY in openReverse()" );
            totalReverse++;
         }
      }
   }
}

void manageReverse(){
   if( totalTrades == 2 && AccountProfit() > openLots * ReverseProfitPerLot ){
      closeAll( "manageReverse()" );
   }
}

  
void start(){

   /* 
   *   @desc: Main tick function calls all other functions in order
   */  
   debug = "";
   lotSize();
   preparePositions(); 
   prepareHistory(); 
   prepareIndicators();
   prepareSignal(); 
   
   openPosition(); 
   openReverse(); 
   
   manageReverse();
   manageBreakeven();
   manageLoss();
   manageProfitTime(); 
   manageRunners(); 
   
   display();
   
}

// signals
// proctect single trade
// multiple reverse trades
 