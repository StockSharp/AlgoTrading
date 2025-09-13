#property copyright "Fracture"
#property link "Fracture"
#define MAGIC 20130704
extern string TOOLS = ".............................................................................................................";
extern bool CloseAll = false;
extern bool ContinueTrading = true;
extern string TRADING = ".............................................................................................................";
extern int QueryHistory = 12;
extern int StartTrades = 6;
extern double BasketProfit = 3;
extern double OpenProfit = 1.3;
extern double TradeSpace = 1.1;
extern double RangingMarket = 0.5;
extern double Aggressive = 8;
extern double DynamicSlippage = 1;
extern double MinProfit = 0.5;
extern double TurboStart = 0.07;
extern double TrendReversal = 0.15;
extern string RISK = ".............................................................................................................";
extern int MaxTrades = 13;
extern double BaseLotSize = 0.01;
extern double RangeUsage = 0.15;
extern double TrendUsage = 0.08;
extern string INDICATOR_ATR = ".............................................................................................................";
extern int ATRTimeFrame = 0;
extern int ATRPeriod = 14;
extern int ATRShift = 0;
extern string INDICATOR_MA = ".............................................................................................................";
extern int MATimeFrame = 0;
extern int MA1Period = 5;
extern int MA2Period = 9;
extern int MA3Period = 22;
extern int MMAShift = 0;
extern int MAShift = 0;
extern string INDICATOR_ADX = ".............................................................................................................";
extern double ADXLine = 40;
extern int ADXTimeFrame = 0;
extern int ADXPeriod = 22;
extern int ADXShift = 0;
extern string INDICATOR_FRACTAL = ".............................................................................................................";
extern int FractalTimeFrame = 0;
extern int FractalShift = 1;
extern int FractalBars = 5;

      double buyLots = 0;
      double sellLots = 0;

double slippage, marginRequirement, lotSize, recentProfit, lastProfit, totalHistoryProfit, totalProfit, totalLoss, symbolHistory,
eATR, eATRPrev, eADX, MA1Cur, MA2Cur, MA3Cur, MA1Prev, MA2Prev, MA3Prev ;

int digits, totalTrades;

int totalHistory = 100;
double pipPoints = 0.00010;
double fractalUpPrice = 0 ;
double fractalDownPrice = 0;  
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
   Comment( display );
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

void preparePositions() {
   nearLongPosition = false;
   nearShortPosition = false;
   totalTrades = 0;
   totalProfit = 0;
   totalLoss = 0;
   buyLots = 0;
   sellLots = 0;
   for( int i = 0 ; i < OrdersTotal() ; i++ ) {
      if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
      if(OrderSymbol() == Symbol()){totalTrades = totalTrades + 1;}
      if( OrderSymbol() == Symbol() && OrderStopLoss() == 0 ) {
      
      
      
      
         if( rangingMarket && eADX < ADXLine ){
            if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace ) nearLongPosition = true ;
            if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace ) nearShortPosition = true ;
         } else {
            if( OrderType() == OP_BUY && MathAbs( OrderOpenPrice() - Ask ) < eATR * TradeSpace / Aggressive ) nearLongPosition = true ;
            if( OrderType() == OP_SELL && MathAbs( OrderOpenPrice() - Bid ) < eATR * TradeSpace / Aggressive ) nearShortPosition = true ;
         }
         
         if( OrderType() == OP_BUY ) buyLots = buyLots + OrderLots();
         if( OrderType() == OP_SELL ) sellLots = sellLots + OrderLots(); 
         
         if( OrderProfit() > 0 ) totalProfit = totalProfit + OrderProfit();
         else totalLoss = totalLoss + OrderProfit(); 
      }
   }
}

void closeHedged(){
   double hedged = 0;
   double counted = 0;
   if( buyLots > sellLots ){
      for( int i = 0 ; i < OrdersTotal() ; i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol() && OrderType() == OP_SELL ) {  
            OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
            hedged = OrderLots();
         }
      } 
      for(   i = 0 ; i < OrdersTotal() ; i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol() && OrderType() == OP_BUY && counted <= hedged ) {  
            OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            counted = counted + OrderLots();
         }
      } 
   } else if( buyLots < sellLots ){
      for(  i = 0 ; i < OrdersTotal() ; i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol() && OrderType() == OP_BUY ) {  
            OrderClose( OrderTicket(), OrderLots(), Bid, slippage );
            hedged = OrderLots();
         }
      } 
      for(   i = 0 ; i < OrdersTotal() ; i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol() && OrderType() == OP_SELL && counted <= hedged ) {  
            OrderClose( OrderTicket(), OrderLots(), Ask, slippage );
            counted = counted + OrderLots();
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
   prepareHistory();
   preparePositions();
   prepareTrend();
   lotSize();   
   update() ;
} 

void openPosition(){ 
   int type = -1;
   if( eADX < ADXLine ){
      if( Close[0] >= fractalUpPrice && Close[0] <= MA1Cur ) type = OP_BUY;
      else if( Close[0] <= fractalDownPrice && Close[0] >= MA1Cur) type = OP_SELL;  
   }   
   if( totalTrades < 1 - ( StartTrades / MaxTrades ) || totalTrades == 0 ){
      
     
         if( !nearLongPosition && totalTrades == 0 && type == -1 && ( ( eADX > ADXLine && Close[0] > Open[0] && longTrendUp ) || ( eADX < ADXLine && Close[0] < Open[0] && shortTrendDown ) ) ) OrderSend( Symbol(), OP_SELL, lotSize, Ask, slippage, 0, 0, "scalp", MAGIC );
         else if( !nearShortPosition && totalTrades == 0 && type == -1 && ( ( eADX > ADXLine && Close[0] < Open[0] && longTrendDown ) || ( eADX < ADXLine && Close[0] > Open[0] && shortTrendUp ) ) ) OrderSend( Symbol(), OP_BUY, lotSize, Bid, slippage, 0, 0, "scalp", MAGIC );
     
   }
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
   if( ( totalHistoryProfit < 0 || totalTrades == 1 ) && MathAbs( totalHistoryProfit ) < totalProfit * BasketProfit  ) closeAll( "profits" );
   else if( totalTrades > 1 && totalProfit > MathAbs( totalLoss ) * OpenProfit ) closeAll();
   else {
      for( int i = 0 ; i < OrdersTotal() ; i++ ) {
         if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break;  
         if( OrderSymbol() == Symbol() && OrderComment() == "scalp" ) { 
            if( OrderType() == OP_BUY && Bid > OrderOpenPrice() &&  MathAbs( Bid - OrderOpenPrice() ) > MinProfit * eATR ) 
               OrderClose( OrderTicket(), OrderLots(), Bid, slippage ); 
            else if( OrderType() == OP_SELL && Ask < OrderOpenPrice() && MathAbs( OrderOpenPrice() - Ask ) > MinProfit * eATR )
               OrderClose( OrderTicket(), OrderLots(), Ask, slippage ); 
         }
      } 
   }
}

int start() { 
   prepare() ;  
   if( CloseAll ) closeAll() ;
   else {
      if( ( ContinueTrading || ( !ContinueTrading && totalTrades > 0 ) ) && ( totalTrades < MaxTrades || MaxTrades == 0 ) ) openPosition() ; 
      managePositions() ;
      
   }
   return( 0 ) ;
}