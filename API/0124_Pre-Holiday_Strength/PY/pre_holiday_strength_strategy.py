import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class pre_holiday_strength_strategy(Strategy):
    """
    Pre-Holiday Strength trading strategy.
    Buys on Thursday (pre-weekend strength effect) if above MA, exits Monday.
    Shorts on Tuesday if below MA, covers Wednesday.
    """

    def __init__(self):
        super(pre_holiday_strength_strategy, self).__init__()
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
        super(pre_holiday_strength_strategy, self).OnReseted()
        self._cooldown = 0
        self._prev_day_of_week = DayOfWeek.Sunday
        self._entered_this_day = False

    def OnStarted(self, time):
        super(pre_holiday_strength_strategy, self).OnStarted(time)

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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        day_of_week = candle.OpenTime.DayOfWeek
        cd = self._cooldown_bars.Value

        # Reset entry flag on new day
        if day_of_week != self._prev_day_of_week:
            self._entered_this_day = False

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_day_of_week = day_of_week
            return

        # Pre-weekend buy: Thursday if above MA
        if day_of_week == DayOfWeek.Thursday and not self._entered_this_day and self.Position == 0 and close > ma:
            self.BuyMarket()
            self._cooldown = cd
            self._entered_this_day = True
        # Exit on Monday
        elif day_of_week == DayOfWeek.Monday and self.Position > 0 and not self._entered_this_day:
            self.SellMarket()
            self._cooldown = cd
            self._entered_this_day = True
        # Short on Tuesday if below MA
        elif day_of_week == DayOfWeek.Tuesday and not self._entered_this_day and self.Position == 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd
            self._entered_this_day = True
        # Cover short on Wednesday
        elif day_of_week == DayOfWeek.Wednesday and self.Position < 0 and not self._entered_this_day:
            self.BuyMarket()
            self._cooldown = cd
            self._entered_this_day = True

        self._prev_day_of_week = day_of_week

    def CreateClone(self):
        return pre_holiday_strength_strategy()
