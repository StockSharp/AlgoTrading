//+------------------------------------------------------------------+
//|                                                  EA_Simplest.mq4 |
//+------------------------------------------------------------------+
#property copyright "Syah's Program"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
//---
#define EAName "EA Simplest "
#define EANumber 12345


input ENUM_TIMEFRAMES TF   = PERIOD_CURRENT;// Select Time Frame
input int period           = 8;// Period DeMarker
extern double lt           = 0.1;// Lots
extern int sl              = 150;// Stop Loss
extern int tp              = 150;// Take Profit
input double OB            = 0.7;// Over Sold
input double OS            = 0.3;// Over Bought
input bool OPENBAR         = false;// Trading on newbar open price
//+------------------------------------------------------------------+

//-- time frame|indicator
double dmrk[5];
int signal  =-1;//-- 0.buy 1.sell
int hold = 0;


//-- order
int ticket  =0;
double lot  =0.0;
int typ     =-1;




//-- pair
datetime t1=0;
bool newbar=false;
bool entry =false;


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnInit()
  {
   ArrayInitialize(dmrk,0.0);
  //---
      const double test_lot   = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
      if(lt<test_lot)   lt    = test_lot;
  }



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(t1!=iTime(Symbol(),TF,0))
     {
      t1=iTime(Symbol(),TF,0);
      newbar=true;//set
     }
   else
     {
      if(OPENBAR) newbar=false;//reset
      else        newbar=true;
     }
//---------------------------------------------------------------------------
   signal = -1;
//---------------------------------------------------------------------------


//---calc
   for(int i=0; i<ArraySize(dmrk); i++)
     {
      dmrk[i]  =  iDeMarker(Symbol(),TF,period,i);
     }
//---


   if(dmrk[1] > OB)
     {
      hold = 1;//set
     }
   else
      if(dmrk[1] < OS)
        {
         hold = -1;//set
        }
      else
        {
         hold = 0;//reset
        }


   if(hold ==  1)
     {
      if(dmrk[0]<OB && dmrk[1]>OB)
        {
         signal = OP_SELL;
        }
     }
   if(hold == -1)
     {
      if(dmrk[0]>OS && dmrk[1]<OS)
        {
         signal = OP_BUY;
        }
     }


//---------------------------------------------------------------------------
   if(signal != -1)
      if(newbar==true)
         if(entry==false)//door open
           {
            //---
            entry =true;//set
            //---

            if(signal == OP_BUY)
              {
               ticket = OrderSend(Symbol(),OP_BUY,lt,Ask,(int)((Ask-Bid)/Point),
                                  sl>0?Bid-sl*Point:0.0,
                                  tp>0?Bid+tp*Point:0.0,
                                  EAName+":signal= "+IntegerToString(signal)+":hold= "+IntegerToString(hold),
                                  EANumber,
                                  0,
                                  clrBlue);
               signal=-1;
               //hold =0;
              }//reset
            else
               if(signal == OP_SELL)
                 {
                  ticket = OrderSend(Symbol(),OP_SELL,lt,Bid,(int)((Ask-Bid)/Point),
                                     sl>0?Ask+sl*Point:0.0,
                                     tp>0?Ask-tp*Point:0.0,
                                     EAName+":signal= "+IntegerToString(signal)+":hold= "+IntegerToString(hold),
                                     EANumber,
                                     0,
                                     clrRed);
                  signal=-1;
                  //hold =0;
                 }//reset signal

           }

//---------------------------------------------------------------------------

   if(entry == true) // closing
     {

      if(OrderSelect(ticket,SELECT_BY_TICKET))
        {
         if(OrderCloseTime() == 0)//-- order active trade
           {
            /*  todo condition to modify|close  */
            //entry = false;
           }
         //else
            if(OrderCloseTime() != 0)//--  close by 1. manual 2. sl/tp 3. ea
              {
               entry = false;//reset entry
              }
        }
     }

   Comment(
      "dmrk[0]= "+DoubleToString(dmrk[0],Digits)+"\n"+
      "\n"+
      "signal= "+IntegerToString(signal)+"     >0 \n"+
      "hold= "+IntegerToString(hold)+"         !0 "
   );

  }//-- End of OnTick




//+------------------------------------------------------------------+
