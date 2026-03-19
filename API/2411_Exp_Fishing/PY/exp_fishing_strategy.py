import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Strategies import Strategy

class exp_fishing_strategy(Strategy):
    """
    Trend-following strategy that adds to position every time price moves by configured step.
    Uses StartProtection for TP/SL.
    """

    def __init__(self):
        super(exp_fishing_strategy, self).__init__()
        self._price_step = self.Param("PriceStep", 300.0) \
            .SetDisplay("Price Step", "Minimum price move to enter or add", "Parameters")
        self._max_orders = self.Param("MaxOrders", 10) \
            .SetDisplay("Max Orders", "Maximum orders in one direction", "Parameters")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss distance in price units", "Parameters")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit distance in price units", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")

        self._entry_price = 0.0
        self._orders_count = 0
        self._is_long = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(exp_fishing_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._orders_count = 0
        self._is_long = False

    def OnStarted(self, time):
        super(exp_fishing_strategy, self).OnStarted(time)

        tp = float(self._take_profit.Value)
        sl = float(self._stop_loss.Value)
        self.StartProtection(
            Unit(tp, UnitTypes.Absolute),
            Unit(sl, UnitTypes.Absolute)
        )

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        move = close - open_p
        ps = float(self._price_step.Value)

        if self.Position == 0:
            self._orders_count = 0
            if move >= ps:
                self.BuyMarket()
                self._entry_price = close
                self._orders_count = 1
                self._is_long = True
            elif move <= -ps:
                self.SellMarket()
                self._entry_price = close
                self._orders_count = 1
                self._is_long = False
            return

        if self._orders_count >= self._max_orders.Value:
            return

        if self._is_long:
            if close - self._entry_price >= ps:
                self.BuyMarket()
                self._entry_price = close
                self._orders_count += 1
        else:
            if self._entry_price - close >= ps:
                self.SellMarket()
                self._entry_price = close
                self._orders_count += 1

    def CreateClone(self):
        return exp_fishing_strategy()
