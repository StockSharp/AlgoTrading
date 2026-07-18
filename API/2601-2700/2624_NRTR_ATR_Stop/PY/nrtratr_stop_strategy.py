import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import AverageTrueRange


class nrtratr_stop_strategy(Strategy):
    """NRTR ATR Stop strategy: ATR-based trailing levels switch direction on price crossing."""

    def __init__(self):
        super(nrtratr_stop_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Number of candles used for ATR calculation", "Indicator")
        self._coefficient = self.Param("Coefficient", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Coefficient", "Multiplier applied to ATR for stop levels", "Indicator")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Closed candles to wait before acting", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for calculations", "General")
        self._enable_long_entry = self.Param("EnableLongEntry", True) \
            .SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading")
        self._enable_short_entry = self.Param("EnableShortEntry", True) \
            .SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading")
        self._enable_long_exit = self.Param("EnableLongExit", True) \
            .SetDisplay("Enable Long Exit", "Allow closing longs on sell signals", "Risk")
        self._enable_short_exit = self.Param("EnableShortExit", True) \
            .SetDisplay("Enable Short Exit", "Allow closing shorts on buy signals", "Risk")

        self._atr = None
        self._signals = []
        self._upper_stop = 0.0
        self._lower_stop = 0.0
        self._trend = 0
        self._has_stops = False
        self._has_previous = False
        self._prev_high = 0.0
        self._prev_low = 0.0

    @property
    def AtrPeriod(self):
        return self._atr_period.Value
    @property
    def Coefficient(self):
        return self._coefficient.Value
    @property
    def SignalBar(self):
        return self._signal_bar.Value
    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def EnableLongEntry(self):
        return self._enable_long_entry.Value
    @property
    def EnableShortEntry(self):
        return self._enable_short_entry.Value
    @property
    def EnableLongExit(self):
        return self._enable_long_exit.Value
    @property
    def EnableShortExit(self):
        return self._enable_short_exit.Value

    def OnStarted2(self, time):
        super(nrtratr_stop_strategy, self).OnStarted2(time)

        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._atr, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._atr.IsFormed:
            self._update_prev(candle)
            return

        if not self._has_previous:
            self._update_prev(candle)
            return

        prev_trend = self._trend
        trend = prev_trend
        upper_stop = self._upper_stop
        lower_stop = self._lower_stop
        coeff = float(self.Coefficient)
        atr_val = float(atr_value)
        rez = coeff * atr_val

        if not self._has_stops:
            upper_stop = self._prev_low - rez
            lower_stop = self._prev_high + rez
            self._has_stops = True

        if trend <= 0 and self._prev_low > lower_stop:
            upper_stop = self._prev_low - rez
            trend = 1

        if trend >= 0 and self._prev_high < upper_stop:
            lower_stop = self._prev_high + rez
            trend = -1

        if trend >= 0:
            if self._prev_low > upper_stop + rez:
                upper_stop = self._prev_low - rez

        if trend <= 0:
            if self._prev_high < lower_stop - rez:
                lower_stop = self._prev_high + rez

        buy_signal = trend > 0 and prev_trend <= 0
        sell_signal = trend < 0 and prev_trend >= 0

        self._trend = trend
        self._upper_stop = upper_stop
        self._lower_stop = lower_stop

        self._signals.append((buy_signal, sell_signal))

        if len(self._signals) <= self.SignalBar:
            self._update_prev(candle)
            return

        signal = self._signals.pop(0)

        if signal[0]:
            self._handle_buy()

        if signal[1]:
            self._handle_sell()

        self._update_prev(candle)

    def _handle_buy(self):
        vol = 0.0
        if self.EnableShortExit and self.Position < 0:
            vol += abs(self.Position)
        if self.EnableLongEntry and self.Position <= 0 and self.Volume > 0:
            vol += float(self.Volume)
        if vol > 0:
            self.BuyMarket()

    def _handle_sell(self):
        vol = 0.0
        if self.EnableLongExit and self.Position > 0:
            vol += float(self.Position)
        if self.EnableShortEntry and self.Position >= 0 and self.Volume > 0:
            vol += float(self.Volume)
        if vol > 0:
            self.SellMarket()

    def _update_prev(self, candle):
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
        self._has_previous = True

    def OnReseted(self):
        super(nrtratr_stop_strategy, self).OnReseted()
        self._signals = []
        self._upper_stop = 0.0
        self._lower_stop = 0.0
        self._trend = 0
        self._has_stops = False
        self._has_previous = False
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._atr = None

    def CreateClone(self):
        return nrtratr_stop_strategy()
