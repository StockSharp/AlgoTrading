import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal
from StockSharp.Algo.Strategies import Strategy


class macd_waterline_cross_expectator_strategy(Strategy):
    def __init__(self):
        super(macd_waterline_cross_expectator_strategy, self).__init__()
        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._signal_period = self.Param("SignalPeriod", 9) \
            .SetDisplay("Signal", "Signal line period", "Indicators")
        self._stop_loss_pct = self.Param("StopLossPct", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._rr_multiplier = self.Param("RRMultiplier", 2.0) \
            .SetDisplay("RR Multiplier", "Risk reward multiplier", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle", "Candle time frame", "General")
        self._should_buy = True
        self._has_prev = False
        self._prev_signal = 0.0

    @property
    def fast_ema_period(self):
        return self._fast_ema_period.Value

    @property
    def slow_ema_period(self):
        return self._slow_ema_period.Value

    @property
    def signal_period(self):
        return self._signal_period.Value

    @property
    def stop_loss_pct(self):
        return self._stop_loss_pct.Value

    @property
    def rr_multiplier(self):
        return self._rr_multiplier.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(macd_waterline_cross_expectator_strategy, self).OnReseted()
        self._should_buy = True
        self._has_prev = False
        self._prev_signal = 0.0

    def OnStarted(self, time):
        super(macd_waterline_cross_expectator_strategy, self).OnStarted(time)
        sl = float(self.stop_loss_pct)
        rr = float(self.rr_multiplier)
        self.StartProtection(
            takeProfit=Unit(sl * rr, UnitTypes.Percent),
            stopLoss=Unit(sl, UnitTypes.Percent))
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.fast_ema_period
        macd.Macd.LongMa.Length = self.slow_ema_period
        macd.SignalMa.Length = self.signal_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, macd)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return
        if not macd_value.IsFinal or not macd_value.IsFormed:
            return
        signal = macd_value.Signal
        if signal is None:
            return
        signal_val = float(signal)
        if not self._has_prev:
            self._prev_signal = signal_val
            self._has_prev = True
            return
        crossed_above = self._prev_signal < 0 and signal_val > 0
        crossed_below = self._prev_signal > 0 and signal_val < 0
        if crossed_above and self._should_buy:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._should_buy = False
        elif crossed_below and not self._should_buy:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._should_buy = True
        self._prev_signal = signal_val

    def CreateClone(self):
        return macd_waterline_cross_expectator_strategy()
