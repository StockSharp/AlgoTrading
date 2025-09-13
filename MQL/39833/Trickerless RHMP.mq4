#property copyright "Copyright 2022, Lulus Arga Kustyarso"
#property link      "https://www.mql5.com/en/users/lulusargak"
#property version   "22.9"
#property strict  
int MAGIC = 20130721; 
extern string TOOLS = "TOOLS";  
extern bool CloseAll = false; 
extern bool ContinueTrading = true;   
extern string SAFE = "SAFE";
extern bool SafeSpread = true; 
extern bool SafeGrowth = true; 
extern bool SafeExits = true; 
extern bool AllowHedge = true; 
extern bool EnableStop = false;   
extern bool StopOnlyFriday = false;
extern string SIGNAL = "SIGNAL";  
extern bool SignalA = true; 
extern bool SignalB = true;
extern bool SignalC = true; 
extern string TIME = "TIME";
extern int RefreshHours = 26;
extern string NEWS = "NEWS";  
extern int NewsStartHour = 18; 
extern int NewsEndHour = 22;  
extern int SleepSeconds = 1440;
extern string PROFIT = "PROFIT";
extern double BasketProfit = 1.06;
extern double OpenProfit = 0.011;  
extern double MinProfit =  0.085;
extern double SafeProfit =  0.005;
extern string GROWTH = "GROWTH";
extern double StopGrowth =  0.075;  
extern double DailyGrowth = 0.045; 
extern string STOP = "STOP";
extern double RelativeStop = 0.19;
extern string HISTORY = "HISTORY";
extern int QueryHistory = 14;   
extern string TREND = "TREND";
extern double MinTrend = 2; 
extern double MaxTrend = 9;  
extern double CandleSpike = 7;
extern string BACK_SYSTEM = "BACK_SYSTEM";
extern double TriggerBackSystem = 0.995;
extern double TrendSpace = 5; 
extern string MARGIN = "MARGIN";  
extern double MinMarginLevel = 300;
extern double MarginUsage = 0.03;
extern double BackupMargin = 0.05;
extern double NewsMargin = 0.04;
extern string TRADE = "TRADE"; 
extern double MinLots = 0.03;
extern double TradeSpace = 3.5;
extern double MaxSpread = 7; 
extern string INDICATOR_ATR = "INDICATOR_ATR"; 
extern int ATRPeriod = 14;  
extern string INDICATOR_ADX = "INDICATOR_ADX";
extern double ADXMain = 10;  
extern int ADXPeriod = 14; 
extern int ADXShiftCheck = 4; 
extern string INDICATOR_MA = "INDICATOR_MA";  
extern int MA1Period = 120; 
extern int MA2Period = 60;
extern int MAShiftCheck = 30; 

double slippage, marginRequirement, lotSize, backupLotSize, newsLotSize, totalHistoryProfit, totalProfit, totalLoss, symbolHistory, ical,
eATR, eADXMain, eADXPlusDi, eADXMinusDi, eADXMainPrev, eADXPlusDiPrev, eADXMinusDiPrev, MA1Cur, MA1Prev,MA2Cur, MA2Prev; 
int digits, totalTrades, totalBackupTrades; 
bool nearLongPosition = false;
bool nearShortPosition = false;  
bool rangingMarket = false;
bool bullish = false;
bool bearish = false; 
bool incrementLimits = false;
int MaxStartTrades = 1;  
int ATRTimeFrame = 0;
int ATRShift = 0; 
int ADXTimeFrame = 0;
int ADXShift = 0;
int MATimeFrame = 0; 
int lastTradeTime = 0;
int MMAShift = 0;
int MAShift = 0; 
int totalHistory = 100;
int basketNumber = 0;
int basketNumberType = -1;
int basketCount = -1;  
int MaxTrades = 1;  
double buyLots = 0;
double sellLots = 0;
double pipPoints = 0.00010;  
double DynamicSlippage = 1;   
double BaseLotSize = 0.01;     
double marginLevel = 0;
double spread = 0;
double trendStrength = 0;
double longHistoryProfit = 0;  
double dailyGrowth = 0;
string display = "\n";  

int init(){   
   prepare();  
   return( 0 );
} 

double marginCalculate( string symbol, double volume ){ 
   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
} 

void lotSize(){   
   spread = ( Ask - Bid ) / pipPoints;
   slippage = NormalizeDouble( ( eATR / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize ); 
   lotSize = NormalizeDouble( ( AccountBalance() * MarginUsage / marginRequirement ) * BaseLotSize, 2 ) ; 
   backupLotSize = NormalizeDouble( ( AccountBalance() * BackupMargin / marginRequirement ) * BaseLotSize, 2 ) ; 
   newsLotSize = NormalizeDouble( ( AccountBalance() * NewsMargin / marginRequirement ) * BaseLotSize, 2 ) ; 
   if( lotSize < MinLots ) lotSize = MinLots;  
   if( backupLotSize < MinLots ) backupLotSize = MinLots;
   if( newsLotSize < MinLots ) newsLotSize = MinLots;  
   if( AccountMargin() > 0 ) marginLevel = AccountEquity() / AccountMargin() * 100  ; 
   if( totalTrades == 0 ) marginLevel = 0;
   if( MathMod( TimeCurrent(), 3600 * RefreshHours ) <= 60 ){ 
      if( dailyGrowth / AccountBalance()  > DailyGrowth ) Print( "Daily growth reached" );
      dailyGrowth = 0; 
      if( totalProfit + totalLoss > 0 ) closeAll();
   } 
   if( SafeGrowth ) if( dailyGrowth / AccountBalance() > DailyGrowth  )closeAll();
} 

void setPipPoint(){
   digits = MarketInfo( Symbol(), MODE_DIGITS );
   if( digits == 3 ) pipPoints = 0.010;
   else if( digits == 5 ) pipPoints = 0.00010;
} 

void closeAll( string type = "none" ){
   if( totalTrades == 1 ) lastTradeTime = TimeCurrent();
   for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
   if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;
      if( OrderSymbol() == Symbol() ){ 
         RefreshRates();
         if( ( OrderStopLoss() == 0 && OrderProfit() > 0 && type == "profits" ) || type == "none" ){
            if( OrderType() == OP_BUY ){
             OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
             dailyGrowth = dailyGrowth + OrderProfit();
             lastTradeTime = TimeCurrent();
            }
            if( OrderType() == OP_SELL ) {
               OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
               dailyGrowth = dailyGrowth + OrderProfit();
               lastTradeTime = TimeCurrent();
            }
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
      if( SignalA ){
         if( MathAbs( trendStrength ) > MinTrend * pipPoints && MathAbs( trendStrength ) < MaxTrend * pipPoints && MathAbs( Close[0] - MA1Cur ) > TrendSpace * pipPoints ){
            if( MA1Cur < MA2Cur && MA2Cur > MA2Prev && Close[0] < MA2Cur ) {
               bullish = true;  
               bearish = false;
            } else if( MA1Cur > MA2Cur && MA2Cur < MA2Prev && Close[0] > MA2Cur ) {
               bearish = true;
               bullish = false;   
            }
         }
      }
      if( SignalB ){ 
         if( MA1Cur < MA2Cur && eADXPlusDi > ADXMain && eADXPlusDiPrev < eADXMinusDi && Close[0] < MA2Cur ) {
            bullish = true;  
            bearish = false;
         } else if( MA1Cur > MA2Cur && eADXMinusDi > ADXMain && eADXMinusDiPrev < eADXMinusDi && Close[0] > MA2Cur ) {
            bearish = true;
            bullish = false;   
         } 
      } 
      if( SignalC ){
         if( MathAbs( trendStrength ) > MaxTrend * pipPoints ) {
            if( MA1Cur < MA2Cur && MA2Cur > MA2Prev ) {
               bearish = true;
               bullish = false; 
            } else if( MA1Cur > MA2Cur && MA2Cur < MA2Prev ) {
               bullish = true;  
               bearish = false; 
            }
         }
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
   for( int i = 0 ; i < OrdersTotal(); i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if( OrderSymbol() == Symbol() ) totalTrades = totalTrades + 1;
      if( OrderSymbol() == Symbol() && OrderComment() == "Trickerless RHMP Backup" ) totalBackupTrades = totalBackupTrades + 1;
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
   trendStrength = MA1Cur - MA1Prev;
}   

void prepare(){ 
   prepareIndicators(); 
   prepareTrend();
   setPipPoint(); 
   prepareHistory();
   preparePositions();  
   lotSize();   
   update();  
} 

void sendOpen(){
   if( ( SafeSpread && spread < MaxSpread ) || !SafeSpread ){
      if( !nearLongPosition && bullish && sellLots == 0 ) {
        if( basketNumberType != OP_BUY ) basketCount = 0;
        if( basketCount < MaxTrades ){
           if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
           OrderSend( Symbol(), OP_BUY , lotSize, Ask, slippage, 0, 0, "Trickerless RHMP Min " + DoubleToStr( basketNumber, 0 ), MAGIC ) ;
           lastTradeTime = TimeCurrent(); 
           basketCount = basketCount + 1;
           if( basketNumberType != OP_BUY ) basketNumber = basketNumber + 1; 
           basketNumberType = OP_BUY;  
         } 
      } else if( !nearShortPosition && bearish && buyLots == 0 ) {
         if( basketNumberType != OP_SELL ) basketCount = 0;
         if( basketCount < MaxTrades ){
            if( AccountFreeMarginCheck( Symbol(), OP_SELL, lotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "Trickerless RHMP Min " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
            basketCount = basketCount + 1; 
            if( basketNumberType != OP_SELL ) basketNumber = basketNumber + 1; 
            basketNumberType = OP_SELL;  
         }
      }  
   }
}

void openPosition(){    
   sendOpen(); 
} 

void sendBack(){
   if( ( ContinueTrading || ( !ContinueTrading && totalBackupTrades > 0 ) ) && ( totalBackupTrades < MaxTrades ) ) {
      if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs(High[1] - Low[1] ) && Open[0] < Close[0] && Close[0] < ( High[0] + Low[0] ) / 2 && ( ( !AllowHedge && sellLots == 0 ) || AllowHedge ) ){
         if( AccountFreeMarginCheck( Symbol(), OP_BUY, backupLotSize ) <= 0 || GetLastError() == 134 ) return;
         OrderSend( Symbol(), OP_BUY , backupLotSize, Ask, slippage, 0, 0, "Trickerless RHMP Backup " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
         lastTradeTime = TimeCurrent();
      } 
      if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs(High[1] - Low[1] ) && Open[0] > Close[0] && Close[0] > ( High[0] + Low[0] ) / 2 && ( ( !AllowHedge && buyLots == 0 ) || AllowHedge ) ){
         if( AccountFreeMarginCheck( Symbol(), OP_SELL, backupLotSize ) <= 0 || GetLastError() == 134 ) return;
         OrderSend( Symbol(), OP_BUY , backupLotSize, Ask, slippage, 0, 0, "Trickerless RHMP Backup " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
         lastTradeTime = TimeCurrent();
      }
   }
}

void backSystem(){ 
   sendBack(); 
}

void sendNews(){
   if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxTrades )  ) { 
      if( !nearLongPosition  && sellLots == 0 ) {
         if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs( High[1] - Low[1] ) && Open[0] < Close[0] && Close[0] < ( High[0] + Low[0] ) / 2 ){ 
            if( AccountFreeMarginCheck( Symbol(), OP_BUY, newsLotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_BUY , newsLotSize, Ask, slippage, 0, 0, "Trickerless RHMP News " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
            lastTradeTime = TimeCurrent();
         } 
      }
      if( !nearShortPosition  && buyLots == 0 ) {
         if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs( High[1] - Low[1] ) && Open[0] > Close[0] && Close[0] > ( High[0] + Low[0] ) / 2 ){
            if( AccountFreeMarginCheck( Symbol(), OP_SELL, newsLotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_BUY , newsLotSize, Ask, slippage, 0, 0, "Trickerless RHMP News " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
            lastTradeTime = TimeCurrent();
         }
      }
   }
}

void newsSystem(){ 
   if( Hour() >= NewsStartHour && Hour() < NewsEndHour ) sendNews(); 
}

void managePositions(){ 
   if( totalHistoryProfit < 0 && totalProfit > MathAbs( totalHistoryProfit ) * BasketProfit ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit + totalLoss > OpenProfit * AccountBalance() ) closeAll();
   else if( SafeExits && totalTrades > 0 && totalProfit + totalLoss > MathAbs( totalProfit + totalLoss ) > SafeProfit * AccountBalance() && ( ( bullish && basketNumberType == OP_SELL ) || ( bearish && basketNumberType == OP_BUY ) ) ) closeAll(); 
   else { 
      for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol()  ) {  
            if( totalTrades <= MaxStartTrades ){
               if( OrderType() == OP_BUY && Bid > OrderOpenPrice() && OrderProfit() > MinProfit * AccountBalance() ) {
                  OrderClose( OrderTicket(), OrderLots(), Bid, slippage ); 
                  dailyGrowth = dailyGrowth + OrderProfit();
                  lastTradeTime = TimeCurrent();
               } else if( OrderType() == OP_SELL && Ask < OrderOpenPrice() && OrderProfit() > MinProfit * AccountBalance() ){
                  OrderClose( OrderTicket(), OrderLots(), Ask, slippage );   
                  dailyGrowth = dailyGrowth + OrderProfit();
                  lastTradeTime = TimeCurrent();
               }
            }
         }  
      }
   }
}   
 
void longStop(){
   if( ( EnableStop || ( StopOnlyFriday && DayOfWeek() == 5 ) ) && totalHistoryProfit > StopGrowth * AccountBalance() && ( totalProfit + totalLoss ) < 0 && MathAbs( totalProfit + totalLoss ) > RelativeStop * totalHistoryProfit ) closeAll(); 
}
 
void update(){
   display = "";      
   display = display + "\n Daily Growth: " +  DoubleToStr( dailyGrowth / AccountBalance() * 100, 1 )+ "% of " + DoubleToStr( DailyGrowth * 100, 1 ) + "%"; 
   display = display + " Spread: " + DoubleToStr( spread, 1 );    
   if( dailyGrowth / AccountBalance() > DailyGrowth ) display = display + " No more risk " ; 
   
   if( ObjectFind("hud") == -1 ) ObjectCreate( "hud", OBJ_LABEL, 0, 0, 0 );     
	ObjectSetText( "hud", display, 10, "Arial Bold", LightGray ); 
	ObjectSet( "hud", OBJPROP_XDISTANCE, 6 );
	ObjectSet( "hud", OBJPROP_COLOR, LightGray );     
}

int start() { 
   prepare() ;   
   if( CloseAll ) closeAll() ;
   else {  
      if( dailyGrowth / AccountBalance() < DailyGrowth && TimeCurrent() - lastTradeTime > SleepSeconds && ( marginLevel == 0 || marginLevel > MinMarginLevel ) ){  
         if( totalTrades >= MaxStartTrades && ( AccountBalance() + ( totalProfit + totalLoss ) ) / AccountBalance() < TriggerBackSystem ) backSystem();
         else if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxStartTrades || MaxStartTrades == 0 ) ) openPosition();  
      }
      managePositions() ; 
      longStop();
   }
   return( 0 ) ;
}