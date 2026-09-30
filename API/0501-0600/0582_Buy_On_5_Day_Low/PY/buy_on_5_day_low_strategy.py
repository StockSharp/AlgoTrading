import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTimeOffset, TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class buy_on_5_day_low_strategy(Strategy):
    """Buys below the previous N-bar low and exits above the previous bar's high."""

    def __init__(self):
        super(buy_on_5_day_low_strategy, self).__init__()
        self._lowest_period = self.Param("LowestPeriod", 5) \
            .SetDisplay("Lowest Period", "Number of previous candles in the low window", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._start_time = self.Param("StartTime", DateTimeOffset(2014, 1, 1, 0, 0, 0, TimeSpan.Zero)) \
            .SetDisplay("Start Time", "Beginning of the trading window", "General")
        self._end_time = self.Param("EndTime", DateTimeOffset(2099, 1, 1, 0, 0, 0, TimeSpan.Zero)) \
            .SetDisplay("End Time", "End of the trading window", "General")
        self._lows = []
        self._previous_high = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(buy_on_5_day_low_strategy, self).OnReseted()
        self._lows = []
        self._previous_high = None

    def OnStarted2(self, time):
        super(buy_on_5_day_low_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        period = int(self._lowest_period.Value)

        # Evaluate before adding this candle: its own low cannot be the entry threshold.
        if (len(self._lows) == period and self.IsFormedAndOnlineAndAllowTrading()
                and self._start_time.Value.UtcDateTime <= candle.OpenTime <= self._end_time.Value.UtcDateTime):
            if self.Position == 0 and candle.ClosePrice < min(self._lows):
                self.BuyMarket()
            elif self.Position > 0 and candle.ClosePrice > self._previous_high:
                self.SellMarket(self.Position)

        self._lows.append(candle.LowPrice)
        while len(self._lows) > period:
            self._lows.pop(0)
        self._previous_high = candle.HighPrice

    def CreateClone(self):
        return buy_on_5_day_low_strategy()
