import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, DayOfWeek
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class day_of_week_strategy(Strategy):
    """
    Day of Week trading strategy.
    Enters long on Monday/Tuesday and short on Thursday/Friday, with MA trend filter.
    Uses daily transitions to limit trade frequency.
    """

    def __init__(self):
        super(day_of_week_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 300).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._last_trade_day = -1
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(day_of_week_strategy, self).OnReseted()
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._last_trade_day = -1
        self._cooldown = 0

    def OnStarted(self, time):
        super(day_of_week_strategy, self).OnStarted(time)

        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._last_trade_day = -1
        self._cooldown = 0

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
        day = candle.OpenTime.DayOfWeek
        cd = self._cooldown_bars.Value

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ma = ma
            self._prev_close = close
            return

        # Exit logic: MA cross
        if self.Position > 0 and close < ma and self._prev_ma > 0 and self._prev_close >= self._prev_ma:
            self.SellMarket()
            self._cooldown = cd
            self._last_trade_day = day
        elif self.Position < 0 and close > ma and self._prev_ma > 0 and self._prev_close <= self._prev_ma:
            self.BuyMarket()
            self._cooldown = cd
            self._last_trade_day = day

        # Entry logic: day-of-week based (one trade per day transition)
        if self.Position == 0 and day != self._last_trade_day:
            # Monday/Tuesday: buy if above MA
            if (day == DayOfWeek.Monday or day == DayOfWeek.Tuesday) and close > ma:
                self.BuyMarket()
                self._cooldown = cd
                self._last_trade_day = day
            # Thursday/Friday: sell if below MA
            elif (day == DayOfWeek.Thursday or day == DayOfWeek.Friday) and close < ma:
                self.SellMarket()
                self._cooldown = cd
                self._last_trade_day = day

        self._prev_ma = ma
        self._prev_close = close

    def CreateClone(self):
        return day_of_week_strategy()
