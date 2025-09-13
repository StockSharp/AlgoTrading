//+------------------------------------------------------------------+
//|                                             ZigAndZagScalpel.mq4 |
//|                           Bookkeeper, 2006, yuzefovich@gmail.com |
//+------------------------------------------------------------------+
#property copyright ""
#property link      ""
//----
#property indicator_chart_window
#property indicator_buffers 7     // ��� 8 - ��� testBuffer
#property indicator_color1 Aqua
#property indicator_color2 Black
#property indicator_color3 Black
#property indicator_color4 Black
#property indicator_color5 White
#property indicator_color6 Red
#property indicator_color7 Red
//#property indicator_color8 White // ��� ������� ����-������
extern int KeelOver=55; // ��� M15
extern int Slalom=17;  // ��� M15
double KeelOverZigAndZagSECTION[];
double KeelOverZagBuffer[];
double SlalomZigBuffer[];
double SlalomZagBuffer[];
double LimitOrdersBuffer[];
double BuyOrdersBuffer[];
double SellOrdersBuffer[];
//double testBuffer[];
int    shift,back,CountBar,Backstep=3;
int    LastSlalomZagPos,LastSlalomZigPos,LastKeelOverZagPos,LastKeelOverZigPos;
double Something,LimitPoints,Navel;
double CurKeelOverZig,CurKeelOverZag,CurSlalomZig,CurSlalomZag;
double LastSlalomZag,LastSlalomZig,LastKeelOverZag,LastKeelOverZig;
bool   TrendUp,SetBuyOrder,SetLimitOrder,SetSellOrder,Second=false;
string LastZigOrZag="None";
//----
int init()
  {
   SetIndexBuffer(0,KeelOverZigAndZagSECTION);
   SetIndexStyle(0,DRAW_SECTION,STYLE_DOT);//DRAW_SECTION ��� DRAW_NONE
   SetIndexEmptyValue(0,0.0);
   SetIndexBuffer(1,KeelOverZagBuffer);
   SetIndexStyle(1,DRAW_NONE);
   SetIndexEmptyValue(1,0.0);
   SetIndexBuffer(2,SlalomZigBuffer);
   SetIndexStyle(2,DRAW_NONE);
   SetIndexEmptyValue(2,0.0);
   SetIndexBuffer(3,SlalomZagBuffer);
   SetIndexStyle(3,DRAW_NONE);
   SetIndexEmptyValue(3,0.0);
   SetIndexBuffer(4,LimitOrdersBuffer);
   SetIndexStyle(4,DRAW_ARROW);
   SetIndexArrow(4,108);
   SetIndexEmptyValue(4,0.0);
   SetIndexBuffer(5,BuyOrdersBuffer);
   SetIndexStyle(5,DRAW_ARROW);
   SetIndexArrow(5,233);
   SetIndexEmptyValue(5,0.0);
   SetIndexBuffer(6,SellOrdersBuffer);
   SetIndexStyle(6,DRAW_ARROW);
   SetIndexArrow(6,234);
   SetIndexEmptyValue(6,0.0);
   //   SetIndexStyle(7,DRAW_SECTION);
   //   SetIndexBuffer(7,testBuffer);
   //   SetIndexEmptyValue(7,0.0);
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   CountBar=240-KeelOver;
   LimitPoints=Ask-Bid;
   if(CountBar<=3*KeelOver) return(-1); // �������� �����
   if(KeelOver<=2*Slalom) return(-1);  // ���������� ����
   // �������� ������������ �������
   for(shift=240-1; shift>240-KeelOver; shift--)
     {
      SlalomZagBuffer[shift]=0.0;
      SlalomZigBuffer[shift]=0.0;
      KeelOverZagBuffer[shift]=0.0;
      KeelOverZigAndZagSECTION[shift]=0.0;
      LimitOrdersBuffer[shift]=0.0;
      BuyOrdersBuffer[shift]=0.0;
      SellOrdersBuffer[shift]=0.0;
      //      testBuffer[shift]=0.0;
     }
   //+---������ ����� �� �������----------------------------------------+
   The_First_Crusade();
   //+---������ ������ �� ������������ ������---------------------------+
   //+---� ����� ��������� ������� ������� �������----------------------+
   LastSlalomZag=-1; LastSlalomZagPos=-1;
   LastSlalomZig=-1;  LastSlalomZigPos=-1;
   LastKeelOverZag=-1; LastKeelOverZagPos=-1;
   LastKeelOverZig=-1;  LastKeelOverZigPos=-1;
   The_Second_Crusade();
   //+---������ ������������ ������� - ���������� "������"--------------+
   //+---� ����������� "�������� ��������"------------------------------+
   LastSlalomZag=-1; LastSlalomZagPos=-1;
   LastSlalomZig=-1;  LastSlalomZigPos=-1;
   LastZigOrZag="None";
   The_Third_Crusade();
   //+---� ���� �� ������ ����� ����� ����� � ������?-------------------+
   Shift_Zerro();
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void The_First_Crusade()
  {
   for(shift=CountBar; shift>0; shift--)
     {
      // ����� ����� "����� � ����" - "���� � �����"
      CurSlalomZig=Low[Lowest(NULL,0,MODE_LOW,Slalom,shift)];
      CurSlalomZag=High[Highest(NULL,0,MODE_HIGH,Slalom,shift)];
      // ��������� shift �� ������� ���������� ���������� ��� ����� 
      // � ������� ��� ��� ������ �� �������
      if(CurSlalomZig==LastSlalomZig) CurSlalomZig=0.0;
      else
        {
         LastSlalomZig=CurSlalomZig;
         if((Low[shift]-CurSlalomZig)>LimitPoints) CurSlalomZig=0.0;
         else
           {
            // �� ��������� Backstep ����� ���� ������ ���� ���, 
            // ��������� ������ ���������, ����� ������ �������
            for(back=1; back<=Backstep; back++)
              {
               Something=SlalomZigBuffer[shift+back];
               if((Something!=0)&&(Something>CurSlalomZig))
                  SlalomZigBuffer[shift+back]=0.0;
              }
           }
        }
      // ��������� shift �� ������� ���������� ���������� ��� ����� ����
      // ��� ��� ������ �� �������
      if(CurSlalomZag==LastSlalomZag) CurSlalomZag=0.0;
      else
        {
         LastSlalomZag=CurSlalomZag;
         if((CurSlalomZag-High[shift])>LimitPoints) CurSlalomZag=0.0;
         else
           {
            // �� ��������� Backstep ����� ���� ������ ���� ���, 
            // ��������� ������ ���������, ����� ������ �������
            for(back=1; back<=Backstep; back++)
              {
               Something=SlalomZagBuffer[shift+back];
               if((Something!=0)&&(Something<CurSlalomZag))
                  SlalomZagBuffer[shift+back]=0.0;
              }
           }
        }
      // ���, ��� ����� ���������� � �������� - ������ � ������ �������
      SlalomZigBuffer[shift]=CurSlalomZig;
      SlalomZagBuffer[shift]=CurSlalomZag;
      // ���� ����� ��������� ��� ���������� "��������� ������", ��� ���� 
      // � ����� ���������� ZigAndZag ���� ��� ����� ������ ������ ������������
      CurKeelOverZig=Low[Lowest(NULL,0,MODE_LOW,KeelOver,shift)];
      CurKeelOverZag=High[Highest(NULL,0,MODE_HIGH,KeelOver,shift)];
      // ��������� shift �� ������� ���������� ������������
      if(CurKeelOverZig==LastKeelOverZig) CurKeelOverZig=0.0;
      else
        {
         LastKeelOverZig=CurKeelOverZig;
         if((Low[shift]-CurKeelOverZig)>LimitPoints) CurKeelOverZig=0.0;
         else
           {
            // �� ��������� Backstep ����� ���� ������ ���� ���, 
            // ��������� ������ ���������, ����� ������ �������
            for(back=1; back<=Backstep; back++)
              {
               Something=KeelOverZigAndZagSECTION[shift+back];
               if((Something!=0)&&(Something>CurKeelOverZig))
                  KeelOverZigAndZagSECTION[shift+back]=0.0;
              }
           }
        }
      // ��������� shift �� ������� ���������� ������������
      if(CurKeelOverZag==LastKeelOverZag) CurKeelOverZag=0.0;
      else
        {
         LastKeelOverZag=CurKeelOverZag;
         if((CurKeelOverZag-High[shift])>LimitPoints) CurKeelOverZag=0.0;
         else
           {
            // �� ��������� Backstep ����� ���� ������ ���� ���, 
            // ����� ������ �������
            for(back=1; back<=Backstep; back++)
              {
               Something=KeelOverZagBuffer[shift+back];
               if((Something!=0)&&(Something<CurKeelOverZag))
                  KeelOverZagBuffer[shift+back]=0.0;
              }
           }
        }
      // ���, ��� ����� ��� �� ����� - ������ � ������ ����������
      KeelOverZigAndZagSECTION[shift]=CurKeelOverZig;
      KeelOverZagBuffer[shift]=CurKeelOverZag;
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void The_Second_Crusade()
  {
   // ������ ��������� �������
   for(shift=CountBar; shift>0; shift--)
     {
      CurSlalomZig=SlalomZigBuffer[shift];
      CurSlalomZag=SlalomZagBuffer[shift];
      if((CurSlalomZig==0)&&(CurSlalomZag==0)) continue;
      if(CurSlalomZag!=0)
        {
         if(LastSlalomZag>0)
           {
            if(LastSlalomZag<CurSlalomZag) SlalomZagBuffer[LastSlalomZagPos]=0;
            else SlalomZagBuffer[shift]=0;
           }
         if(LastSlalomZag<CurSlalomZag || LastSlalomZag<0)
           {
            LastSlalomZag=CurSlalomZag;
            LastSlalomZagPos=shift;
           }
         LastSlalomZig=-1;
        }
      if(CurSlalomZig!=0)
        {
         if(LastSlalomZig>0)
           {
            if(LastSlalomZig>CurSlalomZig) SlalomZigBuffer[LastSlalomZigPos]=0;
            else SlalomZigBuffer[shift]=0;
           }
         if((CurSlalomZig<LastSlalomZig)||(LastSlalomZig<0))
           {
            LastSlalomZig=CurSlalomZig;
            LastSlalomZigPos=shift;
           }
         LastSlalomZag=-1;
        }
      CurKeelOverZig=KeelOverZigAndZagSECTION[shift];
      CurKeelOverZag=KeelOverZagBuffer[shift];
      if((CurKeelOverZig==0)&&(CurKeelOverZag==0)) continue;
      if(CurKeelOverZag !=0)
        {
         if(LastKeelOverZag>0)
           {
            if(LastKeelOverZag<CurKeelOverZag)
               KeelOverZagBuffer[LastKeelOverZagPos]=0;
            else KeelOverZagBuffer[shift]=0.0;
           }
         if(LastKeelOverZag<CurKeelOverZag || LastKeelOverZag<0)
           {
            LastKeelOverZag=CurKeelOverZag;
            LastKeelOverZagPos=shift;
           }
         LastKeelOverZig=-1;
        }
      if(CurKeelOverZig!=0)
        {
         if(LastKeelOverZig>0)
           {
            if(LastKeelOverZig>CurSlalomZig)
               KeelOverZigAndZagSECTION[LastKeelOverZigPos]=0;
            else KeelOverZigAndZagSECTION[shift]=0;
           }
         if((CurKeelOverZig<LastKeelOverZig)||(LastKeelOverZig<0))
           {
            LastKeelOverZig=CurKeelOverZig;
            LastKeelOverZigPos=shift;
           }
         LastKeelOverZag=-1;
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void The_Third_Crusade()
  {
   bool first=true;
   for(shift=CountBar; shift>0; shift--)
     {
      // ��������� ������� ��������
      LimitOrdersBuffer[shift]=0.0;
      BuyOrdersBuffer[shift]=0.0;
      SellOrdersBuffer[shift]=0.0;
      // ������ ����� ���������� ��������� shift (�� ������ -
      // ������ �������� ������������� ���� �� �����)
      Navel=(5*Close[shift]+2*Open[shift]+High[shift]+Low[shift])/9;
      // ���� �������� - �������,
      // ���� (����� ����) ������ ����������� ������: ����� ��� ����
      if(KeelOverZigAndZagSECTION[shift]!=0.0)
        {
         TrendUp=true;
         first=false;
        }
      if(KeelOverZagBuffer[shift]!=0.0)
        {
         TrendUp=false;
         first=false;
        }
      // �������� � KeelOverZigAndZagSECTION � ������������, � ������������, 
      // � �������� - ��� � ���� �����, ����� ������� �������� �������������
      // ZigAndZag, ��������� ���� "������" �� ����� ����������� ������
      if(KeelOverZagBuffer[shift]!=0.0 || KeelOverZigAndZagSECTION[shift]!=0.0)
        {
         KeelOverZigAndZagSECTION[shift]=Navel;
        }
      else KeelOverZigAndZagSECTION[shift]=0.0;
      // ��������� shift �� ������� ���������� ��� ����������
      if(SlalomZigBuffer[shift]!=0.0)
        {
         LastZigOrZag="Zig";
         LastSlalomZig=Navel;
         SetBuyOrder=false;
         SetLimitOrder=false;
         SetSellOrder=false;
        }
      if(SlalomZagBuffer[shift]!=0.0)
        {
         LastZigOrZag="Zag";
         LastSlalomZag=Navel;
         SetBuyOrder=false;
         SetLimitOrder=false;
         SetSellOrder=false;
        }
      // �, ���� �� ����������, �� ���������� ��� ���,
      // � �������� ��� ��� - �������, � ��� ���� �� �����-������
      if(SlalomZigBuffer[shift]==0.0 &&
         SlalomZagBuffer[shift]==0.0 &&
         first==false)                  Slalom_With_A_Scalpel();
     }
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Shift_Zerro()
  {
   shift=0;
   Navel=(5*Close[0]+2*Open[0]+High[0]+Low[0])/9;
   Slalom_With_A_Scalpel();
   KeelOverZigAndZagSECTION[0]=Navel;
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void Slalom_With_A_Scalpel()
  {
   // ��������� ������������ ������ �� ����� ����� ����
   // ��� �� ���� �� �����, � �������� ��:  
   // ���� ��� ����� ��������� �� ����� - �������� ������� �� ������,
   // ���� ������ - ������ �� ����� ������ �����-����
   if(LastZigOrZag=="Zig")
     {
      if(TrendUp==true)
        {
         if((Navel-LastSlalomZig)>=LimitPoints && SetBuyOrder==false)
           {
            SetBuyOrder=true;
            BuyOrdersBuffer[shift]=Low[shift+1];
            LastSlalomZigPos=shift;
           }
         if(Navel<=LastSlalomZig && SetBuyOrder==true)
           {
            SetBuyOrder=false;
            BuyOrdersBuffer[LastSlalomZigPos]=0.0;
            LastSlalomZigPos=-1;
           }
        }
      if(TrendUp==false)
        {
         if(Navel>LastSlalomZig && SetLimitOrder==false)
           {
            SetLimitOrder=true;
            LimitOrdersBuffer[shift]=Navel;
            //            LimitOrdersBuffer[shift]=Close[shift];
            LastSlalomZigPos=shift;
           }
         if(Navel<=LastSlalomZig && SetLimitOrder==true)
           {
            SetLimitOrder=false;
            LimitOrdersBuffer[LastSlalomZigPos]=0.0;
            LastSlalomZigPos=-1;
           }
        }
     }
   if(LastZigOrZag=="Zag")
     {
      if(TrendUp==false)
        {
         if((LastSlalomZag-Navel)>=LimitPoints && SetSellOrder==false)
           {
            SetSellOrder=true;
            SellOrdersBuffer[shift]=High[shift+1];
            LastSlalomZagPos=shift;
           }
         if(Navel>=LastSlalomZag && SetSellOrder==true)
           {
            SetSellOrder=false;
            SellOrdersBuffer[LastSlalomZagPos]=0.0;
            LastSlalomZagPos=-1;
           }
        }
      if(TrendUp==true)
        {
         if(LastSlalomZag>Navel && SetLimitOrder==false)
           {
            SetLimitOrder=true;
            LimitOrdersBuffer[shift]=Navel;
            //            LimitOrdersBuffer[shift]=Close[shift];
            LastSlalomZagPos=shift;
           }
         if(Navel>=LastSlalomZag && SetLimitOrder==true)
           {
            SetLimitOrder=false;
            LimitOrdersBuffer[LastSlalomZagPos]=0.0;
            LastSlalomZagPos=-1;
           }
        }
     }
   return;
  }
//+--����������, � ��� ������. �������, ���� ��� ��� �������� �����--+
//+------------------------------------------------------------------+

