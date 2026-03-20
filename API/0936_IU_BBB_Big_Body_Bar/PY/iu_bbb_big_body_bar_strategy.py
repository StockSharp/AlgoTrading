import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class iu_bbb_big_body_bar_strategy(Strategy):
    def __init__(self):
        super(iu_bbb_big_body_bar_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(240))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._big_body_threshold = self.Param("BigBodyThreshold", 1.5) \
            .SetDisplay("Big Body Threshold", "Multiplier of average body", "Parameters")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Period", "ATR indicator period", "Indicators")
        self._atr_factor = self.Param("AtrFactor", 2.0) \
            .SetDisplay("ATR Factor", "ATR multiplier for trailing stop", "Risk Management")
        self._sum_body = 0.0
        self._body_count = 0
        self._atr_stop = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(iu_bbb_big_body_bar_strategy, self).OnReseted()
        self._sum_body = 0.0
        self._body_count = 0
        self._atr_stop = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(iu_bbb_big_body_bar_strategy, self).OnStarted(time)
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, atr_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        atr_v = float(atr_val)
        body = abs(close - open_p)
        self._sum_body += body
        self._body_count += 1
        avg_body = self._sum_body / self._body_count
        if self._body_count < 20 or avg_body <= 0:
            return
        threshold = float(self._big_body_threshold.Value)
        factor = float(self._atr_factor.Value)
        long_cond = body > avg_body * threshold and close > open_p
        short_cond = body > avg_body * threshold and close < open_p
        if self.Position > 0:
            if self._atr_stop is None:
                self._atr_stop = self._entry_price - atr_v * factor
            else:
                new_stop = close - atr_v * factor
                if new_stop > self._atr_stop:
                    self._atr_stop = new_stop
            if low <= self._atr_stop:
                self.SellMarket()
                self._atr_stop = None
            return
        elif self.Position < 0:
            if self._atr_stop is None:
                self._atr_stop = self._entry_price + atr_v * factor
            else:
                new_stop = close + atr_v * factor
                if new_stop < self._atr_stop:
                    self._atr_stop = new_stop
            if high >= self._atr_stop:
                self.BuyMarket()
                self._atr_stop = None
            return
        if long_cond:
            self.BuyMarket()
            self._entry_price = close
        elif short_cond:
            self.SellMarket()
            self._entry_price = close

    def CreateClone(self):
        return iu_bbb_big_body_bar_strategy()
