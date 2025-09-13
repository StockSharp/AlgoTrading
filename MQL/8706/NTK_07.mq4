/*
��� ��������:
������ ��� ���������� ������ �� ������� ���� �� ���������� netstep
���� ���� �� ��� �����������, �� ������ �������
������ ��� ���� ���������� � ����������� �������� ���� �� ���������� netstep, �� � ����� ���������� �� mul
� �.�. �� ��� ��� ���� �� ��������� ���������� ������� ������ maxtrades ��� LotLim
� ������ ����� ����� � ������� � ����������� ����, �.�. ������ ������� ����� � �� ����� �� �����
������� maxtrades, �����, ������� ������� �� ������������� �����, � mul �� ���� ������ ���� ������ ����, ����� �� ���� ����������� ����� �� ����� �����
������� ������� �������� ��������� �������� � ����������� ���������� ���������� ���� ����������
�� �����

�� ������� 1 ��� ������ ��������� ��������� ���� �� eurusd (�������� ����), ��������� �������� �������������� �� ������ 01.01.2008 - 01.11.2008.
������� ��������� �������� � ��������� ��� ������ � ������ �������� 2007 ����, ��� � �������������� ��������

��������� �� ������� � ��� ���� ���������� ���� ������� ��� �� ����� � ��� ������������ �����.
�������� ��������:
1. ������� ��������, ����� ��������� ������� �� ������ ����� ���� ����������� ����� ��������� ������, ����� ��� �����, ������� ����������� (�� ���������� � ����)
2. � ���� ���� ������� ���� ������ �� �����, �� ������� �� ����� �������� �� ����� ��������� ������� ...
(��������, �� ����� fxstart �� ������������� ������ (� ������� ���������), � �� forex4u ��� ��)
���� ���������� �� ����� ������, ��������� � �����������.
���������� ������������ ��������, set ����� ��� ������ ������������, ��������� ������
������� �� ���� ��������� ��� ���������, ���� ��� ������� ������� ��� � ��� ���� �������� ��������� ������.
� � ������ ���� ����� ���� ������� ��������� !
(�� ������ ��� �� �������/������, � ������ ��� IMHO ������ �����(�) ����� �� ��� �����) 

������, ���� ��� ����� ���� ���������, �� ��� ��� (351 ��) http://depositfiles.com/files/lxs1avv7l
(������� � �������� � ������ �����, ��� ����� ������)
*/
//
//
#property copyright "runik"
#property link      "ngb2008@mail.ru"

//---- input parameters
extern string  g1="�������� ���������";
extern int       netstep=23;
extern int       sl=115;
extern int       tp=300;
extern int       TrailingStop=75;
extern double    mul=1.7;
extern int       trailprofit=1;   // ����������� �� ������

extern string  gg="���������� ��������";

extern int       mm=2; 
                  // 0 - ���������� ���, 
                  // 1 - ����������� � % �� ���� � maxtrades, 
                  // 2 - ����������� � ����������� �� ��������������� ���� � maxtrades
extern double    Lots=1;
extern double    LotLim=7;
extern int       maxtrades=4;
extern double    percent=10;
extern double    minsum=5000;
extern int       bezub=0; // ���� 1, ����������� ����� � ���������
extern int       deltalast=5; // ���� ����� ���������� ��������� ������ ���� ������� �� 5 ������� � ������ �������, �� ��������� � ��������� 

extern string  bq="�������";

extern int       usema=0;
extern int    MovingPeriod       = 100;
extern int    MovingShift        = 0;

extern string  bb="������������ ����������";

extern int       chas1=0;        // ���� ������
extern int       chas2=24;
extern int       per=0;       // ������ � �����
extern int       center=0; // 0 - ������� �� ����� ���������, 1- ������� �� ������
extern int       c10=10; // ������������ ��� ���������� �����
extern int       c20=10000; // ������������ ��� ���������� ���

int oldtr=0;
double nulpoint=0;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----

  
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
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
 if (oldtr>TrailingStop) {TrailingStop=oldtr;}
//   ���������� ������ ����, ���-�� �� ����� ��������
if (mm==1)
   {
double lotsi=MathCeil(AccountBalance()/1000*percent/100);
  LotLim=lotsi;
  for(int t=1;t<=maxtrades;t++)   
    {
    lotsi=lotsi/mul;
    }
    lotsi=MathRound(lotsi*c10)/c10;    
    Lots=lotsi;
    }

if (mm==2)
   {
   lotsi=Lots;
  for(t=1;t<=maxtrades;t++)   
    {
    lotsi=lotsi*mul;
    }
    LotLim=lotsi;
    }    
    
    
    


if(DayOfWeek()==0 || DayOfWeek()==6)  return(0);// �� �������� � �������� ���.

if(AccountFreeMargin()<minsum) // ������ ���������
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }   
        
        

if (OrdersTotal()<1) // �������� !
  {
  if (Hour()<chas1 || Hour()>(chas2) || chas1>chas2) return(0);// ����� � ���������
  
     if (per>0 && center==0) 
        {   
          double ssmax=High[1];
          double ssmin=Low[1];
          for (int x=2;x<=per;x++)
            {
            if (ssmax < High[x]) ssmax=High[x];
            if (ssmin > Low[x]) ssmin=Low[x];
            }
         if (Ask>=ssmax || Ask<=ssmin)
            {
            int ticket=OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+netstep*Point,3,Ask+netstep*Point-sl*Point,Ask+netstep*Point+tp*Point,"BUYSTOP",0,0,Green);          
            ticket=OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-netstep*Point,3,Bid-netstep*Point+sl*Point,Bid-netstep*Point-tp*Point,"STOPLIMIT",0,0,Green);   
            }
        }
        
     if (per>0 && center==1) 
        {   
          ssmax=High[1];
          ssmin=Low[1];
          for (x=2;x<=per;x++)
            {
            if (ssmax < High[x]) ssmax=High[x];
            if (ssmin > Low[x]) ssmin=Low[x];
            }
         if (Ask==(ssmax+ssmin)/2 || Bid==(ssmax+ssmin)/2)
            {
            ticket=OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+netstep*Point,3,Ask+netstep*Point-sl*Point,Ask+netstep*Point+tp*Point,"BUYSTOP",0,0,Green);          
            ticket=OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-netstep*Point,3,Bid-netstep*Point+sl*Point,Bid-netstep*Point-tp*Point,"STOPLIMIT",0,0,Green);   
            }
        }        
  
    if (per==0)
    {
    ticket=OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+netstep*Point,3,Ask+netstep*Point-sl*Point,Ask+netstep*Point+tp*Point,"BUYSTOP",0,0,Green) ;        
    ticket=OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-netstep*Point,3,Bid-netstep*Point+sl*Point,Bid-netstep*Point-tp*Point,"STOPLIMIT",0,0,Green);  
    }
    
  }

int cnt=0;
int mode=0; 

double buylot=0;double buyprice=0;double buystoplot=0;double buystopprice=0;double buylimitlot=0;double buylimitprice=0;
double selllot=0;double sellprice=0;double sellstoplot=0;double sellstopprice=0;double selllimitlot=0;double selllimitprice=0;
double buysl=0;double buytp=0;double sellsl=0;double selltp=0;
int bt=-1;int bst=-1;int blt=-1;int st=-1;int sst=-1;int slt=-1;

if (OrdersTotal()>0)////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
{

  for(cnt=0;cnt<OrdersTotal();cnt++)   // ���������� ��������� �������
    {
    OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
    mode = OrderType(); 
    
    if (mode==OP_BUY )        
      {
      if (buylot<OrderLots())  // ���������� ��������� ������ �������� ��������� ������
        {     buylot=OrderLots();
              buyprice=OrderOpenPrice();
              bt=OrderTicket(); 
              buysl=OrderStopLoss(); 
              buytp=OrderTakeProfit(); 
        }  
      }     
           
           
    if (mode==OP_BUYSTOP )    {       buystoplot=OrderLots();    buystopprice=OrderOpenPrice();    bst=OrderTicket();  }         
    if (mode==OP_BUYLIMIT )   {       buylimitlot=OrderLots();   buylimitprice=OrderOpenPrice();   blt=OrderTicket();  }      
    
    if (mode==OP_SELL )       
      { 
      if (selllot<OrderLots())      // ���������� ��������� ������ �������� ��������� ������
         {    selllot=OrderLots();       
              sellprice=OrderOpenPrice();       
              st=OrderTicket(); 
              sellsl=OrderStopLoss(); 
              selltp=OrderTakeProfit(); 
         }      
      }     
    
    if (mode==OP_SELLSTOP )   {       sellstoplot=OrderLots();   sellstopprice=OrderOpenPrice();   sst=OrderTicket();  }     
    if (mode==OP_SELLLIMIT )  {       selllimitlot=OrderLots();  selllimitprice=OrderOpenPrice();  slt=OrderTicket();  }     
    
    }   
    
if (selllot>=Lots) // ���� ���� ����� ����
  {   
  if (bst>0) {OrderDelete(bst);      return(0);    }
  
  if (sellstopprice==0  && LotLim>MathRound(c10*selllot*mul)/c10) {
  ticket=OrderSend(Symbol(),OP_SELLSTOP,MathRound(c10*selllot*mul)/c10,sellprice-netstep*Point,3,sellprice-netstep*Point+sl*Point,sellprice-netstep*Point-tp*Point,"STOPLIMIT",0,0,Green);         return(0);}
  }
  
if (buylot>=Lots) // ���� ���� ����� �����
  {   

  if (sst>0) {OrderDelete(sst);      return(0);    }  

  if (buystopprice==0 && LotLim>MathRound(c10*buylot*mul)/c10) {
  ticket=OrderSend(Symbol(),OP_BUYSTOP,MathRound(c10*buylot*mul)/c10,buyprice+netstep*Point,3,buyprice+netstep*Point-sl*Point,buyprice+netstep*Point+tp*Point,"BUYSTOP",0,0,Green);               return(0);}

  }

if (buylot!=0 || selllot!=0) // ������������ �������� �������
  {
double tp1,sl1;

  for(cnt=0;cnt<OrdersTotal();cnt++)    // ����� ���� ��� ��� ������������� ����� ������� �����, ���� ����� ������ ������������ � ����� � ������� ��������
    {
    OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
    mode = OrderType(); tp1=OrderTakeProfit();sl1=OrderStopLoss();
    if (mode==OP_BUY) 
      {
      if (buytp>tp1 || buysl>sl1 ) 
        {
        OrderModify(OrderTicket(),OrderOpenPrice(),buysl,buytp,0,Purple);return(0);
        }        
      }     
    if (mode==OP_SELL) 
      {
      if (selltp<tp1 || sellsl<sl1 ) 
        {        
        OrderModify(OrderTicket(),OrderOpenPrice(),sellsl,selltp,0,Purple);return(0);
        
        }        
      } 
    
    }
      
  oldtr=TrailingStop;
  if (LotLim<MathRound(c10*selllot*mul)/c10 || LotLim<MathRound(c10*buylot*mul)/c10) // ��������� ����� ���������� ����������� ������, ���� ������ ���� � ������
    {     
    if(TrailingStop>0)
      {
         if (usema==1) 
         {
          double oldts=TrailingStop;
          double ma=iMA(NULL,0,MovingPeriod,MovingShift,MODE_SMA,PRICE_CLOSE,0);
             if (buylot>0 && Ask<ma){TrailingStop=MathRound(TrailingStop/2);}
             if (selllot>0 && Bid>ma){TrailingStop=MathRound(TrailingStop/2);}
             Print(Ask,"   ",ma,"   ",TrailingStop,"   ",buylot,"   ",selllot,"   ");
         }
         
         if (bezub==1) 
         {
           if (buyprice<(Ask+deltalast*Point)) 
             {
              OrderModify(bt,buyprice,nulfunc(),buytp,0,Green);return(0);   
             }
           if (sellprice>(Bid-deltalast*Point)) 
             {              
              OrderModify(st,sellprice,nulfunc(),selltp,0,Green);return(0);                 
             }             
         }      
      
      if(buysl>0)
        {
        if(High[0]-buysl>TrailingStop*Point+1*Point) // +1 - ����� �� ���� ERR_NO_RESULT 1 OrderModify �������� �������� ��� ������������� �������� ������ �� ����������.
          {   
            if (trailprofit==1) 
            {
            //Print(Ask,"   ",bt,"   ",buyprice,"   ",High[0]-TrailingStop*Point,"   ",buytp);
            OrderModify(bt,buyprice,High[0]-TrailingStop*Point,High[0]+tp*Point,0,Green);return(0);               
            }else
            {
            OrderModify(bt,buyprice,High[0]-TrailingStop*Point,buytp,0,Green);return(0);
            }
          }   
        }   
        if(sellsl>0)
        {
        if(sellsl-Low[0]>TrailingStop*Point+1*Point)
          {
          //Print(Ask,"   ",st,"   ",sellprice,"   ",Low[0]+TrailingStop*Point,"   ",selltp);
            if (trailprofit==1) 
            {
            OrderModify(st,sellprice,Low[0]+TrailingStop*Point,Low[0]-tp*Point,0,Green);return(0);
            
            } else 
            {          
            OrderModify(st,sellprice,Low[0]+TrailingStop*Point,selltp,0,Green);return(0);
            
            }
          }   
        }       
                     
      }//if(TrailingStop>0)
    
    }//if (LotLim<MathRound(c10*selllot*mul)/c10 || LotLim<MathRound(c10*buylot*mul)/c10) 
  
  } // if (buylot!=0 || selllot!=0)
  
  if (buylot==selllot && buylot==0) // ����� ��� ���������, �� ����� ����������
  {
  if (bst>0 && buystoplot!=Lots) {OrderDelete(bst);      return(0);    }
  if (slt>0 && selllimitlot!=Lots) {OrderDelete(slt);      return(0);    }
  if (blt>0 && buylimitlot!=Lots) {OrderDelete(blt);      return(0);    }
  if (sst>0 && sellstoplot!=Lots) {OrderDelete(sst);      return(0);    }            
  } 

  
} //if (OrdersTotal()>0)


 
//----
   return(0);
  }
//+------------------------------------------------------------------+


double nulfunc() // ��� �������� ����� �������������� ���� �������� ������� (���� ��� �� ������� �� ���� ���� �� ������� 0)
  {
    double np=0;double f=0; double p=0;double l=0; int m=0;
    for(int t1=0;t1<OrdersTotal();t1++)    
    {
    OrderSelect(t1, SELECT_BY_POS, MODE_TRADES); 
    m = OrderType();p=OrderOpenPrice();l=OrderLots();
    if  (m==OP_BUY || m==OP_SELL) 
      {
      np=np+l*p;  
      f=f+l;
      }
    }
    np=np/f;
    np=MathCeil(np*c20)/c20;
    Print(np);  
   return (np);
  }