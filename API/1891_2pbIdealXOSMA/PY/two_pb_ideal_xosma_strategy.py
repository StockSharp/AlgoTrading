import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class two_pb_ideal_xosma_strategy(Strategy):
    def __init__(self):
        super(two_pb_ideal_xosma_strategy, self).__init__()
        self._fast_period = self.Param("FastPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Fast MA", "Fast moving average period", "Indicator")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Slow MA", "Slow moving average period", "Indicator")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal", "Signal line period", "Indicator")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Minimum number of bars between entries", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._prev_hist = None
        self._bars_since_trade = 6

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def signal_period(self):
        return self._signal_period.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(two_pb_ideal_xosma_strategy, self).OnReseted()
        self._prev_hist = None
        self._bars_since_trade = self.cooldown_bars

    def OnStarted(self, time):
        super(two_pb_ideal_xosma_strategy, self).OnStarted(time)
        self.StartProtection(None, None)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.fast_period
        macd.Macd.LongMa.Length = self.slow_period
        macd.SignalMa.Length = self.signal_period
        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(macd, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        macd_line = value.Macd
        signal = value.Signal
        if macd_line is None or signal is None:
            return

        macd_line = float(macd_line)
        signal = float(signal)
        histogram = macd_line - signal
        self._bars_since_trade += 1

        if self._prev_hist is not None:
            buy_signal = self._prev_hist <= 0.0 and histogram > 0.0 and macd_line > 0.0
            sell_signal = self._prev_hist >= 0.0 and histogram < 0.0 and macd_line < 0.0

            if buy_signal and self.Position <= 0 and self._bars_since_trade >= self.cooldown_bars:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
                self._bars_since_trade = 0
            elif sell_signal and self.Position >= 0 and self._bars_since_trade >= self.cooldown_bars:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()
                self._bars_since_trade = 0

        self._prev_hist = histogram

    def CreateClone(self):
        return two_pb_ideal_xosma_strategy()
