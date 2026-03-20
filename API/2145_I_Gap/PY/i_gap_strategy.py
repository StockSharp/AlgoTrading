import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy


class i_gap_strategy(Strategy):
    def __init__(self):
        super(i_gap_strategy, self).__init__()
        self._gap_size = self.Param("GapSize", 5.0) \
            .SetDisplay("Gap Size", "Gap in price steps required to trigger signal", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for gap detection", "General")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Enable Buy", "Allow opening long positions", "Trading")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Enable Sell", "Allow opening short positions", "Trading")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Buy", "Close long on opposite signal", "Trading")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Sell", "Close short on opposite signal", "Trading")
        self._prev_close = None

    @property
    def gap_size(self):
        return self._gap_size.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value

    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value

    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value

    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    def OnReseted(self):
        super(i_gap_strategy, self).OnReseted()
        self._prev_close = None

    def OnStarted(self, time):
        super(i_gap_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        self.StartProtection(
            takeProfit=Unit(2.0, UnitTypes.Percent),
            stopLoss=Unit(1.0, UnitTypes.Percent))

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        if self._prev_close is None:
            self._prev_close = close
            return
        # Use percentage-based gap
        threshold = self._prev_close * float(self.gap_size) * 0.01 / 100.0
        gap = self._prev_close - open_price
        if gap > threshold and self.Position == 0:
            if self.buy_pos_open:
                self.BuyMarket()
        elif -gap > threshold and self.Position == 0:
            if self.sell_pos_open:
                self.SellMarket()
        self._prev_close = close

    def CreateClone(self):
        return i_gap_strategy()
