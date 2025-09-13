//+------------------------------------------------------------------+
//|                                                        panel.mq5 |
//|                                                     Igor Volodin |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Igor Volodin"
#property link      "http://www.mql5.com"
#property version   "1.00"
#property indicator_separate_window
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit() {
	IndicatorSetString(INDICATOR_SHORTNAME, "panel");
	return(0);
}
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total, const int prev_calculated, const datetime& time[], const double& open[], const double& high[], const double& low[], const double& close[], const long& tick_volume[], const long& volume[], const int& spread[]) {
	return(rates_total);
}
//+------------------------------------------------------------------+
