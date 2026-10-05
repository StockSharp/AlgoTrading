import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage, SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class stoch_rsi_crossover_strategy(Strategy):
    """
    Stoch RSI Crossover Strategy.
    RSI is turned into a Stochastic RSI over StochLength bars and smoothed into %K (SmoothK) and %D (SmoothD).
    A long opens when %K crosses above %D with %K in [10, 60], EMA1 > EMA2 > EMA3 and the close above EMA1.
    A short opens when %K crosses below %D with %K in [40, 95], EMA1 < EMA2 < EMA3 and the close below EMA1.
    There is no built-in exit: an opposite signal reverses the position. The ATR multipliers only describe suggested levels.
    """

    def __init__(self):
        super(stoch_rsi_crossover_strategy, self).__init__()
        self._smooth_k = self.Param("SmoothK", 3).SetGreaterThanZero().SetDisplay("Smooth K", "%K smoothing", "Stoch RSI")
        self._smooth_d = self.Param("SmoothD", 3).SetGreaterThanZero().SetDisplay("Smooth D", "%D smoothing", "Stoch RSI")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Stoch RSI")
        self._stoch_length = self.Param("StochLength", 14).SetGreaterThanZero().SetDisplay("Stoch Length", "Stochastic period applied to RSI", "Stoch RSI")
        self._ema1_length = self.Param("Ema1Length", 20).SetGreaterThanZero().SetDisplay("EMA1 Length", "Fast EMA period", "Trend")
        self._ema2_length = self.Param("Ema2Length", 50).SetGreaterThanZero().SetDisplay("EMA2 Length", "Medium EMA period", "Trend")
        self._ema3_length = self.Param("Ema3Length", 100).SetGreaterThanZero().SetDisplay("EMA3 Length", "Slow EMA period", "Trend")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period of the suggested levels", "Risk")
        self._atr_loss_multiplier = self.Param("AtrLossMultiplier", 1.5).SetGreaterThanZero().SetDisplay("ATR Loss Multiplier", "ATR multiplier of the suggested stop loss", "Risk")
        self._atr_profit_multiplier = self.Param("AtrProfitMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Profit Multiplier", "ATR multiplier of the suggested profit target", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_high = None
        self._rsi_low = None
        self._k_ma = None
        self._d_ma = None
        self._prev_k = None
        self._prev_d = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stoch_rsi_crossover_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(stoch_rsi_crossover_strategy, self).OnStarted2(time)

        self._prev_k = None
        self._prev_d = None

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        ema1 = ExponentialMovingAverage()
        ema1.Length = self._ema1_length.Value
        ema2 = ExponentialMovingAverage()
        ema2.Length = self._ema2_length.Value
        ema3 = ExponentialMovingAverage()
        ema3.Length = self._ema3_length.Value

        self._rsi_high = Highest()
        self._rsi_high.Length = self._stoch_length.Value
        self._rsi_low = Lowest()
        self._rsi_low.Length = self._stoch_length.Value
        self._k_ma = SimpleMovingAverage()
        self._k_ma.Length = self._smooth_k.Value
        self._d_ma = SimpleMovingAverage()
        self._d_ma.Length = self._smooth_d.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, ema1, ema2, ema3, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema1)
            self.DrawIndicator(area, ema2)
            self.DrawIndicator(area, ema3)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, rsi_value, ema1_value, ema2_value, ema3_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed:
            return

        rsi_dec = rsi_value.GetValue[Decimal](None)
        rsi = float(rsi_dec)
        high = float(process_float(self._rsi_high, rsi_dec, candle.OpenTime, True).GetValue[Decimal](None))
        low = float(process_float(self._rsi_low, rsi_dec, candle.OpenTime, True).GetValue[Decimal](None))

        if not self._rsi_high.IsFormed or not self._rsi_low.IsFormed:
            return

        stoch = 100.0 * (rsi - low) / (high - low) if high - low > 0 else 0.0
        k_value = process_float(self._k_ma, stoch, candle.OpenTime, True)
        if not self._k_ma.IsFormed:
            return

        k_dec = k_value.GetValue[Decimal](None)
        k = float(k_dec)
        d_value = process_float(self._d_ma, k_dec, candle.OpenTime, True)
        if not self._d_ma.IsFormed:
            return

        d = float(d_value.GetValue[Decimal](None))

        prev_k = self._prev_k
        prev_d = self._prev_d
        self._prev_k = k
        self._prev_d = d

        if prev_k is None or prev_d is None:
            return

        if not ema1_value.IsFormed or not ema2_value.IsFormed or not ema3_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema1 = float(ema1_value.GetValue[Decimal](None))
        ema2 = float(ema2_value.GetValue[Decimal](None))
        ema3 = float(ema3_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        cross_up = prev_k <= prev_d and k > d
        cross_down = prev_k >= prev_d and k < d

        long_signal = cross_up and 10.0 <= k <= 60.0 and ema1 > ema2 and ema2 > ema3 and close > ema1
        short_signal = cross_down and 40.0 <= k <= 95.0 and ema1 < ema2 and ema2 < ema3 and close < ema1

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return stoch_rsi_crossover_strategy()
