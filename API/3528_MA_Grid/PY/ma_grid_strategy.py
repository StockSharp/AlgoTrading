import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_grid_strategy(Strategy):
    def __init__(self):
        super(ma_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._ma_period = self.Param("MaPeriod", 48)
        self._grid_amount = self.Param("GridAmount", 6)
        self._distance = self.Param("Distance", 0.005)

        self._current_grid = 0
        self._next_grid_price = 0.0
        self._last_grid_price = 0.0
        self._is_grid_initialized = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def MaPeriod(self):
        return self._ma_period.Value

    @MaPeriod.setter
    def MaPeriod(self, value):
        self._ma_period.Value = value

    @property
    def GridAmount(self):
        return self._grid_amount.Value

    @GridAmount.setter
    def GridAmount(self, value):
        self._grid_amount.Value = value

    @property
    def Distance(self):
        return self._distance.Value

    @Distance.setter
    def Distance(self, value):
        self._distance.Value = value

    def _get_effective_grid_amount(self):
        amount = self.GridAmount
        if amount < 2:
            amount = 2
        if amount % 2 != 0:
            amount += 1
        return amount

    def OnReseted(self):
        super(ma_grid_strategy, self).OnReseted()
        self._current_grid = 0
        self._next_grid_price = 0.0
        self._last_grid_price = 0.0
        self._is_grid_initialized = False

    def OnStarted(self, time):
        super(ma_grid_strategy, self).OnStarted(time)
        self._current_grid = 0
        self._next_grid_price = 0.0
        self._last_grid_price = 0.0
        self._is_grid_initialized = False

        ema = ExponentialMovingAverage()
        ema.Length = self.MaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _update_grid_levels(self, ema_val):
        dist = float(self.Distance)
        effective = self._get_effective_grid_amount()

        if self._current_grid < effective - 1:
            self._next_grid_price = ema_val * (1.0 + dist * (1.0 + self._current_grid))
        else:
            self._next_grid_price = 0.0

        if self._current_grid > 1 - effective:
            self._last_grid_price = ema_val * (1.0 - dist * (1.0 - self._current_grid))
        else:
            self._last_grid_price = 0.0

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        close = float(candle.ClosePrice)

        if not self._is_grid_initialized:
            self._is_grid_initialized = True
            # Determine initial grid position
            effective = self._get_effective_grid_amount()
            half = effective // 2
            dist = float(self.Distance)
            if close < ema_val:
                for i in range(1, half + 1):
                    level = ema_val * (1.0 - dist * i)
                    if close > level:
                        self._current_grid = 1 - i
                        break
                else:
                    self._current_grid = -half
            else:
                for i in range(1, half + 1):
                    level = ema_val * (1.0 + dist * i)
                    if close < level:
                        self._current_grid = i - 1
                        break
                else:
                    self._current_grid = half

            # Initial entry based on grid position
            if self._current_grid < 0 and self.Position <= 0:
                self.BuyMarket()
            elif self._current_grid > 0 and self.Position >= 0:
                self.SellMarket()

            self._update_grid_levels(ema_val)
            return

        self._update_grid_levels(ema_val)

        if self._next_grid_price > 0 and close >= self._next_grid_price:
            self._current_grid += 1
            self.SellMarket()
            self._update_grid_levels(ema_val)
        elif self._last_grid_price > 0 and close <= self._last_grid_price:
            self._current_grid -= 1
            self.BuyMarket()
            self._update_grid_levels(ema_val)

    def CreateClone(self):
        return ma_grid_strategy()
