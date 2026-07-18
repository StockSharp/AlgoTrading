import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class specific_day_time_strategy(Strategy):
    def __init__(self):
        super(specific_day_time_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30)))
        self._fast_period = self.Param("FastPeriod", 8)
        self._slow_period = self.Param("SlowPeriod", 21)
        self._trade_hour = self.Param("TradeHour", 12)
        self._cooldown_bars = self.Param("CooldownBars", 8)

        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def TradeHour(self):
        return self._trade_hour.Value

    @TradeHour.setter
    def TradeHour(self, value):
        self._trade_hour.Value = value

    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    @CooldownBars.setter
    def CooldownBars(self, value):
        self._cooldown_bars.Value = value

    def OnReseted(self):
        super(specific_day_time_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted2(self, time):
        super(specific_day_time_strategy, self).OnStarted2(time)
        self._cooldown = 0

        fast = SimpleMovingAverage()
        fast.Length = self.FastPeriod
        slow = SimpleMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1

        bullish_trend = float(fast_value) > float(slow_value)
        bearish_trend = float(fast_value) < float(slow_value)

        if self._cooldown == 0 and candle.OpenTime.Hour == self.TradeHour:
            if bullish_trend and self.Position <= 0:
                self.BuyMarket()
                self._cooldown = self.CooldownBars
            elif bearish_trend and self.Position >= 0:
                self.SellMarket()
                self._cooldown = self.CooldownBars
        elif self._cooldown == 0:
            if self.Position > 0 and bearish_trend:
                self.SellMarket()
                self._cooldown = self.CooldownBars
            elif self.Position < 0 and bullish_trend:
                self.BuyMarket()
                self._cooldown = self.CooldownBars

    def CreateClone(self):
        return specific_day_time_strategy()
