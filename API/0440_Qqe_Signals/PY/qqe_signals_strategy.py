import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *


class qqe_signals_strategy(Strategy):
    """QQE Signals strategy based on smoothed RSI bands.

    Implements the Quantitative Qualitative Estimation technique to build
    dynamic bands around RSI. Trades when RSI crosses the bands, signalling
    potential trend shifts.
    """

    def __init__(self):
        super(qqe_signals_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", tf(1)).SetDisplay(
            "Candle type", "Candle type for strategy calculation.", "General"
        )
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay(
            "RSI Length", "RSI period", "QQE"
        )
        self._rsi_smoothing = self.Param("RsiSmoothing", 5).SetDisplay(
            "RSI Smoothing", "RSI smoothing period", "QQE"
        )
        self._qqe_factor = self.Param("QqeFactor", 4.238).SetDisplay(
            "Fast QQE Factor", "QQE factor", "QQE"
        )
        self._threshold = self.Param("Threshold", 10.0).SetDisplay(
            "Threshold", "Threshold value", "QQE"
        )

        self._rsi = None
        self._rsi_ma = None
        self._atr_rsi = None
        self._ma_atr_rsi = None
        self._dar = None

        self._longband = 0.0
        self._shortband = 0.0
        self._trend = 0
        self._qqe_xlong = 0
        self._qqe_xshort = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(qqe_signals_strategy, self).OnReseted()
        self._longband = 0.0
        self._shortband = 0.0
        self._trend = 0
        self._qqe_xlong = 0
        self._qqe_xshort = 0

    def OnStarted(self, time):
        super(qqe_signals_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_period.Value
        self._rsi_ma = ExponentialMovingAverage()
        self._rsi_ma.Length = self._rsi_smoothing.Value

        wilders = self._rsi_period.Value * 2 - 1
        self._atr_rsi = ExponentialMovingAverage()
        self._atr_rsi.Length = 1
        self._ma_atr_rsi = ExponentialMovingAverage()
        self._ma_atr_rsi.Length = wilders
        self._dar = ExponentialMovingAverage()
        self._dar.Length = wilders

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi_val = self._rsi.Process(candle)
        if not self._rsi.IsFormed:
            return
        rsi_ma_val = self._rsi_ma.Process(rsi_val)
        if not self._rsi_ma.IsFormed:
            return
        rs_index = rsi_ma_val.GetValue[float]()

        prev_rsi_ma = self._rsi_ma.GetValue(1)
        atr_rsi_val = abs(prev_rsi_ma - rs_index)
        ma_atr_rsi_val = self._ma_atr_rsi.Process(atr_rsi_val, candle.ServerTime, candle.State == CandleStates.Finished)
        if not self._ma_atr_rsi.IsFormed:
            return
        dar_val = self._dar.Process(ma_atr_rsi_val)
        if not self._dar.IsFormed:
            return
        delta_fast = dar_val.GetValue[float]() * self._qqe_factor.Value

        new_shortband = rs_index + delta_fast
        new_longband = rs_index - delta_fast
        prev_longband = self._longband
        prev_shortband = self._shortband
        prev_rs_index = self._rsi_ma.GetValue(1)

        if prev_rs_index > prev_longband and rs_index > prev_longband:
            self._longband = max(prev_longband, new_longband)
        else:
            self._longband = new_longband
        if prev_rs_index < prev_shortband and rs_index < prev_shortband:
            self._shortband = min(prev_shortband, new_shortband)
        else:
            self._shortband = new_shortband

        if rs_index > self._shortband and prev_rs_index <= prev_shortband:
            self._trend = 1
        elif rs_index < self._longband and prev_rs_index >= prev_longband:
            self._trend = -1

        fast_atr_rsi_tl = self._longband if self._trend == 1 else self._shortband

        if fast_atr_rsi_tl < rs_index:
            self._qqe_xlong += 1
            self._qqe_xshort = 0
        else:
            self._qqe_xshort += 1
            self._qqe_xlong = 0

        qqe_long = self._qqe_xlong == 1
        qqe_short = self._qqe_xshort == 1

        if qqe_long and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif qqe_short and self.Position > 0:
            self.ClosePosition()

    def CreateClone(self):
        return qqe_signals_strategy()
