//+------------------------------------------------------------------+
//|                                                   HardcoreFX.mq4 |
//|                        Copyright 2013, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Louis Christian Stoltz (lightsites@gmail.com Paypal please donate)"
#property link      "http://www.lightsites.co.za/"

int StopLoss; // Use this variable for StopLoss
int ProfitPips; // Use this variable for ProfitPips

//extern int GuarenteedProfit=5; // 5 dollars guaranteed

extern double Risk=0.02;  // 0.02 = 2% Risk Money Management
extern double FixedLots = 0.0; // Leave blank to use Money Management

// Variables for Orderfind
int MagicBuy = 7141;
int MagicSell = 7142;
double glbOrderProfit;
double glbOrderOpen;
double glbOrderStop;
double glbOrderType;
double glbOrderTicket;

// Variables for placeholders
  int t1,t2,p;
  
// Variables for signals  
bool Buy_Signal = false;
bool Sell_Signal = false;
bool Buy_StopSignal = false;
bool Sell_StopSignal = false;

//+------------------------------------------------------------------+
//| Signals                                                          |
//| Signal = 1 ( Awesome Indicator )                                 |
//+------------------------------------------------------------------+
extern int Signal = 2;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init() {
//----
   
//----
   return(0);
}
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit() {
//----
   
//----
   return(0);
}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start() {
//----

      Signals(Signal);
      
      if (Buy_Signal){
        buyit(Symbol());
      }
      if (Sell_Signal) {
        sellit(Symbol());
      }

      
      checkClose();
         
 
//----
   return(0);
}
//+------------------------------------------------------------------+

bool checkClose() {

       if(Buy_StopSignal == true && OrderFind(MagicBuy) == true){    
         RefreshRates();
         if(OrderProfit() > 0) {
           OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_BID),3,Violet);
         }
         
       } 
       if(Sell_StopSignal == true && OrderFind(MagicSell) == true){    
         RefreshRates();
         if(OrderProfit() > 0) {
           OrderClose(OrderTicket(),OrderLots(),MarketInfo(OrderSymbol(),MODE_ASK),3,Violet);
         }
         
       }   
}       

bool Signals(int n) {
 //Reset Signals
 
   switch(n)
     {                                           
      case 1 : 
      StopLoss=1400; // Use this variable for StopLoss
      ProfitPips=5400; // Use this variable for ProfitPips
      Awesome(Symbol());    
      break;
      case 2 : 
      StopLoss=1400; // Use this variable for StopLoss
      ProfitPips=5400; // Use this variable for ProfitPips
      Elliot(Symbol());    
      break;            
      case 3 : 
      StopLoss=1400; // Use this variable for StopLoss
      ProfitPips=1400; // Use this variable for ProfitPips
      TrailOrders(500);  // Trailing Stop
      ZZ(Symbol(), Period());    
      break;
      default: Alert("Default");   
     }    
}

int buyit(string crunchies) {
      if(OrderFind(MagicBuy) == false && OrderFind(MagicSell) == false){
        double ask = MarketInfo(crunchies,MODE_ASK);              
        OrderSend(crunchies,OP_BUY,Lots(),ask,3,ask-StopLoss*Point,ask+ProfitPips*Point,"Buy Order" + crunchies,MagicBuy,0,Green);   
      }              
   }

int sellit(string crunchies) {
      if(OrderFind(MagicBuy) == false && OrderFind(MagicSell) == false){   
        double bid = MarketInfo(crunchies,MODE_BID);                   
        OrderSend(crunchies,OP_SELL,Lots(),bid,3,bid+StopLoss*Point,bid-ProfitPips*Point,"Sell Order" + crunchies,MagicSell,0,Red);
      }  
    }

bool OrderFind(int Magic) {
   glbOrderType = -1;
   glbOrderTicket = -1;
   glbOrderProfit = 0;
   glbOrderOpen = -1;
   glbOrderStop = -1;
   int total = OrdersTotal();
   bool res = false;
   for(int cnt = 0 ; cnt < total ; cnt++)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() == Magic && OrderSymbol() == Symbol())
         {
           glbOrderType = OrderType();
           glbOrderTicket = OrderTicket();
           glbOrderProfit = OrderProfit();
           glbOrderOpen = OrderOpenPrice();
           glbOrderStop = OrderStopLoss();
           res = true;
         }
     }
   return(res);
  }
  
double Lots() {

    if(FixedLots > 0.0)
        return (FixedLots);

    double pipValue = MarketInfo(Symbol(), MODE_TICKVALUE);
    double lots = AccountFreeMargin() * Risk / (StopLoss * pipValue);
    
    double lotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
    int digits = 0;
    if(lotStep <= 0.01)
        digits = 2;
    else if(lotStep <= 0.1)
        digits = 1;
    lots = NormalizeDouble(lots, digits);
      
      double minLots = MarketInfo(Symbol(), MODE_MINLOT);
      if(lots < minLots)
          lots = minLots;
      
      double maxLots = MarketInfo(Symbol(), MODE_MAXLOT);
      if(lots > maxLots)
          lots = maxLots;
      
      return (lots);
}  

// Trailing stop for orders  
void TrailOrders(int trail){
   bool   result;
   double stop_loss,op,sl,tp,point,bid,ask;
   int    cmd,total,error;
   int w = 0;
//----
   total=OrdersTotal();
  
//----
   for(int j=0; j<total; j++){
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES)){
        point=MarketInfo(OrderSymbol(),MODE_POINT);
        bid=MarketInfo(OrderSymbol(),MODE_BID);
        ask=MarketInfo(OrderSymbol(),MODE_ASK);
 
         //---- print selected order
         cmd=OrderType();
         op=OrderOpenPrice();
         sl=OrderStopLoss();
         tp=OrderTakeProfit();
         //---- buy or sell orders are considered
         if(cmd==OP_BUY){
         
            if(sl < (ask-trail*point) && (ask-trail*point) > op){
             if(OrderModify(OrderTicket(), op, ask-trail*point, ask+(trail*2)*point, 0, Yellow) == false)Print("Err (", GetLastError(), ") ModifyBuy");  
             }                
         }
         if(cmd==OP_SELL){
            if(sl > (bid+trail*point)&& (bid+trail*point) < op ){
              if(OrderModify(OrderTicket(), op, bid+trail*point, bid-(trail*2)*point, 0, Yellow) == false)Print("Err (", GetLastError(), ") ModifySell");         
            }
         
         }
         
            
      }
  }

   //----
   return(0);
 } 
     
     
void SetObject(string name,datetime T1,double P1,datetime T2,double P2,color clr)
{

         if(!ObjectCreate(name, OBJ_TREND, 0, T1, P1, T2, P2))
         {
            Print("error: can't create label_object! code #",GetLastError());
         
         }

       //ObjectSet(name, OBJPROP_RAY, false);
       ObjectSet(name, OBJPROP_COLOR, clr);
       //ObjectSet(name, OBJPROP_STYLE, STYLE_DOT);
       ObjectSet(name,OBJPROP_STYLE,STYLE_SOLID);
       ObjectSet(name,OBJPROP_WIDTH,2);
}

//Awesome indicator Signal
bool Awesome(string s) {
      int set, succeed;
      
      double ao1 = iCustom(s,Period(),"Awesome",0,0);  
      double hh1 = iCustom(s,Period(),"Awesome",0,1);             
      double hh2 = iCustom(s,Period(),"Awesome",0,2); 
      
      if (ao1 > 0 && hh1 < 0  && hh2 < hh1){ Sell_Signal = false;Buy_Signal = true;}
      if (ao1 < 0 && hh1 > 0  && hh2 > hh1){ Buy_Signal = false;Sell_Signal = true;}
      
      if(ao1 < 0) {
        Sell_StopSignal = false;
        Buy_StopSignal = true; 
      }
      if(ao1 > 0) {
        Sell_StopSignal = true;
        Buy_StopSignal = false; 
      }
            
      return(true);
   }
//Awesome indicator end

//Elliot indicator Signal
bool Elliot(string s) {
      int set;     
      double MA5,MA35;

      MA5=iMA(NULL,0,5,0,MODE_SMA,PRICE_MEDIAN,0);
      MA35=iMA(NULL,0,35,0,MODE_SMA,PRICE_MEDIAN,0);
      double f1 = MA5-MA35;
      MA5=iMA(NULL,0,5,0,MODE_SMA,PRICE_MEDIAN,1);
      MA35=iMA(NULL,0,35,0,MODE_SMA,PRICE_MEDIAN,1);
      double f2 = MA5-MA35;
    
      if (f2 < 0 && f1 > 0){ Sell_Signal = false;Buy_Signal = true;}
      if (f2 > 0 && f1 < 0){ Buy_Signal = false;Sell_Signal = true;}
      
      if(f1 < 0) {
        Sell_StopSignal = false;
        Buy_StopSignal = true; 
      }
      if(f1 > 0) {
        Sell_StopSignal = true;
        Buy_StopSignal = false; 
      }    
      return(true);
   }
//Elliot indicator end

// ZIGZAG Patterns
double ZZ(string curr,int p)
  {
   double array[9][2];
   int gblx1 = 0;
   int gblx2 = 0;
   double ask = MarketInfo(curr,MODE_ASK);  
   double bid = MarketInfo(curr,MODE_BID);  
   double point = MarketInfo(curr,MODE_POINT);
   Comment(point);
   double zzh;
  ArrayInitialize(array,'');

  for(int i=0;i<9000;i++)
  {
    //Unknown
    zzh=iCustom(curr,p,"ZigZag",17,5,8, 0, i); 
    if (zzh!=0){
    
          if(!array[0][0]){
               array[0][0] = zzh;
               array[0][1] = i;
               }
             else if(!array[1][0] && zzh!=array[0][0]){
               array[1][0] = zzh;
               array[1][1] = i;    
             }
             else if(!array[2][0] && zzh!=array[1][0] && i>array[1][1]){
               array[2][0] = zzh;
               array[2][1] = i;    
             }
             else if(!array[3][0] && zzh!=array[2][0] && i>array[2][1]){
               array[3][0] = zzh;
               array[3][1] = i;    
             }
             else if(!array[4][0] && zzh!=array[3][0] && i>array[3][1]){
               array[4][0] = zzh;
               array[4][1] = i;    
             }   
             else if(!array[5][0] && zzh!=array[4][0] && i>array[4][1]){
               array[5][0] = zzh;
               array[5][1] = i;    
             }  
             else if(!array[6][0] && zzh!=array[5][0] && i>array[5][1]){
               array[6][0] = zzh;
               array[6][1] = i;    
             }
             else if(!array[7][0] && zzh!=array[6][0] && i>array[6][1]){
               array[7][0] = zzh;
               array[7][1] = i;    
             } 
             else if(!array[8][0] && zzh!=array[7][0] && i>array[7][1]){
               array[8][0] = zzh;
               array[8][1] = i;    
             }                                     
      }
  }
   
   

      double bM;  
      double tM;  
      int l=0;
      gblx1=0;
      gblx2=0;
 
 
      if(array[3][0]>array[4][0]){     

         if(ObjectFind("topline") != -1) ObjectDelete("topline");  
         if(ObjectFind("botline") != -1) ObjectDelete("botline"); 
      
         if(array[5][0]>array[3][0]){SetObject("topline", iTime(curr,p,array[5][1]), array[5][0], iTime(curr,p,array[1][1]),array[1][0],Yellow);}      
         else{SetObject("topline", iTime(curr,p,array[3][1]), array[3][0], iTime(curr,p,array[1][1]),array[1][0],Yellow);}            
         if(array[6][0]<array[4][0]){SetObject("botline", iTime(curr,p,array[6][1]), array[6][0], iTime(curr,p,array[2][1]),array[2][0],Yellow);}
         else{SetObject("botline", iTime(curr,p,array[4][1]), array[4][0], iTime(curr,p,array[2][1]),array[2][0],Yellow);}
      

      }
      else{
      
            
      if(ObjectFind("topline") != -1) ObjectDelete("topline");  
      if(ObjectFind("botline") != -1) ObjectDelete("botline"); 
      
      if(array[6][0]>array[4][0]){SetObject("topline", iTime(curr,p,array[6][1]), array[6][0], iTime(curr,p,array[2][1]),array[2][0],Yellow);}      
      else{SetObject("topline", iTime(curr,p,array[4][1]), array[4][0], iTime(curr,p,array[2][1]),array[2][0],Yellow);}            
       if(array[5][0]<array[3][0]){SetObject("botline", iTime(curr,p,array[5][1]), array[5][0], iTime(curr,p,array[1][1]),array[1][0],Yellow);}
       else{SetObject("botline", iTime(curr,p,array[3][1]), array[3][0], iTime(curr,p,array[1][1]),array[1][0],Yellow);}
      
      }
   
    
  if(ObjectFind("tline") != -1) ObjectDelete("tline"); 
  if(ObjectFind("bline") != -1) ObjectDelete("bline");  
 
  double top = NormalizeDouble(ObjectGetValueByShift("topline",0),Digits);  
  double bottom = NormalizeDouble(ObjectGetValueByShift("botline",0),Digits); 

  if(t1 == 0 && ask > top){t1 = 1;Buy_Signal = true;}
  if(t2 == 0 && bid < bottom){t2=1;Sell_Signal = true;}
  
  if(bid < top && bid > bottom){t1=0;t2=0;Buy_Signal = false;Sell_Signal = false;} 
     
  return(0);
  }
// END ZIGZAG

