import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, DayOfWeek, TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class dca_strategy(Strategy):
    """Buy a fixed quote-currency amount on a weekday or every N finished candles."""

    def __init__(self):
        super(dca_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Completed-candle schedule", "General")
        self._capital_to_invest = self.Param("CapitalToInvest", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Capital To Invest", "Fixed quote-currency amount per purchase", "Trading")
        self._start_date = self.Param("StartDate", DateTime.MinValue) \
            .SetDisplay("Start Date", "Inclusive buying start", "Calendar")
        self._end_date = self.Param("EndDate", DateTime.MaxValue) \
            .SetDisplay("End Date", "Exclusive buying end", "Calendar")
        self._close_all_on_last_candle = self.Param("CloseAllOnLastCandle", False) \
            .SetDisplay("Close All On Last Candle", "Close holdings when EndDate is reached", "Calendar")
        self._use_day_of_week = self.Param("UseDayOfWeek", False) \
            .SetDisplay("Use Day Of Week", "Use weekday rather than N bars", "Calendar")
        self._day_of_week = self.Param("DayOfWeek", DayOfWeek.Wednesday) \
            .SetDisplay("Day Of Week", "Selected purchase weekday", "Calendar")
        self._every_x_candles = self.Param("EveryXCandles", 72) \
            .SetGreaterThanZero() \
            .SetDisplay("Every X Candles", "Period of purchases in completed candles", "Calendar")

        self._eligible_bars = 0
        self._last_weekday_purchase = None
        self._end_reached = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dca_strategy, self).OnReseted()
        self._eligible_bars = 0
        self._last_weekday_purchase = None
        self._end_reached = False

    def OnStarted2(self, time):
        super(dca_strategy, self).OnStarted2(time)

        if self._start_date.Value.Date > self._end_date.Value.Date:
            raise ValueError("StartDate must not be after EndDate")

        self._eligible_bars = 0
        self._last_weekday_purchase = None
        self._end_reached = False

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date
        if date < self._start_date.Value.Date:
            return

        if date >= self._end_date.Value.Date:
            if not self._end_reached and bool(self._close_all_on_last_candle.Value) and self.Position > 0 and self.IsFormedAndOnlineAndAllowTrading():
                self.SellMarket(self.Position)
                self._end_reached = True
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        weekday_mode = bool(self._use_day_of_week.Value)
        if weekday_mode:
            if candle.OpenTime.DayOfWeek != self._day_of_week.Value or self._last_weekday_purchase == date:
                return
        else:
            self._eligible_bars += 1
            if self._eligible_bars % int(self._every_x_candles.Value) != 0:
                return

        price = float(candle.ClosePrice)
        if price <= 0 or self.Security is None:
            return

        step = float(self.Security.VolumeStep)
        if step <= 0:
            step = 0.001
        max_volume = float(self.Security.MaxVolume)
        if max_volume <= 0:
            max_volume = float("inf")

        # Never round a quote-valued investment UP above the budget.
        volume = math.floor(min(float(self._capital_to_invest.Value) / price, max_volume) / step) * step
        min_volume = float(self.Security.MinVolume)
        if volume <= 0 or (min_volume > 0 and volume < min_volume):
            return

        self.BuyMarket(Decimal.Parse(str(volume)))
        if weekday_mode:
            self._last_weekday_purchase = date

    def CreateClone(self):
        return dca_strategy()
