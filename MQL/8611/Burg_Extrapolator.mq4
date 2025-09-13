//+--------------------------------------------------------------------------------------+
//|                                                                Burg Extrapolator.mq4 |
//|                                                               Copyright © 2008, gpwr |
//|                                                                   vlad1004@yahoo.com |
//+--------------------------------------------------------------------------------------+
#property copyright "Copyright © 2008, gpwr"
//Global constants
#define MNo 0
//Input parameters
extern double   MaxRisk     =0.5;  //Maximum risk for all trades at any time
extern int      ntmax       =5;    //Maximum number of trades in one direction
extern int      MinProfit   =160;  //Open positions if predicted profit >= MinProfit; 150
extern int      MaxLoss     =130;  //Maximum allowed loss; 3
extern int      TakeProfit  =0;    //0: disable; >0: enable
extern int      StopLoss    =180;  //0: disable; >0: enable
extern int      TrailingStop=10;   //0: disable; >0: enable (StopLoss must be enabled too)
extern int      PastBars    =200;  //Number of past bars
extern double   ModelOrder  =0.37; //Order of Burg model as a fraction of PastBars
extern bool     UseMOM      =true; //Enable Logarithmic Momentum: mom(i)=log[p(i)/p(i-1)]
extern bool     UseROC      =false;//Enable Rate of Change: roc=100*(p(i)/p(i-1)-1)

//Global parameters----------------------------------------------------------------------+
int PrevBars,InitBars,Run;
int np,nf,lb,no;
double p[],av;

//Initialize-----------------------------------------------------------------------------+
int init()
{
   PrevBars=Bars;
   Run=1;

   np=PastBars;
   no=ModelOrder*PastBars;
   nf=np-no-1;
   
   return(0);
}

//Start----------------------------------------------------------------------------------+
int start()
{
//Calculate initial values (index 0 in p[] corresponds to the oldest price)
   if(Run==1) 
   {
      InitBars=Bars;
      double pf[],a[];
      ArrayResize(p,np);
      ArrayResize(pf,nf+1);
      ArrayResize(a,no+1);
      av=0.0;
      for(int i=0;i<np;i++)
      {
         if(UseMOM) p[np-1-i]=MathLog(Open[i]/Open[i+1]);
		   else if(UseROC) p[np-1-i]=Open[i]/Open[i+1]-1.0;
		   else av+=Open[i];
      }
      av/=np;
   }
   
//Run calculations only for the first time or if a new bar started
   if(Run==1 || Bars>PrevBars)
   {
   
   //Update input data
      if(Bars>InitBars)
      {
         if(UseMOM || UseROC)
         {
            for(i=0;i<np-1;i++) p[i]=p[i+1];
            if(UseMOM) p[np-1]=MathLog(Open[0]/Open[1]);
		      else if(UseROC) p[np-1]=Open[0]/Open[1]-1.0;
         }
         else av+=(Open[0]-Open[np])/np;
      }
      if(!(UseMOM || UseROC))
         for(i=0;i<np;i++) p[np-1-i]=Open[i]-av;
         
      
   //Find LP coefficients and predictions
   //index 0 in pf[] corresponds to the first predicted price
      Burg(a);
      for(int n=np-1;n<np+nf;n++)
      {
         double sum=0.0;
         for(i=1;i<=no;i++)
            if(n-i<np) sum-=a[i]*p[n-i];
            else sum-=a[i]*pf[n-i-np+1];
         pf[n-np+1]=sum;
      }
      if(UseMOM || UseROC) pf[0]=Open[0];
      else pf[0]+=av;
      for(i=1;i<=nf;i++)
      {
         if(UseMOM) pf[i]=pf[i-1]*MathExp(pf[i]);
		   else if(UseROC) pf[i]=pf[i-1]*(1.0+pf[i]);
		   else pf[i]+=av;
      }
   
   //Find trading signals
      double ymax=pf[0];
      double ymin=pf[0];
      int imax=0;
      int imin=0;
      int OpenSignal=0; // 1 = open long, -1 = open short, 0 = no action
      int CloseSignal=0; // 1 = close short, -1 = close long, 0 = no action
      for(i=1;i<np;i++)
      {  
         if(pf[i]>ymax && OpenSignal==0)
         {
            ymax=pf[i];
            imax=i;
            if(imin==0 && ymax-ymin>=MaxLoss*Point)CloseSignal=1;
            if(imin==0 && ymax-ymin>=MinProfit*Point)OpenSignal=1;
         }  
         if(pf[i]<ymin  && OpenSignal==0)
         {
            ymin=pf[i];
            imin=i;
            if(imax==0 && ymax-ymin>=MaxLoss*Point)CloseSignal=-1;
            if(imax==0 && ymax-ymin>=MinProfit*Point)OpenSignal=-1;
         } 
      }
      if(Run==1) for(i=0;i<=nf;i++) Print(DoubleToStr(pf[i],4));

   //Begin Trading--------------------------------------------------------------------------+
      double   lots,SL,TP;
      double   Spread   =Ask-Bid;
      int      Slippage =Spread/Point;
      int      nt       =OrdersTotal();
 
   //Closing LONG positions-----------------------------------------------------------------+ 
      if(OrdersTotal()>0)  
      if(OrderSelect(0,SELECT_BY_POS) && OrderType()==OP_BUY && (CloseSignal==-1 || OpenSignal==-1))
      {
         for(i=nt-1;i>=0;i--) 
            if(OrderSelect(i,SELECT_BY_POS) && OrderType()==OP_BUY)  
               OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Violet);
      }
   //Closing SHORT positions----------------------------------------------------------------+ 
      else if(OrderSelect(0,SELECT_BY_POS) && OrderType()==OP_SELL && (CloseSignal==1 || OpenSignal==1))      
      {
         for(i=nt-1;i>=0;i--) 
            if(OrderSelect(i,SELECT_BY_POS) && OrderType()==OP_SELL)
               OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Violet);
      }
      nt=OrdersTotal();
      
   //Modifying stop-loss of open orders------------------------------------------------------+
      if(TrailingStop>0 && StopLoss>0)
         for(i=0;i<nt;i++)
         {
            if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
            if(OrderType()==OP_BUY)
            {
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Blue);
            }
            else if(OrderType()==OP_SELL)
            {
               if(Ask+OrderOpenPrice()<Point*TrailingStop)
                  if(OrderStopLoss()>Ask+Point*TrailingStop)
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
            }
      }
      
   //Sending OPEN LONG order----------------------------------------------------------------+ 
      if(OpenSignal==1 && nt<ntmax)
      {
         lots=Lts(nt);
         if(lots>0.0)
         {
            if(StopLoss!=0) SL=Ask-StopLoss*Point;
            else SL=0;
            if(TakeProfit!=0) TP=Ask+TakeProfit*Point;
            else TP=0;
            OrderSend(Symbol(),OP_BUY,lots,Ask,Slippage,SL,TP,TimeToStr(Time[0]),MNo,0,Blue);
         }
      }
     
   //Sending OPEN SHORT order---------------------------------------------------------------+ 
      if(OpenSignal==-1 && nt<ntmax)      
      {
         lots=Lts(nt);
         if(lots>0.0)
         {
            if(StopLoss!=0) SL=Bid+StopLoss*Point;
            else SL=0;
            if(TakeProfit!=0) TP=Bid-TakeProfit*Point;
            else TP=0;
            OrderSend(Symbol(),OP_SELL,lots,Bid,Slippage,SL,TP,TimeToStr(Time[0]),MNo,0,Red);
         }  
      }
   }
   PrevBars=Bars;
   Run++;
   return(0);
}
// Lots Calculation Function--------------------------------------------------------------+
double Lts(int nt)
{
   double MinLot     =MarketInfo(Symbol(),MODE_MINLOT);
   double MaxLot     =MarketInfo(Symbol(),MODE_MAXLOT);
   double FreeMargin =AccountFreeMargin();  
   double LotRqdMgn  =MarketInfo(Symbol(),MODE_MARGINREQUIRED);//required margin per 1 lot
   double Step       =MarketInfo(Symbol(),MODE_LOTSTEP);
    
   double frac=1.0/(ntmax/MaxRisk-nt);
   double lots=MathFloor(FreeMargin*frac/LotRqdMgn/Step)*Step;
   if(lots<MinLot) lots=MinLot;
   if(lots>MaxLot) lots=MaxLot;              
   if(lots*LotRqdMgn>FreeMargin) lots=0.0; //not enough money
   return(lots);
}
//+-------------------------------------------------------------------------+
void Burg(double& a[])
{
   double df[],db[];
   ArrayResize(df,np);
   ArrayResize(db,np);
   int i,k,kh,ki;
   double tmp,num,den,r;
   den=0.0;
   for(i=0;i<np;i++) den+=p[i]*p[i];
   den*=2.0;
   for(i=0;i<np;i++)
   {
      df[i]=p[i];
      db[i]=p[i];
   }
   r=0.0;
   //Main loop
   for(k=1;k<=no;k++)
   {
      //Calculate reflection coefficient
      num=0.0;
      for(i=k;i<np;i++) num+=df[i]*db[i-1];
      den=(1-r*r)*den-df[k-1]*df[k-1]-db[np-1]*db[np-1];
      r=-2.0*num/den;
      //Calculate prediction coefficients
      a[k]=r;
      kh=k/2;
      for(i=1;i<=kh;i++)
	   {
	     ki=k-i;  
	     tmp=a[i];
	     a[i]+=r*a[ki];
	     if(i!=ki) a[ki]+=r*tmp;
	   }
	   if(k<no)
         //Calculate new residues
         for(i=np-1;i>=k;i--)
         {
            tmp=df[i];
            df[i]+=r*db[i-1];
            db[i]=db[i-1]+r*tmp;
         }
   }
}


