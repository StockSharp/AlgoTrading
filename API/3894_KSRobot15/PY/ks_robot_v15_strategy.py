import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ks_robot_v15_strategy(Strategy):
    def __init__(self):
        super(ks_robot_v15_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 50) \
            .SetDisplay("SMA Period", "SMA filter", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 200) \
            .SetDisplay("Cooldown", "Candles between signals", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def cooldown_candles(self):
        return self._cooldown_candles.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ks_robot_v15_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(ks_robot_v15_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0

        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, sma, self.process_candle).Start()

    def process_candle(self, candle, ema, sma):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ema_val = float(ema)
        sma_val = float(sma)

        if not self._has_prev:
            self._prev_close = close
            self._prev_ema = ema_val
            self._has_prev = True
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            self._prev_close = close
            self._prev_ema = ema_val
            return

        if self._prev_close <= self._prev_ema and close > ema_val and close > sma_val and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_candles
        elif self._prev_close >= self._prev_ema and close < ema_val and close < sma_val and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_candles

        self._prev_close = close
        self._prev_ema = ema_val

    def CreateClone(self):
        return ks_robot_v15_strategy()
