//< 1. Property >==============================================================================================//<  1>
                                                                                                               //<  2>
#property     copyright                 "Copyright (C) 2009, MetaQuotes Software Corp."                        //<  3>
#property     link                      "http://www.metaquotes.net"                                            //<  4>
                                                                                                               //<  5>
#define       A.System.Robot            "AIS1 Trading Robot"                                                   //<  6>
#define       A.System.Strategy         "A System: EURUSD Daily Metrics"                                       //<  7>
#define       A.System.ReleaseDate      "09.02.2009"                                                           //<  8>
#define       A.System.ReleaseCode      "1"                                                                    //<  9>
#define       A.System.Programmer       "Airat Safin                            http://www.mql4.com/users/Ais" //< 10>
                                                                                                               //< 11>
//</1. Property >==============================================================================================//< 12>
                                                                                                               //< 13>
//< 2. Constants >=============================================================================================//< 14>
                                                                                                               //< 15>
#define       acs.Symbol                "EURUSD"                                                               //< 16>
#define       aci.OrderID               0                                                                      //< 17>
#define       acd.TrailStepping         1.0                                                                    //< 18>
                                                                                                               //< 19>
#define       aci.Index.1               7                                                                      //< 20>
#define       aci.Index.2               6                                                                      //< 21>
int           aci.Timeframe             []  = { 0 , 1 , 5 , 15 , 30 , 60 , 240 , 1440 , 10080 , 43200 }      ; //< 22>
                                                                                                               //< 23>
//</2. Constants >=============================================================================================//< 24>
                                                                                                               //< 25>
//< 3. Input >=================================================================================================//< 26>
                                                                                                               //< 27>
       double aed.AccountReserve      = 0.20                                                                 ; //< 28>
       double aed.OrderReserve        = 0.04                                                                 ; //< 29>
       double aed.TakeFactor          = 0.8                                                                  ; //< 30>
       double aed.StopFactor          = 1.0                                                                  ; //< 31>
       double aed.TrailFactor         = 5.0                                                                  ; //< 32>
                                                                                                               //< 33>
//</3. Input >=================================================================================================//< 34>
                                                                                                               //< 35>
//< 4. Global Variables >======================================================================================//< 36>
                                                                                                               //< 37>
double        avi.TimeStamp                                                                                  ; //< 38>
double        avd.MaximalEquity                                                                              ; //< 39>
double        avd.DrawdownLimit                                                                              ; //< 40>
                                                                                                               //< 41>
//</4. Global Variables >======================================================================================//< 42>
                                                                                                               //< 43>
//< 5. Program Initialization >================================================================================//< 44>
                                                                                                               //< 45>
int    init   ()                                                                                               //< 46>
{                                                                                                              //< 47>
avi.TimeStamp            = TimeLocal     ()                                                                  ; //< 48>
avd.MaximalEquity        = AccountEquity ()                                                                  ; //< 49>
avd.DrawdownLimit        = aed.AccountReserve      - aed.OrderReserve                                        ; //< 50>
                                                                                                               //< 51>
Alert                    ( ""                                                                              ) ; //< 52>
Alert                    ( A.System.Robot          , ": Reload code "      , UninitializeReason ()         ) ; //< 53>
}                                                                                                              //< 54>
//</5. Program Initialization >================================================================================//< 55>
                                                                                                               //< 56>
//< 6. Program Deinitialization >==============================================================================//< 57>
                                                                                                               //< 58>
int    deinit ()                                                                                               //< 59>
{                                                                                                              //< 60>
Alert                    ( A.System.Robot          , ": Stop code "        , UninitializeReason ()         ) ; //< 61>
}                                                                                                              //< 62>
//</6. Program Deinitialization >==============================================================================//< 63>
                                                                                                               //< 64>
//< 7. Main Program >==========================================================================================//< 65>
                                                                                                               //< 66>
int    start  ()                                                                                               //< 67>
{                                                                                                              //< 68>
//< 7.1. Trading Pause Control >-------------------------------------------------------------------------------//< 69>
                                                                                                               //< 70>
if   ( TimeLocal     ()  - avi.TimeStamp       < 5 )                                                  return ; //< 71>
                                                                                                               //< 72>
//</7.1. Trading Pause Control >-------------------------------------------------------------------------------//< 73>
                                                                                                               //< 74>
//< 7.2. Equity Control >--------------------------------------------------------------------------------------//< 75>
                                                                                                               //< 76>
if   ( AccountEquity ()  > avd.MaximalEquity   )     avd.MaximalEquity     = AccountEquity      ()           ; //< 77>
if   ( AccountEquity ()  < avd.MaximalEquity * ( 1 - avd.DrawdownLimit ) )                            return ; //< 78>
                                                                                                               //< 79>
//</7.2. Equity Control >--------------------------------------------------------------------------------------//< 80>
                                                                                                               //< 81>
//< 7.3. Data Feed >-------------------------------------------------------------------------------------------//< 82>
                                                                                                               //< 83>
double ald.QuoteAsk      = MarketInfo ( acs.Symbol , MODE_ASK            )                                   ; //< 84>
double ald.QuoteBid      = MarketInfo ( acs.Symbol , MODE_BID            )                                   ; //< 85>
double ald.QuotePoint    = MarketInfo ( acs.Symbol , MODE_POINT          )                                   ; //< 86>
double ald.QuoteSpread   = MarketInfo ( acs.Symbol , MODE_SPREAD         ) * ald.QuotePoint                  ; //< 87>
double ald.QuoteStops    = MarketInfo ( acs.Symbol , MODE_STOPLEVEL      ) * ald.QuotePoint                  ; //< 88>
double ald.QuoteTick     = MarketInfo ( acs.Symbol , MODE_TICKSIZE       )                                   ; //< 89>
double ald.NominalTick   = MarketInfo ( acs.Symbol , MODE_TICKVALUE      )                                   ; //< 90>
double ald.NominalMargin = MarketInfo ( acs.Symbol , MODE_MARGINREQUIRED )                                   ; //< 91>
double ald.NominalLot    = MarketInfo ( acs.Symbol , MODE_LOTSIZE        )                                   ; //< 92>
double ald.MaximumLots   = MarketInfo ( acs.Symbol , MODE_MAXLOT         )                                   ; //< 93>
double ald.MinimumLots   = MarketInfo ( acs.Symbol , MODE_MINLOT         )                                   ; //< 94>
double ald.LotStep       = MarketInfo ( acs.Symbol , MODE_LOTSTEP        )                                   ; //< 95>
int    ald.Digits        = MarketInfo ( acs.Symbol , MODE_DIGITS         )                                   ; //< 96>
                                                                                                               //< 97>
int    ali.Period.1      = aci.Timeframe           [ aci.Index.1         ]                                   ; //< 98>
int    ali.Period.2      = aci.Timeframe           [ aci.Index.2         ]                                   ; //< 99>
                                                                                                               //<100>
double ald.Low.1         = iLow       ( acs.Symbol , ali.Period.1 , 1    )                                   ; //<101>
double ald.High.1        = iHigh      ( acs.Symbol , ali.Period.1 , 1    )                                   ; //<102>
double ald.Close.1       = iClose     ( acs.Symbol , ali.Period.1 , 1    )                                   ; //<103>
                                                                                                               //<104>
double ald.Low.2         = iLow       ( acs.Symbol , ali.Period.2 , 1    )                                   ; //<105>
double ald.High.2        = iHigh      ( acs.Symbol , ali.Period.2 , 1    )                                   ; //<106>
double ald.Close.2       = iClose     ( acs.Symbol , ali.Period.2 , 1    )                                   ; //<107>
                                                                                                               //<108>
double ald.Average.1     =            ( ald.High.1 + ald.Low.1           ) / 2                               ; //<109>
                                                                                                               //<110>
double ald.Range.1       = ald.High.1              - ald.Low.1                                               ; //<111>
double ald.Range.2       = ald.High.2              - ald.Low.2                                               ; //<112>
                                                                                                               //<113>
double ald.QuoteTake     = ald.Range.1             * aed.TakeFactor                                          ; //<114>
double ald.QuoteStop     = ald.Range.1             * aed.StopFactor                                          ; //<115>
double ald.QuoteTrail    = ald.Range.2             * aed.TrailFactor                                         ; //<116>
                                                                                                               //<117>
double ald.TrailStep     = ald.QuoteSpread         * acd.TrailStepping                                       ; //<118>
                                                                                                               //<119>
//</7.3. Data Feed >-------------------------------------------------------------------------------------------//<120>
                                                                                                               //<121>
//< 7.4. Strategy Outputs Reset >------------------------------------------------------------------------------//<122>
                                                                                                               //<123>
int    ali.Command       = EMPTY                                                                             ; //<124>
double ald.Price         = EMPTY                                                                             ; //<125>
double ald.Stop          = EMPTY                                                                             ; //<126>
double ald.Take          = EMPTY                                                                             ; //<127>
double ald.Risk          = EMPTY                                                                             ; //<128>
                                                                                                               //<129>
//</7.4. Strategy Outputs Reset >------------------------------------------------------------------------------//<130>
                                                                                                               //<131>
//< 7.5. Trailing Stop >---------------------------------------------------------------------------------------//<132>
                                                                                                               //<133>
if ( OrdersTotal ()      > 0 )                                                                                 //<134>
   { int   i , N ; N     = OrdersTotal ()                                                                    ; //<135>
     for ( i = 0 ; i     < N ; i ++     )                                                                      //<136>
       { OrderSelect     ( i ,  SELECT_BY_POS      , MODE_TRADES           )                                 ; //<137>
                                                                                                               //<138>
         if ( OrderType   () == OP_BUY                                                                     )   //<139>
         if ( OrderProfit ()  > 0                                                                          )   //<140>
         if ( ald.QuoteTrail  > ald.QuoteStops                                                             )   //<141>
         if ( ald.QuoteBid    < OrderTakeProfit ()                         - ald.QuoteStops                )   //<142>
         if ( ald.QuoteBid    > OrderStopLoss   () + ald.TrailStep         + ald.QuoteTrail                )   //<143>
              ald.Stop        = NormalizeDouble (    ald.QuoteBid          - ald.QuoteTrail   , ald.Digits ) ; //<144>
                                                                                                               //<145>
         if ( OrderType   () == OP_SELL                                                                    )   //<146>
         if ( OrderProfit ()  > 0                                                                          )   //<147>
         if ( ald.QuoteTrail  > ald.QuoteStops                                                             )   //<148>
         if ( ald.QuoteAsk    > OrderTakeProfit ()                         + ald.QuoteStops                )   //<149>
         if ( ald.QuoteAsk    < OrderStopLoss   () - ald.TrailStep         - ald.QuoteTrail                )   //<150>
              ald.Stop        = NormalizeDouble (    ald.QuoteAsk          + ald.QuoteTrail   , ald.Digits ) ; //<151>
                                                                                                               //<152>
         if ( ald.Stop        > 0 )                                                                            //<153>
            { OrderModify     ( OrderTicket     () , OrderOpenPrice ()     , ald.Stop         ,                //<154>
                                OrderTakeProfit () , 0 , 0                                                 ) ; //<155>
              avi.TimeStamp   = TimeLocal       ()                                                           ; //<156>
              return                                                                                   ; } } } //<157>
                                                                                                               //<158>
//</7.5. Trailing Stop >---------------------------------------------------------------------------------------//<159>
                                                                                                               //<160>
//< 7.6. Strategy Logic >--------------------------------------------------------------------------------------//<161>
                                                                                                               //<162>
if ( OrdersTotal () > 0             )                                                                 return ; //<163>
                                                                                                               //<164>
if ( ald.Close.1  > ald.Average.1   )                                                                          //<165>
if ( ald.QuoteAsk > ald.High.1      )                                                                          //<166>
   { ali.Command  = OP_BUY                                                                                   ; //<167>
     ald.Price    = ald.QuoteAsk                                                                             ; //<168>
     ald.Stop     = NormalizeDouble ( ald.High.1   - ald.QuoteStop                            , ald.Digits ) ; //<169>
     ald.Take     = NormalizeDouble ( ald.QuoteAsk + ald.QuoteTake                            , ald.Digits ) ; //<170>
     ald.Risk     = ald.Price       - ald.Stop                                                             ; } //<171>
                                                                                                               //<172>
if ( ald.Close.1  < ald.Average.1   )                                                                          //<173>
if ( ald.QuoteBid < ald.Low.1       )                                                                          //<174>
   { ali.Command  = OP_SELL                                                                                  ; //<175>
     ald.Price    = ald.QuoteBid                                                                             ; //<176>
     ald.Stop     = NormalizeDouble ( ald.Low.1    + ald.QuoteStop                            , ald.Digits ) ; //<177>
     ald.Take     = NormalizeDouble ( ald.QuoteBid - ald.QuoteTake                            , ald.Digits ) ; //<178>
     ald.Risk     = ald.Stop        - ald.Price                                                            ; } //<179>
                                                                                                               //<180>
//</7.6. Strategy Logic >--------------------------------------------------------------------------------------//<181>
                                                                                                               //<182>
//< 7.7. Trading Module >--------------------------------------------------------------------------------------//<183>
                                                                                                               //<184>
//< 7.7.1. Entry Point >                                                                                       //<185>
if (   IsTradeAllowed     ()   )                                                                               //<186>
if ( ! IsTradeContextBusy ()   )                                                                               //<187>
if (   ali.Command > EMPTY     )                                                                               //<188>
   {                                                                                                           //<189>
//</7.7.1. Entry Point >                                                                                       //<190>
                                                                                                               //<191>
//< 7.7.2. Risk Management >                                                                                   //<192>
     double ald.NominalPoint   = ald.NominalTick   * ald.QuotePoint        / ald.QuoteTick                   ; //<193>
     int    ali.PositionPoints = MathRound         ( ald.NominalMargin     / ald.NominalPoint              ) ; //<194>
     int    ali.RiskPoints     = MathRound         ( ald.Risk              / ald.QuotePoint                ) ; //<195>
     double ald.VARLimit       = AccountEquity ()  * aed.OrderReserve                                        ; //<196>
     double ald.RiskPoint      = ald.VARLimit      / ali.RiskPoints                                          ; //<197>
     double ald.PositionLimit  = ald.RiskPoint     * ali.PositionPoints                                      ; //<198>
     double ald.SizeLimit      = ald.PositionLimit / ald.NominalMargin                                       ; //<199>
//</7.7.2. Risk Management >                                                                                   //<200>
                                                                                                               //<201>
//< 7.7.3. Operation Size Control >                                                                            //<202>
     if   ( ald.SizeLimit     >= ald.MinimumLots )                                                             //<203>
            int ali.Steps      = MathFloor       ( ( ald.SizeLimit         - ald.MinimumLots  )                //<204>
                                                   / ald.LotStep                                           ) ; //<205>
     else                                                                                             return ; //<206>
                                                                                                               //<207>
     double ald.Size           = ald.MinimumLots   + ald.LotStep           * ali.Steps                       ; //<208>
                                                                                                               //<209>
     if   ( ald.Size           > ald.MaximumLots )                                                             //<210>
            ald.Size           = ald.MaximumLots                                                             ; //<211>
                                                                                                               //<212>
     if   ( AccountFreeMarginCheck (  acs.Symbol   , ali.Command           , ald.Size ) <= 0                   //<213>
         || GetLastError  ()  == 134                                                          )       return ; //<214>
                                                                                                               //<215>
//</7.7.3. Operation Size Control >                                                                            //<216>
                                                                                                               //<217>
//< 7.7.4. Order Send >                                                                                        //<218>
     OrderSend ( acs.Symbol    , ali.Command       , ald.Size              , ald.Price   , 0  ,                //<219>
                                 ald.Stop          , ald.Take         , "" , aci.OrderID , 0  , 0          ) ; //<220>
     avi.TimeStamp             = TimeLocal ()                                                                ; //<221>
//</7.7.4. Order Send >                                                                                        //<222>
                                                                                                               //<223>
//< 7.7.5. Exit Point >                                                                                        //<224>
   }                                                                                                           //<225>
//</7.7.5. Exit Point >                                                                                        //<226>
                                                                                                               //<227>
//</7.7. Trading Module >--------------------------------------------------------------------------------------//<228>
}                                                                                                              //<229>
//</7. Main Program >==========================================================================================//<230>

