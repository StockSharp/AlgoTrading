int start ()                                                                          // 01
{                                                                                     // 02
static int    ali.Digits                                                            ; // 03
static double ald.QuotePoint                                                        ; // 04
                                                                                      // 05
static int    ali.TotalBars                                                         ; // 06
                                                                                      // 07
static int    ali.Resolution.H                                                      ; // 08
static int    ali.LastVisibleBar                                                    ; // 09
static int    ali.FirstVisibleBar                                                   ; // 10
static int    ali.TotalVisibleBars                                                  ; // 11
static int    ali.ShiftBars                                                         ; // 12
                                                                                      // 13
static double ald.PriceMax                                                          ; // 14
static double ald.PriceMin                                                          ; // 15
static double ald.PriceRange                                                        ; // 16
static int    ali.Resolution.V                                                      ; // 17
                                                                                      // 18
ali.Digits               = MarketInfo ( Symbol () , MODE_DIGITS )                   ; // 19
ald.QuotePoint           = MarketInfo ( Symbol () , MODE_POINT  )                   ; // 20
                                                                                      // 21
ali.TotalBars            = iBars      ( Symbol () , Period ()   )                   ; // 22
                                                                                      // 23
ali.Resolution.H         = WindowBarsPerChart    ()                                 ; // 24
ali.LastVisibleBar       = WindowFirstVisibleBar ()                                 ; // 25
if ( ali.LastVisibleBar  < ali.Resolution.H )                                         // 26
     ali.FirstVisibleBar = 0                                                        ; // 27
else ali.FirstVisibleBar = ali.LastVisibleBar     - ali.Resolution.H    + 1         ; // 28
ali.TotalVisibleBars     = ali.LastVisibleBar     - ali.FirstVisibleBar + 1         ; // 29
ali.ShiftBars            = ali.Resolution.H       - ali.TotalVisibleBars            ; // 30
                                                                                      // 31
ald.PriceMax             = WindowPriceMax ()                                        ; // 32
ald.PriceMin             = WindowPriceMin ()                                        ; // 33
ald.PriceRange           = ald.PriceMax           - ald.PriceMin                    ; // 34
ali.Resolution.V         = ald.PriceRange         / ald.QuotePoint                  ; // 35
                                                                                      // 36
Alert   ( ""                                                                      ) ; // 37
Alert   ( "ali.Resolution.H     = " , ali.Resolution.H                            ) ; // 38
Alert   ( "ali.Resolution.V     = " , ali.Resolution.V                            ) ; // 39
Alert   ( "Summary:"                                                              ) ; // 40
Alert   ( ""                                                                      ) ; // 41
Alert   ( "ali.TotalBars        = " , ali.TotalBars                               ) ; // 42
Alert   ( "ali.TotalVisibleBars = " , ali.TotalVisibleBars                        ) ; // 43
Alert   ( "ali.LastVisibleBar   = " , ali.LastVisibleBar                          ) ; // 44
Alert   ( "ali.FirstVisibleBar  = " , ali.FirstVisibleBar                         ) ; // 45
Alert   ( "ali.ShiftBars        = " , ali.ShiftBars                               ) ; // 46
Alert   ( "Horizontal:"                                                           ) ; // 47
Alert   ( ""                                                                      ) ; // 48
Alert   ( "ald.PriceRange       = " , DoubleToStr ( ald.PriceRange , ali.Digits ) ) ; // 49
Alert   ( "ald.PriceMin         = " , DoubleToStr ( ald.PriceMin   , ali.Digits ) ) ; // 50
Alert   ( "ald.PriceMax         = " , DoubleToStr ( ald.PriceMax   , ali.Digits ) ) ; // 51
Alert   ( "Vertical:"                                                             ) ; // 51
Alert   ( ""                                                                      ) ; // 52
Alert   ( "Chart Parameters:"                                                     ) ; // 53
}                                                                                     // 54