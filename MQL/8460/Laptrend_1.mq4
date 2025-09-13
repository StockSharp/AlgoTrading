//+------------------------------------------------------------------+
//|                                                   Laptrend_1.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                            Московцев Сергей    sergomsk@mail.ru
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+-
// Описание глобальных переменных
extern double Lots =0.0; // Количество лотов
extern int Percent =0; // Процент выделенных средств
extern int StopLoss =100; // StopLoss для новых ордеров (пунктов) 
extern int TakeProfit =40; // TakeProfit для новых ордеров (пунктов)
extern int TralingStop=100; // TralingStop для рыночных ордеров (пунк)
extern int Parol=12345; // Пароль для работы на реальном счёте
extern double  Delta=7; //Скважность ADX

static bool SHUp, SHD, SMUp, SMD, FUp, FD, FclUp, FclD, SM1Up, SM1D;
int
Level_new, // Новое значение минимальной дистанции
Level_old, // Предыдущее значение минимальной дистанции
Mas_Tip[6]; // Массив типов ордеров
// [] тип орд: 0=B,1=S,2=BL,3=SL,4=BS,5=SS

double
Lots_New, // Количество лотов для новых ордеров
Mas_Ord_New[31][9], // Массив ордеров текущий ..
Mas_Ord_Old[31][9]; // .. и старый
// 1й индекс = порядковый номер ордера 
// [][0] не определяется
// [][1] курс откр. ордера (абс.знач.курса)
// [][2] StopLoss ордера (абс.знач.курса)
// [][3] TakeProfit ордера (абс.знач.курса)
// [][4] номер ордера 
// [][5] колич. лотов орд. (абс.знач.курса)
// [][6] тип орд. 0=B,1=S,2=BL,3=SL,4=BS,5=SS
// [][7] магическое число ордера
// [][8] 0/1 факт наличия комментария

int init()
  {
//----
Level_old=MarketInfo(Symbol(),MODE_STOPLEVEL );//Миним. дистаниция
Terminal(); // Функция учёта ордеров 
SHUp=false;
SHD=false;
SMUp=false;
SMD=false;
FUp=false; 
FD=false; 
FclUp=false; 
FclD=false;
return; // Выход из init()    
//----
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
Inform(-1); // Для удаления объектов
return; // Выход из deinit()   
//----
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
if(Check()==false) // Если условия использования..
return; // ..не выполняются, то выход
//PlaySound("tick.wav"); // На каждом тике
Terminal(); // Функция учёта ордеров 
Events(); // Информация о событиях
Trade(Criterion()); // Торговая функция
Inform(0); // Для перекрашивания объектов
return; // Выход из start()   
//----
  }
  
//+------------------------------------------------------------------+

// Функция проверки легальности использования программы
// Входные параметры:
// - глобальная переменная Parol
// - локальная константа "SuperBank"
// Возвращаемые значения:
// true - если условия использования соблюдены
// false - если условия использования нарушены
 bool Check() // Пользовательская функция
 {
     if (IsDemo()==true) // Если это демо-счёт, то..
     return(true); // .. других ограничений нет
     if (AccountCompany()=="SuperBank") // Для корпоративных клиентов..
     return(true); // ..пароль не нужен
     int Key=AccountNumber()*2+1000001; // Вычисляем ключ 
     if (Parol==Key) // Если пароль верный, то..
     return(true); // ..разрешаем работу на реале
     Inform(14); // Сообщение о несанкц. работе
     return(false); // Выход из пользов. функции
 }
  int Terminal()
 {
  int Qnt=0; // Счётчик количества ордеров
    ArrayCopy(Mas_Ord_Old, Mas_Ord_New);// Сохраняем предыдущую историю
    Qnt=0; // Обнуление счётчика ордеров
    ArrayInitialize(Mas_Ord_New,0); // Обнуление массива
    ArrayInitialize(Mas_Tip, 0); // Обнуление массива
    for(int i=0; i<OrdersTotal(); i++) // По рыночн. и отлож. ордерам
   {
     if((OrderSelect(i,SELECT_BY_POS)==true) //Если есть следующ.
     && (OrderSymbol()==Symbol())) //.. и наша вал.пара
    {
     Qnt++; // Колич. ордеров
     Mas_Ord_New[Qnt][1]=OrderOpenPrice(); // Курс открытия орд
     Mas_Ord_New[Qnt][2]=OrderStopLoss(); // Курс SL
     Mas_Ord_New[Qnt][3]=OrderTakeProfit(); // Курс ТР
     Mas_Ord_New[Qnt][4]=OrderTicket(); // Номер ордера
     Mas_Ord_New[Qnt][5]=OrderLots(); // Количество лотов
     Mas_Tip[OrderType()]++; // Кол. ордеров типа
     Mas_Ord_New[Qnt][6]=OrderType(); // Тип ордера
     Mas_Ord_New[Qnt][7]=OrderMagicNumber(); // Магическое число 
     if (OrderComment()=="")
     Mas_Ord_New[Qnt][8]=0; // Если нет коммент
     else
     Mas_Ord_New[Qnt][8]=1; // Если есть коммент
    }
  }
   Mas_Ord_New[0][0]=Qnt; // Колич. ордеров
   return;
 }

//+------------------------------------------------------------------+

// Функция вывода на экран графических сообщений.

int Inform(int Mess_Number, int Number=0, double Value=0.0)
{
// int Mess_Number // Номер сообщения 
// int Number // Передаваемое целое значение
// double Value // Передаваемое действит. знач.
int Win_ind; // Номер окна индикатора
string Graf_Text; // Строка сообщения
color Color_GT; // Цвет строки сообщения
static int Time_Mess; // Время последней публикации сообщ.
static int Nom_Mess_Graf; // Счётчик графических сообщений
static string Name_Grf_Txt[30]; // Массив имён графич. сообщений
Win_ind= WindowFind("inform"); // Ищем номер окна индикатора
if (Win_ind<0)return; // Если такого окна нет, уходим
if (Mess_Number==0) // Это происходит в каждом тике
{
if (Time_Mess==0) return; // Если уже крашено серым
if (GetTickCount()-Time_Mess>15000)// За 15 сек цвет устарел
{
for(int i=0;i<=29; i++) // Красим cтроки серым
ObjectSet( Name_Grf_Txt[i], OBJPROP_COLOR, Gray);
Time_Mess=0; // Флажок: все строки серые
WindowRedraw(); // Перерисовываем объекты
}
return; // Выход из функции
}
if (Mess_Number==-1) // Это происходит при deinit()
{
for(i=0; i<=29; i++) // По индексам объектов
ObjectDelete(Name_Grf_Txt[i]);// Удаление объекта
return; // Выход из функции
}
Nom_Mess_Graf++; // Счётчик графических сообщ.
Time_Mess=GetTickCount(); // Время последней публикации 
Color_GT=Lime;
switch(Mess_Number) // Переход на сообщение
{
case 1:
Graf_Text="Закрыт ордер Buy "+ Number;
PlaySound("Close_order.wav"); break;
case 2:
Graf_Text="Закрыт ордер Sell "+ Number;
PlaySound("Close_order.wav"); break;
case 3:
Graf_Text="Удалён отложенный ордер "+ Number;
PlaySound("Close_order.wav"); break;
case 4:
Graf_Text="Открыт ордер Buy "+ Number;
PlaySound("Ok.wav"); break;
case 5:
Graf_Text="Открыт ордер Sell "+ Number;
PlaySound("Ok.wav"); break;
case 6:
Graf_Text="Установлен отложенный ордер "+ Number;
PlaySound("Ok.wav"); break;
case 7:
Graf_Text="Ордер "+Number+" преобразовался в рыночный";
PlaySound("Transform.wav"); break;
case 8:
Graf_Text="Переоткрыт ордер "+ Number; break;
PlaySound("Bulk.wav");
case 9:
Graf_Text="Частично закрыт ордер "+ Number;
PlaySound("Close_order.wav"); break;
case 10:
Graf_Text="Новая минимальная дистанция: "+ Number;
PlaySound("Inform.wav"); break;
case 11:
Graf_Text=" Не хватает денег на "+
DoubleToStr(Value,2) + " лотов";
Color_GT=Red;
PlaySound("Oops.wav"); break;
case 12:
Graf_Text="Попытка закрыть ордер "+ Number;
PlaySound("expert.wav"); break;
case 13:
if (Number>0)
Graf_Text="Попытка открыть ордер Sell..";
else
Graf_Text="Попытка открыть ордер Buy..";
PlaySound("expert.wav"); break;
case 14:
Graf_Text="Неправильный пароль. Эксперт не работает.";
Color_GT=Red;
PlaySound("Oops.wav"); break;
case 15:
switch(Number) // Переход на номер ошибки
{
case 2: Graf_Text="Общая ошибка."; break;
case 129: Graf_Text="Неправильная цена. "; break;
case 135: Graf_Text="Цена изменилась. "; break;
case 136: Graf_Text="Нет цен. Ждём новый тик.."; break;
case 146: Graf_Text="Подсистема торговли занята";break;
case 5 : Graf_Text="Старая версия терминала."; break;
case 64: Graf_Text="Счет заблокирован."; break;
case 133: Graf_Text="Торговля запрещена"; break;
default: Graf_Text="Возникла ошибка " + Number;//Другие
}
Color_GT=Red;
PlaySound("Error.wav"); break;
case 16:
Graf_Text="Эксперт работает только на EURUSD";
Color_GT=Red;
PlaySound("Oops.wav"); break;
default:
Graf_Text="default "+ Mess_Number;
Color_GT=Red;
PlaySound("Bzrrr.wav");
}
ObjectDelete(Name_Grf_Txt[29]); // 29й(верхний) объект удаляем
for(i=29; i>=1; i--) // Цикл по индексам массива ..
{ // .. графических объектов
Name_Grf_Txt[i]=Name_Grf_Txt[i-1];// Поднимаем объекты:
ObjectSet( Name_Grf_Txt[i], OBJPROP_YDISTANCE, 2+15*i);
}
Name_Grf_Txt[0]="Inform_"+Nom_Mess_Graf+"_"+Symbol(); // Имя объект
ObjectCreate (Name_Grf_Txt[0],OBJ_LABEL, Win_ind,0,0);// Создаём
ObjectSet (Name_Grf_Txt[0],OBJPROP_CORNER, 3 ); // Угол
ObjectSet (Name_Grf_Txt[0],OBJPROP_XDISTANCE, 450);// Коорд. Х
ObjectSet (Name_Grf_Txt[0],OBJPROP_YDISTANCE, 2); // Коорд. Y
// Текстовое описание объекта
ObjectSetText(Name_Grf_Txt[0],Graf_Text,10,"Courier New",Color_GT);
WindowRedraw(); // Перерисовываем все объекты
return;
}

//+------------------------------------------------------------------+

// Функция слежения за событиями.
// Глобальные переменные:
// Level_new Новое значение минимальной дистанции
// Level_old Предыдущее значение минимальной дистанции
// Mas_Ord_New[31][9] Массив ордеров последний известный
// Mas_Ord_Old[31][9] Массив ордеров предыдущий (старый)
int Events() // Пользовательская функция
{
bool Conc_Nom_Ord; // Совпадение ордеров в ..
//.. старом и новом массивах
Level_new=MarketInfo(Symbol(),MODE_STOPLEVEL );// Последн.известное
if (Level_old!=Level_new) // Новое не равно старому..
{ // значит изменились условия
Level_old=Level_new; // Новое "старое значение"
Inform(10,Level_new); // Сообщение: новая дистанц.
}
// Поиск пропавших, поменявших тип, частично закрытых и переоткрытых
for(int old=1;old<=Mas_Ord_Old[0][0];old++)// По массиву старых
{ // Исходим из того, что..
Conc_Nom_Ord=false; // ..ордера не совпадают
for(int new=1;new<=Mas_Ord_New[0][0];new++)//Цикл по массиву ..
{ //..новых ордеров
if (Mas_Ord_Old[old][4]==Mas_Ord_New[new][4])// Совпал номер 
{ // Тип ордера стал ..
if (Mas_Ord_New[new][6]!=Mas_Ord_Old[old][6])//.. другим
Inform(7,Mas_Ord_New[new][4]);// Сообщение: преобраз.:)
Conc_Nom_Ord=true; // Ордер найден, ..
break; // ..значит выходим из ..
} // .. внутреннего цикла
// Не совпал номер ордера
if (Mas_Ord_Old[old][7]>0 && // MagicNumber есть, совпал
Mas_Ord_Old[old][7]==Mas_Ord_New[new][7])//.. со старым
{ //значит он переоткрыт или частично закрыт
// Если лоты совпадают,.. 
if (Mas_Ord_Old[old][5]==Mas_Ord_New[new][5])
Inform(8,Mas_Ord_Old[old][4]);// ..то переоткрытие
else // А иначе это было.. 
Inform(9,Mas_Ord_Old[old][4]);// ..частичное закрытие
Conc_Nom_Ord=true; // Ордер найден, ..
break; // ..значит выходим из ..
} // .. внутреннего цикла
}
if (Conc_Nom_Ord==false) // Если мы сюда дошли,..
{ // ..то ордера нет:(
if (Mas_Ord_Old[old][6]==0)
Inform(1, Mas_Ord_Old[old][4]); // Ордер Buy закрыт
if (Mas_Ord_Old[old][6]==1)
Inform(2, Mas_Ord_Old[old][4]); // Ордер Sell закрыт
if (Mas_Ord_Old[old][6]> 1)
Inform(3, Mas_Ord_Old[old][4]); // Отложен. ордер удалён
}
}
// Поиск новых ордеров 
for(new=1; new<=Mas_Ord_New[0][0]; new++)// По массиву новых орд.
{
if (Mas_Ord_New[new][8]>0) //Это не новый,а переоткр
continue; //..или частично закрытый
Conc_Nom_Ord=false; // Пока совпадения нет
for(old=1; old<=Mas_Ord_Old[0][0]; old++)// Поищем этот ордерок 
{ // ..в массиве старых
if (Mas_Ord_New[new][4]==Mas_Ord_Old[old][4])//Совпал номер..
{ //.. ордера
Conc_Nom_Ord=true; // Ордер найден, ..
break; // ..значит выходим из ..
} // .. внутреннего цикла
}
if (Conc_Nom_Ord==false) // Если совпадения нет,..
{ // ..то ордер новый :)
if (Mas_Ord_New[new][6]==0)
Inform(4, Mas_Ord_New[new][4]); // Ордер Buy открыт
if (Mas_Ord_New[new][6]==1)
Inform(5, Mas_Ord_New[new][4]); // Ордер Sell открыт
if (Mas_Ord_New[new][6]> 1)
Inform(6, Mas_Ord_New[new][4]); // Установлен отлож.ордер
}
}
return;
}

//+------------------------------------------------------------------+

// Функция вычисления количества лотов.
// Глобальные переменные:
// double Lots_New - количество лотов для новых ордеров (вычисляется)
// double Lots - желаемое количество лотов, заданное пользовател.
// int Percent - процент средств, заданный пользователем
// Возвращаемые значения:
// true - если средств хватает на минимальный лот
// false - если средств не хватает на минимальный лот
bool Lot() // Позовательская ф-ия
{
string Symb =Symbol(); // Финансовый инструм.
double One_Lot=MarketInfo(Symb,MODE_MARGINREQUIRED);//Стоим. 1 лота
double Min_Lot=MarketInfo(Symb,MODE_MINLOT);// Мин. размер. лотов
double Step =MarketInfo(Symb,MODE_LOTSTEP);//Шаг изменен размера
double Free =AccountFreeMargin(); // Свободные средства
if (Lots>0) // Лоты заданы явно..
{ // ..проверим это
double Money=Lots*One_Lot; // Стоимость ордера
if(Money<=AccountFreeMargin()) // Средств хватает..
Lots_New=Lots; // ..принимаем заданное
else // Если не хватает..
Lots_New=MathFloor(Free/One_Lot/Step)*Step;// Расчёт лотов
}
else // Если лоты не заданы
{ // ..то берём процент
if (Percent > 100) // Задано ошибочно ..
Percent=100; // .. то не более 100
if (Percent==0) // Если установлен 0 ..
Lots_New=Min_Lot; // ..то минимальный лот
else // Желаем. колич.лотов:
Lots_New=MathFloor(Free*Percent/100/One_Lot/Step)*Step;//Расч
}
if (Lots_New < Min_Lot) // Если меньше допуст..
Lots_New=Min_Lot; // .. то миниамальный
if (Lots_New*One_Lot > AccountFreeMargin()) // Не хватает даже..
{ // ..на минимальн. лот:(
Inform(11,0,Min_Lot); // Сообщение..
return(false); // ..и выход 
}
return(true); // Выход из польз. ф-ии
}

//+------------------------------------------------------------------+

// Функция вычисления торговых критериев.
// Возвращаемые значения:
// 10 - открытие Buy 
// 20 - открытие Sell 
// 11 - закрытие Buy
// 21 - закрытие Sell
// 0 - значимых критериев нет

int Criterion() // Пользовательская функция
{
string Sym=Symbol();
int i, k, o, m, s, f;
double Fx_0n, Fx_1n, Fx_2n, Fx_0, Fx_1, Sh1, Sh2, Sh3, Sh4, Sh0, Sh00;//

// Параметры пользоват. индикат:
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

Tral_Stop(0); // Трейлинг стоп Buy
Tral_Stop(1); // Трейлинг стоп Sell
if(mod(di_p_m5,di_m_m5)<Delta && mod(adx_m5,di_p_m5)<Delta && mod(adx_m5,di_m_m5)<Delta)
{
Comment("Час верх ",SHUp," ",Sh1," Лап ",i," Мин вверх ",SMUp," ",Sh3," Лап ",o," FxUp ",FUp," FxUpCl ",FclUp," Час вниз ",SHD," ",Sh2," Лап ",k," Мин вниз ",SMD," ",Sh4," Лап ",m," FxD ",FD," FxDCl ",FclD,
"\n ADX ",adx_m5," ADX 1 ",adx_1ago_m5," +DI ",di_p_m5," -DI ",di_m_m5," Fx на 0 баре ",Fx_0," Fx на 2 баре ",Fx_1,"  Сильный флет!!!");
FUp=false; FD=false; FclUp=false; FclD=false;
for(int l=1;l<=Mas_Ord_New[0][0];l++)
{ 
if(Mas_Ord_New[l][6]==0)
return(11); // Закрытие Buy 
if(Mas_Ord_New[l][6]==1)
return(21); // Закрытие Sell
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

Comment("Час верх ",SHUp," ",Sh1," Лап ",i," Мин вверх ",SMUp," ",Sh3," Лап ",o," FxUp ",FUp," FxUpCl ",FclUp," Час вниз ",SHD," ",Sh2," Лап ",k," Мин вниз ",SMD," ",Sh4," Лап ",m," FxD ",FD," FxDCl ",FclD,
"\n ADX ",adx_m5," ADX1 ",adx_1ago_m5," +DI ",di_p_m5," -DI ",di_m_m5," Fx на 0 баре ",Fx_0," Fx на 2 баре ",Fx_1);

if(SHUp==true && SMUp==true && FUp==true && di_p_m5>di_m_m5 && adx_m5>adx_1ago_m5)
return(10); // Открытие Buy 
if(SHD==true && SMD==true && FD==true && di_p_m5<di_m_m5 && adx_m5>adx_1ago_m5)
return(20); // Открытие Sell
if(SMD==true || FD==true || FclUp==true)
return(11); // Закрытие Buy 
if(SMUp==true || FUp==true || FclD==true)
return(21); // Закрытие Sell 

return(0); // Выход из пользов. функции
}

double mod(double x, double y)
{
  if((x-y)<0)
  return((x-y)*(-1));
  return(x-y);
}

double sglag2(double x, double y) //Функция сглаживания с коф. МА=2
{return((x+y)/2);}

//+------------------------------------------------------------------+
 
// Торговая функция.
int Trade(int Trad_Oper) // Пользовательская функция
{
// Trad_Oper - тип торговой операции:
// 10 - открытие Buy 
// 20 - открытие Sell 
// 11 - закрытие Buy
// 21 - закрытие Sell
// 0 - значимых критериев нет
// -1 - используется другой финансовый инструмент
switch(Trad_Oper)
{
case 10: // Торговый критерий = Buy
Close_All(1); // Закрыть все Sell
if (Lot()==false) // Средств не хватает на миним.
return; // Выход из пользов. функции
Open_Ord(0); // Открыть Buy
return; // Поторговали - уходим
case 11: // Торг. крит. = закрытие Buy
Close_All(0); // Закрыть все Buy
return; // Поторговали - уходим
case 20: // Торговый критерий = Sell
Close_All(0); // Закрыть все Buy
if (Lot()==false)
return; // Выход из пользов. функции
Open_Ord(1); // Открыть Sell 
return; // Поторговали - уходим
case 21: // Торг. крит. = закрытие Sell
Close_All(1); // Закрыть все Sell
return; // Поторговали - уходим
case 0: // Удержание открытых позиций
Tral_Stop(0); // Трейлинг стоп Buy
Tral_Stop(1); // Трейлинг стоп Sell
return; // Поторговали - уходим
}
}

//+------------------------------------------------------------------+

// Функция закрытия всех рыночных ордеров указанного типа
// Глобальные переменные:
// Mas_Ord_New Массив ордеров последний известный
// Mas_Tip Массив типов ордеров
int Close_All(int Tip) // Пользовательская функция
{
// int Tip // Тип ордера
int Ticket=0; // Номер ордера
double Lot=0; // Количество закр. лотов
double Price_Cls; // Цена закрытия ордера--
while(Mas_Tip[Tip]>0) // До тех пор, пока есть ..
{ //.. ордера заданного типа 
for(int i=1; i<=Mas_Ord_New[0][0]; i++)// Цикл по живым ордерам
{
if(Mas_Ord_New[i][6]==Tip && // Среди ордеров нашего типа
Mas_Ord_New[i][5]>Lot) // .. выбираем самый дорогой
{ // Этот больше ранее найден.
Lot=Mas_Ord_New[i][5]; // Наибольший найденный лот
Ticket=Mas_Ord_New[i][4]; // Номер его ордера такой
}
}
if (Tip==0) Price_Cls=Bid; // Для ордеров Buy
if (Tip==1) Price_Cls=Ask; // Для ордеров Sell
Inform(12,Ticket); // Сообщение о попытке закр.
bool Ans=OrderClose(Ticket,Lot,Price_Cls,2);// Закрыть ордер !:)
if (Ans==false) // Не получилось :( 
{ // Поинтересуемся ошибками:
if(Errors(GetLastError())==false)// Если ошибка непреодолимая
return; // .. то уходим.
}
Terminal(); // Функция учёта ордеров 
Events(); // Отслеживание событий
}
return; // Выход из пользов. функции
}

//+------------------------------------------------------------------+

// Функция открытия одного рыночного ордера указанного типа
// Глобальные переменные:
// int Mas_Tip Массив типов ордеров
// int StopLoss Значение StopLoss (количество пунктов)
// int TakeProfit Значение TakeProfit (количество пунктов)
int Open_Ord(int Tip)
{
int Ticket, // Номер ордера
MN; // MagicNumber
double SL, // StopLoss (относит.знач.цены)
TP; // TakeProf (относит.знач.цены)
while(Mas_Tip[Tip]==0) // До тех пор, пока ..
{ //.. не достигнут успех
if (StopLoss<Level_new) // Если меньше допустимого..
StopLoss=Level_new; // .. то допустимый
if (TakeProfit<Level_new) // Если меньше допустимого..
TakeProfit=Level_new; // ..то допустимый
MN=TimeCurrent(); // Простой MagicNumber
Inform(13,Tip); // Сообщение о попытке откр
if (Tip==0) // Будем открывать Buy
{
SL=Bid - StopLoss* Point; // StopLoss (цена)
TP=Bid + TakeProfit*Point; // TakeProfit (цена)
Ticket=OrderSend(Symbol(),0,Lots_New,Ask,2,SL,TP,"",MN);
}
if (Tip==1) // Будем открывать Sell
{
SL=Ask + StopLoss* Point; // StopLoss (цена)
TP=Ask - TakeProfit*Point; // TakeProfit (цена)
Ticket=OrderSend(Symbol(),1,Lots_New,Bid,2,SL,TP,"",MN);
}
if (Ticket<0) // Не получилось :( 
{ // Поинтересуемся ошибками:
if(Errors(GetLastError())==false)// Если ошибка непреодолимая
return; // .. то уходим.
}
Terminal(); // Функция учёта ордеров 
Events(); // Отслеживание событий
}
return; // Выход из пользов. функции
}

//+------------------------------------------------------------------+

// Функция модификации StopLoss всех ордеров указанного типа
// Глобальные переменные:
// Mas_Ord_New Массив ордеров последний известный
// int TralingStop Значение TralingStop(количество пунктов)
int Tral_Stop(int Tip)
{
int Ticket; // Номер ордера
double
Price, // Цена открытия рыночного ордера
TS, // TralingStop (относит.знач.цены)
SL, // Значение StopLoss ордера
TP; // Значение TakeProfit ордера
bool Modify; // Признак необходимости модифи.
for(int i=1;i<=Mas_Ord_New[0][0];i++) // Цикл по всем ордерам
{ // Ищем ордера задан. типа
if (Mas_Ord_New[i][6]!=Tip) // Если это не наш тип..
continue; //.. то переступим ордер
Modify=false; // Пока не назначен к модифи
Price =Mas_Ord_New[i][1]; // Цена открытия ордера
SL =Mas_Ord_New[i][2]; // Значение StopLoss ордера
TP =Mas_Ord_New[i][3]; // Значение TakeProft ордера
Ticket=Mas_Ord_New[i][4]; // Номер ордера
if (TralingStop<Level_new) // Если меньше допустимого..
TralingStop=Level_new; // .. то допустимый
TS=TralingStop*Point; // То же в относит.знач.цены
switch(Tip) // Переход на тип ордера
{
case 0 : // Ордер Buy
if (NormalizeDouble(SL,Digits)<// Если ниже желаемого..
NormalizeDouble(Bid-TS,Digits))
{ // ..то модифицируем его:
SL=Bid-TS; // Новый его StopLoss
Modify=true; // Назначен к модифи.
}
break; // Выход из switch
case 1 : // Ордер Sell
if (NormalizeDouble(SL,Digits)>// Если выше желаемого..
NormalizeDouble(Ask+TS,Digits)||
NormalizeDouble(SL,Digits)==0)//.. или нулевой(!)
{ // ..то модифицируем его
SL=Ask+TS; // Новый его StopLoss
Modify=true; // Назначен к модифи.
}
} // Конец switch
if (Modify==false) // Если его не надо модифи..
continue; // ..то идём по циклу дальше
bool Ans=OrderModify(Ticket,Price,SL,TP,0);//Модифицируем его!
if (Ans==false) // Не получилось :( 
{ // Поинтересуемся ошибками:
if(Errors(GetLastError())==false)// Если ошибка непреодолимая
return; // .. то уходим.
i--; // Понижение счётчика
}
Terminal(); // Функция учёта ордеров 
Events(); // Отслеживание событий
}
return; // Выход из пользов. функции
}

//+------------------------------------------------------------------+

// Функция обработки ошибок.
// Возвращаемые значения:
// true - если ошибка преодолимая (т.е. можно продолжать работу)
// false - если ошибка критическая (т.е. торговать нельзя)
bool Errors(int Error) // Пользовательская функция
{
// Error // Номер ошибки 
if(Error==0)
return(false); // Нет ошибки
Inform(15,Error); // Сообщение
switch(Error)
{ // Преодолимые ошибки:
case 129: // Неправильная цена
case 135: // Цена изменилась
RefreshRates(); // Обновим данные
return(true); // Ошибка преодолимая
case 136: // Нет цен. Ждём новый тик.
while(RefreshRates()==false) // До нового тика
Sleep(1); // Задержка в цикле
return(true); // Ошибка преодолимая
case 146: // Подсистема торговли занята
Sleep(500); // Простое решение
RefreshRates(); // Обновим данные
return(true); // Ошибка преодолимая
// Критические ошибки:
case 2 : // Общая ошибка
case 5 : // Старая версия клиентского терминала
case 64: // Счет заблокирован
case 133: // Торговля запрещена
default: // Другие варианты
return(false); // Критическая ошибка
}
}
//+------------------------------------------------------------------+