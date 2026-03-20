import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class panel_joke_strategy(Strategy):
    def __init__(self):
        super(panel_joke_strategy, self).__init__()
        self._enable_autopilot = self.Param("EnableAutopilot", True) \
            .SetDisplay("Enable Autopilot", "Automatically trade based on signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for incoming candles", "General")
        self._prev_open = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def enable_autopilot(self):
        return self._enable_autopilot.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(panel_joke_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(panel_joke_strategy, self).OnStarted(time)
        warmup = ExponentialMovingAverage()
        warmup.Length = 5
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _warmup_val):
        if candle.State != CandleStates.Finished:
            return
        o = float(candle.OpenPrice)
        h = float(candle.HighPrice)
        l = float(candle.LowPrice)
        c = float(candle.ClosePrice)
        if not self._has_prev:
            self._prev_open = o
            self._prev_high = h
            self._prev_low = l
            self._prev_close = c
            self._has_prev = True
            return
        buy = 0
        sell = 0
        if o > self._prev_open:
            buy += 1
        else:
            sell += 1
        if h > self._prev_high:
            buy += 1
        else:
            sell += 1
        if l > self._prev_low:
            buy += 1
        else:
            sell += 1
        avg_hl = (h + l) / 2.0
        prev_avg_hl = (self._prev_high + self._prev_low) / 2.0
        if avg_hl > prev_avg_hl:
            buy += 1
        else:
            sell += 1
        if c > self._prev_close:
            buy += 1
        else:
            sell += 1
        avg_hlc = (h + l + c) / 3.0
        prev_avg_hlc = (self._prev_high + self._prev_low + self._prev_close) / 3.0
        if avg_hlc > prev_avg_hlc:
            buy += 1
        else:
            sell += 1
        avg_hlcc = (h + l + 2.0 * c) / 4.0
        prev_avg_hlcc = (self._prev_high + self._prev_low + 2.0 * self._prev_close) / 4.0
        if avg_hlcc > prev_avg_hlcc:
            buy += 1
        else:
            sell += 1
        if buy > sell and self.Position <= 0:
            self.BuyMarket()
        elif sell > buy and self.Position >= 0:
            self.SellMarket()
        self._prev_open = o
        self._prev_high = h
        self._prev_low = l
        self._prev_close = c

    def CreateClone(self):
        return panel_joke_strategy()
