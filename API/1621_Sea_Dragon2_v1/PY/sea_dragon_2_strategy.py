import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class sea_dragon_2_strategy(Strategy):
    def __init__(self):
        super(sea_dragon_2_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20).SetGreaterThanZero().SetDisplay("EMA Length", "EMA period for trend", "General")
        self._grid_percent = self.Param("GridPercent", 0.5).SetDisplay("Grid %", "Grid spacing as price percent", "Trading")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Candle Type", "General")

    @property
    def CandleType(self): return self._candle_type.Value
    @CandleType.setter
    def CandleType(self, value): self._candle_type.Value = value

    def OnReseted(self):
        super(sea_dragon_2_strategy, self).OnReseted()
        self._entry_price = 0
        self._last_grid_price = 0

    def OnStarted(self, time):
        super(sea_dragon_2_strategy, self).OnStarted(time)
        self._entry_price = 0
        self._last_grid_price = 0

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        sub = self.SubscribeCandles(self.CandleType)
        sub.Bind(ema, self.OnProcess).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return

        price = float(candle.ClosePrice)
        grid_step = price * self._grid_percent.Value / 100.0

        if self.Position == 0:
            if price > ema_val:
                self.BuyMarket()
                self._entry_price = price
                self._last_grid_price = price
            elif price < ema_val:
                self.SellMarket()
                self._entry_price = price
                self._last_grid_price = price
            return

        if self._last_grid_price == 0:
            self._last_grid_price = price

        if self.Position > 0:
            if price >= self._entry_price + grid_step * 2:
                self.SellMarket()
                self._entry_price = 0
                self._last_grid_price = 0
            elif price <= self._entry_price - grid_step * 4:
                self.SellMarket()
                self._entry_price = 0
                self._last_grid_price = 0
        elif self.Position < 0:
            if price <= self._entry_price - grid_step * 2:
                self.BuyMarket()
                self._entry_price = 0
                self._last_grid_price = 0
            elif price >= self._entry_price + grid_step * 4:
                self.BuyMarket()
                self._entry_price = 0
                self._last_grid_price = 0

    def CreateClone(self):
        return sea_dragon_2_strategy()
