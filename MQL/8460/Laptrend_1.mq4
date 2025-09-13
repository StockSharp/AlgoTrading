//+------------------------------------------------------------------+
//|                                                   Laptrend_1.mq4 |
//|                      Copyright � 2008, MetaQuotes Software Corp. |
//|                            ��������� ������    sergomsk@mail.ru
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+-
// �������� ���������� ����������
extern double Lots =0.0; // ���������� �����
extern int Percent =0; // ������� ���������� �������
extern int StopLoss =100; // StopLoss ��� ����� ������� (�������) 
extern int TakeProfit =40; // TakeProfit ��� ����� ������� (�������)
extern int TralingStop=100; // TralingStop ��� �������� ������� (����)
extern int Parol=12345; // ������ ��� ������ �� �������� �����
extern double  Delta=7; //���������� ADX

static bool SHUp, SHD, SMUp, SMD, FUp, FD, FclUp, FclD, SM1Up, SM1D;
int
Level_new, // ����� �������� ����������� ���������
Level_old, // ���������� �������� ����������� ���������
Mas_Tip[6]; // ������ ����� �������
// [] ��� ���: 0=B,1=S,2=BL,3=SL,4=BS,5=SS

double
Lots_New, // ���������� ����� ��� ����� �������
Mas_Ord_New[31][9], // ������ ������� ������� ..
Mas_Ord_Old[31][9]; // .. � ������
// 1� ������ = ���������� ����� ������ 
// [][0] �� ������������
// [][1] ���� ����. ������ (���.����.�����)
// [][2] StopLoss ������ (���.����.�����)
// [][3] TakeProfit ������ (���.����.�����)
// [][4] ����� ������ 
// [][5] �����. ����� ���. (���.����.�����)
// [][6] ��� ���. 0=B,1=S,2=BL,3=SL,4=BS,5=SS
// [][7] ���������� ����� ������
// [][8] 0/1 ���� ������� �����������

int init()
  {
//----
Level_old=MarketInfo(Symbol(),MODE_STOPLEVEL );//�����. ����������
Terminal(); // ������� ����� ������� 
SHUp=false;
SHD=false;
SMUp=false;
SMD=false;
FUp=false; 
FD=false; 
FclUp=false; 
FclD=false;
return; // ����� �� init()    
//----
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
Inform(-1); // ��� �������� ��������
return; // ����� �� deinit()   
//----
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
if(Check()==false) // ���� ������� �������������..
return; // ..�� �����������, �� �����
//PlaySound("tick.wav"); // �� ������ ����
Terminal(); // ������� ����� ������� 
Events(); // ���������� � ��������
Trade(Criterion()); // �������� �������
Inform(0); // ��� �������������� ��������
return; // ����� �� start()   
//----
  }
  
//+------------------------------------------------------------------+

// ������� �������� ����������� ������������� ���������
// ������� ���������:
// - ���������� ���������� Parol
// - ��������� ��������� "SuperBank"
// ������������ ��������:
// true - ���� ������� ������������� ���������
// false - ���� ������� ������������� ��������
 bool Check() // ���������������� �������
 {
     if (IsDemo()==true) // ���� ��� ����-����, ��..
     return(true); // .. ������ ����������� ���
     if (AccountCompany()=="SuperBank") // ��� ������������� ��������..
     return(true); // ..������ �� �����
     int Key=AccountNumber()*2+1000001; // ��������� ���� 
     if (Parol==Key) // ���� ������ ������, ��..
     return(true); // ..��������� ������ �� �����
     Inform(14); // ��������� � �������. ������
     return(false); // ����� �� �������. �������
 }
  int Terminal()
 {
  int Qnt=0; // ������� ���������� �������
    ArrayCopy(Mas_Ord_Old, Mas_Ord_New);// ��������� ���������� �������
    Qnt=0; // ��������� �������� �������
    ArrayInitialize(Mas_Ord_New,0); // ��������� �������
    ArrayInitialize(Mas_Tip, 0); // ��������� �������
    for(int i=0; i<OrdersTotal(); i++) // �� ������. � �����. �������
   {
     if((OrderSelect(i,SELECT_BY_POS)==true) //���� ���� �������.
     && (OrderSymbol()==Symbol())) //.. � ���� ���.����
    {
     Qnt++; // �����. �������
     Mas_Ord_New[Qnt][1]=OrderOpenPrice(); // ���� �������� ���
     Mas_Ord_New[Qnt][2]=OrderStopLoss(); // ���� SL
     Mas_Ord_New[Qnt][3]=OrderTakeProfit(); // ���� ��
     Mas_Ord_New[Qnt][4]=OrderTicket(); // ����� ������
     Mas_Ord_New[Qnt][5]=OrderLots(); // ���������� �����
     Mas_Tip[OrderType()]++; // ���. ������� ����
     Mas_Ord_New[Qnt][6]=OrderType(); // ��� ������
     Mas_Ord_New[Qnt][7]=OrderMagicNumber(); // ���������� ����� 
     if (OrderComment()=="")
     Mas_Ord_New[Qnt][8]=0; // ���� ��� �������
     else
     Mas_Ord_New[Qnt][8]=1; // ���� ���� �������
    }
  }
   Mas_Ord_New[0][0]=Qnt; // �����. �������
   return;
 }

//+------------------------------------------------------------------+

// ������� ������ �� ����� ����������� ���������.

int Inform(int Mess_Number, int Number=0, double Value=0.0)
{
// int Mess_Number // ����� ��������� 
// int Number // ������������ ����� ��������
// double Value // ������������ ��������. ����.
int Win_ind; // ����� ���� ����������
string Graf_Text; // ������ ���������
color Color_GT; // ���� ������ ���������
static int Time_Mess; // ����� ��������� ���������� �����.
static int Nom_Mess_Graf; // ������� ����������� ���������
static string Name_Grf_Txt[30]; // ������ ��� ������. ���������
Win_ind= WindowFind("inform"); // ���� ����� ���� ����������
if (Win_ind<0)return; // ���� ������ ���� ���, ������
if (Mess_Number==0) // ��� ���������� � ������ ����
{
if (Time_Mess==0) return; // ���� ��� ������� �����
if (GetTickCount()-Time_Mess>15000)// �� 15 ��� ���� �������
{
for(int i=0;i<=29; i++) // ������ c����� �����
ObjectSet( Name_Grf_Txt[i], OBJPROP_COLOR, Gray);
Time_Mess=0; // ������: ��� ������ �����
WindowRedraw(); // �������������� �������
}
return; // ����� �� �������
}
if (Mess_Number==-1) // ��� ���������� ��� deinit()
{
for(i=0; i<=29; i++) // �� �������� ��������
ObjectDelete(Name_Grf_Txt[i]);// �������� �������
return; // ����� �� �������
}
Nom_Mess_Graf++; // ������� ����������� �����.
Time_Mess=GetTickCount(); // ����� ��������� ���������� 
Color_GT=Lime;
switch(Mess_Number) // ������� �� ���������
{
case 1:
Graf_Text="������ ����� Buy "+ Number;
PlaySound("Close_order.wav"); break;
case 2:
Graf_Text="������ ����� Sell "+ Number;
PlaySound("Close_order.wav"); break;
case 3:
Graf_Text="����� ���������� ����� "+ Number;
PlaySound("Close_order.wav"); break;
case 4:
Graf_Text="������ ����� Buy "+ Number;
PlaySound("Ok.wav"); break;
case 5:
Graf_Text="������ ����� Sell "+ Number;
PlaySound("Ok.wav"); break;
case 6:
Graf_Text="���������� ���������� ����� "+ Number;
PlaySound("Ok.wav"); break;
case 7:
Graf_Text="����� "+Number+" �������������� � ��������";
PlaySound("Transform.wav"); break;
case 8:
Graf_Text="���������� ����� "+ Number; break;
PlaySound("Bulk.wav");
case 9:
Graf_Text="�������� ������ ����� "+ Number;
PlaySound("Close_order.wav"); break;
case 10:
Graf_Text="����� ����������� ���������: "+ Number;
PlaySound("Inform.wav"); break;
case 11:
Graf_Text=" �� ������� ����� �� "+
DoubleToStr(Value,2) + " �����";
Color_GT=Red;
PlaySound("Oops.wav"); break;
case 12:
Graf_Text="������� ������� ����� "+ Number;
PlaySound("expert.wav"); break;
case 13:
if (Number>0)
Graf_Text="������� ������� ����� Sell..";
else
Graf_Text="������� ������� ����� Buy..";
PlaySound("expert.wav"); break;
case 14:
Graf_Text="������������ ������. ������� �� ��������.";
Color_GT=Red;
PlaySound("Oops.wav"); break;
case 15:
switch(Number) // ������� �� ����� ������
{
case 2: Graf_Text="����� ������."; break;
case 129: Graf_Text="������������ ����. "; break;
case 135: Graf_Text="���� ����������. "; break;
case 136: Graf_Text="��� ���. ��� ����� ���.."; break;
case 146: Graf_Text="���������� �������� ������";break;
case 5 : Graf_Text="������ ������ ���������."; break;
case 64: Graf_Text="���� ������������."; break;
case 133: Graf_Text="�������� ���������"; break;
default: Graf_Text="�������� ������ " + Number;//������
}
Color_GT=Red;
PlaySound("Error.wav"); break;
case 16:
Graf_Text="������� �������� ������ �� EURUSD";
Color_GT=Red;
PlaySound("Oops.wav"); break;
default:
Graf_Text="default "+ Mess_Number;
Color_GT=Red;
PlaySound("Bzrrr.wav");
}
ObjectDelete(Name_Grf_Txt[29]); // 29�(�������) ������ �������
for(i=29; i>=1; i--) // ���� �� �������� ������� ..
{ // .. ����������� ��������
Name_Grf_Txt[i]=Name_Grf_Txt[i-1];// ��������� �������:
ObjectSet( Name_Grf_Txt[i], OBJPROP_YDISTANCE, 2+15*i);
}
Name_Grf_Txt[0]="Inform_"+Nom_Mess_Graf+"_"+Symbol(); // ��� ������
ObjectCreate (Name_Grf_Txt[0],OBJ_LABEL, Win_ind,0,0);// ������
ObjectSet (Name_Grf_Txt[0],OBJPROP_CORNER, 3 ); // ����
ObjectSet (Name_Grf_Txt[0],OBJPROP_XDISTANCE, 450);// �����. �
ObjectSet (Name_Grf_Txt[0],OBJPROP_YDISTANCE, 2); // �����. Y
// ��������� �������� �������
ObjectSetText(Name_Grf_Txt[0],Graf_Text,10,"Courier New",Color_GT);
WindowRedraw(); // �������������� ��� �������
return;
}

//+------------------------------------------------------------------+

// ������� �������� �� ���������.
// ���������� ����������:
// Level_new ����� �������� ����������� ���������
// Level_old ���������� �������� ����������� ���������
// Mas_Ord_New[31][9] ������ ������� ��������� ���������
// Mas_Ord_Old[31][9] ������ ������� ���������� (������)
int Events() // ���������������� �������
{
bool Conc_Nom_Ord; // ���������� ������� � ..
//.. ������ � ����� ��������
Level_new=MarketInfo(Symbol(),MODE_STOPLEVEL );// �������.���������
if (Level_old!=Level_new) // ����� �� ����� �������..
{ // ������ ���������� �������
Level_old=Level_new; // ����� "������ ��������"
Inform(10,Level_new); // ���������: ����� �������.
}
// ����� ���������, ���������� ���, �������� �������� � ������������
for(int old=1;old<=Mas_Ord_Old[0][0];old++)// �� ������� ������
{ // ������� �� ����, ���..
Conc_Nom_Ord=false; // ..������ �� ���������
for(int new=1;new<=Mas_Ord_New[0][0];new++)//���� �� ������� ..
{ //..����� �������
if (Mas_Ord_Old[old][4]==Mas_Ord_New[new][4])// ������ ����� 
{ // ��� ������ ���� ..
if (Mas_Ord_New[new][6]!=Mas_Ord_Old[old][6])//.. ������
Inform(7,Mas_Ord_New[new][4]);// ���������: ��������.:)
Conc_Nom_Ord=true; // ����� ������, ..
break; // ..������ ������� �� ..
} // .. ����������� �����
// �� ������ ����� ������
if (Mas_Ord_Old[old][7]>0 && // MagicNumber ����, ������
Mas_Ord_Old[old][7]==Mas_Ord_New[new][7])//.. �� ������
{ //������ �� ���������� ��� �������� ������
// ���� ���� ���������,.. 
if (Mas_Ord_Old[old][5]==Mas_Ord_New[new][5])
Inform(8,Mas_Ord_Old[old][4]);// ..�� ������������
else // � ����� ��� ����.. 
Inform(9,Mas_Ord_Old[old][4]);// ..��������� ��������
Conc_Nom_Ord=true; // ����� ������, ..
break; // ..������ ������� �� ..
} // .. ����������� �����
}
if (Conc_Nom_Ord==false) // ���� �� ���� �����,..
{ // ..�� ������ ���:(
if (Mas_Ord_Old[old][6]==0)
Inform(1, Mas_Ord_Old[old][4]); // ����� Buy ������
if (Mas_Ord_Old[old][6]==1)
Inform(2, Mas_Ord_Old[old][4]); // ����� Sell ������
if (Mas_Ord_Old[old][6]> 1)
Inform(3, Mas_Ord_Old[old][4]); // �������. ����� �����
}
}
// ����� ����� ������� 
for(new=1; new<=Mas_Ord_New[0][0]; new++)// �� ������� ����� ���.
{
if (Mas_Ord_New[new][8]>0) //��� �� �����,� ��������
continue; //..��� �������� ��������
Conc_Nom_Ord=false; // ���� ���������� ���
for(old=1; old<=Mas_Ord_Old[0][0]; old++)// ������ ���� ������� 
{ // ..� ������� ������
if (Mas_Ord_New[new][4]==Mas_Ord_Old[old][4])//������ �����..
{ //.. ������
Conc_Nom_Ord=true; // ����� ������, ..
break; // ..������ ������� �� ..
} // .. ����������� �����
}
if (Conc_Nom_Ord==false) // ���� ���������� ���,..
{ // ..�� ����� ����� :)
if (Mas_Ord_New[new][6]==0)
Inform(4, Mas_Ord_New[new][4]); // ����� Buy ������
if (Mas_Ord_New[new][6]==1)
Inform(5, Mas_Ord_New[new][4]); // ����� Sell ������
if (Mas_Ord_New[new][6]> 1)
Inform(6, Mas_Ord_New[new][4]); // ���������� �����.�����
}
}
return;
}

//+------------------------------------------------------------------+

// ������� ���������� ���������� �����.
// ���������� ����������:
// double Lots_New - ���������� ����� ��� ����� ������� (�����������)
// double Lots - �������� ���������� �����, �������� �����������.
// int Percent - ������� �������, �������� �������������
// ������������ ��������:
// true - ���� ������� ������� �� ����������� ���
// false - ���� ������� �� ������� �� ����������� ���
bool Lot() // �������������� �-��
{
string Symb =Symbol(); // ���������� �������.
double One_Lot=MarketInfo(Symb,MODE_MARGINREQUIRED);//�����. 1 ����
double Min_Lot=MarketInfo(Symb,MODE_MINLOT);// ���. ������. �����
double Step =MarketInfo(Symb,MODE_LOTSTEP);//��� ������� �������
double Free =AccountFreeMargin(); // ��������� ��������
if (Lots>0) // ���� ������ ����..
{ // ..�������� ���
double Money=Lots*One_Lot; // ��������� ������
if(Money<=AccountFreeMargin()) // ������� �������..
Lots_New=Lots; // ..��������� ��������
else // ���� �� �������..
Lots_New=MathFloor(Free/One_Lot/Step)*Step;// ������ �����
}
else // ���� ���� �� ������
{ // ..�� ���� �������
if (Percent > 100) // ������ �������� ..
Percent=100; // .. �� �� ����� 100
if (Percent==0) // ���� ���������� 0 ..
Lots_New=Min_Lot; // ..�� ����������� ���
else // ������. �����.�����:
Lots_New=MathFloor(Free*Percent/100/One_Lot/Step)*Step;//����
}
if (Lots_New < Min_Lot) // ���� ������ ������..
Lots_New=Min_Lot; // .. �� ������������
if (Lots_New*One_Lot > AccountFreeMargin()) // �� ������� ����..
{ // ..�� ���������. ���:(
Inform(11,0,Min_Lot); // ���������..
return(false); // ..� ����� 
}
return(true); // ����� �� �����. �-��
}

//+------------------------------------------------------------------+

// ������� ���������� �������� ���������.
// ������������ ��������:
// 10 - �������� Buy 
// 20 - �������� Sell 
// 11 - �������� Buy
// 21 - �������� Sell
// 0 - �������� ��������� ���

int Criterion() // ���������������� �������
{
string Sym=Symbol();
int i, k, o, m, s, f;
double Fx_0n, Fx_1n, Fx_2n, Fx_0, Fx_1, Sh1, Sh2, Sh3, Sh4, Sh0, Sh00;//

// ��������� ���������. �������:
double adx_m5 = iADX(Sym,15,14,PRICE_CLOSE,0,0);
double adx_1ago_m5 = iADX(NULL,15,14,PRICE_CLOSE,0,1); // ADX 5 min 1 bar ago
double di_p_m5 = iADX(NULL,15,14,PRICE_CLOSE,1,0); // DI+ 5 min
double di_m_m5 = iADX(NULL,15,14,PRICE_CLOSE,2,0); // DI- 5 min

Fx_0n=iCustom(Sym,PERIOD_M15,"Fisher_Yur4ik_Alert",10,0,0);
Fx_1n=iCustom(Sym,PERIOD_M15,"Fisher_Yur4ik_Alert",10,0,1);
Fx_2n=iCustom(Sym,PERIOD_M15,"Fisher_Yur4ik_Alert",10,0,2);

Fx_0=sglag2(Fx_0n, Fx_1n);
Fx_1=sglag2(Fx_1n, Fx_2n); 

if(Fx_1<0 && Fx_0>0) {FUp=true; FD=false;}
if(Fx_1>0 && Fx_0<0) {FD=true; FUp=false;}
if(Fx_1>0.25 && Fx_0<0.25) {FclUp=true; FclD=false;} 
if(Fx_1<-0.25 && Fx_0>-0.25) {FclD=true; FclUp=false;}

Tral_Stop(0); // �������� ���� Buy
Tral_Stop(1); // �������� ���� Sell
if(mod(di_p_m5,di_m_m5)<Delta && mod(adx_m5,di_p_m5)<Delta && mod(adx_m5,di_m_m5)<Delta)
{
Comment("��� ���� ",SHUp," ",Sh1," ��� ",i," ��� ����� ",SMUp," ",Sh3," ��� ",o," FxUp ",FUp," FxUpCl ",FclUp," ��� ���� ",SHD," ",Sh2," ��� ",k," ��� ���� ",SMD," ",Sh4," ��� ",m," FxD ",FD," FxDCl ",FclD,
"\n ADX ",adx_m5," ADX 1 ",adx_1ago_m5," +DI ",di_p_m5," -DI ",di_m_m5," Fx �� 0 ���� ",Fx_0," Fx �� 2 ���� ",Fx_1,"  ������� ����!!!");
FUp=false; FD=false; FclUp=false; FclD=false;
for(int l=1;l<=Mas_Ord_New[0][0];l++)
{ 
if(Mas_Ord_New[l][6]==0)
return(11); // �������� Buy 
if(Mas_Ord_New[l][6]==1)
return(21); // �������� Sell
}
return(0);
}

for(s=0;s<3000;s++)
    { 
      Sh0=iCustom(Symbol(),PERIOD_M1,"LabTrend1_v2.1",3,0,s);
     if(Sh0<1000 && Sh0>0)
     break;
    }
for(f=0;f<3000;f++)
    { 
      Sh00=iCustom(Symbol(),PERIOD_M1,"LabTrend1_v2.1",3,1,f);
     if(Sh00<1000 && Sh00>0)
     break;
    }

for(i=0;i<3000;i++)
    { 
      Sh1=iCustom(Symbol(),PERIOD_H1,"LabTrend1_v2.1",3,0,i);
     if(Sh1<1000 && Sh1>0)
     break;
    }
for(k=0;k<3000;k++)
    { 
      Sh2=iCustom(Symbol(),PERIOD_H1,"LabTrend1_v2.1",3,1,k);
     if(Sh2<1000 && Sh2>0)
     break;
    }
for(o=0;o<3000;o++)
    { 
      Sh3=iCustom(Symbol(),PERIOD_M15,"LabTrend1_v2.1",3,0,o);
     if(Sh3<1000 && Sh3>0)
     break;
    }    
 for(m=0;m<3000;m++)
    { 
      Sh4=iCustom(Symbol(),PERIOD_M15,"LabTrend1_v2.1",3,1,m);
     if(Sh4<1000 && Sh4>0)
     break;
    }
if(s<f)
{
SM1Up=true;
SM1D=false;
}

if(s>f)
{
SM1Up=true;
SM1D=false;
}

if(i<k)
{
SHUp=true;
SHD=false;
}

if(o<m)
{
SMUp=true;
SMD=false;
}

if(i>k)
{
SHD=true;
SHUp=false;
}

if(o>m)
{
SMD=true;
SMUp=false;
}

Comment("��� ���� ",SHUp," ",Sh1," ��� ",i," ��� ����� ",SMUp," ",Sh3," ��� ",o," FxUp ",FUp," FxUpCl ",FclUp," ��� ���� ",SHD," ",Sh2," ��� ",k," ��� ���� ",SMD," ",Sh4," ��� ",m," FxD ",FD," FxDCl ",FclD,
"\n ADX ",adx_m5," ADX1 ",adx_1ago_m5," +DI ",di_p_m5," -DI ",di_m_m5," Fx �� 0 ���� ",Fx_0," Fx �� 2 ���� ",Fx_1);

if(SHUp==true && SMUp==true && FUp==true && di_p_m5>di_m_m5 && adx_m5>adx_1ago_m5)
return(10); // �������� Buy 
if(SHD==true && SMD==true && FD==true && di_p_m5<di_m_m5 && adx_m5>adx_1ago_m5)
return(20); // �������� Sell
if(SMD==true || FD==true || FclUp==true)
return(11); // �������� Buy 
if(SMUp==true || FUp==true || FclD==true)
return(21); // �������� Sell 

return(0); // ����� �� �������. �������
}

double mod(double x, double y)
{
  if((x-y)<0)
  return((x-y)*(-1));
  return(x-y);
}

double sglag2(double x, double y) //������� ����������� � ���. ��=2
{return((x+y)/2);}

//+------------------------------------------------------------------+
 
// �������� �������.
int Trade(int Trad_Oper) // ���������������� �������
{
// Trad_Oper - ��� �������� ��������:
// 10 - �������� Buy 
// 20 - �������� Sell 
// 11 - �������� Buy
// 21 - �������� Sell
// 0 - �������� ��������� ���
// -1 - ������������ ������ ���������� ����������
switch(Trad_Oper)
{
case 10: // �������� �������� = Buy
Close_All(1); // ������� ��� Sell
if (Lot()==false) // ������� �� ������� �� �����.
return; // ����� �� �������. �������
Open_Ord(0); // ������� Buy
return; // ����������� - ������
case 11: // ����. ����. = �������� Buy
Close_All(0); // ������� ��� Buy
return; // ����������� - ������
case 20: // �������� �������� = Sell
Close_All(0); // ������� ��� Buy
if (Lot()==false)
return; // ����� �� �������. �������
Open_Ord(1); // ������� Sell 
return; // ����������� - ������
case 21: // ����. ����. = �������� Sell
Close_All(1); // ������� ��� Sell
return; // ����������� - ������
case 0: // ��������� �������� �������
Tral_Stop(0); // �������� ���� Buy
Tral_Stop(1); // �������� ���� Sell
return; // ����������� - ������
}
}

//+------------------------------------------------------------------+

// ������� �������� ���� �������� ������� ���������� ����
// ���������� ����������:
// Mas_Ord_New ������ ������� ��������� ���������
// Mas_Tip ������ ����� �������
int Close_All(int Tip) // ���������������� �������
{
// int Tip // ��� ������
int Ticket=0; // ����� ������
double Lot=0; // ���������� ����. �����
double Price_Cls; // ���� �������� ������--
while(Mas_Tip[Tip]>0) // �� ��� ���, ���� ���� ..
{ //.. ������ ��������� ���� 
for(int i=1; i<=Mas_Ord_New[0][0]; i++)// ���� �� ����� �������
{
if(Mas_Ord_New[i][6]==Tip && // ����� ������� ������ ����
Mas_Ord_New[i][5]>Lot) // .. �������� ����� �������
{ // ���� ������ ����� ������.
Lot=Mas_Ord_New[i][5]; // ���������� ��������� ���
Ticket=Mas_Ord_New[i][4]; // ����� ��� ������ �����
}
}
if (Tip==0) Price_Cls=Bid; // ��� ������� Buy
if (Tip==1) Price_Cls=Ask; // ��� ������� Sell
Inform(12,Ticket); // ��������� � ������� ����.
bool Ans=OrderClose(Ticket,Lot,Price_Cls,2);// ������� ����� !:)
if (Ans==false) // �� ���������� :( 
{ // �������������� ��������:
if(Errors(GetLastError())==false)// ���� ������ �������������
return; // .. �� ������.
}
Terminal(); // ������� ����� ������� 
Events(); // ������������ �������
}
return; // ����� �� �������. �������
}

//+------------------------------------------------------------------+

// ������� �������� ������ ��������� ������ ���������� ����
// ���������� ����������:
// int Mas_Tip ������ ����� �������
// int StopLoss �������� StopLoss (���������� �������)
// int TakeProfit �������� TakeProfit (���������� �������)
int Open_Ord(int Tip)
{
int Ticket, // ����� ������
MN; // MagicNumber
double SL, // StopLoss (�������.����.����)
TP; // TakeProf (�������.����.����)
while(Mas_Tip[Tip]==0) // �� ��� ���, ���� ..
{ //.. �� ��������� �����
if (StopLoss<Level_new) // ���� ������ �����������..
StopLoss=Level_new; // .. �� ����������
if (TakeProfit<Level_new) // ���� ������ �����������..
TakeProfit=Level_new; // ..�� ����������
MN=TimeCurrent(); // ������� MagicNumber
Inform(13,Tip); // ��������� � ������� ����
if (Tip==0) // ����� ��������� Buy
{
SL=Bid - StopLoss* Point; // StopLoss (����)
TP=Bid + TakeProfit*Point; // TakeProfit (����)
Ticket=OrderSend(Symbol(),0,Lots_New,Ask,2,SL,TP,"",MN);
}
if (Tip==1) // ����� ��������� Sell
{
SL=Ask + StopLoss* Point; // StopLoss (����)
TP=Ask - TakeProfit*Point; // TakeProfit (����)
Ticket=OrderSend(Symbol(),1,Lots_New,Bid,2,SL,TP,"",MN);
}
if (Ticket<0) // �� ���������� :( 
{ // �������������� ��������:
if(Errors(GetLastError())==false)// ���� ������ �������������
return; // .. �� ������.
}
Terminal(); // ������� ����� ������� 
Events(); // ������������ �������
}
return; // ����� �� �������. �������
}

//+------------------------------------------------------------------+

// ������� ����������� StopLoss ���� ������� ���������� ����
// ���������� ����������:
// Mas_Ord_New ������ ������� ��������� ���������
// int TralingStop �������� TralingStop(���������� �������)
int Tral_Stop(int Tip)
{
int Ticket; // ����� ������
double
Price, // ���� �������� ��������� ������
TS, // TralingStop (�������.����.����)
SL, // �������� StopLoss ������
TP; // �������� TakeProfit ������
bool Modify; // ������� ������������� ������.
for(int i=1;i<=Mas_Ord_New[0][0];i++) // ���� �� ���� �������
{ // ���� ������ �����. ����
if (Mas_Ord_New[i][6]!=Tip) // ���� ��� �� ��� ���..
continue; //.. �� ���������� �����
Modify=false; // ���� �� �������� � ������
Price =Mas_Ord_New[i][1]; // ���� �������� ������
SL =Mas_Ord_New[i][2]; // �������� StopLoss ������
TP =Mas_Ord_New[i][3]; // �������� TakeProft ������
Ticket=Mas_Ord_New[i][4]; // ����� ������
if (TralingStop<Level_new) // ���� ������ �����������..
TralingStop=Level_new; // .. �� ����������
TS=TralingStop*Point; // �� �� � �������.����.����
switch(Tip) // ������� �� ��� ������
{
case 0 : // ����� Buy
if (NormalizeDouble(SL,Digits)<// ���� ���� ���������..
NormalizeDouble(Bid-TS,Digits))
{ // ..�� ������������ ���:
SL=Bid-TS; // ����� ��� StopLoss
Modify=true; // �������� � ������.
}
break; // ����� �� switch
case 1 : // ����� Sell
if (NormalizeDouble(SL,Digits)>// ���� ���� ���������..
NormalizeDouble(Ask+TS,Digits)||
NormalizeDouble(SL,Digits)==0)//.. ��� �������(!)
{ // ..�� ������������ ���
SL=Ask+TS; // ����� ��� StopLoss
Modify=true; // �������� � ������.
}
} // ����� switch
if (Modify==false) // ���� ��� �� ���� ������..
continue; // ..�� ��� �� ����� ������
bool Ans=OrderModify(Ticket,Price,SL,TP,0);//������������ ���!
if (Ans==false) // �� ���������� :( 
{ // �������������� ��������:
if(Errors(GetLastError())==false)// ���� ������ �������������
return; // .. �� ������.
i--; // ��������� ��������
}
Terminal(); // ������� ����� ������� 
Events(); // ������������ �������
}
return; // ����� �� �������. �������
}

//+------------------------------------------------------------------+

// ������� ��������� ������.
// ������������ ��������:
// true - ���� ������ ����������� (�.�. ����� ���������� ������)
// false - ���� ������ ����������� (�.�. ��������� ������)
bool Errors(int Error) // ���������������� �������
{
// Error // ����� ������ 
if(Error==0)
return(false); // ��� ������
Inform(15,Error); // ���������
switch(Error)
{ // ����������� ������:
case 129: // ������������ ����
case 135: // ���� ����������
RefreshRates(); // ������� ������
return(true); // ������ �����������
case 136: // ��� ���. ��� ����� ���.
while(RefreshRates()==false) // �� ������ ����
Sleep(1); // �������� � �����
return(true); // ������ �����������
case 146: // ���������� �������� ������
Sleep(500); // ������� �������
RefreshRates(); // ������� ������
return(true); // ������ �����������
// ����������� ������:
case 2 : // ����� ������
case 5 : // ������ ������ ����������� ���������
case 64: // ���� ������������
case 133: // �������� ���������
default: // ������ ��������
return(false); // ����������� ������
}
}
//+------------------------------------------------------------------+