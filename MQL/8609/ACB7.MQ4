//< Input Data >==============================================================================================// < 01>
                                                                                                              // < 02>
#define aci.Matrixes          4                                                                               // < 03>
                                                                                                              // < 04>
int     avi.Dimension         [ aci.Matrixes ,  3 ] = { 0 , 0 , 0 ,                                           // < 05>
                                                        0 , 2 , 3 ,                                           // < 06>
                                                        0 , 3 , 2 ,                                           // < 07>
                                                        0 , 0 , 0                                         } ; // < 08>
#define aci.Matrix.1.ID       1                                                                               // < 09>
double  avd.Matrix.1          [] = { aci.Matrix.1.ID ,                                                        // < 10>
                                      1 ,  2 ,  3 ,                                                           // < 11>
                                      4 ,  5 ,  6                                                         } ; // < 12>
#define aci.Matrix.2.ID       2                                                                               // < 13>
double  avd.Matrix.2          [] = { aci.Matrix.2.ID ,                                                        // < 14>
                                      7 ,  8 ,                                                                // < 15>
                                      9 , 10 ,                                                                // < 16>
                                     11 , 12                                                              } ; // < 17>
                                                                                                              // < 18>
//</Input Data >==============================================================================================// < 19>
                                                                                                              // < 20>
//< Output Data >=============================================================================================// < 21>
                                                                                                              // < 22>
#define aci.Matrix.3.ID       3                                                                               // < 23>
double  avd.Matrix.3          [] = { aci.Matrix.3.ID                                                      } ; // < 24>
                                                                                                              // < 25>
//</Output Data >=============================================================================================// < 26>
                                                                                                              // < 27>
//< Program >=================================================================================================// < 28>
                                                                                                              // < 29>
int start ()                                                                                                  // < 30>
                                                                                                              // < 31>
{//< body >                                                                                                   // < 32>
                                                                                                              // < 33>
static int ali.Runs      ; ali.Runs             = 1000                                                      ; // < 34>
static int ali.StartTime ; ali.StartTime        = GetTickCount ()                                           ; // < 35>
                                                                                                              // < 36>
for  ( int i = 1 ; i    <= ali.Runs ; i ++ )                                                                  // < 37>
       int ali.Result    = afi.MatrixProcessing ( avd.Matrix.1 , avd.Matrix.2 , avd.Matrix.3  )             ; // < 38>
                                                                                                              // < 39>
static int ali.EndTime   ; ali.EndTime          = GetTickCount ()                                           ; // < 40>
                                                                                                              // < 41>
if ( ali.Result == True  )                                                                                    // < 42>
   { afr.MatrixPrint     ( avd.Matrix.3 )                                                                   ; // < 43>
     afr.MatrixPrint     ( avd.Matrix.2 )                                                                   ; // < 44>
     afr.MatrixPrint     ( avd.Matrix.1 )                                                                   ; // < 45>
     Alert ( "Success:"  , ali.Runs           , " runs  in "   , ali.EndTime  - ali.StartTime , " ms" )   ; } // < 46>
else Alert ( "Failure"   )                                                                                  ; // < 47>
     Alert ( ""          )                                                                                  ; // < 48>
                                                                                                              // < 49>
}//</body >                                                                                                   // < 50>
                                                                                                              // < 51>
//</Program >=================================================================================================// < 52>
                                                                                                              // < 53>
//< Function 1 >==============================================================================================// < 54>
                                                                                                              // < 55>
int afi.MatrixProcessing    (                                                                                 // < 56>
                                                                                                              // < 57>
///< head >                                                                                                   // < 58>
    double& aad.Matrix.1 [] ,                                                                                 // < 59>
    double& aad.Matrix.2 [] ,                                                                                 // < 60>
    double& aad.Matrix.3 [] )                                                                                 // < 61>
///</head >                                                                                                   // < 62>
                                                                                                              // < 63>
{//< body >                                                                                                   // < 64>
                                                                                                              // < 65>
static int  ali.Result                                                                                      ; // < 66>
                                                                                                              // < 67>
static int  ali.Matrix.1.ID       ; ali.Matrix.1.ID      = aad.Matrix.1  [ 0                   ]            ; // < 68>
static int  ali.Matrix.1.Rows     ; ali.Matrix.1.Rows    = avi.Dimension [ ali.Matrix.1.ID , 1 ]            ; // < 69>
static int  ali.Matrix.1.Columns  ; ali.Matrix.1.Columns = avi.Dimension [ ali.Matrix.1.ID , 2 ]            ; // < 70>
                                                                                                              // < 71>
static int  ali.Matrix.2.ID       ; ali.Matrix.2.ID      = aad.Matrix.2  [ 0                   ]            ; // < 72>
static int  ali.Matrix.2.Rows     ; ali.Matrix.2.Rows    = avi.Dimension [ ali.Matrix.2.ID , 1 ]            ; // < 73>
static int  ali.Matrix.2.Columns  ; ali.Matrix.2.Columns = avi.Dimension [ ali.Matrix.2.ID , 2 ]            ; // < 74>
                                                                                                              // < 75>
static int  ali.Matrix.3.ID                                                                                 ; // < 76>
static int  ali.Matrix.3.Rows                                                                               ; // < 77>
static int  ali.Matrix.3.Columns                                                                            ; // < 78>
                                                                                                              // < 79>
if        ( ali.Matrix.1.Columns == ali.Matrix.2.Rows )                                                       // < 80>
          { ali.Matrix.3.Rows     = ali.Matrix.1.Rows                                                       ; // < 81>
            ali.Matrix.3.Columns  = ali.Matrix.2.Columns                                                    ; // < 82>
            ali.Matrix.3.ID       = aad.Matrix.3  [ 0 ]                                                     ; // < 83>
                                                                                                              // < 84>
            afr.ResizeMatrix      ( ali.Matrix.3.Rows    , ali.Matrix.3.Columns , aad.Matrix.3 )            ; // < 85>
                                                                                                              // < 86>
            ali.Result            = 1                                                                     ; } // < 87>
else        ali.Result            = 0                                                                       ; // < 88>
                                                                                                              // < 89>
if        ( ali.Result           == 1 )                                                                       // < 90>
          { for ( int i = 1 ; i  <= ali.Matrix.3.Rows    ; i ++ )                                             // < 91>
            for ( int j = 1 ; j  <= ali.Matrix.3.Columns ; j ++ )                                             // < 92>
                { double ald.Cell ; ald.Cell = 0                                                            ; // < 93>
                  for ( int k = 1 ; k <= ali.Matrix.1.Columns  ; k ++ )                                       // < 94>
                        ald.Cell  = ald.Cell + afd.GetCell ( i , k , aad.Matrix.1 )                           // < 95>
                                             * afd.GetCell ( k , j , aad.Matrix.2 )                         ; // < 96>
                  afr.SetCell     ( i , j    , aad.Matrix.3        , ald.Cell     )                     ; } } // < 97>
                                                                                                              // < 98>
return    ( ali.Result )                                                                                    ; // < 99>
                                                                                                              // <100>
}//</body >                                                                                                   // <101>
                                                                                                              // <102>
//</Function 1 >==============================================================================================// <103>
                                                                                                              // <104>
//< Function 2 >==============================================================================================// <105>
                                                                                                              // <106>
int afr.MatrixPrint       (                                                                                   // <107>
                                                                                                              // <108>
///< head >                                                                                                   // <109>
    double& aad.Matrix [] )                                                                                   // <110>
///</head >                                                                                                   // <111>
                                                                                                              // <112>
{//< body >                                                                                                   // <113>
                                                                                                              // <114>
static int    ali.MatrixID     ; ali.MatrixID     = aad.Matrix    [ 0                ]                      ; // <115>
static int    ali.Rows         ; ali.Rows         = avi.Dimension [ ali.MatrixID , 1 ]                      ; // <116>
static int    ali.Columns      ; ali.Columns      = avi.Dimension [ ali.MatrixID , 2 ]                      ; // <117>
                                                                                                              // <118>
static string als.Row                                                                                       ; // <119>
static int    i , j                                                                                         ; // <120>
                                                                                                              // <121>
for   (           i = ali.Rows ; i >= 1           ; i -- )                                                    // <122>
      { for     ( j = 1        ; j <= ali.Columns ; j ++ )                                                    // <123>
                  als.Row   = als.Row + " (" + i + "," + j+ ")= " + afd.GetCell ( i , j , aad.Matrix )+ ";" ; // <124>
        Alert   ( als.Row ) ; als.Row = ""                                                                ; } // <125>
                                                                                                              // <126>
Alert ( "Matrix ID: " , ali.MatrixID , " Rows=" , ali.Rows  , " Columns=" , ali.Columns )                   ; // <127>
                                                                                                              // <128>
}//</body >                                                                                                   // <129>
                                                                                                              // <130>
//</Function 2 >==============================================================================================// <131>
                                                                                                              // <132>
//< Function 3 >==============================================================================================// <133>
                                                                                                              // <134>
int afd.GetCell            (                                                                                  // <135>
                                                                                                              // <136>
///< head >                                                                                                   // <137>
    int     aai.Row        ,                                                                                  // <138>
    int     aai.Column     ,                                                                                  // <139>
    double& aad.Matrix []  )                                                                                  // <140>
///</head >                                                                                                   // <141>
                                                                                                              // <142>
{//< body >                                                                                                   // <143>
                                                                                                              // <144>
static double ald.Result                                                                                    ; // <145>
                                                                                                              // <146>
static int    ali.MatrixID ; ali.MatrixID  = aad.Matrix    [                0 ]                             ; // <147>
static int    ali.Columns  ; ali.Columns   = avi.Dimension [ ali.MatrixID , 2 ]                             ; // <148>
                                                                                                              // <149>
ald.Result  = aad.Matrix   [ ali.Columns * ( aai.Row - 1 ) + aai.Column       ]                             ; // <150>
                                                                                                              // <151>
return      ( ald.Result   )                                                                                ; // <152>
                                                                                                              // <153>
}//</body >                                                                                                   // <154>
                                                                                                              // <155>
//</Function 3 >==============================================================================================// <156>
                                                                                                              // <157>
//< Function 4 >==============================================================================================// <158>
                                                                                                              // <159>
int afr.SetCell            (                                                                                  // <160>
                                                                                                              // <161>
///< head >                                                                                                   // <162>
    int     aai.Row        ,                                                                                  // <163>
    int     aai.Column     ,                                                                                  // <164>
    double& aad.Matrix []  ,                                                                                  // <165>
    double  aad.Value      )                                                                                  // <166>
///</head >                                                                                                   // <167>
                                                                                                              // <168>
{//< body >                                                                                                   // <169>
                                                                                                              // <170>
static int   ali.MatrixID  ; ali.MatrixID  = aad.Matrix    [                0 ]                             ; // <171>
static int   ali.Columns   ; ali.Columns   = avi.Dimension [ ali.MatrixID , 2 ]                             ; // <172>
                                                                                                              // <173>
aad.Matrix [ ali.Columns * ( aai.Row - 1 ) + aai.Column ]  = aad.Value                                      ; // <174>
                                                                                                              // <175>
}//</body >                                                                                                   // <176>
                                                                                                              // <177>
//</Function 4 >==============================================================================================// <178>
                                                                                                              // <179>
//< Function 5 >==============================================================================================// <180>
                                                                                                              // <181>
int afr.ResizeMatrix       (                                                                                  // <182>
                                                                                                              // <183>
///< head >                                                                                                   // <184>
    int     aai.Rows       ,                                                                                  // <185>
    int     aai.Columns    ,                                                                                  // <186>
    double& aad.Matrix []  )                                                                                  // <187>
///</head >                                                                                                   // <188>
                                                                                                              // <189>
{//< body >                                                                                                   // <190>
                                                                                                              // <191>
static int      ali.MatrixID       ; ali.MatrixID = aad.Matrix  [ 0 ]                                       ; // <192>
                                                                                                              // <193>
avi.Dimension [ ali.MatrixID , 1 ] = aai.Rows                                                               ; // <194>
avi.Dimension [ ali.MatrixID , 2 ] = aai.Columns                                                            ; // <195>
ArrayResize   ( aad.Matrix         , aai.Rows     * aai.Columns + 1 )                                       ; // <196>
                                                                                                              // <197>
}//</body >                                                                                                   // <198>
                                                                                                              // <199>
//</Function 5 >==============================================================================================// <200>
