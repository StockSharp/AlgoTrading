import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Sides
from StockSharp.Algo.Strategies import Strategy

class cheduecoglioni_alternating_strategy(Strategy):
    """
    Alternates buy and sell market orders with fixed stop loss and take profit distances.
    """

    def __init__(self):
        super(cheduecoglioni_alternating_strategy, self).__init__()
        self._take_profit_pips = self.Param("TakeProfitPips", 10.0) \
            .SetDisplay("Take Profit (pips)", "Distance to take profit", "Risk")
        self._stop_loss_pips = self.Param("StopLossPips", 10.0) \
            .SetDisplay("Stop Loss (pips)", "Distance to stop loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Source candles for timing", "General")

        self._pip_size = 0.0
        self._next_side = Sides.Sell
        self._active_side = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cheduecoglioni_alternating_strategy, self).OnReseted()
        self._next_side = Sides.Sell
        self._active_side = None
        self._pip_size = 0.0

    def OnStarted(self, time):
        super(cheduecoglioni_alternating_strategy, self).OnStarted(time)

        price_step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and float(self.Security.PriceStep) > 0:
            price_step = float(self.Security.PriceStep)
        else:
            decimals = 4
            if self.Security is not None and self.Security.Decimals is not None:
                decimals = int(self.Security.Decimals)
            price_step = 10.0 ** (-decimals)

        self._pip_size = price_step

        if self.Security is not None and self.Security.Decimals is not None:
            digits = int(self.Security.Decimals)
            if digits == 3 or digits == 5:
                self._pip_size *= 10.0

        if self._pip_size <= 0:
            self._pip_size = 1.0

        self.StartProtection(
            takeProfit=Unit(self._take_profit_pips.Value * self._pip_size, UnitTypes.Absolute),
            stopLoss=Unit(self._stop_loss_pips.Value * self._pip_size, UnitTypes.Absolute),
            useMarketOrders=True)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.on_process).Start()

    def on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position != 0:
            return

        if self._next_side == Sides.Buy:
            self.BuyMarket()
        else:
            self.SellMarket()

    def OnPositionReceived(self, position):
        super(cheduecoglioni_alternating_strategy, self).OnPositionReceived(position)

        if self.Position > 0:
            self._active_side = Sides.Buy
            return

        if self.Position < 0:
            self._active_side = Sides.Sell
            return

        if self._active_side is not None:
            if self._active_side == Sides.Buy:
                self._next_side = Sides.Sell
            else:
                self._next_side = Sides.Buy
            self._active_side = None

    def CreateClone(self):
        return cheduecoglioni_alternating_strategy()
