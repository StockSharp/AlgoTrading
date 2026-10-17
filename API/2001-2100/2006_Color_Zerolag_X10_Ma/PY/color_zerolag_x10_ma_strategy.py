import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates
from StockSharp.Algo.Indicators import ZeroLagExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_x10_ma_strategy(Strategy):
    """ZLEMA slope reversals with independent entry and exit permissions."""

    def __init__(self):
        super(color_zerolag_x10_ma_strategy, self).__init__()
        self._length = self.Param("Length", 20).SetDisplay("Length", "ZLEMA length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._open_buy = self.Param("BuyPosOpen", True).SetDisplay("Open Buy", "Allow long entries", "Trading")
        self._open_sell = self.Param("SellPosOpen", True).SetDisplay("Open Sell", "Allow short entries", "Trading")
        self._close_buy = self.Param("BuyPosClose", True).SetDisplay("Close Buy", "Allow long exits", "Trading")
        self._close_sell = self.Param("SellPosClose", True).SetDisplay("Close Sell", "Allow short exits", "Trading")
        self._previous = None
        self._before_previous = None
        self._pending_order = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(color_zerolag_x10_ma_strategy, self).OnReseted()
        self._previous = self._before_previous = None
        self._pending_order = None

    def OnStarted2(self, time):
        super(color_zerolag_x10_ma_strategy, self).OnStarted2(time)
        self._previous = self._before_previous = None
        self._pending_order = None
        zlma = ZeroLagExponentialMovingAverage()
        zlma.Length = self._length.Value
        self.SubscribeCandles(self.CandleType).Bind(zlma, self.on_process).Start()

    def on_process(self, candle, value):
        if candle.State != CandleStates.Finished:
            return

        current = float(value)
        up = self._before_previous is not None and self._previous < self._before_previous and current > self._previous
        down = self._before_previous is not None and self._previous > self._before_previous and current < self._previous
        self._before_previous, self._previous = self._previous, current

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if up:
            if self.Position < 0 and self._close_sell.Value:
                amount = -self.Position + (self.Volume if self._open_buy.Value else 0)
                self._pending_order = self.BuyMarket(amount)
            elif self.Position == 0 and self._open_buy.Value:
                self._pending_order = self.BuyMarket(self.Volume)
        elif down:
            if self.Position > 0 and self._close_buy.Value:
                amount = self.Position + (self.Volume if self._open_sell.Value else 0)
                self._pending_order = self.SellMarket(amount)
            elif self.Position == 0 and self._open_sell.Value:
                self._pending_order = self.SellMarket(self.Volume)

    def CreateClone(self):
        return color_zerolag_x10_ma_strategy()
