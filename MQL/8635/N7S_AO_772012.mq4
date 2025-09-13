//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//и                                                                                                             и//
//и                         N7S_AO_194694_0.mq4       v.77.2012                                                 и//
//и                         Hunter by profit                                                                    и//  
//и                         Balashiha         S&N@yandex.ru                                                     и//  
//и                         Version  Desember 20, 2007                                                          и// 
//и                                                                                                             и//  
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
#property copyright "Copyright Љ 2007, Shooter777"
#property link      "S7N@mail.ru"

#include <WinUser32.mqh>
#include <stderror.mqh>
#include <stdlib.mqh>
//------------------------------------------------------------------//
int    HM_ALL     = 2;          //          
int    Trade      = 1;          //
//------------------------------------------------------------------//
extern bool   Trd_Up_X = true;  //
int           HM_Up_X    = 1;   //   
//------------------------------------------------------------------//
extern double       tpx = 50;
extern double       slx = 50;
extern int          px  = 10;
extern int          x1  = 0;
extern int          x2  = 0;
extern int          x3  = 0;
extern int          x4  = 0;
int                 tx  = 0;
//------------------------------------------------------------------//
extern bool   Trd_Dn_Y = true;  //
int           HM_Dn_Y    = 1;   //        
//------------------------------------------------------------------//
extern double       tpy = 50;
extern double       sly = 50;
extern int          py  = 10;
extern int          y1  = 0;
extern int          y2  = 0;
extern int          y3  = 0;
extern int          y4  = 0;
int                 ty  = 0;
//------------------------------------------------------------------//
extern string       Text0="BTS  F=1";
extern int          F   = 1;     //
extern int          pz  = 10;
extern int          z1  = 0;
extern int          z2  = 0;
extern int          z3  = 0;
extern int          z4  = 0;
int                 tz  = 0;
//------------------------------------------------------------------//
//------------------------------------------------------------------//
extern string       Text1="Neyro  G=4";
extern int          G   = 4;     //
extern string       Text2="XXXXXXXXXXXXX";
extern double       tpX = 50;
extern double       slX = 50;
extern int          pX  = 10;
extern int          X1  = 0;
extern int          X2  = 0;
extern int          X3  = 0;
extern int          X4  = 0;
int                 tX  = 0;
//------------------------------------------------------------------//
extern string       Text3="YYYYYYYYYYYYY";
extern double       tpY = 50;
extern double       slY = 50;
extern int          pY  = 10;
extern int          Y1  = 0;
extern int          Y2  = 0;
extern int          Y3  = 0;
extern int          Y4  = 0;
int                 tY  = 0;
//------------------------------------------------------------------//
extern string       Text4="ZZZZZZZZZZZZ";
extern int          pZ  = 10;
extern int          Z1  = 0;
extern int          Z2  = 0;
extern int          Z3  = 0;
extern int          Z4  = 0;
int                 tZ  = 0;
//------------------------------------------------------------------//
double       lots = 0.02;
       int          mn;
       int          mnx1 = 772012055;
       int          mny1 = 772012155;
       int          mnX1 = 772012011;
       int          mnY1 = 772012111;
static int          prvtm = 0;
static double       sl = 10;
static double       tp = 10;
/*
//------------------------------------------------------------------//
//    тр№шрэђ ъюфр я№ш яюфъыўїхэшш ртђююяђшьшчрђю№р  X_1008_auto.mqh
//------------------------------------------------------------------//
#import "TrlngFnc.ex4"
   void Stairs(int ticket,int trldistance,int trlstep);   
   void Udavka(int ticket,int trl_dist_1,int level_1,int trl_dist_2,int level_2,int trl_dist_3);
   void ByTime(int ticket,int interval,int trlstep,bool trlinloss);   
   void ByATR(int ticket,int atr_timeframe,int atr1_period,int atr1_shift,int atr2_period,int atr2_shift,double coeff,bool trlinloss);
   void ByMA(int iTicket,int iTmFrme,int iMAPeriod,int iMAShift,int MAMethod,int iApplPrice,int iShift,int iIndent);
#import
//------------------------------------------------------------------//
//------------------------------------------------------------------//

extern int    Set1         = 14341 ;          // 
extern int    Set2         = 111111;          //
 
extern int    Set3         = 14341;           // 
extern int    Set4         = 659999997;       //
 
extern int    Set5         = 14341 ;          // 
extern int    Set6         = 339999997;       //
 
extern int    Set7         = 14341;           // 
extern int    Set8         = 659999997;       // 

extern int    Set9         = 14341;           // 
extern int    Set10        = 659999997;       // 


//extern 
int    PdtB         = 40;
//extern 
int    StpB         = 25;
//extern 
int    PdtS         = 40;
//extern 
int    StpS         = 25;

//extern 
int l1=30 ;
//extern 
int l2=20 ;
//extern 
int l3=10 ;
//extern 
int L1=50 ;
//extern 
int L2=70 ;
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//extern 
int    LibSL        = 0;     // сшсышюђхър ђ№рыр
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//extern       
int SetHour   = 00;             //зрё ёђр№ђр юяђшьшчрішш 
//extern       
int SetMinute = 05;             //Ьшэѓђр ёђр№ђр юяђшьшчрішш 
int    TestDay     = 3;                      //Ъюышїхёђтю фэхщ фыџ юяђшьшчрішш 
int    TimeOut     = 10;                     //Т№хьџ юцшфрэшџ юъюэїрэшџ юяђшьшчрішш т ьшэѓђрѕ
string NameMTS     = "X_1008_Exp_RT.mq4";    //Шьџ ёютхђэшър
string NFS      = "X_1008_Exp_RT.set";     //Шьџ Set єрщыр ё ѓёђрэютърьш
string NFSOne   = "X_1008_Exp_RT1.set";    //Шьџ Set єрщыр ё ѓёђрэютърьш 1 §ђря
string NFSTwo   = "X_1008_Exp_RT2.set";    //Шьџ Set єрщыр ё ѓёђрэютърьш 2 §ђря
string NFSThree = "X_1008_Exp_RT3.set";    //Шьџ Set єрщыр ё ѓёђрэютърьш 3 §ђря
string NFSFour  = "X_1008_Exp_RT4.set";    //Шьџ Set єрщыр ё ѓёђрэютърьш 4 §ђря
string PuthTester  = "C:\Forex\Tester";//Яѓђќ ъ ђхёђх№ѓ
//--- Яюёыхфютрђхыќэюёђќ єшыќђ№рішш
int    Gr_Pr = 1;                   //бю№ђш№ютър яю Ьръёшьрыќэющ я№шсћыш
int    Pr_F  = 2;                   //бю№ђш№ютър яю Ьръёшьрыќэющ я№шсћыќэюёђш
int    Ex_P  = 3;                   //бю№ђш№ютър яю Ьръёшьрыќэюьѓ ьрђюцшфрэшў
//--шьхэр ях№хьхээћѕ фыџ юяђшьшчрішш
string Per1 = "Set1";
string Per2 = "Set2";
string Per3 = "";
string Per4 = "";
bool StartTest=false;
datetime TimeStart;
//--- Яюфъыўїхэшх сшсышюђхъш ртђююяђшьшчрђю№р
#include <X_1008_auto.mqh>
double Lots = 0.01;
//------------------------------------------------------------------//
//---  BUY ---
extern int    P_TSM_L_B    = 33;
extern int    P_TSM_H_B    = 39;
extern int    P_TSM_G_B    = 21;
extern int    P_TSM_D_B    = 39;

//---  SELL ---
int    P_TSM_L_S    = 21;
int    P_TSM_H_S    = 12;
int    P_TSM_G_S    = 42;
int    P_TSM_D_S    = 42;

int X_Tfr_M    = 1;
int X_Tfr_L    = 15;
int X_Tfr_H    = 60;
int X_Tfr_G    = 240;
int X_Tfr_D    = 1440;
//------------------------------------------------------------------//
int Sl,Tp;
//------------------------------------------------------------------//

//+------------------------------------------------------------------+

string Value ;
//+------------------------------------------------------------------+
int bu, sll,ttt, OrdBuOK=0, OrdSllOK=0; int Pdt,Stp;int total; double SL,TP;
int SLS,SLB,VrB_SL,VrS_SL,SlB,SlS,VrB_TP,VrS_TP,kTPB,kTPS,TPB,TPS,VrB_TR,VrS_TR;
int SLIV,SLIII,VrIII_SL,VrIV_SL,SlIII,SlIV,VrIII_TP,VrIV_TP,kTPIII,kTPIV,TPIII,TPIV,VrIII_TR,VrIV_TR;
int SLVI,SLV,VrV_SL,VrVI_SL,SlV,SlVI,VrV_TP,VrVI_TP,kTPV,kTPVI,TPV,TPVI,VrV_TR,VrVI_TR;
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
int VrB_X1,VrS_X1,VrB_X2,VrS_X2,VrB_X3,VrS_X3,VrB_X4,VrS_X4,VrB_P,VrS_P,VrB_Tst,VrS_Tst,TstB,TstS;
int VrIII_X1,VrIV_X1,VrIII_X2,VrIV_X2,VrIII_X3,VrIV_X3,VrIII_X4,VrIV_X4,VrIII_P,VrIV_P,VrIII_Tst,VrIV_Tst,TstIII,TstIV;
int VrV_X1,VrVI_X1,VrV_X2,VrVI_X2,VrV_X3,VrVI_X3,VrV_X4,VrVI_X4,VrV_P,VrVI_P,VrV_Tst,VrVI_Tst,TstV,TstVI;
//+------------------------------------------------------------------+
//                      Array
//+------------------------------------------------------------------+
//double arrStLs  [12] = {25,37,49,62,75,87,99,125,149,174,199,249};
double arrStLs  [10]  = { 25 , 37 , 49 , 62 , 75 , 87 , 99 , 125 , 149 , 174};  
double arrTkPt  [10]  = { 1.5 , 2 , 2.5 , 3, 3.5 , 4 , 4.5 , 5 , 5.5 , 6};
int    arrTrlng [10]  = { 0 , 1 , 2 , 3 , 4 , 5 , 6 , 7 , 8 , 9};
int    arrTst   [10]  = { 7 , 9 , 11 , 13 , 15 , 17 , 19 , 21 , 23 , 25};

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
//Tester(TestDay,NameMTS,NameFileSet,PuthTester,TimeOut, Gross_Profit,Profit_Factor, 
//       Expected_Payoff, Per1,Per2,Per3,Per4); 
LastFinishedBar = iTime(Symbol(), 0, 0);  
//----
//Set1     =GlobalVariableGet(Per1);
//Set2     =GlobalVariableGet(Per2);
//Set3     =GlobalVariableGet(Per3);
//Set4     =GlobalVariableGet(Per4); 
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
//----
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
VrB_TR   = Strng( Set1 , 0 , 1 );           VrS_TR   = Strng( Set3 , 0 , 1 );

VrB_SL   = Strng( Set1 , 1 , 1 );           VrS_SL   = Strng( Set3 , 1 , 1 );
SlB      = arrStLs  [VrB_SL];               SlS      = arrStLs  [VrS_SL];
SLB      = SlB          ;                   SLS      = SlS          ;

VrB_TP   = Strng( Set1 , 2 , 1 );           VrS_TP   = Strng( Set3 , 2 , 1 );
kTPB     = arrTkPt  [VrB_TP];               kTPS     = arrTkPt  [VrS_TP];
TPB      = kTPB * SlB ;                     TPS      = kTPS * SlS ;

VrB_Tst  = Strng( Set1 , 3 , 1 );           VrS_Tst  = Strng( Set3 , 3 , 1 );
TstB     = arrTst[VrB_Tst]   ;              TstS      = arrTst[VrS_Tst]   ;
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
VrB_X1   = Strng( Set2 , 0 , 2 );           VrS_X1   = Strng( Set4 , 0 , 2 );
VrB_X2   = Strng( Set2 , 2 , 2 );           VrS_X2   = Strng( Set4 , 2 , 2 );
VrB_X3   = Strng( Set2 , 4 , 2 );           VrS_X3   = Strng( Set4 , 4 , 2 );
VrB_X4   = Strng( Set2 , 6 , 2 );           VrS_X4   = Strng( Set4 , 6 , 2 );
VrB_P    = Strng( Set2 , 8 , 1 );           VrS_P    = Strng( Set4 , 8 , 1 );
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
VrIII_TR   = Strng( Set5 , 0 , 1 );    VrIV_TR   = Strng( Set7 , 0 , 1 );    VrV_TR   = Strng( Set9 , 0 , 1 );

VrIII_SL   = Strng( Set5 , 1 , 1 );    VrIV_SL   = Strng( Set7 , 1 , 1 );    VrV_SL   = Strng( Set9 , 1 , 1 );
SlIII      = arrStLs  [VrIII_SL];      SlIV      = arrStLs  [VrIV_SL];       SlV      = arrStLs  [VrV_SL];
SLIII      = SlB          ;            SLIV      = SlS          ;            SLV      = SlS          ;

VrIII_TP   = Strng( Set5 , 2 , 1 );    VrIV_TP   = Strng( Set7 , 2 , 1 );    VrV_TP   = Strng( Set9 , 2 , 1 );
kTPIII     = arrTkPt  [VrIII_TP];      kTPIV     = arrTkPt  [VrIV_TP];       kTPV     = arrTkPt  [VrV_TP];
TPIII      = kTPB * SlB ;              TPIV      = kTPS * SlS ;              TPV      = kTPS * SlS ;

VrIII_Tst  = Strng( Set5 , 3 , 1 );    VrIV_Tst  = Strng( Set7 , 3 , 1 );    VrV_Tst  = Strng( Set9 , 3 , 1 );
TstIII     = arrTst[VrIII_Tst]   ;     TstIV     = arrTst[VrIV_Tst]   ;      TstV     = arrTst[VrV_Tst]   ;
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//
VrIII_X1   = Strng( Set6 , 0 , 2 );    VrIV_X1   = Strng( Set8 , 0 , 2 );     VrV_X1   = Strng( Set10 , 0 , 2 );
VrIII_X2   = Strng( Set6 , 2 , 2 );    VrIV_X2   = Strng( Set8 , 2 , 2 );     VrV_X2   = Strng( Set10 , 2 , 2 );
VrIII_X3   = Strng( Set6 , 4 , 2 );    VrIV_X3   = Strng( Set8 , 4 , 2 );     VrV_X3   = Strng( Set10 , 4 , 2 );
VrIII_X4   = Strng( Set6 , 6 , 2 );    VrIV_X4   = Strng( Set8 , 6 , 2 );     VrV_X4   = Strng( Set10 , 6 , 2 );
VrIII_P    = Strng( Set6 , 8 , 1 );    VrIV_P    = Strng( Set8 , 8 , 1 );     VrV_P    = Strng( Set10 , 8 , 1 );
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~//

//Print ("SLB = ",SlB," TPB = ",TPB," VrB_P = ",VrB_P);

//----
   return(0);
  }
  
//+ддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//+дд                        s m a l l       f u n c t i o n                                дд+
//+ддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//+-------------------------------------------------------------------------------------------+
int Strng ( int set , int m , int n ){ int Vr = StrToInteger(StringSubstr(DoubleToStr (set, 0), m, n));return(Vr);}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~+
//+-------------------------------------------------------------------------------------------+
*/  
//        №рёъюььхэђш№ютрђќ я№ш №рсюђх ё ртђююяђшьшчрђю№юь  X_1008_auto.mqh

bool cm15=false;
bool cmH1=false;
bool cmH4=false;
int MMH1 ;
int MN   ;
static int bu,sll;
int i;
int ticket = -1;
int total;int spread;
datetime LFB;
double prcptx1=0,prcpty1=0,prcptX1=0,prcptY1=0,prcptZ1=0;
static double       Delta_G12 ;
//QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ
//QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ
//+------------------------------------------------------------------+
//| expert init function                                             |
//+------------------------------------------------------------------+
int init(){MMH1 = Hour( );LFB  = iTime(Symbol(), 0, 0);}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
{ //---- 
   if (! IsTradeAllowed()) {again();return(0);}
  //----
   total = OrdersTotal();spread = MarketInfo(Symbol(), MODE_SPREAD);
  //---- 
   int iTm = iTime(Symbol(), 0, 0);if(LFB == iTm) return(0); LFB  = iTime(Symbol(), 0, 0); 
      // iTest ();    єѓэъішџ я№ш №рсюђх ё ртђююяђшьшчрђю№юь
      int MMM15 = MathMod(Minute( ) , 15);
      if(cm15) {if(MMM15 == 0 ) {cm15 =false; trl();}} if(MMM15 !=0){cm15=true;}
    
      if(Hour() != MMH1)  {MMH1 = Hour( ); //Print ("H1");
      prcptx1   = prcptrnAC(x1,x2,x3,x4,px,tx) ;
      prcpty1   = prcptrnAC(y1,y2,y3,y4,py,ty) ;
      prcptX1 = prcptrnAC(X1,X2,X3,X4,pX,tX) ;
      prcptY1 = prcptrnAC(Y1,Y2,Y3,Y4,pY,tY) ;
      prcptZ1 = prcptrnAC(Z1,Z2,Z3,Z4,pZ,tZ) ;
      }
   
      int MMH4 = MathMod(Hour( ) , 4);
      if(cmH4) {if(MMH4 == 0 ) {cmH4 =false; //Print ("H4");
        // double   iCustom_G01  =  iCus(240,24,0,1);
        // double   iCustom_G02  =  iCus(240,24,0,2);
        //          Delta_G12    =  iCustom_G01 - iCustom_G02 ;//Print (Delta_G12);
        //double   iStoch01  =  iStochastic(NULL,0,39,1,3,MODE_SMA,0,MODE_MAIN,1);
        //double   iStoch02  =  iStochastic(NULL,0,39,1,3,MODE_SMA,0,MODE_MAIN,2);  
        //        Delta_G12 =  iStoch02 - iStoch01 ;
        double       iCusAO_1     =  iAO(NULL, 240, 1);
        double       iCusAO_2     =  iAO(NULL, 240, 2);
                     Delta_G12    =  iCusAO_1 -iCusAO_2;
       }}
      if(MMH4 !=0){cmH4=true;}
startM1();
}
//QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ
//QQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQQ      
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//|                                                                  
//|                         N7S_AO_194694_0.mq4       v.77.2012    
//|                         Hunter by profit                         
//|                         Balashiha         S&N@yandex.ru          
//|                         Version  Desember 20, 2007              
//|                                                                  
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//      

//+--------------------------- IIIIIIIIIIIIIIIIIIIIIII ----------------------------------+
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
double BTS()    {
       if (prcptrnz1() > 0 || F==0) 
            {if (prcptx1 > 0 && Delta_G12>0) {sl = slx; tp = tpx*slx; mn= mnx1; return (1);}} 
       if (prcptrnz1() < 0 || F==0) 
            {if (prcpty1 > 0 && Delta_G12<0) {sl = sly; tp = tpy*sly; mn= mny1 ;return (-1);}}
      return (0);}
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//|    function  PERCEPTRON - AC                                                         |
//+--------------------------------------------------------------------------------------+
double prcptrnAC(int q1,int q2,int q3,int q4,int pr,int at) 
  {double qw = ((q1-50)+((q2-50)*iA_C(pr)+(q3-50)*iA_C(2*pr)+(q4-50)*iA_C(3*pr))/iA_C(0));
   if (MathAbs(qw)>at) return(qw);else return(0);}
//+--------------------------------------------------------------------------------------+
double iA_C (int pr)
   {int tmfr=60; return(iAO(Symbol(), tmfr, pr));}
//+--------------------------------------------------------------------------------------+
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
/*
int MOS(int Op_,double Vl,double StLs, double TkPt,string cmnt,int mgc)                           
  { bool OrSd = false;
     if(!(Op_==0 ||Op_==1)){Print ("Юјшсър!!!Ю№фх№ эх ѓёђрэютыхэ! Эхтх№эћщ ђшя!");return (-1);}
      else {OrSd = true;}
   datetime Begin=TimeCurrent();
   int err,rslt;  double prc; color clr;
        switch (Op_)
         {case 0: prc=Ask; clr=Green; break;
          case 1: prc=Bid; clr=Red;   break;}          
   if( OrSd )
    {rslt= OrderSend(Symbol(),Op_,Vl,prc,5,StLs,TkPt,cmnt,mgc,0,clr);
   if (rslt==-1)
    {err=GetLastError(); Print("Юјшсър я№ш ѓёђрэютъх ю№фх№р!!!(",err,") ");
       if(!IsTesting())
         {while(!( rslt>0 || TimeCurrent()-Begin>20))
          {Sleep(1000);   RefreshRates();
           rslt= OrderSend(Symbol(),Op_,Vl,prc,5,StLs,TkPt,cmnt,mgc,0,clr); }}}}//4// while | if | if | if | 
return(rslt);} */
//+*****************************************************************************************+
//+-----------------------------------------------------------------------------------------+
//| дѓэъішџ iCustom_XXX                                                                     |
//| "iCustom_N7S_TSM"                                                                       |
//+-----------------------------------------------------------------------------------------+
/*
double iCus (int XXX, int XX, int B , int C)
            { double   iCus_XXX_XX_B_C = iCustom(NULL,XXX,"iCustom_N7S_TSM_forExp",XX,B,C);
            return (iCus_XXX_XX_B_C); }
//+*****************************************************************************************+
*/
void again() {
   prvtm = Time[1];
   Sleep(30000);}
   
//+------------------------------------------------------------------+
//| бїшђрхь ю№фх№р                                                   |
//| Яр№рьхђ№ћ:                                                       |
//|                                                                  |
//+------------------------------------------------------------------+
 void BuSll ( int pl, int OrdTp, int mgnmb) // pl-шёђюїэшъ 0-фхщёђтѓўљшх 1- чръ№ћђћх  //  OrdTp=ђшя ю№фх№р 1-№ћэюїэћх(0ш1)  3-LIMIT(2ш3)  5-STOP(4ш5)
      { switch(pl)
            {case 0: bu=0; sll=0; int ChTpBu,ChTpSll; // Print (" OrdTp  ",OrdTp);            // тћс№рэ яюфёїхђ B&S  №ћэюїэћѕ ю№фх№ют 
               switch(OrdTp)
                  {case 1:  ChTpBu=OP_BUY;     ChTpSll =OP_SELL;      break;
                   case 3:  ChTpBu=OP_BUYSTOP; ChTpSll =OP_SELLSTOP;  break;
                   case 5:  ChTpBu=OP_BUYLIMIT;ChTpSll =OP_SELLLIMIT; break;}
               for(int  i=0;i<OrdersTotal();i++)
                  {OrderSelect(i, SELECT_BY_POS, MODE_TRADES); int OMN=OrderMagicNumber();
                   if(OrderType()==ChTpBu  &&(OMN>=mgnmb && OMN<=mgnmb+99)){ bu++;}
                   if(OrderType()==ChTpSll &&(OMN>=mgnmb+100 && OMN<=mgnmb+199)){sll++;}
                   }break; 
              case 1:  int pstv=0, ngtv=0;                              // тћс№рэ рэрышч шёђю№шш 
                  Print ("Яюфёїхђ ю№фх№ют т шёђю№шш"); break;}}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+

double prcptrnz1()   {double qz =(z1-50)*(Close[0]-Open[pz])+(z2-50)*(Open[pz]-Open[pz*2])
                     +(z3-50)*(Open[pz*2]-Open[pz*3])+(z4-50)*(Open[pz*3]-Open[pz*4]);return(qz);}
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+


//+ииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии+
//+ии                   ђшяр ђ№хщышэу                              ии+
//+ииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии+  
//----
void trl(){
      total= OrdersTotal(); spread = MarketInfo(Symbol(), MODE_SPREAD);
  for(  i = total - 1; i >= 0; i--) 
     { 
       OrderSelect(i, SELECT_BY_POS, MODE_TRADES); MN=OrderMagicNumber();
       // check for symbol & magic number
       if(OrderSymbol() == Symbol() && MN>= 772012000 && MN<=772012199) 
         {  if (MN==772012055) {sl = slx; tp = tpx*slx; mn= mnx1;}
            if (MN==772012155) {sl = sly; tp = tpy*sly; mn= mny1;}
         
         
           int prevticket = OrderTicket();
           // long position is opened
           if(OrderType() == OP_BUY) 
             {if(DayOfWeek( ) == 5 && Hour( ) >=22)  { OrderClose(prevticket,OrderLots( ) ,Bid,3,Red);} 
              if(Bid > (OrderStopLoss() + (sl * 2  + spread) * Point)) 
                 { if(BTS()< 0) 
                     { OrderClose(prevticket,OrderLots( ) ,Bid,3,Red);} 
                   else 
                    { OrderModify(OrderTicket(), OrderOpenPrice(), Bid - sl * Point,0, 0, Blue);}
                 }  
             } 
           else 
             {if(DayOfWeek( ) == 5 && Hour( ) >=22) { OrderClose(prevticket,OrderLots( ) ,Ask,3,Blue);} 
              if(Ask < (OrderStopLoss() - (sl * 2 + spread) * Point)) 
                 {if(BTS() > 0) 
                     { OrderClose(prevticket,OrderLots( ) ,Ask,3,Blue);} 
                  else 
                     { OrderModify(OrderTicket(), OrderOpenPrice(), Ask + sl * Point, 0, 0, Blue);}
                 }  
             }
           // exit
           return(0);
         }
     }
}
//+ииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии+ 
//+ии                   ъюэхі ђ№хщышэур                            ии+
//+ииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии+ 
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//+дддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддддд+
//| дѓэъішџ я№ютх№ъш Trade    BTS                                                        |
//| C_XYZ (Check X Y Z)                                                                  |
//+--------------------------------------------------------------------------------------+
/*   bool C_XYZ2(int Tr, int Signal, int Tfr_G,int Per_G,int Kp)
      {bool     XYZ    = false;
      if (Per_G!=0)  {            
         double   iCustom_G01  =  iCus(Tfr_G,Per_G,0,1);
         double   iCustom_G02  =  iCus(Tfr_G,Per_G,0,2);
         double   iCustom_G03  =  iCus(Tfr_G,Per_G,0,3);
              
         double   Delta_G12    =  iCustom_G01 - iCustom_G02 ;
         double   Delta_G23    =  iCustom_G02 - iCustom_G03 ;}
         
         double   icc          =  iCCI(Symbol(),Tfr_G, Kp, PRICE_OPEN, 0);
              
      switch (Tr){//********************
         case 0://------------------------
            switch (Signal){//********************
               case 0: if((Delta_G12>0  ))                 XYZ = true; else  XYZ = false; break;
               case 1: if((icc>0 ))                        XYZ = true; else  XYZ = false; break;
               case 2: if((Delta_G12>0 && Delta_G23<=0 ))  XYZ = true; else  XYZ = false; break;}
                           break; 
         case 1://------------------------ 
            switch (Signal){//********************
               case 0: if((Delta_G12<0  ))                 XYZ = true; else  XYZ = false; break;
               case 1: if((icc<0 ))                        XYZ = true; else  XYZ = false; break;
               case 2: if((Delta_G12<0 ))                  XYZ = true; else  XYZ = false; break;
               case 3: if((icc<0 ))                        XYZ = true; else  XYZ = false; break;}
                           break;
                     } 
                     return (XYZ);}*/
//+-----------------------------------------------------------------------------------------+
//| дѓэъішџ я№ютх№ъш ALL Trade                                                              |
//| A_XYZ (Check  ALL X Y Z)                                                                |
//+-----------------------------------------------------------------------------------------+
        /*       bool A_XYZ(int HM_XYZ ,int HM_X ,int HM_Y,int HM_Z)
                  {  bool ALL_XYZ = false ; int MagNum ;
                    if (OrdersTotal( ) >=HM_XYZ  )
                        {ALL_XYZ = true ; }
                    else{ int bu=0 , se=0 ,mggg = 0;
                           for(int rrr = OrdersTotal( )- 1; rrr >= 0; rrr--)
                           {
                            OrderSelect(rrr,SELECT_BY_POS,MODE_TRADES);
                            MagNum = OrderMagicNumber();
                            
                            if  ( 990805000<=MagNum && MagNum<=990805099){ bu++;}
                            if  ( 990805100<=MagNum && MagNum<=990805199) {se++;}
                        
                             
                            if  ( bu>=HM_X || se >=HM_Y ) {ALL_XYZ = true;   break;}
                                          else { ALL_XYZ = false ;}
                            
                           } //for
                    } //else{
                       
                      return (ALL_XYZ);  
                   } //bool A_XYZ(*/
//+*****************************************************************************************+
//+*****************************************************************************************+

//+-----------------------------------------------------------------------------------------+
//| дѓэъішџ я№ютх№ъш Trade                                                                  |
//| C_XYZ (Check X Y Z)                                                                     |
//+-----------------------------------------------------------------------------------------+
    /*           bool C_XYZ(int Tr,int Value , int Tfr_G,int Tfr_L,int Km,int Kl,int ld,int lu)
                  {  
                     bool     XYZ    = false; int B=1 , B1=B,B2=B+1,B3=B+2;
                     double   Stoch_G1 = Stoch(Tfr_G,Km,B1);
                     double   Stoch_G2 = Stoch(Tfr_G,Km,B2);
                     double   Stoch_L1 = Stoch(Tfr_L,Kl,B1);
                     double   Stoch_L2 = Stoch(Tfr_L,Kl,B2);
                     double   Stoch_L3 = Stoch(Tfr_L,Kl,B3);
                     double   MACD_1   = MACD_Z(Z_Tfr,Fst_EMA,Slw_EMA,Smp_SMA,Prc_MACD,0,1);
                     double   MACD_2   = MACD_Z(Z_Tfr,Fst_EMA,Slw_EMA,Smp_SMA,Prc_MACD,0,2);
                     double   MACD_1s  = MACD_Z(Z_Tfr,Fst_EMA,Slw_EMA,Smp_SMA,Prc_MACD,1,1);
                  switch (Tr){
                     case 0:
                           if(Stoch_G1>=lu && Stoch_G1>=Stoch_G2 &&
                              Stoch_L2<=ld && Stoch_L1>Stoch_L2 && Stoch_L3>=Stoch_L2)
                                 XYZ    =   true;
                           else  XYZ    =   false;
                           
                           
                           
                           if(Control_MACD_Z && Value != 0)
                              {switch (Value){
                                 case 1:  if(!(MACD_1<=0 &&  MACD_1 > MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 2:  if(!(MACD_1<=0 &&  MACD_1 > MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break; 
                                 case 3:  if(!(MACD_1<=0 &&  MACD_1 < MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 4:  if(!(MACD_1<=0 &&  MACD_1 < MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break; 
                                 case 5:  if(!(MACD_1>=0 &&  MACD_1 > MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 6:  if(!(MACD_1>=0 &&  MACD_1 > MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break; 
                                 case 7:  if(!(MACD_1>=0 &&  MACD_1 < MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 8:  if(!(MACD_1>=0 &&  MACD_1 < MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break;                              
                                             } //switch (Value)
                              }//if(Control_MACD_Z && Value =! 0)
                           break;
                           
                           
                           
                     case 1:      
                           if(Stoch_G1<=ld && Stoch_G1<Stoch_G2 &&
                              Stoch_L2>=lu && Stoch_L1<Stoch_L2 && Stoch_L3<=Stoch_L2)
                                 XYZ    =   true;
                           else  XYZ    =   false;
                       //    if(Control_MACD_Z && MACD_1>=0)
                       //       XYZ    =   false;
                              
                              if(Control_MACD_Z && Value != 0)
                              {switch (Value){
                                 case 1:  if(!(MACD_1<=0 &&  MACD_1 > MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 2:  if(!(MACD_1<=0 &&  MACD_1 > MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break; 
                                 case 3:  if(!(MACD_1<=0 &&  MACD_1 < MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 4:  if(!(MACD_1<=0 &&  MACD_1 < MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break; 
                                 case 5:  if(!(MACD_1>=0 &&  MACD_1 > MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 6:  if(!(MACD_1>=0 &&  MACD_1 > MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break; 
                                 case 7:  if(!(MACD_1>=0 &&  MACD_1 < MACD_2 && MACD_1<MACD_1s))
                                          XYZ    =   false; break;
                                 case 8:  if(!(MACD_1>=0 &&  MACD_1 < MACD_2 && MACD_1>MACD_1s))
                                          XYZ    =   false; break;                              
                                             } //switch (Value)
                              }//if(Control_MACD_Z && Value =! 0)
                           break;
                           
                           break;
                              }
                    return (XYZ);       
                   }*/
//+*****************************************************************************************+
//+*****************************************************************************************+
//+-----------------------------------------------------------------------------------------+
//| дѓэъішџ MACD                                                                            |
//|                                                                                         |
//+-----------------------------------------------------------------------------------------+
         /*      double MACD_Z(int ZT, int ZF, int ZS, int ZM, int ZP, int ZL,int ZB)
                  { double   MACD_TFSMPLB = iMACD(NULL,ZT,ZF,ZS,ZM,ZP,ZL,ZB);
                    return (MACD_TFSMPLB); }*/
//+*****************************************************************************************+
//+*****************************************************************************************+
//+-----------------------------------------------------------------------------------------+
//| дѓэъішџ Stochastic                                                                      |
//| Stoch (Stochastik)                                                                      |
//+-----------------------------------------------------------------------------------------+
         /*      double Stoch(int XXX, int XX, int B)
                  { double   Stch_XXX_XX_B = iStochastic(NULL,XXX,XX,1,3,MODE_SMA,1,MODE_MAIN,B);
                    return (Stch_XXX_XX_B); }*/
//+*****************************************************************************************+
//+*****************************************************************************************+
//+-----------------------------------------------------------------------------------------+
//| дѓэъішџ тћсю№р №рчьх№р lot                                                              |
//| Lot_S (Lot size)                                                                |
//+-----------------------------------------------------------------------------------------+
          /*     double Lot_S(int kLs)
                  {   RefreshRates();

                      double AcB=AccountBalance( ) , AcE=AccountEquity( ) ; 
                      double Ac_BE = NormalizeDouble(  (AcB- AcE) /AcB ,2) ;
                      double L_S =  MarketInfo (Symbol() ,MODE_MINLOT ) ;
                      
                   // /*  
                    if ( Risk_MM == 0){ L_S = HM_L*L_S*kLs ;return (L_S) ;}
                      else {
                            L_S =  NormalizeDouble((MathSqrt( Risk_MM*AcE)/(2000*Risk_MM))*HM_L*kLs,1) ;
                                 if (L_S  >= MarketInfo (Symbol() ,MODE_MAXLOT ) )
                                     {L_S = MarketInfo (Symbol() ,MODE_MAXLOT );}
                                 if (L_S  >= MarketInfo (Symbol() ,MODE_MAXLOT ) || L_S  >5 )
                                     {L_S = 5;}    
                                     
                                 if (L_S  <= MarketInfo (Symbol() ,MODE_MINLOT ))
                                     {L_S = MarketInfo (Symbol() ,MODE_MINLOT );} 
                                     
                                 if (L_S  <= MarketInfo (Symbol() ,MODE_MINLOT )|| L_S  <0.1)
                                     {L_S = 0.1;}     
                                 
                            /*     if (AccountFreeMarginCheck(Symbol(),OP_BUY,L_S)<=0)
                                     {
                                     L_S = NormalizeDouble((AcE/10000),1);
                                     }     
                                        
                                     
                            return (L_S);
                            
                           }      
                     // return (L_S);     
                   }*/
//+*****************************************************************************************+
//+*****************************************************************************************+                     
/*
void startH1() {
   ticket = -1;
   
   RefreshRates();
   
if (total < HM_ALL) { BuSll (0,1,772012000); 
                                       
if( !(DayOfWeek( ) == 1 && Hour( ) <2) && !(DayOfWeek( ) == 5 && Hour( ) >=18))
{   
if (Trd_Up_X && BTS() > 0 && bu<HM_Up_X)  {  Print ("bu ",bu);
      OrderSend(Symbol(), OP_BUY, lots, Ask, 1, Bid - sl * Point, Bid + tp * Point, WindowExpertName(), mn, 0, Blue); }
      //OrderSend(Symbol(), OP_BUY, lots, Ask, 1, Bid - sl * Point, 0, WindowExpertName(), mn, 0, Green); }
if (Trd_Dn_Y && BTS() < 0  && sll<HM_Dn_Y) { 
      OrderSend(Symbol(), OP_SELL, lots, Bid, 1, Ask + sl * Point, Ask - tp * Point, WindowExpertName(), mn, 0, Red); }
      //OrderSend(Symbol(), OP_SELL, lots, Bid, 1, Ask + sl * Point, 0, WindowExpertName(), mn, 0, Red); }
//-- Exit --
  // return(0);
}}}
*/

void startM1() {
   ticket = -1;RefreshRates();
   
if (total < HM_ALL) { BuSll (0,1,772012000); 
                                       
if( !(DayOfWeek( ) == 1 && Hour( ) <2) && !(DayOfWeek( ) == 5 && Hour( ) >=18))
{   
if (Trd_Up_X && VSR() > 0 && bu<HM_Up_X)  {  Print ("bu ",bu);
      OrderSend(Symbol(), OP_BUY, lots, Ask, 1, Bid - sl * Point, Bid + tp * Point, WindowExpertName(), mn, 0, Blue); }
      //OrderSend(Symbol(), OP_BUY, lots, Ask, 1, Bid - sl * Point, 0, WindowExpertName(), mn, 0, Green); }
if (Trd_Dn_Y && VSR() < 0  && sll<HM_Dn_Y) { 
      OrderSend(Symbol(), OP_SELL, lots, Bid, 1, Ask + sl * Point, Ask - tp * Point, WindowExpertName(), mn, 0, Red); }
      //OrderSend(Symbol(), OP_SELL, lots, Bid, 1, Ask + sl * Point, 0, WindowExpertName(), mn, 0, Red); }
//-- Exit --
  // return(0);
}}}

double VSR() { if(G==4){ if(prcptZ1>0){if(prcptX1>0){sl = slX; tp = tpX*slX; mn= mnX1;return ( 1);}}
                               else   {if(prcptY1>0){sl = slY; tp = tpY*slY; mn= mnY1;return (-1);}} 
                         return (BTS());}
               if(G==3){ if(prcptY1>0){sl = slY; tp = tpY*slY; mn= mnY1;return (-1);}           
               else   {return (BTS());}}
               
               if(G==2){ if(prcptX1>0){sl = slX; tp = tpX*slX; mn= mnX1;return ( 1);}           
               else   {return (BTS());}}
 return (BTS());
} 
/*    єѓэъішџ фыџ №рсюђћ ё X_1008_auto.mqh
void iTest (){   
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   if(!IsTesting() && !IsOptimization()){                //Я№ш ђхёђш№ютрэшш ш юяђшьшчрішш эх чряѓёърђќ
      if(TimeHour(TimeCurrent())==SetHour){                //б№ртэхэшх ђхъѓљхую їрёр ё ѓёђрэютыхээћь фыџ чряѓёър
        if(!StartTest){                                 //Чрљшђр юђ яютђю№эюую чряѓёър
            if(TimeMinute(TimeCurrent())>SetMinute-1){     //б№ртэхэшх фшрярчюэр ьшэѓђ ё ѓёђрэютыхээющ фыџ чряѓёър ьшэѓђющ
               if(TimeMinute(TimeCurrent())<SetMinute+1){  //фшрярчюэ эѓцхэ т ёыѓїрх хёыш яю ъръшь-ђю я№шїшэрь фюыую эхђ эютюую ђшър
                  TimeStart   =TimeLocal();
                  StartTest   =true;                     //дыру чряѓёър ђхёђх№р
                  Print("гфрыхэю ",  GlobalVariablesDeleteAll("Set")," уыюсрыќэћѕ ях№хьхээћѕ я№ш чряѓёъх юяђшьшчрішш");
                  Print("SetStart1 ",GlobalVariableSet("SetStart",1));
                  //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
  while(GlobalVariableCheck("SetStart"))
  {int g=GlobalVariableGet("SetStart");
   switch (g){
   case 1: GlobalVariableSet("SetStart",2); NFS = NFSOne;   Per1="Set1";Per2="Set2";Per3="";Per4="";break;
   case 2: GlobalVariableSet("SetStart",3); NFS = NFSTwo;   Per1="Set3";Per2="Set4";Per3="";Per4="";break;
   case 3: GlobalVariableSet("SetStart",4); NFS = NFSThree; Per1="Set5";Per2="Set6";Per3="";Per4="";break;
   case 4: GlobalVariableDel("SetStart"  ); NFS = NFSFour;  Per1="Set7";Per2="Set8";Per3="";Per4="";break;
             }
                  //~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
   Tester(TestDay,NameMTS,NFS,PuthTester,TimeOut,Gr_Pr,Pr_F,Ex_P,Per1,Per2,Per3,Per4);
   }
   }}}}}
   if(StartTest){                                        //Хёыш єыру чряѓёър ђхёђх№р ѓёђрэютыхэ
       if(TimeLocal()-TimeStart > TimeOut*60)            //Хёыш ё ьюьхэђр чряѓёър я№юјыю сюыќјх ѓёђрэютыхээюую т№хьхэш юцшфрэшџ ђхёђш№ютрэшџ
        {StartTest = false;                              //Юсэѓышь єыру
      }}   
//~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~ 
   if(!StartTest){Comment("Set1 ",Set1,"  | Set2 ",Set2,"  | Set3 ",Set3,"  | Set4 ",Set4);}     
}     
*/

//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//и                                                                                                             и//
//и                         N7S_AO_194694_0.mq4       v.77.2012                                                 и//
//и                         Hunter by profit                                                                    и//  
//и                         Balashiha         S&N@yandex.ru                                                     и//  
//и                         Version  Desember 20, 2007                                                          и// 
//и                                                                                                             и//  
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//
//иииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииииии//