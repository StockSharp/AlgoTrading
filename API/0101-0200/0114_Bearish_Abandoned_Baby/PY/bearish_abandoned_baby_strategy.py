import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class bearish_abandoned_baby_strategy(Strategy):
    """
    Bearish Abandoned Baby strategy.
    While flat it sells after a bullish candle, a doji whose body gaps above the first body, and a bearish candle whose body gaps below the doji.
    The stop lies StopLossPercent above the doji's high, and a close beyond it closes the position.
    """

    def __init__(self):
        super(bearish_abandoned_baby_strategy, self).__init__()
        self._doji_body_percent = self.Param("DojiBodyPercent", 10.0).SetNotNegative().SetDisplay("Doji Body %", "Largest body of the doji, in percent of its range", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Distance of the stop beyond the pattern, in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._candles = []
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bearish_abandoned_baby_strategy, self).OnReseted()
        self._candles = []
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(bearish_abandoned_baby_strategy, self).OnStarted2(time)

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
        if len(self._candles) > 3:
            self._candles.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self.Position < 0:
            if candle.ClosePrice >= self._stop_price:
                self.BuyMarket(-self.Position)
            return

        if self.Position != 0 or len(self._candles) < 3:
            return

        c0, c1, c2 = self._candles

        if not (c0.ClosePrice > c0.OpenPrice and Math.Abs(c1.ClosePrice - c1.OpenPrice) <= (c1.HighPrice - c1.LowPrice) * Decimal(self._doji_body_percent.Value) / Decimal(100) and Math.Min(c1.OpenPrice, c1.ClosePrice) > Math.Max(c0.OpenPrice, c0.ClosePrice) and c2.ClosePrice < c2.OpenPrice and Math.Max(c2.OpenPrice, c2.ClosePrice) < Math.Min(c1.OpenPrice, c1.ClosePrice)):
            return

        self.SellMarket(self.Volume)
        self._stop_price = c1.HighPrice * (Decimal(1) + Decimal(self._stop_loss_percent.Value) / Decimal(100))

    def CreateClone(self):
        return bearish_abandoned_baby_strategy()
