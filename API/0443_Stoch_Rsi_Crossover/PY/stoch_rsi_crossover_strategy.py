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


class stoch_rsi_crossover_strategy(Strategy):
    """Stochastic RSI crossover strategy with triple EMA trend filter.

    The system calculates a standard RSI, transforms it into a Stochastic RSI,
    and then applies smoothing to derive %K and %D lines. Trading signals are
    generated when %K crosses %D within predefined zones while a triple EMA
    structure confirms trend direction. ATR‑based multipliers outline
    theoretical stop‑loss and profit targets.
    """

    def __init__(self):
        super(stoch_rsi_crossover_strategy, self).__init__()

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

        self._ema1_length = self.Param("Ema1Length", 20) \
            .SetDisplay("EMA1 Length", "Fast EMA period", "Moving Averages")

        self._ema2_length = self.Param("Ema2Length", 50) \
            .SetDisplay("EMA2 Length", "Middle EMA period", "Moving Averages")

        self._ema3_length = self.Param("Ema3Length", 100) \
            .SetDisplay("EMA3 Length", "Slow EMA period", "Moving Averages")

        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR period for risk levels", "ATR")

        self._atr_loss_multiplier = self.Param("AtrLossMultiplier", 1.5) \
            .SetDisplay("ATR Loss Mult", "ATR multiplier for stop loss", "ATR")

        self._atr_profit_multiplier = self.Param("AtrProfitMultiplier", 2.0) \
            .SetDisplay("ATR Profit Mult", "ATR multiplier for take profit", "ATR")

        self._rsi = None
        self._high = None
        self._low = None
        self._smooth_k_sma = None
        self._smooth_d_sma = None
        self._ema1 = None
        self._ema2 = None
        self._ema3 = None
        self._atr = None

        self._prev_k = 0
        self._prev_d = 0

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
    def ema1_length(self):
        return self._ema1_length.Value

    @property
    def ema2_length(self):
        return self._ema2_length.Value

    @property
    def ema3_length(self):
        return self._ema3_length.Value

    @property
    def atr_length(self):
        return self._atr_length.Value

    @property
    def atr_loss_multiplier(self):
        return self._atr_loss_multiplier.Value

    @property
    def atr_profit_multiplier(self):
        return self._atr_profit_multiplier.Value
    # endregion

    def OnReseted(self):
        super(stoch_rsi_crossover_strategy, self).OnReseted()
        self._prev_k = 0
        self._prev_d = 0

    def OnStarted(self, time):
        super(stoch_rsi_crossover_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_length

        self._high = Highest()
        self._high.Length = self.stoch_length

        self._low = Lowest()
        self._low.Length = self.stoch_length

        self._smooth_k_sma = SimpleMovingAverage()
        self._smooth_k_sma.Length = self.smooth_k

        self._smooth_d_sma = SimpleMovingAverage()
        self._smooth_d_sma.Length = self.smooth_d

        self._ema1 = ExponentialMovingAverage(); self._ema1.Length = self.ema1_length
        self._ema2 = ExponentialMovingAverage(); self._ema2.Length = self.ema2_length
        self._ema3 = ExponentialMovingAverage(); self._ema3.Length = self.ema3_length

        self._atr = AverageTrueRange(); self._atr.Length = self.atr_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._rsi, self._ema1, self._ema2, self._ema3, self._atr, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ema1)
            self.DrawIndicator(area, self._ema2)
            self.DrawIndicator(area, self._ema3)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, rsi_value, ema1_value, ema2_value, ema3_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not (self._rsi.IsFormed and self._ema1.IsFormed and self._ema2.IsFormed and self._ema3.IsFormed and self._atr.IsFormed):
            return

        rsi_price = rsi_value
        highest = self._high.Process(rsi_price, candle.ServerTime, True)
        lowest = self._low.Process(rsi_price, candle.ServerTime, True)
        if not (highest.IsFormed and lowest.IsFormed):
            return

        high_val = highest.ToDecimal()
        low_val = lowest.ToDecimal()
        stoch_rsi = 50 if high_val == low_val else (rsi_price - low_val) / (high_val - low_val) * 100

        k_val = self._smooth_k_sma.Process(stoch_rsi, candle.ServerTime, True)
        d_val = self._smooth_d_sma.Process(k_val.ToDecimal(), candle.ServerTime, True)
        if not (k_val.IsFormed and d_val.IsFormed):
            return

        k = k_val.ToDecimal()
        d = d_val.ToDecimal()

        crossed_over = self._prev_k <= self._prev_d and k > d
        crossed_under = self._prev_k >= self._prev_d and k < d

        price = candle.ClosePrice

        if (crossed_over and 10 <= k <= 60 and
                ema1_value > ema2_value > ema3_value and
                price > ema1_value and self.Position == 0):
            stop_loss = price - atr_value * self.atr_loss_multiplier
            take_profit = price + atr_value * self.atr_profit_multiplier
            self.RegisterOrder(self.CreateOrder(Sides.Buy, price, self.Volume))

        if (crossed_under and 40 <= k <= 95 and
                ema3_value > ema2_value > ema1_value and
                price < ema1_value and self.Position == 0):
            stop_loss = price + atr_value * self.atr_loss_multiplier
            take_profit = price - atr_value * self.atr_profit_multiplier
            self.RegisterOrder(self.CreateOrder(Sides.Sell, price, self.Volume))

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return stoch_rsi_crossover_strategy()
