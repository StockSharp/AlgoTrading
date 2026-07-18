import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class pending_order_strategy(Strategy):
    def __init__(self):
        super(pending_order_strategy, self).__init__()
        self._distance = self.Param("Distance", 50.0).SetDisplay("Distance", "Offset from prev candle range for entry", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candle type", "General")
        self._prev_high = None
        self._prev_low = None
    @property
    def distance(self): return self._distance.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(pending_order_strategy, self).OnReseted()
        self._prev_high = None
        self._prev_low = None
    def OnStarted2(self, time):
        super(pending_order_strategy, self).OnStarted2(time)
        sma = ExponentialMovingAverage()
        sma.Length = 5
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished: return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_high = float(candle.HighPrice)
            self._prev_low = float(candle.LowPrice)
            return
        if self._prev_high is not None and self._prev_low is not None:
            dist = float(self.distance)
            break_up = self._prev_high + dist
            break_down = self._prev_low - dist
            close = float(candle.ClosePrice)
            if close > break_up and self.Position <= 0:
                self.BuyMarket()
            elif close < break_down and self.Position >= 0:
                self.SellMarket()
        self._prev_high = float(candle.HighPrice)
        self._prev_low = float(candle.LowPrice)
    def CreateClone(self): return pending_order_strategy()
