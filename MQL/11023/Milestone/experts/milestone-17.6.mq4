#property copyright "trevone"
#property link "Milestone 17.4"  
int MAGIC = 20130721; 
extern string TOOLS = ".............................................................................................................";  
extern bool CloseAll = false; 
extern bool ContinueTrading = true; 
extern string SAFE = ".............................................................................................................";
extern bool SafeSpread = true; 
extern bool SafeGrowth = false; 
extern bool SafeExits = true; 
extern bool EnableStop = false; 
extern string CALENDAR = ".............................................................................................................";
extern bool EnableCalendar = true;
extern bool IncludeHigh = true;
extern bool IncludeMedium = false;
extern bool IncludeLow = false;
extern bool IncludeSpeaks = true;  
extern string NEWS = ".............................................................................................................";  
extern int NewsStartHour = 18; 
extern int NewsEndHour = 22;  
extern string SIGNAL = ".............................................................................................................";  
extern bool SignalA = true; 
extern bool SignalB = true;
extern bool SignalC = true;
extern bool Aggressive = false; 
extern string TIME = ".............................................................................................................";
extern int RefreshHours = 24; 
extern int NewsReleaseMinutes = 15;
extern int LeadCalendarMinutes = 60;  
extern int TrailCalendarMinutes = 30; 
extern int SleepSeconds = 1440;
extern string GROWTH = ".............................................................................................................";
extern double StopGrowth =  0.025;  
extern double DailyGrowth = 0.015; 
extern string HISTORY = ".............................................................................................................";
extern int QueryHistory = 14;  
extern string PROFIT = ".............................................................................................................";
extern double BasketProfit = 1.3;
extern double OpenProfit = 0.05;  
extern double MinProfit =  0.005;
extern double SafeProfit =  0.007;
extern string STOP = ".............................................................................................................";
extern double RelativeStop = 0.2;
extern string TREND = ".............................................................................................................";
extern double MinTrend = 3; 
extern double MaxTrend = 7;  
extern double CandleSpike = 5;
extern string BACK_SYSTEM = ".............................................................................................................";
extern double TriggerBackSystem = 0.995;
extern double TrendSpace = 5; 
extern string MARGIN = ".............................................................................................................";  
extern double MinMarginLevel = 300;
extern double MarginUsage = 0.02;
extern double BackupMargin = 0.03;
extern double NewsMargin = 0.04;
extern string TRADE = "............................................................................................................."; 
extern double MinLots = 0.03;
extern double TradeSpace = 1.5;
extern double MaxSpread = 5; 
extern string INDICATOR_ATR = "............................................................................................................."; 
extern int ATRPeriod = 14;  
extern string INDICATOR_ADX = ".............................................................................................................";
extern double ADXMain = 11;  
extern int ADXPeriod = 14; 
extern int ADXShiftCheck = 1; 
extern string INDICATOR_MA = ".............................................................................................................";  
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
int milestoneType1 = -1;
int milestoneType2 = -1; 
int milestoneImpact1 = -1;
int milestoneImpact2 = -1; 
int MaxTrades = 1;  
double buyLots = 0;
double sellLots = 0;
double pipPoints = 0.00010;  
double DynamicSlippage = 1;   
double BaseLotSize = 0.01;    
double milestoneHours1 = -1;
double milestoneHours2 = -1; 
double milestoneMinutes1 = -1;
double milestoneMinutes2 = -1; 
double ffCalenadarEventTime1 = 0; 
double ffCalenadarEventTime2 = 0; 
double marginLevel = 0;
double spread = 0;
double trendStrength = 0;
double longHistortProfit = 0;  
double dailyGrowth = 0;
string display = "\n"; 
string milestoneCurrency1 = "";
string milestoneCurrency2 = ""; 
string milestoneText1 = "";
string milestoneText2 = "";  

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
      if( OrderSymbol() == Symbol() && OrderComment() == "Milestone 17.4 Backup" ) totalBackupTrades = totalBackupTrades + 1;
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
   if( EnableCalendar ) ical = iCustom( NULL, PERIOD_M5, "milestone_calendar", IncludeHigh, IncludeMedium, IncludeLow, IncludeSpeaks, 0, 0 ); 
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

void sendOpen(){
   if( ( SafeSpread && spread < MaxSpread ) || !SafeSpread ){
      if( !nearLongPosition && bullish && sellLots == 0 ) {
        if( basketNumberType != OP_BUY ) basketCount = 0;
        if( basketCount < MaxTrades ){
           if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
           OrderSend( Symbol(), OP_BUY , lotSize, Ask, slippage, 0, 0, "Milestone 17.4 Min " + DoubleToStr( basketNumber, 0 ), MAGIC ) ;
           lastTradeTime = TimeCurrent(); 
           basketCount = basketCount + 1;
           if( basketNumberType != OP_BUY ) basketNumber = basketNumber + 1; 
           basketNumberType = OP_BUY;  
         } 
      } else if( !nearShortPosition && bearish && buyLots == 0 ) {
         if( basketNumberType != OP_SELL ) basketCount = 0;
         if( basketCount < MaxTrades ){
            if( AccountFreeMarginCheck( Symbol(), OP_SELL, lotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "Milestone 17.4 Min " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
            basketCount = basketCount + 1; 
            if( basketNumberType != OP_SELL ) basketNumber = basketNumber + 1; 
            basketNumberType = OP_SELL;  
         }
      }  
   }
}

void openPosition(){    
   if( EnableCalendar && ffCalenadarEventTime1 < NewsReleaseMinutes && ObjectDescription( "milestoneType1" ) == "since" && ffCalenadarEventTime1 > 0 ) sendOpen();
   else if( EnableCalendar && ffCalenadarEventTime1 > TrailCalendarMinutes && ObjectDescription( "milestoneType1" ) == "since" && ffCalenadarEventTime1 > 0 ) sendOpen();
   else sendOpen(); 
} 

void sendBack(){
   if( ( ContinueTrading || ( !ContinueTrading && totalBackupTrades > 0 ) ) && ( totalBackupTrades < MaxTrades ) ) {
      if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs(High[1] - Low[1] ) && Open[0] < Close[0] && Close[0] < ( High[0] + Low[0] ) / 2 ){
         if( AccountFreeMarginCheck( Symbol(), OP_BUY, backupLotSize ) <= 0 || GetLastError() == 134 ) return;
         OrderSend( Symbol(), OP_BUY , backupLotSize, Ask, slippage, 0, 0, "Milestone 17.4 Backup " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
         if( !Aggressive ) lastTradeTime = TimeCurrent();
      } 
      if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs(High[1] - Low[1] ) && Open[0] > Close[0] && Close[0] > ( High[0] + Low[0] ) / 2 ){
         if( AccountFreeMarginCheck( Symbol(), OP_SELL, backupLotSize ) <= 0 || GetLastError() == 134 ) return;
         OrderSend( Symbol(), OP_BUY , backupLotSize, Ask, slippage, 0, 0, "Milestone 17.4 Backup " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
         if( !Aggressive ) lastTradeTime = TimeCurrent();
      }
   }
}

void backSystem(){ 
   if( EnableCalendar && ffCalenadarEventTime1 < NewsReleaseMinutes && ObjectDescription( "milestoneType1" ) == "since" && ffCalenadarEventTime1 > 0 ) sendBack();
   else if( EnableCalendar && ffCalenadarEventTime1 > TrailCalendarMinutes && ObjectDescription( "milestoneType1" ) == "since" && ffCalenadarEventTime1 > 0 ) sendBack();
   else sendBack(); 
}

void sendNews(){
   if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxTrades )  ) { 
      if( !nearLongPosition  && sellLots == 0 ) {
         if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs(High[1] - Low[1] ) && Open[0] < Close[0] && Close[0] < ( High[0] + Low[0] ) / 2 ){ 
            if( AccountFreeMarginCheck( Symbol(), OP_BUY, newsLotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_BUY , newsLotSize, Ask, slippage, 0, 0, "Milestone 17.4 News " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
            lastTradeTime = TimeCurrent();
         } 
      }
      if( !nearShortPosition  && buyLots == 0 ) {
         if( MathAbs( High[0] - Low[0] ) > CandleSpike * MathAbs(High[1] - Low[1] ) && Open[0] > Close[0] && Close[0] > ( High[0] + Low[0] ) / 2 ){
            if( AccountFreeMarginCheck( Symbol(), OP_SELL, newsLotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_BUY , newsLotSize, Ask, slippage, 0, 0, "Milestone 17.4 News " + DoubleToStr( basketNumber, 0 ), MAGIC ) ; 
            lastTradeTime = TimeCurrent();
         }
      }
   }
}

void newsSystem(){ 
   if( EnableCalendar && ffCalenadarEventTime1 < NewsReleaseMinutes && ObjectDescription( "milestoneType1" ) == "since" && ffCalenadarEventTime1 > 0 ) sendNews();
   else if( Hour() >= NewsStartHour && Hour() < NewsEndHour )sendNews(); 
}

void managePositions(){ 
   if( totalHistoryProfit < 0 && totalProfit > MathAbs( totalHistoryProfit ) * BasketProfit ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit + totalLoss > OpenProfit * AccountBalance() ) closeAll();
   else if( SafeExits && totalTrades > 0 && totalProfit + totalLoss > MathAbs( totalProfit + totalLoss ) > SafeProfit * AccountBalance() && ( ( bullish && basketNumberType == OP_SELL ) || ( bearish && basketNumberType == OP_BUY ) ) ) closeAll(); 
   else if( EnableCalendar && totalTrades > 0 && totalProfit + totalLoss > 0 && ffCalenadarEventTime1 > LeadCalendarMinutes && ObjectDescription( "milestoneType1" ) == "until" && ffCalenadarEventTime1 > 0 ) closeAll();
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
   if( EnableStop && totalHistoryProfit > StopGrowth * AccountBalance() && ( totalProfit + totalLoss ) < 0 && MathAbs( totalProfit + totalLoss ) > RelativeStop * totalHistoryProfit ) closeAll(); 
}
 
void update(){
   display = "";      
   display = display + "\n DailyGrowth: " +  DoubleToStr( dailyGrowth / AccountBalance() * 100, 1 )+ "% of " + DoubleToStr( DailyGrowth * 100, 1 ) + "%"; 
   display = display + " Spread: " + DoubleToStr( spread, 1 );    
   if( dailyGrowth / AccountBalance() > DailyGrowth ) display = display + " No more risk " ; 
   if( !EnableCalendar ) display = display + " Calendar disabled " ; 
   if( ObjectFind("hud") == -1 ) ObjectCreate( "hud", OBJ_LABEL, 0, 0, 0 );   
	if( EnableCalendar ) ObjectSet( "hud", OBJPROP_YDISTANCE, 90 ); 
   else ObjectSet( "hud", OBJPROP_YDISTANCE, 20 );  
	ObjectSetText( "hud", display, 10, "Arial Bold", LightGray ); 
	ObjectSet( "hud", OBJPROP_XDISTANCE, 6 );
	ObjectSet( "hud", OBJPROP_COLOR, LightGray );  
	if( ObjectFind("hud2") == -1 ) ObjectCreate( "hud2", OBJ_LABEL, 0, 0, 0 );   
	ObjectSet( "hud2", OBJPROP_XDISTANCE, 10 );
	if( EnableCalendar ) ObjectSet( "hud2", OBJPROP_YDISTANCE, 110 ); 
   else ObjectSet( "hud2", OBJPROP_YDISTANCE, 30 );  
   ObjectSetText( "hud2", "Analyzing ...", 10, "Arial Bold", LightGray );  
   if( ffCalenadarEventTime1 > LeadCalendarMinutes && ObjectDescription( "milestoneType1" ) == "until" && ffCalenadarEventTime1 > 0 )
      ObjectSetText( "hud2", "Upcomming news signal waiting/exit", 10, "Arial Bold", LightGray );  
   else if( ffCalenadarEventTime1 < NewsReleaseMinutes && ObjectDescription( "milestoneType1" ) == "since" && ffCalenadarEventTime1 > 0 )
      ObjectSetText( "hud2", "Recent news looking for entry", 10, "Arial Bold", LightGray );  
   else if( ffCalenadarEventTime1 > TrailCalendarMinutes && ObjectDescription( "milestoneType1" ) == "since" && ffCalenadarEventTime1 > 0 )
      ObjectSetText( "hud2", "Past news trading as normal", 10, "Arial Bold", LightGray );
   else if( ffCalenadarEventTime1 < LeadCalendarMinutes && ObjectDescription( "milestoneType1" ) == "until" && ffCalenadarEventTime1 > 0 )
      ObjectSetText( "hud2", "Upcomming news have time to trade", 10, "Arial Bold", LightGray ); 
}

int start() { 
   prepare() ;   
   if( CloseAll ) closeAll() ;
   else {  
      if( dailyGrowth / AccountBalance() < DailyGrowth &&  TimeCurrent() - lastTradeTime > SleepSeconds && ( marginLevel == 0 || marginLevel > MinMarginLevel ) ){  
         if( totalTrades >= MaxStartTrades && ( AccountBalance() + ( totalProfit + totalLoss ) ) / AccountBalance() < TriggerBackSystem ) backSystem();
         else if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxStartTrades || MaxStartTrades == 0 ) ) openPosition();  
         else if( EnableCalendar && ffCalenadarEventTime1 < NewsReleaseMinutes ) newsSystem();
      }
      managePositions() ; 
      longStop();
   }
   return( 0 ) ;
}