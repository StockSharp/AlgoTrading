import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergence
from StockSharp.Algo.Strategies import Strategy


class fm_one_scalping_strategy(Strategy):

    def __init__(self):
        super(fm_one_scalping_strategy, self).__init__()

        self._fast_ma_period = self.Param("FastMaPeriod", 12) \
            .SetDisplay("Fast EMA Period", "Period for fast EMA", "Indicators")

        self._slow_ma_period = self.Param("SlowMaPeriod", 26) \
            .SetDisplay("Slow EMA Period", "Period for slow EMA", "Indicators")

        self._macd_signal_period = self.Param("MacdSignalPeriod", 9) \
            .SetDisplay("MACD Signal Period", "Signal line period for MACD", "Indicators")

        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percent of entry price", "Risk")

        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit as percent of entry price", "Risk")

        self._enable_trailing_stop = self.Param("EnableTrailingStop", True) \
            .SetDisplay("Trailing Stop", "Enable trailing stop", "Risk")

        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_since_trade = 0
        self._is_initialized = False

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_ma_period.Value = value

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_ma_period.Value = value

    @property
    def MacdSignalPeriod(self):
        return self._macd_signal_period.Value

    @MacdSignalPeriod.setter
    def MacdSignalPeriod(self, value):
        self._macd_signal_period.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    @property
    def EnableTrailingStop(self):
        return self._enable_trailing_stop.Value

    @EnableTrailingStop.setter
    def EnableTrailingStop(self, value):
        self._enable_trailing_stop.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(fm_one_scalping_strategy, self).OnStarted(time)

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        slow_ma = ExponentialMovingAverage()
        slow_ma.Length = self.SlowMaPeriod

        slow_inner = ExponentialMovingAverage()
        slow_inner.Length = self.SlowMaPeriod
        fast_inner = ExponentialMovingAverage()
        fast_inner.Length = self.FastMaPeriod
        macd = MovingAverageConvergenceDivergence(slow_inner, fast_inner)

        self._bars_since_trade = self.CooldownBars
        self._is_initialized = False

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(fast_ma, slow_ma, macd, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(self.TakeProfitPercent, UnitTypes.Percent),
            stopLoss=Unit(self.StopLossPercent, UnitTypes.Percent),
            isStopTrailing=self.EnableTrailingStop)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, fast_ma, slow_ma, macd_value):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_trade += 1
        fast = float(fast_ma)
        slow = float(slow_ma)
        macd_val = float(macd_value)

        if not self._is_initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._is_initialized = True
            return

        long_signal = self._prev_fast <= self._prev_slow and fast > slow and macd_val > 0.0
        short_signal = self._prev_fast >= self._prev_slow and fast < slow and macd_val < 0.0

        pos = self.Position

        if long_signal and pos <= 0 and self._bars_since_trade >= self.CooldownBars:
            self.BuyMarket(self.Volume + abs(pos))
            self._bars_since_trade = 0
        elif short_signal and pos >= 0 and self._bars_since_trade >= self.CooldownBars:
            self.SellMarket(self.Volume + abs(pos))
            self._bars_since_trade = 0

        self._prev_fast = fast
        self._prev_slow = slow

    def OnReseted(self):
        super(fm_one_scalping_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._bars_since_trade = self.CooldownBars
        self._is_initialized = False

    def CreateClone(self):
        return fm_one_scalping_strategy()
