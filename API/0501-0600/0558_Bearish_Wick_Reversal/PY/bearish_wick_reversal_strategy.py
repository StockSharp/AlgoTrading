import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bearish_wick_reversal_strategy(Strategy):
    def __init__(self):
        super(bearish_wick_reversal_strategy, self).__init__()
        self._threshold = self.Param("Threshold", -1.5) \
            .SetDisplay("Long Threshold", "Percentage threshold for lower wick", "Strategy Settings")
        self._ema_period = self.Param("EmaPeriod", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Period", "Period for EMA trend filter", "Trend Filter")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(bearish_wick_reversal_strategy, self).OnReseted()
        self._previous_high = 0.0
        self._previous_low = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bearish_wick_reversal_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        if self._cooldown > 0:
            self._cooldown -= 1
        threshold = float(self._threshold.Value)
        long_cond = False
        short_cond = False
        if close < open_p and close != 0:
            pct = 100.0 * (low - close) / close
            long_cond = pct <= threshold
        if close > open_p and close != 0:
            pct = 100.0 * (high - close) / close
            short_cond = pct >= -threshold
        if long_cond and self.Position <= 0 and self._cooldown == 0:
            self.BuyMarket()
            self._cooldown = 60
        elif short_cond and self.Position >= 0 and self._cooldown == 0:
            self.SellMarket()
            self._cooldown = 60
        if self.Position > 0 and self._previous_high > 0 and close > self._previous_high:
            self.SellMarket()
            self._cooldown = 60
        elif self.Position < 0 and self._previous_low > 0 and close < self._previous_low:
            self.BuyMarket()
            self._cooldown = 60
        self._previous_high = high
        self._previous_low = low

    def CreateClone(self):
        return bearish_wick_reversal_strategy()
