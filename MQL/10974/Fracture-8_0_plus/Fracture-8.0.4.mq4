#property copyright "Fracture 7.4.0"
#property link "Fracture"
#define MAGIC 20130706
extern string TOOLS = ".............................................................................................................";
extern bool CloseAll = false;
extern bool ContinueTrading = true;
extern bool CertaintySystem = true;
extern bool BreakoutSystem = true;
extern string RISK = ".............................................................................................................";
extern double BaseLotSize = 0.01;
extern double RangeUsage = 0.05;
extern double TrendUsage = 0.1;
extern string TRADING = ".............................................................................................................";
extern int MaxTrades = 9;
extern int MaxScalping = 5; 
extern double Aggressive = 10; 
extern double TradeSpace = 1.1;
extern double DynamicSlippage = 0.5;
extern string PROFITS = ".............................................................................................................";
extern int QueryHistory = 22; 
extern double BasketProfit = 3;
extern double OpenProfit = 0.85;
extern double MinProfit = 1; 
extern string TRENDING = ".............................................................................................................";
extern double CrossBars = 5;
extern double RangingMarket = 0.7;
extern double TurboStart = 0.2; 
extern double AnticipationBars = 14;
extern double AnticipationATR = 0.3;
extern double AnticipationADXCount = 3;
extern double AnticipationATRCount = 5;
extern int CertaintyBars = 14;
extern int CertaintyCount = 9;
extern string INDICATOR_ATR = ".............................................................................................................";
extern int ATRTimeFrame = 0;
extern int ATRPeriod = 14;
extern int ATRShift = 0;
extern string INDICATOR_MA = ".............................................................................................................";
extern int MATimeFrame = 0;
extern int MA1Period = 5;
extern int MA2Period = 9;
extern int MA3Period = 18;
extern int MMAShift = 0;
extern int MAShift = 0;
extern string INDICATOR_ADX = ".............................................................................................................";
extern double ADXLine1 = 45;
extern double ADXLine2 = 55;
extern int ADXTimeFrame = 0;
extern int ADXPeriod = 14;
extern int ADXShift = 0;
extern string INDICATOR_FRACTAL = ".............................................................................................................";
extern int FractalTimeFrame = 0;
extern int FractalShift = 1;
extern int FractalBars = 5;
 
double slippage, marginRequirement, lotSize, lastProfit, totalHistoryProfit, totalProfit, totalLoss, symbolHistory, 
eATR, eATRPrev, eADX, MA1Cur, MA2Cur, MA3Cur, MA1Prev, MA2Prev, MA3Prev ;

int digits, totalTrades, totalScalping;

int totalHistory = 100;
double pipPoints = 0.00010;
double fractalUpPrice = 0 ;
double fractalDownPrice = 0;  
double trendStrength = 0;
double drawdown = 0; 
bool nearLongPosition = false;
bool nearShortPosition = false;
bool longTrendUp = false;
bool longTrendDown = false;
bool shortTrendUp = false;
bool shortTrendDown = false;
bool rangingMarket = false; 
bool shortBullishCross1 = false;
bool shortBullishCross2 = false; 
bool shortBearishCross1 = false;
bool shortBearishCross2 = false;  
bool anticipation = false;
bool certaintyBullish = false;
bool certaintyBearish = false;
bool breakoutBullish = false;
bool breakoutBearish = false; 

int totalHistoryScalpingBuys = 0;
int totalHistoryScalpingSells = 0;

int totalOpenScalpingBuys = 0;
int totalOpenScalpingSells = 0;

string display = "\n"; 

int init(){ 
   prepare() ; 
   return( 0 );
}

double marginCalculate( string symbol, double volume ){ 
   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
} 

void lotSize(){ 
   slippage = NormalizeDouble( ( eATR / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize ); 
   trendStrength = MathAbs( MA1Cur - MA3Cur ) / MathAbs( MA2Cur - MA3Cur );
   drawdown = 1 - AccountEquity() / AccountBalance();
   if( rangingMarket ) lotSize = NormalizeDouble( ( AccountFreeMargin() * RangeUsage / marginRequirement ) * BaseLotSize , 2 ) ;
   else lotSize = NormalizeDouble( ( AccountFreeMargin() * TrendUsage / marginRequirement ) * BaseLotSize, 2 ) ; 
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
            if( OrderType() == OP_BUY ) OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            if( OrderType() == OP_SELL ) OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
         } 
      }
   }
}

void prepareFractals(){
   fractalUpPrice = 0;
   fractalDownPrice = 0;
   for( int i = 0; i < FractalBars; i++ ){ 
      double ifractalUp = iFractals( NULL, 0, MODE_UPPER, i );
      double ifractalDown = iFractals( NULL, 0, MODE_LOWER, i );
      if( ifractalUp > 0 && Open[i] > Open[0] ){
         if( Open[i] > Close[i] ) fractalUpPrice = Open[i];
         else fractalUpPrice = Close[i]; 
      } else if( ifractalDown > 0 && Open[i] < Open[0] ){
         if( Open[i] < Close[i] ) fractalDownPrice = Open[i];
         else fractalDownPrice = Close[i];
      }
   }
} 

void prepareHistory(){
   symbolHistory = 0;
   totalHistoryProfit = 0; 
   totalScalping = 0;
   totalHistoryScalpingBuys = 0;
   totalHistoryScalpingSells = 0;
   for( int iPos = OrdersHistoryTotal() - 1 ; iPos > ( OrdersHistoryTotal() - 1 ) - totalHistory; iPos-- ){
      OrderSelect( iPos, SELECT_BY_POS, MODE_HISTORY ) ;
      double QueryHistoryDouble = ( double ) QueryHistory;
      if( symbolHistory >= QueryHistoryDouble ) break;
      if( OrderSymbol() == Symbol() ){ 
         if( OrderComment() == "scalp" ) {
            totalScalping = totalScalping + 1;
            if( OrderType() == OP_BUY ) totalHistoryScalpingBuys = totalHistoryScalpingBuys + 1;
            if( OrderType() == OP_SELL ) totalHistoryScalpingSells = totalHistoryScalpingSells + 1;
         }
         totalHistoryProfit = totalHistoryProfit + OrderProfit() ;
         symbolHistory = symbolHistory + 1 ;
      }
   }
}

void preparePositions() {
   nearLongPosition = false;
   nearShortPosition = false;
   totalTrades = 0;
   totalProfit = 0;
   totalLoss = 0; 
   totalOpenScalpingBuys = 0;
   totalOpenScalpingSells = 0;
   for( int i = 0 ; i < OrdersTotal() ; i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if( OrderSymbol() == Symbol() ) { 
         if( rangingMarket ){
            if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace ) nearLongPosition = true ;
            if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace ) nearShortPosition = true ;
         } else {
            if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace / Aggressive ) nearLongPosition = true ;
            if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace / Aggressive ) nearShortPosition = true ;
         } 
         if( OrderComment() == "default" || OrderComment() == "certainty" ) totalTrades = totalTrades + 1;
         else if( OrderComment() == "certainty" ) {
            if( OrderType() == OP_BUY ) totalOpenScalpingBuys = totalOpenScalpingBuys + 1; 
            if( OrderType() == OP_SELL ) totalOpenScalpingSells = totalOpenScalpingSells + 1;
         } 
         if( OrderProfit() > 0 ) totalProfit = totalProfit + OrderProfit();
         else totalLoss = totalLoss + OrderProfit();  
      }
   }
}
 
void prepareIndicators(){
   eATR = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift );
   eATRPrev = iATR( NULL, ATRTimeFrame, ATRPeriod, ATRShift + 1 );
   eADX = iADX( NULL, ADXTimeFrame, MA1Period, PRICE_MEDIAN, MODE_MAIN, ADXShift );
   MA1Cur = iMA( NULL, MATimeFrame, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );
   MA2Cur = iMA( NULL, MATimeFrame, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );
   MA3Cur = iMA( NULL, MATimeFrame, MA3Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift );
   MA1Prev = iMA( NULL, MATimeFrame, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + 1 );
   MA2Prev = iMA( NULL, MATimeFrame, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + 1 );
   MA3Prev = iMA( NULL, MATimeFrame, MA3Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, MAShift + 1 );   
} 

bool prepareAnticipation(){
   int bars = 0; 
   double averageATR = 0;
   double totalATR = 0;
   double AnticipationBarsDouble = ( double ) AnticipationBars;
   for( int i = 0; i < AnticipationBars; i++ ) totalATR = totalATR + iATR( NULL, ATRTimeFrame, ATRPeriod, i );
   averageATR = averageATR / AnticipationBarsDouble; 
   int anticipationADXCount = 0;
   int anticipationATRCount = 0;
   for( i = 0; i < AnticipationBars; i++ ){
      double tATR = iATR( NULL, ATRTimeFrame, ATRPeriod, i );
      double tADXMainCur = iADX( NULL, ATRTimeFrame, ATRPeriod, PRICE_HIGH, MODE_MAIN, i );
      double tADXPlusCur = iADX( NULL, ATRTimeFrame, ATRPeriod, PRICE_HIGH, MODE_PLUSDI, i );
      double tADXMinusCur = iADX( NULL, ATRTimeFrame, ATRPeriod, PRICE_HIGH, MODE_MINUSDI, i );
      double tADXMainPrev = iADX( NULL, ATRTimeFrame, ATRPeriod, PRICE_HIGH, MODE_MAIN, i + 1 );
      double tADXPlusPrev = iADX( NULL, ATRTimeFrame, ATRPeriod, PRICE_HIGH, MODE_PLUSDI, i + 1 );
      double tADXMinusPrev = iADX( NULL, ATRTimeFrame, ATRPeriod, PRICE_HIGH, MODE_MINUSDI, i + 1 ); 
      if( tADXPlusCur > tADXMinusCur && tADXPlusPrev <= tADXMinusPrev || tADXPlusCur < tADXMinusCur && tADXPlusPrev >= tADXMinusPrev ) 
         anticipationADXCount = anticipationADXCount + 1; 
      if( tATR < eATR * AnticipationATR ) anticipationATRCount = anticipationATRCount + 1; 
   }
   if( anticipationATRCount > AnticipationATRCount && anticipationADXCount > AnticipationADXCount ) anticipation = true;
   else anticipation = false;
}

int prepareCertainty(){ 
   double totalUp = 0; 
   double totalDown = 0;
   for( int i = 0; i < CertaintyBars; i++ ){
      double tMA1Cur = iMA( NULL, MATimeFrame, MA1Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, i );
      double tMA2Cur = iMA( NULL, MATimeFrame, MA2Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, i );
      double tMA3Cur = iMA( NULL, MATimeFrame, MA3Period, MMAShift, MODE_SMMA, PRICE_MEDIAN, i );
      if( tMA1Cur > tMA3Cur && tMA2Cur > tMA3Cur ) totalUp = totalUp + 1; 
      if( tMA1Cur < tMA3Cur && tMA2Cur < tMA3Cur ) totalDown = totalDown + 1; 
   }
   if( totalUp > CertaintyCount && Close[0] < Open[0] && eADX > ADXLine1 &&  eADX < ADXLine2 ) certaintyBearish = true;
   else certaintyBearish = false;
   if( totalDown > CertaintyCount  && Close[0] <  Open[0]  && eADX > ADXLine1 &&  eADX < ADXLine2   ) certaintyBullish = true; 
   else certaintyBullish = false; 
} 
 
void prepareTrend(){  
   if( MathAbs( MA2Cur - MA3Cur ) < eATR * RangingMarket && eADX < ADXLine1 ) {
      rangingMarket = true ;
      shortTrendUp = false ;
      shortTrendDown = false ;
      longTrendUp = false ;
      longTrendDown = false ;
   } else {
      if( MA1Cur > MA2Cur && MA1Cur > MA1Prev && MA2Cur > MA2Prev ) shortTrendUp = true ;
      else shortTrendUp = false ;
      if( MA1Cur < MA2Cur && MA1Cur < MA1Prev && MA2Cur < MA2Prev ) shortTrendDown = true ;
      else shortTrendDown = false ;
      if( MA2Cur > MA3Cur && MA2Cur > MA2Prev && MA3Cur > MA3Prev ) longTrendUp = true ;
      else longTrendUp = false ;
      if( MA2Cur < MA3Cur && MA2Cur < MA2Prev && MA3Cur < MA3Prev ) longTrendDown = true ; 
      else longTrendDown = false ;
      if( shortTrendUp || shortTrendDown || longTrendUp || longTrendDown ) rangingMarket = false ;
      else rangingMarket = true ;
   }
} 
 
void managePositions(){

   if( ( totalHistoryProfit < 0 || totalTrades == 1 ) && MathAbs( totalHistoryProfit ) < totalProfit * BasketProfit  ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit > MathAbs( totalLoss ) * OpenProfit &&  totalHistoryProfit   > 0 ) closeAll();
   else {
      for( int i = 0 ; i < OrdersTotal() ; i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol() && OrderComment() == "certainty" ) { 
             
            if( OrderType() == OP_BUY &&  certaintyBearish) 
               OrderClose( OrderTicket(), OrderLots(), Bid, slippage ); 
            else if( OrderType() == OP_SELL && certaintyBullish)
               OrderClose( OrderTicket(), OrderLots(), Ask, slippage );     
         }    
         
      } 
   }
}

void openPosition(){ 
   int type = -1; 
   RefreshRates(); 
   /*
   if( rangingMarket ){  
      if( !nearLongPosition && Close[0] >= fractalDownPrice ) OrderSend( Symbol(), OP_BUY, lotSize, Ask, slippage, 0, 0, "default", MAGIC );
      if( !nearShortPosition && Close[0] <= fractalUpPrice   ) OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "default", MAGIC );
   }
   */
   if( CertaintySystem ){
      if( !nearLongPosition && certaintyBullish    && totalOpenScalpingSells < 1 ) OrderSend( Symbol(), OP_BUY, lotSize, Ask, slippage, 0, 0, "certainty", MAGIC );
      else if( !nearShortPosition && certaintyBearish  && totalOpenScalpingBuys < 1) OrderSend( Symbol(), OP_SELL, lotSize, Bid, slippage, 0, 0, "certainty", MAGIC );
   } 
}

void update(){
   display = "";
   display = display + " Trade Space: " + DoubleToStr( TradeSpace * eATR / pipPoints, 1 ) + "pips";  
   display = display + " Lot Size: " + DoubleToStr( lotSize, 2 ); 
   display = display + "\n\n Trend Strength: " + DoubleToStr( trendStrength, 2 ); 
   display = display + " Ranging: " + DoubleToStr( rangingMarket, 0 );
   display = display + "\n Bull: " + DoubleToStr( longTrendUp, 0 ); 
   display = display + " Bullish: " + DoubleToStr( shortTrendUp, 0 ) ;
   display = display + " Bearish: " + DoubleToStr( shortTrendDown, 0 );
   display = display + " Bear: " + DoubleToStr( longTrendDown, 0 ); 
   display = display + "\n\n Draw Down: " + DoubleToStr( drawdown, 2 );
   display = display + " Open Trades: " + DoubleToStr( totalTrades, 0 ) + " (" + DoubleToStr( MaxTrades, 0 ) + ")";  
   display = display + "\n Profit: " + DoubleToStr( totalProfit, 2 );
   display = display + " Loss: " + DoubleToStr( totalLoss, 2 );
   display = display + " History: " + DoubleToStr( totalHistoryProfit, 2 );   
   
   display = display + " Loss: " + DoubleToStr( totalOpenScalpingBuys, 2 );
   display = display + " History: " + DoubleToStr( totalOpenScalpingSells, 2 );  
   Comment( display );
}

void prepare(){
   prepareIndicators();
   prepareFractals();
   setPipPoint(); 
   prepareHistory();
   preparePositions();
   prepareAnticipation(); 
   prepareCertainty();
   prepareTrend();
   lotSize();   
   update() ;
} 



int start() { 
   prepare() ;  
   if( CloseAll ) closeAll() ;
   else {
      if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) 
      && ( totalTrades < MaxTrades || MaxTrades == 0 ) ) openPosition() ; 
      managePositions() ;  
   }
   return( 0 ) ;
}

/*

1.) Fractal Scalping Sytem
2.) 99% certanty System
3.) Big Boy System




Each system uses the Anticipation Rule:
If the market has been ranging for n periods and the atr is less than the average atr of n periods * user setting then close all positions and trade on next trend with big boy and 99% certanty
If the adx is less than user defined and the plusdi and minusdi have crossed 


Scalping
Main strategy using fractals and moving averages, uses all basket profits

99% Certanty

If the market has been trending for n periods and abs(ma1-3)+abs(ma2-3)/2 is greater than user setting then enter trade with take profit and include in basket profits

Big Boy
If the market has been in anticipation and 99% certantity then enter trade, always use stop on each fractal and exclude from basket profits
When the longer moving averages cross then place take profits on the big boys

Strategy tester should be able to test each system individually - ablility to switch on or off

Each system should have start and end trade times for 2 sets of risk settings

Only send orders if enough margin

If variation margin call then close the worst position - cannot ever have a margin call

A trade rotator must take profits if no action in n minutes

*/