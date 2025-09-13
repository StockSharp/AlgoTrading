//+------------------------------------------------------------------+
//|                             Copyright © 2014, Tradingfloor Kenya |
//|                                              nathan@tfxkenya.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2014 Tradingfloor Kenya"
#property link      "nathan@tfxkenya.com"
#property version   "1.04"
#property strict

int acc=123;
int Time_com;
extern int Number_Of_Position_To_Close_Them_All_with_profit=3;
extern double Profit_For_Close_all=1;


extern int Max_Number_pos_in_same_time=4;


extern bool Give_alert=false;


 bool Allow_Trade_before_News=false;


extern bool Close_only_when_position_have_a_profit=true;
extern bool Close_position_with_oposite_signal=true;

 double Multiply_Lotsize=2,Max_Lotsize_Digit=1;
 double max_lot_size=2;

extern int magic_number=120;


 string t3="balance filters:";
 double max_loss_account_balance=.9;
 bool   EA_Restart=false;
 double Max_Margin=0.9;


extern string t1_1="account filters:";
extern int Slippage_pip=100;


 extern bool time_filters=false;
 extern int Hour_start=3,Min_start,Hour_end=15,Min_end;



 string t4="news filters:";
 bool News_filter=false;
 int News_Hour=3,News_Min=0;
 int Stop_Trade_Until_hour=2,Stop_Trade_Until_min;
bool News_Filter2=false;


double lot_last;
extern string t5="lotsize filters:";
extern double lotsize=0.1;
 double Increase_lot_size=0.2;



extern bool Money_management=false;
extern double Percent_risk=0.01;
extern int Max_Digit=2;
extern double Min_Lot=0.1;




extern string t7="stop and trail filters:";
extern int Stoploss_pip=100,Takeprofit_pip=200;


extern  int  TrailingStop=0;
 int Max_TrailingStop=99999;
extern int Trailling_step;
extern int Trailling_Start_from=10;


extern bool Stoploss_zero,Takeprofit_zero;
extern int Max_spread=2000;


 extern bool Break_even=false;
extern  int Decreasing_StopLoss_pip=20,New_Stop_Loss=0;



 int Time_frame=60;



 




int bar_last;
double open_last;
 int Pause_Number_candle=10;


//extern int Hiken_Timeframe1,Hiken_Timeframe2,Hiken_Timeframe3,Hiken_Timeframe4,Macd_Timeframe1;
//extern bool Macd_enable=true;




// had aghal faslele for pos gozare

double lot_pos=0;

 int takeprofit1=100,takeprofit2=100,takeprofit3=100,takeprofit4=100,takeprofit5=100,takeprofit6=100,takeprofit7=100,takeprofit8=100,takeprofit9=100,takeprofit10=100,takeprofit11=100,takeprofit12=100,takeprofit13=100,takeprofit14=100,takeprofit15=100;
 int stoploss1=100,stoploss2=100,stoploss3=100,stoploss4=100,stoploss5=100,stoploss6=100,stoploss7=100,stoploss8=100,stoploss9=100,stoploss10=100,stoploss11=100,stoploss12=100,stoploss13=100,stoploss14=100,stoploss15=100;
 double lot1=0.1,lot2=0.1,lot3=0.1,lot4=0.1,lot5=0.1,lot6=0.1,lot7=0.1,lot8=0.1,lot9=0.1,lot10=0.1,lot11=0.1,lot12=0.1,lot13=0.1,lot14=0.1,lot15=0.1;
//extern double lotsize=0.1;



double pr_last,tp_last;
double stop_last,type_last_pos_live;
int com_last_pos;
double take_last;

int trade=123;
double price = 0;
bool tp_khorde_last;

int time=0;
double max_balance;





//--





 double LotSize=0.1; 



  bool Revers_signal=false;



 bool pivots = true;
 bool camarilla = true;
 bool midpivots = true;
 int MyPeriod = PERIOD_M1;


 bool Close_by_pivot=true;
 int Gap_to_Pivot=3;
 int Gap_to_Pivot_for_entry=20;

int Takeprofit,Stoploss;
int Max_Slippage;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   time=0;
   Takeprofit=Takeprofit_pip;
   Stoploss=Stoploss_pip;
   Max_Slippage=Slippage_pip;
   if(Digits==5 || Digits==3)
    {
     Takeprofit=Takeprofit_pip*10;
     Stoploss=Stoploss_pip*10;
     Max_Slippage=Slippage_pip*10;
    }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
 //if (TimeMonth(Time[0])>7 && TimeYear(Time[0])>=2014) return(0);
 int signal =can_trade();


int tr=Count_order(OP_BUY)+Count_order(OP_SELL);
if (tr>=Number_Of_Position_To_Close_Them_All_with_profit)
 if (sum_profit()>=Profit_For_Close_all)
{
Comment("2333");
    Close_all_order(OP_BUY);
    Close_all_order(OP_SELL);
}

  //  Comment(signal, " " ,Time_com, " " ,Close[0]);
   int Time_hourage=Time_com;
 
if (TrailingStop!=0) DoTrail();
 if (Break_even) Break_Even_On_Pip();

 if (signal==OP_BUY)
 // if (confirm_trade(OP_BUY)==123) 
 //  if (Count_order(OP_BUY)==0&& Take_All_trade ) //&& nazdik_pivot(OP_BUY,Gap_to_Pivot_for_entry)==false
   {// buy
    get_position(OP_BUY,Time_hourage);
   
   }
   
if (signal==OP_SELL)
  //if (confirm_trade(OP_SELL)==123) 
   if (Count_order(OP_SELL)<=Max_Number_pos_in_same_time) // && nazdik_pivot(OP_SELL,Gap_to_Pivot_for_entry)==false
   {// buy
    get_position(OP_SELL,Time_hourage);
   
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+





bool Time_trade()
{

if (time_filters)
 {
   if (Hour_start==TimeHour(Time[0]))
      if (TimeMinute(Time[0])>=Min_start) return(true);
      
  if (Hour_start<TimeHour(Time[0]) && TimeHour(Time[0])<Hour_end) return(true);
  
   if (Hour_end==TimeHour(Time[0]))
      if (TimeMinute(Time[0])<=Min_end) return(true);
         
   return(false);
 }
  return(true);

}

double Lot_Cal()
{
last_pos();
int cnt=Count_order(OP_BUY)+Count_order(OP_SELL);
//double l=NormalizeDouble( lot_last*Multiply_Lotsize,Max_Lotsize_Digit);
//if (l>max_lot_size) l =max_lot_size;
//if (cnt>=1) return(l);
double l=(NormalizeDouble(((AccountBalance()* Percent_risk) )*0.01,Max_Digit));
if (l<Min_Lot)l=Min_Lot;
if (Money_management) return(l);
return(lotsize);




int cnt_pos=Count_order_all();
double lot=LotSize;
for (int i=1;i<=cnt_pos;i++)
{
  lot=lot+(LotSize*Increase_lot_size);
} 
   return(lot);
}


bool close_with_analyzer()
{
int candle= 0;



string first=StringSubstr(Symbol(),0,3);
string second=StringSubstr(Symbol(),3,6);
int firstbuff,secondbuff;
   if(first == "USD") firstbuff = 0; 
   if(first == "CAD") firstbuff = 1; 
   if(first == "GBP") firstbuff = 2; 
   if(first == "EUR") firstbuff = 3; 
   if(first == "CHF") firstbuff = 4; 
   if(first == "JPY") firstbuff = 5; 
   if(first == "AUD") firstbuff = 6; 
   if(first == "NZD") firstbuff = 7; 
   
   if(second == "USD") secondbuff = 0; 
   if(second == "CAD") secondbuff = 1; 
   if(second == "GBP") secondbuff = 2; 
   if(second == "EUR") secondbuff = 3; 
   if(second == "CHF") secondbuff = 4; 
   if(second == "JPY") secondbuff = 5; 
   if(second == "AUD") secondbuff = 6; 
   if(second == "NZD") secondbuff = 7; 
   
   
 double Value1 = iCustom(Symbol(),Period(),"Analyzer-Pro.ex4",firstbuff,candle);
 //double Value2 = iCustom(Symbol(),Period(),"Analyzer-Pro.ex4",secondbuff,candle);
 first=StringConcatenate( first,"p");
 first=StringChangeToLowerCase(first);
 second=second+"p";
 second=StringChangeToLowerCase(second);
 first=ObjectGetString(0,first, OBJPROP_TEXT);
 second=ObjectGetString(0,second, OBJPROP_TEXT);
  Value1=StrToDouble(first);
 double Value2=StrToDouble(second);
 
 //Alert(first, " " ,second);
 
 if (MathAbs(Value1-Value2)<=1 && Value1>0 && Value2>0) 
 {
// Alert(MathAbs(Value1-Value2)," " ,Value1," " ,Value2," " ,firstbuff, " " ,secondbuff);
   Close_all_order(OP_BUY);
   Close_all_order(OP_SELL);
   return(false);
 }
return(true); 
 
}

string StringChangeToLowerCase(string sText) {
  // Example: StringChangeToLowerCase("oNe mAn"); // one man
  int iLen=StringLen(sText), i, iChar;
  for(i=0; i < iLen; i++) {
    iChar=StringGetChar(sText, i);
    if(iChar >= 65 && iChar <= 90) sText=StringSetChar(sText, i, iChar+32);
  }
  return(sText);  
}
 
 
 
int can_trade()
{
//double b=iCustom(Symbol(),0,"SPV_volumes",ADX_bar,0,which_candle_use_for_trade);
double s=iCustom(Symbol(),0,"SPV_volumes",0,1);

int type=123;
Time_com=Time[1];


//if (b>0 && b<9999 && b!=EMPTY_VALUE && b!=EMPTY ) type=OP_BUY;
if (s>0 && s<99999 && s!=EMPTY && s!=EMPTY_VALUE ) type=OP_SELL;



return(type);
}





int get_position(int position,int com)
{

if (Count_order_com(OP_SELL,com)==0 && position==OP_SELL)
             { 
          //    lot=Last_Pos_hist_live(OP_BUY);
           //  if (lot==0) lot=lot1; else lot=lot*Increase_Lot;
           //  com_last_pos++;
          if (Close_position_with_oposite_signal)   Close_all_order(OP_BUY);
          //   if (profit()>0) com_last_pos=1;
            if (Time_trade()==false) {Comment("Not trade allow is not time trade"); return(0);}
          if (Give_alert)  Alert("Sell ",Symbol());
             Open_Sellorder(com,magic_number,Lot_Cal());
             pr_last=Close[0];
             return(0);
             }
if (Count_order_com(OP_BUY,com)==0 && position==OP_BUY)
             { 
          //    lot=Last_Pos_hist_live(OP_BUY);
           //  if (lot==0) lot=lot1; else lot=lot*Increase_Lot;
          //   com_last_pos++;
         if (Close_position_with_oposite_signal)   Close_all_order(OP_SELL);
         //    if (profit()>0) com_last_pos=1;
         if (Time_trade()==false) {Comment("Not trade allow is not time trade"); return(0);}
        if (Give_alert) Alert("Buy ",Symbol());
             Open_Buyorder(com,magic_number,Lot_Cal());
             pr_last=Close[0];
             return(0);
             }
return(0);
}



 int  Trilling_by_powerplay(double UpBuffer,double DownBuffer,int t)
  {
    for (int i = 0; i < OrdersTotal(); i++) {
     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
     if ( OrderSymbol()==Symbol() )  // only look if mygrid and symbol...
        {
          
          if (OrderType() == OP_BUY && t==OP_BUY) {
          double news=UpBuffer;
             //if(Bid-OrderOpenPrice()>Point*TrailingStop && (news-OrderOpenPrice())>=Trailling_Start_from*Point &&  Bid-OrderOpenPrice()<= Max_TrailingStop*Point && MathAbs(OrderStopLoss()-Close[0])>=Trailling_step*Point )
             {
               // if(OrderStopLoss()<Bid-Point*TrailingStop || OrderStopLoss()==0)
                 if (news!=OrderStopLoss() || OrderStopLoss()==0)
                  {
                     OrderModify(OrderTicket(),OrderOpenPrice(),news,OrderTakeProfit(),0,Green);
                     return(0);
                  }
             }
          }

          if (OrderType() == OP_SELL && t==OP_SELL) 
          {
          double news=DownBuffer;
        
           //  if((OrderOpenPrice()-Ask)>(Point*TrailingStop) && (OrderOpenPrice()-news)>=Trailling_Start_from*Point && (OrderOpenPrice()-Ask)<=Max_TrailingStop*Point  && MathAbs(OrderStopLoss()-Close[0])>=Trailling_step*Point)
             {
               // if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                if (news!=OrderStopLoss() || OrderStopLoss()==0)
                {
                   OrderModify(OrderTicket(),OrderOpenPrice(),news,OrderTakeProfit(),0,Red);
                   return(0);
                }
             }
          }
       }
    }
    return(0);
 }
 



 int  DoTrail()
  {
    for (int i = 0; i < OrdersTotal(); i++) {
     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
     if ( OrderSymbol()==Symbol() )  // only look if mygrid and symbol...
        {
          
          if (OrderType() == OP_BUY) {
          double news=Bid-(Point*TrailingStop);
             if(Bid-OrderOpenPrice()>Point*TrailingStop && (news-OrderOpenPrice())>=Trailling_Start_from*Point &&  Bid-OrderOpenPrice()<= Max_TrailingStop*Point && MathAbs(OrderStopLoss()-Close[0])>=Trailling_step*Point )
             {
                if(OrderStopLoss()<Bid-Point*TrailingStop || OrderStopLoss()==0)
                  {
                     OrderModify(OrderTicket(),OrderOpenPrice(),news,OrderTakeProfit(),0,Green);
                     return(0);
                  }
             }
          }

          if (OrderType() == OP_SELL) 
          {
          double news=Ask+Point*TrailingStop;
             if((OrderOpenPrice()-Ask)>(Point*TrailingStop) && (OrderOpenPrice()-news)>=Trailling_Start_from*Point && (OrderOpenPrice()-Ask)<=Max_TrailingStop*Point  && MathAbs(OrderStopLoss()-Close[0])>=Trailling_step*Point)
             {
                if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                {
                   OrderModify(OrderTicket(),OrderOpenPrice(),news,OrderTakeProfit(),0,Red);
                   return(0);
                }
             }
          }
       }
    }
    return(0);
 }
 

 


 
 void Break_Even_On_Pip()
  {
    for (int i = 0; i < OrdersTotal(); i++) {
     bool as=OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
     if ( OrderSymbol()==Symbol() )  // only look if mygrid and symbol___
        {
          
          if (OrderType() == OP_BUY ) {
             if(Bid-OrderOpenPrice()>Point*Decreasing_StopLoss_pip  )
             {
                if(OrderStopLoss()!=(OrderOpenPrice()+(Point*New_Stop_Loss)) && OrderStopLoss()<(OrderOpenPrice()+(Point*New_Stop_Loss)))
                  {
                     as=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+(Point*New_Stop_Loss),OrderTakeProfit(),0,Green);
                  }
             }
          }

          if (OrderType() == OP_SELL ) 
          {
             if((OrderOpenPrice()-Ask)>(Point*Decreasing_StopLoss_pip) )
             {
               if(OrderStopLoss()!=(OrderOpenPrice()-(Point*New_Stop_Loss)) && OrderStopLoss()>(OrderOpenPrice()-(Point*New_Stop_Loss)))
                {
                   as=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-(Point*New_Stop_Loss),OrderTakeProfit(),0,Red);
                }
             }
          }
       }
    }
 }
 
 
 
double takeprofit_size(int i)
{
return(Takeprofit);

return(0);
}  



double stoploss_size(int i)
{
return(Stoploss);

return(0);
}  

int Close_all_order(int t)
{
bool tic=false;
 for(int cnt=OrdersTotal();cnt>=0;cnt--)   
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
	  if (OrderSymbol()==Symbol() && OrderMagicNumber()==magic_number)
	  {				
	  	  tic=false;
	  	  if (OrderType()==t) 
	  	  {
	  	     if (t==OP_BUYLIMIT || t==OP_SELLLIMIT || t==OP_BUYSTOP || t==OP_SELLSTOP) tic=OrderDelete(OrderTicket());
	  	       else {
	  	           if (Close_only_when_position_have_a_profit && OrderProfit()<0) return(0); 
	   
	  	           tic=OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),5,Green);
	  	           if (tic==false) tic=OrderClose(OrderTicket(),OrderLots(),Bid,5,Green);
	  	           if (tic==false) tic=OrderClose(OrderTicket(),OrderLots(),Ask,5,Green);
	  	  
	  	           }
	  	  }
	  	  //}
	  }
   }    
   return(0); 
}


double last_pos()
{
int bar = 99999;
 int cnt_trade=0;
 double op=0;
 bool tp=true;
 for (int i = 0; i <OrdersHistoryTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
     {
     if ( bar >=iBarShift(Symbol(),Period(),OrderOpenTime(),true) && iBarShift(Symbol(),Period(),OrderOpenTime(),true)>=0 && OrderMagicNumber()==magic_number && OrderSymbol()==Symbol() )  
               {  
                tp=false;
                bar =iBarShift(Symbol(),Period(),OrderOpenTime(),true);
                op=OrderOpenPrice();
                if (OrderProfit()>0) { tp=true; }else tp=false;
                stop_last=OrderStopLoss();
                take_last=OrderTakeProfit();
                type_last_pos_live=OrderType();
                lot_last=OrderLots();
                com_last_pos=StrToInteger(OrderComment());
               }
     }      
 }       


 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     {
     if ( bar >=iBarShift(Symbol(),Period(),OrderOpenTime(),true) && iBarShift(Symbol(),Period(),OrderOpenTime(),true)>=0 && OrderMagicNumber()==magic_number && OrderSymbol()==Symbol() )  
               {  
                tp=false;
                bar =iBarShift(Symbol(),Period(),OrderOpenTime(),true);
                op=OrderOpenPrice();
                if (OrderProfit()>0) { tp=true; } else tp=false;
                stop_last=OrderStopLoss();
                take_last=OrderTakeProfit();
                lot_last=OrderLots();
                 type_last_pos_live=OrderType();
                 com_last_pos=StrToInteger(OrderComment());
               }
     }      
 }       
bar_last=bar;
open_last=op;
tp_khorde_last=tp;
return(op);
}



int Count_order_all_curency(int tt)
{int cnt=0;
lot_pos=0;
 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     if (OrderSymbol()==Symbol() && OrderType()==tt ) 
          {cnt++;}
 }        
return(cnt);
}



int Count_order(int tt)
{int cnt=0;
lot_pos=0;
 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     if (OrderSymbol()==Symbol() && OrderType()==tt && OrderMagicNumber()==magic_number) 
          {cnt++;lot_pos=OrderLots();}
 }        
return(cnt);
}


double  sum_profit()
{int cnt=0;
lot_pos=0;
double pr=0;
 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     if (OrderSymbol()==Symbol()  && OrderMagicNumber()==magic_number) 
          {pr=pr+OrderProfit();}
 }        
return(pr);
}



int Count_order_all()
{int cnt=0;
lot_pos=0;
int last=0;
 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     {
     if (last==0) last=OrderOpenTime();
     
     if (OrderSymbol()==Symbol() && OrderMagicNumber()==magic_number) 
          {
          cnt++;
          if (last<=OrderOpenTime()) 
           {
           last=OrderOpenTime();
           lot_pos=OrderLots();
           }
          
          }
    }      
          
 }        
return(cnt);
}

/*
double Count_profit()
{double profit=0;
 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     {
     if (OrderSymbol()==Symbol() && OrderMagicNumber()==magic_number) 
          {
         //   profit=profit+OrderProfit();
           if (OrderType()==OP_BUY) profit=profit+(OrderClosePrice()-OrderOpenPrice());
           if (OrderType()==OP_SELL) profit=profit+(OrderOpenPrice()-OrderClosePrice());
           
          }
    }      
          
 }        
return(profit);
}*/



double Count_profit()
{double profit=0;
 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     {
     if (OrderSymbol()==Symbol() && OrderMagicNumber()==magic_number) 
          {
         //   profit=profit+OrderProfit();
           
          profit=profit+(OrderProfit());
           
          }
    }      
          
 }        
return(profit);
}



int Count_order_com(int tt,int com)
{int cnt=0;
lot_pos=0;
 for (int i = 0; i <OrdersTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
     if (OrderSymbol()==Symbol() && OrderType()==tt && OrderMagicNumber()==magic_number) 
     if (StrToInteger(OrderComment())==com)
          {cnt++;lot_pos=OrderLots();}
 }  
 
 for (int i = 0; i <OrdersHistoryTotal(); i++) 
 {
     if (OrderSelect(i, SELECT_BY_POS, MODE_HISTORY))
     if (OrderSymbol()==Symbol() && OrderType()==tt && OrderMagicNumber()==magic_number) 
     if (StrToInteger(OrderComment())==com)
          {cnt++;lot_pos=OrderLots();}
 }        
return(cnt);
}


int Open_Sellorder(double num,int magic,double Lots)
 {
 int lasterror;
    int ticket=0;
    double entry=Bid;//NormalizeDouble(Open[0]-(Gap_sell*Point),Digits);
 
    ////////
  /*  double low=iLow(Symbol(),1440,1);
    double tp_pro=low+(Protect_space_for_tp*Point);
    if (Close[0]>tp) 
      if (tp < tp_pro) tp=tp_pro;*/
    /////////////  
    int spread=    MarketInfo(Symbol(),MODE_SPREAD);
    if (spread>Max_spread) {Comment("Max spread"); return(0);}
   double st=NormalizeDouble(entry+(stoploss_size(num)*Point),Digits);
    double tp=NormalizeDouble(entry-(Takeprofit*Point),Digits);
   
  // double tp=min_down+Gap_To_Takeprofit_line*Point;//NormalizeDouble(entry-(takeprofit_size(num)*Point),Digits);
  // if (min_down==90000) tp=entry-(MathAbs(entry-st)*2);
    
  if (Stoploss_zero) {st=0; }
 if (Takeprofit_zero) tp =0; 
 //Print("Take sell with ",accept_s, " " ,accept_b, " :: " ,ind1, " " ,ind2, " " ,ind3," " ,ind4, " :::3:: " ,ind3_1," " ,ind3_2," " ,ind3_3," " ,ind3_4," " ,ind3_5," " ,ind3_6," " ,ind3_7);
      ticket = OrderSend(Symbol(),OP_SELL,Lots,NormalizeDouble(entry,Digits),Max_Slippage,st,tp,DoubleToStr(num,Digits),magic,0,Red);              
         lasterror=GetLastError();
        // Print(num,"  ",Lots);
         if (ticket<0) Print("2Ticket : ",ticket,"  Error :",lasterror,"   ",Symbol()," sell   Lots: ",Lots,"   ask: ",entry,"   st: ",st,"   Tp: ",tp,"  num: ",DoubleToStr(num,0));
         Sleep(100);
         
return(0);
 }


int Open_Buyorder(double num,int magic,double Lots)
{
int lasterror=0;
    int ticket=0;
    double entry = Ask;//NormalizeDouble(Open[0]+(Gap_buy*Point),Digits);
    
     ////////
   /* double high=iLow(Symbol(),1440,1);
    double tp_pro=high-(Protect_space_for_tp*Point);
    if (Close[0]<tp) 
      if (tp > tp_pro) tp=tp_pro;*/
    /////////////  
   double st=NormalizeDouble(entry-(stoploss_size(num)*Point),Digits);
   //double tp=min_up-Gap_To_Takeprofit_line*Point;//NormalizeDouble(entry+(takeprofit_size(num)*Point),Digits);
  //  if (min_up==90000) tp=entry+(MathAbs(entry-st)*2);
  double tp=NormalizeDouble(entry+(Takeprofit*Point),Digits);
  
    int spread=    MarketInfo(Symbol(),MODE_SPREAD);
      if (spread>Max_spread) {Comment("Max spread"); return(0);}
    
    
   // if (st_tp_sefr) {st=0; tp =0; }
  if (Stoploss_zero) {st=0; }
 if (Takeprofit_zero) tp =0; 
 //Print("Take buy with ",accept_s, " " ,accept_b, " :: " ,ind1, " " ,ind2, " " ,ind3," " ,ind4, " :::3:: " ,ind3_1," " ,ind3_2," " ,ind3_3," " ,ind3_4," " ,ind3_5," " ,ind3_6," " ,ind3_7);
      ticket = OrderSend(Symbol(),OP_BUY,Lots,NormalizeDouble(entry,Digits),Max_Slippage,st,tp,DoubleToStr(num,Digits),magic,0,Blue);              
         lasterror=GetLastError();
        // Print(num,"  ",Lots);
         if (ticket<0) Print("2Ticket : ",ticket,"  Error :",lasterror,"   ",Symbol()," Buy   Lots: ",Lots,"   ask: ",entry,"   st: ",st,"   Tp: ",tp,"  num: ",DoubleToStr(num,0));
         Sleep(100);
    return(0);   
}

int Open_Sell(double num,int magic,double entry,double Lots,double stop)
 {
 int lasterror;
    int ticket=0;
   // double entry=Bid;//NormalizeDouble(Open[0]-(Gap_sell*Point),Digits);
    double tp=NormalizeDouble(entry-(takeprofit_size(num)*Point),Digits);
    double st=NormalizeDouble(entry+(stoploss_size(num)*Point),Digits);
  // double st=stop;
   // if (st_tp_sefr) {st=0; tp =0; }
    
   int spread=    MarketInfo(Symbol(),MODE_SPREAD);
    if (spread>Max_spread) {Comment("Max spread"); return(0);}
    
      ticket = OrderSend(Symbol(),OP_SELLSTOP,Lots,NormalizeDouble(entry,Digits),Max_Slippage,st,tp,DoubleToStr(num,Digits),magic,0,Red);              
         lasterror=GetLastError();
        // Print(num,"  ",Lots);
         if (ticket<0) Print("2Ticket : ",ticket,"  Error :",lasterror,"   ",Symbol()," sell   Lots: ",Lots,"   ask: ",entry,"   st: ",st,"   Tp: ",tp,"  num: ",DoubleToStr(num,0));
         Sleep(100);
         

 return(0);}


int Open_Buy(double num,int magic,double entry,double Lots,double stop)
{
int lasterror=0;
    int ticket=0;
   // double entry = Ask;//NormalizeDouble(Open[0]+(Gap_buy*Point),Digits);
    double tp=NormalizeDouble(entry+(takeprofit_size(num)*Point),Digits);
    double st=NormalizeDouble(entry-(stoploss_size(num)*Point),Digits);
  // double st=stop;
    //if (st_tp_sefr) {st=0; tp =0; }
 
   int spread=    MarketInfo(Symbol(),MODE_SPREAD);
    if (spread>Max_spread) {Comment("Max spread"); return(0);}
    
      ticket = OrderSend(Symbol(),OP_BUYSTOP,Lots,NormalizeDouble(entry,Digits),Max_Slippage,st,tp,DoubleToStr(num,Digits),magic,0,Blue);              
         lasterror=GetLastError();
        // Print(num,"  ",Lots);
         if (ticket<0) Print("2Ticket : ",ticket,"  Error :",lasterror,"   ",Symbol()," Buy   Lots: ",Lots,"   ask: ",entry,"   st: ",st,"   Tp: ",tp,"  num: ",DoubleToStr(num,0));
         Sleep(100);
      
 return(0); 
}