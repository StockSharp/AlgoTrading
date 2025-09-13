//+------------------------------------------------------------------+
//|                                                _Fuzzy logic_.mq4 |
//|                                          Copyright © 2007, B@ss. |
//|                                               albass@mail333.com |
//+------------------------------------------------------------------+
#define MAGICMA  16419780400

double Lots               =  0.1;
extern int TrailingStop          =  35;
extern double PercentMM          =  8;
extern double  DeltaMM           =  0;
extern int     InitialBalance    =  10000;
bool UseMM                       =  true;
extern double SL                 =  60;
bool FirstSL = true;

//######################################################################################################################################
double LotsOptimized()
   {
      double volume,TempVolume, F;  
      TempVolume=Lots;
      
      if (UseMM) TempVolume =0.00001*(AccountBalance()*(PercentMM+DeltaMM)-InitialBalance*DeltaMM); 
      
      volume=NormalizeDouble(TempVolume,2);
         
      if (volume>MarketInfo(Symbol(),MODE_MAXLOT)) volume=MarketInfo(Symbol(),MODE_MAXLOT);
      if (volume<MarketInfo(Symbol(),MODE_MINLOT)) volume=MarketInfo(Symbol(),MODE_MINLOT);
          
      return (volume);      
   }
//######################################################################################################################################
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
   if(buys>0) return(buys);
   else       return(-sells);
  }
//######################################################################################################################################
double FuzzyLogic()
  {
      double Gator, Gator2, SumGator, WPR, AC1, AC2, AC3, AC4, AC5, tempAC_b, tempAC_s, DeMarker, RSI, Decision;
      double Rang[5,5], Summary[5];
      int x, y;
      double arGator[7]    ={10,20,30,40,40,30,20,10};
      double arWPR[7]      ={-95,-90,-80,-75,-25,-20,-10,-5};
      double arAC[7]       ={5,4,3,2,2,3,4,5};
      double arDeMarker[7] ={0.15,0.2,0.25,0.3,0.7,0.75,0.8,0.85};
      double arRSI[7]      ={25,30,35,40,60,65,70,75};
      double Weight[7]     ={0.133,0.133,0.133,0.268,0.333};
            
      Gator    =iGator(NULL,0,13,8,8,5,5,3,MODE_SMMA,PRICE_MEDIAN,MODE_UPPER,1); 
      Gator2   =iGator(NULL,0,13,8,8,5,5,3,MODE_SMMA,PRICE_MEDIAN,MODE_LOWER,1);       
      SumGator =MathAbs(Gator)+MathAbs(Gator2);
      
      WPR      =iWPR(NULL,0,14,1);      
      DeMarker =iDeMarker(NULL,0,14,1);
      RSI      =iRSI(NULL,0,14,PRICE_CLOSE,1);
      
      AC1      =iAC(NULL,0,1);
      AC2      =iAC(NULL,0,2);
      AC3      =iAC(NULL,0,3);
      AC4      =iAC(NULL,0,4);
      AC5      =iAC(NULL,0,5);
            
      ArrayInitialize(Rang,0);
      ArrayInitialize(Summary,0);
      
      //построение нечеткого классификатора
//1)=========================================================Gator==================================================      
      if (SumGator<arGator[0]){Rang[0,0]=0.5;Rang[0,4]=0.5;}                                                 
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (SumGator>=arGator[0] && SumGator<arGator[1])
               {
                  Rang[0,0]=(1-(SumGator-arGator[0])/(arGator[1]-arGator[0]))/2;
                  Rang[0,1]=(1-Rang[0,0]*2)/2;
                  
                  Rang[0,4]=Rang[0,0];
                  Rang[0,3]=Rang[0,1];
               }
      //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (SumGator>=arGator[1] && SumGator<arGator[2]){Rang[0,1]=0.5;Rang[0,3]=0.5;}
      //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (SumGator>=arGator[2] && SumGator<arGator[3])
               {
                  Rang[0,1]=(1-(SumGator-arGator[2])/(arGator[3]-arGator[2]))/2;
                  Rang[0,2]=1-Rang[0,1]*2;
                  
                  Rang[0,3]=Rang[0,1]; 
               }
      //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (SumGator>=arGator[3] || SumGator>=arGator[4]){Rang[0,2]=1;}      
      //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//2)========================================================WPR=======================================================
      if (WPR<arWPR[0]){Rang[1,0]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[0] && WPR<arWPR[1])
               {
                  Rang[1,0]=1-(WPR-arWPR[0])/(arWPR[1]-arWPR[0]);
                  Rang[1,1]=1-Rang[1,0];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[1] && WPR<arWPR[2]){Rang[1,1]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[2] && WPR<arWPR[3])
               {
                  Rang[1,1]=1-(WPR-arWPR[2])/(arWPR[3]-arWPR[2]);
                  Rang[1,2]=1-Rang[1,1];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[3] && WPR<arWPR[4]){Rang[1,2]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[4] && WPR<arWPR[5])
               {
                  Rang[1,2]=1-(WPR-arWPR[4])/(arWPR[5]-arWPR[4]);
                  Rang[1,3]=1-Rang[1,2];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[5] && WPR<arWPR[6]){Rang[1,3]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[6] && WPR<arWPR[7])
               {
                  Rang[1,3]=1-(WPR-arWPR[6])/(arWPR[7]-arWPR[6]);                  
                  Rang[1,4]=1-Rang[1,3];                  
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (WPR>=arWPR[7]){Rang[1,4]=1;}         
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//3)============================================================AC=====================================================     
      if (AC1<AC2 && AC1<0 && AC2<0){tempAC_b=2;}
      if (AC1<AC2 && AC2<AC3 && AC1<0 && AC2<0 && AC3<0){tempAC_b=3;}
      if (AC1<AC2 && AC2<AC3 && AC3<AC4 && AC1<0 && AC2<0 && AC3<0 && AC4<0){tempAC_b=4;}
      if (AC1<AC2 && AC2<AC3 && AC3<AC4 && AC4<AC5 && AC1<0 && AC2<0 && AC3<0 && AC4<0 && AC5<5){tempAC_b=5;}
      
      if (AC1>AC2 && AC1>0 && AC2>0){tempAC_s=2;}      
      if (AC1>AC2 && AC2>AC3 && AC1>0 && AC2>0 && AC3>0){tempAC_s=3;}
      if (AC1>AC2 && AC2>AC3 && AC3>AC4 && AC1>0 && AC2>0 && AC3>0 && AC4>0){tempAC_s=4;}
      if (AC1>AC2 && AC2>AC3 && AC3>AC4 && AC4>AC5 && AC1>0 && AC2>0 && AC3>0 && AC4>0 && AC5>0){tempAC_s=5;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (tempAC_b==arAC[0] || tempAC_b==arAC[1]){Rang[2,0]=1;}      
      if (tempAC_b==arAC[2] || tempAC_b==arAC[3]){Rang[2,1]=1;}
      
      if (tempAC_s==arAC[4] || tempAC_s==arAC[5]){Rang[2,3]=1;}
      if (tempAC_s==arAC[6] || tempAC_s==arAC[7]){Rang[2,4]=1;}      
      
      if (Rang[2,0]==0 && Rang[2,1]==0 && Rang[2,3]==0 && Rang[2,4]==0){Rang[2,2]=1;}
      //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++     
//4)=========================================================DeMarker==================================================
      if (DeMarker<arDeMarker[0]){Rang[3,0]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[0] && DeMarker<arDeMarker[1])
               {
                  Rang[3,0]=1-(DeMarker-arDeMarker[0])/(arDeMarker[1]-arDeMarker[0]);
                  Rang[3,1]=1-Rang[3,0];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[1] && DeMarker<arDeMarker[2]){Rang[3,1]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[2] && DeMarker<arDeMarker[3])
               {
                  Rang[3,1]=1-(DeMarker-arDeMarker[2])/(arDeMarker[3]-arDeMarker[2]);
                  Rang[3,2]=1-Rang[3,1];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[3] && DeMarker<arDeMarker[4]){Rang[3,2]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[4] && DeMarker<arDeMarker[5])
               {
                  Rang[3,2]=1-(DeMarker-arDeMarker[4])/(arDeMarker[5]-arDeMarker[4]);
                  Rang[3,3]=1-Rang[3,2];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[5] && DeMarker<arDeMarker[6]){Rang[3,3]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[6] && DeMarker<arDeMarker[7])
               {
                  Rang[3,3]=1-(DeMarker-arDeMarker[6])/(arDeMarker[7]-arDeMarker[6]);
                  Rang[3,4]=1-Rang[3,3];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (DeMarker>=arDeMarker[7]){Rang[3,4]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

//5)==========================================================RSI======================================================
      if (RSI<arRSI[0]){Rang[4,0]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[0] && RSI<arRSI[1])
               {
                  Rang[4,0]=1-(RSI-arRSI[0])/(arRSI[1]-arRSI[0]);
                  Rang[4,1]=1-Rang[4,0];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[1] && RSI<arRSI[2]){Rang[4,1]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[2] && RSI<arRSI[3])
               {
                  Rang[4,1]=1-(RSI-arRSI[2])/(arRSI[3]-arRSI[2]);
                  Rang[4,2]=1-Rang[4,1];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[3] && RSI<arRSI[4]){Rang[4,2]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[4] && RSI<arRSI[5])
               {
                  Rang[4,2]=1-(RSI-arRSI[4])/(arRSI[5]-arRSI[4]);
                  Rang[4,3]=1-Rang[4,2];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[5] && RSI<arRSI[6]){Rang[4,3]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[6] && RSI<arRSI[7])
               {
                  Rang[4,3]=1-(RSI-arRSI[6])/(arRSI[7]-arRSI[6]);
                  Rang[4,4]=1-Rang[4,3];
               }
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if (RSI>=arRSI[7]){Rang[4,4]=1;}
      //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++            
//________________________________________________________________свертка для рангов__________________________________________________      
      for(x=0;x<4;x++)
            {
               for(y=0;y<4;y++)
                  {Summary[x]=Summary[x]+Rang[y,x]*Weight[x];}
                  if (Summary[x]>1) {Print (Summary[x]," x=",x);}
            }
      
      for(x=0;x<4;x++)
            {Decision=Decision+Summary[x]*(0.2*(x+1)-0.1);}
            
      //Print("Gator-     ",SumGator,"==",Rang[0,0],"--",Rang[0,1],"--",Rang[0,2],"--",Rang[0,3],"--",Rang[0,4]);
      //Print("WPR-       ",WPR,"==",Rang[1,0],"--",Rang[1,1],"--",Rang[1,2],"--",Rang[1,3],"--",Rang[1,4]);
      //Print("tempAC_b- ",tempAC_b,"       ","tempAC_s- ",tempAC_s,"    ==",Rang[2,0],"--",Rang[2,1],"--",Rang[2,2],"--",Rang[2,3],"--",Rang[2,4]);
      //Print("DeMarker-  ",DeMarker,"==",Rang[3,0],"--",Rang[3,1],"--",Rang[3,2],"--",Rang[3,3],"--",Rang[3,4]);
      //Print("RSI-       ",RSI,"==",Rang[4,0],"--",Rang[4,1],"--",Rang[4,2],"--",Rang[4,3],"--",Rang[4,4]);
      
      return(Decision);
  }
//######################################################################################################################################
void CheckForOpen()
  {   
   int res;
   if(Volume[0]>1) return;  
   
   //Print (FuzzyLogic());
   
   if(FuzzyLogic()<0.25)  
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"",MAGICMA,0,Red);
      FirstSL=True;      
      return;
     }

   if(FuzzyLogic()>0.75)  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"",MAGICMA,0,Blue);
      FirstSL=True;
      return;
     }
  }
//######################################################################################################################################
void SetStopLoss()
   {
      double StopLoss, TakeProfit;
      int cnt1, err;
      bool tic;
      
      StopLoss=NormalizeDouble(SL*Point,0);      
      for(cnt1=0;cnt1<OrdersTotal();cnt1++)
            {
               OrderSelect(cnt1, SELECT_BY_POS, MODE_TRADES);
               if (OrderType()==OP_SELL && OrderStopLoss()!=OrderOpenPrice()+StopLoss && OrderSymbol()==Symbol())
                     {
                        tic=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+StopLoss,0,0,Green);                        
                     }
                if (OrderType()==OP_BUY && OrderStopLoss()!=OrderOpenPrice()-StopLoss && OrderSymbol()==Symbol())
                     {
                        tic=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-StopLoss,0,0,Green);                      
                     }
            }      
   }
//######################################################################################################################################
void start()
  {
   if(Bars<100 || IsTradeAllowed()==false) return;
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   
   if (FirstSL==True) {SetStopLoss();}
   int cnt1;
   
   for(cnt1=0;cnt1<OrdersTotal();cnt1++)
   {
   OrderSelect(cnt1,SELECT_BY_POS);
   if(OrderType()==OP_BUY)
           {
            if(TrailingStop>0)
              {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                 {
                  FirstSL=false;
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                    {OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);}
                 }
              }
           }
         else 
           {           
            if(TrailingStop>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  FirstSL=false;
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                    {OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);}
                 }
              }
           }
          }
  }