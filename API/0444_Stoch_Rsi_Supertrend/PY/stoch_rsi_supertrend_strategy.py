import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex,
    Highest,
    Lowest,
    SimpleMovingAverage,
    ExponentialMovingAverage,
    AverageTrueRange,
)
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class stoch_rsi_supertrend_strategy(Strategy):
    """Stochastic RSI combined with SuperTrend and moving‑average trend filter.

    The strategy computes a Stochastic RSI oscillator and waits for %K to
    cross %D in oversold or overbought zones. A moving average defines the
    broader trend direction and a simplified SuperTrend (ATR bands) confirms
    momentum. Only when these elements align does the strategy open a
    position. Opposite crosses are used for exits."""

    def __init__(self):
        super(stoch_rsi_supertrend_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._smooth_k = self.Param("SmoothK", 3) \
            .SetDisplay("Smooth %K", "%K smoothing periods", "StochRSI")

        self._smooth_d = self.Param("SmoothD", 3) \
            .SetDisplay("Smooth %D", "%D smoothing periods", "StochRSI")

        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI calculation length", "StochRSI")

        self._stoch_length = self.Param("StochLength", 14) \
            .SetDisplay("Stoch Length", "Stochastic RSI period", "StochRSI")

        self._ma_type = self.Param("MaType", "EMA") \
            .SetDisplay("MA Type", "Trend moving average type", "Trend")

        self._ma_length = self.Param("MaLength", 100) \
            .SetDisplay("MA Length", "Trend moving average length", "Trend")

        self._atr_period = self.Param("AtrPeriod", 10) \
            .SetDisplay("ATR Period", "ATR period for Supertrend", "Supertrend")

        self._atr_factor = self.Param("AtrFactor", 3.0) \
            .SetDisplay("ATR Factor", "ATR factor for Supertrend", "Supertrend")

        self._show_short = self.Param("ShowShort", False) \
            .SetDisplay("Short Entries", "Enable short entries", "Strategy")

        self._rsi = None
        self._stoch_high = None
        self._stoch_low = None
        self._smooth_k_sma = None
        self._smooth_d_sma = None
        self._trend_ma = None
        self._atr = None

        self._prev_k = 0
        self._prev_d = 0
        self._prev_super = 0

        self._cross_over = False
        self._cross_under = False

    # region parameter properties
    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def smooth_k(self):
        return self._smooth_k.Value

    @property
    def smooth_d(self):
        return self._smooth_d.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def stoch_length(self):
        return self._stoch_length.Value

    @property
    def ma_type(self):
        return self._ma_type.Value

    @property
    def ma_length(self):
        return self._ma_length.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_factor(self):
        return self._atr_factor.Value

    @property
    def show_short(self):
        return self._show_short.Value

    # endregion

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super().OnReseted()
        self._prev_k = self._prev_d = self._prev_super = 0
        self._cross_over = self._cross_under = False

    def OnStarted(self, time):
        super().OnStarted(time)

        self._rsi = RelativeStrengthIndex(Length=self.rsi_length)
        self._stoch_high = Highest(Length=self.stoch_length)
        self._stoch_low = Lowest(Length=self.stoch_length)
        self._smooth_k_sma = SimpleMovingAverage(Length=self.smooth_k)
        self._smooth_d_sma = SimpleMovingAverage(Length=self.smooth_d)
        self._atr = AverageTrueRange(Length=self.atr_period)

        if self.ma_type.upper() == "SMA":
            self._trend_ma = SimpleMovingAverage(Length=self.ma_length)
        else:
            self._trend_ma = ExponentialMovingAverage(Length=self.ma_length)

        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(self._rsi, self._trend_ma, self._atr, self.ProcessCandle)
        sub.Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, self._trend_ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, ma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._rsi.IsFormed or not self._trend_ma.IsFormed or not self._atr.IsFormed:
            return

        rsi_price = rsi_value.ToDecimal()
        high_val = self._stoch_high.Process(rsi_price).ToDecimal()
        low_val = self._stoch_low.Process(rsi_price).ToDecimal()
        if not self._stoch_high.IsFormed or not self._stoch_low.IsFormed:
            return

        stoch_rsi = (rsi_price - low_val) / (high_val - low_val) * 100 if high_val != low_val else 50
        k_val = self._smooth_k_sma.Process(stoch_rsi).ToDecimal()
        d_val = self._smooth_d_sma.Process(k_val).ToDecimal()
        if not self._smooth_k_sma.IsFormed or not self._smooth_d_sma.IsFormed:
            return

        cur_price = candle.ClosePrice
        hl2 = (candle.HighPrice + candle.LowPrice) / 2
        atr = atr_value.ToDecimal()
        upper = hl2 + (self.atr_factor * atr)
        lower = hl2 - (self.atr_factor * atr)
        supertrend = lower if cur_price > self._prev_super else upper
        direction = -1 if cur_price > supertrend else 1

        if self._prev_k and self._prev_d:
            self._cross_over = self._prev_k <= self._prev_d and k_val > d_val
            self._cross_under = self._prev_k >= self._prev_d and k_val < d_val

        self.CheckEntry(candle, k_val, d_val, ma_value.ToDecimal(), direction)
        self.CheckExit(candle, k_val, d_val)

        self._prev_k = k_val
        self._prev_d = d_val
        self._prev_super = supertrend

    def CheckEntry(self, candle, k, d, ma, direction):
        price = candle.ClosePrice

        if price > ma and k < 20 and self._cross_over and direction < 0 and self.Position == 0:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))

        if self.show_short and price < ma and k > 80 and self._cross_under and direction > 0 and self.Position == 0:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

    def CheckExit(self, candle, k, d):
        price = candle.ClosePrice

        if self.Position > 0 and k > 80 and self._cross_under:
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, abs(self.Position)))

        if self.Position < 0 and k < 20 and self._cross_over:
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, abs(self.Position)))

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return stoch_rsi_supertrend_strategy()
