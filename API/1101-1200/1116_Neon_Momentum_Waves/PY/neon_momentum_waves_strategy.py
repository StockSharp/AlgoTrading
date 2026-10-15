import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class neon_momentum_waves_strategy(Strategy):
    """
    Neon Momentum Waves strategy based on MACD histogram levels.
    The histogram (MACD line minus signal line) crossing above EntryLevel goes long and crossing below it goes short, reversing an
    opposite position. A long closes once the histogram reaches LongExitLevel and a short once it reaches ShortExitLevel.
    """

    def __init__(self):
        super(neon_momentum_waves_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetDisplay("Fast Length", "MACD fast EMA length", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetDisplay("Slow Length", "MACD slow EMA length", "MACD")
        self._signal_length = self.Param("SignalLength", 20).SetDisplay("Signal Length", "MACD signal smoothing", "MACD")
        self._entry_level = self.Param("EntryLevel", 0.0).SetDisplay("Entry Level", "Histogram entry threshold", "Parameters")
        self._long_exit_level = self.Param("LongExitLevel", 11.0).SetDisplay("Long Exit Level", "Histogram level to exit longs", "Parameters")
        self._short_exit_level = self.Param("ShortExitLevel", -9.0).SetDisplay("Short Exit Level", "Histogram level to exit shorts", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_hist = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(neon_momentum_waves_strategy, self).OnReseted()
        self._prev_hist = None

    def OnStarted2(self, time):
        super(neon_momentum_waves_strategy, self).OnStarted2(time)

        self._prev_hist = None

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

        if not macd_value.IsFormed:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal
        if macd_line is None or signal_line is None:
            return

        hist = float(macd_line) - float(signal_line)
        prev = self._prev_hist
        self._prev_hist = hist

        if prev is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        entry = float(self._entry_level.Value)

        if prev <= entry and hist > entry and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif prev >= entry and hist < entry and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and hist >= float(self._long_exit_level.Value):
            self.SellMarket(self.Position)
        elif self.Position < 0 and hist <= float(self._short_exit_level.Value):
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return neon_momentum_waves_strategy()
