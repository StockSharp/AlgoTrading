import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergence, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class tester_v014_strategy(Strategy):
    def __init__(self):
        super(tester_v014_strategy, self).__init__()
        self._bars_number = self.Param("BarsNumber", 3) \
            .SetDisplay("Bars Number", "Holding period in bars", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._bars_counter = 0
        self._position_opened = False

    @property
    def bars_number(self):
        return self._bars_number.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tester_v014_strategy, self).OnReseted()
        self._bars_counter = 0
        self._position_opened = False

    def OnStarted2(self, time):
        super(tester_v014_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = 14
        macd = MovingAverageConvergenceDivergence()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, macd, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val, macd_val):
        if candle.State != CandleStates.Finished:
            return
        # Close after specified bars
        if self._position_opened:
            self._bars_counter += 1
            if self._bars_counter >= self.bars_number:
                if self.Position > 0:
                    self.SellMarket()
                elif self.Position < 0:
                    self.BuyMarket()
                self._position_opened = False
                self._bars_counter = 0
        # Entry
        if self.Position == 0 and not self._position_opened:
            if candle.ClosePrice > sma_val and macd_val > 0:
                self.BuyMarket()
                self._bars_counter = 0
                self._position_opened = True
            elif candle.ClosePrice < sma_val and macd_val < 0:
                self.SellMarket()
                self._bars_counter = 0
                self._position_opened = True

    def CreateClone(self):
        return tester_v014_strategy()
