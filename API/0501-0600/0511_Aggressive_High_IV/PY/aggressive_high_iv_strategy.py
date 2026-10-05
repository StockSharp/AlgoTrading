import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

STOP_ATR_MULTIPLE = 2.0
TAKE_ATR_MULTIPLE = 4.0


class aggressive_high_iv_strategy(Strategy):
    """
    Aggressive High IV strategy.
    Trades fast/slow EMA crossovers only while ATR is above its mean plus one standard deviation. A bullish cross goes long and
    a bearish cross goes short, reversing an opposite position. Each position is closed by an ATR stop-loss or take-profit
    measured from the entry close.
    """

    def __init__(self):
        super(aggressive_high_iv_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast EMA Length", "Fast EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 30) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow EMA Length", "Slow EMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period", "Volatility")
        self._atr_mean_length = self.Param("AtrMeanLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Mean Length", "Period of the ATR mean", "Volatility")
        self._atr_std_length = self.Param("AtrStdLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Std Length", "Period of the ATR standard deviation", "Volatility")
        self._risk_factor = self.Param("RiskFactor", 0.01) \
            .SetNotNegative() \
            .SetDisplay("Risk Factor", "Fraction of equity risked per trade", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._atr_mean = None
        self._atr_std = None
        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(aggressive_high_iv_strategy, self).OnReseted()
        self._atr_mean = None
        self._atr_std = None
        self._reset_state()

    def OnStarted2(self, time):
        super(aggressive_high_iv_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        self._atr_mean = SimpleMovingAverage()
        self._atr_mean.Length = self._atr_mean_length.Value
        self._atr_std = StandardDeviation()
        self._atr_std.Length = self._atr_std_length.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)
        atr = float(atr_value)

        atr_mean = float(process_float(self._atr_mean, atr, candle.ServerTime, True))
        atr_std = float(process_float(self._atr_std, atr, candle.ServerTime, True))

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is None or prev_slow is None or not self._atr_mean.IsFormed or not self._atr_std.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._check_exits(candle):
            return

        high_volatility = atr > atr_mean + atr_std
        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow
        close = float(candle.ClosePrice)

        if high_volatility and cross_up and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - STOP_ATR_MULTIPLE * atr
            self._take_price = close + TAKE_ATR_MULTIPLE * atr
        elif high_volatility and cross_down and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + STOP_ATR_MULTIPLE * atr
            self._take_price = close - TAKE_ATR_MULTIPLE * atr

    def _check_exits(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0 and self._stop_price > 0 and (low <= self._stop_price or high >= self._take_price):
            self.SellMarket(self.Position)
            self._stop_price = 0.0
            self._take_price = 0.0
            return True

        if self.Position < 0 and self._stop_price > 0 and (high >= self._stop_price or low <= self._take_price):
            self.BuyMarket(-self.Position)
            self._stop_price = 0.0
            self._take_price = 0.0
            return True

        return False

    def CreateClone(self):
        return aggressive_high_iv_strategy()
