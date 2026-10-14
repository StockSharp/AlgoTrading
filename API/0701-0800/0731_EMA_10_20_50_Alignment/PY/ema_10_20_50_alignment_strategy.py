import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, DateTimeOffset
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ema_10_20_50_alignment_strategy(Strategy):
    """
    EMA 10/20/50 alignment strategy.
    Long only: buys when EMA(10) is above EMA(20) and EMA(20) is above EMA(50) and closes the long when the three EMAs align
    in descending order. Entries are allowed only for candles between StartTime and EndTime.
    """

    def __init__(self):
        super(ema_10_20_50_alignment_strategy, self).__init__()
        self._start_time = self.Param("StartTime", DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero)).SetDisplay("Start Time", "Start of the trading date range", "General")
        self._end_time = self.Param("EndTime", DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero)).SetDisplay("End Time", "End of the trading date range", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ema_10_20_50_alignment_strategy, self).OnStarted2(time)

        ema10 = ExponentialMovingAverage()
        ema10.Length = 10
        ema20 = ExponentialMovingAverage()
        ema20.Length = 20
        ema50 = ExponentialMovingAverage()
        ema50.Length = 50

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema10, ema20, ema50, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema10)
            self.DrawIndicator(area, ema20)
            self.DrawIndicator(area, ema50)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema10_value, ema20_value, ema50_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema10 = float(ema10_value)
        ema20 = float(ema20_value)
        ema50 = float(ema50_value)

        open_ticks = candle.OpenTime.Ticks
        in_range = self._start_time.Value.UtcDateTime.Ticks <= open_ticks <= self._end_time.Value.UtcDateTime.Ticks

        if self.Position <= 0 and in_range and ema10 > ema20 and ema20 > ema50:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and ema10 < ema20 and ema20 < ema50:
            self.SellMarket(self.Position)

    def CreateClone(self):
        return ema_10_20_50_alignment_strategy()
