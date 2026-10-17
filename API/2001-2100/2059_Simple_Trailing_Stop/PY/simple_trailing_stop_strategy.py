import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class simple_trailing_stop_strategy(Strategy):
    """One long entry after first finished candle; engine-native trailing protection."""

    def __init__(self):
        super(simple_trailing_stop_strategy, self).__init__()
        self._trail_points = self.Param("TrailPoints", 100.0).SetGreaterThanZero() \
            .SetDisplay("Trail Points", "Trailing distance in security price steps", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._submitted_entry = False
        self._entry_order = None

    @property
    def TrailPoints(self):
        return self._trail_points.Value

    @TrailPoints.setter
    def TrailPoints(self, value):
        self._trail_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(simple_trailing_stop_strategy, self).OnReseted()
        self._submitted_entry = False
        self._entry_order = None

    def OnStarted2(self, time):
        super(simple_trailing_stop_strategy, self).OnStarted2(time)
        self._submitted_entry = False
        self._entry_order = None
        step = self.Security.PriceStep
        if step is None or step <= 0:
            step = 1
        self.StartProtection(
            takeProfit=None,
            stopLoss=Unit(self.TrailPoints * step, UnitTypes.Absolute),
            isStopTrailing=True,
            useMarketOrders=True)
        self.SubscribeCandles(self.CandleType).Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished or self._submitted_entry:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        self._submitted_entry = True
        self._entry_order = self.BuyMarket(self.Volume)

    def CreateClone(self):
        return simple_trailing_stop_strategy()
