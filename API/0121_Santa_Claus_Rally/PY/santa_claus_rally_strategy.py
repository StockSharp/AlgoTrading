import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class santa_claus_rally_strategy(Strategy):
    """
    Santa Claus Rally trading strategy.
    Buys at end of each month and exits early next month.
    Short mid-month if below MA.
    """

    def __init__(self):
        super(santa_claus_rally_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 50).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._cooldown = 0
        self._prev_day_of_month = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(santa_claus_rally_strategy, self).OnReseted()
        self._cooldown = 0
        self._prev_day_of_month = 0

    def OnStarted(self, time):
        super(santa_claus_rally_strategy, self).OnStarted(time)

        self._cooldown = 0
        self._prev_day_of_month = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        day_of_month = candle.OpenTime.Day
        cd = self._cooldown_bars.Value
        is_new_day = day_of_month != self._prev_day_of_month

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_day_of_month = day_of_month
            return

        is_rally_zone = day_of_month >= 25
        is_exit_zone = day_of_month >= 3 and day_of_month <= 7
        is_short_zone = day_of_month >= 12 and day_of_month <= 18

        # Buy at end of month for rally
        if is_rally_zone and is_new_day and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Exit long in early next month
        elif is_exit_zone and is_new_day and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        # Short mid-month if below MA
        elif is_short_zone and is_new_day and self.Position == 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd
        # Cover short before rally zone
        elif is_rally_zone and is_new_day and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_day_of_month = day_of_month

    def CreateClone(self):
        return santa_claus_rally_strategy()
