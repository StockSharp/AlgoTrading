double avd.OperationVolume                                                                          ; // 01
                                                                                                      // 02
double avd.VARLimit  = 200.00                                                                       ; // 03
int    avi.VARPoints = 50                                                                           ; // 04
                                                                                                      // 05
//<start>                                                                                             // 06
int    start                                                                                          // 07
 (//<0>                                                                                               // 08
){//<1>                                                                                               // 09
                                                                                                      // 10
avd.OperationVolume = amd.OperationVolume ( Symbol () , avd.VARLimit , avi.VARPoints ) ;              // 11
                                                                                                      // 12
}//</start>                                                                                           // 13
                                                                                                      // 14
//<amd.OperationVolume>                                                                               // 15
double amd.OperationVolume                                                                            // 16
 (//<3>                                                                                               // 17
       string aas.Symbol    ,                                                                         // 18
       double aad.VARLimit  ,                                                                         // 19
       int    aai.VARPoints                                                                           // 20
){//<31>                                                                                              // 21
                                                                                                      // 22
double ald.VolumeStep      = MarketInfo ( aas.Symbol , MODE_LOTSTEP        )                        ; // 23
double ald.MinimalVolume   = MarketInfo ( aas.Symbol , MODE_MINLOT         )                        ; // 24
double ald.NominalLot      = MarketInfo ( aas.Symbol , MODE_LOTSIZE        )                        ; // 25
double ald.NominalMargin   = MarketInfo ( aas.Symbol , MODE_MARGINREQUIRED )                        ; // 26
double ald.NominalTick     = MarketInfo ( aas.Symbol , MODE_TICKVALUE      )                        ; // 27
double ald.QuoteTick       = MarketInfo ( aas.Symbol , MODE_TICKSIZE       )                        ; // 28
double ald.QuotePoint      = MarketInfo ( aas.Symbol , MODE_POINT          )                        ; // 29
                                                                                                      // 30
double ald.MinimalMargin   = ald.NominalMargin       * ald.MinimalVolume                            ; // 31
double ald.MinimalTick     = ald.NominalTick         * ald.MinimalVolume                            ; // 32
double ald.MinimalPoint    = ald.MinimalTick         * ald.QuotePoint    / ald.QuoteTick            ; // 33
int    ali.PositionPoints  = MathRound               ( ald.MinimalMargin / ald.MinimalPoint )       ; // 34
                                                                                                      // 35
double ald.VARPositions    = aai.VARPoints * 1.0     / ali.PositionPoints                           ; // 36
double ald.MarginLimit     = avd.VARLimit            / ald.VARPositions                             ; // 37
double ald.VolumeLimit     = ald.MarginLimit         / ald.NominalMargin                            ; // 38
                                                                                                      // 39
double ald.OperationVolume = 0                                                                      ; // 40
                                                                                                      // 41
if   ( ald.VolumeLimit    >= ald.MinimalVolume )                                                      // 42
     { int ali.Steps       = MathFloor ( ( ald.VolumeLimit - ald.MinimalVolume ) / ald.VolumeStep ) ; // 43
       ald.OperationVolume = ald.MinimalVolume + ald.VolumeStep * ali.Steps                       ; } // 44
                                                                                                      // 45
string als.LotMeasure      , als.BaseCurrency                                                       ; // 46
                                                                                                      // 47
if   ( AccountLeverage () != NormalizeDouble ( ald.NominalLot / ald.NominalMargin , 1 ) )             // 48
     { als.BaseCurrency    = StringSubstr    ( aas.Symbol , 0 , 3 )                                 ; // 49
       als.LotMeasure      = als.BaseCurrency                                                     ; } // 50
else   als.LotMeasure      = AccountCurrency ()                                                     ; // 51
                                                                                                      // 52
Alert  ( " "                                                                                      ) ; // 53
Alert  ( "ald.OperationVolume = " , ald.OperationVolume , " " , "lots"                            ) ; // 54
Alert  ( "Output :"                                                                               ) ; // 55
Alert  ( " "                                                                                      ) ; // 56
Alert  ( "ald.MinimalVolume   = " , ald.MinimalVolume   , " " , "lots"                            ) ; // 57
Alert  ( "ald.VolumeStep      = " , ald.VolumeStep      , " " , "lots"                            ) ; // 58
Alert  ( "ald.VolumeLimit     = " , ald.VolumeLimit     , " " , "lots"                            ) ; // 59
Alert  ( "ald.MarginLimit     = " , ald.MarginLimit     , " " , AccountCurrency ()                ) ; // 60
Alert  ( "aad.VARLimit        = " , aad.VARLimit        , " " , AccountCurrency ()                ) ; // 61
Alert  ( "ald.VARPositions    = " , ald.VARPositions    , " " , "positions"                       ) ; // 62
Alert  ( "aai.VARPoints       = " , aai.VARPoints       , " " , "points"                          ) ; // 63
Alert  ( "ali.PositionPoints  = " , ali.PositionPoints  , " " , "points"                          ) ; // 64
Alert  ( "ald.MinimalPoint    = " , ald.MinimalPoint    , " " , AccountCurrency ()                ) ; // 65
Alert  ( "ald.MinimalMargin   = " , ald.MinimalMargin   , " " , AccountCurrency ()                ) ; // 66
Alert  ( "ald.NominalMargin   = " , ald.NominalMargin   , " " , AccountCurrency ()                ) ; // 67
Alert  ( "ald.NominalLot      = " , ald.NominalLot      , " " , als.LotMeasure                    ) ; // 68
Alert  ( "Processing :"                                                                           ) ; // 69
Alert  ( " "                                                                                      ) ; // 60
Alert  ( "aai.VARPoints       = " , aai.VARPoints       , " " , "points"                          ) ; // 71
Alert  ( "aad.VARLimit        = " , aad.VARLimit        , " " , AccountCurrency ()                ) ; // 72
Alert  ( "aas.Symbol          = " , aas.Symbol                                                    ) ; // 73
Alert  ( "Input :"                                                                                ) ; // 74
Alert  ( " "                                                                                      ) ; // 75
Alert  ( "AccountLeverage ()  = " , AccountLeverage ()                                            ) ; // 76
Alert  ( "AccountCurrency ()  = " , AccountCurrency ()                                            ) ; // 77
                                                                                                      // 78
return ( ald.OperationVolume )                                                                      ; // 79
                                                                                                      // 80
}//</amd.OperationVolume>                                                                             // 81