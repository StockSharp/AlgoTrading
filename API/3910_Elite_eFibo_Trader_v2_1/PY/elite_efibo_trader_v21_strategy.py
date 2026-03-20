import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class elite_efibo_trader_v21_strategy(Strategy):
    def __init__(self):
        super(elite_efibo_trader_v21_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._cooldown_candles = self.Param("CooldownCandles", 200) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("EMA Period", "EMA lookback", "Indicators")

        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(elite_efibo_trader_v21_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False
        self._cooldown_remaining = 0.0

    def OnStarted(self, time):
        super(elite_efibo_trader_v21_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return elite_efibo_trader_v21_strategy()
