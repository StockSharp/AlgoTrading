int start ()                                                                                        // 01
{                                                                                                   // 02
static int ali.TicksReported.0 ; ali.TicksReported.0 = iVolume ( Symbol () , PERIOD_M1 , 0 )      ; // 03
static int ali.TicksReported.1 ; ali.TicksReported.1 = iVolume ( Symbol () , PERIOD_M1 , 1 )      ; // 04
                                                                                                    // 05
static int ali.OpenTime.0      ; ali.OpenTime.0      = iTime   ( Symbol () , PERIOD_M1 , 0 )      ; // 06
static int ali.OpenTime.1      ; ali.OpenTime.1      = iTime   ( Symbol () , PERIOD_M1 , 1 )      ; // 07
                                                                                                    // 08
static int ali.FrameLength     ; ali.FrameLength     = TimeCurrent ()      - ali.OpenTime.0       ; // 09
                                                                                                    // 10
static int ali.TicksCounted.0                                                                     ; // 11
static int ali.TicksCounted.1                                                                     ; // 12
                                                                                                    // 13
static int ali.LastOpenTime                                                                       ; // 14
                                                                                                    // 15
static int ali.Flag ; if       ( ali.Flag != True ) { ali.Flag = True                             ; // 16
  ali.TicksCounted.0           = ali.TicksReported.0                                              ; // 17
  ali.LastOpenTime             = ali.OpenTime.0                                                   ; // 18
  Alert ( "Start" )            ; return                                                         ; } // 19
                                                                                                    // 20
if   ( ali.LastOpenTime       == ali.OpenTime.0 )                                                   // 21
       ali.TicksCounted.0     ++                                                                  ; // 22
else { ali.TicksCounted.1      = ali.TicksCounted.0                                               ; // 23
       ali.TicksCounted.0      = 1                                                                ; // 24
       ali.LastOpenTime        = ali.OpenTime.0                                                   ; // 25
       Alert ( "Frame 0 "      ,                                                                    // 26
               "is closed: "   , TimeToStr ( ali.OpenTime.0 , TIME_MINUTES | TIME_SECONDS ) , " " , // 27
               "Count: "       , ali.TicksCounted.1                                         , " " , // 28
               "Report: "      , ali.TicksReported.1                                          ) ; } // 29
                                                                                                    // 30
Alert        ( Symbol ()       , " M1: "   , "Frame 0: "                                          , // 31
               "Open: "        , TimeToStr ( ali.OpenTime.0 , TIME_MINUTES | TIME_SECONDS ) , " " , // 32
               "Seconds: "     , ali.FrameLength                                            , " " , // 33
               "Count: "       , ali.TicksCounted.0                                         , " " , // 34
               "Report: "      , ali.TicksReported.0                                        , " " , // 35
                                 " //  "   , "Frame 1: "                                          , // 36
               "Open: "        , TimeToStr ( ali.OpenTime.1 , TIME_MINUTES | TIME_SECONDS ) , " " , // 37
               "Count: "       , ali.TicksCounted.1                                         , " " , // 38
               "Report: "      , ali.TicksReported.1                                            ) ; // 39
}                                                                                                   // 40