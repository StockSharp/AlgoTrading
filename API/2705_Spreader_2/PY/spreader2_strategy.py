import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class spreader2_strategy(Strategy):
    def __init__(self):
        super(spreader2_strategy, self).__init__()

        self._second_security_param = self.Param("SecondSecurity", None) \
            .SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General")
        self._primary_volume_param = self.Param("PrimaryVolume", 1) \
            .SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General")
        self._target_profit_param = self.Param("TargetProfit", 100) \
            .SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General")
        self._shift_param = self.Param("ShiftLength", 6) \
            .SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General")
        self._candle_type_param = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General")
        self._day_bars_param = self.Param("DayBars", 288) \
            .SetDisplay("Second Symbol", "Secondary instrument for the spread trade", "General")

        self._first_pending = new()
        self._second_pending = new()
        self._first_closes = new()
        self._second_closes = new()
        self._last_first_close = 0.0
        self._last_second_close = 0.0
        self._first_entry_price = 0.0
        self._second_entry_price = 0.0
        self._second_position = 0.0
        self._second_portfolio = None
        self._contracts_match = True

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(spreader2_strategy, self).OnReseted()
        self._first_pending = new()
        self._second_pending = new()
        self._first_closes = new()
        self._second_closes = new()
        self._last_first_close = 0.0
        self._last_second_close = 0.0
        self._first_entry_price = 0.0
        self._second_entry_price = 0.0
        self._second_position = 0.0
        self._second_portfolio = None
        self._contracts_match = True

    def OnStarted(self, time):
        super(spreader2_strategy, self).OnStarted(time)


        primary_subscription = self.SubscribeCandles(self.candle_type)
        primary_subscription.Bind(self._process_candle).Start()

        secondary_subscription = self.SubscribeCandles(self.candle_type, security: SecondSecurity)
        secondary_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return spreader2_strategy()
