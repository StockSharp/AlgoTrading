import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class semilong_www_forex_instruments_info_strategy(Strategy):
    """Compares current price with two historical closes. Opens a position when
    the price sharply deviates from older levels. Uses StartProtection for SL/TP."""

    def __init__(self):
        super(semilong_www_forex_instruments_info_strategy, self).__init__()

        self._profit_points = self.Param("ProfitPoints", 120) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._loss_points = self.Param("LossPoints", 60) \
            .SetDisplay("Stop Loss (points)", "Distance in points for the protective stop", "Risk")
        self._shift_one = self.Param("ShiftOne", 5) \
            .SetNotNegative() \
            .SetDisplay("Primary Shift", "Bars between current close and comparison close", "Signals")
        self._move_one_points = self.Param("MoveOnePoints", 0) \
            .SetNotNegative() \
            .SetDisplay("Primary Move (points)", "Minimum deviation from primary shifted close", "Signals")
        self._shift_two = self.Param("ShiftTwo", 10) \
            .SetNotNegative() \
            .SetDisplay("Secondary Shift", "Additional bars on top of primary shift", "Signals")
        self._move_two_points = self.Param("MoveTwoPoints", 0) \
            .SetNotNegative() \
            .SetDisplay("Secondary Move (points)", "Minimum distance between two shifted closes", "Signals")
        self._fixed_volume = self.Param("FixedVolume", 1.0) \
            .SetDisplay("Fixed Volume", "Base volume when auto lot is disabled", "Money Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Time frame used for signal calculations", "General")

        self._closes = []
        self._pip_size = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def ProfitPoints(self):
        return self._profit_points.Value

    @property
    def LossPoints(self):
        return self._loss_points.Value

    @property
    def ShiftOne(self):
        return self._shift_one.Value

    @property
    def MoveOnePoints(self):
        return self._move_one_points.Value

    @property
    def ShiftTwo(self):
        return self._shift_two.Value

    @property
    def MoveTwoPoints(self):
        return self._move_two_points.Value

    @property
    def FixedVolume(self):
        return self._fixed_volume.Value

    def OnReseted(self):
        super(semilong_www_forex_instruments_info_strategy, self).OnReseted()
        self._closes = []
        self._pip_size = 0.0

    def OnStarted2(self, time):
        super(semilong_www_forex_instruments_info_strategy, self).OnStarted2(time)

        step = self.Security.PriceStep if self.Security is not None else 0.0
        if step is None or float(step) <= 0:
            self._pip_size = 1.0
        else:
            self._pip_size = float(step)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(
            Unit(2, UnitTypes.Percent),
            Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        self._closes.append(close)

        total_shift = self.ShiftOne + self.ShiftTwo
        if len(self._closes) > total_shift + 2:
            self._closes.pop(0)

        if len(self._closes) <= total_shift:
            return

        if self.Position != 0:
            return

        shifted_one_value = self._closes[len(self._closes) - 1 - self.ShiftOne]
        shifted_two_value = self._closes[len(self._closes) - 1 - total_shift]

        move_one = float(self.MoveOnePoints) * self._pip_size
        move_two = float(self.MoveTwoPoints) * self._pip_size

        price_delta = close - shifted_one_value
        close_delta = shifted_one_value - shifted_two_value

        buy_signal = price_delta < -move_one and close_delta > move_two
        sell_signal = price_delta > move_one and close_delta < -move_two

        if buy_signal:
            self.BuyMarket()
        elif sell_signal:
            self.SellMarket()

    def CreateClone(self):
        return semilong_www_forex_instruments_info_strategy()
