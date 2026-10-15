import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class bleris_strategy(Strategy):
    def __init__(self):
        super(bleris_strategy, self).__init__()
        self._signal_bar_sample = self.Param("SignalBarSample", 24) \
            .SetDisplay("Signal bar sample", "Signal bar sample", "General")
        self._counter_trend = self.Param("CounterTrend", False) \
            .SetDisplay("Counter trend", "Counter trend", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle type", "Candle type", "General")
        self._highs = []
        self._lows = []
        self._prev_high1 = 0.0
        self._prev_high2 = 0.0
        self._prev_low1 = 0.0
        self._prev_low2 = 0.0

    @property
    def signal_bar_sample(self):
        return self._signal_bar_sample.Value
    @property
    def counter_trend(self):
        return self._counter_trend.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bleris_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._prev_high1 = 0.0
        self._prev_high2 = 0.0
        self._prev_low1 = 0.0
        self._prev_low2 = 0.0

    def OnStarted2(self, time):
        super(bleris_strategy, self).OnStarted2(time)
        self.StartProtection(None, None)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        sbs = self.signal_bar_sample
        if len(self._highs) > sbs:
            self._highs.pop(0)
        if len(self._lows) > sbs:
            self._lows.pop(0)
        if len(self._highs) < sbs:
            return
        highest = max(self._highs)
        lowest = min(self._lows)
        uptrend = self._prev_low2 > 0 and self._prev_low2 < self._prev_low1 and self._prev_low1 < lowest
        downtrend = self._prev_high2 > 0 and self._prev_high2 > self._prev_high1 and self._prev_high1 > highest
        self._prev_high2 = self._prev_high1
        self._prev_high1 = highest
        self._prev_low2 = self._prev_low1
        self._prev_low1 = lowest
        if uptrend and not downtrend:
            if self.counter_trend:
                if self.Position >= 0:
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()
            else:
                if self.Position <= 0:
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
        elif downtrend and not uptrend:
            if self.counter_trend:
                if self.Position <= 0:
                    if self.Position < 0:
                        self.BuyMarket()
                    self.BuyMarket()
            else:
                if self.Position >= 0:
                    if self.Position > 0:
                        self.SellMarket()
                    self.SellMarket()

    def CreateClone(self):
        return bleris_strategy()
