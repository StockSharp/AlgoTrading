import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy

class supertrend_strategy(Strategy):
    """
    Strategy based on Supertrend indicator.
    It turns long when the Supertrend line flips below price and short when it flips above, reversing on every flip.
    """

    def __init__(self):
        super(supertrend_strategy, self).__init__()
        self._period = self.Param("Period", 10).SetGreaterThanZero().SetDisplay("Period", "Period for Supertrend calculation", "Indicators")
        self._multiplier = self.Param("Multiplier", 3.0).SetGreaterThanZero().SetDisplay("Multiplier", "Multiplier for Supertrend calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

        # Trend of the previous candle; unknown until the indicator forms.
        self._prev_is_up_trend = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(supertrend_strategy, self).OnReseted()
        self._prev_is_up_trend = None

    def OnStarted2(self, time):
        super(supertrend_strategy, self).OnStarted2(time)

        supertrend = SuperTrend()
        supertrend.Length = self._period.Value
        supertrend.Multiplier = Decimal(self._multiplier.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(supertrend, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, supertrend_value):
        if candle.State != CandleStates.Finished or not supertrend_value.IsFormed:
            return

        is_up_trend = supertrend_value.IsUpTrend
        was_up_trend = self._prev_is_up_trend
        self._prev_is_up_trend = is_up_trend

        if was_up_trend is None or was_up_trend == is_up_trend:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if is_up_trend and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif not is_up_trend and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))

    def CreateClone(self):
        return supertrend_strategy()
