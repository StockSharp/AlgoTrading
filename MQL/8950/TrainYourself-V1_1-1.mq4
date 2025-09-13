#property copyright "Louis Stoltz lightsites@gmail.com"
#property link      "http://www.lightsites.co.za"

double Lots = 5;
extern int    TakeProfit = 100;
extern int    StopLoss = 100;
extern bool   TrendTrade = True;

int glbOrderType;
int gblClose = 0;
int gblW = 0;
int sunset = 1;
int glbOrderTicket;
double glbOrderProfit; 
double glbOrderOpen; 
double glbOrderStop;
int glbline1=0;
int glbline2=0;
int glbclose1=0;
int glbclose2=0;

datetime cnextRun1,cnextRun2;
double up1,up2,up3,dwn1,dwn2,dwn3;
int a,b,c,d;
int deviation = 30;
int trailing = 30;
int maxorders = 2;

int i,q,b2,pips,pips1,pips2;
int bars = 147;

 int gblx1 = 0;
 int gblx2 = 0;

 int risk=5000;
 int zzd=1;
 datetime cnextRun;

 double rec1,rec2,tribuy,trisell,rec1b,rec2b,tribuyb,trisellb,baskety,basketx;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   if(ObjectFind("BASKET") != -1) ObjectDelete("BASKET"); 
   ObjectCreate   ("BASKET", OBJ_LABEL, 0, 0, 0);  
   ObjectSetText  ("BASKET","1",100, "Webdings", Yellow);
   ObjectSet      ("BASKET", OBJPROP_CORNER,0);
   ObjectSet      ("BASKET", OBJPROP_XDISTANCE,310);
   ObjectSet      ("BASKET", OBJPROP_YDISTANCE,70);

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


int start()
  {
   if(TakeProfit>0)TakeProfit();
   if(StopLoss>0)StopLoss();  
   


   double ask = MarketInfo(Symbol(),MODE_ASK);  
   double bid = MarketInfo(Symbol(),MODE_BID);  
   double point = MarketInfo(Symbol(),MODE_POINT);


    basketx       = ObjectGet("BASKET", OBJPROP_XDISTANCE);  
    baskety       = ObjectGet("BASKET", OBJPROP_YDISTANCE);        



if( cnextRun2 < TimeCurrent() || ObjectFind("CREATE_TRENDLINES") == -1 || ( rec1>(basketx+20)&&rec1<(basketx+113)&&rec1b>(baskety+28)&&rec1b<(baskety+118) ) || ( rec2>(basketx+20)&&rec2<(basketx+113)&&rec2b>(baskety+28)&&rec2b<(baskety+118) ) || ( tribuy>(basketx+20)&&tribuy<(basketx+113)&&tribuyb>(baskety+28)&&tribuyb<(baskety+118) ) || ( trisell>(basketx+20)&&trisell<(basketx+113)&&trisellb>(baskety+28)&&trisellb<(baskety+118) )){ 
cnextRun2 = TimeCurrent() + ((Period()*60)*70); 
   

 if(ObjectFind("CREATE_TRENDLINES") != -1) ObjectDelete("CREATE_TRENDLINES"); 
 ObjectCreate   ("CREATE_TRENDLINES", OBJ_LABEL, 0, 0, 0);  
 ObjectSetText  ("CREATE_TRENDLINES","g",40, "Webdings", Blue);
 ObjectSet      ("CREATE_TRENDLINES", OBJPROP_CORNER,0);
 ObjectSet      ("CREATE_TRENDLINES", OBJPROP_XDISTANCE,300);
 ObjectSet      ("CREATE_TRENDLINES", OBJPROP_YDISTANCE,0);

 if(ObjectFind("CLOSE_ORDER") != -1) ObjectDelete("CLOSE_ORDER"); 
 ObjectCreate   ("CLOSE_ORDER", OBJ_LABEL, 0, 0, 0);  
 ObjectSetText  ("CLOSE_ORDER","g",40, "Webdings", Red);
 ObjectSet      ("CLOSE_ORDER", OBJPROP_CORNER,0);
 ObjectSet      ("CLOSE_ORDER", OBJPROP_XDISTANCE,370);
 ObjectSet      ("CLOSE_ORDER", OBJPROP_YDISTANCE,0);


 if(ObjectFind("BUY_TRIANGLE") != -1) ObjectDelete("BUY_TRIANGLE"); 
 ObjectCreate   ("BUY_TRIANGLE", OBJ_LABEL, 0, 0, 0);  
 ObjectSetText  ("BUY_TRIANGLE","5",40, "Webdings", Orange);
 ObjectSet      ("BUY_TRIANGLE", OBJPROP_CORNER,0);
 ObjectSet      ("BUY_TRIANGLE", OBJPROP_XDISTANCE,430);
 ObjectSet      ("BUY_TRIANGLE", OBJPROP_YDISTANCE,0);


 if(ObjectFind("SELL_TRIANGLE") != -1) ObjectDelete("SELL_TRIANGLE"); 
 ObjectCreate   ("SELL_TRIANGLE", OBJ_LABEL, 0, 0, 0);  
 ObjectSetText  ("SELL_TRIANGLE","6",40, "Webdings", Orange);
 ObjectSet      ("SELL_TRIANGLE", OBJPROP_CORNER,0);
 ObjectSet      ("SELL_TRIANGLE", OBJPROP_XDISTANCE,490);
 ObjectSet      ("SELL_TRIANGLE", OBJPROP_YDISTANCE,0);
 
}
   
   rec1       = ObjectGet("CREATE_TRENDLINES", OBJPROP_XDISTANCE);
   rec2       = ObjectGet("CLOSE_ORDER", OBJPROP_XDISTANCE);
   tribuy     = ObjectGet("BUY_TRIANGLE", OBJPROP_XDISTANCE);     
   trisell    = ObjectGet("SELL_TRIANGLE", OBJPROP_XDISTANCE);   
   
   rec1b       = ObjectGet("CREATE_TRENDLINES", OBJPROP_YDISTANCE);
   rec2b       = ObjectGet("CLOSE_ORDER", OBJPROP_YDISTANCE);
   tribuyb     = ObjectGet("BUY_TRIANGLE", OBJPROP_YDISTANCE);     
   trisellb    = ObjectGet("SELL_TRIANGLE", OBJPROP_YDISTANCE);  
     

   if( cnextRun < TimeCurrent() && rec1>(basketx+20)&&rec1<(basketx+113)&&rec1b>(baskety+28)&&rec1b<(baskety+118) ){
   cnextRun = TimeCurrent() + 90; 
   SetTrendLines(Symbol(),Period(),7);
   }

   
        
      double bM = NormalizeDouble(ObjectGetValueByShift("topline",0),Digits);  
      double sM = NormalizeDouble(ObjectGetValueByShift("botline",0),Digits); 



   if( OrderFind(11)==false && OrderFind(21)==false && ( iClose(Symbol(),Period(),0) < bM-(2*point) 
   && iClose(Symbol(),Period(),0) > sM+(2*point) 
   && q==0) ){
   q=1;
   }
   
      if(OrderFind(331) == false && ( trisell>(basketx+20)&&trisell<(basketx+113)&&trisellb>(baskety+28)&&trisellb<(baskety+118) ) ){     
       OrderSend(Symbol(), OP_SELL, Lots, Bid, 3, 0, 0, "Sell",331, 0, Blue);
      }
      if(OrderFind(441) == false && ( tribuy>(basketx+20)&&tribuy<(basketx+113)&&tribuyb>(baskety+28)&&tribuyb<(baskety+118) ) ){     
       OrderSend(Symbol(), OP_BUY, Lots, Ask, 3, 0, 0, "Buy",441, 0, Blue);
      }
      //Close Buy
      if(OrderFind(331) == true && ( rec2>(basketx+20)&&rec2<(basketx+113)&&rec2b>(baskety+28)&&rec2b<(baskety+118) ) ){       
            OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Blue);          
      }
      //Close Sell
      if(OrderFind(441) == true && ( rec2>(basketx+20)&&rec2<(basketx+113)&&rec2b>(baskety+28)&&rec2b<(baskety+118) ) ){       
            OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Blue);        
      }

//----
   bool   result;
   double stop_loss;
   int    cmd,total,error;
//----
   total=OrdersTotal();

      //Close Buy
      if(OrderFind(21) == true && ( rec2>(basketx+20)&&rec2<(basketx+113)&&rec2b>(baskety+28)&&rec2b<(baskety+118) ) ){       
            OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Blue);        
      }
      //Close Sell
      if(OrderFind(11) == true && ( rec2>(basketx+20)&&rec2<(basketx+113)&&rec2b>(baskety+28)&&rec2b<(baskety+118) ) ){       
            OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),3,Blue);        
      }


      if(OrderFind(11) == false && q==1 && bM>sM && bid < sM ){  
       if(TrendTrade==true)OrderSend(Symbol(), OP_SELL, Lots, Bid, 3, 0, 0, "Sell",11, 0, Blue);
         PlaySound("alert.wav");
         q=0;glbclose2=0;
      }
       
                 
      if(OrderFind(21) == false && q==1 && bM>sM && ask > bM ){
      if(TrendTrade==true)OrderSend(Symbol(), OP_BUY, Lots, Ask, 3, 0, 0, "Buy",21, 0, Blue);
         PlaySound("alert.wav");
         q=0;glbclose1=0;
    
      } 
    

   Display_Info();

//----
   return(0);
  }
//+------------------------------------------------------------------+
 
  bool OrderFind(int Magic)
  {
   glbOrderType = -1;
   glbOrderTicket = -1;
   glbOrderProfit = -1;
   glbOrderOpen = -1;
   glbOrderStop = -1;
   int total = OrdersTotal();
   bool res = false;
   for(int cnt = 0 ; cnt < total ; cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == Magic && OrderSymbol() == Symbol())
         {
           glbOrderProfit = OrderTakeProfit();
           glbOrderOpen = OrderOpenPrice();
           glbOrderStop = OrderStopLoss();
           glbOrderType = OrderType();
           glbOrderTicket = OrderTicket();
           res = true;
         }
     }
   return(res);
  }
  

double SetTrendLines(string curr,int p,int depth)
{

   double ask = MarketInfo(curr,MODE_ASK);  
   double bid = MarketInfo(curr,MODE_BID);  
   double point = MarketInfo(curr,MODE_POINT);   
   
      bid = bid+(50*point);
      ask = ask-(50*point);     
      SetObject("topline", iTime(curr,p,20), bid, iTime(curr,p,0),bid,Yellow);
      setTopLine(curr,p,20,bid,0,bid);  

      SetObject("botline", iTime(curr,p,20), ask, iTime(curr,p,0),ask,Yellow);
      setBottomLine(curr,p,20,ask,0,ask);


     
  return(0);
  }
   

  void setTopLine(string curr,int p,int period1,double val1,int period2,double val2){
                       double total=0,high=0;
                       int ls=0;
                      
                      glbline1=0;
                      for(int y=period2;y<period1;y++)
                     {
                           
                           ls=0;
                           high = NormalizeDouble(ObjectGetValueByShift("topline",y),Digits);
                          if(iHigh(curr,p,y) > high && ( iHigh(curr,p,y)-high ) > total )
                          {       
                            total = iHigh(curr,p,y) - high;  
                          }
                     }
                       if(total>0){
                       if(ObjectFind("topline") != -1)ObjectDelete("topline");   
                       SetObject("topline", iTime(curr,p,period1), val1+total, iTime(curr,p,period2),val2+total,White);    
                
                       glbline1=1;
                       }
 
               
    return(0);
  }
  
   void setBottomLine(string curr,int p,int period1,double val1,int period2,double val2){
        double low=0,total=0;
        int ls=0;

                      glbline2=0;
                      for(int y=period2;y<period1;y++)
                     {
                          low = NormalizeDouble(ObjectGetValueByShift("botline",y),Digits);
                          if(iLow(curr,p,y) < low && ( low - iLow(curr,p,y) ) > total  )
                          {      
                             
                           total = low - iLow(curr,p,y);     

                          }
                     }
                       if(total>0){
                       if(ObjectFind("botline") != -1)ObjectDelete("botline");   
                       SetObject("botline", iTime(curr,p,period1), val1-total, iTime(curr,p,period2),val2-total,White);    
                         glbline2=1;
                       } 

                       

    return(0);
  }  

  
  void SetObject(string name,datetime T1,double P1,datetime T2,double P2,color clr)
  {

         if(!ObjectCreate(name, OBJ_TREND, 0, T1, P1, T2, P2))
         {
            Print("error: can't create Object! code #",GetLastError());
         
         }
       //ObjectSet(name, OBJPROP_RAY, false);
       ObjectSet(name, OBJPROP_COLOR, clr);
       //ObjectSet(name, OBJPROP_STYLE, STYLE_DOT);
       ObjectSet(name,OBJPROP_STYLE,STYLE_SOLID);
       ObjectSet(name,OBJPROP_WIDTH,1);
  }
 
void Display_Info()
{
   Comment("Trainyouself ver 1.1\n",
            "Copyright © 2008, lightsites.co.za \n", 
            "DONATIONS: \nIf you find this useful please donate\nsomething to my paypal \n(lightsites@gmail.com) \n",                           
            "Account Balance:  $",AccountBalance(),"\n",
            "Symbol: ", Symbol(),"\n",        
            "Price:  ",NormalizeDouble(Bid,4),"\n",                                     
            "Spread:  ",MarketInfo(Symbol(),MODE_SPREAD),"\n",         
            "Date: ",Month(),"-",Day(),"-",Year()," Server Time: ",Hour(),":",Minute(),":",Seconds(),"\n",
            "Minimum Lot Size: ",MarketInfo(Symbol(),MODE_MINLOT));
   return(0);
} 

void StopLoss(){
   bool   result;
   double stop_loss,op,sl,tp,point,bid,ask;
   int    cmd,total,error;
   int w = 0;
//----
   total=OrdersTotal();
   point=MarketInfo(Symbol(),MODE_POINT);
   bid=MarketInfo(Symbol(),MODE_BID);
   ask=MarketInfo(Symbol(),MODE_ASK);  
//----
   for(int j=0; j<total; j++){
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES)){
         //---- print selected order
         cmd=OrderType();
         op=OrderOpenPrice();
         sl=OrderStopLoss();
         tp=OrderTakeProfit();
         //---- buy or sell orders are considered
         if(cmd==OP_BUY){
            if(sl==0){
            Print("::",op-(StopLoss*point));
             if(OrderModify(OrderTicket(), 0, op-(StopLoss*point), tp, 0, Yellow) == false)Print("Err (", GetLastError(), ") ModifyBuy");  
            }  
               
         }
         if(cmd==OP_SELL){
            if(sl==0){
              if(OrderModify(OrderTicket(), 0, op+(StopLoss*point), tp, 0, Yellow) == false)Print("Err (", GetLastError(), ") ModifySell");         
            }
         
         }
         
            
      }
  }

   //----
   return(0);
 } 
 
 
void TakeProfit(){
   bool   result;
   double stop_loss,op,sl,tp,point,bid,ask;
   int    cmd,total,error;
   int w = 0;
//----
   total=OrdersTotal();
   point=MarketInfo(Symbol(),MODE_POINT);
   bid=MarketInfo(Symbol(),MODE_BID);
   ask=MarketInfo(Symbol(),MODE_ASK);  
//----
   for(int j=0; j<total; j++){
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES)){
         //---- print selected order
         cmd=OrderType();
         op=OrderOpenPrice();
         sl=OrderStopLoss();
         tp=OrderTakeProfit();
         //---- buy or sell orders are considered
         if(cmd==OP_BUY){
            if(tp==0){
             if(OrderModify(OrderTicket(), 0, sl, op+(TakeProfit*point), 0, Yellow) == false)Print("Err (", GetLastError(), ") ModifyBuy");  
            }  
               
         }
         if(cmd==OP_SELL){
            if(tp==0){
              if(OrderModify(OrderTicket(), 0, sl, op-(TakeProfit*point), 0, Yellow) == false)Print("Err (", GetLastError(), ") ModifySell");         
            }
         
         }
         
            
      }
  }

   //----
   return(0);
 } 
 

 
 