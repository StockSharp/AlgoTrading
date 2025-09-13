#property link        "http:www.trading-go.ru/   full version"
#property copyright   "http:www.trading-go.ru/   full version"


extern string  g = "http:www.trading-go.ru/   full version";
extern double  Lot1 = 0.01 ;
extern double  Lot2 = 0.02 ;
extern double  Lot3 = 0.03 ;
extern double  Lot4 = 0.04 ;
extern double  Lot5 = 0.05 ;
extern int     ShriftOGLAV   =  10  ;
extern int     ShriftDANNIE  =  14  ;
extern int     Slip   = 30;
extern int     magic1 = 0 ;
color          i1  = Green;
color          i2  = Green;
color          i3  = Green;
color          i4  = Green;
color          i5  = Green;
color          i20 = Green;
color          i21 = Green;
int            fer = 0    ;
double         Lot ,si       ;
int    inf, o1,o2,o3,o4,o5;
int    oo1,oo2,oo3,oo4,oo5;
int    inf1,             sou;
int                   sou1;
int      figa=0,figa1,          sou5;
int                  sou15;
bool     flag1,flag2,flag3;
int                 Digats;
int              ob1,  os1;
int              ob,   os ;
int                 go;
string den;
color colorr;
#include <WinUser32.mqh>
int start()
  {
 

  //while(!IsStopped()) // 
{ 
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
if (fer==0)    
{
 sou  = ObjectGet("521",      OBJPROP_XDISTANCE);
 inf  = ObjectGet("561",      OBJPROP_XDISTANCE);
 sou5 = ObjectGet("531",      OBJPROP_XDISTANCE);
 ob   = ObjectGet("op_buy",      OBJPROP_XDISTANCE);
 os   = ObjectGet("op_sell",     OBJPROP_XDISTANCE);
 o1   = ObjectGet("1"  , OBJPROP_XDISTANCE);
 o2   = ObjectGet("2"  , OBJPROP_XDISTANCE);
 o3   = ObjectGet("3"  , OBJPROP_XDISTANCE);
 o4   = ObjectGet("4"  , OBJPROP_XDISTANCE);
 o5   = ObjectGet("5"  , OBJPROP_XDISTANCE);
}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
if (o2!=0&&o3!=0&&o4!=0&&o5!=0&&ob!=0&&os!=0){fer=1;} 
 inf1 = ObjectGet("561",      OBJPROP_XDISTANCE);
 sou1 = ObjectGet("521",      OBJPROP_XDISTANCE);
 sou15 = ObjectGet("531",      OBJPROP_XDISTANCE);
 ob1   = ObjectGet("op_buy",      OBJPROP_XDISTANCE);
 os1   = ObjectGet("op_sell",     OBJPROP_XDISTANCE);
 oo1   = ObjectGet("1"  , OBJPROP_XDISTANCE);
 oo2   = ObjectGet("2"  , OBJPROP_XDISTANCE);
 oo3   = ObjectGet("3"  , OBJPROP_XDISTANCE);
 oo4   = ObjectGet("4"  , OBJPROP_XDISTANCE);
 oo5   = ObjectGet("5"  , OBJPROP_XDISTANCE);
 //ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
if (o1!=oo1)     {Lot=Lot1;  i1   = Red;i2 = Green;i3 = Green;i4 = Green;i5 = Green;}
if (o2!=oo2)     {Lot=Lot2;  i2   = Red;i1 = Green;i3 = Green;i4 = Green;i5 = Green;}
if (o3!=oo3)     {Lot=Lot3;  i3   = Red;i1 = Green;i2 = Green;i4 = Green;i5 = Green;}
if (o4!=oo4)     {Lot=Lot4;  i4   = Red;i1 = Green;i2 = Green;i3 = Green;i5 = Green;}
if (o5!=oo5)     {Lot=Lot5;  i5   = Red;i1 = Green;i2 = Green;i3 = Green;i4 = Green;}

if (sou!=sou1  )     {go=1;  i20   = Blue;i21 =Green;}
if (sou5!=sou15)     {go=2;  i21   = Blue;i20 = Green;}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("LOT", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("LOT", OBJPROP_CORNER   , 3                 );
ObjectSet    ("LOT", OBJPROP_XDISTANCE, 15                 );                    
ObjectSet    ("LOT", OBJPROP_YDISTANCE, 220                );
ObjectSetText("LOT","=============",ShriftDANNIE,"Arial",Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("LOT1", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("LOT1", OBJPROP_CORNER   , 3                 );
ObjectSet    ("LOT1", OBJPROP_XDISTANCE, 15                 );                   
ObjectSet    ("LOT1", OBJPROP_YDISTANCE, 100                );
ObjectSetText("LOT1","=============",ShriftDANNIE,"Arial",Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("1", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("1", OBJPROP_CORNER   , 3                 );
ObjectSet    ("1", OBJPROP_XDISTANCE, 30                );                     
ObjectSet    ("1", OBJPROP_YDISTANCE, 200                );
ObjectSetText("1","LOT 1 = "+DoubleToStr(Lot1,2),ShriftDANNIE,"Arial",i1);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("2", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("2", OBJPROP_CORNER   , 3                 );
ObjectSet    ("2", OBJPROP_XDISTANCE, 30                 );                     
ObjectSet    ("2", OBJPROP_YDISTANCE, 180                );
ObjectSetText("2","LOT 2 = "+DoubleToStr(Lot2,2),ShriftDANNIE,"Arial",i2);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("3", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("3", OBJPROP_CORNER   , 3                 );
ObjectSet    ("3", OBJPROP_XDISTANCE, 30                 );                     
ObjectSet    ("3", OBJPROP_YDISTANCE, 160                );
ObjectSetText("3","LOT 3 = "+DoubleToStr(Lot3,2),ShriftDANNIE,"Arial",i3);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("4", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("4", OBJPROP_CORNER   , 3                 );
ObjectSet    ("4", OBJPROP_XDISTANCE, 30                 );                    
ObjectSet    ("4", OBJPROP_YDISTANCE, 140                );
ObjectSetText("4","LOT 4 = "+DoubleToStr(Lot4,2),ShriftDANNIE,"Arial",i4);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("5", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("5", OBJPROP_CORNER   , 3                 );
ObjectSet    ("5", OBJPROP_XDISTANCE, 30                 );                    
ObjectSet    ("5", OBJPROP_YDISTANCE, 120               );
ObjectSetText("5","LOT 5 = "+DoubleToStr(Lot5,2),ShriftDANNIE,"Arial",i5);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("lt", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("lt", OBJPROP_CORNER   , 1                 );
ObjectSet    ("lt", OBJPROP_XDISTANCE, 15                 );                     
ObjectSet    ("lt", OBJPROP_YDISTANCE, 40                );
ObjectSetText("lt","=============",ShriftDANNIE,"Arial",Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("lt1", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("lt1", OBJPROP_CORNER   , 1                 );
ObjectSet    ("lt1", OBJPROP_XDISTANCE, 15                 );                     
ObjectSet    ("lt1", OBJPROP_YDISTANCE, 100                );
ObjectSetText("lt1","=============",ShriftDANNIE,"Arial",Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("ltt1", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("ltt1", OBJPROP_CORNER   , 3                 );
ObjectSet    ("ltt1", OBJPROP_XDISTANCE, 15                 );                   
ObjectSet    ("ltt1", OBJPROP_YDISTANCE, 40                );
ObjectSetText("ltt1","=============",ShriftDANNIE,"Arial",Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("op_buy", OBJ_LABEL, 0, 0, 0    );           
ObjectSet    ("op_buy", OBJPROP_CORNER, 1    );             
ObjectSet    ("op_buy", OBJPROP_XDISTANCE, 30);             
ObjectSet    ("op_buy", OBJPROP_YDISTANCE, 60);              
ObjectSetText("op_buy", "OP_BUY       ",ShriftDANNIE,"Arial", Blue);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("op_sell", OBJ_LABEL, 0, 0, 0    );               
ObjectSet    ("op_sell", OBJPROP_CORNER, 1    );            
ObjectSet    ("op_sell", OBJPROP_XDISTANCE, 30);            
ObjectSet    ("op_sell", OBJPROP_YDISTANCE, 80);            
ObjectSetText("op_sell", "OP_SELL      ",ShriftDANNIE,"Arial", Red);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("51", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("51", OBJPROP_CORNER   , 3                 );
ObjectSet    ("51", OBJPROP_XDISTANCE, 30                 );                    
ObjectSet    ("51", OBJPROP_YDISTANCE, 80               );
ObjectSetText("51","--- SOUND ---",ShriftDANNIE,"Arial",Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("521", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("521", OBJPROP_CORNER   , 3                 );
ObjectSet    ("521", OBJPROP_XDISTANCE, 80                 );                    
ObjectSet    ("521", OBJPROP_YDISTANCE, 60               );
ObjectSetText("521"," ON   ",ShriftDANNIE,"Arial",i20);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("531", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("531", OBJPROP_CORNER   , 3                 );
ObjectSet    ("531", OBJPROP_XDISTANCE, 40                 );                    
ObjectSet    ("531", OBJPROP_YDISTANCE, 60               );
ObjectSetText("531"," OFF ",ShriftDANNIE,"Arial",i21);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
ObjectCreate ("561", OBJ_LABEL,   0, 0, 0                 );
ObjectSet    ("561", OBJPROP_CORNER   , 3                 );
ObjectSet    ("561", OBJPROP_XDISTANCE, 10                 );                     
ObjectSet    ("561", OBJPROP_YDISTANCE, 20              );
ObjectSetText("561","on-<<-INFO->>-off",ShriftDANNIE,"Arial",Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
   ObjectCreate ("o1", OBJ_LABEL, 0, 0, 0    );              
   ObjectSet    ("o1", OBJPROP_CORNER, 0    );               
   ObjectSet    ("o1", OBJPROP_XDISTANCE, 500);             
   ObjectSet    ("o1", OBJPROP_YDISTANCE, 15);               
   ObjectSetText("o1", "http:www.trading-go.ru/   full version",20, "Times New Roman", Red);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
   int total=OrdersTotal();
   int b=0,s=0, n=0, bl=0,sl=0, bs=0,ss=0,b2=0,s2=0;
   for (int i=total; i>=0; i--)
   {if(OrderSelect(i, SELECT_BY_POS))
   {if(OrderSymbol()==Symbol()      )
   {n++;
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
      if (OrderType()==OP_BUY &&     OrderMagicNumber()==magic1){b++ ;}
      if (OrderType()==OP_SELL&&     OrderMagicNumber()==magic1){s++ ;}    }}}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
double ask=NormalizeDouble(Ask,Digits);
double bid=NormalizeDouble(Bid,Digits);
if (ob !=ob1 )        { flag1=1; }
if (flag1==1&&ob==ob1){ flag1=2; } 
if (flag1==2)  
{OrderSend(Symbol(),OP_BUY      ,Lot,ask   ,Slip,0,0,"Order BUY #",magic1,0,Green);
 flag1=0;}
if (os !=os1 )        { flag2=1; } 
if (flag2==1&&os==os1){ flag2=2; } 
if (flag2==2)   
{OrderSend(Symbol(),OP_SELL     ,Lot,bid   ,Slip,0,0,"Order SELL#",magic1,0,Green);
 flag2=0;}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
if (Digits==4)
{
Digats=1;
}
if (Digits==5)
{
Digats=10;
}

 double profitb  = NormalizeDouble(50*Digats*Point,Digits);
 double profits  = NormalizeDouble(50*Digats*Point,Digits);
 
   for (int ii=total; ii>=0; ii--)
   {if(OrderSelect(ii, SELECT_BY_POS))
   {if(OrderSymbol()==Symbol()      )
   {

        ObjectCreate    ("OPT"+OrderTicket(), OBJ_TEXT,   0,Time[0]+200000,ObjectGet("OP"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSetText   ("OPT"+OrderTicket(), StringConcatenate("Tikket ",OrderTicket(),"||      ",DoubleToStr(OrderProfit(),2),"---LOT="+DoubleToStr(OrderLots(),2)),8,"Arial",Blue);
        ObjectSet       ("OPT"+OrderTicket(), OBJPROP_PRICE1,ObjectGet("OP"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSet       ("OPT"+OrderTicket(), OBJPROP_TIME1,Time[0]+200000);
        ObjectSet       ("OPT"+OrderTicket(), OBJPROP_COLOR,Green);
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
      if (OrderType()==OP_BUY &&     OrderMagicNumber()==magic1)
      {
        ObjectCreate    ("OP"+OrderTicket() ,OBJ_HLINE,0,0,OrderOpenPrice());
        ObjectSet       ("OP"+OrderTicket() ,OBJPROP_COLOR,Blue);
        ObjectSet       ("OP"+OrderTicket() ,OBJPROP_STYLE,1);
        ObjectSet       ("OP"+OrderTicket() ,OBJPROP_WIDTH,1);      
       if (OrderTakeProfit()==0&&OrderStopLoss()==0)
       {
        OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-profitb,OrderOpenPrice()+profitb,0,Blue);
       }
       if (OrderTakeProfit()>0)
       {
        ObjectCreate    ("TPB"+OrderTicket() , OBJ_HLINE,0,0,OrderOpenPrice()+profitb);
        ObjectSet       ("TPB"+OrderTicket() , OBJPROP_COLOR,Blue);
        ObjectSet       ("TPB"+OrderTicket() , OBJPROP_STYLE,0);
        ObjectSet       ("TPB"+OrderTicket() , OBJPROP_WIDTH,1);
        ObjectCreate    ("TPBT"+OrderTicket(), OBJ_TEXT,   0,Time[0]+200000,ObjectGet("TPB"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSetText   ("TPBT"+OrderTicket(), StringConcatenate("Tikket ",OrderTicket(),"Take Profit---",DoubleToStr((OrderTakeProfit()-OrderOpenPrice())/Digats/Point,0),"p---LOT="+DoubleToStr(OrderLots(),2)),8,"Arial",Blue);
        ObjectSet       ("TPBT"+OrderTicket(), OBJPROP_PRICE1,ObjectGet("TPB"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSet       ("TPBT"+OrderTicket(), OBJPROP_TIME1,Time[0]+200000);
        ObjectSet       ("TPBT"+OrderTicket() , OBJPROP_COLOR,Blue);
       }
       //-------------------------------------------------------------------------
       if (OrderStopLoss()>0)
       {
        ObjectCreate    ("SLB"+OrderTicket() ,OBJ_HLINE,0,0,OrderOpenPrice()-profitb);
        ObjectSet       ("SLB"+OrderTicket() ,OBJPROP_COLOR,Blue);
        ObjectSet       ("SLB"+OrderTicket() ,OBJPROP_STYLE,0);
        ObjectSet       ("SLB"+OrderTicket() ,OBJPROP_WIDTH,1);
        ObjectCreate    ("SLBT"+OrderTicket(), OBJ_TEXT,   0,Time[0]+200000,ObjectGet("SLB"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSetText   ("SLBT"+OrderTicket(), StringConcatenate("Tikket ",OrderTicket(),"Stop Loss---",DoubleToStr((OrderOpenPrice()-OrderStopLoss())/Digats/Point,0),"p---LOT="+DoubleToStr(OrderLots(),2)),8,"Arial",Blue);
        ObjectSet       ("SLBT"+OrderTicket(), OBJPROP_PRICE1,ObjectGet("SLB"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSet       ("SLBT"+OrderTicket(), OBJPROP_TIME1,Time[0]+200000);
        ObjectSet       ("SLBT"+OrderTicket() , OBJPROP_COLOR,Blue);
       }       
       if (OrderStopLoss()  !=ObjectGet("SLB"+OrderTicket(),OBJPROP_PRICE1||OrderTakeProfit()!=ObjectGet("TPB"+OrderTicket()   ,OBJPROP_PRICE1)))
       {
       OrderModify(OrderTicket(),OrderOpenPrice(),ObjectGet("SLB"+OrderTicket(),OBJPROP_PRICE1),ObjectGet("TPB"+OrderTicket()   ,OBJPROP_PRICE1),0,Blue);
       }
      }
      //---------------------------------------------------------------------------------------------------------------------------------
      if (OrderType()==OP_SELL&&     OrderMagicNumber()==magic1)
      {
        ObjectCreate    ("OP"+OrderTicket() ,OBJ_HLINE,0,0,OrderOpenPrice());
        ObjectSet       ("OP"+OrderTicket() ,OBJPROP_COLOR,Red);
        ObjectSet       ("OP"+OrderTicket() ,OBJPROP_STYLE,1);
        ObjectSet       ("OP"+OrderTicket() ,OBJPROP_WIDTH,1);      
       if (OrderTakeProfit()==0&&OrderStopLoss()==0)
       {
        OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+profits,OrderOpenPrice()-profits,0,Blue);
       }
       if (OrderTakeProfit()>0)
       {        
        ObjectCreate    ("TPS"+OrderTicket() ,OBJ_HLINE,0,0,OrderOpenPrice()-profits);
        ObjectSet       ("TPS"+OrderTicket() ,OBJPROP_COLOR,Red);
        ObjectSet       ("TPS"+OrderTicket() ,OBJPROP_STYLE,0);
        ObjectSet       ("TPS"+OrderTicket() ,OBJPROP_WIDTH,1);
        ObjectCreate    ("TPST"+OrderTicket(), OBJ_TEXT,   0,Time[0]+200000,ObjectGet("TPS"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSetText   ("TPST"+OrderTicket(), StringConcatenate("Tikket ",OrderTicket(),"Take Profit---",DoubleToStr((OrderOpenPrice()-OrderTakeProfit())/Digats/Point,0),"p---LOT="+DoubleToStr(OrderLots(),2)),8,"Arial",Blue);
        ObjectSet       ("TPST"+OrderTicket(), OBJPROP_PRICE1,ObjectGet("TPS"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSet       ("TPST"+OrderTicket(), OBJPROP_TIME1,Time[0]+200000);
        ObjectSet       ("TPST"+OrderTicket() ,OBJPROP_COLOR,Red);
       }
       //-------------------------------------------------------------------------
       if (OrderStopLoss()>0)
       {
        ObjectCreate    ("SLS"+OrderTicket() ,OBJ_HLINE,0,0,OrderOpenPrice()+profitb);
        ObjectSet       ("SLS"+OrderTicket() ,OBJPROP_COLOR,Red);
        ObjectSet       ("SLS"+OrderTicket() ,OBJPROP_STYLE,0);
        ObjectSet       ("SLS"+OrderTicket() ,OBJPROP_WIDTH,1);
        ObjectCreate    ("SLST"+OrderTicket(), OBJ_TEXT,   0,Time[0]+200000,ObjectGet("SLS"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSetText   ("SLST"+OrderTicket(), StringConcatenate("Tikket ",OrderTicket(),"Stop Loss---",DoubleToStr((OrderStopLoss()-OrderOpenPrice())/Digats/Point,0),"p---LOT="+DoubleToStr(OrderLots(),2)),8,"Arial",Blue);
        ObjectSet       ("SLST"+OrderTicket(), OBJPROP_PRICE1,ObjectGet("SLS"+OrderTicket(),OBJPROP_PRICE1));
        ObjectSet       ("SLST"+OrderTicket(), OBJPROP_TIME1,Time[0]+200000);
        ObjectSet       ("SLST"+OrderTicket() ,OBJPROP_COLOR,Red);
       }       
       if (OrderStopLoss()  !=ObjectGet("SLS"+OrderTicket(),OBJPROP_PRICE1||OrderTakeProfit()!=ObjectGet("TPS"+OrderTicket()   ,OBJPROP_PRICE1)))
       {
       OrderModify(OrderTicket(),OrderOpenPrice(),ObjectGet("SLS"+OrderTicket(),OBJPROP_PRICE1),ObjectGet("TPS"+OrderTicket()   ,OBJPROP_PRICE1),0,Blue);
       }
      }
       }}}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ         
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ         
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ         

if (inf!=inf1)
{
if (MessageBox("INFO",MB_YESNO)==IDYES)
{
double prib = 0;   int zzz=0; double minus=0; 
   for (int uuui=total-1; uuui>=0; uuui--)                                 
   {if(OrderSelect(uuui, SELECT_BY_POS))                          
   {if(OrderSymbol()!=Symbol())continue;                          
   {double  pri=OrderProfit();
    prib=prib+pri;
    zzz++;                                    
    if (pri<0)    { minus+=pri;}
    if (minus<si) { si=minus;  }
    }}}
//-----------------------------------------------------------------------------------------------------
string balance=DoubleToStr(AccountBalance(),2);
string profit =DoubleToStr(prib,2);
string profi  =DoubleToStr(si,2);
color cvet=Green;
if (prib<0){cvet=Red;  }
//-----------------------------------------------------------------------------------------------------
   ObjectCreate ("Balance", OBJ_LABEL, 0, 0, 0    );              
   ObjectSet    ("Balance", OBJPROP_CORNER,0    );              
   ObjectSet    ("Balance", OBJPROP_XDISTANCE, 10);              
   ObjectSet    ("Balance", OBJPROP_YDISTANCE, 10);              
   ObjectSetText("Balance", "==BALANCE   =="+DoubleToStr(AccountBalance(),2),20, "Arial", Green);
//-----------------------------------------------------------------------------------------------------
   ObjectCreate ("Balance1", OBJ_LABEL, 0, 0, 0    );             
   ObjectSet    ("Balance1", OBJPROP_CORNER, 0    );             
   ObjectSet    ("Balance1", OBJPROP_XDISTANCE, 10);               
   ObjectSet    ("Balance1", OBJPROP_YDISTANCE, 40);             
   ObjectSetText("Balance1", "==PROFIT       =="+profit,20, "Arial", cvet);
//-----------------------------------------------------------------------------------------------------
   ObjectCreate ("minus", OBJ_LABEL, 0, 0, 0    );             
   ObjectSet    ("minus", OBJPROP_CORNER,0   );          
   ObjectSet    ("minus", OBJPROP_XDISTANCE, 10);             
   ObjectSet    ("minus", OBJPROP_YDISTANCE, 70);               
   ObjectSetText("minus", "==MAX MINUS=="+profi,20, "Arial", Green);
//===========================================================================================================================//
//                                                             //
//===========================================================================================================================//
	double ði;
   int m,u,k;
   m=Time[0]+Period()*60-CurTime();
   ði=m/60.0;
   u=m%60;
   m=(m-m%60)/60;

   ObjectDelete("time");  
   if(ObjectFind("time") != 0)
   {
   ObjectCreate ("time", OBJ_LABEL, 0, 0, 0);
   ObjectSet    ("time", OBJPROP_CORNER, 2    );
   ObjectSet    ("time", OBJPROP_XDISTANCE, 270);             
   ObjectSet    ("time", OBJPROP_YDISTANCE, 1);
   ObjectSetText("time", " -"+m+":"+u, 30, "Arial Black", Red);
   }
   else
   {
   ObjectMove("time", 0, Time[0], Close[0]+0.0030);
   }
   ObjectCreate ("simvol", OBJ_LABEL, 0, 0, 0);
   ObjectSet    ("simvol", OBJPROP_CORNER, 2    );
   ObjectSet    ("simvol", OBJPROP_XDISTANCE, 1);              
   ObjectSet    ("simvol", OBJPROP_YDISTANCE, 150);
   ObjectSetText("simvol", Symbol(), 40, "Arial", Green);
//===========================================================================================================================//
//            //
//===========================================================================================================================//
   if (Ask>ask){ask=Ask;colorr=Green;}
   if (Ask<ask){ask=Ask;colorr=Red  ;}
   if (DayOfWeek()==1){den="Monday";}
   if (DayOfWeek()==2){den="Tuesday    ";}
   if (DayOfWeek()==3){den="Wednesday      ";}
   if (DayOfWeek()==4){den="Thursday    ";}
   if (DayOfWeek()==5){den="Friday    ";}
//-----------------------------------------------------------------------------------------------------  
   ObjectCreate ("Ask", OBJ_LABEL, 0, 0, 0    );
   ObjectSet    ("Ask", OBJPROP_CORNER, 2     );
   ObjectSet    ("Ask", OBJPROP_XDISTANCE, 100 );              
   ObjectSet    ("Ask", OBJPROP_YDISTANCE, 120);
   ObjectSetText("Ask", DoubleToStr(Ask,5), 20, "Arial", colorr);  
   ObjectCreate ("Bid", OBJ_LABEL, 0, 0, 0    );
   ObjectSet    ("Bid", OBJPROP_CORNER, 2     );
   ObjectSet    ("Bid", OBJPROP_XDISTANCE, 100);              
   ObjectSet    ("Bid", OBJPROP_YDISTANCE, 100);
   ObjectSetText("Bid", DoubleToStr(Bid,5), 20, "Arial", colorr);
   ObjectCreate ("Ask1", OBJ_LABEL, 0, 0, 0    );
   ObjectSet    ("Ask1", OBJPROP_CORNER, 2     );
   ObjectSet    ("Ask1", OBJPROP_XDISTANCE, 10 );               
   ObjectSet    ("Ask1", OBJPROP_YDISTANCE, 120);
   ObjectSetText("Ask1", "ASK", 20, "Arial", Green);
   ObjectCreate ("Bid1", OBJ_LABEL, 0, 0, 0    );
   ObjectSet    ("Bid1", OBJPROP_CORNER, 2     );
   ObjectSet    ("Bid1", OBJPROP_XDISTANCE, 10);               
   ObjectSet    ("Bid1", OBJPROP_YDISTANCE, 100);
   ObjectSetText("Bid1", "BID", 20, "Arial", Green);
   ObjectCreate ("Bd1", OBJ_LABEL, 0, 0, 0   );
   ObjectSet    ("Bd1", OBJPROP_CORNER, 2    );
   ObjectSet    ("Bd1", OBJPROP_XDISTANCE, 10);              
   ObjectSet    ("Bd1", OBJPROP_YDISTANCE, 75);
   ObjectSetText("Bd1", "SPRED "+DoubleToStr((Ask-Bid),Digits), 18, "Arial",Green);
   ObjectCreate ("Bd21", OBJ_LABEL, 0, 0, 0   );
   ObjectSet    ("Bd21", OBJPROP_CORNER, 2    );
   ObjectSet    ("Bd21", OBJPROP_XDISTANCE, 10);               
   ObjectSet    ("Bd21", OBJPROP_YDISTANCE, 50);
   ObjectSetText("Bd21",StringConcatenate("\n",den,"",Day(),".",Month( ),".",Year( ),"ã."),15,"Arial",Green);
   ObjectCreate ("1Bd21", OBJ_LABEL, 0, 0, 0   );
   ObjectSet    ("1Bd21", OBJPROP_CORNER, 2    );
   ObjectSet    ("1Bd21", OBJPROP_XDISTANCE, 10);              
   ObjectSet    ("1Bd21", OBJPROP_YDISTANCE, 30);
   ObjectSetText("1Bd21",StringConcatenate("TERMINAL ",Hour()," h : ",Minute()," m : ",Seconds( )," s"),15,"Arial",Red);
   ObjectCreate ("12Bd21", OBJ_LABEL, 0, 0, 0   );
   ObjectSet    ("12Bd21", OBJPROP_CORNER, 2    );
   ObjectSet    ("12Bd21", OBJPROP_XDISTANCE, 9);             
   ObjectSet    ("12Bd21", OBJPROP_YDISTANCE, 10);
   ObjectSetText("12Bd21",StringConcatenate("MOSCOW  ",Hour()+2," h : ",Minute()," m : ",Seconds( )," s"),15,"Arial",Red);
   }}
//===========================================================================================================================//
//                                                                                                                           //
//===========================================================================================================================//
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
if (inf!=inf1)
{
if(MessageBox("","INFO",MB_YESNO)==IDNO)
{
ObjectDelete ("Balance" );
ObjectDelete ("Balance1");
ObjectDelete ("minus"   );
ObjectDelete ("time"    );
ObjectDelete ("simvol"  );
ObjectDelete ("Ask"     );
ObjectDelete ("Bid"     );
ObjectDelete ("Ask1"    );
ObjectDelete ("Bid1"    );
ObjectDelete ("Bd1"     );
ObjectDelete ("Bd21"    );
ObjectDelete ("1Bd21"   );
ObjectDelete ("12Bd21"  );
}
}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ         
if (n==0)
{
ObjectsDeleteAll(0,OBJ_HLINE); 
ObjectsDeleteAll(0,OBJ_ARROW);
}
   for (int iq=OrdersHistoryTotal( ); iq>=0; iq--)
   {
   if(OrderSelect(iq, SELECT_BY_POS,MODE_HISTORY )) 
   {
   ObjectDelete ("OP"+OrderTicket());
   ObjectDelete ("OPT"+OrderTicket());
   ObjectDelete ("TPB"+OrderTicket());
   ObjectDelete ("TPS"+OrderTicket());
   ObjectDelete ("SLB"+OrderTicket());
   ObjectDelete ("SLS"+OrderTicket());
   ObjectDelete ("TPBT"+OrderTicket());
   ObjectDelete ("TPST"+OrderTicket());
   ObjectDelete ("SLBT"+OrderTicket());
   ObjectDelete ("SLST"+OrderTicket());
}}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
//                           
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ
if (go==1)
{
 PlaySound("111.wav");
}
//ÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆÆ

}


   return(0);
  }