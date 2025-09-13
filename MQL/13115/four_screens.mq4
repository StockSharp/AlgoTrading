//+------------------------------------------------------------------+
//|                                                     JobJobos.mq4 |
//|                                                             Ugar |
//|                                                     Ugar68@bk.ru |
//+------------------------------------------------------------------+
#property copyright "Ugar"
#property link      "Ugar68@bk.ru"
#property strict
#property version   "1.00"

//--- input parameters
extern int       SL=20;
extern int       TP=20;
extern int       Trailing=10;
extern bool      NowBarAfterProfit=true;
extern double    Lot=0.1;
extern int       Slippage=3;
extern int       Magic=120428;
//+------------------------------------------------------------------+
//���������� �� ���������� ������
string _name;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   _name=WindowExpertName();//��� ���������
   NowBar(0,true);//������������� ������� ������ ����
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
//+------------------------------------------------------------------+
//����������
   static double Heiken51,Heiken52;
   double Heiken151,Heiken152,Heiken301,Heiken302,Heiken601,Heiken602;
   double lot,sl=0,tp=0,op=0,oop,osl;
   int Signal=0,Spread,StopLevel,total,i,ticket,orders=0,LastHistOrder,cmd=0;
   static int Signal_;
   string SignalStr="���",SignalStr5,SignalStr15,SignalStr30,SignalStr60,alerts;
   bool OrderSendRun=false,nowbar;
   color arrow=Blue;
//+------------------------------------------------------------------+
//�����, ����������� ���� � �������� ������������ ������
//�����
   Spread=(int)MarketInfo(Symbol(),MODE_SPREAD);

//Stoplevel
   StopLevel=(int)MathMax(Spread,MarketInfo(Symbol(),MODE_STOPLEVEL))+Spread+1;

//�������� �� ������������ SL
   if(SL<=StopLevel)
     {
      alerts=StringConcatenate("SL ����� ���� ������ StopLevel ",StopLevel);
      Alert(alerts);
     }

//�������� �� ������������ TP
   if(TP<=StopLevel)
     {
      alerts=StringConcatenate("TP ����� ���� ������ StopLevel ",StopLevel);
      Alert(alerts);
     }
//+------------------------------------------------------------------+
//����� ��� �� �5
   nowbar=NowBar(5,false);
//+------------------------------------------------------------------+
//����������
//�� M5 ��������� ���������� ���� ��� �� ���
   if(nowbar)
     {
      Heiken51=iCustom(NULL,5,"Heiken Ashi",2,1);
      Heiken52=iCustom(NULL,5,"Heiken Ashi",3,1);
     }

//��������� �� M15
   Heiken151=iCustom(NULL,15,"Heiken Ashi",2,0);
   Heiken152=iCustom(NULL,15,"Heiken Ashi",3,0);

//��������� �� M30
   Heiken301=iCustom(NULL,30,"Heiken Ashi",2,0);
   Heiken302=iCustom(NULL,30,"Heiken Ashi",3,0);

//��������� �� M60
   Heiken601=iCustom(NULL,60,"Heiken Ashi",2,0);
   Heiken602=iCustom(NULL,60,"Heiken Ashi",3,0);
//+------------------------------------------------------------------+
//�������
   SignalStr5=signal(Heiken52,Heiken51);
   SignalStr15=signal(Heiken152,Heiken151);
   SignalStr30=signal(Heiken302,Heiken301);
   SignalStr60=signal(Heiken602,Heiken601);

//����� ������
   if(Heiken51<Heiken52 && Heiken151<Heiken152 && Heiken301<Heiken302 && Heiken601<Heiken602)
     {
      Signal=1;
      SignalStr="Buy";
     }

//����� ������
   if(Heiken51>Heiken52 && Heiken151>Heiken152 && Heiken301>Heiken302 && Heiken601>Heiken602)
     {
      Signal=-1;
      SignalStr="Sell";
     }

//����������� ��������� ����������� � ��������
   alerts=StringConcatenate("Heiken Ashi ��",
                            "\n","M5   "+SignalStr5,
                            "\n","M15 ",SignalStr15,
                            "\n","M30 ",SignalStr30,
                            "\n","M60 ",SignalStr60,
                            "\n","����� ������ ",SignalStr);
   Comment(alerts);
//+------------------------------------------------------------------+
//�������� �����
   if(!DCOk(30))return(0);
//+------------------------------------------------------------------+
//�������� ������� �������� ������� � ���������� ���
//��������� ����� ������
   total=OrdersTotal()-1;

//���� �������� �������
   for(i=total; i>=0; i--)
     {
      //����� ������
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         Print("����� �� ������, ������ = ",GetLastError());
         return(0);
        }

      //���� ����� ������ ������� ��� �������, ����������.
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic)continue;

      //���� �������� ������
      oop=OrderOpenPrice();

      //���� ������
      osl=OrderStopLoss();

      //���������� Buy ��������
      if(OrderType()==OP_BUY)
        {
         //���� �����
         orders++;

         //���� �������� ��������
         if(Trailing>0)
           {
            //���� ��� ���������
            sl=sltp(Trailing,Bid,-1);

            //������� ��� ���������
            if(sl>oop+0.5*Point && sl>osl+0.5*Point)
              {
               //����������� ������ ��� ���������
               if(!OrderModify(OrderTicket(),NormalizeDouble(oop,Digits),
                  NormalizeDouble(sl,Digits),OrderTakeProfit(),0,Blue))
                 {
                  //���� ����� �� ���������������, ������ ������ � ����� ����� �����
                  Sleep(ErrorTime());
                 }
              }
           }
        }

      //���������� Sell ��������
      if(OrderType()==OP_SELL)
        {
         //���� �����
         orders++;

         //���� �������� ��������
         if(Trailing>0)
           {
            //���� ��� ���������
            sl=sltp(Trailing,Ask,1);

            //������� ��� ���������
            if(sl<oop-0.5*Point && sl<osl-0.5*Point)
              {
               //����������� ������ ��� ���������
               if(!OrderModify(OrderTicket(),NormalizeDouble(oop,Digits),
                  NormalizeDouble(sl,Digits),OrderTakeProfit(),0,Red))
                 {
                  //���� ����� �� ���������������, ������ ������ � ����� ����� �����
                  Sleep(ErrorTime());
                 }
              }
           }
        }
     }
//+------------------------------------------------------------------+
//���� ���� �������� ������ ��� ��� �������, �����
//���� ���� �������� ������
   if(orders>0)
     {
      //����� ������� ��������
      Signal_=0;
      return(0);
     }
//���� ��� �������, �����
   if(Signal==0)return(0);
//+------------------------------------------------------------------+
//����� ���������� ������������� ������ � �������� ��� �������
//��� �� ���������
   lot=Lot;

//����� ���������� ������������� ������
   LastHistOrder=LastHistotyOrder(Symbol(),Magic);

//���� ����� ������
   if(LastHistOrder>=0)
     {
      //����� ������
      int res=OrderSelect(LastHistOrder,SELECT_BY_TICKET);

      //���� ������� ������ ������������� �� ��� �� �����������
      if(OrderProfit()<0)lot=OrderLots()*2;

      //���� ������� >0, ���� ������, ���� ����� ���, ��� ������� �������� 
      //� ����� NowBarAfterProfit ��������� ������� �������� ������ ����������
      if(OrderProfit()>0 && Signal!=0 && nowbar && Signal_==0 && NowBarAfterProfit)
        {
         Signal_=Signal;
        }
      if(OrderProfit()<0)Signal_=Signal;
     }
//���� ������������ ����� �� ������ ��� NowBarAfterProfit=false, ��� ������ <0
//��������� ������� �������� ������ ����������
   if(LastHistOrder<0 || !NowBarAfterProfit)Signal_=Signal;
//+------------------------------------------------------------------+
//�������� �����
   if(!DCOk(30))return(0);
//+------------------------------------------------------------------+
//�������� �������
//��������� ��� Buy �������
   if(Signal_>0)
     {
      op=Ask;
      sl=sltp(SL,op,-1);
      tp=sltp(TP,op,1);
      cmd=OP_BUY;
      arrow=Blue;
      OrderSendRun=true;
     }

//��������� ��� Sell �������
   if(Signal_<0)
     {
      op=Bid;
      sl=sltp(SL,op,1);
      tp=sltp(TP,op,-1);
      cmd=OP_SELL;
      arrow=Red;
      OrderSendRun=true;
     }

//�������� ������
   if(OrderSendRun)
     {
      ticket=OrderSend(Symbol(),cmd,lot,NormalizeDouble(op,Digits),Slippage,
                       NormalizeDouble(sl,Digits),NormalizeDouble(tp,Digits),_name,Magic,0,arrow);

      //���� ����� �� ��������, ������ ������ � ����� ����� �����
      if(ticket<0)Sleep(ErrorTime());
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//������� ��������� �������� ����������. ���������� "�����" ���� ��� Buy �����������
//"�������" ��� Sell �����������
string signal(double fast,double slow)
  {
   string ret="���";
   if(fast>slow)ret="�����";
   if(fast<slow)ret="�������";
   return(ret);
  }
//+------------------------------------------------------------------+
//| ������� �� Ugar eMail:ugar68@bk.ru                               |
/*+------------------------------------------------------------------+
������� ������ ����. 
���������� true ��� ������ ������ ������� ����� ��������� ������ ���� ��������� ���� ������, 
����� false.
timeframe - ���������
initialization true ����� ����������� ���������� �������, false ������*/
bool NowBar(int timeframe,bool initialization)
  {
   bool ret=false;
   static datetime LastTime;
   datetime TimeOpenBar;
   if(initialization)LastTime=0;
   else
     {
      TimeOpenBar=iTime(NULL,timeframe,0);
      if(LastTime!=TimeOpenBar)ret=true;
      LastTime=TimeOpenBar;
     }
   return(ret);
  }
//+------------------------------------------------------------------+
//|������� �� Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
//������� �������� ������ ���������� ������� � ���������� ����� �������� �� �������� ����������
//� ������ ������������ ������� ������� ���������� 60000.
int ErrorTime()
  {
   int err=GetLastError();
   int sec= 0,s,c;
   switch(err)
     {
      case    0: sec=0;
      break;
      case    1:
        {
         Print("������: 1 - ������� �������� ��� ������������� �������� ������ �� ����������");
         sec=0;
         break;
        }
      case    2:
        {
         Print("������: 2 - ����� ������. ���������� ��� ������� �������� �������� �� ��������� �������������.");
         sec=60000;
         break;
        }
      case    3:
        {
         Print("������: 3 - � �������� ������� �������� ������������ ���������. ���������� �������� ������ ���������.");
         sec=60000;
         break;
        }
      case    4:
        {
         Print("������: 4 - �������� ������ �����. ����� ��������� ������� ����� ���������� ������� ���������� �������");
         sec=60000;
         break;
        }
      case    5:
        {
         Print("������: 5 - ������ ������ ����������� ���������.");
         sec=60000;
         break;
        }
      case    6:
        {
         Print("������: 6 - ��� ����� � �������� ��������.");
         for(c=0;c<36000;c++)
           {
            if(IsConnected())
              {
               sec=0;
               break;
              }
            Sleep(100);
           }
         if(c==36000)
           {
            sec=5000;
           }
         break;
        }
      case    8:
        {
         Print("������: 8 - ������� ������ �������. ");
         sec=60000;
         break;
        }
      case   64:
        {
         Print("������: 64 - ���� ������������.");
         sec=60000;
         break;
        }
      case   65:
        {
         Print("������: 65 - ������������ ����� �����. ");
         sec=60000;
         break;
        }
      case  128:
        {
         Print("������: 128 - ����� ���� �������� ���������� ������.");
         sec=60000;
         break;
        }
      case  129:
        {
         Print("������: 129 - ������������ ���� bid ��� ask, ��������, ����������������� ����.");
         sec=5000;
         break;
        }
      case  130:
        {
         Print("������: 130 - ������� ������� ����� ��� ����������� ������������ ��� ����������������� ���� � ������ ");
         sec=5000;
         break;
        }
      case  131:
        {
         Print("������: 131 - ������������ �����, ������ � ���������� ������.");
         sec=60000;
         break;
        }
      case  132:
        {
         Print("������: 132 - ����� ������.");
         sec=60000;
         break;
        }
      case  133:
        {
         Print("������: 133 - �������� ���������. ");
         sec=60000;
         break;
        }
      case  134:
        {
         Print("������: 134 - ������������ ����� ��� ���������� ��������.");
         sec=60000;
         break;
        }
      case  135:
        {
         Print("������: 135 - ���� ����������.");
         sec=0;
         break;
        }
      case  136:
        {
         Print("������: 136 - ��� ���. ������ �� �����-�� ������� (��������, � ������ ������ ��� ���, ���������������� ����, ������� �����) �� ��� ��� ��� �������.");
         sec=5000;
         break;
        }
      case  138:
        {
         Print("������: 138 - ����������� ���� ��������, ���� ���������� bid � ask.");
         sec=0;
         break;
        }
      case  139:
        {
         Print("������: 139 - ����� ������������ � ��� ��������������.");
         sec=0;
         break;
        }
      case  140:
        {
         Print("������: 140 - ��������� ������ �������.");
         sec=0;
         break;
        }
      case  141:
        {
         Print("������: 141 - ������� ����� ��������.");
         sec=3000;
         break;
        }
      case  142:
        {
         Print("������: 142 - ����� ��������� � �������. ��� �� ������, � ���� �� ����� �������������� ����� ���������� ���������� � �������� ��������. ");
         sec=0;
         break;
        }
      case  143:
        {
         Print("������: 143 - ����� ������ ������� � ����������. ���� �� ����� �������������� ����� ���������� ���������� � �������� ��������.");
         sec=0;
         break;
        }
      case  144:
        {
         Print("������: 144 - ����� ����������� ����� �������� ��� ������ ������������� ������. ");
         sec=30000;
         break;
        }
      case  145:
        {
         Print("������: 145 - ����������� ���������, ��� ��� ����� ������� ������ � ����� � ������������ ��-�� ���������� ������� ����������.");
         sec=15000;
         break;
        }
      case  146:
        {
         Print("������: 146 - ���������� �������� ������.");
         for(s=0;s<36000;s++)
           {
            if(!IsTradeContextBusy())
              {
               sec=0;
               break;
              }
            Sleep(100);
           }
         if(s==36000)
           {
            sec=60000;
           }
         break;
        }
      case  147:
        {
         Print("������: 147 - ������������� ���� ��������� ������ ��������� ��������.");
         sec=60000;
         break;
        }
      case  148:
        {
         Print("������: 148 - ���������� �������� � ���������� ������� �������� �������, �������������� ��������.");
         sec=60000;
         break;
        }
      case  149:
        {
         Print("������: 149 - ������� ������� ��������������� ������� � ��� ������������ � ������, ���� ������������ ���������.");
         sec=60000;
         break;
        }
      case  150:
        {
         Print("������: 150 - ������� ������� ������� �� ����������� � ������������ � �������� FIFO. ������� ���������� ������� ����� ������ ������������ ������� ");
         sec=60000;
         break;
        }
      default: Print("����������� ������");
      break;
     }
   return(sec);
  }
//+------------------------------------------------------------------+
//|������� �� Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
/*������� ��������� ����������� ���������� �������� ���������� true ���� �� � ������� 
���� ��� �� �� � ������� ����� ������� � ����������� ����� � 0.1 ������� � ��������� ��������
���� � ������� ��������� ���������� ������ sec �������� �� ��������������� ���������� false
*/
bool DCOk(int sec)
  {
   bool ok=true,conn=true,trade=true;
   int s=sec*10;
   if(IsTesting() || IsOptimization())return(ok);
   for(int n=0;n<s;n++)
     {
      ok=true;
      conn=true;
      trade=true;
      if(!IsConnected())
        {
         //Print("��� ����� � ��������");
         ok=false;
         conn=false;
         Sleep(100);
         continue;
        }
      if(!IsTradeAllowed())
        {
         //Print("�������� ����� ����� ��� ��������� ��������� ��������");
         ok=false;
         trade=false;
         Sleep(100);
         continue;
        }
     }
   if(!conn)Print("��� ����� � ��������");
   if(!trade)Print("�������� ����� ����� ��� ��������� ��������� ��������");
   if(ok)RefreshRates();
   return(ok);
  }
//+------------------------------------------------------------------+
//|������� �� Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
//������� ������� ��������� ������������ ����� � ���������� ��� �����. ���� �� ������� 
//������������ ������� ���������� -1
/*
symb - ������, All- ��� �������
mag - ������ �����, -1 -�����
*/
int LastHistotyOrder(string symb,int mag)
  {
   datetime opentime=0;
   int ticket=-1;
   int hist=OrdersHistoryTotal();
   for(int p=hist-1; p>=0; p--)
     {
      if(!OrderSelect(p,SELECT_BY_POS,MODE_HISTORY))
        {
         Print("����� �� ������, ������ = ",GetLastError());
        }
      if(symb!="All" && OrderSymbol()!=Symbol())continue;
      if(mag>=0 && OrderMagicNumber()!=mag)continue;
      if(opentime<OrderCloseTime())
        {
         opentime=OrderCloseTime();
         ticket=OrderTicket();
        }
     }
   return(ticket);
  }
//+------------------------------------------------------------------+
//|������� �� Ugar eMail:ugar68@bk.ru                                |
//+------------------------------------------------------------------+
/*������� �������� ���������� � �������
po - ���������� �� ������ �������� � �������, ���� =0 ��� ������
pr - ���� ��������
direct - ����������� ������. 1 - �����, -1 ����, 0- ���
*/
double sltp(int po,double pr,int direct)
  {
   if(po==0 || direct==0)return(0);
   double step=MarketInfo(Symbol(),17);
   if(direct==1)return(MathRound((pr+po*Point)/step)*step);
   if(direct==-1)return(MathRound((pr-po*Point)/step)*step);
   return(0);
  }
//+------------------------------------------------------------------+
