import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class fibo_pivot_multi_val_strategy(Strategy):
    def __init__(self):
        super(fibo_pivot_multi_val_strategy, self).__init__()
        self._channel_period = self.Param("ChannelPeriod", 48) \
            .SetDisplay("Channel Period", "Fibo channel lookback", "Indicators")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 150) \
            .SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def channel_period(self):
        return self._channel_period.Value
    @property
    def ema_period(self):
        return self._ema_period.Value
    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value
    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibo_pivot_multi_val_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(fibo_pivot_multi_val_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_mid = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0
        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, ema, self.process_candle).Start()

    def process_candle(self, candle, highest, lowest, ema):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        mid = (float(highest) + float(lowest)) / 2.0
        ema_val = float(ema)
        if not self._has_prev:
            self._prev_close = close
            self._prev_mid = mid
            self._has_prev = True
            return
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_mid = mid
            return
        if self._prev_close <= self._prev_mid and close > mid and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif self._prev_close >= self._prev_mid and close < mid and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles
        self._prev_close = close
        self._prev_mid = mid

    def CreateClone(self):
        return fibo_pivot_multi_val_strategy()
