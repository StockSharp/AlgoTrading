

double AccountBalance()
{
  return AccountInfoDouble(ACCOUNT_BALANCE);
}

double AccountCredit()
{
  return AccountInfoDouble(ACCOUNT_CREDIT);
}

double AccountEquity()
{
  return AccountInfoDouble(ACCOUNT_EQUITY);
}

double AccountFreeMargin()
{
  return AccountInfoDouble(ACCOUNT_MARGIN_FREE);
}

double AccountMargin()
{
  return AccountInfoDouble(ACCOUNT_MARGIN);
}

double AccountProfit()
{
  return AccountInfoDouble(ACCOUNT_PROFIT);
}

double AccountStopoutLevel()
{
  return AccountInfoDouble(ACCOUNT_MARGIN_SO_SO);
}

double AccountFreeMarginCheck(string symbol, int cmd, double volume)
{
  double margin = 0;
  if(OrderCalcMargin((ENUM_ORDER_TYPE)cmd, symbol, volume, (cmd == 0 ? SymbolInfoDouble(symbol, SYMBOL_ASK) : SymbolInfoDouble(symbol, SYMBOL_BID)), margin))
  {
    return AccountFreeMargin() - margin;
  }
  return AccountFreeMargin();
}

long AccountFreeMarginMode()
{
  Print(__FUNCTION__, " doesn't have exact counterpart in MQL5");
  return 0;
}

long AccountLeverage()
{
  return AccountInfoInteger(ACCOUNT_LEVERAGE);
}

long AccountNumber()
{
  return AccountInfoInteger(ACCOUNT_LOGIN);
}

long AccountStopoutMode()
{
  return AccountInfoInteger(ACCOUNT_MARGIN_SO_MODE);
}




string AccountCurrency()
{
  return AccountInfoString(ACCOUNT_CURRENCY);
}

string AccountName()
{
  return AccountInfoString(ACCOUNT_NAME);
}

string AccountServer()
{
  return AccountInfoString(ACCOUNT_SERVER);
}

string AccountCompany()
{
  return AccountInfoString(ACCOUNT_COMPANY);
}



