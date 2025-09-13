#property copyright "trevone"
#property link "Milestone 4.3" 
extern string TOOLS = ".............................................................................................................";
extern int MAGIC = 20130721;
extern bool NewSession = false;
extern bool CloseAll = false;
extern bool ContinueTrading = true;
extern bool BackupSystem = true;  
extern bool FrontSystem = true;  
extern bool EarlyExitsFront = false;
extern bool EarlyExitsBack = false;
extern bool FixedSpace = false;
extern bool FixedLots = false;
extern string REWARD = ".............................................................................................................";
extern int QueryHistory = 7; 
extern double BasketProfit = 30.35;
extern double OpenProfit = 3.25; 
extern double MinProfit =  0.45; 
extern int StopLoss = 0.35;
extern int MaxDrawDown = 0.7;
extern int RelativeStop = 1.35;
extern double TriggerFrontSystem = 0.1;
extern double TriggerBackSystem = 0.99;
extern string RISK = "............................................................................................................."; 
extern int MaxTrades = 3; 
extern double MarginUsage = 0.01;
extern double TradeSpace = 1.5;
extern string INDICATOR_ATR = ".............................................................................................................";
extern int ATRTimeFrame = 0;
extern int ATRPeriod = 10;
extern int ATRShift = 0; 
extern int ATRShiftCheck = 2; 
extern string INDICATOR_ADX = ".............................................................................................................";
extern double ADXMain = 10; 
extern int ADXTimeFrame = 0;
extern int ADXPeriod = 14;
extern int ADXShift = 0;
extern int ADXShiftCheck = 1; 
extern string INDICATOR_MA = ".............................................................................................................";
extern int MATimeFrame = 0; 
extern int MA2Period = 10;
extern int MA1Period = 120;
extern int MMAShift = 0;
extern int MAShift = 0;
extern int MAShiftCheck = 1; 


double slippage, marginRequirement, lotSize, totalHistoryProfit, totalProfit, totalLoss, symbolHistory,
eATR, eATRPrev, eADXMain, eADXPlusDi, eADXMinusDi, eADXMainPrev, eADXPlusDiPrev, eADXMinusDiPrev, MA1Cur, MA1Prev,MA2Cur, MA2Prev;

int digits, totalTrades, totalBackupTrades;

bool nearLongPosition = false;
bool nearShortPosition = false;  
bool rangingMarket = false;
bool bullish = false;
bool bearish = false; 
bool incrementLimits = false;
int totalHistory = 100;
int basketNumber = 0;
int basketNumberType = -1;
int basketCount = -1; 
double buyLots = 0;
double sellLots = 0;
double pipPoints = 0.00010; 
double fractalUpPrice = 0;
double fractalDownPrice = 0; 
double milestoneCount = 0;
double milestoneEquity = 0;
double milestoneBalance = 0;
double nextMilestone = 0;
double prevMilestone = 0;
double accumulatedEquity = 0; 
double mileEquity = 0; 
double DynamicSlippage = 1;   
double BaseLotSize = 0.01; 
double accountLimit = 0;
double accountReserve = 0;

string display = "\n"; 

int MaxStartTrades = 1;

int init(){   
   prepare(); 
   return( 0 );
}
  

double marginCalculate( string symbol, double volume ){ 
   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
} 

void lotSize(){ 
   slippage = NormalizeDouble( ( eATR / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize ); 
   lotSize = NormalizeDouble( ( AccountBalance() * MarginUsage / marginRequirement ) * BaseLotSize, 2 ) ; 
   if( lotSize < 0.01 ) lotSize = 0.01; 
} 

void setPipPoint(){
   digits = MarketInfo( Symbol(), MODE_DIGITS );
   if( digits == 3 ) pipPoints = 0.010;
   else if( digits == 5 ) pipPoints = 0.00010;
} 

void closeAll( string type = "none" ){
   for( int i=OrdersTotal()-1;i>=0;i-- ) {
   if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      if( OrderSymbol() == Symbol() ){ 
         RefreshRates();
         if( ( OrderStopLoss() == 0 && OrderProfit() > 0 && type == "profits" ) || type == "none" ){
            if( OrderType() == OP_BUY ) OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            if( OrderType() == OP_SELL ) OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
         }
      }
   }
} 

void prepareHistory(){
   symbolHistory = 0;
   totalHistoryProfit = 0;
   for( int iPos = OrdersHistoryTotal() - 1 ; iPos > ( OrdersHistoryTotal() - 1 ) - totalHistory; iPos-- ){
      OrderSelect( iPos, SELECT_BY_POS, MODE_HISTORY ) ;
      double QueryHistoryDouble = ( double ) QueryHistory;
      if( symbolHistory >= QueryHistoryDouble ) break;
      if( OrderSymbol() == Symbol() ){
         totalHistoryProfit = totalHistoryProfit + OrderProfit() ;
         symbolHistory = symbolHistory + 1 ;
      }
   }
}

void prepareTrend(){
   if( eADXMain < ADXMain ) {
      rangingMarket = true;
      bullish = false;
      bearish = false;
   } else {
      rangingMarket = false; 
      if( MA1Cur < MA2Cur && eADXPlusDi > ADXMain && eADXPlusDiPrev < eADXMinusDi && Close[0] < MA2Cur ) {
         bullish = true;  
         bearish = false;
      } else if( MA1Cur > MA2Cur && eADXMinusDi > ADXMain && eADXMinusDiPrev < eADXMinusDi && Close[0] > MA2Cur ) {
         bearish = true;
         bullish = false;   
      }
   }
}

void preparePositions() {
   nearLongPosition = false;
   nearShortPosition = false;
   totalTrades = 0;
   totalBackupTrades = 0;
   totalProfit = 0;
   totalLoss = 0;
   buyLots = 0;
   sellLots = 0;
   for( int i = 0 ; i < OrdersTotal() ; i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if( OrderSymbol() == Symbol() ) totalTrades = totalTrades + 1;
      if( OrderSymbol() == Symbol() && OrderComment() == "backup" ) totalBackupTrades = totalBackupTrades + 1;
      if( OrderSymbol() == Symbol() && OrderStopLoss() == 0 ) {
         if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace ) nearLongPosition = true ;
         else if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace ) nearShortPosition = true ;
         if( OrderType() == OP_BUY ) buyLots = buyLots + OrderLots(); 
         else if( OrderType() == OP_SELL ) sellLots = sellLots + OrderLots(); 
         if( OrderProfit() > 0 ) totalProfit = totalProfit + OrderProfit();
         else totalLoss = totalLoss + OrderProfit(); 
      }
   } 
} 

void prepareIndicators(){
   eATR = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift ); 
   eATRPrev = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift + ATRShiftCheck ); 
   eADXMain = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MAIN, ADXShift ); 
   eADXPlusDi = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_PLUSDI, ADXShift );  
   eADXMinusDi = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MINUSDI, ADXShift );    
   eADXMainPrev = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MAIN, ADXShift + ADXShiftCheck ); 
   eADXPlusDiPrev = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_PLUSDI, ADXShift + ADXShiftCheck );  
   eADXMinusDiPrev = iADX( NULL, ADXTimeFrame, ADXPeriod, PRICE_MEDIAN, MODE_MINUSDI, ADXShift + ADXShiftCheck ); 
   MA1Cur = iMA( NULL, PERIOD_M5, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );  
   MA1Prev = iMA( NULL, PERIOD_M5, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + MAShiftCheck ); 
   MA2Cur = iMA( NULL, PERIOD_M5, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );  
   MA2Prev = iMA( NULL, PERIOD_M5, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + MAShiftCheck ); 
} 

void prepareFractals(){ 
   fractalUpPrice = 0 ;
   fractalDownPrice = 0;  
   bool iUp = false; 
   bool iDn = false; 
   for( int i = 0; i < totalHistory; i++ ){ 
      double ifractalUp = iFractals( NULL, 0, MODE_UPPER, i );
      double ifractalDown = iFractals( NULL, 0, MODE_LOWER, i );
      if( ifractalUp > 0 && Open[i] > Open[0] ){
         iUp = true;
         if( Open[i] > Close[i] ) fractalUpPrice = Open[i]; 
         else fractalUpPrice = Close[i];
      }
      if( ifractalDown > 0 && Open[i] < Open[0] ){
         iDn = true;
         if( Open[i] < Close[i] ) fractalDownPrice = Open[i]; 
         else fractalDownPrice = Close[i];  
      }
      if( iUp && iDn ) break;
   } 
}
 

void prepare(){ 
   prepareIndicators();
   prepareFractals();
   prepareTrend();
   setPipPoint(); 
   prepareHistory();
   preparePositions(); 
   lotSize();   
   update();  
} 

void openPosition(){  
   if( !nearLongPosition && bullish && sellLots == 0 ) {
      if( basketNumberType != OP_BUY ) basketCount = 0;
      if( basketCount < MaxTrades ){
         OrderSend( Symbol(), OP_BUY , lotSize, Ask, slippage, 0, 0, "basketNumber" + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
         basketCount = basketCount + 1;
         if( basketNumberType != OP_BUY ) basketNumber = basketNumber + 1; 
         basketNumberType = OP_BUY; 
      } 
   } else if( !nearShortPosition && bearish && buyLots == 0 ) {
      if( basketNumberType != OP_SELL ) basketCount = 0;
      if( basketCount < MaxTrades ){
         OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "basketNumber" + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
         basketCount = basketCount + 1; 
         if( basketNumberType != OP_SELL ) basketNumber = basketNumber + 1; 
         basketNumberType = OP_SELL; 
      }
   }  
} 

void backSystem(){ 
   if( ( ContinueTrading || ( !ContinueTrading && totalBackupTrades > 0 ) ) && ( totalBackupTrades < MaxTrades ) ) {
      int type = -1;
      if( bullish ) type = OP_BUY;
      else if( bearish ) type = OP_SELL; 
      if( !nearLongPosition && type == OP_BUY && sellLots == 0 ) OrderSend( Symbol(), type, lotSize, Ask, slippage, 0, 0, "backup", MAGIC ) ;
      else if( !nearShortPosition && type == OP_SELL && buyLots == 0 ) OrderSend( Symbol(), type , lotSize, Bid, slippage, 0, 0, "backup", MAGIC ) ;
    } 
}

void frontSystem(){
  
 
}

void managePositions(){
   if( totalHistoryProfit < 0 && totalProfit > MathAbs( totalHistoryProfit ) * BasketProfit ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit > MathAbs( totalLoss ) * OpenProfit ) closeAll();
   else if( totalTrades > 0 && totalTrades <= MaxStartTrades && totalProfit > 0 && ( ( bullish && basketNumberType == OP_SELL ) || ( bearish && basketNumberType == OP_BUY ) ) ) closeAll(); 
   else { 
      for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol()  ) {  
            if( totalTrades <= MaxStartTrades ){
               if( OrderType() == OP_BUY && Bid > OrderOpenPrice() &&  MathAbs( Bid - OrderOpenPrice() ) > MinProfit * eATR ) 
                  OrderClose( OrderTicket(), OrderLots(), Bid, slippage ); 
               else if( OrderType() == OP_SELL && Ask < OrderOpenPrice() && MathAbs( OrderOpenPrice() - Ask ) > MinProfit * eATR )
                  OrderClose( OrderTicket(), OrderLots(), Ask, slippage );   
            }
         }  
      }
   }
} 

void finalStop(){ 
    if( totalHistoryProfit > 0 && ( totalProfit + totalLoss ) < 0 && MathAbs( totalProfit + totalLoss )  > StopLoss * totalHistoryProfit ){
      closeAll();
   } else if( AccountEquity() / AccountBalance() < MaxDrawDown ) {
      closeAll();
   } 
}

void update(){
   display = "";   
   display = display + " Leverage: " + DoubleToStr( AccountLeverage(), 0 ); 
   display = display + " Open: " + DoubleToStr( totalTrades, 0 );  
   display = display + " Milestones: " + DoubleToStr( milestoneCount, 0 );   
   display = display + " Next: " + DoubleToStr( nextMilestone, 0 );
   display = display + " Equity: " + DoubleToStr( AccountEquity(), 2 ); 
   display = display + " Accumulated: " + DoubleToStr( accumulatedEquity, 2 );  
   display = display + " MileEquity: " + DoubleToStr( mileEquity, 2 ); 
   display = display + " Profit: " + DoubleToStr( totalProfit, 2 );
   display = display + " Loss: " + DoubleToStr( totalLoss, 2 );    
   Comment( display );
}

int start() { 
   prepare() ;  
   if( CloseAll ) closeAll() ;
   else {
      if( BackupSystem && totalTrades >= MaxStartTrades && AccountEquity() / AccountBalance() < TriggerBackSystem ) backSystem();
      else if( FrontSystem && totalTrades >= MaxStartTrades && totalHistoryProfit < 0 && MathAbs( totalHistoryProfit ) / AccountBalance() > TriggerFrontSystem ) frontSystem();
      else if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxStartTrades || MaxStartTrades == 0 ) ) openPosition() ; 
      managePositions() ;
      finalStop();
   }
   return( 0 ) ;
}