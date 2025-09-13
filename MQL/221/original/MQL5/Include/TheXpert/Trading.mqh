//+------------------------------------------------------------------+
//|                                                      Trading.mqh |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"

#include <Trade/Trade.mqh>
#include <Trade/AccountInfo.mqh>

#include "Comments.mqh"
#include "Utils.mqh"

class MyTrade
{
public:
   MyTrade();
   void SetSlippage(int slipPts);
   void SetFilling(ENUM_ORDER_TYPE_FILLING filling);
   void SetComment(string newComment);
   
   bool SetStop(double stop);
   bool SetTP(double stop);
   bool SetSL(double stop);

   long Buy(double lots);
   long BuyTP(double lots, double TP);
   long BuySL(double lots, double SL);
   
   long Sell(double lots);
   long SellTP(double lots, double TP);
   long SellSL(double lots, double SL);
   
   long OpenBuyLimit(double lots, double price);
   long OpenBuyStop(double lots, double price);

   long OpenSellLimit(double lots, double price);
   long OpenSellStop(double lots, double price);

   bool Close();
   bool Reverse();

   bool GetCurrentPos(ENUM_POSITION_TYPE& type, double& lot);
   bool GetCurrentSLTP(double& SL, double& TP);
   
   bool CanTrade();

private: 
   CTrade m_Trade;
   CPositionInfo m_Position;
   CAccountInfo m_Account;
   string m_Comment;
   int m_LotDigits;
   int m_Slippage;
};

MyTrade::MyTrade()
{
   m_Comment = "Trade Xpert";
   m_LotDigits = DoubleDigits(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP));
   SetSlippage(10);
}

bool MyTrade::CanTrade()
{
   if (!m_Account.TradeAllowed()) 
   {
      Comment_("Trade is not allowed");
      return false;
   }
   
   if (!m_Account.TradeExpert())
   {
      Comment_("Tradind by experts is disabled. Enable this option and try again");
      return false;
   }
   
   return true;
}


bool MyTrade::GetCurrentPos(ENUM_POSITION_TYPE& type, double& lot)
{
   if (!m_Position.Select(_Symbol))
   {
      return false;
   }
   
   type = ENUM_POSITION_TYPE(m_Position.Type());
   lot = m_Position.Volume();
   
   return true;
}

bool MyTrade::GetCurrentSLTP(double& SL, double& TP)
{
   if (!m_Position.Select(_Symbol))
   {
      return false;
   }
   
   SL = m_Position.StopLoss();
   TP = m_Position.TakeProfit();

   return true;
}



bool MyTrade::SetStop(double stop)
{
   if (!CanTrade()) return false;
   
   ENUM_POSITION_TYPE type;
   double lot;
   if (!GetCurrentPos(type, lot))
   {
      Comment_("Can not set stops without position available");
      return false;
   }
   
   if (type == POSITION_TYPE_BUY)
   {
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      if (stop > bid)
      {
         return SetTP(stop);
      }

      return SetSL(stop);
   }
   else if (type == POSITION_TYPE_SELL)
   {
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      if (stop > bid)
      {
         return SetSL(stop);
      }

      return SetTP(stop);
   }
   return false;
}

bool MyTrade::SetTP(double stop)
{
   if (!CanTrade()) return false;
   
   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);

   if (m_Trade.PositionModify(_Symbol, sl, stop))
   {
      string str = "TP set to ";
      str = str + DoubleToString(stop) + " successfully";
      Comment_(str);
      
      return true;
   }
   
   string str = "Set TP to ";
   str = str + DoubleToString(stop) + " failed, error #";
   str = str + string(m_Trade.ResultRetcode()) + " (";
   str = str + m_Trade.ResultRetcodeDescription() + ")";
   Comment_(str);
   
   return false;
}

bool MyTrade::SetSL(double stop)
{
   if (!CanTrade()) return false;

   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);

   if (m_Trade.PositionModify(_Symbol, stop, tp))
   {
      string str = "SL set to ";
      str = str + DoubleToString(stop) + " successfully";
      Comment_(str);
      
      return true;
   }
   
   string str = "Set SL to ";
   str = str + DoubleToString(stop) + " failed, error #";
   str = str + string(m_Trade.ResultRetcode()) + " (";
   str = str + m_Trade.ResultRetcodeDescription() + ")";
   Comment_(str);
   
   return false;
}

void MyTrade::SetSlippage(int slipPts)
{
   m_Trade.SetDeviationInPoints(slipPts);
   m_Slippage = slipPts;
}

void MyTrade::SetFilling(ENUM_ORDER_TYPE_FILLING filling)
{
   m_Trade.SetTypeFilling(filling);
}

void MyTrade::SetComment(string newComment)
{
   m_Comment = newComment;
}

long MyTrade::Buy(double lots)
{
   if (!CanTrade()) return -1;
   
   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);
   
   ENUM_POSITION_TYPE type;
   double lot;
   
   if (GetCurrentPos(type, lot))
   {
      if (type == POSITION_TYPE_SELL)
      {
         double tmp = sl;
         sl = tp;
         tp = tmp;
      }
   }

   if(m_Trade.Buy(lots, _Symbol, 0, sl, tp, m_Comment))
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
      str = str + " Buy at ";
      str = str + DoubleToString(m_Trade.ResultPrice(), _Digits) + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy failed, error code is #";
      str = str + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);

      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::BuyTP(double lots, double TP)
{
   if (!CanTrade()) return -1;

   double normTP = NormalizeDouble(TP, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (TP < ask)
   {
      Comment_("Inconsistent TP value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(TP)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);

      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_SELL)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Buy(lots, _Symbol, 0, sl, normTP, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Buy at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with TP " + DoubleToString(normTP, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Buy with TP failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::BuySL(double lots, double SL)
{
   if (!CanTrade()) return -1;

   double normSL = NormalizeDouble(SL, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (SL > ask)
   {
      Comment_("Inconsistent SL value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(SL)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);

      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_SELL)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Buy(lots, _Symbol, 0, normSL, tp, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Buy at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with SL " + DoubleToString(normSL, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Buy with SL failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::Sell(double lots)
{
   if (!CanTrade()) return -1;

   double sl = 0, tp = 0;
   GetCurrentSLTP(sl, tp);

   ENUM_POSITION_TYPE type;
   double lot;
   
   if (GetCurrentPos(type, lot))
   {
      if (type == POSITION_TYPE_BUY)
      {
         double tmp = sl;
         sl = tp;
         tp = tmp;
      }
   }

   if(m_Trade.Sell(lots, _Symbol, 0, sl, tp, m_Comment))
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
      str = str + " Sell at ";
      str = str + DoubleToString(m_Trade.ResultPrice(), _Digits) + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command ";
      str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell failed, error code is #";
      str = str + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);

      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::SellTP(double lots, double TP)
{
   if (!CanTrade()) return -1;

   double normTP = NormalizeDouble(TP, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (TP > bid)
   {
      Comment_("Inconsistent TP value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(TP)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);
   
      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_BUY)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Sell(lots, _Symbol, 0, sl, normTP, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Sell at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with TP " + DoubleToString(normTP, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Sell with TP failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::SellSL(double lots, double SL)
{
   if (!CanTrade()) return -1;

   double normSL = NormalizeDouble(SL, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (SL < bid)
   {
      Comment_("Inconsistent SL value, stopping command");
      return (-1);
   }
   
   if (lots == 0)
   {
      if (SetStop(SL)) return 0;
      else           return -1;
   }
   else
   {
      double sl = 0, tp = 0;
      GetCurrentSLTP(sl, tp);
   
      ENUM_POSITION_TYPE type;
      double lot;
      
      if (GetCurrentPos(type, lot))
      {
         if (type == POSITION_TYPE_BUY)
         {
            double tmp = sl;
            sl = tp;
            tp = tmp;
         }
      }

      if (m_Trade.Sell(lots, _Symbol, 0, normSL, tp, m_Comment))
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.ResultVolume(), m_LotDigits);
         str = str + " Sell at ";
         str = str + DoubleToString(m_Trade.ResultPrice(), _Digits);
         str = str + " with SL " + DoubleToString(normSL, _Digits);
         str = str + " succeeded ";
         Comment_(str);
         
         return long(m_Trade.ResultDeal());
      }
      else
      {
         string str = "Command ";
         str = str + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
         str = str + " Sell with SL failed, error code is #";
         str = str + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);
   
         return -long(m_Trade.ResultRetcode());
      }
   }
}

long MyTrade::OpenBuyLimit(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (normPrice > ask)
   {
      Comment_("Inconsistent price for Buy Limit, stopping command");
      return (-1);
   }
   
   if (m_Trade.BuyLimit(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::OpenBuyStop(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   if (normPrice < ask)
   {
      Comment_("Inconsistent price for Buy Stop, stopping command");
      return (-1);
   }
   
   if (m_Trade.BuyStop(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Buy Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::OpenSellLimit(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (normPrice < bid)
   {
      Comment_("Inconsistent price for Sell Limit, stopping command");
      return (-1);
   }
   
   if (m_Trade.SellLimit(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Limit at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

long MyTrade::OpenSellStop(double lots, double price)
{
   if (!CanTrade()) return -1;

   double normPrice = NormalizeDouble(price, _Digits);
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   if (normPrice > bid)
   {
      Comment_("Inconsistent price for Sell Stop, stopping command");
      return (-1);
   }
   
   if (m_Trade.SellStop(lots, normPrice, _Symbol, 0, 0, 0, 0, m_Comment))
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " succeeded";
      Comment_(str);
      
      return long(m_Trade.ResultDeal());
   }
   else
   {
      string str = "Command Open " + DoubleToString(m_Trade.RequestVolume(), m_LotDigits);
      str = str + " Sell Stop at " + DoubleToString(normPrice, _Digits);
      str = str + " failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return -long(m_Trade.ResultRetcode());
   }
}

bool MyTrade::Close()
{
   if (!CanTrade()) return -1;
   
   ENUM_POSITION_TYPE type;
   double lot;
      
   if (!GetCurrentPos(type, lot))
   {
      Comment_("No position to close");
      return false;
   }

   if (m_Trade.PositionClose(_Symbol, m_Slippage))
   {
      Comment_("Position closed successfully");
      return true;
   }
   else
   {
      string str = "Close Position failed, error#" + string(m_Trade.ResultRetcode()) + " (";
      str = str + m_Trade.ResultRetcodeDescription() + ")";
      Comment_(str);
      
      return false;
   }
}

bool MyTrade::Reverse()
{
   if (!CanTrade()) return -1;
   
   ENUM_POSITION_TYPE type;
   double lot;
   
   if (!GetCurrentPos(type, lot))
   {
      Comment_("Can not reverse zero position");
      return false;
   }
   
   if (type == POSITION_TYPE_BUY)
   {
      if (m_Trade.Sell(2*lot, _Symbol, 0, 0, 0, m_Comment))
      {
         m_Trade.PositionModify(_Symbol, 0, 0);
         Comment_(DoubleToString(lot, m_LotDigits) + " Buy reversed successfully");
         
         return true;
      }
      else
      {
         string str = "Reverse Position failed, error#" + string(m_Trade.ResultRetcode()) + " (";
         str = str + m_Trade.ResultRetcodeDescription() + ")";
         Comment_(str);

         return false;
      }
   }
   else if (type == POSITION_TYPE_SELL)
   {
      if (m_Trade.Buy(2*lot, _Symbol, 0, 0, 0, m_Comment))
      {
         m_Trade.PositionModify(_Symbol, 0, 0);
         Comment_(DoubleToString(lot, m_LotDigits) + " Sell reversed successfully");
         
         return true;
      }
      else
      {
         Comment_("Reverse position failed");
         return false;
      }
   }
   return false;
}