import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Unit, UnitTypes


class semilong_www_forex_instruments_info_strategy(Strategy):
    def __init__(self):
        super(semilong_www_forex_instruments_info_strategy, self).__init__()

        self._profit_points = self.Param("ProfitPoints", 120) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._loss_points = self.Param("LossPoints", 60) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._shift_one = self.Param("ShiftOne", 5) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._move_one_points = self.Param("MoveOnePoints", 0) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._shift_two = self.Param("ShiftTwo", 10) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._move_two_points = self.Param("MoveTwoPoints", 0) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._decrease_factor = self.Param("DecreaseFactor", 14) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._fixed_volume = self.Param("FixedVolume", 1) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._trailing_points = self.Param("TrailingPoints", 0) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._use_auto_lot = self.Param("UseAutoLot", False) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._auto_margin_divider = self.Param("AutoMarginDivider", 7) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Take Profit (points)", "Distance in points for the take profit target", "Risk")

        self._closes = new()
        self._pip_size = 0.0
        self._position_direction = 0.0
        self._entry_price = 0.0
        self._best_price = 0.0
        self._loss_streak = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(semilong_www_forex_instruments_info_strategy, self).OnReseted()
        self._closes = new()
        self._pip_size = 0.0
        self._position_direction = 0.0
        self._entry_price = 0.0
        self._best_price = 0.0
        self._loss_streak = 0.0

    def OnStarted(self, time):
        super(semilong_www_forex_instruments_info_strategy, self).OnStarted(time)


        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return semilong_www_forex_instruments_info_strategy()
