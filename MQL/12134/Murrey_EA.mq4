
//+------------------------------------------------------------------+
//|                                             Murrey EA.mq4        |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Rodolphe Ahmad"
#property link      "https://www.mql5.com/en/users/rodoboss"
#property version   "4.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
//Define
#define  MINUS2OF8 0
#define  MINUS1OF8 1
#define  ZEROOF8ULTIMATESUPPORT 2 
#define  ONEOF8WEAKREVERSE 3 
#define  TWOOF8STRONGREVERSE 4
#define  THREEOF8LOWERRANGE 5
#define  FOUROF8PIVOTALPOINT 6
#define  FIVEOF8UPPERRANG 7
#define  SIXOF8STRONGREVERSE 8
#define  SEVENOF8WEAKREVERSE 9
#define  EIGHTOFEIGHTULTIMATERESISTANE 10
#define  PLUS1OF8 11
#define  PlUS2OF8 12
//End Define
//Extern Variables
extern string expname = "Murrey Expert"; // Expert Name
extern string contactme = "https://www.mql5.com/en/users/rodoboss"; //Contact Me
extern int magicnumber = 123;// Magic Number
extern double lot_size = 0.10; // Lot Size 0.0x or 0.x
bool autocalculatelots = false; int lookback=20;
extern int max_spread = 20; // Max spread , will be auto adjusted for 4 and 5 digits
extern string how = "Exemple";// 40 -> 0.00040  and 40 -> 0.0040 
extern bool  showmurreygraph=true; //Show Murrey Graph
extern bool  showaccountinfo =  true;//Show Account Info
extern bool enablepush = true;//Enable push notification
extern bool enablmail = true;//Enable Email notification
extern bool enable_buy = true; // Enable Buy
extern bool enable_sell = true; // Enable Sell
extern bool MondayTrade = true; // Monday Trade
extern bool TuesdayTrade = true; // Tuesday Trade
extern bool WednesdayTrade = true; // Wednesday Trade
extern bool ThursdayTrade = true; // Thursday Trade
extern bool FridayTrade = true; // Friday Trade
extern bool sendreportSaturday = true; //Send report each Saturday
extern string info = "4 and/or 5 digits pairs only";// 4 and/or 5 digits pairs only
extern string also = "Your broker 4 or 5 Digits";//Depends on 
extern bool warnme = true; // Warn me if Bid is near from level 0/8 or level 8/8
extern bool donottrade = false;//Do not trade , Only Notify draw lines and arrows and via push,email
extern string donottrade_ = "To activate all features expect auto trade";//Set do not trade to true 
bool locked = false;
//End Extern Variables




    int bbperiod  = 50; // B Band period
       int bbdeviation = 4; // B Band deviation
     
 bool cangolong = false; bool cangoshort = false;
//Variables
     double  pips2dbl;       // Stoploss 15 pips    0.0015      0.00150
     int     Digitspips;    // DoubleToStr(dbl/pips2dbl, Digits.pips)
     int     pips2points;    // slippage  3 pips    3=points    30=points
     int lockedfor =0;    //0 not locked   // 1 locked for buy 2 locked for sell 
     string buff_str = "Murrey";
     string  ln_txt[13];     
     int mml_clr[13];   
     bool mode_ = true;//can trade
     double orderatprice=0;
     double secondstoploss = 25;
     double secondtakeprofit = 25;
   
//End Variables

double bindTomurrey(int a)
{RefreshRates();
//
//init code
   mml_clr[0]  = Magenta;     // [-2]/8
   mml_clr[1]  = Pink;        // [-1]/8
   mml_clr[2]  = Blue;        //  [0]/8
   mml_clr[3]  = Orange;      //  [1]/8
   mml_clr[4]  = Red;         //  [2]/8
   mml_clr[5]  = OliveDrab;   //  [3]/8
   mml_clr[6]  = Blue;        //  [4]/8
   mml_clr[7]  = OliveDrab;   //  [5]/8
   mml_clr[8]  = Red;         //  [6]/8
   mml_clr[9]  = Orange;      //  [7]/8
   mml_clr[10] = Blue;        //  [8]/8
   mml_clr[11] = Pink;        // [+1]/8
   mml_clr[12] = Magenta;     // [+2]/8
   
   ln_txt[0]  = "[-2/8]P";
   ln_txt[1]  = "[-1/8]P";
   ln_txt[2]  = "[0/8] - Ultimate Support";
   ln_txt[3]  = "[1/8] - Weak Reverse";
   ln_txt[4]  = "[2/8] - Strong Reverse";
   ln_txt[5]  = "[3/8] - Lower Range";
   ln_txt[6]  = "[4/8] - Pivotal Point";
   ln_txt[7]  = "[5/8] - Upper Rang";
   ln_txt[8]  = "[6/8] - Strong Reverse";
   ln_txt[9]  = "[7/8] - Weak Reverse";
   ln_txt[10] = "[8/8] - Ultimate Resistance";
   ln_txt[11] = "[+1/8]P";
   ln_txt[12] = "[+2/8]P";
  // end initialse lines on graph
//
int P = 64;
int StepBack = 0;

double  dmml = 0,
        dvtl = 0,
        sum  = 0,
        v1 = 0,
        v2 = 0,
        mn = 0,
        mx = 0,
        x1 = 0,
        x2 = 0,
        x3 = 0,
        x4 = 0,
        x5 = 0,
        x6 = 0,
        y1 = 0,
        y2 = 0,
        y3 = 0,
        y4 = 0,
        y5 = 0,
        y6 = 0,
        octave = 0,
        fractal = 0,
        range   = 0,
        finalH  = 0,
        finalL  = 0,
        mml[13] = {0,0,0,0,0,0,0,0,0,0,0,0,0};
        int     
        bn_v1   = 0,
        bn_v2   = 0,
        OctLinesCnt = 13,
        mml_thk = 8,
       
        mml_shft = 3,
        nTime = 0,
        CurPeriod = 0,
        nDigits = 0,
        i = 0;
        
         mml_shft = 50;
         mml_thk  = 3;
         //---- TODO: add your code here

if( (nTime != Time[0]) || (CurPeriod != Period()) ) {
   
  //price
   bn_v1 = Lowest(NULL,0,MODE_LOW,P+StepBack,0);
   bn_v2 = Highest(NULL,0,MODE_HIGH,P+StepBack,0);
  

   v1 = Low[bn_v1];
   v2 = High[bn_v2];

//determine fractal.....
   if( v2<=250000 && v2>25000 )
   fractal=100000;
   else
     if( v2<=25000 && v2>2500 )
     fractal=10000;
     else
       if( v2<=2500 && v2>250 )
       fractal=1000;
       else
         if( v2<=250 && v2>25 )
         fractal=100;
         else
           if( v2<=25 && v2>12.5 )
           fractal=12.5;
           else
             if( v2<=12.5 && v2>6.25)
             fractal=12.5;
             else
               if( v2<=6.25 && v2>3.125 )
               fractal=6.25;
               else
                 if( v2<=3.125 && v2>1.5625 )
                 fractal=3.125;
                 else
                   if( v2<=1.5625 && v2>0.390625 )
                   fractal=1.5625;
                   else
                     if( v2<=0.390625 && v2>0)
                     fractal=0.1953125;
      
   range=(v2-v1);
   sum=MathFloor(MathLog(fractal/range)/MathLog(2));
   octave=fractal*(MathPow(0.5,sum));
   mn=MathFloor(v1/octave)*octave;
   if( (mn+octave)>v2 )
   mx=mn+octave; 
   else
     mx=mn+(2*octave);


// calculating xx
//x2
    if( (v1>=(3*(mx-mn)/16+mn)) && (v2<=(9*(mx-mn)/16+mn)) )
    x2=mn+(mx-mn)/2; 
    else x2=0;
//x1
    if( (v1>=(mn-(mx-mn)/8))&& (v2<=(5*(mx-mn)/8+mn)) && (x2==0) )
    x1=mn+(mx-mn)/2; 
    else x1=0;

//x4
    if( (v1>=(mn+7*(mx-mn)/16))&& (v2<=(13*(mx-mn)/16+mn)) )
    x4=mn+3*(mx-mn)/4; 
    else x4=0;

//x5
    if( (v1>=(mn+3*(mx-mn)/8))&& (v2<=(9*(mx-mn)/8+mn))&& (x4==0) )
    x5=mx; 
    else  x5=0;

//x3
    if( (v1>=(mn+(mx-mn)/8))&& (v2<=(7*(mx-mn)/8+mn))&& (x1==0) && (x2==0) && (x4==0) && (x5==0) )
    x3=mn+3*(mx-mn)/4; 
    else x3=0;

//x6
    if( (x1+x2+x3+x4+x5) ==0 )
    x6=mx; 
    else x6=0;

     finalH = x1+x2+x3+x4+x5+x6;
// calculating yy
//y1
    if( x1>0 )
    y1=mn; 
    else y1=0;

//y2
    if( x2>0 )
    y2=mn+(mx-mn)/4; 
    else y2=0;

//y3
    if( x3>0 )
    y3=mn+(mx-mn)/4; 
    else y3=0;

//y4
    if( x4>0 )
    y4=mn+(mx-mn)/2; 
    else y4=0;

//y5
    if( x5>0 )
    y5=mn+(mx-mn)/2; 
    else y5=0;

//y6
    if( (finalH>0) && ((y1+y2+y3+y4+y5)==0) )
    y6=mn; 
    else y6=0;

    finalL = y1+y2+y3+y4+y5+y6;

    for( i=0; i<OctLinesCnt; i++) {
         mml[i] = 0;
         }
         
   dmml = (finalH-finalL)/8;

   mml[0] =(finalL-dmml*2); //-2/8
   for( i=1; i<OctLinesCnt; i++) {
        mml[i] = mml[i-1] + dmml;
        }
          for( i=0; i<OctLinesCnt; i++ ){
        buff_str = "mml"+(string)i; ObjectDelete(buff_str);
        if(ObjectFind(buff_str) == -1) {
           ObjectCreate(buff_str, OBJ_HLINE, 0, Time[0], mml[i]);
           ObjectSet(buff_str, OBJPROP_STYLE, STYLE_SOLID);
           ObjectSet(buff_str, OBJPROP_COLOR, mml_clr[i]);
           ObjectMove(buff_str, 0, Time[0],  mml[i]);
            if (showmurreygraph == false)
           ObjectDelete(buff_str);
           }
        else {
           ObjectMove(buff_str, 0, Time[0],  mml[i]);
             if (showmurreygraph == false)
           ObjectDelete(buff_str);
            
           }
             
        buff_str = "mml_txt"+(string)i; ObjectDelete(buff_str);
        if(ObjectFind(buff_str) == -1) {
           ObjectCreate(buff_str, OBJ_TEXT, 0, Time[mml_shft], mml_shft);
           ObjectSetText(buff_str, ln_txt[i], 8, "Arial", mml_clr[i]);
           ObjectMove(buff_str, 0, Time[mml_shft],  mml[i]);
              if (showmurreygraph == false)
           ObjectDelete(buff_str);
           }
        else {
           ObjectMove(buff_str, 0, Time[mml_shft],  mml[i]);
              if (showmurreygraph == false)
           ObjectDelete(buff_str);
           }
        } // for( i=1; i<=OctLinesCnt; i++ ){
}


       return mml[a];
}
int OnInit()
  {
   
//--- 

   InitSlippage();
   showhideAccountInfo();
   showhideMurreyLines();
   showhideCurrentLevel();
   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  { showhideCurrentLevel();    appendMode();
//---
          if (weekdays()) // Monday Tuesday Wednesday Thursday Friday
          {
             if(TradeAllowedforThisDay())
             {
               if ( isspreadok() )
               {
                  if (lockedfor==0 && noorders()){
                   if (enable_buy){trytoGoLong();}
                   if (enable_sell){trytoGoShort();}
                  }
                  
                  if (locked == false)
                  {
              
                           if (Bid > bindTomurrey(ZEROOF8ULTIMATESUPPORT) && Bid < bindTomurrey(ZEROOF8ULTIMATESUPPORT)+0.00025)
                           {
                           if (   cangolong == true &&  cangoshort == false)
                           {
                              cangolong = false; cangolong = false;
                                   locked=true; lockedfor = 1;
                                   
                                   if ( no130error("buy") )
                                   {
                                                   bool ticket1  = OrderSend(Symbol() , OP_BUY , calculateit() , MarketInfo(Symbol(),MODE_ASK) , 2*pips2points , (double) bindTomurrey(MINUS2OF8) , (double) bindTomurrey(ONEOF8WEAKREVERSE),NULL,magicnumber);
                          RefreshRates(); orderatprice = Ask;
                                   }
           
                           }
                              
                           
                   
                         
                           }
           
                  }
                  if (locked == false)
                  {
                
                   if (Bid < bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE) && Bid > bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE) -0.00025)
                      {
 
                              if (   cangolong == false &&  cangoshort == true)
                           {
                              cangolong = false; cangolong = false;
                                   locked=true; lockedfor = 2;
                                   
                                     if(  no130error("sell")  )
                                   {
                                                  bool ticket1  = OrderSend(Symbol() , OP_SELL , calculateit(),  MarketInfo(Symbol(),MODE_BID) , 2*pips2points ,(double) bindTomurrey(PlUS2OF8) ,(double) bindTomurrey(SEVENOF8WEAKREVERSE),NULL,magicnumber); 
                         RefreshRates(); orderatprice = Bid;
                                   }
               
                           }
                           
                           
                           
                           
                      }
                  }
     
                                  
               }
               
             }
           
          }
     
     RefreshRates();
       if (  Bid < bindTomurrey(FIVEOF8UPPERRANG) && lockedfor==2) {
       lockedfor=0;
       locked=false;
        } 
       if (Bid > bindTomurrey(FIVEOF8UPPERRANG) && lockedfor==1) {
       lockedfor=0; 
       locked=false; }
        SurviveOrdersstrategy1();
  }
//+------------------------------------------------------------------+
void InitSlippage()
{
RefreshRates();
     if (Digits == 5 || Digits == 3)
    {    // Adjust for five (5) digit brokers.
                pips2dbl    = Point*10; pips2points = 10;   Digitspips = 1;
    } 
    else {    pips2dbl    = Point;    pips2points =  1;   Digitspips = 0; }
}


void showhideAccountInfo()
{
if(showaccountinfo)  //Show Account essential information
  {
  string str = "Account Name: " + AccountName() + "\n";
  str+=  "Company: " + AccountCompany() + "\n";
  str+=  "Account Leverage: " + (string)AccountLeverage() + "\n";
  str+=  "Min Lot: " + (string)MarketInfo(Symbol(), MODE_MINLOT) + "\n";
  str+= "Long Swap: " + (string)MarketInfo(Symbol(), MODE_SWAPLONG) + "\n";
  str+= "Short Swap: " + (string)MarketInfo(Symbol(), MODE_SWAPSHORT) + "\n";
  str+= "Spread:" + (string)MarketInfo(Symbol(), MODE_SPREAD) +"\n";
  str+= "Expert Name: " + WindowExpertName() +"\n";
  if(IsDemo()){str+= "Account Type: Demo";}else{str+= "Account Type:Real";}
  str+="\n";
  str+= "Recomendation ...   M30 H1 H4 D1 " + "\n";
  str+= "Free Margin: " + (string)AccountFreeMargin() + "\n";
  str+= "Magic Number : " + (string) magicnumber +"\n";
  
  Comment(str);
  }
}

void showhideMurreyLines()
{
  
  bindTomurrey(ONEOF8WEAKREVERSE);

}

bool weekdays()
{
return  ((int) TimeDayOfWeek(TimeLocal())== 1 || (int) TimeDayOfWeek(TimeLocal())== 2 || (int) TimeDayOfWeek(TimeLocal())== 3  || (int) TimeDayOfWeek(TimeLocal())== 4 || (int) TimeDayOfWeek(TimeLocal())== 5);         
}

bool TradeAllowedforThisDay()
{

   
    if ( (int) TimeDayOfWeek(TimeLocal())== 1 && MondayTrade == true)
  {
  return true;
  }
  
      if ( (int) TimeDayOfWeek(TimeLocal())== 2 && TuesdayTrade == true)
  {
  return true;
  }
  
      if ( (int) TimeDayOfWeek(TimeLocal())== 3 && WednesdayTrade == true)
  {
  return true;
  }
  
      if ( (int) TimeDayOfWeek(TimeLocal())== 4 && ThursdayTrade == true)
  {
  return true;
  }
  
      if ( (int) TimeDayOfWeek(TimeLocal())== 5 && FridayTrade == true)
  {
  return true;
  }
  
  return false;

}

    bool  isspreadok()
{RefreshRates();
      if (Digits ==5)
      {
        int ext = max_spread;
        if ( StringLen((string) ext)==1)
        {
          return  ( (Ask- Bid)   < (double)((string)0.0000 + (string) max_spread   ) );
        }
        else if ( StringLen((string) ext)==2)
        {
          return  ( (Ask- Bid)   < (double)((string)0.000 + (string) max_spread   ) );
        }
        else if ( StringLen((string) ext)==3)
        {
           return  ( (Ask- Bid)   < (double)((string)0.00 + (string) max_spread   ) );
        }
        else
        {return false;}
    
      }
      
      if (Digits ==4)
      {
  int ext = max_spread;
        if ( StringLen((string) ext)==1)
        {
          return  ( (Ask- Bid)   < (double)((string)0.000 + (string) max_spread   ) );
        }
        else if ( StringLen((string) ext)==2)
        {
          return  ( (Ask- Bid)   < (double)((string)0.00 + (string) max_spread   ) );
        }
        else if ( StringLen((string) ext)==3)
        {
           return  ( (Ask- Bid)   < (double)((string)0.0 + (string) max_spread   ) );
        }
        else
        {return false;}
      }
      
      return false;
}

void showhideCurrentLevel()
{
if (showmurreygraph)
{
    ObjectDelete("INFO");
    ObjectCreate( "INFO", OBJ_LABEL,0,0,0,0,0,0);
    ObjectSet(    "INFO", OBJPROP_CORNER,3);
    ObjectSet(    "INFO", OBJPROP_XDISTANCE, 3);
    ObjectSet(    "INFO", OBJPROP_YDISTANCE, 2);
    ObjectSetText("INFO", (string)returnBind(),12,"Impact",Black);
}
   
}
string returnBind()
{RefreshRates();
  string str = "";

  if( Bid> bindTomurrey(MINUS2OF8) )
  str = "Bid greater than Minus 2 of 8 level";
  
    if( Bid> bindTomurrey(MINUS1OF8) )
  str = "Bid greater than Minus 1 of 8 level";
  
      if( Bid> bindTomurrey(ZEROOF8ULTIMATESUPPORT) )
  str = "Bid greater than Ultimate Support";

      if( Bid> bindTomurrey(ONEOF8WEAKREVERSE) )
  str = "Bid greater than Weak Reverse";
  
      if( Bid> bindTomurrey(TWOOF8STRONGREVERSE) )
  str = "Bid greater than Strong Reverse"; 
  
        if( Bid> bindTomurrey(THREEOF8LOWERRANGE) )
  str = "Bid greater than Lower Range";  
  
          if( Bid> bindTomurrey(FOUROF8PIVOTALPOINT) )
  str = "Bid greater than Pivotal Point";
  
   if( Bid> bindTomurrey(FIVEOF8UPPERRANG) )
  str = "Bid greater than Upper Rang";  
  
     if( Bid> bindTomurrey(SIXOF8STRONGREVERSE) )
  str = "Bid greater than Strong Reverse"; 
  
       if( Bid> bindTomurrey(SEVENOF8WEAKREVERSE) )
  str = "Bid greater than Weak Reverse";   
  
       if( Bid> bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE) )
  str = "Bid greater than Ultimate Resistance"; 
  
       if( Bid> bindTomurrey(PLUS1OF8) )
  str = "Bid greater than +1/8";
  
   if( Bid> bindTomurrey(PlUS2OF8) )
  str = "Bid greater than +2/8";    
  
  return str;
}

void appendMode()
{
if (donottrade == false)
                 {
                   mode_ = true;
                 }
                 else{mode_ = false;}
}

void trytoGoLong()
{RefreshRates();

    if(getGreenStock() <=21 && getRedStock() < getGreenStock()  )
    {
       if (  Close[0] < bindTomurrey(ZEROOF8ULTIMATESUPPORT) ||  Close[1] < bindTomurrey(ZEROOF8ULTIMATESUPPORT) )
       {
          cangolong = true; cangoshort = false;
       }
    }

}
void trytoGoShort()
{RefreshRates();

    if(getGreenStock() >=79 && getRedStock() < getGreenStock()  )
    {
     if (  Close[0] > bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE)  ||  Close[1] > bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE))
       {
              cangolong = false; cangoshort = true;
       }
       
    }

}

double getGreenStock()
{
  RefreshRates();
       double kline = 0;
       double dline=0;
       kline  = iStochastic(Symbol(), Period(), 5, 3, 3, MODE_SMA, 0, MODE_MAIN, 0);  //old  5   3   3
       dline  = iStochastic(Symbol(), Period(), 5, 3, 3, MODE_SMA, 0, MODE_SIGNAL, 0); 

return  kline;
}

double getRedStock()
{
  RefreshRates();
      double kline = 0;
      double dline=0;
      kline  = iStochastic(Symbol(), Period(), 5, 3, 3, MODE_SMA, 0, MODE_MAIN, 0);
      dline  = iStochastic(Symbol(), Period(), 5, 3, 3, MODE_SMA, 0, MODE_SIGNAL, 0); 
      return  dline;
}

bool CandlesOkforBuy()
{
int cphigher=0; //lookback
int cplower=0;
int atleastonecloseabove=0;
bool toreturn = false;
for(int x=1; x<lookback; x++)
      {
      //Open[x]<Close[x] High[x] Low[x]
        if ( Open[x] < bindTomurrey(ZEROOF8ULTIMATESUPPORT)    /* && Close[x] <  bindTomurrey(ZEROOF8ULTIMATESUPPORT) */ )
        {
          cplower++;
        }
        
        
          if ( Open[x] > bindTomurrey(ZEROOF8ULTIMATESUPPORT)    /*&& Close[x] >  bindTomurrey(ZEROOF8ULTIMATESUPPORT)*/ )
        {
          cphigher++;
        }
        
        if( Close[x] > bindTomurrey(ZEROOF8ULTIMATESUPPORT) )
        {
          atleastonecloseabove++;
        }
        
        if ( (cphigher >1 && cphigher<5)   && (cplower>7 && cplower <lookback) && atleastonecloseabove>=1)
        {
        toreturn = true;
        }
        
      }
return toreturn;
}

bool CandlesOkforSell()
{
int cphigher=0; //lookback
int cplower=0;
int atleastonecloseabove=0;
bool toreturn = false;
for(int x=1; x<lookback; x++)
      {
      //Open[x]<Close[x] High[x] Low[x]
        if ( Open[x] < bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE)    /* && Close[x] <  bindTomurrey(ZEROOF8ULTIMATESUPPORT) */ )
        {
             cphigher++;
        }
        
        
          if ( Open[x] > bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE)    /*&& Close[x] >  bindTomurrey(ZEROOF8ULTIMATESUPPORT)*/ )
        {
             cplower++; 
        }
        
        if( Close[x] < bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE) )
        {
          atleastonecloseabove++;
        }
        
        if ( (cphigher >1 && cphigher<5)   && (cplower>7 && cplower <lookback) && atleastonecloseabove>=1)
        {
        toreturn = true;
        }
        
      }
return toreturn;}




bool noorders()
   {
   
     RefreshRates();
int total = OrdersTotal();

for(int i=0; i<total-1;i++)
{ 

int s = OrderSelect(i, SELECT_BY_POS);
int mn   = OrderMagicNumber();
int ticket=-1;
double ordertp=0;
bool result =  false;

int result2=0;


   if (mn== magicnumber  )
   {
     if (OrderSymbol() == Symbol())
     {
      return false;
     }
   
   }
   
   
    
     
   } return true;}
   
   
   
      double calculateit()
   {
       if (autocalculatelots == false){
       
         if (lot_size < MarketInfo(Symbol(),MODE_MINLOT))
         {
             return (double)MarketInfo(Symbol(),MODE_MINLOT);
         }
         else
         {
         return (double)lot_size;
         }
        
        
        }
       else
       {
                 if (Digits ==4)
                 
                 {
                     
         if ( AccountBalance()>0 && AccountBalance() <=100)
         return 0.10;
     
         if ( AccountBalance()>100 && AccountBalance() <=500)
        return 0.10;
         if ( AccountBalance()>500 && AccountBalance() <=1000)
          return 0.10;
         if ( AccountBalance()>1000 && AccountBalance() <=5000)
          return 0.20;
         if ( AccountBalance()>5000 && AccountBalance() <=10000)
       return 0.20;
         if ( AccountBalance()>10000 && AccountBalance() <=20000)
          return 0.30;
         if ( AccountBalance()>20000 && AccountBalance() <=50000)
        return 0.30;
         if ( AccountBalance()>50000 && AccountBalance() <=100000)
          return 0.30;
         if ( AccountBalance()>100000 && AccountBalance() <=500000)
          return 0.40;
           if ( AccountBalance()>500000 )
          return 0.50;
                 
                 }
                 
                 if (Digits ==5)
                 {
                     
         if ( AccountBalance()>0 && AccountBalance() <=100)
         return 0.01;
     
         if ( AccountBalance()>100 && AccountBalance() <=500)
         return 0.01;
         if ( AccountBalance()>500 && AccountBalance() <=1000)
         return 0.02;
         if ( AccountBalance()>1000 && AccountBalance() <=5000)
         return 0.03;
         if ( AccountBalance()>5000 && AccountBalance() <=10000)
        return 0.05;
         if ( AccountBalance()>10000 && AccountBalance() <=20000)
         return 0.07;
         if ( AccountBalance()>20000 && AccountBalance() <=50000)
         return 0.07;
         if ( AccountBalance()>50000 && AccountBalance() <=100000)
         return 0.08;
         if ( AccountBalance()>100000 && AccountBalance() <=500000)
         return 0.10;
           if ( AccountBalance()>500000 )
         return 0.11;
                 }
          
       }
   return (double)MarketInfo(Symbol(),MODE_MINLOT);
   }
   
   
   void SurviveOrdersstrategy1()
{  

//pips2dbl
   
int ticket1 =-1;

     if (Digits == 5 || Digits == 3)
    {    // Adjust for five (5) digit brokers.
                pips2dbl    = Point*10; pips2points = 10;   Digitspips = 1;
    } 
    else {    pips2dbl    = Point;    pips2points =  1;   Digitspips = 0; }
//end pips2dbl
   string mode= "";
   RefreshRates();
int total = OrdersTotal();

for(int i=total-1;i>=0;i--)
{
// orderstp 7


int s = OrderSelect(i, SELECT_BY_POS);
int type   = OrderType();
int ticket=-1;
double ordertp=0;
bool result =  false;

int result2=0;


  switch(type)
   {
     //Close opened long positions
     case OP_BUY       :
     
      {
        
        ticket= OrderTicket();
        mode = "buy";
        ordertp = OrderTakeProfit();
     
    //     Alert( (string)ticket + "profit : " + (string)ordertp);
       break;
      }
                        

     //Close opened short positions
     case OP_SELL      :
     {
       ticket= OrderTicket();
        mode = "sell";
        ordertp = OrderTakeProfit();
          //     Alert( (string)ticket + "profit : " + (string)ordertp);

           
     break;
     }
     
   //   result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );

   }
   // append order while straight to take profit
   if (mode == "buy")
   {
     RefreshRates();
     if ( (Ask + 0.00001  == OrderTakeProfit()) || (Ask + 0.00002  == OrderTakeProfit())  || (Ask + 0.00003  == OrderTakeProfit())  )
     {
     
     if( (OrderMagicNumber() == magicnumber) && ( OrderSymbol() == Symbol() ) )
     {
      

       //
                                                     if ( Ask < bindTomurrey(PlUS2OF8) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl ,  Bid+secondtakeprofit* pips2dbl,0,White); // stoploss
       }
              
                                               if ( Ask < bindTomurrey(PLUS1OF8) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(PlUS2OF8),0,White); // stoploss
       }
                                               if ( Ask < bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(PLUS1OF8),0,White); // stoploss
       }
                                    if ( Ask < bindTomurrey(SEVENOF8WEAKREVERSE) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(EIGHTOFEIGHTULTIMATERESISTANE),0,White); // stoploss
       }
                                    if ( Ask < bindTomurrey(SIXOF8STRONGREVERSE) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(SEVENOF8WEAKREVERSE),0,White); // stoploss
       }
       
                                   if ( Ask < bindTomurrey(FIVEOF8UPPERRANG) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(SIXOF8STRONGREVERSE),0,White); // stoploss
       }
       
                                 if ( Ask < bindTomurrey(FOUROF8PIVOTALPOINT) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(FIVEOF8UPPERRANG),0,White); // stoploss
       }
       
                          if ( Ask < bindTomurrey(THREEOF8LOWERRANGE) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(FOUROF8PIVOTALPOINT),0,White); // stoploss
       }
                   if ( Ask < bindTomurrey(TWOOF8STRONGREVERSE) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(THREEOF8LOWERRANGE),0,White); // stoploss
       }
        if ( Ask < bindTomurrey(ONEOF8WEAKREVERSE) )
       
       {
            int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , bindTomurrey(TWOOF8STRONGREVERSE),0,White); // stoploss
       }
       
     }
       
      
   
     
     }
       
  
   }
   
   if (mode == "sell")
   {
   RefreshRates();
   if((Bid - 0.00001 == OrderTakeProfit()) || (Bid - 0.00002 == OrderTakeProfit()) || (Bid - 0.00003 == OrderTakeProfit()) )
   {
   
    if((OrderMagicNumber() == magicnumber) && ( OrderSymbol() == Symbol() ))
     {
      
     
                                  if ( Bid > bindTomurrey(MINUS2OF8) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , Ask-secondtakeprofit* pips2dbl,0,White); // stoploss
    
     }
     
                                   if ( Bid > bindTomurrey(MINUS1OF8) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(MINUS2OF8),0,White); // stoploss
     }
                                if ( Bid > bindTomurrey(ONEOF8WEAKREVERSE) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(MINUS1OF8),0,White); // stoploss
     }
                         if ( Bid > bindTomurrey(TWOOF8STRONGREVERSE) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(ONEOF8WEAKREVERSE),0,White); // stoploss
     }
                      if ( Bid > bindTomurrey(THREEOF8LOWERRANGE) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(TWOOF8STRONGREVERSE),0,White); // stoploss
     }
                   if ( Bid > bindTomurrey(FOUROF8PIVOTALPOINT) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(THREEOF8LOWERRANGE),0,White); // stoploss
     }
                if ( Bid > bindTomurrey(FIVEOF8UPPERRANG) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(FOUROF8PIVOTALPOINT),0,White); // stoploss
     }
             if ( Bid > bindTomurrey(SIXOF8STRONGREVERSE) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(FIVEOF8UPPERRANG),0,White); // stoploss
     }
     if ( Bid > bindTomurrey(SEVENOF8WEAKREVERSE) )
     {
      int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , bindTomurrey(SIXOF8STRONGREVERSE),0,White); // stoploss
     }
        }
        
        

   }
          
   }
   
}
}

bool no130error(string a)
{
    if (a=="buy")
    
    { 
     return (getMiddleIband() - getLowerIband() >  0.00080);  
    }
    
    
    
    if (a=="sell")
    
    {
      
     return (getHigherIband() - getMiddleIband() > 0.00080);
       
    }
    
    
   return false;
}

 
   
    
double    getHigherIband()
{         RefreshRates();
        return iBands(NULL,0,bbperiod,bbdeviation,0,PRICE_CLOSE,MODE_LOW,0);
}

double    getLowerIband()
{         RefreshRates();
        return iBands(NULL,0,bbperiod,bbdeviation,0,PRICE_CLOSE,MODE_HIGH,0);
}


double    getMiddleIband()
{         RefreshRates();
        return iBands(NULL,0,bbperiod,bbdeviation,0,PRICE_CLOSE,MODE_MAIN,0);
}