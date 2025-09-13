#property copyright "trevone"
#property link "http://codebase.mql4.com/9050" 
string version = "Milestone 22.5";
extern string DEFAULT_DESCRIPTION = "..........The default settings are the lowest risk"; 
extern string MAGIC_Description = "..........If MAGIC = 0 then trade identification is done using Symbol and not the MAGIC"; 
extern int MAGIC = 0;  
extern string TOOLS = ".............................................................................................................";  
extern string CloseAll_Description = "..........Close all trades for this MAGIC number as soon as possible regardless";
extern bool CloseAll = false; 
extern string ContinueTrading_Description = "..........Allow/Disallow new trades but open trades will close in profit";
extern bool ContinueTrading = true; 
extern string CALENDAR = ".............................................................................................................";
extern string Calendar_Description = "..........If enabled, filter the signals using Forex Factory feed";
extern bool EnableCalendar = false;
extern bool IncludeHigh = true;
extern bool IncludeMedium = false; 
extern bool IncludeSpeaks = true;  
extern string SAFE = ".............................................................................................................";
extern string CrossPairHedging_Description = "..........Keep profitable trades open if the marginLevel falls below MinMarginLevel";
extern bool CrossPairHedging = true;
extern string SafeSpread_Description = "..........Only open trades when the spread is below MaxSpread";
extern bool SafeSpread = false; 
extern string SafeGrowth_Description = "..........Close the basket if the current Milestone has been reached at the end of the period of";
extern bool SafeGrowth = true; 
extern string SafeExits_Description = "..........Take profits if the history profits are greater than SafeProfit if the signal reverses";
extern bool SafeExits = true; 
extern string SafeMilestone_Description = "..........Take profits and limit trading for the period of RefreshHours if MilestoneGrowth has been reached";
extern bool SafeMilestone = true; 
extern string EnableStop_Description = "..........Close losses if the total loss exeeds RelativeStop ratio of the QueryHistory history profits if the history profits is greater than StopGrowth of the AccountBalance";
extern bool EnableStop = false;   
extern string CarryProfits_Description = "..........Keep profits open untill the trend changes below MinTrend";
extern bool CarryProfits = true; 
extern string AvoidSpikes_Description = "..........Only open trades if the candle body size is less than CandleSpike within the last SpikeCount bars";
extern bool AvoidSpikes = true; 
extern string TrueRange_Description = "..........Only open trades if ATR is greater than the MinRange in pips";
extern bool TrueRange = true;
extern string KillBasket_Description = "..........Close the basket as soon as possible after KillSeconds";
extern bool KillBasket = false;
extern string SIGNAL_FRONT = ".............................................................................................................";  
extern string SignalFrontA_Description = "..........Of type OverBought/OverSold when the market begins to move in favor at the end of the trend in the last TrendBars";
extern bool SignalFrontA = true; 
extern string SignalFrontB_Description = "..........Of type Trending when the market begins to move in favor with fast moving average";
extern bool SignalFrontB = false; 
extern string SIGNAL_BACK = ".............................................................................................................";  
extern string SignalBackA_Description = "..........If enabled, use SignalFrontA for Back System";
extern bool SignalBackA = true; 
extern string SignalBackB_Description = "..........If enabled, use SignalFrontB for Back System";
extern bool SignalBackB = true; 
extern string TRADE_DAYS = ".............................................................................................................";  
extern bool TradeMonday = true;  
extern bool TradeFriday = true; 
extern string SIGNAL_HOURS = ".............................................................................................................";  
extern string StartEndHour_Description = "..........If StartHour is less than EndHour then trade between those times otherwise trade outside of those times";
extern int SignalAStartHour = 0;
extern int SignalAEndHour = 23;
extern int SignalBStartHour = 0;
extern int SignalBEndHour = 23;  
extern string TRADES = ".............................................................................................................";
extern string MaxTrades_Description = "..........Limit the total number of open basket trades";
extern int MaxTrades = 30;  
extern string TIME = ".............................................................................................................";
extern string RefreshHours_Description = "..........Milestone resets its profit counter at this interval and limits further trading for that duration if the profit exeeds MilestoneGrowth of the AccountBalance";
extern int RefreshHours = 24;   
extern string LeadCalendarMinutes_Description = "..........Do not open trades if there is a news event comming up and/or close all trades as soon as possible";
extern int LeadCalendarMinutes = 480;  
extern string TrailCalendarMinutes_Descriptio = "..........Only open trades if the news event has past this time";
extern int TrailCalendarMinutes = 480;  
extern string SleepSeconds_Description = "..........Wait this number of seconds before any more trading";
extern int SleepSeconds = 3600;
extern string BasketSeconds_Description = "..........Only stop baskets that have been open past this time";
extern int BasketSeconds = 3600;
extern string KillSeconds_Description = "..........Close the current basket at breakeven profit as soon as possible past this time";
extern int KillSeconds = 14400;
extern string PROFIT = ".............................................................................................................";
extern string BasketProfit_Description = "..........When the history profit is in negative, the basket will try to reach this amount of that hisotry loss but in profit however only close profitable trades";
extern double BasketProfit = 1.1;
extern string OpenProfit_Description = "..........When the total open profit is greater than this ratio of the AccountBalance then close the whole basket";
extern double OpenProfit = 0.002;  
extern string MinProfit_Description = "..........When the first trade gets past this ratio of the AccountBalance then take profit";
extern double MinProfit =  0.002; 
extern string GROWTH = "............................................................................................................."; 
extern string SafeProfit_Description = "..........If SafeExits is enabled then take profit at this ratio of the AccountSize if the signal reverses";
extern double SafeProfit =  0.001;
extern string StopGrowth_Description = "..........Only stop trades if the history is greater than this ratio of the AccountBalance";
extern double StopGrowth =  0.005;
extern string MilestoneGrowth_Description = "..........Limit trading for the RefreshHours if the closed profit for this period is greater than the MilestoneGrowth ratio of the AccountBalance";
extern double MilestoneGrowth = 0.002; 
extern string STOP = ".............................................................................................................";
extern string RelativeStop_Description = "..........The amount to stop of the history profit if positive";
extern double RelativeStop = 0.3; 
extern string HISTORY = ".............................................................................................................";
extern string QueryHistory_Description = "..........The number of historical trades to calculate";
extern int QueryHistory = 14;   
extern string TREND = ".............................................................................................................";  
extern string TrendBars_Description = "..........The number of trending bars to initiate a signal";
extern int TrendBars = 30; 
extern string MinRange_Description = "..........Only open trades if the ATR is above this amount in pips";
extern double MinRange = 3; 
extern string MinTrend_Description = "..........Only open trades if the Trend is above this amount in pips";
extern double MinTrend = 1; 
extern string MaxTrend_Description = "..........Only open trades if the Trend is below this amount in pips";
extern double MaxTrend = 6;     
extern string MARGIN = ".............................................................................................................";  
extern string MinMarginLevel_Description = "..........Only open new trades if the AccountEquity divided by AccountBalance is above this level";
extern double MinMarginLevel = 0.9;
extern string MarginUsage_Description = "..........Use this amount of the AccountBalance for the Front System lotsize calculation";
extern double MarginUsage = 0.003;
extern string BackupMargin_Description = "..........Use this amount of the AccountBalance for the Back System lotsize calculation";
extern double BackupMargin = 0.001; 
extern string MinLots_Description = "..........All trades will be at least this lotsize";
extern double MinLots = 0.01;
extern string TRADE = ".............................................................................................................";  
extern string TradeSpace_Description = "..........Only open new trades if there are no trades near this ratio of the ATR";
extern double TradeSpace = 10;
extern string MaxSpread_Description = "..........Only open new trades if the spread is below this value in pips if SafeSpread is enabled";
extern double MaxSpread = 5; 
extern string CandleSpike_Description = "..........Number of pips to measure a candle spike";
extern double CandleSpike = 5; 
extern string SpikeCount_Description = "..........Number of bars to look back on for candle spike";
extern int SpikeCount = 3;   
extern string INDICATOR_ATR = "............................................................................................................."; 
extern int ATRPeriod = 14;  
extern int ATRTimeFrame = 0; 
extern string INDICATOR_MA = ".............................................................................................................";  
extern string MA1Period_Description = "..........Slow Moving Average trendStrength is calculated as MA1Cur - MA1Prev";
extern int MA1Period = 120; 
extern string MA2Period_Description = "..........Fast Moving Average trendStrength is calculated as MA1Cur - MA1Prev";
extern int MA2Period = 40;  
extern int MAShiftCheck = 10; 
extern int MATimeFrame = 0; 

double slippage, marginRequirement, lotSize, backupLotSize, totalHistoryProfit, totalProfit, totalLoss, symbolHistory, ical, eATR, MA1Cur, MA1Prev, MA2Cur, MA2Prev,MA3Cur, MA3Prev; 

int digits, totalTrades, totalBackupTrades; 

bool nearLongPosition = false;
bool nearShortPosition = false;  
bool rangingMarket = false;
bool bullish = false;
bool bearish = false; 
bool incrementLimits = false;
bool spike = false;
bool multipleMargin = false;

int MaxStartTrades = 1;   
int ATRShift = 0;  
int ADXShift = 0; 
int lastTradeTime = 0;
int MMAShift = 0;
int MAShift = 0; 
int totalHistory = 100;
int basketNumber = 0;
int basketNumberType = -1;
int basketCount = -1; 
int calenadarType = -1;  
int openType = -1; 
int dailyTargets = 0;
int totalDays = 0; 
int turn = 0;

double buyLots = 0;
double sellLots = 0;
double pipPoints = 0.00010;  
double DynamicSlippage = 1;   
double BaseLotSize = 0.01;    
double calenadarHours = -1; 
double calenadarMinutes = -1; 
double calenadarEventTime = 0;  
double marginLevel = 0;
double spread = 0;
double trendStrength = 0;
double longHistortProfit = 0;  
double milestoneGrowth = 0;
double maxEquity = 0;   
string calenadarCurrency = ""; 
string calenadarText = "";   
string signalComment = "";    

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
   if( lotSize < MinLots ) lotSize = MinLots;   
   if( backupLotSize < MinLots ) backupLotSize = MinLots;   
} 

void milestone(){
   multipleMargin = false;
   if( AccountEquity() / AccountBalance() < MinMarginLevel && totalProfit + totalLoss > 0 ) multipleMargin = true;
   if( AccountMargin() > 0 ) marginLevel = AccountEquity() / AccountMargin() * 100  ; 
   if( totalTrades == 0 ) marginLevel = 0;  
   if( MathMod( TimeCurrent(), 3600 * RefreshHours ) <= 10 ){ 
      if( turn == 0 ) totalDays = totalDays + 1;
      turn = 1;
      if( milestoneGrowth / AccountBalance() > MilestoneGrowth ) {
         Print( "Milestone growth reached " ,DoubleToStr(  dailyTargets + 1, 0 ) + " / " + DoubleToStr(  totalDays, 0 ) );
         dailyTargets = dailyTargets + 1;
         turn = 1;
      } 
      milestoneGrowth = 0;  
      if( totalProfit + totalLoss > 0 ) closeAll(); 
   } else {
      turn = 0;
   }
   if( SafeGrowth ) if( milestoneGrowth / AccountBalance() > MilestoneGrowth  ) closeAll();
   if( AccountBalance() > maxEquity ) maxEquity = AccountBalance(); 
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
      if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() ){ 
         RefreshRates();
         if( ( OrderStopLoss() == 0 && OrderProfit() > 0 && type == "profits" ) || type == "none" ){
            if( OrderType() == OP_BUY ){ 
               OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
               milestoneGrowth = milestoneGrowth + OrderProfit();
               lastTradeTime = TimeCurrent();
            }
            if( OrderType() == OP_SELL ) {
               OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
               milestoneGrowth = milestoneGrowth + OrderProfit();
               lastTradeTime = TimeCurrent();
            }
         }
      }
   }
} 

void prepareHistory(){
   symbolHistory = 0;
   totalHistoryProfit = 0;
   for( int iPos = OrdersHistoryTotal() - 1; iPos > ( OrdersHistoryTotal() - 1 ) - totalHistory; iPos-- ){
      OrderSelect( iPos, SELECT_BY_POS, MODE_HISTORY );
      double QueryHistoryDouble = ( double ) QueryHistory;
      if( symbolHistory >= QueryHistoryDouble ) break;
      if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() ){
         totalHistoryProfit = totalHistoryProfit + OrderProfit();
         symbolHistory = symbolHistory + 1;
      }
   }
}

void prepareTrend(){ 
   bullish = false;
   bearish = false; 
   spike = false;
   bool bullishi = true;
   bool bearishi = true;   
   double MA1Curi, MA1Previ, MA2Curi, MA2Previ,MA3Curi;   
   for( int i = 0; i < TrendBars; i++ ) {
      if( i < SpikeCount && MathAbs( Close[i] - Open[i] ) > CandleSpike * pipPoints ) spike = true; 
      MA1Curi = iMA( NULL, MATimeFrame, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, i );   
      MA2Curi = iMA( NULL, MATimeFrame, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, i );   
      if( MA2Curi < MA1Curi ) bullishi = false;  
      if( MA2Curi > MA1Curi ) bearishi = false; 
   }    
   if( ( ( SignalFrontA && totalTrades == 0 ) || ( SignalBackA && totalTrades > 0 ) ) && ( ( SignalAStartHour < SignalAEndHour && Hour() >= SignalAStartHour && Hour() < SignalAEndHour ) || ( SignalAStartHour > SignalAEndHour && ( ( Hour() <= SignalAEndHour && Hour() >= 0 ) || ( Hour() <= 23 && Hour() >= SignalAStartHour ) ) ) ) ){       
      signalComment = "SignalA"; 
      if( ( ( AvoidSpikes && !spike ) || !AvoidSpikes ) && ( ( TrueRange && eATR > MinRange * pipPoints ) || !TrueRange ) ){ 
         if( trendStrength > MinTrend * pipPoints && trendStrength < MaxTrend * pipPoints && bullishi && !bearishi && Close[0] < Open[0] && Close[0] < Low[1] ) bearish = true;
         if( trendStrength < -MinTrend * pipPoints && trendStrength > -MaxTrend * pipPoints && bearishi && !bullishi && Close[0] > Open[0] && Close[0] > High[1] ) bullish = true; 
      }    
   } 
   if( ( ( SignalFrontB && totalTrades == 0 ) || ( SignalBackB && totalTrades > 0 ) ) && ( ( SignalBStartHour < SignalBEndHour && Hour() >= SignalBStartHour && Hour() < SignalBEndHour ) || ( SignalBStartHour > SignalBEndHour && ( ( Hour() <= SignalBEndHour && Hour() >= 0 ) || ( Hour() <= 23 && Hour() >= SignalBStartHour ) ) ) ) ){       
      signalComment = "SignalB"; 
      if( ( ( AvoidSpikes && !spike ) || !AvoidSpikes ) && ( ( TrueRange && eATR > MinRange * pipPoints ) || !TrueRange ) ){ 
         if( trendStrength > MinTrend * pipPoints && trendStrength < MaxTrend * pipPoints && bullishi && !bearishi && Close[0] < MA2Cur )  bullish = true;
         if( trendStrength < -MinTrend * pipPoints && trendStrength > -MaxTrend * pipPoints && bearishi && !bullishi && Close[0] > MA2Cur ) bearish = true;
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
   openType = -1;
   for( int i = 0 ; i < OrdersTotal(); i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;   
      if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() ) {
         if( StringFind( OrderComment(), "Backup", 0 ) > -1 ) totalBackupTrades = totalBackupTrades + 1;  
         totalTrades = totalTrades + 1;
         if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace ) nearLongPosition = true ;
         else if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace ) nearShortPosition = true;
         if( OrderType() == OP_BUY ) {
            buyLots = buyLots + OrderLots(); 
            openType = OP_BUY;
         } else if( OrderType() == OP_SELL ) {
            sellLots = sellLots + OrderLots(); 
            openType = OP_SELL;
         }
         if( OrderProfit() > 0 ) totalProfit = totalProfit + OrderProfit();
         else totalLoss = totalLoss + OrderProfit(); 
      }
   } 
} 

void prepareIndicators(){
   eATR = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift );   
   MA1Cur = iMA( NULL, MATimeFrame, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );  
   MA1Prev = iMA( NULL, MATimeFrame, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + MAShiftCheck ); 
   MA2Cur = iMA( NULL, MATimeFrame, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );  
   MA2Prev = iMA( NULL, MATimeFrame, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + MAShiftCheck );       
   if( EnableCalendar ) ical = iCustom( NULL, PERIOD_M5, "milestone_calendar", IncludeHigh, IncludeMedium, false, IncludeSpeaks, 0, 0 ); 
   trendStrength = MA1Cur - MA1Prev;
} 

void prepareCalendar(){ 
   calenadarCurrency = ObjectDescription( "milestoneCurrency1" );     
   calenadarText = ObjectDescription( "milestoneText1" );  
   if( ObjectDescription( "milestoneType1" ) == "since " ) calenadarType = 0; 
   if( ObjectDescription( "milestoneType1" ) == "until " ) calenadarType = 1;      
   if( ObjectDescription( "milestoneImpact1" ) == "High" ) calenadarType = 0; 
   if( ObjectDescription( "milestoneImpact1" ) == "Medium" ) calenadarType = 1;
   calenadarMinutes = StrToDouble( ObjectDescription( "milestoneMinutes1" ) );  
   if( calenadarMinutes > 0 ) calenadarEventTime = ( calenadarHours * 60 ) + calenadarMinutes; 
   else calenadarEventTime = 0; 
}   

void prepare(){ 
   prepareIndicators(); 
   prepareCalendar(); 
   prepareTrend();
   setPipPoint(); 
   prepareHistory();
   preparePositions();  
   lotSize();   
   milestone();
   update();  
} 

void sendOpen(){
   if( ( SafeSpread && spread < MaxSpread ) || !SafeSpread ){
      if( !nearLongPosition && bullish && sellLots == 0 && Open[0] < Close[0] ) {
         if( basketNumberType != OP_BUY ) basketCount = 0;
         if( basketCount < MaxTrades ){
            if( AccountFreeMarginCheck( Symbol(), OP_BUY, lotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_BUY, lotSize, Ask, slippage, 0, 0, version + " " + signalComment + " Min " + DoubleToStr( basketNumber, 0 ), MAGIC );
            if( basketNumberType != OP_BUY ) basketNumber = basketNumber + 1; 
            basketCount = basketCount + 1; 
            openType = OP_BUY;   
            lastTradeTime = TimeCurrent();
         } 
      } else if( !nearShortPosition && bearish && buyLots == 0 && Open[0] > Close[0] ) {
         if( basketNumberType != OP_SELL ) basketCount = 0;
         if( basketCount < MaxTrades ){
            if( AccountFreeMarginCheck( Symbol(), OP_SELL, lotSize ) <= 0 || GetLastError() == 134 ) return;
            OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, version + " " + signalComment + " Min " + DoubleToStr( basketNumber, 0 ), MAGIC );
            if( basketNumberType != OP_SELL ) basketNumber = basketNumber + 1;  
            basketCount = basketCount + 1;  
            openType = OP_SELL;   
            lastTradeTime = TimeCurrent();
         }
      }  
   }
}

void openPosition(){    
   if( EnableCalendar ){    
      if( calenadarEventTime > TrailCalendarMinutes && ObjectDescription( "milestoneType1" ) == "since" )
         sendOpen(); 
      else if( calenadarEventTime > LeadCalendarMinutes && ObjectDescription( "milestoneType1" ) == "until" )
         sendOpen(); 
      if( calenadarEventTime == 0 )
         sendOpen(); 
   } else sendOpen(); 
} 

void sendBack(){
   if( ( ContinueTrading || ( !ContinueTrading && totalBackupTrades > 0 ) ) && ( totalBackupTrades < MaxTrades - MaxStartTrades ) ) {   
      if( !nearLongPosition && bullish && sellLots == 0 ) {
         OrderSend( Symbol(), OP_BUY, backupLotSize, Ask, slippage, 0, 0,  version + " " + signalComment + " Backup " + DoubleToStr( basketNumber, 0 ), MAGIC ); 
         lastTradeTime = TimeCurrent();
      } else if( !nearShortPosition && bearish && buyLots == 0 ){
         OrderSend( Symbol(), OP_SELL, backupLotSize, Bid, slippage, 0, 0,  version + " " + signalComment + " Backup " + DoubleToStr( basketNumber, 0 ), MAGIC ); 
         lastTradeTime = TimeCurrent();
      }  
   }
}

void backSystem(){   
   if( EnableCalendar ){ 
      if( calenadarEventTime > TrailCalendarMinutes && ObjectDescription( "milestoneType1" ) == "since" )
         sendBack(); 
      else if( calenadarEventTime > LeadCalendarMinutes && ObjectDescription( "milestoneType1" ) == "until" )
         sendBack(); 
      if( calenadarEventTime == 0 )
         sendBack();  
   } else sendBack(); 
}
 
void managePositions(){ 
   if( totalHistoryProfit < 0 && totalProfit > MathAbs( maxEquity - totalHistoryProfit ) * BasketProfit ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit + totalLoss > OpenProfit * AccountBalance() ) closeAll();
   else if( SafeExits && totalTrades > 0 && totalProfit + totalLoss > SafeProfit * AccountBalance() && ( ( bullish && openType == OP_SELL ) || ( bearish && openType == OP_BUY ) ) ) closeAll(); 
   else if( EnableCalendar && totalTrades > 0 && totalProfit + totalLoss > 0 && calenadarEventTime > LeadCalendarMinutes && ObjectDescription( "milestoneType1" ) == "until" && calenadarEventTime > 0 ) closeAll();
   else { 
      for( int i = OrdersTotal() - 1; i >= 0; i-- ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( ( MAGIC > 0 && OrderMagicNumber() == MAGIC ) || OrderSymbol() == Symbol() ) {  
            if( totalTrades <= MaxStartTrades ){
               if( OrderType() == OP_BUY && Bid > OrderOpenPrice() && OrderProfit() > MinProfit * AccountBalance() && ( ( MinTrend && trendStrength < MinTrend * pipPoints ) || !CarryProfits ) ) {
                  OrderClose( OrderTicket(), OrderLots(), Bid, slippage ); 
                  milestoneGrowth = milestoneGrowth + OrderProfit();
                  lastTradeTime = TimeCurrent();
               } else if( OrderType() == OP_SELL && Ask < OrderOpenPrice() && OrderProfit() > MinProfit * AccountBalance() && ( ( CarryProfits && trendStrength > -MinTrend * pipPoints ) || !CarryProfits ) ){
                  OrderClose( OrderTicket(), OrderLots(), Ask, slippage );   
                  milestoneGrowth = milestoneGrowth + OrderProfit();
                  lastTradeTime = TimeCurrent();
               }
            } 
         }  
      }
   }
}   
 
void manageStops(){ 
   if( EnableStop && TimeCurrent() - lastTradeTime > BasketSeconds && totalHistoryProfit > StopGrowth * AccountBalance() && ( totalProfit + totalLoss ) < 0 && MathAbs( totalProfit + totalLoss ) > RelativeStop * totalHistoryProfit ) closeAll(); 
   if( KillBasket && TimeCurrent() - lastTradeTime > KillSeconds && totalProfit + totalLoss > 0 ) closeAll();
   if( SafeMilestone && ( milestoneGrowth + ( totalProfit + totalLoss ) ) / AccountBalance() > MilestoneGrowth && totalTrades > 0 ) closeAll();   
}
 
void update(){
   if( ObjectFind("MilestoneHUD1") == -1 ) ObjectCreate( "MilestoneHUD1", OBJ_LABEL, 0, 0, 0 );   
	if( EnableCalendar ) ObjectSet( "MilestoneHUD1", OBJPROP_YDISTANCE, 90 ); 
   else ObjectSet( "MilestoneHUD1", OBJPROP_YDISTANCE, 20 );  
	ObjectSetText( "MilestoneHUD1", " Milestones: " + DoubleToStr(  dailyTargets, 0 ) + " of " + DoubleToStr( totalDays, 0 ) + ", Growth: " + DoubleToStr( milestoneGrowth / AccountBalance() * 100, 1 ) + "% of " + DoubleToStr( MilestoneGrowth * 100, 1 ) + "% ", 10, "Arial Bold", LightGray ); 
	ObjectSet( "MilestoneHUD1", OBJPROP_XDISTANCE, 6 );
	ObjectSet( "MilestoneHUD1", OBJPROP_COLOR, LightGray );  
   
   if( ObjectFind("info") == -1 ) ObjectCreate( "MilestoneHUD2", OBJ_LABEL, 0, 0, 0 );
   ObjectSet( "MilestoneHUD2", OBJPROP_XDISTANCE, 6 );
   if( EnableCalendar ) ObjectSet( "MilestoneHUD2", OBJPROP_YDISTANCE, 110 ); 
   else ObjectSet( "MilestoneHUD2", OBJPROP_YDISTANCE, 40 ); 
   string hedgeStatus = "No";
   if( AccountEquity() / AccountBalance() < MinMarginLevel && totalProfit + totalLoss > 0 ) hedgeStatus = " Yes";
   ObjectSetText( "MilestoneHUD2", " Hedging: " + hedgeStatus + ", Profit: " + DoubleToStr( totalProfit + totalLoss, 2 ) + ", Margin: " + DoubleToStr( AccountEquity() / AccountBalance() * 100, 1 ) + "%, Lots: " + DoubleToStr( buyLots + sellLots, 2 ), 10, "Arial Bold", LightGray );
   
   if( ObjectFind("info") == -1 ) ObjectCreate( "MilestoneHUD3", OBJ_LABEL, 0, 0, 0 );
   ObjectSet( "MilestoneHUD3", OBJPROP_XDISTANCE, 6 );
   if( EnableCalendar ) ObjectSet( "MilestoneHUD3", OBJPROP_YDISTANCE, 130 ); 
   else ObjectSet( "MilestoneHUD3", OBJPROP_YDISTANCE, 60 );  
   ObjectSetText( "MilestoneHUD3", " Spread: " + DoubleToStr( spread, 1 ) + ", Trend: " + DoubleToStr( trendStrength / pipPoints, 1 ) + ", ATR: " + DoubleToStr( eATR / pipPoints, 1 ), 10, "Arial Bold", LightGray );  
   
   if( ObjectFind("MilestoneHUD4") == -1 ) ObjectCreate( "MilestoneHUD4", OBJ_LABEL, 0, 0, 0 );   
	ObjectSet( "MilestoneHUD4", OBJPROP_XDISTANCE, 10 );
	if( EnableCalendar ) ObjectSet( "MilestoneHUD4", OBJPROP_YDISTANCE, 150 ); 
   else ObjectSet( "MilestoneHUD4", OBJPROP_YDISTANCE, 80 );  
   ObjectSetText( "MilestoneHUD4", "", 10, "Arial Bold", LightGray );
   
   if( EnableCalendar ){
      if( calenadarEventTime < LeadCalendarMinutes && calenadarEventTime > 0 && ObjectDescription( "milestoneType1" ) == "until" )
         ObjectSetText( "MilestoneHUD4", "Upcomming news, signal waiting/exit", 10, "Arial Bold", LightGray );   
      else if( calenadarEventTime > TrailCalendarMinutes && ObjectDescription( "milestoneType1" ) == "since" )
         ObjectSetText( "MilestoneHUD4", "Past news, trading as normal", 10, "Arial Bold", LightGray );
      else if( calenadarEventTime > LeadCalendarMinutes && ObjectDescription( "milestoneType1" ) == "until" )
         ObjectSetText( "MilestoneHUD4", "Upcomming news, trading with caution", 10, "Arial Bold", LightGray ); 
      if( calenadarEventTime == 0 && ObjectDescription( "milestoneType1" ) != "since" )
         ObjectSetText( "MilestoneHUD4", "No news, trading as normal", 10, "Arial Bold", LightGray ); 
      else ObjectSetText( "MilestoneHUD4", "Trading as normal", 10, "Arial Bold", LightGray ); 
   } else ObjectSetText( "MilestoneHUD4", "", 10, "Arial Bold", LightGray ); 
}

int start() { 
   prepare() ;   
   if( CloseAll ) closeAll() ;
   else {  
      if( Bars > MA1Period * 2 ){
         if( ( ( DayOfWeek() != 1 && !TradeMonday ) || TradeMonday ) && ( ( DayOfWeek() != 5 && !TradeFriday ) || TradeFriday ) ){
            if( milestoneGrowth / AccountBalance() < MilestoneGrowth && TimeCurrent() - lastTradeTime > SleepSeconds && ( AccountEquity() / AccountBalance() > MinMarginLevel ) ){  
               if( totalTrades >= MaxStartTrades ) backSystem();
               else if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxStartTrades || MaxStartTrades == 0 ) ) openPosition();   
            }
         }
      } 
      if( !multipleMargin || ( !CrossPairHedging && multipleMargin ) ) managePositions() ; 
      manageStops();
   }
   return( 0 );
}

int deinit() { 
	ObjectDelete("MilestoneHUD1"); 
	ObjectDelete("MilestoneHUD2");
	ObjectDelete("MilestoneHUD3"); 
	ObjectDelete("MilestoneHUD4");  
	return(0);
}