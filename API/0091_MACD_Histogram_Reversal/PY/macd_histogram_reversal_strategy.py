import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceHistogram
from StockSharp.Algo.Strategies import Strategy

class macd_histogram_reversal_strategy(Strategy):
    """
    MACD Histogram Reversal strategy.
    Enters long when MACD histogram (MACD - Signal) crosses above zero.
    Enters short when MACD histogram crosses below zero.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(macd_histogram_reversal_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 12).SetDisplay("Fast Period", "Fast EMA period for MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26).SetDisplay("Slow Period", "Slow EMA period for MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("Signal Period", "Signal line period", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_histogram = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_histogram_reversal_strategy, self).OnReseted()
        self._prev_histogram = None
        self._cooldown = 0

    def OnStarted2(self, time):
        super(macd_histogram_reversal_strategy, self).OnStarted2(time)

        self._prev_histogram = None
        self._cooldown = 0

        macd_hist = MovingAverageConvergenceDivergenceHistogram()
        macd_hist.Macd.ShortMa.Length = self._fast_period.Value
        macd_hist.Macd.LongMa.Length = self._slow_period.Value
        macd_hist.SignalMa.Length = self._signal_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd_hist, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd_hist)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_iv):
        if candle.State != CandleStates.Finished:
            return

        if not macd_iv.IsFormed:
            return

        macd_val = macd_iv.Macd
        signal_val = macd_iv.Signal

        if macd_val is None or signal_val is None:
            return

        histogram = float(macd_val) - float(signal_val)

        if self._prev_histogram is None:
            self._prev_histogram = histogram
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_histogram = histogram
            return

        cd = self._cooldown_bars.Value

        crossed_above_zero = self._prev_histogram < 0 and histogram >= 0
        crossed_below_zero = self._prev_histogram > 0 and histogram <= 0

        if self.Position == 0 and crossed_above_zero:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and crossed_below_zero:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and crossed_below_zero:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and crossed_above_zero:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_histogram = histogram

    def CreateClone(self):
        return macd_histogram_reversal_strategy()
