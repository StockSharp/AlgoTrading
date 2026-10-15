import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class custom_buy_bid_strategy(Strategy):
    """
    Custom Buy BID strategy.
    Goes long when the close crosses above the Supertrend line inside the StartDate..EndDate window.
    The position is closed only by the percent take profit or stop loss.
    """

    def __init__(self):
        super(custom_buy_bid_strategy, self).__init__()
        self._supertrend_period = self.Param("SupertrendPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "ATR period of Supertrend", "Indicators")
        self._supertrend_multiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Indicators")
        self._take_profit_percent = self.Param("TakeProfitPercent", 5.0) \
            .SetNotNegative() \
            .SetDisplay("Take Profit %", "Take profit percentage from entry price", "Risk")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._start_date = self.Param("StartDate", DateTimeOffset(2018, 9, 1, 0, 0, 0, TimeSpan.Zero)) \
            .SetDisplay("Start Date", "First date when entries are allowed", "General")
        self._end_date = self.Param("EndDate", DateTimeOffset(9999, 1, 1, 0, 0, 0, TimeSpan.Zero)) \
            .SetDisplay("End Date", "Date after which no entries are made", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._prev_close = None
        self._prev_supertrend = None

    def OnReseted(self):
        super(custom_buy_bid_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(custom_buy_bid_strategy, self).OnStarted2(time)

        self._reset_state()

        supertrend = SuperTrend()
        supertrend.Length = self._supertrend_period.Value
        supertrend.Multiplier = Decimal(self._supertrend_multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, self._process_candle).Start()

        self.StartProtection(
            Unit(Decimal(self._take_profit_percent.Value), UnitTypes.Percent),
            Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, supertrend_value):
        if candle.State != CandleStates.Finished:
            return

        if not supertrend_value.IsFormed:
            return

        line = supertrend_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        crossed_up = (self._prev_close is not None and self._prev_supertrend is not None
                      and self._prev_close <= self._prev_supertrend and close > line)

        self._prev_close = close
        self._prev_supertrend = line

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        open_time = candle.OpenTime
        in_window = open_time >= self._start_date.Value.UtcDateTime and open_time <= self._end_date.Value.UtcDateTime

        if crossed_up and in_window and self.Position == 0:
            self.BuyMarket()

    def CreateClone(self):
        return custom_buy_bid_strategy()
