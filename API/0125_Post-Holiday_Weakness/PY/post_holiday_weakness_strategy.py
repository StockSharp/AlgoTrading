import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class post_holiday_weakness_strategy(Strategy):
    """
    Post-Holiday Weakness trading strategy.
    Sells short on Monday (post-weekend weakness) if below MA, covers Wednesday.
    Buys on Wednesday if above MA, exits Friday.
    """

    def __init__(self):
        super(post_holiday_weakness_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Moving average period", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 30).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._cooldown = 0
        self._prev_day_of_week = DayOfWeek.Sunday
        self._entered_this_day = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(post_holiday_weakness_strategy, self).OnReseted()
        self._cooldown = 0
        self._prev_day_of_week = DayOfWeek.Sunday
        self._entered_this_day = False

    def OnStarted2(self, time):
        super(post_holiday_weakness_strategy, self).OnStarted2(time)

        self._cooldown = 0
        self._prev_day_of_week = DayOfWeek.Sunday
        self._entered_this_day = False

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
        day_of_week = candle.OpenTime.DayOfWeek
        cd = self._cooldown_bars.Value

        if day_of_week != self._prev_day_of_week:
            self._entered_this_day = False

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_day_of_week = day_of_week
            return

        # Monday: post-weekend weakness - short if below MA
        if day_of_week == DayOfWeek.Monday and not self._entered_this_day and self.Position == 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd
            self._entered_this_day = True
        # Wednesday: cover short
        elif day_of_week == DayOfWeek.Wednesday and self.Position < 0 and not self._entered_this_day:
            self.BuyMarket()
            self._cooldown = cd
            self._entered_this_day = True
        # Wednesday: buy if above MA
        elif day_of_week == DayOfWeek.Wednesday and not self._entered_this_day and self.Position == 0 and close > ma:
            self.BuyMarket()
            self._cooldown = cd
            self._entered_this_day = True
        # Friday: exit long
        elif day_of_week == DayOfWeek.Friday and self.Position > 0 and not self._entered_this_day:
            self.SellMarket()
            self._cooldown = cd
            self._entered_this_day = True

        self._prev_day_of_week = day_of_week

    def CreateClone(self):
        return post_holiday_weakness_strategy()
