import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cross_strategy(Strategy):
    def __init__(self):
        super(cross_strategy, self).__init__()

        self._ema_length = self.Param("EmaLength", 100) \
            .SetDisplay("EMA Length", "EMA period for cross detection", "Indicators")
        self._signal_cooldown = self.Param("SignalCooldownCandles", 3) \
            .SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading")

        self._ema = None
        self._prev_above = False
        self._has_prev = False
        self._candles_since_trade = 0

    @property
    def ema_length(self):
        return self._ema_length.Value

    @property
    def signal_cooldown(self):
        return self._signal_cooldown.Value

    def OnReseted(self):
        super(cross_strategy, self).OnReseted()
        self._ema = None
        self._prev_above = False
        self._has_prev = False
        self._candles_since_trade = self.signal_cooldown

    def OnStarted(self, time):
        super(cross_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_length
        self._has_prev = False
        self._candles_since_trade = self.signal_cooldown

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        subscription.Bind(self._ema, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ema.IsFormed:
            return

        if self._candles_since_trade < self.signal_cooldown:
            self._candles_since_trade += 1

        ema_val = float(ema_value)
        above = float(candle.OpenPrice) > ema_val and float(candle.ClosePrice) > ema_val

        if self._has_prev and self._candles_since_trade >= self.signal_cooldown:
            if above and not self._prev_above and self.Position <= 0:
                self.BuyMarket()
                self._candles_since_trade = 0
            elif not above and self._prev_above and self.Position >= 0:
                self.SellMarket()
                self._candles_since_trade = 0

        self._prev_above = above
        self._has_prev = True

    def CreateClone(self):
        return cross_strategy()
