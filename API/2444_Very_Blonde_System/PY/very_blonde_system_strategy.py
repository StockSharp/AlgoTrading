import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class very_blonde_system_strategy(Strategy):
    def __init__(self):
        super(very_blonde_system_strategy, self).__init__()

        self._count_bars = self.Param("CountBars", 10)
        self._limit = self.Param("Limit", 500.0)
        self._grid = self.Param("Grid", 35.0)
        self._amount = self.Param("Amount", 40.0)
        self._lock_down = self.Param("LockDown", 0.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._entry_price = 0.0
        self._is_long = False
        self._lock_activated = False
        self._lock_price = 0.0

    @property
    def CountBars(self):
        return self._count_bars.Value

    @CountBars.setter
    def CountBars(self, value):
        self._count_bars.Value = value

    @property
    def Limit(self):
        return self._limit.Value

    @Limit.setter
    def Limit(self, value):
        self._limit.Value = value

    @property
    def Grid(self):
        return self._grid.Value

    @Grid.setter
    def Grid(self, value):
        self._grid.Value = value

    @property
    def Amount(self):
        return self._amount.Value

    @Amount.setter
    def Amount(self, value):
        self._amount.Value = value

    @property
    def LockDown(self):
        return self._lock_down.Value

    @LockDown.setter
    def LockDown(self, value):
        self._lock_down.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(very_blonde_system_strategy, self).OnStarted(time)

        self._entry_price = 0.0
        self._is_long = False
        self._lock_activated = False
        self._lock_price = 0.0

        highest = Highest()
        highest.Length = self.CountBars
        lowest = Lowest()
        lowest.Length = self.CountBars

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(highest, lowest, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle, high_val, low_val):
        if candle.State != CandleStates.Finished:
            return

        high = float(high_val)
        low = float(low_val)
        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0

        if self.Position == 0:
            self._check_open(close, high, low, step)
        else:
            self._check_close(candle, step)

    def _check_open(self, close, high, low, step):
        limit_dist = float(self.Limit) * step

        if high - close > limit_dist:
            self.BuyMarket()
            self._entry_price = close
            self._is_long = True
            self._lock_activated = False
            self._lock_price = 0.0
        elif close - low > limit_dist:
            self.SellMarket()
            self._entry_price = close
            self._is_long = False
            self._lock_activated = False
            self._lock_price = 0.0

    def _check_close(self, candle, step):
        close = float(candle.ClosePrice)
        current_profit = self.Position * (close - self._entry_price)

        if current_profit >= float(self.Amount):
            self._close_all()
            return

        lock_down = float(self.LockDown)
        if lock_down <= 0.0:
            return

        if self._is_long:
            if not self._lock_activated and close - self._entry_price > lock_down * step:
                self._lock_activated = True
                self._lock_price = self._entry_price
            elif self._lock_activated and close <= self._lock_price:
                self._close_all()
        else:
            if not self._lock_activated and self._entry_price - close > lock_down * step:
                self._lock_activated = True
                self._lock_price = self._entry_price
            elif self._lock_activated and close >= self._lock_price:
                self._close_all()

    def _close_all(self):
        if self.Position > 0:
            self.SellMarket()
        elif self.Position < 0:
            self.BuyMarket()

        self._entry_price = 0.0
        self._lock_activated = False
        self._lock_price = 0.0

    def OnReseted(self):
        super(very_blonde_system_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._is_long = False
        self._lock_activated = False
        self._lock_price = 0.0

    def CreateClone(self):
        return very_blonde_system_strategy()
