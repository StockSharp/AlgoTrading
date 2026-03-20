import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class nextbar_strategy(Strategy):
    def __init__(self):
        super(nextbar_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA filter", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 200) \
            .SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

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
        super(nextbar_strategy, self).OnReseted()
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(nextbar_strategy, self).OnStarted(time)
        self._prev_open = 0.0
        self._prev_close = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self.process_candle).Start()

    def process_candle(self, candle, ema):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        ema_val = float(ema)

        if not self._has_prev:
            self._prev_open = open_price
            self._prev_close = close
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_open = open_price
            self._prev_close = close
            return

        prev_bullish = self._prev_close > self._prev_open
        prev_bearish = self._prev_close < self._prev_open

        if prev_bullish and close > ema_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif prev_bearish and close < ema_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles

        self._prev_open = open_price
        self._prev_close = close

    def CreateClone(self):
        return nextbar_strategy()
