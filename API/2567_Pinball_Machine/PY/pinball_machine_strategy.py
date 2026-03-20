import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class pinball_machine_strategy(Strategy):
    def __init__(self):
        super(pinball_machine_strategy, self).__init__()

        self._risk_percent = self.Param("RiskPercent", 1.0)
        self._min_offset_points = self.Param("MinOffsetPoints", 10)
        self._max_offset_points = self.Param("MaxOffsetPoints", 100)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0
        self._seed = 0

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def MinOffsetPoints(self):
        return self._min_offset_points.Value

    @MinOffsetPoints.setter
    def MinOffsetPoints(self, value):
        self._min_offset_points.Value = value

    @property
    def MaxOffsetPoints(self):
        return self._max_offset_points.Value

    @MaxOffsetPoints.setter
    def MaxOffsetPoints(self, value):
        self._max_offset_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _next_inclusive(self, min_val, max_val):
        low = min(min_val, max_val)
        high = max(min_val, max_val)
        self._seed = (self._seed * 1103515245 + 12345) & 0x7fffffff
        return low + self._seed % (high - low + 1)

    def _normalize_point_range(self):
        min_p = min(int(self.MinOffsetPoints), int(self.MaxOffsetPoints))
        max_p = max(int(self.MinOffsetPoints), int(self.MaxOffsetPoints))
        if min_p <= 0:
            min_p = 1
        if max_p < min_p:
            max_p = min_p
        return (min_p, max_p)

    def OnStarted(self, time):
        super(pinball_machine_strategy, self).OnStarted(time)

        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0
        self._seed = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._manage_open_position(candle)

        if self.Position != 0:
            return

        v1 = self._next_inclusive(0, 100)
        v2 = self._next_inclusive(0, 100)
        v3 = self._next_inclusive(0, 100)
        v4 = self._next_inclusive(0, 100)

        if v1 == v2:
            if self._try_open_long(candle):
                return

        if v3 == v4:
            self._try_open_short(candle)

    def _manage_open_position(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._stop_loss_price > 0.0 and low <= self._stop_loss_price:
                self.SellMarket()
                self._reset_targets()
                return
            if self._take_profit_price > 0.0 and high >= self._take_profit_price:
                self.SellMarket()
                self._reset_targets()
        elif self.Position < 0:
            if self._stop_loss_price > 0.0 and high >= self._stop_loss_price:
                self.BuyMarket()
                self._reset_targets()
                return
            if self._take_profit_price > 0.0 and low <= self._take_profit_price:
                self.BuyMarket()
                self._reset_targets()

    def _try_open_long(self, candle):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        min_p, max_p = self._normalize_point_range()
        stop_points = self._next_inclusive(min_p, max_p)
        take_points = self._next_inclusive(min_p, max_p)

        entry = float(candle.ClosePrice)
        stop = entry - stop_points * step
        take = entry + take_points * step

        self.BuyMarket()
        self._entry_price = entry
        self._stop_loss_price = stop
        self._take_profit_price = take
        return True

    def _try_open_short(self, candle):
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        if step <= 0.0:
            step = 1.0

        min_p, max_p = self._normalize_point_range()
        stop_points = self._next_inclusive(min_p, max_p)
        take_points = self._next_inclusive(min_p, max_p)

        entry = float(candle.ClosePrice)
        stop = entry + stop_points * step
        take = entry - take_points * step

        self.SellMarket()
        self._entry_price = entry
        self._stop_loss_price = stop
        self._take_profit_price = take
        return True

    def _reset_targets(self):
        self._stop_loss_price = 0.0
        self._take_profit_price = 0.0
        self._entry_price = 0.0

    def OnReseted(self):
        super(pinball_machine_strategy, self).OnReseted()
        self._reset_targets()
        self._seed = 0

    def CreateClone(self):
        return pinball_machine_strategy()
