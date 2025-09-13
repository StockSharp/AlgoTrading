//+------------------------------------------------------------------+
//|                                                    Simplerun.mq4 |
//|                                                             Jaco |
//|                                                    www.nvg.co.za |
//+------------------------------------------------------------------+
#property copyright "Jaco"
#property link      "www.nvg.co.za"
int init()
  {
   return(0);
  }
int deinit()
  {
   return(0);
  }

///////////////////
//GlobalVariables//
///////////////////
int nTicket;
int i;
string name,name2;
int a=0;

double onemin_haOpen,onemin_haClose;
double fivemin_haOpen,fivemin_haClose;
double fifteen_min_haOpen,fifteen_min_haClose;
double thirty_min_haOpen,thirty_min_haClose;
double one_hour_haOpen,one_hour_haClose;
double four_hour_haOpen,four_hour_haClose;

int m1,m5,m15,m30,h1,h4;

int MaMetod=2;
int MaPeriod=6;
int MaMetod2 =3;
int MaPeriod2=2;

color COL1,COL2,COL3,COL4,COL5,COL6;

bool fail=false;

int cm1U,cm5U,cm15U,cm30U,ch1U,ch4U=0;
int cm1D,cm5D,cm15D,cm30D,ch1D,ch4D=0;

//EXTERNAL VALIALBES//
extern int TakeProfit=20;
extern int StopLoss=500;
extern int Magic=12367;
extern double LOT = 0.1;


///////////////////
//GlobalVariables//
///////////////////

/////////////
//FUNCTIONS//--------------------------------------------------------------------------------------------
/////////////

//DRAW UP ARROW//
void Draw_up_arrow(int blank)
{
i=Bars;
name = "Up"+i;
   ObjectCreate(name, OBJ_ARROW, 0, Time[a], High[a]+30*Point); //draw an up arrow
   ObjectSet(name, OBJPROP_STYLE, STYLE_SOLID);
   ObjectSet(name, OBJPROP_ARROWCODE, SYMBOL_ARROWUP);
   ObjectSet(name, OBJPROP_COLOR,Lime); 
}


//DRAW DOWN ARROW//
void Draw_down_arrow(int blank)
{
i=Bars;
name2 = "Dn"+i;
   ObjectCreate(name2,OBJ_ARROW, 0, Time[a], Low[a]+30*Point); //draw a dn arrow
   ObjectSet(name2, OBJPROP_STYLE, STYLE_SOLID);
   ObjectSet(name2, OBJPROP_ARROWCODE, SYMBOL_ARROWDOWN);
   ObjectSet(name2, OBJPROP_COLOR,Red);
}



//BUY//
void BUY_FX(int blank)
{
nTicket=OrderSend(Symbol(),OP_BUY,LOT,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"J Order",Magic,0,Green);
}
//BUY//

//SELL//
void SELL_FX(int blank)
{
nTicket=OrderSend(Symbol(),OP_SELL,LOT,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"J Order",Magic,0,Green);
}
//SELL//


//CREATE TEXT ON SCREEN//
void CreateTextLable
 (string TextLableName, string Text, int TextSize, string FontName, color TextColor, int TextCorner, int X, int Y)
  { 
//---
   ObjectCreate(TextLableName, OBJ_LABEL, 0, 0, 0);
   ObjectSet(TextLableName, OBJPROP_CORNER, TextCorner);
   ObjectSet(TextLableName, OBJPROP_XDISTANCE, X);
   ObjectSet(TextLableName, OBJPROP_YDISTANCE, Y);
   ObjectSetText(TextLableName,Text,TextSize,FontName,TextColor);
}


//CREATE TEXT ON SCREEN//
void DRAW_TEXT(int blank)
{
CreateTextLable("label_object1","M1  :",20,"Times New Roman",White,0,10,10);
CreateTextLable("label_object2","M5  :",20,"Times New Roman",White,0,10,30);
CreateTextLable("label_object3","M15:",20,"Times New Roman",White,0,10,50);
CreateTextLable("label_object4","M30:",20,"Times New Roman",White,0,10,70);
CreateTextLable("label_object5","H1  :",20,"Times New Roman",White,0,10,90);
CreateTextLable("label_object6","H4  :",20,"Times New Roman",White,0,10,110);
CreateTextLable("label_object13","FAILED:",20,"Times New Roman",White,0,10,130);
CreateTextLable("label_object15","STOPLS:",20,"Times New Roman",White,0,10,150);
CreateTextLable("label_object18","TAKEPR:",20,"Times New Roman",White,0,10,170);

CreateTextLable("label_object19","PRICE:",20,"Times New Roman",White,0,10,190);
CreateTextLable("label_object20",Bid,20,"Times New Roman",Yellow,0,90,190);


CreateTextLable("label_object16",TakeProfit,20,"Times New Roman",Yellow,0,120,170);

CreateTextLable("label_object17",StopLoss,20,"Times New Roman",Yellow,0,110,150);

//DRAW HAS STARS
CreateTextLable("label_object7","*",40,"Times New Roman",COL1,0,70,0);
CreateTextLable("label_object8","*",40,"Times New Roman",COL2,0,70,20);
CreateTextLable("label_object9","*",40,"Times New Roman",COL3,0,70,40);
CreateTextLable("label_object10","*",40,"Times New Roman",COL4,0,70,60);
CreateTextLable("label_object11","*",40,"Times New Roman",COL5,0,70,80);
CreateTextLable("label_object12","*",40,"Times New Roman",COL6,0,70,100);

if (fail)
CreateTextLable("label_object14","YES",20,"Times New Roman",Red,0,110,130);
else
CreateTextLable("label_object14","NO",20,"Times New Roman",Blue,0,110,130);



//SHOW HAS TIMES
//1MIN
if (m1==1) CreateTextLable("label_object21",cm1U,15,"Times New Roman",Yellow,0,100,10);
else CreateTextLable("label_object21",cm1D,15,"Times New Roman",Yellow,0,100,10);
//5MIN
if (m5==1) CreateTextLable("label_object22",cm5U,15,"Times New Roman",Yellow,0,100,31);
else CreateTextLable("label_object22",cm5D,15,"Times New Roman",Yellow,0,100,31);
//15MIN
if (m15==1) CreateTextLable("label_object23",cm15U,15,"Times New Roman",Yellow,0,100,50);
else CreateTextLable("label_object23",cm15D,15,"Times New Roman",Yellow,0,100,50);
//30MIN
if (m30==1) CreateTextLable("label_object24",cm30U,15,"Times New Roman",Yellow,0,100,68);
else CreateTextLable("label_object24",cm30D,15,"Times New Roman",Yellow,0,100,68);


}


//CREATE TEXT ON SCREEN//


//GET HAS SIGNALS//
void LOAD_HAS(int blank)
{
onemin_haOpen=iCustom(NULL,1,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,2,0);
onemin_haClose=iCustom(NULL,1,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,3,0);
fivemin_haOpen=iCustom(NULL,5,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,2,0);
fivemin_haClose=iCustom(NULL,5,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,3,0);
fifteen_min_haOpen=iCustom(NULL,15,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,2,0);
fifteen_min_haClose=iCustom(NULL,15,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,3,0);
thirty_min_haOpen=iCustom(NULL,30,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,2,0);
thirty_min_haClose=iCustom(NULL,30,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,3,0);
one_hour_haOpen=iCustom(NULL,60,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,2,0);
one_hour_haClose=iCustom(NULL,60,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,3,0);
four_hour_haOpen=iCustom(NULL,240,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,2,0);
four_hour_haClose=iCustom(NULL,240,"Heiken_Ashi_Smoothed",MaMetod,MaPeriod,MaMetod2,MaPeriod2,3,0);
}
//GET HAS SIGNALS//

//GET COLOURS OF BAS LINES
void GET_HAS_UPDOWN_COLOURS(int blank)
{
//1MIN//   
if (onemin_haOpen < onemin_haClose){COL1=Blue;m1=1;}else{COL1=Red;m1=0;}   
//5MIN//   
if (fivemin_haOpen < fivemin_haClose){COL2=Blue;m5=1;}else{COL2=Red;m5=0;}   
//FIFTEEN MIN//   
if (fifteen_min_haOpen < fifteen_min_haClose){COL3=Blue;m15=1;}else{COL3=Red;m15=0;}   
//THIRTY MIN//     
if (thirty_min_haOpen < thirty_min_haClose){COL4=Blue;m30=1;}else{COL4=Red;m30=0;}  
//ONE HOUR//
if (one_hour_haOpen < one_hour_haClose){COL5=Blue;h1=1;}else{COL5=Red;h1=0;}
//FOUR HOUR//
if (four_hour_haOpen < four_hour_haClose){COL6=Blue;h4=1;}else{COL6=Red;h4=0;}
}

//SHOW HAS TIMES
void ADD_HAD_TIME(int blank)
{
//ONE Minute
if (m1==0) {cm1D++; cm1U=0;} else {cm1U++; cm1D=0;}
//FIVE Minute
if (m5==0) {cm5D++; cm5U=0;} else {cm5U++; cm5D=0;}
//FIFTEEN Minute
if (m15==0) {cm15D++; cm15U=0;} else {cm15U++; cm15D=0;}
//THIRTY Minute
if (m30==0) {cm30D++; cm30U=0;} else {cm30U++; cm30D=0;}
//ONE Hour
if (h1==0) {ch1D++; ch1U=0;} else {ch1U++; ch1D=0;}
//FOUR Hour
if (h4==0) {ch4D++; ch4U=0;} else {ch4U++; ch4D=0;}
}
/////////////
//FUNCTIONS//
/////////////


/////////--------------------------------------------------------------------------------------------------------
//START//--------------------------------------------------------------------------------------------------------
/////////--------------------------------------------------------------------------------------------------------
int start()
  {
//----
//INTERNAL VARIALBES//

//INTERNAL VARIALBES//

LOAD_HAS(NULL);
GET_HAS_UPDOWN_COLOURS(NULL);
ADD_HAD_TIME(NULL);
DRAW_TEXT(NULL);


//CHECK IF FAILED AND AJUST LOT
OrderSelect(OrdersHistoryTotal()-1, SELECT_BY_POS, MODE_HISTORY);
if(OrderProfit() < 0) {fail=true;LOT=0.1;} else {fail=false;LOT=0.1;}
//CHECK IF FAILED AND AJUST LOT





// PLACE ORDER
if (OrdersTotal() < 1)
{
//if (m1==1&&m5==0&&m15==1&&m30==1&&h1==1&&h4==1)
if (/*m1==1&&*/cm5U<10&&m5==1&&cm15U>200&&m15==1&&m30==1&&h1==1&&h4==1)
{
if (fail) StopLoss=StopLoss+5;
BUY_FX(NULL);
}

//if (m1==0&&m5==1&&m15==0&&m30==0&&h1==0&&)//h4==0)
if (/*m1==0&&*/cm5D<10&&m5==0&&cm15D>200&&m15==0&&m30==0&&h1==0&&h4==0)
{
if (fail) StopLoss=StopLoss+5;
SELL_FX(NULL);
}
//SELL_FX(NULL);
//BUY_FX(NULL);
}
  
   
//----
   return(0);
  }
//+------------------------------------------------------------------+




