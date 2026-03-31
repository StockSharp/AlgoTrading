import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class train_yourself_strategy(Strategy):
    """Donchian channel breakout: arm inside channel, trade on breakout, exit at midline."""
    def __init__(self):
        super(train_yourself_strategy, self).__init__()
        self._channel_length = self.Param("ChannelLength", 20).SetDisplay("Channel Length", "Highest/Lowest period", "Channel")
        self._atr_length = self.Param("AtrLength", 14).SetDisplay("ATR Length", "ATR for stop distance", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(train_yourself_strategy, self).OnReseted()
        self._prev_highest = 0
        self._prev_lowest = 0
        self._is_armed = False
        self._entry_price = 0

    def OnStarted2(self, time):
        super(train_yourself_strategy, self).OnStarted2(time)
        self._prev_highest = 0
        self._prev_lowest = 0
        self._is_armed = False
        self._entry_price = 0

        highest = Highest()
        highest.Length = self._channel_length.Value
        lowest = Lowest()
        lowest.Length = self._channel_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(highest, lowest, atr, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, high_val, low_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_highest == 0 or self._prev_lowest == 0 or atr_val <= 0:
            self._prev_highest = float(high_val)
            self._prev_lowest = float(low_val)
            return

        close = float(candle.ClosePrice)
        upper = self._prev_highest
        lower = self._prev_lowest
        mid = (upper + lower) / 2.0

        # Manage position: exit on channel re-entry
        if self.Position > 0:
            if close < mid:
                self.SellMarket()
                self._entry_price = 0
                self._is_armed = False
        elif self.Position < 0:
            if close > mid:
                self.BuyMarket()
                self._entry_price = 0
                self._is_armed = False

        # Arm when price is inside channel
        if self.Position == 0:
            if not self._is_armed:
                margin = float(atr_val) * 0.2
                if close > lower + margin and close < upper - margin:
                    self._is_armed = True
            else:
                if close > upper:
                    self._entry_price = close
                    self.BuyMarket()
                    self._is_armed = False
                elif close < lower:
                    self._entry_price = close
                    self.SellMarket()
                    self._is_armed = False

        self._prev_highest = float(high_val)
        self._prev_lowest = float(low_val)

    def CreateClone(self):
        return train_yourself_strategy()
