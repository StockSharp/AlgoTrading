//----------------------------------------------------------------------+
//|                                         "NeuroNirvaman.mq4"         |
//|                             Copyright © 2008, Gabriel Jaime Mejía A.|
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2008, Gabriel Jaime Mejía Arbelaez"

/*-----------------------------------------------------------------
you can change the code. you can add your code, you can publish your changes
but please dont erase my name from the Copyright Gabriel Jaime Mejía Arbelaez
thanks.

if you ever make money with this EA please give some of your fortune to the more 
needed of my country. Thats  fair in exchange to what I am giving to you.

https://pagos.conexioncolombia.com/home.aspx

I believe in good things happen to you when you do good things to other people.

just try it. ;-)
  
//---------------------------------------------------------------*/



//---- input parameters for Neuro part



double              Kmax = 50.6;


extern double       SSP = 7;
extern double       periods=14;
extern double       distancia=0;
extern int          x11 = 100;
extern int          x12 = 100;
extern double       tp1 = 100;
extern double       sl1 = 50;




extern double       SSP2 = 7;
extern double       periods2=14;
extern double       distancia2=0;
extern int          x21= 100;
extern int          x22 = 100;
extern double       tp2 = 100;
extern double       sl2 = 50;



extern double       distancia3=0;
extern double       distancia4=0;
extern double       periods3=14;
extern double       periods4=14;
extern int          x31 = 100;
extern int          x32 = 100;



//---- input parameters
extern int          pass = 4;
extern double       lots = 0.1;
extern int          mn = 555;

static int          prevtime = 0;
static double       tp = 100;
static double       sl = 50;

//--------------------------------





//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if (Time[0] == prevtime) return(0);
   prevtime = Time[0];
 
   if (! IsTradeAllowed()) {
      again();
      return(0);
   }

  
  

  
  
  
  

   int total = OrdersTotal();
   for (int i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == mn) return(0);
   }
 

   int perceptron = Supervisor();
   int ticket = -1;
 
   if ( perceptron > 0) {
      ticket = OrderSend(Symbol(), OP_BUY, lots, Ask, 1, Bid - sl * Point, Bid + tp * Point, WindowExpertName(), mn, 0, Blue);
      Alert("SEÑAL:"+Symbol());
      if (ticket < 0) {
         again();    
      }
   }
 
   if (perceptron < 0) {
      ticket = OrderSend(Symbol(), OP_SELL, lots, Bid, 1, Ask + sl * Point, Ask - tp * Point, WindowExpertName(), mn, 0, Red);
      Alert("SEÑAL:"+Symbol());
      if (ticket < 0) {
         again();
      }
   }
 
   return(0);
}


//+------------------------------------------------------------------+
//| calculate perciptrons value                                      |
//+------------------------------------------------------------------+
double Supervisor() {


   if (pass == 3) {
      if (perceptron3() > 0) {
         if (perceptron2() > 0) {    
            tp = tp2;
            sl = sl2;
            return(1);
         }
       } else {
         if (perceptron1() < 0) {
            tp = tp1;
            sl = sl1;
          
            return(-1);
         }
      }
      return(0);
   }

   if (pass == 2) {
      if (perceptron2() > 0) {
         tp = tp2;
         sl = sl2;
 
         return(1);
       } else {
         return(-1);
       }
   }

   if (pass == 1) {
      if (perceptron1() < 0) {
         tp = tp1;
         sl = sl1;
 
         return(-1);
       } else {
         return(1);
       }

   }

   return(0);
}

double perceptron3() 
{

   double       w1 = x31 - 100;
   double       w2 = x32 - 100;

 
   double a1=0;
   double a2=0;
 
 
   double Laguerre=0;
   double Laguerre2=0;
   Laguerre = iCustom(NULL, 0, "Laguerre PlusDi",periods3 ,0, 0);
   Laguerre2 = iCustom(NULL, 0, "Laguerre PlusDi",periods4 ,0, 0);
  
  
   if(Laguerre>(0.5+(distancia3/100)))
   {
  
   a1=-1;
  
   }
   if(Laguerre<(0.5-(distancia3/100)))
   {
  
   a1=1;
  
   }
  
  
  
  
   if(Laguerre2>(0.5+(distancia4/100)))
   {
  
   a2=-1;
  
   }
   if(Laguerre2<(0.5-(distancia4/100)))
   {
  
   a2=1;
  
   }
  
  
  
 
  
  
   return(w1 * a1 + w2 * a2  );
  
  
  
}



double perceptron2() 
  {




   double       w2 = x21 - 100;
   double       w4 = x22 - 100;

         
   
   double a2 = tension2();
   double a4 = getSilvertrend2();
   return(w2 * a2 + w4 * a4);
 
}





double perceptron1()   {


 

   double       w2 = x11 - 100;
   double       w4 = x12 - 100;

 

   double a2 = tension();
   double a4 = getSilvertrend();
   return(w2 * a2 + w4 * a4);


}


//+------------------------------------------------------------------+
//| tension value                                                    |
//+------------------------------------------------------------------+

int tension()
{


 
   double Laguerre=0;
   Laguerre = iCustom(NULL, 0, "Laguerre PlusDi",periods ,0, 0);
   if(Laguerre>(0.5+(distancia/100)))
   {
  
   return(-1);
  
   }
   
   
   if(Laguerre<(0.5-(distancia/100)))
   {
  
   return(1);
  
   }
   return(0);


}



//+------------------------------------------------------------------+
//| tension value                                                    |
//+------------------------------------------------------------------+

int tension2()
{


 
   double Laguerre=0;
   Laguerre = iCustom(NULL, 0, "Laguerre PlusDi",periods2 ,0, 0);
   if(Laguerre>(0.5+(distancia2/100)))
   {
  
   return(-1);
  
   }
   if(Laguerre<(0.5-(distancia2/100)))
   {
  
   return(1);
  
   }
   return(0);


}




//+------------------------------------------------------------------+
//| Calculate Silvertrend value                                             |
//+------------------------------------------------------------------+
int getSilvertrend()
 {
 
 
   double buff0=0;
   double buff1=0;
 
   buff0=iCustom(NULL, 0, "Sv2",SSP,Kmax,False,13000,0,0);
   buff1=iCustom(NULL, 0, "Sv2",SSP,Kmax,False,13000,1,0);
 
 
   if(buff0==0 && buff1==1)
    return(-1);
 
   if(buff0==1 && buff1==0)
     return(1);

}


//+------------------------------------------------------------------+
//| Calculate Silvertrend2 value                                             |
//+------------------------------------------------------------------+
int getSilvertrend2()
 {
 
 
   double buff0=0;
   double buff1=0;
 
   buff0=iCustom(NULL, 0, "Sv2",SSP2,Kmax,False,13000,0,0);
   buff1=iCustom(NULL, 0, "Sv2",SSP2,Kmax,False,13000,1,0);
 
 
   if(buff0==0 && buff1==1)
    return(-1);
 
   if(buff0==1 && buff1==0)
     return(1);

}



//+------------------------------------------------------------------+
//| pause and try to do expert again                                 |
//+------------------------------------------------------------------+
void again() {
   prevtime = Time[1];
   Sleep(30000);
}