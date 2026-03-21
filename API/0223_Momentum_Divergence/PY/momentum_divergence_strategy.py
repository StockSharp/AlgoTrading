import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Momentum, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class momentum_divergence_strategy(Strategy):
    """
    Momentum Divergence: trades based on divergence between price and momentum.
    """

    def __init__(self):
        super(momentum_divergence_strategy, self).__init__()
        self._momentum_period = self.Param("MomentumPeriod", 14).SetDisplay("Momentum Period", "Period for Momentum", "Parameters")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "Common")

        self._prev_price = 0.0
        self._prev_momentum = 0.0
        self._current_price = 0.0
        self._current_momentum = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momentum_divergence_strategy, self).OnReseted()
        self._prev_price = 0.0
        self._prev_momentum = 0.0
        self._current_price = 0.0
        self._current_momentum = 0.0

    def OnStarted(self, time):
        super(momentum_divergence_strategy, self).OnStarted(time)
        mom = Momentum()
        mom.Length = self._momentum_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(mom, sma, self._process_candle).Start()
        self.StartProtection(None, Unit(2, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, mom)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, mom_val, sma_val):
        if candle.State != CandleStates.Finished:
            return
        self._prev_price = self._current_price
        self._prev_momentum = self._current_momentum
        self._current_price = float(candle.ClosePrice)
        self._current_momentum = float(mom_val)
        if self._prev_price == 0 or self._prev_momentum == 0:
            return
        bullish_div = self._current_price < self._prev_price and self._current_momentum > self._prev_momentum
        bearish_div = self._current_price > self._prev_price and self._current_momentum < self._prev_momentum
        sma = float(sma_val)
        if bullish_div and self.Position <= 0:
            self.BuyMarket()
        elif bearish_div and self.Position >= 0:
            self.SellMarket()
        elif self.Position > 0 and float(candle.ClosePrice) < sma:
            self.SellMarket()
        elif self.Position < 0 and float(candle.ClosePrice) > sma:
            self.BuyMarket()

    def CreateClone(self):
        return momentum_divergence_strategy()
