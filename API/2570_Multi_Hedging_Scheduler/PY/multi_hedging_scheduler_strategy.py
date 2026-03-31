import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

SIDE_BUY = 0
SIDE_SELL = 1


class multi_hedging_scheduler_strategy(Strategy):
    def __init__(self):
        super(multi_hedging_scheduler_strategy, self).__init__()

        self._trade_direction = self.Param("TradeDirection", SIDE_BUY)
        self._trade_start_hour = self.Param("TradeStartHour", 10)
        self._trade_start_minute = self.Param("TradeStartMinute", 0)
        self._trade_duration_minutes = self.Param("TradeDurationMinutes", 5)
        self._enable_time_close = self.Param("UseTimeClose", True)
        self._close_hour = self.Param("CloseHour", 17)
        self._close_minute = self.Param("CloseMinute", 0)
        self._enable_equity_close = self.Param("CloseByEquityPercent", True)
        self._profit_percent = self.Param("PercentProfit", 1.0)
        self._loss_percent = self.Param("PercentLoss", 55.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))

        self._initial_balance = 0.0
        self._position_opened = False

    @property
    def TradeDirection(self):
        return self._trade_direction.Value

    @TradeDirection.setter
    def TradeDirection(self, value):
        self._trade_direction.Value = value

    @property
    def TradeStartHour(self):
        return self._trade_start_hour.Value

    @TradeStartHour.setter
    def TradeStartHour(self, value):
        self._trade_start_hour.Value = value

    @property
    def TradeStartMinute(self):
        return self._trade_start_minute.Value

    @TradeStartMinute.setter
    def TradeStartMinute(self, value):
        self._trade_start_minute.Value = value

    @property
    def TradeDurationMinutes(self):
        return self._trade_duration_minutes.Value

    @TradeDurationMinutes.setter
    def TradeDurationMinutes(self, value):
        self._trade_duration_minutes.Value = value

    @property
    def UseTimeClose(self):
        return self._enable_time_close.Value

    @UseTimeClose.setter
    def UseTimeClose(self, value):
        self._enable_time_close.Value = value

    @property
    def CloseHour(self):
        return self._close_hour.Value

    @CloseHour.setter
    def CloseHour(self, value):
        self._close_hour.Value = value

    @property
    def CloseMinute(self):
        return self._close_minute.Value

    @CloseMinute.setter
    def CloseMinute(self, value):
        self._close_minute.Value = value

    @property
    def CloseByEquityPercent(self):
        return self._enable_equity_close.Value

    @CloseByEquityPercent.setter
    def CloseByEquityPercent(self, value):
        self._enable_equity_close.Value = value

    @property
    def PercentProfit(self):
        return self._profit_percent.Value

    @PercentProfit.setter
    def PercentProfit(self, value):
        self._profit_percent.Value = value

    @property
    def PercentLoss(self):
        return self._loss_percent.Value

    @PercentLoss.setter
    def PercentLoss(self, value):
        self._loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def _is_within_window(self, current_minutes, start_hour, start_minute, duration_minutes):
        start_total = start_hour * 60 + start_minute
        end_total = start_total + duration_minutes

        if end_total < 1440:
            return current_minutes >= start_total and current_minutes < end_total
        else:
            overflow = end_total - 1440
            return current_minutes >= start_total or current_minutes < overflow

    def OnStarted2(self, time):
        super(multi_hedging_scheduler_strategy, self).OnStarted2(time)

        self._initial_balance = 0.0
        self._position_opened = False

        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._initial_balance == 0.0 and self.Portfolio is not None and self.Portfolio.CurrentValue is not None:
            self._initial_balance = float(self.Portfolio.CurrentValue)

        open_time = candle.OpenTime
        hour = open_time.Hour
        minute = open_time.Minute
        current_minutes = hour * 60 + minute
        duration = int(self.TradeDurationMinutes)

        if self.CloseByEquityPercent and self._try_handle_equity_targets():
            return

        if self.UseTimeClose and self._is_within_window(current_minutes, int(self.CloseHour), int(self.CloseMinute), duration):
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._position_opened = False
            return

        if not self._is_within_window(current_minutes, int(self.TradeStartHour), int(self.TradeStartMinute), duration):
            return

        if self._position_opened:
            return

        direction = int(self.TradeDirection)

        if direction == SIDE_BUY and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._position_opened = True
        elif direction == SIDE_SELL and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._position_opened = True

    def _try_handle_equity_targets(self):
        if self._initial_balance <= 0.0:
            return False

        if self.Portfolio is None or self.Portfolio.CurrentValue is None:
            return False

        equity = float(self.Portfolio.CurrentValue)
        profit_level = self._initial_balance * (1.0 + float(self.PercentProfit) / 100.0)
        loss_level = self._initial_balance * (1.0 - float(self.PercentLoss) / 100.0)

        if equity >= profit_level or equity <= loss_level:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._position_opened = False
            return True

        return False

    def OnReseted(self):
        super(multi_hedging_scheduler_strategy, self).OnReseted()
        self._initial_balance = 0.0
        self._position_opened = False

    def CreateClone(self):
        return multi_hedging_scheduler_strategy()
