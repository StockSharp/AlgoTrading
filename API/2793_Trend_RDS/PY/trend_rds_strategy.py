import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class trend_rds_strategy(Strategy):
    """Trend recognition via three consecutive candle highs/lows with SL/TP management."""
    def __init__(self):
        super(trend_rds_strategy, self).__init__()
        self._sl_points = self.Param("StopLossPoints", 30).SetDisplay("Stop Loss", "Stop loss distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 65).SetDisplay("Take Profit", "Take profit distance", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(trend_rds_strategy, self).OnReseted()
        self._prev_high1 = 0
        self._prev_high2 = 0
        self._prev_high3 = 0
        self._prev_low1 = 0
        self._prev_low2 = 0
        self._prev_low3 = 0
        self._history_count = 0
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0

    def OnStarted(self, time):
        super(trend_rds_strategy, self).OnStarted(time)
        self._prev_high1 = 0
        self._prev_high2 = 0
        self._prev_high3 = 0
        self._prev_low1 = 0
        self._prev_low2 = 0
        self._prev_low3 = 0
        self._history_count = 0
        self._entry_price = 0
        self._stop_price = 0
        self._take_price = 0

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        sl = self._sl_points.Value
        tp = self._tp_points.Value

        # Handle exits
        if self.Position > 0:
            if self._stop_price > 0 and low <= self._stop_price:
                self.SellMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0
                self._update_history(candle)
                return
            if self._take_price > 0 and high >= self._take_price:
                self.SellMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0
                self._update_history(candle)
                return
        elif self.Position < 0:
            if self._stop_price > 0 and high >= self._stop_price:
                self.BuyMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0
                self._update_history(candle)
                return
            if self._take_price > 0 and low <= self._take_price:
                self.BuyMarket()
                self._entry_price = 0
                self._stop_price = 0
                self._take_price = 0
                self._update_history(candle)
                return

        if self._history_count >= 3:
            higher_lows = self._prev_low1 > self._prev_low2 and self._prev_low2 > self._prev_low3
            lower_highs = self._prev_high1 < self._prev_high2 and self._prev_high2 < self._prev_high3
            conflict = higher_lows and lower_highs

            if not conflict:
                if higher_lows and self.Position <= 0:
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
                    self._entry_price = close
                    self._stop_price = close - sl if sl > 0 else 0
                    self._take_price = close + tp if tp > 0 else 0
                elif lower_highs and self.Position >= 0:
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()
                    self._entry_price = close
                    self._stop_price = close + sl if sl > 0 else 0
                    self._take_price = close - tp if tp > 0 else 0

        self._update_history(candle)

    def _update_history(self, candle):
        self._prev_high3 = self._prev_high2
        self._prev_high2 = self._prev_high1
        self._prev_high1 = float(candle.HighPrice)
        self._prev_low3 = self._prev_low2
        self._prev_low2 = self._prev_low1
        self._prev_low1 = float(candle.LowPrice)
        if self._history_count < 3:
            self._history_count += 1

    def CreateClone(self):
        return trend_rds_strategy()
