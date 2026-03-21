import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class quarterly_expiry_strategy(Strategy):
    """
    Quarterly Expiry trading strategy.
    Trades around monthly expiry dates (3rd Friday area of each month).
    Buys if above MA in expiry week, sells if below. Exits next week.
    """

    def __init__(self):
        super(quarterly_expiry_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Moving average period for trend confirmation", "Strategy")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles for strategy", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 50).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._cooldown = 0
        self._prev_day_of_month = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(quarterly_expiry_strategy, self).OnReseted()
        self._cooldown = 0
        self._prev_day_of_month = 0

    def OnStarted(self, time):
        super(quarterly_expiry_strategy, self).OnStarted(time)

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

        # Expiry week zone: day 15-19 (around 3rd Friday of each month)
        is_expiry_week = day_of_month >= 15 and day_of_month <= 19
        # Post-expiry exit zone: day 22-25
        is_post_expiry = day_of_month >= 22 and day_of_month <= 25
        # Start of month entry zone: day 1-5
        is_start_of_month = day_of_month >= 1 and day_of_month <= 5
        # Pre-expiry exit: day 12-14
        is_pre_expiry = day_of_month >= 12 and day_of_month <= 14

        # Entry in expiry week: buy if above MA
        if is_expiry_week and is_new_day and self.Position == 0 and close > ma:
            self.BuyMarket()
            self._cooldown = cd
        # Exit after expiry week
        elif is_post_expiry and is_new_day and self.Position > 0:
            self.SellMarket()
            self._cooldown = cd
        # Short entry at start of month if below MA
        elif is_start_of_month and is_new_day and self.Position == 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd
        # Cover short before expiry
        elif is_pre_expiry and is_new_day and self.Position < 0:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_day_of_month = day_of_month

    def CreateClone(self):
        return quarterly_expiry_strategy()
