import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy

class macd_breakout_strategy(Strategy):
    """
    MACD Breakout: enters when MACD histogram breaks out of its normal range.
    """

    def __init__(self):
        super(macd_breakout_strategy, self).__init__()
        self._fast_ema = self.Param("FastEmaPeriod", 12).SetDisplay("Fast EMA", "Fast EMA period", "MACD")
        self._slow_ema = self.Param("SlowEmaPeriod", 26).SetDisplay("Slow EMA", "Slow EMA period", "MACD")
        self._signal_period = self.Param("SignalPeriod", 9).SetDisplay("Signal Period", "Signal line period", "MACD")
        self._sma_period = self.Param("SmaPeriod", 20).SetDisplay("SMA Period", "Histogram SMA period", "Indicators")
        self._dev_mult = self.Param("DeviationMultiplier", 2.0).SetDisplay("Dev Mult", "Stddev multiplier", "Breakout")
        self._sl_pct = self.Param("StopLossPercent", 2.0).SetDisplay("SL %", "Stop loss percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._hist_values = []
        self._prev_hist = 0.0
        self._prev_hist_sma = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_breakout_strategy, self).OnReseted()
        self._hist_values = []
        self._prev_hist = 0.0
        self._prev_hist_sma = 0.0

    def OnStarted(self, time):
        super(macd_breakout_strategy, self).OnStarted(time)
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_ema.Value
        macd.Macd.LongMa.Length = self._slow_ema.Value
        macd.SignalMa.Length = self._signal_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self._process_candle).Start()
        sl = self._sl_pct.Value
        self.StartProtection(Unit(sl, UnitTypes.Percent), Unit(sl * 1.5, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        typed_val = macd_value
        if typed_val.Macd is None or typed_val.Signal is None:
            return
        macd_line = float(typed_val.Macd)
        signal_line = float(typed_val.Signal)
        hist = macd_line - signal_line
        lb = self._sma_period.Value
        self._hist_values.append(hist)
        if len(self._hist_values) > lb:
            self._hist_values.pop(0)
        if len(self._hist_values) < lb:
            return
        avg = sum(self._hist_values) / lb
        var = sum((h - avg) ** 2 for h in self._hist_values) / lb
        std = math.sqrt(var) if var > 0 else 0.0
        dm = self._dev_mult.Value
        upper = avg + dm * std
        lower = avg - dm * std
        if hist > upper and self.Position <= 0:
            self.BuyMarket()
        elif hist < lower and self.Position >= 0:
            self.SellMarket()
        elif self.Position > 0 and hist < avg:
            self.SellMarket()
        elif self.Position < 0 and hist > avg:
            self.BuyMarket()

    def CreateClone(self):
        return macd_breakout_strategy()
