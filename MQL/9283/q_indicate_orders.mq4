//+------------------------------------------------------------------+
//|                                            #_indicate_orders.mq4 |
//|                                                Александр Смирнов |
//|                                                   rainal@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Александр Смирнов"
#property link      "rainal@mail.ru"

extern string font_name   =  "Tahoma";     // шрифт текстовых меток
extern int    font_size1  =  10;           // размер шрифта заголовков
extern int    font_size2  =  9;            // размер шрифта списка позиций
extern int    labs_corner =  1;            // угол привязки текстовых меток
extern int    labs_xdist  =  5;            // расcтояние X от границы окна
extern int    labs_ydist  =  10;           // расстояние Y от границы окна
extern int    labs_space  =  17;           // интервал Y между метками
extern int    labs_max    =  25;           // максимальное количество меток
extern color  clr_profit  =  DeepSkyBlue;  // цвет профитных позиций
extern color  clr_loss    =  DeepPink;     // цвет убыточных позиций

//--------------------------------------------------------------------------+

void init()
{
	// создание 25 текстовых поля для вывода информации
	for(int i = 1; i <= labs_max; i++)
	{
	  ObjectCreate("lab"+i, OBJ_LABEL, 0, 0, 0);
  	ObjectSet("lab"+i, OBJPROP_CORNER, labs_corner);
	  ObjectSet("lab"+i, OBJPROP_XDISTANCE, labs_xdist);
  	ObjectSet("lab"+i, OBJPROP_YDISTANCE, i * labs_space + labs_ydist);
    ObjectSetText("lab"+i, "");
  }
}

//--------------------------------------------------------------------------+

void deinit()
{
	// удаление всех текстовых полей
	for(int i = 1; i <= labs_max; i++) ObjectDelete("lab"+i);
}

//--------------------------------------------------------------------------+

void indicate_orders()
{
  int i, nb = 0, ns = 0, lab = 1, lbn = 0;
  int buys_profit = 0, sells_profit = 0;
  double buys_volume = 0.0, sells_volume = 0.0;
  double buys_list[50][3];
  double sells_list[50][3];
  string res;

  // подсчет всех открытых позиций на данный момент по текущей паре
  for(i = 0; i < OrdersTotal(); i++)
  {
    if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) break;
    if(OrderSymbol() != Symbol()) continue;
    
    if(OrderType() == OP_BUY)
    {
      buys_list[nb][0] = OrderTicket();
      buys_list[nb][1] = OrderProfit();
      buys_list[nb][2] = OrderLots();
      buys_profit += buys_list[nb][1];
      buys_volume += buys_list[nb][2];
      nb++;
    }
    if(OrderType() == OP_SELL)
    {
      sells_list[ns][0] = OrderTicket();
      sells_list[ns][1] = OrderProfit();
      sells_list[ns][2] = OrderLots();
      sells_profit += sells_list[ns][1];
      sells_volume += sells_list[ns][2];
      ns++;
    }
  }

  // отображение всех открытых позиций
  if(nb > 0)
  {
    ArrayResize(buys_list, nb);
    if(buys_profit > 0)
    {
      res = "BUYS | " + nb + " | " + DoubleToStr(buys_volume, 2) + " | + " + buys_profit;
      ObjectSetText("lab"+lab, res, font_size1, font_name, clr_profit);
      lab++;
      ObjectSetText("lab"+lab, "------------------", font_size1, font_name, clr_profit);
    }
    else
    {
      res = "BUYS | " + nb + " | " + DoubleToStr(buys_volume, 2) + " | " + buys_profit;
      ObjectSetText("lab"+lab, res, font_size1, font_name, clr_loss);
      lab++;
      ObjectSetText("lab"+lab, "------------------", font_size1, font_name, clr_loss);
    }
    lab++;
    lbn = lab;
    for(i = 0; i < nb; i++)
    {
      lab = lbn + i;
      if(buys_list[i][1] > 0)
      {
        res = "#" + DoubleToStr(buys_list[i][0], 0) + ": " + DoubleToStr(buys_list[i][2], 2) + ": +" + DoubleToStr(buys_list[i][1], 0);
        ObjectSetText("lab"+lab, res, font_size2, font_name, clr_profit);
      }
      else
      {
        res = "#" + DoubleToStr(buys_list[i][0], 0) + ": " + DoubleToStr(buys_list[i][2], 2) + ": " + DoubleToStr(buys_list[i][1], 0);
        ObjectSetText("lab"+lab, res, font_size2, font_name, clr_loss);
      }
    }
  }
  if(ns > 0)
  {
    ArrayResize(sells_list, ns);
    if(nb > 0)
    {
      lab++;
      ObjectSetText("lab"+lab, "");
      lab++;
    }
    if(sells_profit > 0)
    {
      res = "SELLS | " + ns + " | " + DoubleToStr(sells_volume, 2) + " | +" + sells_profit;
      ObjectSetText("lab"+lab, res, font_size1, font_name, clr_profit);
      lab++;
      ObjectSetText("lab"+lab, "------------------", font_size1, font_name, clr_profit);
    }
    else
    {
      res = "SELLS | " + ns + " | " + DoubleToStr(sells_volume, 2) + " | " + sells_profit;
      ObjectSetText("lab"+lab, res, font_size1, font_name, clr_loss);
      lab++;
      ObjectSetText("lab"+lab, "------------------", font_size1, font_name, clr_loss);
    }
    lab++;
    lbn = lab;
    for(i = 0; i < ns; i++)
    {
      lab = lbn + i;
      if(sells_list[i][1] > 0)
      {
        res = "#" + DoubleToStr(sells_list[i][0], 0) + ": " + DoubleToStr(sells_list[i][2], 2) + ": +" + DoubleToStr(sells_list[i][1], 0);
        ObjectSetText("lab"+lab, res, font_size2, font_name, clr_profit);
      }
      else
      {
        res = "#" + DoubleToStr(sells_list[i][0], 0) + ": " + DoubleToStr(sells_list[i][2], 2) + ": " + DoubleToStr(sells_list[i][1], 0);
        ObjectSetText("lab"+lab, res, font_size2, font_name, clr_loss);
      }
    }
  }
  for(i = lab+1; i <= labs_max; i++) ObjectSetText("lab"+i, "");
}

//--------------------------------------------------------------------------+

void start()
{
  indicate_orders();

  //
  // ваш текст советника
  //
}