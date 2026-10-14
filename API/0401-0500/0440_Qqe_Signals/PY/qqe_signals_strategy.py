import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class qqe_signals_strategy(Strategy):
    """
    QQE Signals Strategy.
    RSI is smoothed by an EMA of RsiSmoothing bars. The bar-to-bar change of that line is double smoothed over
    2 * RsiPeriod - 1 bars and multiplied by QqeFactor to build long and short trailing bands. The trailing line follows
    the long band in an up trend and the short band in a down trend. When the smoothed RSI moves above the trailing
    line a long is opened; when it falls below it the long is closed. Threshold marks the 50 +/- Threshold zone on the chart.
    """

    def __init__(self):
        super(qqe_signals_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period", "QQE")
        self._rsi_smoothing = self.Param("RsiSmoothing", 5).SetGreaterThanZero().SetDisplay("RSI Smoothing", "EMA smoothing of RSI", "QQE")
        self._qqe_factor = self.Param("QqeFactor", 4.238).SetGreaterThanZero().SetDisplay("QQE Factor", "Multiplier of the smoothed RSI volatility", "QQE")
        self._threshold = self.Param("Threshold", 10.0).SetNotNegative().SetDisplay("Threshold", "Distance of the reference zone from the 50 level", "QQE")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_ma = None
        self._atr_rsi_ma = None
        self._dar_ma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_rsi_ma = None
        self._long_band = None
        self._short_band = None
        self._trend = 1
        self._above_count = 0
        self._below_count = 0

    def OnReseted(self):
        super(qqe_signals_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(qqe_signals_strategy, self).OnStarted2(time)

        self._reset_state()

        wilders_period = self._rsi_period.Value * 2 - 1
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        self._rsi_ma = ExponentialMovingAverage()
        self._rsi_ma.Length = self._rsi_smoothing.Value
        self._atr_rsi_ma = ExponentialMovingAverage()
        self._atr_rsi_ma.Length = wilders_period
        self._dar_ma = ExponentialMovingAverage()
        self._dar_ma.Length = wilders_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_ma_result = process_float(self._rsi_ma, rsi_value, candle.OpenTime, True)
        if not self._rsi_ma.IsFormed:
            return

        rsi_ma = float(rsi_ma_result.GetValue[Decimal](None))
        previous = self._prev_rsi_ma
        self._prev_rsi_ma = rsi_ma

        if previous is None:
            return

        atr_rsi = abs(previous - rsi_ma)
        ma_atr_rsi = process_float(self._atr_rsi_ma, atr_rsi, candle.OpenTime, True)
        if not self._atr_rsi_ma.IsFormed:
            return

        dar = process_float(self._dar_ma, ma_atr_rsi.GetValue[Decimal](None), candle.OpenTime, True)
        if not self._dar_ma.IsFormed:
            return

        delta = float(dar.GetValue[Decimal](None)) * float(self._qqe_factor.Value)
        new_long_band = rsi_ma - delta
        new_short_band = rsi_ma + delta

        prev_long_band = self._long_band
        prev_short_band = self._short_band

        if prev_long_band is not None and previous > prev_long_band and rsi_ma > prev_long_band:
            long_band = max(prev_long_band, new_long_band)
        else:
            long_band = new_long_band

        if prev_short_band is not None and previous < prev_short_band and rsi_ma < prev_short_band:
            short_band = min(prev_short_band, new_short_band)
        else:
            short_band = new_short_band

        if prev_short_band is not None and previous <= prev_short_band and rsi_ma > prev_short_band:
            self._trend = 1
        elif prev_long_band is not None and previous >= prev_long_band and rsi_ma < prev_long_band:
            self._trend = -1

        self._long_band = long_band
        self._short_band = short_band

        trailing_line = long_band if self._trend == 1 else short_band

        self._above_count = self._above_count + 1 if trailing_line < rsi_ma else 0
        self._below_count = self._below_count + 1 if trailing_line > rsi_ma else 0

        if prev_long_band is None or prev_short_band is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._above_count == 1 and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self._below_count == 1 and self.Position > 0:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return qqe_signals_strategy()
