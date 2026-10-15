import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Unit, UnitTypes, CandleStates
from StockSharp.Algo.Strategies import Strategy


class trendline_alert_strategy(Strategy):

    def __init__(self):
        super(trendline_alert_strategy, self).__init__()

        self._breakout_points = self.Param("BreakoutPoints", 0) \
            .SetDisplay("Breakout Points", "Additional points for breakout", "General")
        self._start_hour = self.Param("StartHour", 0) \
            .SetDisplay("Start Hour", "Strategy start hour", "General")
        self._end_hour = self.Param("EndHour", 24) \
            .SetDisplay("End Hour", "Strategy end hour", "General")
        self._use_trailing_stop = self.Param("UseTrailingStop", False) \
            .SetDisplay("Use Trailing Stop", "Enable trailing stop", "Protection")
        self._trailing_stop_points = self.Param("TrailingStopPoints", 5) \
            .SetDisplay("Trailing Stop Points", "Trailing stop distance", "Protection")
        self._upper_line = self.Param("UpperLine", 68000.0) \
            .SetDisplay("Upper Line", "Upper trendline level", "Levels")
        self._lower_line = self.Param("LowerLine", 62000.0) \
            .SetDisplay("Lower Line", "Lower trendline level", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._last_price = 0.0
        self._stop_price = 0.0
        self._up_alerted = False
        self._down_alerted = False

    @property
    def BreakoutPoints(self):
        return self._breakout_points.Value

    @BreakoutPoints.setter
    def BreakoutPoints(self, value):
        self._breakout_points.Value = value

    @property
    def StartHour(self):
        return self._start_hour.Value

    @StartHour.setter
    def StartHour(self, value):
        self._start_hour.Value = value

    @property
    def EndHour(self):
        return self._end_hour.Value

    @EndHour.setter
    def EndHour(self, value):
        self._end_hour.Value = value

    @property
    def UseTrailingStop(self):
        return self._use_trailing_stop.Value

    @UseTrailingStop.setter
    def UseTrailingStop(self, value):
        self._use_trailing_stop.Value = value

    @property
    def TrailingStopPoints(self):
        return self._trailing_stop_points.Value

    @TrailingStopPoints.setter
    def TrailingStopPoints(self, value):
        self._trailing_stop_points.Value = value

    @property
    def UpperLine(self):
        return self._upper_line.Value

    @UpperLine.setter
    def UpperLine(self, value):
        self._upper_line.Value = value

    @property
    def LowerLine(self):
        return self._lower_line.Value

    @LowerLine.setter
    def LowerLine(self, value):
        self._lower_line.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _update_trailing_stop(self, candle):
        step_raw = self.Security.PriceStep
        step = float(step_raw) if step_raw is not None else 1.0
        trail = self.TrailingStopPoints * step

        if self.Position > 0:
            self._stop_price = max(self._stop_price, float(candle.ClosePrice) - trail)
            if float(candle.LowPrice) <= self._stop_price:
                self.SellMarket()
        elif self.Position < 0:
            self._stop_price = min(self._stop_price, float(candle.ClosePrice) + trail)
            if float(candle.HighPrice) >= self._stop_price:
                self.BuyMarket()
        else:
            self._stop_price = 0.0

    def OnStarted2(self, time):
        super(trendline_alert_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(2, UnitTypes.Percent))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        hour = candle.OpenTime.Hour
        if hour < self.StartHour or hour >= self.EndHour:
            return

        step_raw = self.Security.PriceStep
        step = float(step_raw) if step_raw is not None else 1.0
        threshold = self.BreakoutPoints * step

        upper = float(self.UpperLine) + threshold
        lower = float(self.LowerLine) - threshold
        price = float(candle.ClosePrice)

        if not self._up_alerted and price > upper and self._last_price <= upper:
            self._up_alerted = True
            if self.Position <= 0:
                self.BuyMarket()
        elif not self._down_alerted and price < lower and self._last_price >= lower:
            self._down_alerted = True
            if self.Position >= 0:
                self.SellMarket()

        if self.UseTrailingStop:
            self._update_trailing_stop(candle)

        self._last_price = price

    def OnReseted(self):
        super(trendline_alert_strategy, self).OnReseted()
        self._last_price = 0.0
        self._stop_price = 0.0
        self._up_alerted = False
        self._down_alerted = False

    def CreateClone(self):
        return trendline_alert_strategy()
