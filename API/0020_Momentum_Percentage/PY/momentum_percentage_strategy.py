import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class momentum_percentage_strategy(Strategy):
    """
    Momentum Percentage: buys when momentum crosses above zero with price above SMA.
    """

    def __init__(self):
        super(momentum_percentage_strategy, self).__init__()
        self._momentum_period = self.Param("MomentumPeriod", 10).SetDisplay("Momentum Period", "Momentum period", "Indicators")
        self._sma_period = self.Param("SmaPeriod", 20).SetDisplay("SMA Period", "SMA trend filter period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momentum_percentage_strategy, self).OnReseted()
        self._prev_mom = 0.0
        self._has_prev = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(momentum_percentage_strategy, self).OnStarted(time)
        mom = Momentum()
        mom.Length = self._momentum_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mom, sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, mom)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, mom_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        mom = float(mom_val)
        sma = float(sma_val)
        if sma == 0:
            return
        if not self._has_prev:
            self._has_prev = True
            self._prev_mom = mom
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_mom = mom
            return
        price = float(candle.ClosePrice)
        if self._prev_mom <= 0 and mom > 0 and price > sma and self.Position <= 0:
            self.BuyMarket()
            self._cooldown = 30
        elif self._prev_mom >= 0 and mom < 0 and price < sma and self.Position >= 0:
            self.SellMarket()
            self._cooldown = 30
        self._prev_mom = mom

    def CreateClone(self):
        return momentum_percentage_strategy()
