//+------------------------------------------------------------------+
//|                 This has been coded by MT-Coder                  |
//|                                                                  |
//|                     Email: mt-coder@hotmail.com                  |
//|                      Website: mt-coder.110mb.com                 |
//|                                                                  |
//|          I can code for you any strategy you have in mind        |
//|           into EA, I can code any indicator you have in mind     |
//|                                                                  |
//|                     For any programming idea                     |
//|          Don't hesitate to contact me at mt-coder@hotmail.com    |
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|                                                                  |
//|             __________Stop Hunter__________                      |
//|                 Copyright © 2010 MT-Coder                        |
//|                                                                  |
//|          This EA is based on the strategy :                      |
//|                                                                  |
//|          "Stop Hunting with the big players"                     |         
//|          ___________________________________                     |
//|                                                                  |
//| It sends BuyStop and SellStop orders at the given distance from  |
//|   the round price target.                                        |
//| Stop Hunter uses hiden TakeProfit and StopLoss, this way you can |
//| hide them from your broker, and you get to set small values near |
//| spread.                                                          |
//|              ___________Testing___________                       |
//|  The reports you may see on my website are made with virtual     |
//|  conditions of Spread=0.                                         |
//|                                                                  |
//|              ___________Settings__________                       |
//|      Zeroes: how many zeroes to the right of the price.          |
//|      Distance: how far from the round price target should the    |
//|                order be placed.                                  |
//|                                                                  |
//|      The other settings are usual.                               |
//|                                                                  |
//|              ___________ Update __________                       |
//|                Latest: Oct 02 2010                               |
//| Oct 02 2010: Loop problem seem to be fixed, thanks to ApacheD    |
//|            * Magical number problem fixed, may be attached to    |
//|              several charts at once.                             |
//| Sep 18 2010: Now fixed even more problems.                       |
//| Sep 09 2010: Now fixed many problems.                            |
//| Sep 06 2010: Now fixed the problem regarding Zeroes input.       |
//+------------------------------------------------------------------+


#property copyright "Copyright © 2010 MT-Coder "
#property link      "http://mt-coder.110mb.com/"


extern int Zeroes = 2;//



extern int Distance = 15;
extern int TakeProfit       = 15;
extern int StopLoss        = 15;

extern bool LongOrders = TRUE;
extern bool ShortOrders = TRUE;

extern double  Risk_percent   = 5;
extern double minLots           = 0.1;
extern double maxLots           = 30;
extern int        MaxBuyPos           = 1;//maximum Buy positions at once
extern int        MaxSellPos           = 1;//maximum Sell positions at once
extern int        magic = 3265;


//----
int     POS_n_BUY;
int     POS_n_SELL;
int     POS_n_BUYSTOP;
int     POS_n_SELLSTOP;
int     POS_n_total;
double   Lots;
double   NewLots;
//----
double OrderLevelB;
double OrderLevelS;

   //----
   int TP2= 0;
   int SL2= 0;
   bool SecondTrade = FALSE;
   //----
string name = "Stop_Hunter";
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
{
//-----Up target
CreateObjects("up", STYLE_SOLID, Blue);
CreateObjects("upup", STYLE_DOT, Blue);
CreateObjects("updn", STYLE_DOT, Blue);

//-----Dn target
CreateObjects("dn", STYLE_SOLID, Red);
CreateObjects("dnup", STYLE_DOT, Red);
CreateObjects("dndn", STYLE_DOT, Red);

//-----
return(0);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
{
//-------delete the remaining positions
DeleteBuyStop();
DeleteSellStop();
//-------delete the lines
DeleteObjects();
//-----
return(0);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
  

   int cnt, total;
   int ticketB, ticketS, ticketC;
   int MaxOpenPos = MaxBuyPos + MaxSellPos;
   int digits = MarketInfo(Symbol(), MODE_DIGITS);

   string DisplayText;
   string LongStat; 
   string ShortStat;
   //---
   string BidString ;
   int    Len;

   //---
   if(LongOrders) LongStat = "YES"; else LongStat = "NO";
   if(ShortOrders) ShortStat = "YES"; else ShortStat = "NO";
   //---
    //-----
    count_position();  
    //-----
    
   RefreshRates();
   
   BidString = DoubleToStr(Bid,digits);
   Len = StringLen(BidString)-1;
   
   double LevelB = MathCeil(Bid / (Point* MathPow(10,Zeroes))) * Point* MathPow(10,Zeroes);
   if(LevelB - Distance*Point <= Ask) {LevelB = LevelB + MathPow(10,Zeroes)*Point;}
//   if(LevelB - Distance*Point > OrderLevelB + Distance*Point && OrderLevelB != 0){ DeleteBuyStop(); }
if( (LevelB  > getGlobal("LevelB") || Ask < getGlobal("LevelB")-MathPow(10,Zeroes)*Point-Distance*50*Point)&& getGlobal("LevelB")!=0){ DeleteBuyStop(); }

   
   double LevelS = LevelB - MathPow(10,Zeroes)*Point;
   if(LevelS + Distance*Point >= Bid) {LevelS = LevelS - MathPow(10,Zeroes)*Point; }
 //  if(LevelS + Distance*Point < OrderLevelS - Distance*Point && OrderLevelS != 0){ DeleteSellStop(); }
 if( (LevelS < getGlobal("LevelS") || Bid > getGlobal("LevelS")+MathPow(10,Zeroes)*Point+Distance*50*Point) && getGlobal("LevelS")!=0){ DeleteSellStop(); }

   
   
   //-------Check settings 
   if(Zeroes >= Len || Zeroes <= 0)
   {   
   DisplayText = "\n" + "______ Stop Hunter ______\n" + 
   "Coded By: MT-Coder\n" + 
   "** MT-Coder@hotmail.com **\n" + 
   "http://MT-Coder.110mb.com\n\n" 
   + "Wrong settings\n" + "Zeroes should be smaller than " + Len + " and bigger than 0";
   
   Comment(DisplayText);
   return(0);
   }
   else
   DisplayText = "\n" +  "______ Stop Hunter ______\n" + 
   "Coded By: MT-Coder\n" + 
   "** MT-Coder@hotmail.com **\n" + 
   "http://MT-Coder.110mb.com\n\n" + 
   "Up Target: " + DoubleToStr(LevelB,digits) + "| Long Orders " + LongStat + "\n" + 
   "Dn Target: " + DoubleToStr(LevelS,digits) + "| Short Orders " + ShortStat;
   
   Comment(DisplayText);
    
    //-----------Draw lines
      //lines for up target
    DrawObjects("up",LevelB);
    DrawObjects("upup",LevelB + Distance*Point);
    DrawObjects("updn",LevelB - Distance*Point);
      //lines for down target
    DrawObjects("dn",LevelS);
    DrawObjects("dnup",LevelS + Distance*Point);
    DrawObjects("dndn",LevelS - Distance*Point);
    

   
   if(POS_n_total < MaxOpenPos) 
     {

      // 
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }
        
        Call_MM();
        
      // BUY
   if(LongOrders && POS_n_BUYSTOP + POS_n_BUY < MaxBuyPos && POS_n_SELL == 0) 
     {
     
         ticketB=OrderSend(Symbol(),OP_BUYSTOP,Lots,LevelB-Distance*Point,1,0,0,"Stop Hunter",magic,0,Green);
         if(ticketB>0)
           {
            if(OrderSelect(ticketB,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY Stop order sent : ",OrderOpenPrice());
            setGlobal("LevelB",LevelB);
           }
         else {
         Print("Error sending BUY Stop order : ",GetLastError()); 
         return(0); 
         }
         return(0); 
      }
      // END BUY
      
      // SELL
      
   if(ShortOrders && POS_n_SELLSTOP + POS_n_SELL < MaxSellPos && POS_n_BUY == 0) 
   {

         ticketS=OrderSend(Symbol(),OP_SELLSTOP,Lots,LevelS+Distance*Point,1,0,0,"Stop Hunter",magic,0,Red);
         if(ticketS>0)
           {
            if(OrderSelect(ticketS,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL Stop order sent : ",OrderOpenPrice());
            setGlobal("LevelS",LevelS);
           }
         else {
         Print("Error sending SELL Stop order : ",GetLastError()); 
         return(0); 
        }
      return(0);
      }
      // END SELL
}

//-------

   total=OrdersTotal();


  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() != magic) break;
      // delete the useless positions
       if(OrderType()==OP_BUYSTOP && POS_n_SELL != 0  && OrderSymbol() == Symbol())
         {
         OrderDelete(OrderTicket());
         }
         
       if(OrderType()==OP_SELLSTOP && POS_n_BUY != 0 && OrderSymbol() == Symbol())
         {
         OrderDelete(OrderTicket());
         }
      //achieve the hidden TakeProfit and StopLoss
       RefreshRates();
       
         if(OrderType()==OP_BUY  && OrderSymbol() == Symbol())
         {
         if(Bid  >= OrderOpenPrice() + (TakeProfit+TP2)*Point || Ask  <= OrderOpenPrice() - (StopLoss+SL2)*Point)
         {
         
         if(!SecondTrade && Bid  >= OrderOpenPrice() + (TakeProfit+TP2)*Point) 
         {
         NewLots = NormalizeDouble(OrderLots()/2,2);
         
            ticketC = OrderClose(OrderTicket(),NewLots,Bid,3,Violet);
            if(ticketC > 0)
            {
               if(OrderSelect(ticketC,SELECT_BY_TICKET,MODE_TRADES)) Print("Order closed : ",OrderProfit());
               TP2 = TakeProfit;
               SL2 = StopLoss;
               SecondTrade = TRUE;
            }
            else {
            Print("Error closing order : ",GetLastError()); 
            if(GetLastError() == 131) 
                                    
                  ticketC = OrderClose(OrderTicket(),0.1,Ask,3,Violet);
                  }
         }
         else 
         {
            ticketC = OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);
            if(ticketC > 0)
           {
            if(OrderSelect(ticketC,SELECT_BY_TICKET,MODE_TRADES)) Print("Order closed : ",OrderProfit());
            SecondTrade = FALSE;
           }
            else {Print("Error closing order : ",GetLastError());}
         
         }
                  
         }
         }
         
       RefreshRates();

         if(OrderType()==OP_SELL && OrderSymbol() == Symbol())
         {
            if(Ask <= OrderOpenPrice() - (TakeProfit+TP2)*Point || Bid >= OrderOpenPrice() + (StopLoss+SL2)*Point)
             {
               if(!SecondTrade && Ask <= OrderOpenPrice() - (TakeProfit+TP2)*Point) 
                  {
                  NewLots = NormalizeDouble(OrderLots()/2,2);
               
                  ticketC = OrderClose(OrderTicket(),NewLots,Ask,3,Violet);
                     if(ticketC > 0)
                     {
                    
                        if(OrderSelect(ticketC,SELECT_BY_TICKET,MODE_TRADES)) Print("Order closed : ",OrderProfit());
                        TP2 = TakeProfit;
                        SL2 = StopLoss;
                        SecondTrade = TRUE;
                     }
                     else {Print("Error closing order : ",GetLastError());
                     if(GetLastError() == 131) 
                     
               
                  ticketC = OrderClose(OrderTicket(),0.1,Ask,3,Violet);}
                  }
               else 
                  {
                  ticketC = OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);  
                     if(ticketC > 0)
                     {
                        if(OrderSelect(ticketC,SELECT_BY_TICKET,MODE_TRADES)) Print("Order closed : ",OrderProfit());
                        SecondTrade = FALSE;
                     }
                     else {Print("Error closing order : ",GetLastError());}
                  }
             }
         }
         
      }      
//-------

   return(0);
}
//----------------
void Call_MM()
{

   Lots=AccountFreeMargin()/100000*Risk_percent; 
   
   
   Lots=MathMin(maxLots,MathMax(minLots,Lots));
   if(minLots<0.1) 
     Lots=NormalizeDouble(Lots,2);
   else
     {
     if(minLots<1) Lots=NormalizeDouble(Lots,1);
     else          Lots=NormalizeDouble(Lots,0);
     }


   return(0);
   
   }

//-------
void DeleteBuyStop()
{
int cnt, total;
total=OrdersTotal();
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() != magic || OrderType()!=OP_BUYSTOP) break;
      
      if( OrderSymbol() == Symbol())  OrderDelete(OrderTicket());
      
     }
}
//-------
void DeleteSellStop()
{
int cnt, total;
total=OrdersTotal();
  for(cnt=total-1;cnt>=0;cnt--)
     {
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderMagicNumber() != magic || OrderType()!=OP_SELLSTOP) break;
      
if( OrderSymbol() == Symbol())  OrderDelete(OrderTicket());      
     }
}
//--------
void count_position()
{
    POS_n_BUY  = 0;
    POS_n_SELL = 0;
    
    POS_n_BUYSTOP = 0;
    POS_n_SELLSTOP = 0;
    
    for( int i = 0 ; i < OrdersTotal() ; i++ ){
        if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false || OrderMagicNumber() != magic){
            break;
        }
        /*
        if( OrderSymbol() != Symbol() ){
            continue;
        }
        */
        if( OrderType() == OP_BUY  && OrderSymbol() == Symbol() && OrderMagicNumber()==magic){
            POS_n_BUY++;
        }
        else if( OrderType() == OP_SELL  && OrderSymbol() == Symbol() && OrderMagicNumber()==magic){
            POS_n_SELL++;
        }
        
        else if( OrderType() == OP_BUYSTOP  && OrderSymbol() == Symbol() && OrderMagicNumber()==magic){
            POS_n_BUYSTOP++;
            OrderLevelB = OrderOpenPrice();
        }
        else if( OrderType() == OP_SELLSTOP  && OrderSymbol() == Symbol() && OrderMagicNumber()==magic){
            POS_n_SELLSTOP++;
            OrderLevelS = OrderOpenPrice();
        }
        
    }
    POS_n_total = POS_n_BUY + POS_n_SELL + POS_n_BUYSTOP + POS_n_SELLSTOP;
}
//------------- 
void CreateObjects(string no, double style, color col) {

  ObjectCreate(no, OBJ_HLINE, 0,0,0);
  ObjectSet(no, OBJPROP_STYLE, style);
  ObjectSet(no, OBJPROP_COLOR, col);
  ObjectSet(no, OBJPROP_BACK, True);
}
//-------------
void DrawObjects(string no, double price) {

  ObjectSet(no, OBJPROP_PRICE1, price);

}
//-------------
void DeleteObjects() {
  ObjectDelete("up");
  ObjectDelete("upup");
  ObjectDelete("updn");
  ObjectDelete("dn");
  ObjectDelete("dnup");
  ObjectDelete("dndn");
  }
//----------------
void setGlobal(string key, double value){
   GlobalVariableSet(name + magic + "_" + key, value);
}

double getGlobal(string key){
   return(GlobalVariableGet(name + magic + "_" + key));
}
//-----------
// the end.