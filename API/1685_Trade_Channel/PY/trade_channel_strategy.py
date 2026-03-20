import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class trade_channel_strategy(Strategy):
    def __init__(self):
        super(trade_channel_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 20) \
            .SetDisplay("Channel Period", "Donchian channel period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR length for stop calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._stop_price = 0.0
        self._has_prev = False

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(trade_channel_strategy, self).OnReseted()
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._stop_price = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(trade_channel_strategy, self).OnStarted(time)
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, upper, lower, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_upper = upper
            self._prev_lower = lower
            self._has_prev = True
            return
        if atr_val <= 0:
            self._prev_upper = upper
            self._prev_lower = lower
            return
        close = candle.ClosePrice
        # Breakout above channel => long
        if close >= self._prev_upper and self.Position <= 0:
            if self.Position < 0) BuyMarket(:
                self.BuyMarket()
            self._stop_price = lower - atr_val
        # Breakout below channel => short
        elif close <= self._prev_lower and self.Position >= 0:
            if self.Position > 0) SellMarket(:
                self.SellMarket()
            self._stop_price = upper + atr_val
        # Manage long
        elif self.Position > 0:
            # Trailing stop
            new_stop = close - atr_val * 2
            if new_stop > self._stop_price:
                if candle.LowPrice <= self._stop_price:
                self.SellMarket()
                self._stop_price = 0
        # Manage short
        elif self.Position < 0:
            new_stop = close + atr_val * 2
            if new_stop < self._stop_price:
                if candle.HighPrice >= self._stop_price:
                self.BuyMarket()
                self._stop_price = 0
        self._prev_upper = upper
        self._prev_lower = lower

    def CreateClone(self):
        return trade_channel_strategy()
