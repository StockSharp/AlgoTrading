//+------------------------------------------------------------------+
//|                                                RUBBERBANDS_2.mq4 |
//|                                                      Version 1.2 |
//|                                Copyright © 2009, SummerSoft Labs |
//|                                   http://www.summersoftlabs.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, SummerSoft Labs"
#property link      "http://www.summersoftlabs.com/"

//////////////////////////////////////
extern double Lots = 0.02;
extern int maxcount = 10;
extern int pipstep = 50;
//////////////////////////////////////
extern bool quiescenow = false;
extern bool donow = false;
extern bool stopnow = false;
extern bool closenow = false;
//////////////////////////////////////
extern bool use_sessionTP = true;
extern double sessionTP = 1000; // per lot
extern bool use_sessionSL = false;
extern double sessionSL = 300; // per lot
//////////////////////////////////////
extern bool useinvalues = false; // set to true on restart
extern double inmax = 0; // set former max on restart
extern double inmin = 0; // set former min on restart
//////////////////////////////////////
//global var's
double GLBmax;
double GLBmin;
bool GLBcloseall;
bool GLBbuynow;
bool GLBsellnow;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int tradeno()
{
int cnt, total, xcnt;
    
   total=OrdersTotal();

  if (total==0)
  {
    return(0);
  }

   xcnt=0;
   
   for(cnt=0;cnt<total;cnt++)
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
     if(OrderSymbol()==Symbol())
     {
       xcnt++;
     }
  } // for
       return(xcnt);
}
//////////////////////////////////////
int close1by1()
{
int cnt, total, xcnt;
    
  total=OrdersTotal();

  if (total==0)
  {
    return(0);
  }

  for(cnt=0;cnt<total;cnt++)
  {
    OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
    if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
    {
      if(OrderType()==OP_BUY)
      {
        OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);
        return(0);
      }
      else /*OP_SELL*/
      {
        OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);
        return(0);
      }
    } // if ordertype
  } // for
  return(0);
}
//////////////////////////////////////
void comment_stopped()
{
Comment(
"SummerSoft Labs","\n",
"Lots = "+DoubleToStr(Lots,2),"\n",
"maxcount = "+maxcount,"\n",
"pipstep = "+pipstep,"\n",
"GLBmax = "+DoubleToStr(GLBmax,4),"\n",
"GLBmin = "+DoubleToStr(GLBmin,4),"\n",
"use_sessionTP = "+use_sessionTP,"\n",
"sessionTP = "+DoubleToStr(sessionTP,0),"\n",
"use_sessionSL = "+use_sessionSL,"\n",
"sessionSL = "+DoubleToStr(sessionSL,0),"\n","\n",
"STOPPED BY USER"
);
}
//////////////////////////////////////
void comment_quiesced()
{
Comment(
"SummerSoft Labs","\n",
"Lots = "+DoubleToStr(Lots,2),"\n",
"maxcount = "+maxcount,"\n",
"pipstep = "+pipstep,"\n",
"GLBmax = "+DoubleToStr(GLBmax,4),"\n",
"GLBmin = "+DoubleToStr(GLBmin,4),"\n",
"use_sessionTP = "+use_sessionTP,"\n",
"sessionTP = "+DoubleToStr(sessionTP,0),"\n",
"use_sessionSL = "+use_sessionSL,"\n",
"sessionSL = "+DoubleToStr(sessionSL,0),"\n","\n",
"QUIESCED BY USER"
);
}
//////////////////////////////////////
void comment_null()
{
Comment(
"SummerSoft Labs","\n",
"Lots = "+DoubleToStr(Lots,2),"\n",
"maxcount = "+maxcount,"\n",
"pipstep = "+pipstep,"\n",
"GLBmax = "+DoubleToStr(GLBmax,4),"\n",
"GLBmin = "+DoubleToStr(GLBmin,4),"\n",
"use_sessionTP = "+use_sessionTP,"\n",
"sessionTP = "+DoubleToStr(sessionTP,0),"\n",
"use_sessionSL = "+use_sessionSL,"\n",
"sessionSL = "+DoubleToStr(sessionSL,0),"\n","\n",
"RUNNING"
);
}
//////////////////////////////////////
void comment_pl(string bs, double pl, int total)
{
Comment(
"SummerSoft Labs","\n",
"Lots = "+DoubleToStr(Lots,2),"\n",
"maxcount = "+maxcount,"\n",
"pipstep = "+pipstep,"\n",
"GLBmax = "+DoubleToStr(GLBmax,4),"\n",
"GLBmin = "+DoubleToStr(GLBmin,4),"\n",
"total = "+total,"\n",
"use_sessionTP = "+use_sessionTP,"\n",
"sessionTP = "+DoubleToStr(sessionTP,0),"\n",
"use_sessionSL = "+use_sessionSL,"\n",
"sessionSL = "+DoubleToStr(sessionSL,0),"\n","\n",
bs,"\n",
"Profit/Loss = "+DoubleToStr(pl,2)
);
}
///////////////////////////////////////////////////////////////
int init()
{

   if (useinvalues==true)
   {
     GLBmax=inmax;
     GLBmin=inmin;
   }
   else
   {
     GLBmax=Ask;
     GLBmin=Ask;
   }

  GLBcloseall=closenow;
  GLBbuynow=donow;
  GLBsellnow=donow;
  return(0);
}
/////////////////////////////////////////////////////////////////////////
int start()
  {
   int cnt, ticket, total;
   bool closenow, buynow, sellnow;

// STOP NOW?
      if (stopnow==true)
        {
          comment_stopped();
          return(0);
        }

   comment_null();

   total=tradeno();
   
   if(total==0 && quiescenow==true) 
    {
      comment_quiesced();
      return(0);  
    }

// CLOSE NOW?
// if true, close one by one
    if (GLBcloseall==true && total>0)
    {
      close1by1();
      return(0);
    }

    if (GLBcloseall==true && total==0)
    {
      GLBcloseall=false;
      GLBmax=Ask;
      GLBmin=Ask;
      GLBbuynow=false;
      GLBsellnow=false;
    }

// check free margin
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }

// BUY NOW?
      if (GLBbuynow==true)
      {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"RUBBERBANDS_2",10000,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
           }
         else
         {
            Print("Error opening BUY order : ",GetLastError()); 
            return(-1);
         }
         GLBbuynow=false;
         return(0); 
      }

// SELL NOW?
   if (GLBsellnow==true)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"RUBBERBANDS_2",20000,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
           }
         else
         {
            Print("Error opening SELL order : ",GetLastError()); 
            return(-1);
         }
         GLBsellnow=false;
         return(0); 
      }

      // new order from 0 orders
        if (total==0 && Seconds()==0)
        {
          GLBbuynow=true;
          GLBsellnow=true;
          return(0); 
        }

///////////////////////////////////////////////////////////////////////////////////////
// counter if you will
  if (total>0)
  {

// what's the profit made
double myprofit=0;
int xtotal=OrdersTotal();

   for(cnt=0;cnt<xtotal;cnt++)
   {
     OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
     if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
     {
       myprofit+=OrderProfit();
     } //-if
    }  //-for

  comment_pl("myprofit",myprofit,total);

// close all trades for profit made?
   if (use_sessionTP==true
       && myprofit>=sessionTP*Lots)
   {
     GLBcloseall=true;
   }

// close all trades for loss cut?
   if (use_sessionSL==true
       && myprofit<=(-1)*sessionSL*Lots)
   {
     GLBcloseall=true;
   }

   if (total>=maxcount)
   {
     return(0);
   }

// do we buy or sell now?
      if (Ask>=GLBmax+pipstep*Point)
      {
        GLBmax=Ask;
        GLBsellnow=true;
        return(0); 
      }
      if (Ask<=GLBmin-pipstep*Point)
      {
        GLBmin=Ask;
        GLBbuynow =true;
        return(0); 
      }

  } // if total
  return(0);
} //-start
///////////////////////////////////////////////////////////////////////////////////////
int deinit()
{
  return(0);
}
///////////////////////////////////////////////////////////////////////////////////////
// the end.