#property copyright "trevone"
#property link "Milestone 5M EURUSD 5min"   
#define MAGIC 20130719
bool NewSession=true; 
bool CloseAll=false; 
bool ContinueTrading=true; 
bool AccumulateMiles=true; 
int MaxStartTrades=1; 
int TradesPerStop=1; 
 
extern double MarginUsage=0.001; 
extern double IncrementLimits=1.5; 
extern double AccountLimit=3; 
extern double AccountReserve=1; 
extern double MinMarginLevel=500;
extern int OpenProfitTrades=10; 
extern int MaxBasketTrades=30; 
extern int QueryHistory=5; 
extern double TradeSpace=1.6; 
extern double MinProfit=1.5;
extern double BasketProfit=0.6;
extern double OpenProfit=0.9;
extern double  DrawDownProfit=0.4;
 extern int ATRPeriod=7;
 extern int  ATRShiftCheck=1; 
extern double   ADXMain=10; 
extern int   ADXPeriod=14;
 extern int   ADXShiftCheck=1; 
 extern int   MAPeriod=9;
 extern int    MAShiftCheck=3;

double slippage, marginRequirement, lotSize, totalHistoryProfit, totalHistoryLoss, maxBasketDrawDown, totalProfit, totalLoss, symbolHistory,
eATR, eATRPrev, eADXMain, eADXPlusDi, eADXMinusDi, eADXMainPrev, eADXPlusDiPrev, eADXMinusDiPrev, MA3Cur, MA3Prev;

int digits, totalTrades, totalBackupTrades;

int ATRTimeFrame = 0;
int ATRShift = 0; 
int ADXTimeFrame = 0;
int ADXShift = 0;
int MATimeFrame = 0;  
int MMAShift = 0;
int MAShift = 0;
int stopTrades = 0;
bool BackupSystem = true; 
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
double marginLevel = 0;

string display = "\n"; 

int init(){  
   maxBasketDrawDown = 0;
   accountLimit = AccountEquity() * AccountLimit;
   accountReserve = AccountEquity() * AccountReserve;
   if( !NewSession ){
      readMilestone();
      if( AccountFreeMargin() - accumulatedEquity < 0 ) defaultMilestone(); 
   } else {
      defaultMilestone();
      writeMilestone();
   }
   prepare(); 
   return( 0 );
}

void defaultMilestone(){
   nextMilestone = AccountEquity() + accountLimit;
   prevMilestone = AccountEquity();
   accumulatedEquity = 0; 
}

void writeMilestone(){
   int handle = FileOpen( MAGIC + "_milestone.csv", FILE_CSV|FILE_WRITE, ';' );
   if( handle > 0 ) {
      FileWrite( handle, nextMilestone, prevMilestone, accumulatedEquity, milestoneCount );
      FileClose( handle );
   } 
}

void readMilestone(){
   string strNextMilestone, strPrevMilestone, strAccumulatedEquity, strMilestoneCount;
   double valNextMilestone, valPrevMilestone, valAccumulatedEquity, valMilestoneCount;
   int handle = FileOpen( MAGIC + "_milestone.csv", FILE_CSV | FILE_READ );
   if( handle > 0 ) { 
      strNextMilestone = FileReadString( handle );
      valNextMilestone = StrToDouble( strNextMilestone );
      strPrevMilestone = FileReadString( handle );
      valPrevMilestone = StrToDouble( strPrevMilestone );
      strAccumulatedEquity = FileReadString( handle );
      valAccumulatedEquity = StrToDouble( strAccumulatedEquity );
      strMilestoneCount = FileReadString( handle );
      valMilestoneCount = StrToDouble( strMilestoneCount );
      nextMilestone = valNextMilestone;
      prevMilestone = valPrevMilestone;
      accumulatedEquity = valAccumulatedEquity; 
      milestoneCount = valMilestoneCount;
      FileClose( handle );
   } else {
      nextMilestone = AccountEquity() + accountLimit;
      prevMilestone = AccountEquity();
      accumulatedEquity = 0;
      milestoneCount = 0;
   } 
}

double marginCalculate( string symbol, double volume ){ 
   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
} 

void lotSize(){ 
   slippage = NormalizeDouble( ( eATR / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize ); 
   lotSize = NormalizeDouble( ( ( AccountFreeMargin() - accumulatedEquity ) * MarginUsage / marginRequirement ) * BaseLotSize, 2 ) ; 
   if( lotSize < 0.01 ) lotSize = 0.01;
} 

void setPipPoint(){
   digits = MarketInfo( Symbol(), MODE_DIGITS );
   if( digits == 3 ) pipPoints = 0.010;
   else if( digits == 5 ) pipPoints = 0.00010;
} 

void closeAll( string type = "none" ){
   for( int i = 0; i < OrdersTotal(); i++ ) {
   if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      if( OrderSymbol() == Symbol() ){ 
         RefreshRates();
         if( ( OrderStopLoss() == 0 && OrderProfit() > 0 && type == "profits" ) || type == "none" ){
            if( OrderProfit() > 0 ) stopTrades = stopTrades + 1;
            if( OrderType() == OP_BUY ) OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            if( OrderType() == OP_SELL ) OrderClose( OrderTicket(), OrderLots(), Ask, slippage ); 
         }
      }
   }
} 

void closeLargestLoss(){
   int closeTicket = 0;
   double closeLots = 0;
   double closeType = 0;
   double closeProfit = -999999999;  
   for( int i = 0 ; i < OrdersTotal() ; i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if( OrderSymbol() == Symbol() ){  
         if( OrderProfit() > closeProfit ) {
            closeProfit = OrderProfit();
            closeTicket = OrderTicket();
            closeLots = OrderLots();
            closeType = OrderType();   
         } 
      }
   }  
   if( closeTicket > 0 && stopTrades >= TradesPerStop ){
      if( closeType == OP_BUY ) OrderClose( closeTicket, closeLots, Bid, slippage );
      if( closeType == OP_SELL ) OrderClose( closeTicket, closeLots, Ask, slippage ); 
      stopTrades = 0; 
   } 
}

void prepareHistory(){
   symbolHistory = 0;
   totalHistoryProfit = 0;
   totalHistoryLoss = 0;
   for( int iPos = OrdersHistoryTotal() - 1; iPos > ( OrdersHistoryTotal() - 1 ) - totalHistory; iPos-- ){
      OrderSelect( iPos, SELECT_BY_POS, MODE_HISTORY ) ;
      double QueryHistoryDouble = ( double ) QueryHistory;
      if( symbolHistory >= QueryHistoryDouble ) break;
      if( OrderSymbol() == Symbol() ){
         totalHistoryProfit = totalHistoryProfit + OrderProfit() ;
         symbolHistory = symbolHistory + 1 ;
         if( OrderProfit() < 0 ) totalHistoryLoss = totalHistoryLoss + OrderProfit() ;
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
      if( eADXPlusDi > eADXMinusDi ){
         bullish = true;
         bearish = false; 
      } else if( eADXMinusDi > eADXPlusDi ){
         bullish = false;
         bearish = true; 
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
      if( OrderSymbol() == Symbol() && OrderStopLoss() == 0 ) {
         if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace ) nearLongPosition = true ;
         else if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace ) nearShortPosition = true ;
         if( OrderType() == OP_BUY ) buyLots = buyLots + OrderLots(); 
         else if( OrderType() == OP_SELL ) sellLots = sellLots + OrderLots(); 
         if( OrderProfit() > 0 ) totalProfit = totalProfit + OrderProfit();
         else totalLoss = totalLoss + OrderProfit();  
      }
   } 
   if( totalTrades == 0 ) maxBasketDrawDown = 0;
   if( maxBasketDrawDown > totalLoss ) maxBasketDrawDown = totalLoss;
   if( AccountMargin() > 0 ) marginLevel = AccountEquity() / AccountMargin() * 100  ; 
   else marginLevel = 0 ; 
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
   MA3Cur = iMA( NULL, MATimeFrame, MAPeriod, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );  
   MA3Prev = iMA( NULL, MATimeFrame, MAPeriod, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + MAShiftCheck ); 
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

void prepareMilestone(){  
   mileEquity = AccountEquity() - accumulatedEquity;
   if( AccountEquity() > nextMilestone && totalTrades == 0 ) {
      if( AccumulateMiles ){
         prevMilestone = nextMilestone;
         nextMilestone = AccountEquity() + ( accountLimit - accountReserve );
         accumulatedEquity = prevMilestone - ( accountLimit - accountReserve );
         milestoneCount = milestoneCount + 1;
         incrementLimits = true; 
         writeMilestone();
      } else ContinueTrading = false; 
   } else ContinueTrading = true;
}

void prepare(){ 
   prepareIndicators();
   prepareFractals();
   prepareTrend();
   setPipPoint(); 
   prepareHistory();
   preparePositions();
   prepareMilestone();
   lotSize();   
   update();  
} 

void openPosition(){ 
   if( AccountFreeMargin() - accumulatedEquity > 0 ){ 
      if( eATR > eATRPrev ){ 
         double tlots = NormalizeDouble( lotSize * ( ( MaxBasketTrades - basketCount ) / MaxBasketTrades ), 2 ); 
         if( !nearLongPosition && bullish && sellLots == 0 ) {
            if( basketNumberType != OP_BUY ) basketCount = 0;
            if( totalTrades < MaxBasketTrades ){
               if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return; 
               OrderSend( Symbol(), OP_BUY, lotSize, Ask, slippage, 0, 0, "basketNumber" + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
               basketCount = basketCount + 1;
               if( basketNumberType != OP_BUY ) basketNumber = basketNumber + 1; 
               basketNumberType = OP_BUY;
               if( totalTrades == 0 && milestoneCount > 0 && incrementLimits ){
                  accountLimit = accountLimit * IncrementLimits;
                  accountReserve = accountReserve * IncrementLimits;
                  incrementLimits = false;
               } 
            } 
         } else if( !nearShortPosition && bearish && buyLots == 0 ) {
            if( basketNumberType != OP_SELL ) basketCount = 0;
            if( totalTrades < MaxBasketTrades ){
               if( AccountFreeMarginCheck( Symbol(), OP_SELL, lotSize ) <= 0 || GetLastError() == 134 ) return; 
               OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "basketNumber" + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
               basketCount = basketCount + 1; 
               if( basketNumberType != OP_SELL ) basketNumber = basketNumber + 1; 
               basketNumberType = OP_SELL; 
               if( totalTrades == 0 && milestoneCount > 0 && incrementLimits ){
                  accountLimit = accountLimit * IncrementLimits;
                  accountReserve = accountReserve * IncrementLimits;
                  incrementLimits = false;
               } 
            }
         }
      } 
   }
} 

void backupSystem(){ 
   if( AccountFreeMargin() - accumulatedEquity > 0 ){  
      if( ( ContinueTrading || ( !ContinueTrading && totalBackupTrades > 0 ) ) && ( totalTrades < MaxBasketTrades ) ) {
         int type = -1;
         if( rangingMarket ){
            if( Close[0] >= fractalUpPrice && Close[0] >= MA3Cur ) type = OP_BUY;
            else if( Close[0] <= fractalDownPrice && Close[0] <= MA3Cur ) type = OP_SELL; 
         } else {
            if( Close[0] >= fractalUpPrice && Close[0] >= MA3Cur ) type = OP_SELL;
            else if( Close[0] <= fractalDownPrice && Close[0] <= MA3Cur ) type = OP_BUY; 
         }
         if( !nearLongPosition && type == OP_BUY && sellLots == 0  &&  bullish) {
            if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), type, lotSize, Ask, slippage, 0, 0, "backup", MAGIC ) ;
         } else if( !nearShortPosition && type == OP_SELL && buyLots == 0  && bearish  ) {
            if( AccountFreeMarginCheck( Symbol(), OP_SELL, lotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), type, lotSize, Bid, slippage, 0, 0, "backup", MAGIC ) ;
         }
       }
    }
} 

void managePositions(){
   if( marginLevel > 0 && marginLevel < MinMarginLevel ) closeLargestLoss();
   else if( totalHistoryLoss < 0 && totalProfit > MathAbs( totalHistoryLoss ) * BasketProfit ) closeAll( "profits" );
   else if( totalTrades > 1 && totalTrades <= OpenProfitTrades && totalProfit > MathAbs( maxBasketDrawDown ) * DrawDownProfit ) closeAll(); 
   else if( totalTrades > OpenProfitTrades && totalProfit > MathAbs( totalLoss ) * OpenProfit ) closeAll();
   else if( totalTrades > 0 && totalTrades <= MaxStartTrades && totalProfit > 0 && ( bullish && basketNumberType == OP_SELL ) || ( bearish && basketNumberType == OP_BUY ) ) closeAll(); 
   else { 
      for( int i = 0; i < OrdersTotal(); i++ ) {
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
   display = display + " HistoryLoss: " + DoubleToStr( totalHistoryLoss, 2 );
   display = display + " maxBasketDrawDown: " + DoubleToStr( maxBasketDrawDown, 2 ); 
   display = display + " marginLevel: " + DoubleToStr( marginLevel, 2 ); 
   display = display + " stopTrades: " + DoubleToStr( stopTrades, 0 );  
   Comment( display );
   
}

int start() { 
   prepare() ;  
   if( CloseAll ) closeAll() ;
   else { 
      if( BackupSystem && totalTrades >= MaxStartTrades ) backupSystem();
      else if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxStartTrades || MaxStartTrades == 0 ) ) openPosition();      
      managePositions() ;
   }
   return( 0 ) ;
}