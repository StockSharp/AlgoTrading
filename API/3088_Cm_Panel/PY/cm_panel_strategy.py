import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class cm_panel_strategy(Strategy):
    def __init__(self):
        super(cm_panel_strategy, self).__init__()

        self._buy_offset_points = self.Param("BuyOffsetPoints", 100) \
            .SetDisplay("Buy Offset", "Distance above SMA for buy entry (points)", "Distances")
        self._sell_offset_points = self.Param("SellOffsetPoints", 100) \
            .SetDisplay("Buy Offset", "Distance above SMA for buy entry (points)", "Distances")
        self._stop_loss_points = self.Param("StopLossPoints", 100) \
            .SetDisplay("Buy Offset", "Distance above SMA for buy entry (points)", "Distances")
        self._take_profit_points = self.Param("TakeProfitPoints", 150) \
            .SetDisplay("Buy Offset", "Distance above SMA for buy entry (points)", "Distances")
        self._candle_type = self.Param("CandleType", TimeSpan.FromMinutes(5) \
            .SetDisplay("Buy Offset", "Distance above SMA for buy entry (points)", "Distances")

        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._price_step = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cm_panel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._price_step = 0.0

    def OnStarted(self, time):
        super(cm_panel_strategy, self).OnStarted(time)

        self._sma = SimpleMovingAverage()
        self._sma.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._sma, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return cm_panel_strategy()
