import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class brakeout_trader_v1_strategy(Strategy):
    def __init__(self):
        super(brakeout_trader_v1_strategy, self).__init__()

        self._breakout_level = self.Param("BreakoutLevel", 65000.0)
        self._enable_long = self.Param("EnableLong", True)
        self._enable_short = self.Param("EnableShort", True)
        self._stop_loss_points = self.Param("StopLossPoints", 140.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 180.0)
        self._risk_percent = self.Param("RiskPercent", 10.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._previous_close = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._pip_size = 0.0

    @property
    def BreakoutLevel(self):
        return self._breakout_level.Value

    @BreakoutLevel.setter
    def BreakoutLevel(self, value):
        self._breakout_level.Value = value

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
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(brakeout_trader_v1_strategy, self).OnStarted2(time)

        ps = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        self._pip_size = ps

        if self.Security is not None and self.Security.Decimals is not None:
            d = self.Security.Decimals
            if d == 3 or d == 5:
                self._pip_size = ps * 10.0

        if self._pip_size <= 0.0:
            self._pip_size = 1.0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._manage_open_position(candle):
            self._previous_close = float(candle.ClosePrice)
            return

        if self._previous_close is None:
            self._previous_close = float(candle.ClosePrice)
            return

        current_close = float(candle.ClosePrice)
        prev_value = self._previous_close
        level = float(self.BreakoutLevel)

        breakout_up = current_close > level and prev_value <= level
        breakout_down = current_close < level and prev_value >= level

        if breakout_up and self.EnableLong:
            self._enter_long(current_close)
        elif breakout_down and self.EnableShort:
            self._enter_short(current_close)

        self._previous_close = current_close

    def _manage_open_position(self, candle):
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if self._stop_price is not None and low <= self._stop_price:
                self.SellMarket()
                self._reset_position_state()
                return True
            if self._take_price is not None and high >= self._take_price:
                self.SellMarket()
                self._reset_position_state()
                return True
        elif self.Position < 0:
            if self._stop_price is not None and high >= self._stop_price:
                self.BuyMarket()
                self._reset_position_state()
                return True
            if self._take_price is not None and low <= self._take_price:
                self.BuyMarket()
                self._reset_position_state()
                return True
        elif self._entry_price is not None:
            self._reset_position_state()

        return False

    def _enter_long(self, price):
        if self.Position > 0:
            return

        if self.Position < 0:
            self._reset_position_state()

        self.BuyMarket()
        self._set_position_targets(price, True)

    def _enter_short(self, price):
        if self.Position < 0:
            return

        if self.Position > 0:
            self._reset_position_state()

        self.SellMarket()
        self._set_position_targets(price, False)

    def _set_position_targets(self, entry_price, is_long):
        self._entry_price = entry_price
        sl = float(self.StopLossPoints)
        tp = float(self.TakeProfitPoints)

        if sl > 0.0 and self._pip_size > 0.0:
            self._stop_price = entry_price - sl * self._pip_size if is_long else entry_price + sl * self._pip_size
        else:
            self._stop_price = None

        if tp > 0.0 and self._pip_size > 0.0:
            self._take_price = entry_price + tp * self._pip_size if is_long else entry_price - tp * self._pip_size
        else:
            self._take_price = None

    def _reset_position_state(self):
        self._entry_price = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(brakeout_trader_v1_strategy, self).OnReseted()
        self._previous_close = None
        self._entry_price = None
        self._stop_price = None
        self._take_price = None
        self._pip_size = 0.0

    def CreateClone(self):
        return brakeout_trader_v1_strategy()
