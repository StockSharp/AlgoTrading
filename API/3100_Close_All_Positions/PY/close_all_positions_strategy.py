import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class close_all_positions_strategy(Strategy):
    """
    Opens positions based on SMA trend and closes when SL/TP reached.
    """

    def __init__(self):
        super(close_all_positions_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 100) \
            .SetDisplay("SMA Period", "Moving average period for entry signals", "Indicators")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 300) \
            .SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk")

        self._entry_price = 0.0
        self._prev_sma = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return DataType.TimeFrame(TimeSpan.FromMinutes(5))

    def OnReseted(self):
        super(close_all_positions_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._prev_sma = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(close_all_positions_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self._sma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_sma == 0.0:
            self._prev_sma = sma_val
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_sma = sma_val
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None and float(self.Security.PriceStep) > 0:
            step = float(self.Security.PriceStep)

        if self.Position > 0 and self._entry_price > 0:
            if self._stop_loss_points.Value > 0 and close <= self._entry_price - self._stop_loss_points.Value * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 60
                self._prev_sma = sma_val
                return
            if self._take_profit_points.Value > 0 and close >= self._entry_price + self._take_profit_points.Value * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 60
                self._prev_sma = sma_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self._stop_loss_points.Value > 0 and close >= self._entry_price + self._stop_loss_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 60
                self._prev_sma = sma_val
                return
            if self._take_profit_points.Value > 0 and close <= self._entry_price - self._take_profit_points.Value * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 60
                self._prev_sma = sma_val
                return

        if close > sma_val and open_price <= self._prev_sma and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 60
        elif close < sma_val and open_price >= self._prev_sma and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 60

        self._prev_sma = sma_val

    def CreateClone(self):
        return close_all_positions_strategy()
