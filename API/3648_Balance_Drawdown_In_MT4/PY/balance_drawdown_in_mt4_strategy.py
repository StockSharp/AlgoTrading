import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class balance_drawdown_in_mt4_strategy(Strategy):
    def __init__(self):
        super(balance_drawdown_in_mt4_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._start_balance = self.Param("StartBalance", 1000.0)
        self._stop_loss_points = self.Param("StopLossPoints", 300.0)
        self._take_profit_points = self.Param("TakeProfitPoints", 400.0)
        self._entry_cooldown_days = self.Param("EntryCooldownDays", 5)

        self._max_balance = 0.0
        self._last_entry_date = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def StartBalance(self):
        return self._start_balance.Value

    @StartBalance.setter
    def StartBalance(self, value):
        self._start_balance.Value = value

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
    def EntryCooldownDays(self):
        return self._entry_cooldown_days.Value

    @EntryCooldownDays.setter
    def EntryCooldownDays(self, value):
        self._entry_cooldown_days.Value = value

    def OnReseted(self):
        super(balance_drawdown_in_mt4_strategy, self).OnReseted()
        self._max_balance = 0.0
        self._last_entry_date = None

    def OnStarted(self, time):
        super(balance_drawdown_in_mt4_strategy, self).OnStarted(time)
        self._max_balance = float(self.StartBalance)
        self._last_entry_date = None

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        candle_date = candle.CloseTime.Date

        # Ensure position (long-only, with cooldown)
        if self.Position == 0:
            if self._last_entry_date is not None:
                days_diff = (candle_date - self._last_entry_date).TotalDays
                if days_diff < self.EntryCooldownDays:
                    return
            self.BuyMarket()
            self._last_entry_date = candle_date

    def CreateClone(self):
        return balance_drawdown_in_mt4_strategy()
