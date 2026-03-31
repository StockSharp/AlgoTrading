import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ima_expert_strategy(Strategy):
    def __init__(self):
        super(ima_expert_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 5).SetDisplay("SMA Period", "Length of moving average", "Parameters")
        self._signal_level = self.Param("SignalLevel", 0.5).SetDisplay("Signal Level", "IMA change threshold", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._previous_ima = None
    @property
    def sma_period(self): return self._sma_period.Value
    @property
    def signal_level(self): return self._signal_level.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(ima_expert_strategy, self).OnReseted()
        self._previous_ima = None
    def OnStarted2(self, time):
        super(ima_expert_strategy, self).OnStarted2(time)
        sma = ExponentialMovingAverage()
        sma.Length = self.sma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
    def process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished: return
        sv = float(sma_value)
        if sv == 0.0: return
        price = float(candle.ClosePrice)
        ima = price / sv - 1.0
        if self._previous_ima is None or self._previous_ima == 0.0:
            self._previous_ima = ima
            return
        k1 = (ima - self._previous_ima) / abs(self._previous_ima)
        self._previous_ima = ima
        if not self.IsFormedAndOnlineAndAllowTrading(): return
        sl = float(self.signal_level)
        if self.Position == 0:
            if k1 >= sl: self.BuyMarket()
            elif k1 <= -sl: self.SellMarket()
        elif self.Position > 0 and k1 <= -sl:
            self.SellMarket()
        elif self.Position < 0 and k1 >= sl:
            self.BuyMarket()
    def CreateClone(self): return ima_expert_strategy()
