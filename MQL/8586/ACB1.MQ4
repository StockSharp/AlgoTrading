int    start () {                                                                                                 //<  1>
                                                                                                                  //<  2>
                                                                                                                  //<  3>
static double             ai.TimeStamp                                                                          ; //<  4>
if   ( TimeLocal ()     - ai.TimeStamp     < 5 )   return                                                       ; //<  5>
                                                                                                                  //<  6>
static double             ad.MaximalEquity                                                                      ; //<  7>
if   ( AccountEquity () > ad.MaximalEquity     )   ad.MaximalEquity = AccountEquity ()                          ; //<  8>
if   ( AccountEquity () < ad.MaximalEquity / 2 )   return                                                       ; //<  9>
                                                                                                                  //< 10>
string as.Symbol        = "EURUSD"                                                                              ; //< 11>
double ad.QuoteAsk      = MarketInfo ( as.Symbol , MODE_ASK            )                                        ; //< 12>
double ad.QuoteBid      = MarketInfo ( as.Symbol , MODE_BID            )                                        ; //< 13>
double ad.QuotePoint    = MarketInfo ( as.Symbol , MODE_POINT          )                                        ; //< 14>
double ad.QuoteSpread   = MarketInfo ( as.Symbol , MODE_SPREAD         ) * ad.QuotePoint                        ; //< 15>
double ad.QuoteStops    = MarketInfo ( as.Symbol , MODE_STOPLEVEL      ) * ad.QuotePoint                        ; //< 16>
double ad.QuoteTick     = MarketInfo ( as.Symbol , MODE_TICKSIZE       )                                        ; //< 17>
double ad.NominalTick   = MarketInfo ( as.Symbol , MODE_TICKVALUE      )                                        ; //< 18>
double ad.NominalMargin = MarketInfo ( as.Symbol , MODE_MARGINREQUIRED )                                        ; //< 19>
double ad.NominalLot    = MarketInfo ( as.Symbol , MODE_LOTSIZE        )                                        ; //< 20>
double ad.MinimumLots   = MarketInfo ( as.Symbol , MODE_MINLOT         )                                        ; //< 21>
double ad.LotStep       = MarketInfo ( as.Symbol , MODE_LOTSTEP        )                                        ; //< 22>
int    ad.Digits        = MarketInfo ( as.Symbol , MODE_DIGITS         )                                        ; //< 23>
                                                                                                                  //< 24>
int    ai.Command       = EMPTY                                                                                 ; //< 25>
double ad.Price         = EMPTY                                                                                 ; //< 26>
double ad.Stop          = EMPTY                                                                                 ; //< 27>
double ad.Take          = EMPTY                                                                                 ; //< 28>
double ad.Risk          = EMPTY                                                                                 ; //< 29>
                                                                                                                  //< 30>
double ad.RiskLimit     = 0.05                                                                                  ; //< 31>
int    ai.Index.1       = 7                                                                                     ; //< 32>
int    ai.Index.2       = 6                                                                                     ; //< 33>
double ad.TakeFactor    = 0.8                                                                                   ; //< 34>
double ad.TrailFactor   = 10                                                                                    ; //< 35>
                                                                                                                  //< 36>
int    ac.Timeframe []  = { 0 , 1 , 5 , 15 , 30 , 60 , 240 , 1440 , 10080 , 43200 }                             ; //< 37>
int    ai.Period.1      = ac.Timeframe [ ai.Index.1 ]                                                           ; //< 38>
int    ai.Period.2      = ac.Timeframe [ ai.Index.2 ]                                                           ; //< 39>
                                                                                                                  //< 40>
double ad.Low.1         = iLow   ( as.Symbol , ai.Period.1 , 1 )                                                ; //< 41>
double ad.High.1        = iHigh  ( as.Symbol , ai.Period.1 , 1 )                                                ; //< 42>
double ad.Open.1        = iOpen  ( as.Symbol , ai.Period.1 , 1 )                                                ; //< 43>
double ad.Close.1       = iClose ( as.Symbol , ai.Period.1 , 1 )                                                ; //< 44>
                                                                                                                  //< 45>
double ad.Low.2         = iLow   ( as.Symbol , ai.Period.2 , 1 )                                                ; //< 46>
double ad.High.2        = iHigh  ( as.Symbol , ai.Period.2 , 1 )                                                ; //< 47>
double ad.Open.2        = iOpen  ( as.Symbol , ai.Period.2 , 1 )                                                ; //< 48>
double ad.Close.2       = iClose ( as.Symbol , ai.Period.2 , 1 )                                                ; //< 49>
                                                                                                                  //< 50>
double ad.QuoteTake     = ( ad.High.1 - ad.Low.1 ) * ad.TakeFactor                                              ; //< 51>
double ad.QuoteTrail    = ( ad.High.2 - ad.Low.2 ) * ad.TrailFactor                                             ; //< 52>
                                                                                                                  //< 53>
                                                                                                                  //< 54>
if ( OrdersTotal ()     > 0 )                                                                                     //< 55>
     for ( int j = 0 ;    j  < OrdersTotal () ; j ++ )                                                            //< 56>
         { OrderSelect  ( j  , SELECT_BY_POS  , MODE_TRADES )                                                   ; //< 57>
                                                                                                                  //< 58>
           if ( OrderType   () == OP_BUY                                                       )                  //< 59>
           if ( OrderProfit () > 0                                                             )                  //< 60>
           if ( ad.QuoteBid  < OrderTakeProfit ()                  - ad.QuoteStops             )                  //< 61>
           if ( ad.QuoteBid  > OrderStopLoss   ()                  + ad.QuoteTrail             )                  //< 62>
                ad.Stop      = NormalizeDouble ( ad.QuoteBid       - ad.QuoteTrail , ad.Digits )                ; //< 63>
                                                                                                                  //< 64>
           if ( OrderType   () == OP_SELL                                                      )                  //< 65>
           if ( OrderProfit () > 0                                                             )                  //< 66>
           if ( ad.QuoteAsk  > OrderTakeProfit ()                  + ad.QuoteStops             )                  //< 67>
           if ( ad.QuoteAsk  < OrderStopLoss   ()                  - ad.QuoteTrail             )                  //< 68>
                ad.Stop      = NormalizeDouble ( ad.QuoteAsk       + ad.QuoteTrail , ad.Digits )                ; //< 69>
                                                                                                                  //< 70>
           if ( ad.Stop      > 0 )                                                                                //< 71>
           if ( ad.Stop     != NormalizeDouble ( OrderStopLoss  () , ad.Digits ) )                                //< 72>
              { OrderModify  ( OrderTicket ()  , OrderOpenPrice () , ad.Stop , OrderTakeProfit () , 0 , 0 )     ; //< 73>
                ai.TimeStamp = TimeLocal   ()                                                                   ; //< 74>
                return                                                                                      ; } } //< 75>
                                                                                                                  //< 76>
                                                                                                                  //< 77>
if ( OrdersTotal () > 2 ) return                                                                                ; //< 78>
                                                                                                                  //< 79>
if ( ad.Close.1 > ( ad.High.1 + ad.Low.1 ) / 2 )                                                                  //< 80>
if ( ad.High.1  < ad.QuoteAsk                  )                                                                  //< 81>
   { ai.Command = OP_BUY                                                                                        ; //< 82>
     ad.Price   = ad.QuoteAsk                                                                                   ; //< 83>
     ad.Stop    = NormalizeDouble ( ad.Low.1                   , ad.Digits )                                    ; //< 84>
     ad.Take    = NormalizeDouble ( ad.QuoteAsk + ad.QuoteTake , ad.Digits )                                    ; //< 85>
     ad.Risk    = ad.Price - ad.Stop                                                                          ; } //< 86>
                                                                                                                  //< 87>
if ( ad.Close.1 < ( ad.High.1 + ad.Low.1 ) / 2 )                                                                  //< 88>
if ( ad.Low.1   > ad.QuoteBid                  )                                                                  //< 89>
   { ai.Command = OP_SELL                                                                                       ; //< 90>
     ad.Price   = ad.QuoteBid                                                                                   ; //< 91>
     ad.Stop    = NormalizeDouble ( ad.High.1                  , ad.Digits )                                    ; //< 92>
     ad.Take    = NormalizeDouble ( ad.QuoteBid - ad.QuoteTake , ad.Digits )                                    ; //< 93>
     ad.Risk    = ad.Stop - ad.Price                                                                          ; } //< 94>
                                                                                                                  //< 95>
if ( IsTradeAllowed       () )                                                                                    //< 96>
if ( ! IsTradeContextBusy () )                                                                                    //< 97>
if ( ai.Command > EMPTY      )                                                                                    //< 98>
   { int    ai.Position      = MathRound ( ad.NominalMargin / ad.NominalTick * ad.QuoteTick / ad.QuotePoint )   ; //< 99>
     int    ai.RiskPoints    = ad.Risk          / ad.QuotePoint                                                 ; //<100>
     double ad.VARLimit      = AccountEquity () * ad.RiskLimit                                                  ; //<101>
     double ad.RiskPoint     = ad.VARLimit      / ai.RiskPoints                                                 ; //<102>
     double ad.PositionLimit = ai.Position      * ad.RiskPoint                                                  ; //<103>
     double ad.VolumeLimit   = ad.PositionLimit / ad.NominalMargin                                              ; //<104>
                                                                                                                  //<105>
     if   ( ad.VolumeLimit  >= ad.MinimumLots )                                                                   //<106>
            int ai.Steps     = MathFloor ( ( ad.VolumeLimit - ad.MinimumLots ) / ad.LotStep )                   ; //<107>
     else   return                                                                                              ; //<108>
                                                                                                                  //<109>
     double ad.Volume        = ad.MinimumLots + ad.LotStep * ai.Steps                                           ; //<110>
     if   ( ad.Volume        > 5.0 )                                                                              //<111>
            ad.Volume        = 5.0                                                                              ; //<112>
                                                                                                                  //<113>
     if ( AccountFreeMarginCheck ( as.Symbol , ai.Command , ad.Volume ) <= 0 || GetLastError () == 134 ) return ; //<114>
                                                                                                                  //<115>
     OrderSend    ( as.Symbol , ai.Command , ad.Volume , ad.Price , 0 , ad.Stop , ad.Take , "" , 0 , 0 , 0 )    ; //<116>
     ai.TimeStamp = TimeLocal ()                                                                              ; } //<117>
}                                                                                                                 //<118>