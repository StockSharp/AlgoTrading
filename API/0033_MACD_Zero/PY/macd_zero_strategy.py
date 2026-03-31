import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_zero_strategy(Strategy):
    """
    MACD Zero line crossover strategy.
    Buys when MACD crosses above zero, sells when crosses below.
    """

    def __init__(self):
        super(macd_zero_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 8).SetDisplay("Fast EMA", "Fast EMA period for MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 17).SetDisplay("Slow EMA", "Slow EMA period for MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("Signal", "Signal line period for MACD", "MACD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 700).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_macd = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_zero_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(macd_zero_strategy, self).OnStarted2(time)

        self._prev_macd = 0.0
        self._has_prev = False
        self._cooldown = 0

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_period.Value
        macd.Macd.LongMa.Length = self._slow_period.Value
        macd.SignalMa.Length = self._signal_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_val):
        if candle.State != CandleStates.Finished:
            return

        if not macd_val.IsFormed:
            return

        if macd_val.Macd is None or macd_val.Signal is None:
            return

        macd_line = float(macd_val.Macd)

        if not self._has_prev:
            self._prev_macd = macd_line
            self._has_prev = True
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_macd = macd_line
            return

        cd = self._cooldown_bars.Value
        prev_below = self._prev_macd < 0
        curr_above = macd_line >= 0
        prev_above = self._prev_macd >= 0
        curr_below = macd_line < 0

        if self.Position == 0 and prev_below and curr_above:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and prev_above and curr_below:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and prev_above and curr_below:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and prev_below and curr_above:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_macd = macd_line

    def CreateClone(self):
        return macd_zero_strategy()
