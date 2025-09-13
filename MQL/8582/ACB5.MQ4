//< afd.AdjustLotSize >                                                                                // 01
                                                                                                       // 02
//< head>                                                                                              // 03
                                                                                                       // 04
double afd.AdjustLotSize             ( // input 2 / code  7 / output 1                                 // 05
                                       //                                                              // 06
       double aad.InputLotSize       , // input 1 / 2                                                  // 07
       int    aai.Fractions            // input 2 / 2                                                  // 08
                                       //                                                              // 09
                                     ) //                                                              // 10
                                                                                                       // 11
//</head>                                                                                              // 12
                                                                                                       // 13
{//<body>                                                                                              // 14
                                                                                                       // 15
double   ald.OrderLotStep    = MarketInfo       ( Symbol ()        , MODE_LOTSTEP  )                 ; // 16
double   ald.StepsInput      = aad.InputLotSize / ald.OrderLotStep                                   ; // 17
int      ali.StepsRounded    = MathRound        ( ald.StepsInput                   )                 ; // 18
int      ali.StepsOutput     = MathFloor        ( ali.StepsRounded / aai.Fractions ) * aai.Fractions ; // 19
double   ald.AdjustedLotSize = ali.StepsOutput  * ald.OrderLotStep                                   ; // 20
                                                                                                       // 21
Alert  ( "                      "                       )                                            ; // 22
Alert  ( "ald.AdjustedLotSize = " , ald.AdjustedLotSize )                                            ; // 23
Alert  ( "Output:               "                       )                                            ; // 24
Alert  ( "                      "                       )                                            ; // 25
Alert  ( "ali.StepsOutput     = " , ali.StepsOutput     )                                            ; // 26
Alert  ( "ali.StepsRounded    = " , ali.StepsRounded    )                                            ; // 27
Alert  ( "ald.StepsInput      = " , ald.StepsInput      )                                            ; // 28
Alert  ( "Processing:           "                       )                                            ; // 29
Alert  ( "                      "                       )                                            ; // 30
Alert  ( "ald.OrderLotStep    = " , ald.OrderLotStep    )                                            ; // 31
Alert  ( "aai.Fractions       = " , aai.Fractions       )                                            ; // 32
Alert  ( "aad.InputLotSize    = " , aad.InputLotSize    )                                            ; // 33
Alert  ( "Input:                "                       )                                            ; // 34
                                                                                                       // 35
return ( ald.AdjustedLotSize )                                                                       ; // 36
                                                                                                       // 37
}//</body>                                                                                             // 38
                                                                                                       // 39
//</afd.AdjustLotSize >                                                                                // 40
                                                                                                       // 41
                                                                                                       // 42
//< start >                                                                                            // 43
int    start ()                                                                                        // 44
{                                                                                                      // 45
double avd.InputLotSize    = 0.58                                                                    ; // 46
int    avi.Fractions       = 2                                                                       ; // 47
double avd.AdjustedLotSize = afd.AdjustLotSize ( avd.InputLotSize , avi.Fractions )                  ; // 48
}                                                                                                      // 49
//</start >                                                                                            // 50