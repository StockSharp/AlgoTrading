import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class zigzag_candles_strategy(Strategy):
    def __init__(self):
        super(zigzag_candles_strategy, self).__init__()
        self._zigzag_length = self.Param("ZigzagLength", 5) \
            .SetDisplay("ZigZag Length", "Lookback for pivot search", "ZigZag")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._last_zigzag = 0.0
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._direction = 0
        self._signal_fired = False

    @property
    def zigzag_length(self):
        return self._zigzag_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zigzag_candles_strategy, self).OnReseted()
        self._last_zigzag = 0.0
        self._last_zigzag_high = 0.0
        self._last_zigzag_low = 0.0
        self._direction = 0
        self._signal_fired = False

    def OnStarted2(self, time):
        super(zigzag_candles_strategy, self).OnStarted2(time)
        highest = Highest()
        highest.Length = self.zigzag_length
        lowest = Lowest()
        lowest.Length = self.zigzag_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, highest, lowest):
        if candle.State != CandleStates.Finished:
            return
        prev_direction = self._direction
        if candle.HighPrice >= highest and self._direction != 1:
            self._last_zigzag = candle.HighPrice
            self._last_zigzag_high = candle.HighPrice
            self._direction = 1
            self._signal_fired = False
        elif candle.LowPrice <= lowest and self._direction != -1:
            self._last_zigzag = candle.LowPrice
            self._last_zigzag_low = candle.LowPrice
            self._direction = -1
            self._signal_fired = False
        if self._signal_fired:
            return
        if self._direction == -1 and prev_direction != -1 and self.Position <= 0:
            self.BuyMarket()
            self._signal_fired = True
        elif self._direction == 1 and prev_direction != 1 and self.Position >= 0:
            self.SellMarket()
            self._signal_fired = True

    def CreateClone(self):
        return zigzag_candles_strategy()
