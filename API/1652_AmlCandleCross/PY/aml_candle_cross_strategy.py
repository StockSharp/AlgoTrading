import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class aml_candle_cross_strategy(Strategy):
    """Adaptive Market Level candle cross strategy.
    Uses EMA as proxy for the custom AdaptiveMarketLevel indicator.
    Opens position when indicator value lies between candle open and close.
    """
    def __init__(self):
        super(aml_candle_cross_strategy, self).__init__()
        self._fractal = self.Param("Fractal", 10) \
            .SetDisplay("Fractal", "Fractal window size", "General")
        self._lag = self.Param("Lag", 5) \
            .SetDisplay("Lag", "Lag for smoothing", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle Type", "General")

    @property
    def fractal(self):
        return self._fractal.Value

    @property
    def lag(self):
        return self._lag.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(aml_candle_cross_strategy, self).OnStarted(time)
        # Use EMA as proxy for custom AdaptiveMarketLevel indicator
        aml = ExponentialMovingAverage()
        aml.Length = self.fractal
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(aml, self.on_process).Start()
        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, aml)
            self.DrawOwnTrades(area)

    def on_process(self, candle, aml_value):
        if candle.State != CandleStates.Finished:
            return
        if aml_value == 0:
            return
        opn = candle.OpenPrice
        close = candle.ClosePrice
        # Bullish: AML between open and close, bullish candle
        bullish = close > opn and aml_value >= opn and aml_value <= close
        # Bearish: AML between close and open, bearish candle
        bearish = close < opn and aml_value >= close and aml_value <= opn
        if self.Position == 0:
            if bullish:
                self.BuyMarket()
            elif bearish:
                self.SellMarket()

    def CreateClone(self):
        return aml_candle_cross_strategy()
