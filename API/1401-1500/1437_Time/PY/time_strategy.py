import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, Level1Fields
from StockSharp.Algo.Strategies import Strategy


class time_strategy(Strategy):
    def __init__(self):
        super(time_strategy, self).__init__()
        self._ticks_from_open = self.Param("TicksFromOpen", 0).SetNotNegative()
        self._seconds_condition = self.Param("SecondsCondition", 20).SetNotNegative()
        self._reset_on_new_bar = self.Param("ResetOnNewBar", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))

        self._bar_start = None
        self._bar_open = 0.0
        self._bar_high = 0.0
        self._condition_since = None

    def GetWorkingSecurities(self):
        return [(self.Security, DataType.Level1)]

    def OnReseted(self):
        super(time_strategy, self).OnReseted()
        self._bar_start = None
        self._bar_open = 0.0
        self._bar_high = 0.0
        self._condition_since = None

    def OnStarted2(self, time):
        super(time_strategy, self).OnStarted2(time)
        self.SubscribeLevel1().Bind(self._process_level1).Start()

    def _process_level1(self, message):
        price = message.TryGetDecimal(Level1Fields.LastTradePrice)
        if price is None:
            price = message.TryGetDecimal(Level1Fields.BestAskPrice)
        if price is None:
            price = message.TryGetDecimal(Level1Fields.BestBidPrice)
        if price is None or float(price) <= 0:
            return

        current_price = float(price)
        arg = self._candle_type.Value.Arg
        frame = arg if isinstance(arg, TimeSpan) and arg > TimeSpan.Zero else TimeSpan.FromMinutes(1)
        current_bar_start = self._align(message.ServerTime, frame)

        if self._bar_start != current_bar_start:
            self._bar_start = current_bar_start
            self._bar_open = current_price
            self._bar_high = current_price
            if bool(self._reset_on_new_bar.Value):
                self._condition_since = None
        else:
            self._bar_high = max(self._bar_high, current_price)

        point = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 0.0001
        if point <= 0:
            point = 0.0001

        condition = self.is_price_condition_met(
            self._bar_open, self._bar_high, point, int(self._ticks_from_open.Value))

        if not condition:
            self._condition_since = None
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            return

        if self._condition_since is None:
            self._condition_since = message.ServerTime

        if self.Position == 0 and (message.ServerTime - self._condition_since).TotalSeconds >= int(self._seconds_condition.Value):
            self.BuyMarket()

    @staticmethod
    def is_price_condition_met(open_price, high, price_step, ticks_from_open):
        return high - open_price >= ticks_from_open * price_step

    @staticmethod
    def _align(time, frame):
        ticks = (time.TimeOfDay.Ticks // frame.Ticks) * frame.Ticks
        return time.Date.AddTicks(ticks)

    def CreateClone(self):
        return time_strategy()
