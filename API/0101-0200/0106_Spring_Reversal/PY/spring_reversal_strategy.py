import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class spring_reversal_strategy(Strategy):
    """
    Spring Reversal strategy.
    Support is the lowest low of the previous LookbackPeriod candles. A bullish candle that breaks below support and closes back above it
    is a spring and buys while flat. The stop lies StopLossPercent below the spring low, and a close below it closes the position.
    """

    def __init__(self):
        super(spring_reversal_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("LookbackPeriod", "Number of previous candles that form the level", "Pattern")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Distance of the stop beyond the level, in percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._candles = []
        self._stop_price = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(spring_reversal_strategy, self).OnReseted()
        self._candles = []
        self._stop_price = Decimal(0)

    def OnStarted2(self, time):
        super(spring_reversal_strategy, self).OnStarted2(time)

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

        # The level is formed by the candles before this one.
        period = self._lookback_period.Value
        ready = len(self._candles) == period
        high = max(c[0] for c in self._candles) if ready else Decimal(0)
        low = min(c[1] for c in self._candles) if ready else Decimal(0)

        self._candles.append((candle.HighPrice, candle.LowPrice))
        if len(self._candles) > period:
            self._candles.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice

        if self.Position > 0:
            if close <= self._stop_price:
                self.SellMarket(self.Position)
            return

        if self.Position != 0 or not ready or not (candle.LowPrice < low and close > low and close > candle.OpenPrice):
            return

        percent = Decimal(self._stop_loss_percent.Value) / Decimal(100)
        self.BuyMarket(self.Volume)
        self._stop_price = candle.LowPrice * (Decimal(1) - percent)

    def CreateClone(self):
        return spring_reversal_strategy()
