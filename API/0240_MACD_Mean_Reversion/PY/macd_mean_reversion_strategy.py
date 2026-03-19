import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_mean_reversion_strategy(Strategy):
    """
    MACD Histogram Mean Reversion: enters when histogram is significantly above/below its average.
    """

    def __init__(self):
        super(macd_mean_reversion_strategy, self).__init__()
        self._fast_period = self.Param("FastMacdPeriod", 12).SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowMacdPeriod", 26).SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("Signal Period", "Signal line period", "Indicators")
        self._average_period = self.Param("AveragePeriod", 20).SetDisplay("Average Period", "Period for histogram stats", "Settings")
        self._dev_mult = self.Param("DeviationMultiplier", 2.0).SetDisplay("Dev Mult", "Stddev multiplier", "Settings")
        self._sl_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._hist_values = []
        self._avg_hist = 0.0
        self._std_hist = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_mean_reversion_strategy, self).OnReseted()
        self._hist_values = []
        self._avg_hist = 0.0
        self._std_hist = 0.0

    def OnStarted(self, time):
        super(macd_mean_reversion_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_period.Value
        macd.Macd.LongMa.Length = self._slow_period.Value
        macd.SignalMa.Length = self._signal_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()
        self.StartProtection(None, Unit(self._sl_pct.Value, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        typed_val = macd_value
        if typed_val.Macd is None or typed_val.Signal is None:
            return
        macd_line = float(typed_val.Macd)
        signal_line = float(typed_val.Signal)
        hist = macd_line - signal_line
        lb = self._average_period.Value
        self._hist_values.append(hist)
        if len(self._hist_values) > lb:
            self._hist_values.pop(0)
        if len(self._hist_values) < lb:
            return
        cnt = len(self._hist_values)
        avg = sum(self._hist_values) / cnt
        var = sum((h - avg) ** 2 for h in self._hist_values) / cnt
        std = math.sqrt(var) if var > 0 else 0.0
        self._avg_hist = avg
        self._std_hist = std
        dm = self._dev_mult.Value
        if self.Position == 0:
            if hist < avg - dm * std:
                self.BuyMarket()
            elif hist > avg + dm * std:
                self.SellMarket()
        elif self.Position > 0:
            if hist > avg:
                self.SellMarket()
        elif self.Position < 0:
            if hist < avg:
                self.BuyMarket()

    def CreateClone(self):
        return macd_mean_reversion_strategy()
