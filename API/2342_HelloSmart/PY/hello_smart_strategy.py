import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class hello_smart_strategy(Strategy):
    """
    Grid strategy that opens sequential orders in a single direction.
    Closes all positions on reaching profit or loss limits.
    Mode 0 = Buy direction, Mode 1 = Sell direction.
    """

    def __init__(self):
        super(hello_smart_strategy, self).__init__()
        self._trade_mode = self.Param("Mode", 1) \
            .SetDisplay("Trade Direction", "0=Buy, 1=Sell", "General")
        self._step_ticks = self.Param("StepTicks", 300.0) \
            .SetDisplay("Step", "Price movement to add position", "Risk")
        self._profit_target = self.Param("ProfitTarget", 60.0) \
            .SetDisplay("Profit Target", "Close all positions on this profit", "Risk")
        self._loss_limit = self.Param("LossLimit", 5100.0) \
            .SetDisplay("Loss Limit", "Close all positions on this loss", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._last_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(hello_smart_strategy, self).OnReseted()
        self._last_price = 0.0

    def OnStarted(self, time):
        super(hello_smart_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        step_price = self._step_ticks.Value * 0.01

        if self.Position != 0:
            pnl = float(self.PnL)
            if pnl > self._profit_target.Value or pnl < -self._loss_limit.Value:
                if self.Position > 0:
                    self.SellMarket()
                else:
                    self.BuyMarket()
                self._last_price = price
                return

        if self._trade_mode.Value == 0:
            need_open = self.Position <= 0 or (self.Position > 0 and (self._last_price - price) >= step_price)
            if need_open:
                self.BuyMarket()
                self._last_price = price
        else:
            need_open = self.Position >= 0 or (self.Position < 0 and (price - self._last_price) >= step_price)
            if need_open:
                self.SellMarket()
                self._last_price = price

    def CreateClone(self):
        return hello_smart_strategy()
