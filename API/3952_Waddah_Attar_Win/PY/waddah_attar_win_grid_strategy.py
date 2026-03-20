import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class waddah_attar_win_grid_strategy(Strategy):
    def __init__(self):
        super(waddah_attar_win_grid_strategy, self).__init__()

        self._step_points = self.Param("StepPoints", 1500).SetDisplay("Step (Points)", "Distance between grid levels in points", "Grid")
        self._first_volume = self.Param("FirstVolume", 0.1).SetDisplay("First Volume", "Volume for the initial orders", "Trading")
        self._increment_volume = self.Param("IncrementVolume", 0.0).SetDisplay("Increment Volume", "Additional volume added when stacking new orders", "Trading")
        self._min_profit = self.Param("MinProfit", 450.0).SetDisplay("Min Profit", "Floating profit target in account currency", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle type for price data", "General")

        self._last_buy_grid_price = 0.0
        self._last_sell_grid_price = 0.0
        self._current_buy_volume = 0.0
        self._current_sell_volume = 0.0
        self._reference_balance = 0.0
        self._grid_active = False

    @property
    def StepPoints(self): return self._step_points.Value
    @property
    def FirstVolume(self): return self._first_volume.Value
    @property
    def IncrementVolume(self): return self._increment_volume.Value
    @property
    def MinProfit(self): return self._min_profit.Value
    @property
    def CandleType(self): return self._candle_type.Value

    def OnStarted(self, time):
        super(waddah_attar_win_grid_strategy, self).OnStarted(time)
        self._reference_balance = float(self.Portfolio.CurrentValue) if self.Portfolio is not None else 0.0
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        price_step = 0.01
        if self.Security is not None and self.Security.PriceStep is not None and float(self.Security.PriceStep) > 0:
            price_step = float(self.Security.PriceStep)

        step_offset = int(self.StepPoints) * price_step
        if step_offset <= 0:
            return

        price = float(candle.ClosePrice)

        current_value = float(self.Portfolio.CurrentValue) if self.Portfolio is not None else 0.0
        floating_profit = current_value - self._reference_balance

        if float(self.MinProfit) > 0 and floating_profit >= float(self.MinProfit) and self._grid_active:
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(abs(self.Position))
            self._reference_balance = float(self.Portfolio.CurrentValue) if self.Portfolio is not None else self._reference_balance
            self._grid_active = False
            self._last_buy_grid_price = 0.0
            self._last_sell_grid_price = 0.0
            self._current_buy_volume = 0.0
            self._current_sell_volume = 0.0
            return

        if not self._grid_active:
            self._last_buy_grid_price = price
            self._last_sell_grid_price = price
            self._current_buy_volume = float(self.FirstVolume)
            self._current_sell_volume = float(self.FirstVolume)
            self._grid_active = True
            self._reference_balance = float(self.Portfolio.CurrentValue) if self.Portfolio is not None else self._reference_balance
            return

        if price <= self._last_buy_grid_price - step_offset:
            self.BuyMarket(self._current_buy_volume)
            self._last_buy_grid_price = price
            self._current_buy_volume += float(self.IncrementVolume)

        if price >= self._last_sell_grid_price + step_offset:
            self.SellMarket(self._current_sell_volume)
            self._last_sell_grid_price = price
            self._current_sell_volume += float(self.IncrementVolume)

    def OnReseted(self):
        super(waddah_attar_win_grid_strategy, self).OnReseted()
        self._last_buy_grid_price = 0.0
        self._last_sell_grid_price = 0.0
        self._current_buy_volume = 0.0
        self._current_sell_volume = 0.0
        self._reference_balance = 0.0
        self._grid_active = False

    def CreateClone(self):
        return waddah_attar_win_grid_strategy()
