import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_crossover_strategy(Strategy):
    """
    MACD crossover within predefined zone. Goes long on cross above signal in zone.
    """

    def __init__(self):
        super(macd_crossover_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetDisplay("Fast Length", "Fast EMA period", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetDisplay("Slow Length", "Slow EMA period", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetDisplay("Signal Length", "Signal line period", "MACD")
        self._lower_threshold = self.Param("LowerThreshold", -100.0).SetDisplay("Lower Threshold", "Lower bound for MACD zone", "Zone")
        self._upper_threshold = self.Param("UpperThreshold", 100.0).SetDisplay("Upper Threshold", "Upper bound for MACD zone", "Zone")
        self._cooldown_bars = self.Param("SignalCooldownBars", 3).SetDisplay("Cooldown Bars", "Bars between signals", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(10))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_above = False
        self._bars_from_signal = 9999

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_crossover_strategy, self).OnReseted()
        self._prev_above = False
        self._bars_from_signal = 9999

    def OnStarted(self, time):
        super(macd_crossover_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        typed_val = macd_value
        macd_line = typed_val.Macd
        signal_line = typed_val.Signal
        if macd_line is None or signal_line is None:
            return
        macd_f = float(macd_line)
        signal_f = float(signal_line)
        is_above = macd_f > signal_f
        cross_up = is_above and not self._prev_above
        cross_down = not is_above and self._prev_above
        in_zone = macd_f >= self._lower_threshold.Value and macd_f <= self._upper_threshold.Value
        self._bars_from_signal += 1
        if self._bars_from_signal >= self._cooldown_bars.Value:
            if cross_up:
                if self.Position < 0:
                    self.BuyMarket()
                    self._bars_from_signal = 0
                elif in_zone and self.Position == 0:
                    self.BuyMarket()
                    self._bars_from_signal = 0
            elif cross_down:
                if self.Position > 0:
                    self.SellMarket()
                    self._bars_from_signal = 0
                elif in_zone and self.Position == 0:
                    self.SellMarket()
                    self._bars_from_signal = 0
        self._prev_above = is_above

    def CreateClone(self):
        return macd_crossover_strategy()
