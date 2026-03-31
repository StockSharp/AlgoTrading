import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

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
            .SetDisplay("Sell Offset", "Distance below SMA for sell entry (points)", "Distances")
        self._stop_loss_points = self.Param("StopLossPoints", 100) \
            .SetDisplay("Stop Loss", "Stop-loss distance in points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 150) \
            .SetDisplay("Take Profit", "Take-profit distance in points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle series for signals", "General")

        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._price_step = 0.0

    @property
    def buy_offset_points(self):
        return self._buy_offset_points.Value

    @property
    def sell_offset_points(self):
        return self._sell_offset_points.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cm_panel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None
        self._price_step = 0.0

    def OnStarted2(self, time):
        super(cm_panel_strategy, self).OnStarted2(time)

        step = self.Security.PriceStep if self.Security is not None else None
        self._price_step = float(step) if step is not None and float(step) > 0 else 0.01

        sma = SimpleMovingAverage()
        sma.Length = 20

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormed:
            return

        price = float(candle.ClosePrice)
        step = self._price_step if self._price_step > 0 else 0.01
        sma_val = float(sma_value)

        # Check SL/TP for open positions
        if self.Position != 0 and self._entry_price > 0:
            if self.Position > 0:
                if self._stop_price is not None and price <= self._stop_price:
                    self.SellMarket(abs(self.Position))
                    self._reset_position()
                    return
                if self._take_price is not None and price >= self._take_price:
                    self.SellMarket(abs(self.Position))
                    self._reset_position()
                    return
            elif self.Position < 0:
                if self._stop_price is not None and price >= self._stop_price:
                    self.BuyMarket(abs(self.Position))
                    self._reset_position()
                    return
                if self._take_price is not None and price <= self._take_price:
                    self.BuyMarket(abs(self.Position))
                    self._reset_position()
                    return

        # Entry signals
        if self.Position == 0:
            buy_level = sma_val + self.buy_offset_points * step
            sell_level = sma_val - self.sell_offset_points * step

            if price >= buy_level:
                self.BuyMarket()
                self._entry_price = price
                self._stop_price = price - self.stop_loss_points * step if self.stop_loss_points > 0 else None
                self._take_price = price + self.take_profit_points * step if self.take_profit_points > 0 else None
            elif price <= sell_level:
                self.SellMarket()
                self._entry_price = price
                self._stop_price = price + self.stop_loss_points * step if self.stop_loss_points > 0 else None
                self._take_price = price - self.take_profit_points * step if self.take_profit_points > 0 else None

    def _reset_position(self):
        self._entry_price = 0.0
        self._stop_price = None
        self._take_price = None

    def CreateClone(self):
        return cm_panel_strategy()
