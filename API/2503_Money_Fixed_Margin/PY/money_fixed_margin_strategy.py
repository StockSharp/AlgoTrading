import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class money_fixed_margin_strategy(Strategy):
    def __init__(self):
        super(money_fixed_margin_strategy, self).__init__()

        self._stop_loss_pips = self.Param("StopLossPips", 25.0)
        self._risk_percent = self.Param("RiskPercent", 10.0)
        self._check_interval = self.Param("CheckInterval", 150)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))

        self._bar_count = 0
        self._pip_size = 1.0

    @property
    def StopLossPips(self):
        return self._stop_loss_pips.Value

    @StopLossPips.setter
    def StopLossPips(self, value):
        self._stop_loss_pips.Value = value

    @property
    def RiskPercent(self):
        return self._risk_percent.Value

    @RiskPercent.setter
    def RiskPercent(self, value):
        self._risk_percent.Value = value

    @property
    def CheckInterval(self):
        return self._check_interval.Value

    @CheckInterval.setter
    def CheckInterval(self, value):
        self._check_interval.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(money_fixed_margin_strategy, self).OnStarted(time)

        self._bar_count = 0

        sec = self.Security
        price_step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if price_step <= 0.0:
            price_step = 1.0
        decimals = int(sec.Decimals) if sec is not None and sec.Decimals is not None else 0
        adjust = 10.0 if decimals == 3 or decimals == 5 else 1.0
        self._pip_size = price_step * adjust
        if self._pip_size <= 0.0:
            self._pip_size = price_step if price_step > 0.0 else 1.0

        sl_distance = float(self.StopLossPips) * self._pip_size

        self.StartProtection(
            Unit(sl_distance, UnitTypes.Absolute),
            Unit(sl_distance * 2.0, UnitTypes.Absolute))

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._bar_count += 1

        if self._bar_count < int(self.CheckInterval):
            return

        entry_price = float(candle.ClosePrice)
        if entry_price <= 0.0:
            return

        self.BuyMarket()
        self._bar_count = 0

    def OnReseted(self):
        super(money_fixed_margin_strategy, self).OnReseted()
        self._bar_count = 0
        self._pip_size = 1.0

    def CreateClone(self):
        return money_fixed_margin_strategy()
