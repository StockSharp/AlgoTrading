//+------------------------------------------------------------------+
//|                                               Hybrid Scalper.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Rodolphe Ahmad"
#property link      "https://www.mql5.com/en/users/rodoboss"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+


     extern string expname = "Hybrid Scalper"; // Expert Name
     extern string expname2 = "M1 timeframe only"; // Expert Name
     extern string expname3 = "Optimised for Eur/Usd Ecn Broker Spread no more then 5 pipes "; // Expert Rules
     extern string expnameconfirm = " https://www.mql5.com/en/users/rodoboss"; // Contact Me :
     extern string ii = "//////////////////////"; ////////////////////// 
     extern int Rsilevel = 7; //RSI level
     extern int max_spread = 10; // Max spread ! Max 12 , best performance with 4 and lower
     extern string Ea="Ea will not work if Spread is more then 11 pipes! ";
     extern bool autocalculatelots = true; //Auto Calculate lot size
     extern double lot_size = 0.01; // Lot Size
     bool locked = false;
     extern bool  showinfo=true; //Show Account Info
     extern bool enablepush = true;//Enable push notification
     extern bool allowreverse = true; //Allow stop and reverse , please keep true
     int     pips2points;    // slippage  3 pips    3=points    30=points
     extern int magicnumber = 11;// Magic Number for Strategy 1
     extern int magicnumber2 = 12;// Magic Number for Strategy 2
     double secondstoploss = 1;
     double secondtakeprofit = 1;
     double firststoploss = 1;
     double firsttakeprofit = 1;
     double  pips2dbl;       // Stoploss 15 pips    0.0015      0.00150
     int     Digitspips;    // DoubleToStr(dbl/pips2dbl, Digits.pips)
     extern string expver = "V1"; // Expert Version
     extern bool MondayTrade = true; // Monday Trade
     extern bool TuesdayTrade = true; // Tuesday Trade
     extern bool WednesdayTrade = true; // Wednesday Trade
     extern bool ThursdayTrade = true; // Thursday Trade
     extern bool FridayTrade = true; // Friday Trade
     extern bool sendreportSaturday = true; //Send Report about Your Account to you via email each Saturday
     extern bool stopduringMarketvolatilityDays =true; // do not Stop expert during Market Volatility , please keep true!
     extern string VolatilityDetector = "if you wanna Risk set it to false!"; //Set Stop expert during market volatility to false if u wanna risk
     
     extern int bbperiod  = 50; // B Band period
     extern int bbdeviation = 4; // B Band deviation
     extern string iii = "//////////////////////"; //////////////////////
     int lockedfor =0;    //0 locked for buy   // 1 locked for sell  //Ea will not open more then 1 order only 
    
int OnInit()
  {
  
  if(showinfo ==  true)  //Show Account essential information
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
  str +="\n";
  if(IsLibrariesAllowed()){str+= "Libraries Allowed";}else{str+= "Libraries not allowed";}
  str +="\n";
  str+= "Recomendation ...   Eur/usd M1  *Max recomended spread  0 to 5 pipes " + "\n";
  str+= "Magic Number 1: " + (string) magicnumber +"\n";
  str+= "Magic Number 2: " + (string) magicnumber2 +"\n";
  
  Comment(str);
  }

//---
     if (Digits == 5 || Digits == 3)
    {    // Adjust for five (5) digit brokers.
                pips2dbl    = Point*10; pips2points = 10;   Digitspips = 1;
    } 
    else {    pips2dbl    = Point;    pips2points =  1;   Digitspips = 0; }
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
  {
//---
          if (   (int) TimeDayOfWeek(TimeLocal())== 1 || (int) TimeDayOfWeek(TimeLocal())== 2 || (int) TimeDayOfWeek(TimeLocal())== 3  || (int) TimeDayOfWeek(TimeLocal())== 4 || (int) TimeDayOfWeek(TimeLocal())== 5          )
          {
               if (isspreadok() && Period() == 1 && TradeAllowedforThisDay())
  {
          if (NoVolatility() == true)
          {
                trytoUnlock();
  
      if (locked ==false)
      {
        if ( noorders() == true)
        {
        
          if(getGreenStock() <20 && getRedStock() < getGreenStock() &&trenddirection()==true )
          tryCodeBuy();
          
           if(getGreenStock() >80 && getRedStock() < getGreenStock()&& trenddirection()==false )
          tryCodeSell();
        }
      
      }
      
      SellorBuy();
      SurviveOrdersstrategy1();
       SurviveOrdersstrategy2();
          }
  }
          }
          else if(  (int) TimeDayOfWeek(TimeLocal())== 6 )
          {
            if (sendreportSaturday == true)
            {
               Sleep(6 *3600);  //Sleep 6 H
               SendReport();
               Sleep(3600 *20); // Then Sleep 20 Hour
            }
          
          }
          
          
          
  }
//+------------------------------------------------------------------+

double getRsi()
{
    RefreshRates();
 double RSI;
 RSI=iRSI(Symbol(),0,Rsilevel,PRICE_CLOSE,0);
 return RSI;
}


double testSpread()
   { 
      if ( max_spread ==0)
      {
      return 0.0000;
      }
      else if( max_spread ==1)
      {
      return 0.0001;
      }
       else if( max_spread ==2)
      {
       return 0.0002;
      }
       else if( max_spread ==3)
      {
       return 0.0003;
      }
       else if( max_spread ==4)
      {
       return 0.0004;
      }
       else if( max_spread ==5)
      {
       return 0.0005;
      }
       else if( max_spread ==6)
      {
       return 0.0006;
      }
       else if( max_spread ==7)
      {
       return 0.0007;
      }
       else if( max_spread ==8)
      {
       return 0.0008;
      }
       else if( max_spread ==9)
      {
       return 0.0009;
      }
       else if( max_spread ==10)
      {
      return 0.0010;
      }
       else if( max_spread ==11)
      {
       return 0.0011;
      }
       else if( max_spread ==12)
      {
       return 0.0012;
      }
      else { return 0.0010;}
      return 0.0010;
   }
   double calculateit()
   {
       if (autocalculatelots == false){ return lot_size;}
       else
       {
       
         if ( AccountBalance()>0 && AccountBalance() <=100)
         return 0.03;
     
         if ( AccountBalance()>100 && AccountBalance() <=500)
         return 0.10;
         if ( AccountBalance()>500 && AccountBalance() <=1000)
         return 0.20;
         if ( AccountBalance()>1000 && AccountBalance() <=5000)
         return 0.22;
         if ( AccountBalance()>5000 && AccountBalance() <=10000)
        return 0.30;
         if ( AccountBalance()>10000 && AccountBalance() <=20000)
         return 0.35;
         if ( AccountBalance()>20000 && AccountBalance() <=50000)
         return 0.40;
         if ( AccountBalance()>50000 && AccountBalance() <=100000)
         return 0.70;
         if ( AccountBalance()>100000 && AccountBalance() <=500000)
         return 0.80;
           if ( AccountBalance()>500000 )
         return 1.00;
          
       }
   return lot_size;
   }
   
    bool  isspreadok()
{
      return  ( (Ask- Bid)  <= testSpread() );
}

void trytoUnlock()
{
 
  if (locked == true)
  {
 
            if(lockedfor == 1)
            {
              if (getRsi()>70 )
        
             {
              lockedfor = 0;
              locked = false;
             }
            }
                  if(lockedfor == 2)
            {
            if (getRsi()<30 )
              
             {
              lockedfor = 0;
              locked = false;
             }
            }
            
            
  }
}

   bool noorders()
   {
   
     RefreshRates();
 int total = OrdersTotal();
 
 for(int i=total-1;i>=0;i--)
 {
 // orderstp 7
 

int s = OrderSelect(i, SELECT_BY_POS);
 int mn   = OrderMagicNumber();
 int ticket=-1;
 double ordertp=0;
 bool result =  false;
 
int result2=0;
 
 
   if (mn== magicnumber  || mn==magicnumber2)
   {
    return false;
   }
   
   
     return true;
     
   } return true;}
void tryCodeBuy()      
{
        
          if ( Bid < getRsi() && Ask < getRsi() && getRsi()<25 ) 
          {
                   locked = true;
                   lockedfor = 1;
                    sendpush(1);
                openorder("buy",calculateit());
          }
       
      
 
       
}

void tryCodeSell()     
{
        
          if (  Bid > getRsi() && Ask > getRsi() && getRsi()>85)  
          {
                   locked = true;
                   lockedfor = 2;
                    sendpush(2);
                   openorder("sell",calculateit());
          }
       
        

}

 void openorder(string buysell , double lotsize  )
 {
    
           int ticket1 =-1;
           
 
    
    
    RefreshRates(); //must be always called before opening an order
    
    if(buysell == "buy")
    {
    ticket1 = OrderSend(Symbol() , OP_BUY , lotsize , Ask , 2*pips2points ,/* stoploss */0 ,  0,"",magicnumber);
       
    
    }
    else if (buysell == "sell")
    {
    ticket1 = OrderSend(Symbol() , OP_SELL , lotsize, Bid , 2*pips2points ,/* stoploss */ 0,0,"",magicnumber); 
    }
 }
  void openorderstrategy2(string buysell , double lotsize  )
 {
    
           int ticket1 =-1;
           
 
    
    
    RefreshRates(); //must be always called before opening an order
    
    if(buysell == "buy")
    {
    ticket1 = OrderSend(Symbol() , OP_BUY , lotsize , Ask , 2*pips2points ,Bid-firststoploss* pips2dbl , Bid+firsttakeprofit* pips2dbl,"",magicnumber2);
       
    
    }
    else if (buysell == "sell")
    {
    ticket1 = OrderSend(Symbol() , OP_SELL , lotsize , Bid , 2*pips2points ,Ask+firststoploss* pips2dbl , Ask-firsttakeprofit* pips2dbl,"",magicnumber2); 
    }
 }
 void SellorBuy()
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
     
           if(lockedfor ==1)
           {
            if( OrderMagicNumber() == magicnumber  && OrderSymbol() == Symbol() )
            {
           
            
            if (getRsi() >31) //impold 31
            {
          if( (OrderMagicNumber() == magicnumber) && ( OrderSymbol() == Symbol() ) )
     {
     int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , Bid+secondtakeprofit* pips2dbl,0,White); // stoploss
     }
            }
            if (getRsi()<5)
            {
              bool   cl = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID),  2*pips2points, Red );
              
              if(allowreverse == true)
              {
               sendpush(2);
              openorderstrategy2("sell",calculateit());
              }
              
            }
           }
   }
    }
   
   if (mode == "sell")
   {
   RefreshRates();
      if(lockedfor ==2)
           {
            if( OrderMagicNumber() == magicnumber  && OrderSymbol() == Symbol() )
            {
                  if (getRsi() <65)//impold 65
            {
                if((OrderMagicNumber() == magicnumber) && ( OrderSymbol() == Symbol() ))
     {
     int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , Ask-secondtakeprofit* pips2dbl,0,White); // stoploss
        }   }
            if (getRsi()>95)
            {
            
             bool cl = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK),  2*pips2points, Red );
             if(allowreverse == true)
             {
             sendpush(1);
             openorderstrategy2("buy",calculateit());
             }
             
            }
           
           
           }
 
          }
   }
   
 }
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
     if ( (Bid + 0.00001  == ordertp) || (Bid + 0.00002  == ordertp) || (Bid + 0.00003  == ordertp) || (Bid + 0.00004  == ordertp) || (Bid + 0.00005  == ordertp) || (Bid + 0.00006  == ordertp) || (Bid + 0.00007  == ordertp)|| (Bid + 0.00008  == ordertp)|| (Bid + 0.00009  == ordertp)|| (Bid + 0.00010  == ordertp) )
     {
     
     if( (OrderMagicNumber() == magicnumber) && ( OrderSymbol() == Symbol() ) )
     {
     int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , Bid+secondtakeprofit* pips2dbl,0,White); // stoploss
     }
       
      
   
     
     }
       
  
   }
   
   if (mode == "sell")
   {
   RefreshRates();
   if((Ask - 0.00001 == ordertp) || (Ask - 0.00002 == ordertp) || (Ask - 0.00003 == ordertp)|| (Ask - 0.00004 == ordertp)|| (Ask - 0.00005 == ordertp)|| (Ask - 0.00006 == ordertp)|| (Ask - 0.00007 == ordertp)|| (Ask - 0.00008 == ordertp)|| (Ask - 0.00009 == ordertp)|| (Ask - 0.00010 == ordertp) )
   {
   
    if((OrderMagicNumber() == magicnumber) && ( OrderSymbol() == Symbol() ))
     {
     int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , Ask-secondtakeprofit* pips2dbl,0,White); // stoploss
        }
        
        
 
   }
          
   }
   
 }
}



void SurviveOrdersstrategy2()
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
     if ( (Bid + 0.00001  == ordertp) || (Bid + 0.00002  == ordertp) || (Bid + 0.00003  == ordertp) || (Bid + 0.00004  == ordertp) || (Bid + 0.00005  == ordertp) || (Bid + 0.00006  == ordertp) || (Bid + 0.00007  == ordertp)|| (Bid + 0.00008  == ordertp)|| (Bid + 0.00009  == ordertp)|| (Bid + 0.00010  == ordertp) )
     {
     
     if( (OrderMagicNumber() == magicnumber2) && ( OrderSymbol() == Symbol() ) )
     {
     int md =OrderModify(ticket,Ask,Bid-secondstoploss* pips2dbl , Bid+secondtakeprofit* pips2dbl,0,White); // stoploss
     }
       
      
   
     
     }
       
  
   }
   
   if (mode == "sell")
   {
   RefreshRates();
   if((Ask - 0.00001 == ordertp) || (Ask - 0.00002 == ordertp) || (Ask - 0.00003 == ordertp)|| (Ask - 0.00004 == ordertp)|| (Ask - 0.00005 == ordertp)|| (Ask - 0.00006 == ordertp)|| (Ask - 0.00007 == ordertp)|| (Ask - 0.00008 == ordertp)|| (Ask - 0.00009 == ordertp)|| (Ask - 0.00010 == ordertp) )
   {
   
    if((OrderMagicNumber() == magicnumber2) && ( OrderSymbol() == Symbol() ))
     {
     int md= OrderModify(ticket,Bid,Ask+secondstoploss* pips2dbl , Ask-secondtakeprofit* pips2dbl,0,White); // stoploss
        }
        
        
 
   }
          
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
void sendpush(int a )
{
               if(enablepush==true && a==1)
               {
                SendNotification("Hybrid Scalper Good time to go long " + Symbol() + (string)TimeCurrent() + "Spread" + (string)MarketInfo(Symbol(),MODE_SPREAD) + " Equity:" + (string)AccountEquity() + "Free Margin" + (string)AccountFreeMargin()) ;
                SendMail("Hybrid Scalper"," Good time to go long " + Symbol() + (string)TimeCurrent() + "Spread" + (string)MarketInfo(Symbol(),MODE_SPREAD) + " Equity:" + (string)AccountEquity() + "Free Margin" + (string)AccountFreeMargin());
               }
               
                   if(enablepush==true && a==2)
               {
                SendNotification("Hybrid Scalper Good time to go Short " + Symbol() + (string)TimeCurrent() + "Spread" + (string)MarketInfo(Symbol(),MODE_SPREAD) + " Equity:" + (string)AccountEquity() + "Free Margin" + (string)AccountFreeMargin()) ;
                SendMail("Hybrid Scalper"," Good time to go Short " + Symbol() + (string)TimeCurrent() + "Spread" + (string)MarketInfo(Symbol(),MODE_SPREAD) + " Equity:" + (string)AccountEquity() + "Free Margin" + (string)AccountFreeMargin());
               }
               
               }
                  
 
  bool trenddirection()
{
double price_ma_period_fast =21; //slow ma
double price_ma_period_slow =89; //fast ma 
//----
double pricefastmanow,priceslowmanow;
pricefastmanow = iMA(Symbol(),15,(int)price_ma_period_fast,0,MODE_EMA,PRICE_CLOSE,0);
priceslowmanow = iMA(Symbol(),15,(int)price_ma_period_slow,0,MODE_EMA,PRICE_CLOSE,0);

if (pricefastmanow > priceslowmanow)// bullish
{
return(true);
}

if (pricefastmanow < priceslowmanow)// bearish
{
return(false);
}
////////////////////////////////////////////////////////////////////////////////////////////////////////

return(0);
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

void SendReport()

{
  string str = "Account Name: " + AccountName() + "\n";
  str+=  "Company: " + AccountCompany() + "\n";
  str+=  "Account Leverage: " + (string)AccountLeverage() + "\n";
  str+= "Expert Name: " + WindowExpertName() +"\n";
  str +="\n";
  str += "Account Balance " + (string)AccountBalance() + "\n";
  str += "Account Equity " + (string)AccountEquity() + "\n";
  str += "Account Margin " + (string)AccountMargin() + "\n";
  str += "Account Free Margin " + (string)AccountFreeMargin() + "\n";
  str += "Total Profits (All time Profit)" + (string)AccountProfit() + "\n";
  str += "Have a nice Week end :)";
  SendMail("Hybrid Scalper Report",str);
  
  
}

bool NoVolatility()
{
 if (stopduringMarketvolatilityDays==false)
 return true;
 
 if (Day() == 14 && Month() == 12)
 return false;
 
  if (Day() == 15 && Month() == 12)
 return false;
  if (Day() == 16 && Month() == 12)
 return false;
  if (Day() == 17 && Month() == 12)
 return false;
  if (Day() == 18 && Month() == 12)
 return false;
  if (Day() == 19 && Month() == 12)
 return false;
  if (Day() == 20 && Month() == 12)
 return false;
  if (Day() == 21 && Month() == 12)
 return false;
  if (Day() == 22 && Month() == 12)
 return false;
  if (Day() == 23 && Month() == 12)
 return false;
  if (Day() == 24 && Month() == 12)
 return false;
  if (Day() == 25 && Month() == 12)
 return false;
  if (Day() == 26 && Month() == 12)
 return false;
  if (Day() == 27 && Month() == 12)
 return false;
  if (Day() == 28 && Month() == 12)
 return false;
  if (Day() == 29 && Month() == 12)
 return false;
  if (Day() == 30 && Month() == 12)
 return false;
  if (Day() == 31 && Month() == 12)
 return false;
 
 
 
 
   if (Day() == 1 && Month() == 1)
 return false;
   if (Day() == 2 && Month() == 1)
 return false;
   if (Day() == 3 && Month() == 1)
 return false;
   if (Day() == 4 && Month() == 1)
 return false;
 
 if (stopduringMarketvolatilityDays==true)
 {
  if(getHigherIband() - getMiddleIband() > 0.00262)
 return false;
 
  if(getMiddleIband() - getLowerIband() > 0.00262)
 return false;
 
 
    if(getHigherIband() - getMiddleIband() < 0.00045)
 return false;
 
  if(getMiddleIband() - getLowerIband() <  0.00045)
 return false; 
 }

 
 
  
 
 return true;
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