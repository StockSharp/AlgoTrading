import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy

class exp_multic_strategy(Strategy):
    def __init__(self):
        super(exp_multic_strategy, self).__init__()
        self._period = self.Param("Period", 14).SetDisplay("Period", "Momentum lookback period", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Timeframe", "General")
        self._prev_momentum = None
    @property
    def period(self): return self._period.Value
    @property
    def candle_type(self): return self._candle_type.Value
    def OnReseted(self):
        super(exp_multic_strategy, self).OnReseted()
        self._prev_momentum = None
    def OnStarted2(self, time):
        super(exp_multic_strategy, self).OnStarted2(time)
        momentum = Momentum()
        momentum.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(momentum, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            area2 = self.CreateChartArea()
            if area2 is not None:
                self.DrawIndicator(area2, momentum)
    def process_candle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished: return
        mv = float(momentum_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_momentum = mv
            return
        if self._prev_momentum is not None:
            if self._prev_momentum <= 0.0 and mv > 0.0 and self.Position <= 0:
                self.BuyMarket()
            elif self._prev_momentum >= 0.0 and mv < 0.0 and self.Position >= 0:
                self.SellMarket()
        self._prev_momentum = mv
    def CreateClone(self): return exp_multic_strategy()
