import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy

class fxf_safe_trend_scalp_v1_strategy(Strategy):
    """
    FXF Safe Trend Scalp V1: SMA crossover with ATR volatility filter.
    Enters long when fast SMA crosses above slow SMA and close confirms.
    Enters short on reverse cross with ATR confirmation.
    """

    def __init__(self):
        super(fxf_safe_trend_scalp_v1_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 5) \
            .SetDisplay("Fast Period", "Fast SMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 20) \
            .SetDisplay("Slow Period", "Slow SMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._signal_cooldown_candles = self.Param("SignalCooldownCandles", 3) \
            .SetDisplay("Signal Cooldown", "Bars to wait between entries", "Trading")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._candles_since_trade = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fxf_safe_trend_scalp_v1_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._has_prev = False
        self._candles_since_trade = self._signal_cooldown_candles.Value

    def OnStarted2(self, time):
        super(fxf_safe_trend_scalp_v1_strategy, self).OnStarted2(time)

        self._has_prev = False
        self._candles_since_trade = self._signal_cooldown_candles.Value

        fast = SimpleMovingAverage()
        fast.Length = self._fast_period.Value
        slow = SimpleMovingAverage()
        slow.Length = self._slow_period.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast, slow, atr, self._process_candle).Start()

    def _process_candle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_val)
        slow = float(slow_val)
        atr = float(atr_val)

        if self._candles_since_trade < self._signal_cooldown_candles.Value:
            self._candles_since_trade += 1

        if self._has_prev:
            close = float(candle.ClosePrice)
            long_signal = self._prev_fast <= self._prev_slow and fast > slow and close > slow + atr * 0.25
            short_signal = self._prev_fast >= self._prev_slow and fast < slow and close < slow - atr * 0.25

            if self._candles_since_trade >= self._signal_cooldown_candles.Value:
                if long_signal and self.Position <= 0:
                    self.BuyMarket()
                    self._candles_since_trade = 0
                elif short_signal and self.Position >= 0:
                    self.SellMarket()
                    self._candles_since_trade = 0

        self._prev_fast = fast
        self._prev_slow = slow
        self._has_prev = True

    def CreateClone(self):
        return fxf_safe_trend_scalp_v1_strategy()
