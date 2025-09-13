//+------------------------------------------------------------------+
//|                                        FT_BillWillams_Trader.mq4 |
//|                                                     FORTRADER.RU |
//|                                              http://FORTRADER.RU |
//+------------------------------------------------------------------+
#property copyright "FORTRADER.RU"
#property link      "http://FORTRADER.RU"

extern string FT1="------��������� ��������:----------";
extern int CountBarsFractal=5;//���������� ����� �� ������� ������� �������
extern int ClassicFractal=1; //��������� ���������� ������������� ��������
extern int MaxDistance=1000;//��������� �������� ���������� �� ������� ����� �� ����� �����
extern string FT2="------��������� ���� ������ ��������:----------";
extern int indent=1; //���������� ������� ��� ������� �� ��������� � ��������
extern int TypeEntry=2; //��� ����� ����� ������ �������� 1 - �� ������� ���� 2 - �� ���� �������� 3 �� ������ � ����� ����� ����� ������
extern int RedContol=1; //�������������� ��������� �� ��������� ���� ���� ���� ������ ������� �����
extern string FT3="------��������� ����������:----------";
extern int jaw_period=13;  // -   ������ ���������� ����� ����� (������� ����������). 
extern int jaw_shift=8;  // -   �������� ����� ����� ������������ ������� ����. 
extern int teeth_period=8;  // -   ������ ���������� ������� ����� (����� ����������). 
extern int teeth_shift=5;  // -   �������� ������� ����� ������������ ������� ����. 
extern int lips_period=5; //  -   ������ ���������� ������� ����� (��� ����������). 
extern int lips_shift=3;  // -   �������� ������� ����� ������������ ������� ����. 
extern int ma_method=0;   //- �� 0 �� 3 ����� ����������. ����� ���� ����� �� �������� ������� ����������� �������� (Moving Average). 
extern int applied_price=4; // - �� 0 �� 6  -   ������������ ����. ����� ���� ����� �� ������� ��������. 
extern string FT4="-------��������� �������� ������ �� ����������:----------";
extern int TrendAligControl=0; // ��������� �������� ������ �� ���������
extern int jaw_teeth_distense=10; //������� ����� ������� � �������
extern int teeth_lips_distense=10;//������� ����� ������� � ������
extern string FT5="-------��������� �������� �������� ������:----------";
extern int CloseDropTeeth=2; //��������� �������� ������ ��� ������� ��� ������ �������. 0 - ���������� 1 - �� ������� 2 �� �������� ����
extern int CloseReversSignal=2;//��������� �������� ������ ��� 1- ����������� ��������� �������� 2 - ��� ������������ ��������� �������� 0 ��������� 
extern string FT6="-------��������� ������������� StopLoss ������:----------";
extern int TrailingGragus=1; //��������� �������� ����� �� �������� ������� �������, ���� ������� ���� �� �������� �� �������, ���� ����� ���� �� �������� �� �������
extern int smaperugol=5;
extern int raznica=5;
extern string FT7="-------���������  StopLoss � TakeProfit ������ ������:----------";
extern double  StopLoss=50;
extern double  TakeProfit=50;
extern double  Lots=0.1;

int start()
  {

   ClassicFractal();
   return(0);
  }
  double oldopb,opb,ops,oldops, otkatb,otkats;
  int fractalnew,vpravovlevo,numsredbar,colish;
  
 int  ClassicFractal()
  {   int buy,sell;double sl,tp;
   

   //���������� ������
   ClassicFractalPosManager();
   
      buy=0;sell=0;
      for(int  i=0;i<OrdersTotal();i++)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY ){buy=1;}
      if(OrderType()==OP_SELL ){sell=1;}
      }  
      
   //������ ������� �������� ������ � � ����
   vpravovlevo=(CountBarsFractal-1)/2;
   numsredbar=(CountBarsFractal-vpravovlevo);
   colish=numsredbar-1;
   
   /*----------------------------------------�������------------------------------------------*/
   
   //������ ������� �� �������
   if(High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,numsredbar+1)] && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,1)] && (RedContol(Close[1],0)==true && RedContol==1))
   {
   opb=NormalizeDouble(High[numsredbar]+indent*Point,4);

   }
    //�������� ����� �� ������� ��� �� �������� ����
   if(buy==0&&  ((Ask>opb && TypeEntry==1 ) || (Close[1]>opb && TypeEntry==2)) 
   && opb!=oldopb && MaxDistance(opb)==true && opb>0 
   && ((RedContol(Close[1],0)==true && RedContol==1) || RedContol==0)
   && ((TrendAligControl(0)==true && TrendAligControl==1) || TrendAligControl==0))
   {oldopb=opb;
   sl=NormalizeDouble(Ask-StopLoss*Point,4);
   tp=NormalizeDouble(Ask+TakeProfit*Point,4);
   OrderSend(Symbol(),OP_BUY,Lots,Ask,3,sl,tp,"FORTRADER.RU",16384,10,Green);
   }
   
   /*------------------------------------------�������----------------------------------------*/
   
   //������ ������� �� �������
   if(Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,numsredbar+1)] && Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,0)]  && (RedContol(Close[1],1)==true && RedContol==1) )
   {
   ops=NormalizeDouble(Low[numsredbar]-indent*Point,4);
  

   }
   //�������� ����� �� ������� ��� �� �������� ����
   if(sell==0&& ( (Bid<ops && TypeEntry==1) ||  (Close[1]<ops && TypeEntry==2))   
   && oldops!=ops && MaxDistance(ops)==true 
   && ((RedContol(Close[1],1)==true && RedContol==1) ||RedContol==0)
   && ((TrendAligControl(1)==true && TrendAligControl==1) || TrendAligControl==0))
   {
   oldops=ops;
   sl=NormalizeDouble(Bid+StopLoss*Point,4);
   tp=NormalizeDouble(Bid-TakeProfit*Point,4);
   OrderSend(Symbol(),OP_SELL,Lots,Bid,3,sl,tp,"FORTRADER.RU",16384,10,Green);
   }
   

  return(0);
  }

bool MaxDistance(double entryprice)
{

double lips=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,1);

if(MathAbs(entryprice-lips)<MaxDistance*Point){return(true);}
return(false);
}

bool RedContol(double entryprice,int  type)
{

double teeth=iMA(NULL,0,teeth_period,teeth_shift,ma_method,applied_price,1);

if(entryprice>teeth && type==0){return(true);}
if(entryprice<teeth && type==1){return(true);}
return(false);
}

bool TrendAligControl(int type)
{

double teeth=iMA(NULL,0,teeth_period,teeth_shift,ma_method,applied_price,1);
double lips=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,1);
double jaw=iMA(NULL,0,jaw_period,jaw_shift,ma_method,applied_price,1);


if(type==0 && lips-teeth>teeth_lips_distense*Point && teeth-jaw>jaw_teeth_distense*Point ){return(true);}
if(type==1 && teeth-lips>teeth_lips_distense*Point && jaw-teeth>jaw_teeth_distense*Point ){return(true);}


return(false);
}

int ClassicFractalPosManager()
{int i,buy,sell;
double jaw=iMA(NULL,0,jaw_period,jaw_shift,ma_method,applied_price,1);
double teeth=iMA(NULL,0,teeth_period,teeth_shift,ma_method,applied_price,1);
double lips=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,1);
double lipsl=iMA(NULL,0,lips_period,lips_shift,ma_method,applied_price,2);
double sma=iMA(NULL,0,smaperugol,0,MODE_SMA,PRICE_CLOSE,1);
double smal=iMA(NULL,0,smaperugol,0,MODE_SMA,PRICE_CLOSE,2);

      buy=0;sell=0;
      for(  i=0;i<OrdersTotal();i++)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY ){buy=1;}
      if(OrderType()==OP_SELL ){sell=1;}
      }  

   for( i=1; i<=OrdersTotal(); i++)          
     {
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
            if(OrderType()==OP_BUY && ((CloseDropTeeth==1 && Bid<=jaw ) ||  (CloseDropTeeth==2 && Close[1]<=jaw )))
            { 
            OrderClose(OrderTicket(),Lots,Bid,3,Violet); 
            return(0);
            }
          }
          
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
            if(OrderType()==OP_BUY && 
            ((CloseReversSignal==1 && Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,numsredbar+1)] && Low[numsredbar]<Low[iLowest(NULL,0,MODE_LOW,colish,0)] ) 
            ||(CloseReversSignal==2 && sell==1 )))
            { 
            OrderClose(OrderTicket(),Lots,Bid,3,Violet); 
            return(0);
            }
          }
          
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_BUY && TrailingGragus==1 && lips-lipsl>sma-smal && OrderProfit()>0)
             {
             if(OrderStopLoss()<lips)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),lips,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
           
           
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_BUY && TrailingGragus==1 && lips-lipsl<=sma-smal && OrderProfit()>0)
             {
             if(OrderStopLoss()<teeth || lips>teeth)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),teeth,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
         
         if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
           if(OrderType()==OP_SELL && ((CloseDropTeeth==1 && Ask>=jaw ) ||  (CloseDropTeeth==2 && Close[1]>=jaw )))
           {
           OrderClose(OrderTicket(),Lots,Ask,3,Violet); 
           return(0);
           }
         }
          
          if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         { 
           if(OrderType()==OP_SELL && ((CloseReversSignal==1 && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,numsredbar+1)] && High[numsredbar]>High[iHighest(NULL,0,MODE_HIGH,colish,1)]) 
           ||  (CloseReversSignal==2 && buy==1  )))
           {
           OrderClose(OrderTicket(),Lots,Ask,3,Violet); 
           return(0);
           }
         }     
         
         
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_SELL && TrailingGragus==1 && lipsl-lips<smal-sma && OrderProfit()>0)
             {
             if(OrderStopLoss()>lips)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),lips,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
           
           
        if (OrderSelect(i-1,SELECT_BY_POS)==true) 
         {
            if(OrderType()==OP_SELL && TrailingGragus==1 && lipsl-lips>smal-sma && OrderProfit()>0)
             {
             if(OrderStopLoss()>teeth || lips<teeth)
              {
              OrderModify(OrderTicket(),OrderOpenPrice(),teeth,OrderTakeProfit(),0,White);
              return(0);
              }  
             } 
          }
                      
       }
       

}