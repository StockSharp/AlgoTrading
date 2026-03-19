import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BullPower
from StockSharp.Algo.Strategies import Strategy

class jk_bull_power_auto_trader_strategy(Strategy):
    """
    Bulls Power indicator strategy.
    Sells when Bulls Power weakens above zero, buys when below zero.
    """

    def __init__(self):
        super(jk_bull_power_auto_trader_strategy, self).__init__()
        self._bulls_period = self.Param("BullsPeriod", 13).SetDisplay("Bulls Period", "Indicator length", "Indicators")
        self._tp_points = self.Param("TakeProfitPoints", 350.0).SetDisplay("TP", "TP in price steps", "Risk")
        self._sl_points = self.Param("StopLossPoints", 100.0).SetDisplay("SL", "SL in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_bulls = None
        self._prev_prev_bulls = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._tp_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(jk_bull_power_auto_trader_strategy, self).OnReseted()
        self._prev_bulls = None
        self._prev_prev_bulls = None
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._tp_price = 0.0

    def OnStarted(self, time):
        super(jk_bull_power_auto_trader_strategy, self).OnStarted(time)
        bp = BullPower()
        bp.Length = self._bulls_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(bp, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bulls_val):
        if candle.State != CandleStates.Finished:
            return

        bv = float(bulls_val)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0

        if self.Position > 0:
            if self._stop_price > 0 and low <= self._stop_price:
                self.SellMarket()
                self._stop_price = 0
                self._tp_price = 0
                self._update_history(bv)
                return
            if self._tp_price > 0 and high >= self._tp_price:
                self.SellMarket()
                self._stop_price = 0
                self._tp_price = 0
                self._update_history(bv)
                return
        elif self.Position < 0:
            if self._stop_price > 0 and high >= self._stop_price:
                self.BuyMarket()
                self._stop_price = 0
                self._tp_price = 0
                self._update_history(bv)
                return
            if self._tp_price > 0 and low <= self._tp_price:
                self.BuyMarket()
                self._stop_price = 0
                self._tp_price = 0
                self._update_history(bv)
                return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._update_history(bv)
            return

        if self._prev_bulls is None or self._prev_prev_bulls is None:
            self._update_history(bv)
            return

        sell_signal = self._prev_prev_bulls > self._prev_bulls and self._prev_bulls > 0 and bv < self._prev_bulls
        buy_signal = self._prev_prev_bulls < self._prev_bulls and self._prev_bulls < 0 and bv > self._prev_bulls

        if sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._stop_price = close + self._sl_points.Value * step
            self._tp_price = close - self._tp_points.Value * step
        elif buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._stop_price = close - self._sl_points.Value * step
            self._tp_price = close + self._tp_points.Value * step

        self._update_history(bv)

    def _update_history(self, bv):
        self._prev_prev_bulls = self._prev_bulls
        self._prev_bulls = bv

    def CreateClone(self):
        return jk_bull_power_auto_trader_strategy()
