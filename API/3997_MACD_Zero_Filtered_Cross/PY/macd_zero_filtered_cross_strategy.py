import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal

class macd_zero_filtered_cross_strategy(Strategy):
    def __init__(self):
        super(macd_zero_filtered_cross_strategy, self).__init__()

        self._fast_period = self.Param("FastPeriod", 12) \
            .SetDisplay("Fast Period", "Short EMA period for MACD", "MACD")
        self._slow_period = self.Param("SlowPeriod", 26) \
            .SetDisplay("Slow Period", "Long EMA period for MACD", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal Period", "Signal line length for MACD", "MACD")
        self._take_profit_points = self.Param("TakeProfitPoints", 300.0) \
            .SetDisplay("Take Profit (points)", "Fixed take-profit distance in price points", "Risk Management")
        self._lot_volume = self.Param("LotVolume", 1.0) \
            .SetDisplay("Lot Volume", "Trading volume per order", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe that drives MACD calculations", "General")

        self._previous_macd = None
        self._previous_signal = None

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def SignalPeriod(self):
        return self._signal_period.Value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @property
    def LotVolume(self):
        return self._lot_volume.Value

    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(macd_zero_filtered_cross_strategy, self).OnStarted2(time)

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.FastPeriod
        macd.Macd.LongMa.Length = self.SlowPeriod
        macd.SignalMa.Length = self.SignalPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, self.ProcessCandle).Start()

        tp = float(self.TakeProfitPoints)
        if tp > 0:
            self.StartProtection(Unit(tp, UnitTypes.Absolute), None)

    def ProcessCandle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_line = macd_value.Macd
        signal_line = macd_value.Signal

        if macd_line is None or signal_line is None:
            return

        macd_line = float(macd_line)
        signal_line = float(signal_line)

        if self._previous_macd is not None and self._previous_signal is not None:
            cross_up = self._previous_macd < self._previous_signal and macd_line > signal_line
            cross_down = self._previous_macd > self._previous_signal and macd_line < signal_line

            if cross_down and self.Position > 0:
                self.SellMarket(self.Position)
                self._previous_macd = macd_line
                self._previous_signal = signal_line
                return

            if cross_up and self.Position < 0:
                self.BuyMarket(abs(self.Position))
                self._previous_macd = macd_line
                self._previous_signal = signal_line
                return

            if cross_up and macd_line < 0 and signal_line < 0 and self.Position <= 0:
                self.BuyMarket(float(self.LotVolume))
            elif cross_down and macd_line > 0 and signal_line > 0 and self.Position >= 0:
                self.SellMarket(float(self.LotVolume))

        self._previous_macd = macd_line
        self._previous_signal = signal_line

    def OnReseted(self):
        super(macd_zero_filtered_cross_strategy, self).OnReseted()
        self._previous_macd = None
        self._previous_signal = None

    def CreateClone(self):
        return macd_zero_filtered_cross_strategy()
