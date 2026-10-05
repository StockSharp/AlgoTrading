import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class tweezer_bottom_strategy(Strategy):
    """
    Tweezer Bottom strategy.
    While flat it buys after a bearish candle followed by a bullish one whose low is within TolerancePercent of the first low.
    The stop lies StopLossPercent below the pattern's lowest low, and a close beyond it closes the position.
    """

    def __init__(self):
        super(tweezer_bottom_strategy, self).__init__()
        self._tolerance_percent = self.Param("TolerancePercent", 0.1).SetNotNegative().SetDisplay("Tolerance %", "How close the two extremes must be, in percent of the first", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Distance of the stop beyond the pattern, in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._candles = []
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(tweezer_bottom_strategy, self).OnReseted()
        self._candles = []
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(tweezer_bottom_strategy, self).OnStarted2(time)

        self._candles = []
        self._stop_price = Decimal(0)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._candles.append(candle)
        if len(self._candles) > 2:
            self._candles.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position > 0:
            if candle.ClosePrice <= self._stop_price:
                self.SellMarket(self.Position)
            return

        if self.Position != 0 or len(self._candles) < 2:
            return

        c0, c1 = self._candles

        if not (c0.ClosePrice < c0.OpenPrice and c1.ClosePrice > c1.OpenPrice and Math.Abs(c0.LowPrice - c1.LowPrice) <= c0.LowPrice * Decimal(self._tolerance_percent.Value) / Decimal(100)):
            return

        self.BuyMarket(self.Volume)
        self._stop_price = min(c0.LowPrice, c1.LowPrice) * (Decimal(1) - Decimal(self._stop_loss_percent.Value) / Decimal(100))

    def CreateClone(self):
        return tweezer_bottom_strategy()
