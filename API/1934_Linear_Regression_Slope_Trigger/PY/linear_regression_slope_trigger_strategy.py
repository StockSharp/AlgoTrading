import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class linear_regression_slope_trigger_strategy(Strategy):

    def __init__(self):
        super(linear_regression_slope_trigger_strategy, self).__init__()

        self._slope_length = self.Param("SlopeLength", 12) \
            .SetDisplay("Slope Length", "Period for the smoothed trend line", "Indicator")
        self._trigger_shift = self.Param("TriggerShift", 2) \
            .SetDisplay("Trigger Shift", "Bars used for trigger smoothing", "Indicator")
        self._enable_long = self.Param("EnableLong", True) \
            .SetDisplay("Enable Long", "Allow long trades", "Trading")
        self._enable_short = self.Param("EnableShort", True) \
            .SetDisplay("Enable Short", "Allow short trades", "Trading")
        self._take_profit_percent = self.Param("TakeProfitPercent", 5.0) \
            .SetDisplay("Take Profit %", "Take-profit percentage", "Risk Management")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop-loss percentage", "Risk Management")
        self._cooldown_bars = self.Param("CooldownBars", 1) \
            .SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")

        self._trend_line = ExponentialMovingAverage()
        self._trigger_line = SimpleMovingAverage()
        self._previous_trend_value = 0.0
        self._previous_slope = 0.0
        self._previous_trigger = 0.0
        self._is_initialized = False
        self._bars_since_trade = 0

    @property
    def SlopeLength(self):
        return self._slope_length.Value

    @SlopeLength.setter
    def SlopeLength(self, value):
        self._slope_length.Value = value

    @property
    def TriggerShift(self):
        return self._trigger_shift.Value

    @TriggerShift.setter
    def TriggerShift(self, value):
        self._trigger_shift.Value = value

    @property
    def EnableLong(self):
        return self._enable_long.Value

    @EnableLong.setter
    def EnableLong(self, value):
        self._enable_long.Value = value

    @property
    def EnableShort(self):
        return self._enable_short.Value

    @EnableShort.setter
    def EnableShort(self, value):
        self._enable_short.Value = value

    @property
    def TakeProfitPercent(self):
        return self._take_profit_percent.Value

    @TakeProfitPercent.setter
    def TakeProfitPercent(self, value):
        self._take_profit_percent.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

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
        super(linear_regression_slope_trigger_strategy, self).OnStarted(time)

        self._trend_line.Length = self.SlopeLength
        self._trigger_line.Length = self.TriggerShift

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfitPercent, UnitTypes.Percent),
            Unit(self.StopLossPercent, UnitTypes.Percent))

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ti = DecimalIndicatorValue(self._trend_line, candle.ClosePrice, candle.ServerTime)
        ti.IsFinal = True
        trend_value = float(self._trend_line.Process(ti))

        if not self._trend_line.IsFormed:
            return

        if not self._is_initialized:
            self._previous_trend_value = trend_value
            self._is_initialized = True
            return

        slope = trend_value - self._previous_trend_value
        tri = DecimalIndicatorValue(self._trigger_line, slope, candle.ServerTime)
        tri.IsFinal = True
        trigger = float(self._trigger_line.Process(tri))

        if not self._trigger_line.IsFormed:
            self._previous_trend_value = trend_value
            self._previous_slope = slope
            self._previous_trigger = slope
            return

        if self._bars_since_trade < self.CooldownBars:
            self._bars_since_trade += 1

        buy_signal = self._previous_trigger <= self._previous_slope and trigger > slope and slope > 0.0
        sell_signal = self._previous_trigger >= self._previous_slope and trigger < slope and slope < 0.0

        if self._bars_since_trade >= self.CooldownBars and self.Position == 0:
            if buy_signal and self.EnableLong:
                self.BuyMarket()
                self._bars_since_trade = 0
            elif sell_signal and self.EnableShort:
                self.SellMarket()
                self._bars_since_trade = 0

        self._previous_trend_value = trend_value
        self._previous_slope = slope
        self._previous_trigger = trigger

    def OnReseted(self):
        super(linear_regression_slope_trigger_strategy, self).OnReseted()
        self._trend_line.Reset()
        self._trigger_line.Reset()
        self._previous_trend_value = 0.0
        self._previous_slope = 0.0
        self._previous_trigger = 0.0
        self._is_initialized = False
        self._bars_since_trade = self.CooldownBars

    def CreateClone(self):
        return linear_regression_slope_trigger_strategy()
