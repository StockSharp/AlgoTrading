//< 1. Property 7 >===========================================================================================//<   1>
                                                                                                              //<   2>
#property     copyright                 "Copyright (C) 2009, MetaQuotes Software Corp."                       //<   3>
#property     link                      "http://www.metaquotes.net"                                           //<   4>
                                                                                                              //<   5>
#define       A.System.Series           "AIS"                                                                 //<   6>
#define       A.System.Modification     "70001"                                                               //<   7>
#define       A.System.ReleaseDate      "2009.06.03"                                                          //<   8>
#define       A.System.Program          "Peak Volume Counter"                                                 //<   9>
#define       A.System.Programmer       "Airat Safin                           http://www.mql4.com/users/AIS" //<  10>
                                                                                                              //<  11>
//</1. Property 7 >===========================================================================================//<  12>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  13>
//< 2. Constants 3 >==========================================================================================//<  14>
                                                                                                              //<  15>
#define       aci.Threshold                       7                                                           //<  16>
#define       aci.FontSize                        7                                                           //<  17>
#define       acs.FontName                        "Courier New"                                               //<  18>
                                                                                                              //<  19>
//</2. Constants 3 >==========================================================================================//<  20>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  21>
//< 3. Presets 0 >============================================================================================//<  22>
//</3. Presets 0 >============================================================================================//<  23>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  24>
//< 4. Global Variables 12 >==================================================================================//<  25>
                                                                                                              //<  26>
int           avi.Volume.0                                                                                  ; //<  27>
int           avi.Volume.1                                                                                  ; //<  28>
int           avi.OpenTime.0                                                                                ; //<  29>
int           avi.OpenTime.1                                                                                ; //<  30>
                                                                                                              //<  31>
int           avi.FrameLength                                                                               ; //<  32>
int           avi.OpenTimeLast                                                                              ; //<  33>
                                                                                                              //<  34>
int           avi.VolumeLast                                                                                ; //<  35>
int           avi.VolumePack                                                                                ; //<  36>
                                                                                                              //<  37>
int           avi.FontColor                                                                                 ; //<  38>
int           avi.LabelIndex                                                                                ; //<  39>
string        avs.LabelValue                                                                                ; //<  40>
string        avs.LabelName                                                                                 ; //<  41>
                                                                                                              //<  42>
//</4. Global Variables 12 >==================================================================================//<  43>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  44>
//< 5. Program Initialization 3 >=============================================================================//<  45>
                                                                                                              //<  46>
int    init                          ()                                                                       //<  47>
{                                                                                                             //<  48>
Alert                    ( A.System.Series + A.System.Modification + " "   + A.System.Program + ": Start" ) ; //<  49>
avi.VolumeLast           = iVolume         ( Symbol ()      , PERIOD_M1                               , 0 ) ; //<  50>
avi.OpenTimeLast         = iTime           ( Symbol ()      , PERIOD_M1                               , 0 ) ; //<  51>
}                                                                                                             //<  52>
//</5. Program Initialization 3 >=============================================================================//<  53>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  54>
//< 6. Program Deinitialization 1 >===========================================================================//<  55>
                                                                                                              //<  56>
int    deinit                        ()                                                                       //<  57>
{                                                                                                             //<  58>
Alert                    ( A.System.Series + A.System.Modification + " "   + A.System.Program + ": Stop"  ) ; //<  59>
}                                                                                                             //<  60>
//</6. Program Deinitialization 1 >===========================================================================//<  61>
////////////////////////////////////////////////////////////////////////////////////////////////////////////////<  62>
//< 7. Main Program 28 >======================================================================================//<  63>
                                                                                                              //<  64>
int    start                         ()                                                                       //<  65>
{                                                                                                             //<  66>
avi.Volume.0             = iVolume         ( Symbol ()      , PERIOD_M1                               , 0 ) ; //<  67>
avi.Volume.1             = iVolume         ( Symbol ()      , PERIOD_M1                               , 1 ) ; //<  68>
avi.OpenTime.0           = iTime           ( Symbol ()      , PERIOD_M1                               , 0 ) ; //<  69>
avi.OpenTime.1           = iTime           ( Symbol ()      , PERIOD_M1                               , 1 ) ; //<  70>
                                                                                                              //<  71>
avi.FrameLength          = TimeCurrent     ()               - avi.OpenTime.0                                ; //<  72>
                                                                                                              //<  73>
if   ( avi.OpenTimeLast == avi.OpenTime.0  )                                                                  //<  74>
       avi.VolumePack    = avi.Volume.0    - avi.VolumeLast                                                 ; //<  75>
else { avi.VolumePack    = avi.Volume.0    - avi.VolumeLast + avi.Volume.1                                  ; //<  76>
       avi.OpenTimeLast  = avi.OpenTime.0                                                                   ; //<  77>
     } // else                                                                                                //<  78>
                                                                                                              //<  79>
if   ( avi.VolumePack   >= aci.Threshold   )                                                                  //<  80>
     { avi.LabelIndex   ++                                                                                  ; //<  81>
       avs.LabelValue    = avi.VolumePack                                                                   ; //<  82>
       avs.LabelName     = avi.LabelIndex                                                             + "_" + //<  83>
                           TimeToStr       ( TimeLocal   () , TIME_SECONDS )                          + "_" + //<  84>
                           TimeToStr       ( TimeCurrent () , TIME_SECONDS )                          + "_" + //<  85>
                           DoubleToStr     ( Ask            , 4            )                                ; //<  86>
                                                                                                              //<  87>
       if      ( avi.VolumePack >=  7     && avi.VolumePack <=  8          ) avi.FontColor = Red            ; //<  88>
       else if ( avi.VolumePack >=  9     && avi.VolumePack <= 10          ) avi.FontColor = Orange         ; //<  89>
       else if ( avi.VolumePack >= 11     && avi.VolumePack <= 12          ) avi.FontColor = Yellow         ; //<  90>
       else if ( avi.VolumePack >= 13     && avi.VolumePack <= 14          ) avi.FontColor = LawnGreen      ; //<  91>
       else if ( avi.VolumePack >= 15     && avi.VolumePack <= 16          ) avi.FontColor = Aqua           ; //<  92>
       else if ( avi.VolumePack >= 17     && avi.VolumePack <= 18          ) avi.FontColor = Blue           ; //<  93>
       else if ( avi.VolumePack >= 19                                      ) avi.FontColor = Violet         ; //<  94>
                                                                                                              //<  95>
       ObjectCreate      ( avs.LabelName   , OBJ_TEXT       , 0            , TimeCurrent ()         , Ask ) ; //<  96>
       ObjectSetText     ( avs.LabelName   , avs.LabelValue , aci.FontSize , acs.FontName , avi.FontColor ) ; //<  97>
     } // if                                                                                                  //<  98>
                                                                                                              //<  99>
avi.VolumeLast           = avi.Volume.0                                                                     ; //< 100>
}                                                                                                             //< 101>
//</7. Main Program 28 >======================================================================================//< 102>