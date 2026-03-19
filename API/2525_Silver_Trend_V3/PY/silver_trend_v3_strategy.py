import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class silver_trend_v3_strategy(Strategy):
    """SilverTrend V3: custom momentum indicator with trailing stop and SL/TP."""
    def __init__(self):
        super(silver_trend_v3_strategy, self).__init__()
        self._count_bars = self.Param("CountBars", 150).SetGreaterThanZero().SetDisplay("Count Bars", "Candles before trading", "Indicator")
        self._ssp = self.Param("Ssp", 9).SetGreaterThanZero().SetDisplay("SSP", "Sliding window length", "Indicator")
        self._risk = self.Param("Risk", 3).SetGreaterThanZero().SetDisplay("Risk", "Risk coefficient", "Trading")
        self._trailing_points = self.Param("TrailingStopPoints", 50.0).SetNotNegative().SetDisplay("Trailing Stop", "Trailing distance", "Risk")
        self._tp_points = self.Param("TakeProfitPoints", 50.0).SetNotNegative().SetDisplay("Take Profit", "TP distance", "Risk")
        self._sl_points = self.Param("InitialStopLossPoints", 0.0).SetNotNegative().SetDisplay("Initial Stop Loss", "SL distance", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(30).TimeFrame()).SetDisplay("Candle Type", "Candle type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(silver_trend_v3_strategy, self).OnReseted()
        self._close_hist = []
        self._high_hist = []
        self._low_hist = []
        self._entry_price = 0
        self._trailing_stop = None
        self._prev_signal = 0

    def OnStarted(self, time):
        super(silver_trend_v3_strategy, self).OnStarted(time)
        self._close_hist = []
        self._high_hist = []
        self._low_hist = []
        self._entry_price = 0
        self._trailing_stop = None
        self._prev_signal = 0
        self._step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and self.Security.PriceStep > 0:
            self._step = float(self.Security.PriceStep)

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._close_hist.append(float(candle.ClosePrice))
        self._high_hist.append(float(candle.HighPrice))
        self._low_hist.append(float(candle.LowPrice))
        if len(self._close_hist) > 220:
            self._close_hist.pop(0)
            self._high_hist.pop(0)
            self._low_hist.pop(0)

        ssp = self._ssp.Value
        cb = self._count_bars.Value
        if len(self._close_hist) < cb + ssp + 1:
            return

        signal = self._calc_signal()
        close = float(candle.ClosePrice)

        # Manage existing position
        self._manage_position(candle, close)

        # Entry
        long_signal = self._prev_signal != signal and signal > 0
        short_signal = self._prev_signal != signal and signal < 0

        if self.Position <= 0 and long_signal:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._trailing_stop = None
        elif self.Position >= 0 and short_signal:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._trailing_stop = None

        self._prev_signal = signal

    def _manage_position(self, candle, close):
        step = self._step
        if self.Position > 0 and self._entry_price > 0:
            # Trailing
            if self._trailing_points.Value > 0:
                dist = self._trailing_points.Value * step
                if close > self._entry_price + dist:
                    candidate = close - dist
                    if self._trailing_stop is None or candidate > self._trailing_stop:
                        self._trailing_stop = candidate
                if self._trailing_stop is not None and float(candle.LowPrice) <= self._trailing_stop:
                    self.SellMarket()
                    self._trailing_stop = None
                    return
            # SL
            if self._sl_points.Value > 0:
                sl = self._entry_price - self._sl_points.Value * step
                if float(candle.LowPrice) <= sl:
                    self.SellMarket()
                    self._trailing_stop = None
                    return
            # TP
            if self._tp_points.Value > 0:
                tp = self._entry_price + self._tp_points.Value * step
                if float(candle.HighPrice) >= tp:
                    self.SellMarket()
                    self._trailing_stop = None
        elif self.Position < 0 and self._entry_price > 0:
            if self._trailing_points.Value > 0:
                dist = self._trailing_points.Value * step
                if close < self._entry_price - dist:
                    candidate = close + dist
                    if self._trailing_stop is None or candidate < self._trailing_stop:
                        self._trailing_stop = candidate
                if self._trailing_stop is not None and float(candle.HighPrice) >= self._trailing_stop:
                    self.BuyMarket()
                    self._trailing_stop = None
                    return
            if self._sl_points.Value > 0:
                sl = self._entry_price + self._sl_points.Value * step
                if float(candle.HighPrice) >= sl:
                    self.BuyMarket()
                    self._trailing_stop = None
                    return
            if self._tp_points.Value > 0:
                tp = self._entry_price - self._tp_points.Value * step
                if float(candle.LowPrice) <= tp:
                    self.BuyMarket()
                    self._trailing_stop = None

    def _calc_signal(self):
        k = 33 - self._risk.Value
        ssp = self._ssp.Value
        cb = self._count_bars.Value
        uptrend = False
        val = 0
        for i in range(cb - ssp, -1, -1):
            ss_max = self._get_high(i)
            ss_min = self._get_low(i)
            for i2 in range(i, i + ssp):
                h = self._get_high(i2)
                if ss_max < h:
                    ss_max = h
                lo = self._get_low(i2)
                if ss_min >= lo:
                    ss_min = lo
            smin = ss_min + (ss_max - ss_min) * k / 100.0
            smax = ss_max - (ss_max - ss_min) * k / 100.0
            c = self._get_close(i)
            if c < smin:
                uptrend = False
            if c > smax:
                uptrend = True
            val = 1 if uptrend else -1
        return val

    def _get_close(self, shift):
        idx = len(self._close_hist) - 1 - shift
        if idx < 0:
            idx = 0
        return self._close_hist[idx]

    def _get_high(self, shift):
        idx = len(self._high_hist) - 1 - shift
        if idx < 0:
            idx = 0
        return self._high_hist[idx]

    def _get_low(self, shift):
        idx = len(self._low_hist) - 1 - shift
        if idx < 0:
            idx = 0
        return self._low_hist[idx]

    def CreateClone(self):
        return silver_trend_v3_strategy()
