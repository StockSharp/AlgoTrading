import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, StochasticK, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class three_signal_directional_trend_strategy(Strategy):
    """
    Three Signal Directional Trend Strategy.
    Three indicators vote: the MACD signal line rising or falling, the Stochastic %K (StochLength, smoothed over SmoothK) below
    Oversold or above Overbought, and the rate of change over RocLength bars of the AvgLength SMA, averaged over AvgRocLength
    bars, above or below zero. At least two long votes open a long and at least two short votes open a short, reversing
    an opposite position.
    """

    def __init__(self):
        super(three_signal_directional_trend_strategy, self).__init__()
        self._avg_length = self.Param("AvgLength", 50).SetGreaterThanZero().SetDisplay("Average Length", "SMA period of the rate of change", "ROC")
        self._roc_length = self.Param("RocLength", 1).SetGreaterThanZero().SetDisplay("ROC Length", "Bars of the rate of change", "ROC")
        self._avg_roc_length = self.Param("AvgRocLength", 10).SetGreaterThanZero().SetDisplay("Average ROC Length", "Averaging period of the rate of change", "ROC")
        self._stoch_length = self.Param("StochLength", 14).SetGreaterThanZero().SetDisplay("Stochastic Length", "Stochastic lookback", "Stochastic")
        self._smooth_k = self.Param("SmoothK", 3).SetGreaterThanZero().SetDisplay("Smooth K", "Smoothing of %K", "Stochastic")
        self._overbought = self.Param("Overbought", 80.0).SetDisplay("Overbought", "Stochastic overbought level", "Stochastic")
        self._oversold = self.Param("Oversold", 20.0).SetDisplay("Oversold", "Stochastic oversold level", "Stochastic")
        self._macd_fast_length = self.Param("MacdFastLength", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
        self._macd_slow_length = self.Param("MacdSlowLength", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
        self._macd_avg_length = self.Param("MacdAvgLength", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal line period", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._k_smooth = None
        self._roc_average = None
        self._ma_history = []
        self._prev_signal = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(three_signal_directional_trend_strategy, self).OnReseted()
        self._ma_history = []
        self._prev_signal = None

    def OnStarted2(self, time):
        super(three_signal_directional_trend_strategy, self).OnStarted2(time)

        self._ma_history = []
        self._prev_signal = None

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast_length.Value
        macd.Macd.LongMa.Length = self._macd_slow_length.Value
        macd.SignalMa.Length = self._macd_avg_length.Value
        stoch_k = StochasticK()
        stoch_k.Length = self._stoch_length.Value
        sma = SimpleMovingAverage()
        sma.Length = self._avg_length.Value

        self._k_smooth = SimpleMovingAverage()
        self._k_smooth.Length = self._smooth_k.Value
        self._roc_average = SimpleMovingAverage()
        self._roc_average.Length = self._avg_roc_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, stoch_k, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, macd_value, stoch_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        time = candle.OpenTime

        k = None
        if stoch_value.IsFormed:
            smoothed = process_float(self._k_smooth, stoch_value.GetValue[Decimal](None), time, True)
            if self._k_smooth.IsFormed:
                k = float(smoothed.GetValue[Decimal](None))

        avg_roc = None
        if sma_value.IsFormed:
            ma = float(sma_value.GetValue[Decimal](None))
            roc_length = self._roc_length.Value
            self._ma_history.append(ma)
            if len(self._ma_history) > roc_length + 1:
                self._ma_history.pop(0)

            if len(self._ma_history) == roc_length + 1 and self._ma_history[0] != 0:
                roc = (ma - self._ma_history[0]) / self._ma_history[0] * 100.0
                averaged = process_float(self._roc_average, roc, time, True)
                if self._roc_average.IsFormed:
                    avg_roc = float(averaged.GetValue[Decimal](None))

        signal = None
        if macd_value.IsFormed and macd_value.Signal is not None:
            signal = float(macd_value.Signal)

        previous = self._prev_signal
        if signal is not None:
            self._prev_signal = signal

        if k is None or avg_roc is None or signal is None or previous is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        oversold = float(self._oversold.Value)
        overbought = float(self._overbought.Value)

        long_votes = (1 if signal > previous else 0) + (1 if k < oversold else 0) + (1 if avg_roc > 0 else 0)
        short_votes = (1 if signal < previous else 0) + (1 if k > overbought else 0) + (1 if avg_roc < 0 else 0)

        if long_votes >= 2 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_votes >= 2 and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return three_signal_directional_trend_strategy()
