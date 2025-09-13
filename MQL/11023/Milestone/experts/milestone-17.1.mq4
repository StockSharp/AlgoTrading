#property copyright "trevone"
#property link "Milestone" 

int MAGIC = 20130721;
bool CloseAll = false;
bool BackupSystem = true;   


extern string FFCAL = ".............................................................................................................";
extern bool FFCal = false;
extern bool IncludeHigh = true;
extern bool IncludeMedium = false;
extern bool IncludeLow = false;
extern bool IncludeSpeaks = true; 
extern string TRADE_BEFORE_AFTER = ".............................................................................................................";
extern int LeadTime = 1440;  
extern int TrailTime = 1440; 
extern double OpenMarginLevel = 300;
extern string TOOLS = ".............................................................................................................";  
extern bool ContinueTrading = true; 
extern string SIGNAL = ".............................................................................................................";  
extern bool SignalModeA = true; 
extern bool SignalModeB = true;
extern bool SignalModeC = true;
extern string TIME = ".............................................................................................................";
extern int Refresh = 24;
extern int SleepAfterTrade = 1440;
extern string GROWTH = ".............................................................................................................";
extern double StopGrowth =  0.03;  
extern double DailyGrowth = 0.01; 
extern string HISTORY = ".............................................................................................................";
extern int QueryHistory = 14;  
extern string PROFIT = ".............................................................................................................";
extern double BasketProfit = 1.8;
extern double OpenProfit = 0.05;  
extern double MinProfit =  0.005;
extern string STOP = ".............................................................................................................";
extern double RelativeStop = 0.5;
extern string TREND = ".............................................................................................................";
extern double MinTrend = 9; 
extern double MaxTrend = 19;  
extern string BACK_SYSTEM = ".............................................................................................................";
extern double TriggerBackSystem = 0.995;
extern double TrendSpace = 0.0005; 
extern string RISK = ".............................................................................................................";  
extern double MarginUsage = 0.03;
extern double MinLots = 0.03;
extern double TradeSpace = 1.5;
extern int MaxTrades = 2; 
extern string INDICATOR_ATR = "............................................................................................................."; 
extern int ATRPeriod = 14; 
extern int ATRShiftCheck = 2; 
extern string INDICATOR_ADX = ".............................................................................................................";
extern double ADXMain = 11;  
extern int ADXPeriod = 14; 
extern int ADXShiftCheck = 1; 
extern string INDICATOR_MA = "............................................................................................................."; 
extern int MA2Period = 60;
extern int MA1Period = 120; 
extern int MAShiftCheck = 30;  

int ATRTimeFrame = 0;
int ATRShift = 0; 
int ADXTimeFrame = 0;
int ADXShift = 0;
int MATimeFrame = 0; 
int lastTradeTime = 0;
int MMAShift = 0;
int MAShift = 0;

double trendStrength = 0;
double longHistortProfit = 0;  
double dailyGrowth = 0;


double slippage, marginRequirement, lotSize, totalHistoryProfit, totalProfit, totalLoss, symbolHistory, ical,
eATR, eATRPrev, eADXMain, eADXPlusDi, eADXMinusDi, eADXMainPrev, eADXPlusDiPrev, eADXMinusDiPrev, MA1Cur, MA1Prev,MA2Cur, MA2Prev;

int digits, totalTrades, totalBackupTrades;

bool nearLongPosition = false;
bool nearShortPosition = false;  
bool rangingMarket = false;
bool bullish = false;
bool bearish = false; 
bool incrementLimits = false;
int MaxStartTrades = 1;  
int totalHistory = 100;
int basketNumber = 0;
int basketNumberType = -1;
int basketCount = -1; 
double buyLots = 0;
double sellLots = 0;
double pipPoints = 0.00010;  
double DynamicSlippage = 1;   
double BaseLotSize = 0.01;  
string display = "\n";  

string milestoneCurrency1 = "";
string milestoneCurrency2 = "";

string milestoneText1 = "";
string milestoneText2 = "";

int milestoneType1 = -1;
int milestoneType2 = -1;

int milestoneImpact1 = -1;
int milestoneImpact2 = -1;

double milestoneHours1 = -1;
double milestoneHours2 = -1;

double milestoneMinutes1 = -1;
double milestoneMinutes2 = -1;

double ffCalenadarEventTime1 = 0; 
double ffCalenadarEventTime2 = 0;

double marginLevel = 0;

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
   if( lotSize < MinLots ) lotSize = MinLots;    
   if( AccountMargin() > 0 ) marginLevel = AccountEquity() / AccountMargin() * 100  ; 
   if( totalTrades == 0 ) marginLevel = 0;
   if( MathMod( TimeCurrent(), 3600 * Refresh ) <= 60 ){ 
      if( dailyGrowth / AccountBalance()  > DailyGrowth ) Print( "Daily growth reached" );
      dailyGrowth = 0; 
      if( totalProfit + totalLoss > 0 ) closeAll();
   } 
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
      if( SignalModeA ){
         if( MathAbs( trendStrength ) > MinTrend*pipPoints && MathAbs( trendStrength )  < MaxTrend*pipPoints && MathAbs( Close[0] - MA1Cur ) > TrendSpace ){
            if( MA1Cur < MA2Cur && MA2Cur > MA2Prev && Close[0] < MA2Cur ) {
               bullish = true;  
               bearish = false;
            } else if( MA1Cur > MA2Cur && MA2Cur < MA2Prev && Close[0] > MA2Cur ) {
               bearish = true;
               bullish = false;   
            }
         }
      }
      if( SignalModeB ){ 
         if( MA1Cur < MA2Cur && eADXPlusDi > ADXMain && eADXPlusDiPrev < eADXMinusDi && Close[0] < MA2Cur ) {
            bullish = true;  
            bearish = false;
         } else if( MA1Cur > MA2Cur && eADXMinusDi > ADXMain && eADXMinusDiPrev < eADXMinusDi && Close[0] > MA2Cur ) {
            bearish = true;
            bullish = false;   
         } 
      }
      
      if( SignalModeC ){
         if( MathAbs( trendStrength ) > MaxTrend * pipPoints  ) {
            if( MA1Cur < MA2Cur && MA2Cur > MA2Prev && ( Open[0] > ObjectGet( "OverPriced", OBJPROP_PRICE1 ) || totalTrades == 0 ) ) {
               bearish = true;
               bullish = false; 
            } else if( MA1Cur > MA2Cur && MA2Cur < MA2Prev && ( Open[0] < ObjectGet( "UnderPriced", OBJPROP_PRICE1 ) || totalTrades == 0 ) ) {
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
   if( FFCal ) ical = iCustom( NULL, PERIOD_M5, "milestone_calendar", IncludeHigh, IncludeMedium, IncludeLow, IncludeSpeaks, 0, 0 ); 
   trendStrength = MA1Cur - MA1Prev;
} 

void prepareCalendar(){ 
   milestoneCurrency1 = ObjectDescription( "milestoneCurrency1" ); 
   milestoneCurrency2 = ObjectDescription( "milestoneCurrency2" );   
   milestoneText1 = ObjectDescription( "milestoneText1" ); 
   milestoneText2 = ObjectDescription( "milestoneText2" );  
   if( ObjectDescription( "milestoneType1" ) == "since " ) milestoneType1 = 0; 
   if( ObjectDescription( "milestoneType1" ) == "until " ) milestoneType1 = 1;  
   if( ObjectDescription( "milestoneType2" ) == "since " ) milestoneType2 = 0;  
   if( ObjectDescription( "milestoneType2" ) == "until " ) milestoneType2 = 1;   
   if( ObjectDescription( "milestoneImpact1" ) == "High" ) milestoneType1 = 0; 
   if( ObjectDescription( "milestoneImpact1" ) == "Medium" ) milestoneType1 = 1; 
   if( ObjectDescription( "milestoneImpact1" ) == "Low" ) milestoneType2 = 2; 
   if( ObjectDescription( "milestoneImpact1" ) == "Speaks" || ObjectDescription( "milestoneImpact1" ) == "speaks" ) milestoneType2 = 3; 
   milestoneHours1 = StrToDouble( ObjectDescription( "milestoneHours1" ) ); 
   milestoneMinutes1 = StrToDouble( ObjectDescription( "milestoneMinutes1" ) ); 
   milestoneHours2 = StrToDouble( ObjectDescription( "milestoneHours2" ) ); 
   milestoneMinutes2 = StrToDouble( ObjectDescription( "milestoneMinutes2" ) ); 
   if( milestoneMinutes1 > 0 ) ffCalenadarEventTime1 = ( milestoneHours1 * 60 ) + milestoneMinutes1; 
   else ffCalenadarEventTime1 = 0;
   if( milestoneMinutes2 > 0 ) ffCalenadarEventTime2 = ( milestoneHours2 * 60 ) + milestoneMinutes2; 
   else ffCalenadarEventTime2 = 0;
} 

 

void prepare(){ 
   prepareIndicators(); 
   prepareCalendar();
   prepareTrend();
   setPipPoint(); 
   prepareHistory();
   preparePositions();  
   lotSize();   
   update();  
} 

void openPosition(){  
   if( ( FFCal && ( ( ffCalenadarEventTime1 > LeadTime && milestoneType1 == 1 && ffCalenadarEventTime1 > 0 ) ) ) || !FFCal ){
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
} 

void backSystem(){ 
   if( ( FFCal && ( ( ffCalenadarEventTime1 > LeadTime && milestoneType1 == 1 && ffCalenadarEventTime1 > 0 ) ) ) || !FFCal ){
      if( ( ContinueTrading || ( !ContinueTrading && totalBackupTrades > 0 ) ) && ( totalBackupTrades < MaxTrades ) ) {
         int type = -1;
         if( bullish ) type = OP_BUY;
         else if( bearish ) type = OP_SELL; 
         if( !nearLongPosition && type == OP_BUY && sellLots == 0 ) {
            OrderSend( Symbol(), type, lotSize, Ask, slippage, 0, 0, "backup", MAGIC ) ; 
         } else if( !nearShortPosition && type == OP_SELL && buyLots == 0 ){
            OrderSend( Symbol(), type , lotSize, Bid, slippage, 0, 0, "backup", MAGIC ) ; 
         }
       } 
    }
}

void managePositions(){
   if( totalHistoryProfit < 0 && totalProfit > MathAbs( totalHistoryProfit ) * BasketProfit ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit + totalLoss > OpenProfit * AccountBalance() ) closeAll();
   else if( totalTrades > 0 && totalProfit + totalLoss > 0 && ( ( bullish && basketNumberType == OP_SELL ) || ( bearish && basketNumberType == OP_BUY ) ) ) closeAll(); 
   else if( totalTrades > 0 && totalProfit + totalLoss > 0 && ( ( FFCal && ( ffCalenadarEventTime1 < LeadTime && milestoneType1 == 1 && ffCalenadarEventTime1 > 0 ) || !FFCal ) ) ) closeAll();
   else { 
      for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol()  ) {  
            if( totalTrades <= MaxStartTrades ){
               if( OrderType() == OP_BUY && Bid > OrderOpenPrice() &&  OrderProfit() > MinProfit * AccountBalance() ) {
                  OrderClose( OrderTicket(), OrderLots(), Bid, slippage ); 
                  dailyGrowth = dailyGrowth + OrderProfit();
                  lastTradeTime = TimeCurrent();
               }   
               else if( OrderType() == OP_SELL && Ask < OrderOpenPrice() &&  OrderProfit() > MinProfit * AccountBalance() ){
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
   if( totalHistoryProfit > StopGrowth * AccountBalance() && ( totalProfit + totalLoss ) < 0 && MathAbs( totalProfit + totalLoss ) > RelativeStop * totalHistoryProfit ){
      closeAll(); 
   }  
}
 
void update(){
   display = "";    
   display = display + " Equity: " + DoubleToStr( ( AccountBalance() + ( totalProfit + totalLoss ) ), 2 );     
   display = display + "\n DailyGrowth: " +  DoubleToStr( dailyGrowth / AccountBalance() * 100, 1 )+ "% of " + DoubleToStr( DailyGrowth * 100, 1 ) + "%"; 
   if( dailyGrowth / AccountBalance() > DailyGrowth ) display = display + " No more risk " ; 
   if( FFCal && ( ffCalenadarEventTime1 < LeadTime && milestoneType1 == 1 ) || !FFCal ) display = display + " exit calendar" ; 
   if( FFCal && ( ( ffCalenadarEventTime1 > LeadTime && milestoneType1 == 1 ) || ( ffCalenadarEventTime2 > TrailTime && milestoneType1 == 0 ) ) ) display = display + " waiting calendar" ; 
   if( !FFCal ) display = display + " calendar disabled " ; 
   if( ObjectFind("hud") == -1 ){
      ObjectCreate( "hud", OBJ_LABEL, 0, 0, 0 );  
		ObjectSet( "hud", OBJPROP_XDISTANCE, 6 );
		ObjectSet( "hud", OBJPROP_COLOR, Orange );  
	} 
	if( FFCal ) ObjectSet( "hud", OBJPROP_YDISTANCE, 175 ); 
   else ObjectSet( "hud", OBJPROP_YDISTANCE, 20 );  
	ObjectSetText( "hud", display, 10, "Arial Bold", LightGray ); 
}

int start() { 
   prepare() ;  
   if( CloseAll ) closeAll() ;
   else {  
      if( dailyGrowth / AccountBalance() < DailyGrowth &&  TimeCurrent() - lastTradeTime > SleepAfterTrade && ( marginLevel == 0 || marginLevel > OpenMarginLevel ) ){  
         if( BackupSystem && totalTrades >= MaxStartTrades && ( AccountBalance() + ( totalProfit + totalLoss ) ) / AccountBalance() < TriggerBackSystem ) backSystem();
         else if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxStartTrades || MaxStartTrades == 0 ) ) openPosition();  
      }
      managePositions() ; 
      longStop();
   }
   return( 0 ) ;
}