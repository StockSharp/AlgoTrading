import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class basic_atr_stop_take_strategy(Strategy):
    def __init__(self):
        super(basic_atr_stop_take_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(15) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 50) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._stop_factor = self.Param("StopFactor", 1.5) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._take_factor = self.Param("TakeFactor", 2.0) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 6) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_above_ema = False
        self._has_prev_signal = False
        self._candles_since_trade = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(basic_atr_stop_take_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = 0.0
        self._take_price = 0.0
        self._prev_above_ema = False
        self._has_prev_signal = False
        self._candles_since_trade = 0.0

    def OnStarted(self, time):
        super(basic_atr_stop_take_strategy, self).OnStarted(time)

        self._ema = ExponentialMovingAverage()
        self._ema.Length = self.ema_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._atr, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return basic_atr_stop_take_strategy()
