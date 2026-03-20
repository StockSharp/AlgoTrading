import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class end_of_month_strength_strategy(Strategy):
    """
    End of Month Strength trading strategy.
    Buys on the last week of the month, exits on the first week of the next month.
    Also sells short in mid-month if price below MA.
    """

    def __init__(self):
        super(end_of_month_strength_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 50).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._cooldown = 0
        self._prev_day_of_month = 0
        self._prev_month = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(end_of_month_strength_strategy, self).OnReseted()
        self._cooldown = 0
        self._prev_day_of_month = 0
        self._prev_month = 0

    def OnStarted(self, time):
        super(end_of_month_strength_strategy, self).OnStarted(time)

        self._cooldown = 0
        self._prev_day_of_month = 0
        self._prev_month = 0

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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        day_of_month = candle.OpenTime.Day
        month = candle.OpenTime.Month
        cd = self._cooldown_bars.Value

        is_new_day = day_of_month != self._prev_day_of_month

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_day_of_month = day_of_month
            self._prev_month = month
            return

        is_end_of_month = day_of_month >= 24
        is_begin_of_month = day_of_month <= 5
        is_mid_month = day_of_month >= 10 and day_of_month <= 20

        # Entry: buy at end of month if flat
        if is_end_of_month and is_new_day and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Exit: sell at beginning of next month
        elif is_begin_of_month and is_new_day and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        # Short in mid-month if below MA
        elif is_mid_month and is_new_day and self.Position == 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd
        # Cover short at end of month
        elif is_end_of_month and is_new_day and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_day_of_month = day_of_month
        self._prev_month = month

    def CreateClone(self):
        return end_of_month_strength_strategy()
