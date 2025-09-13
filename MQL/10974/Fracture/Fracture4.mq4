#property copyright "Equita"
#property link "Equita"
#define MAGIC 33788
extern string TOOLS = ".............................................................................................................";
extern bool CloseAll = false;
extern bool ContinueTrading = true;
extern string TRADING = ".............................................................................................................";
extern int QueryHistory = 22;
extern double BasketProfit = 1.1;
extern double OpenProfit = 1.1;
extern double TradeSpace = 1.2;
extern double RangingMarket = 0.7;
extern double DynamicSlippage = 0.5;
extern string RISK = ".............................................................................................................";
extern int MaxTrades = 30;
extern double BaseLotSize = 0.01;
extern double RangeUsage = 0.03;
extern double TrendUsage = 0.07;
extern string INDICATOR_ATR = ".............................................................................................................";
extern int ATRTimeFrame = 0;
extern int ATRPeriod = 14;
extern int ATRShift = 0;
extern string INDICATOR_MA = ".............................................................................................................";
extern int MATimeFrame = 0;
extern int MA1Period = 8;
extern int MA2Period = 14;
extern int MA3Period = 60;
extern double MARange = 1.0;
extern int MMAShift = 0;
extern int MAShift = 0;
extern string INDICATOR_ADX = ".............................................................................................................";
extern double ADXLine = 35;
extern int ADXTimeFrame = 0;
extern int ADXPeriod = 14;
extern int ADXShift = 0;
extern string INDICATOR_FRACTAL = ".............................................................................................................";
extern int FractalTimeFrame = 0;
extern int FractalShift = 1;

double slippage, marginRequirement, lotSize, recentProfit, lastProfit, totalHistoryProfit, 
historyProfitCount, totalProfit, totalLoss, symbolHistory, numberHistoryProfits, 
eATR, eATRPrev, eADX, MA1Cur, MA2Cur, MA3Cur, MA1Prev, MA2Prev, MA3Prev ;

int digits, lastProfitType, totalTrades, lastTime, recentTicket, firstTime, secondTime;

int recentType = -1; 
int totalHistory = 100;
int barSeconds = 0;
double pipPoints = 0.00010;
double fractalUp = 0;
double fractalDown = 0;
double fractalUpPrice = 0 ;
double fractalDownPrice = 0;
double fractalUpPriceFirst = 0 ;
double fractalDownPriceFirst = 0;
double fractalHighPrice = 0;
double fractalLowPrice = 0;
double fractalHighPriceFirst = 0;
double fractalLowPriceFirst = 0;
bool nearLongPosition = false;
bool nearShortPosition = false;
double trendStrength = 0;
double drawdown = 0;
bool longTrendUp = false;
bool longTrendDown = false;
bool shortTrendUp = false;
bool shortTrendDown = false;
bool rangingMarket = false;
string display = "\n"; 

int init(){ 
   prepare() ; 
   return( 0 );
}

double marginCalculate( string symbol, double volume ){ 
   return ( MarketInfo( symbol, MODE_MARGINREQUIRED ) * volume ) ; 
} 

void lotSize(){ 
   if( rangingMarket ) lotSize = NormalizeDouble( ( AccountFreeMargin() * RangeUsage / marginRequirement ) * BaseLotSize , 2 ) ;
   else {
      lotSize = NormalizeDouble( ( AccountFreeMargin() * TrendUsage / marginRequirement ) * BaseLotSize * trendStrength, 2 ) ;
   }
   if( lotSize < 0.01 ) lotSize = 0.01;
   Print(lotSize);
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
         if( ( OrderStopLoss() == 0 && OrderProfit() > 0 && type == "profits" ) || ( OrderType() == OP_BUY && type == "long" ) || ( OrderType() == OP_SELL && type == "short" ) || type == "none" ){
            if( OrderType() == OP_BUY ) OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            if( OrderType() == OP_SELL ) OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
         }
      }
   }
}

void prepareFractals(){
   fractalUp = iFractals( NULL, FractalTimeFrame, MODE_UPPER, FractalShift );
   fractalDown = iFractals( NULL, FractalTimeFrame, MODE_LOWER, FractalShift );
   fractalUpPrice = 0 ;
   fractalDownPrice = 0;
   fractalUpPriceFirst = 0 ;
   fractalDownPriceFirst = 0;
   fractalHighPrice = 0;
   fractalLowPrice = 0;
   fractalHighPriceFirst = 0;
   fractalLowPriceFirst = 0;
   bool iup = false;
   bool iupp = false;
   bool idn = false;
   bool idnn = false;
   for( int i = 0; i < 999; i++ ){ 
      double ifractalUp = iFractals( NULL, 0, MODE_UPPER, i );
      double ifractalDown = iFractals( NULL, 0, MODE_LOWER, i );
      if( ifractalUp > 0 && Open[i] > Open[0] ){
         iup = true;
         if( Open[i] > Close[i] ){
            fractalUpPrice = Open[i];
            fractalHighPrice = High[i];
         } else {
            fractalUpPrice = Close[i];
            fractalHighPrice = High[i];
         }
      }
      if( ifractalDown > 0 && Open[i] < Open[0] ){
         idn = true;
         if( Open[i] < Close[i] ){
            fractalDownPrice = Open[i];
            fractalLowPrice = Low[i];
         } else {
            fractalDownPrice = Close[i];
            fractalLowPrice = Low[i];
         }
      }
      if( iup && idn ) break;
   }
   iup = false;
   idn = false;
   for( i = 0; i < 999; i++ ){ 
      ifractalUp = iFractals( NULL, 0, MODE_UPPER, i );
      ifractalDown = iFractals( NULL, 0, MODE_LOWER, i );
      if( ifractalUp > 0 ){
         iup = true;
         if( Open[i] > Close[i] ){
            fractalUpPriceFirst = Open[i];
            fractalHighPriceFirst = High[i];
         } else {
            fractalUpPriceFirst = Close[i];
            fractalHighPriceFirst = High[i];
         }
      }
      if( ifractalDown > 0 ){
         idn = true;
         if( Open[i] < Close[i] ){
            fractalDownPriceFirst = Open[i];
            fractalLowPriceFirst = Low[i];
         } else {
            fractalDownPriceFirst = Close[i];
            fractalLowPriceFirst = Low[i];
         }
      }
      if( iup && idn ) break;
   }
}

void update(){
   display = "";
   display = display + " Space: " + DoubleToStr( TradeSpace * eATR / pipPoints, 1 ) ;  
   display = display + " Open: " + DoubleToStr( totalTrades, 0 ) + "/" + DoubleToStr( MaxTrades, 0 );  
   display = display + " Prof: " + DoubleToStr( totalProfit, 2 ) ;
   display = display + " Loss: " + DoubleToStr( totalLoss, 2 ) ;
   display = display + " Hist: " + DoubleToStr( totalHistoryProfit, 2 ) ;
   display = display + " DD: " + DoubleToStr( drawdown, 2 ) ;
   display = display + " Trend Strength: " + DoubleToStr( trendStrength, 2 ) ;
   display = display + " Long Up: " + DoubleToStr( longTrendUp, 0 ) ;
   display = display + " Long Down: " + DoubleToStr( longTrendDown, 0 ) ;
   display = display + " Short Up: " + DoubleToStr( shortTrendUp, 0 ) ;
   display = display + " Short Down: " + DoubleToStr( shortTrendDown, 0 ) ;
   display = display + " Ranging: " + DoubleToStr( rangingMarket, 0 ) ;
   Comment( display );
} 

void prepareHistory(){
   bool firstSymbol = false;
   symbolHistory = 0;
   numberHistoryProfits = 0;
   totalHistoryProfit = 0;
   for( int iPos = OrdersHistoryTotal() - 1 ; iPos > ( OrdersHistoryTotal() - 1 ) - totalHistory; iPos-- ){
      OrderSelect( iPos, SELECT_BY_POS, MODE_HISTORY ) ;
      double QueryHistoryDouble = ( double ) QueryHistory;
      if( symbolHistory >= QueryHistoryDouble ) break;
      if( OrderSymbol() == Symbol() ){
         if( !firstSymbol ) {
            firstSymbol = true;
            lastProfit = OrderProfit() ;
            lastProfitType = OrderType() ; 
            lastTime = MathAbs( TimeCurrent() - OrderCloseTime() ); 
         }
         if( OrderProfit() > 0 ) numberHistoryProfits = numberHistoryProfits + 1; 
         totalHistoryProfit = totalHistoryProfit + OrderProfit() ;
         symbolHistory = symbolHistory + 1 ;
      }
   }
}

void preparePositions() {
   nearLongPosition = false;
   nearShortPosition = false;
   firstTime = 999999;
   secondTime = 999999;
   totalTrades = 0;
   recentProfit = 0;
   recentTicket = 0;
   recentType = -1 ;
   totalProfit = 0;
   totalLoss = 0;
   for( int i = 0 ; i < OrdersTotal() ; i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if( OrderSymbol() == Symbol() && OrderStopLoss() == 0 ) {
         if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace ) nearLongPosition = true ;
         if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace ) nearShortPosition = true ;
         int tradeTime =  TimeCurrent() - OrderOpenTime() ; 
         if( tradeTime < firstTime ) {
            firstTime = tradeTime ;
            recentProfit = OrderProfit() ;
            recentType = OrderType() ; 
            recentTicket = OrderTicket() ; 
         } 
         totalTrades = totalTrades + 1;
         if( OrderProfit() > 0 ) {
            totalProfit = totalProfit + OrderProfit();
         } else {
            totalLoss = totalLoss + OrderProfit();
         }
      }
   }
   for( i = 0 ; i < OrdersTotal() ; i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if( OrderSymbol() == Symbol() && OrderStopLoss() == 0 ) {
         tradeTime =  TimeCurrent() - OrderOpenTime() ; 
         if( tradeTime < secondTime && tradeTime > firstTime ) {
            secondTime = tradeTime ;
            recentProfit = OrderProfit() ;
            recentType = OrderType() ; 
            recentTicket = OrderTicket() ; 
         } 
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

void prepare(){
   prepareIndicators();
   prepareFractals();
   setPipPoint();
   slippage = NormalizeDouble( ( eATR / pipPoints ) * DynamicSlippage, 1 );
   marginRequirement = marginCalculate( Symbol(), BaseLotSize ); 
   trendStrength = MathAbs( MA1Cur - MA3Cur ) / MathAbs( MA2Cur - MA3Cur );
   drawdown = 1 - AccountEquity() / AccountBalance();
   
   prepareHistory();
   preparePositions();
   prepareTrend();
   lotSize();   
   update() ;
} 

void openPosition(){ 
   int type = -1;
   string comment = "Equita Default";
   if(  eADX < ADXLine ){
      if( Close[0] >= fractalUpPrice && Close[0] >=  MA3Cur ) type = OP_BUY;
      else if( Close[0] <= fractalDownPrice && Close[0] <=  MA3Cur) type = OP_SELL; 
   } else {
      if( Close[0] >= fractalUpPrice && Close[0] >=  MA3Cur ) type = OP_SELL;
      else if( Close[0] <= fractalDownPrice && Close[0] <=  MA3Cur) type = OP_BUY; 
   }
   if( !nearLongPosition && type == OP_BUY ) OrderSend( Symbol(), type , lotSize, Ask, slippage, 0, 0, comment, MAGIC ) ;
   else if( !nearShortPosition && type == OP_SELL ) OrderSend( Symbol(), type , lotSize, Bid, slippage, 0, 0, comment, MAGIC ) ;
}

void prepareTrend(){
   if( MathAbs( MA2Cur - MA3Cur ) < eATR * RangingMarket ) {
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
   if( totalHistoryProfit < 0 && MathAbs( totalHistoryProfit ) < totalProfit * BasketProfit  ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit > MathAbs( totalLoss ) * OpenProfit ) closeAll();
   else { 
   }
}

int start() { 
   prepare() ;  
   if( CloseAll ) closeAll() ;
   else {
      if( ContinueTrading && DayOfWeek() != 5 && ( totalTrades < MaxTrades || MaxTrades == 0 ) ) openPosition() ; 
      managePositions() ;
   }
   return( 0 ) ;
}


















