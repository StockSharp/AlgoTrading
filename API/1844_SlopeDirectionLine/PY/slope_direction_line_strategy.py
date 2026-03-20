import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class slope_direction_line_strategy(Strategy):
    def __init__(self):
        super(slope_direction_line_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for candles", "General")
        self._length = self.Param("Length", 20) \
            .SetDisplay("Trend Length", "Number of bars in the trend line", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 2.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._allow_long = self.Param("AllowLong", True) \
            .SetDisplay("Allow Long", "Enable long entries", "Trading")
        self._allow_short = self.Param("AllowShort", True) \
            .SetDisplay("Allow Short", "Enable short entries", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._prev_trend = None
        self._prev_slope = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def length(self):
        return self._length.Value

    @property
    def take_profit_percent(self):
        return self._take_profit_percent.Value

    @property
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def allow_long(self):
        return self._allow_long.Value

    @property
    def allow_short(self):
        return self._allow_short.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(slope_direction_line_strategy, self).OnReseted()
        self._prev_trend = None
        self._prev_slope = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(slope_direction_line_strategy, self).OnStarted(time)
        trend = ExponentialMovingAverage()
        trend.Length = self.length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(trend, self.process_candle).Start()
        self.StartProtection(
            Unit(float(self.take_profit_percent), UnitTypes.Percent),
            Unit(float(self.stop_loss_percent), UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, trend)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, trend_value):
        if candle.State != CandleStates.Finished:
            return
        trend_value = float(trend_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._prev_trend is None:
            self._prev_trend = trend_value
            return
        current_slope = trend_value - self._prev_trend
        if self._prev_slope is not None:
            if current_slope > 0 and self._prev_slope <= 0 and self._cooldown_remaining == 0:
                if self.Position < 0:
                    self.BuyMarket()
                if self.Position <= 0 and self.allow_long:
                    self.BuyMarket()
                    self._cooldown_remaining = self.cooldown_bars
            elif current_slope < 0 and self._prev_slope >= 0 and self._cooldown_remaining == 0:
                if self.Position > 0:
                    self.SellMarket()
                if self.Position >= 0 and self.allow_short:
                    self.SellMarket()
                    self._cooldown_remaining = self.cooldown_bars
        self._prev_trend = trend_value
        self._prev_slope = current_slope

    def CreateClone(self):
        return slope_direction_line_strategy()
