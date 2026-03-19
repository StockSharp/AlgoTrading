import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

class ilan14_grid_strategy(Strategy):
    """
    Grid martingale strategy. Opens on last two candle closes direction,
    adds averaging positions when price moves against by pip step.
    """

    def __init__(self):
        super(ilan14_grid_strategy, self).__init__()
        self._lot_exponent = self.Param("LotExponent", 1.667).SetDisplay("Lot Exponent", "Multiplier for averaging", "Trading")
        self._pip_step = self.Param("PipStep", 30.0).SetDisplay("Pip Step", "Grid step in price steps", "Trading")
        self._take_profit = self.Param("TakeProfit", 10.0).SetDisplay("Take Profit", "TP in price steps", "Trading")
        self._max_trades = self.Param("MaxTrades", 10).SetDisplay("Max Trades", "Max averaging orders", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(8))).SetDisplay("Candle Type", "Timeframe", "General")

        self._trade_count = 0
        self._avg_price = 0.0
        self._total_vol = 0.0
        self._last_entry = 0.0
        self._direction = 0
        self._prev_close = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ilan14_grid_strategy, self).OnReseted()
        self._trade_count = 0
        self._avg_price = 0.0
        self._total_vol = 0.0
        self._last_entry = 0.0
        self._direction = 0
        self._prev_close = 0.0

    def OnStarted(self, time):
        super(ilan14_grid_strategy, self).OnStarted(time)
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
        sec = self.Security
        step = float(sec.PriceStep) if sec is not None and sec.PriceStep is not None else 1.0
        if step <= 0:
            step = 1.0

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_close = price
            return

        if self._trade_count > 0:
            tp_dist = self._take_profit.Value * step
            if self._direction == 1 and price >= self._avg_price + tp_dist:
                self.SellMarket()
                self._reset_basket()
            elif self._direction == -1 and price <= self._avg_price - tp_dist:
                self.BuyMarket()
                self._reset_basket()

            if self._trade_count > 0 and self._trade_count < self._max_trades.Value:
                trigger = self._pip_step.Value * step
                if trigger > 0:
                    if self._direction == 1 and self._last_entry - price >= trigger:
                        self.BuyMarket()
                        prev_vol = self._total_vol
                        self._total_vol += 1
                        self._avg_price = (self._avg_price * prev_vol + price) / self._total_vol
                        self._last_entry = price
                        self._trade_count += 1
                    elif self._direction == -1 and price - self._last_entry >= trigger:
                        self.SellMarket()
                        prev_vol = self._total_vol
                        self._total_vol += 1
                        self._avg_price = (self._avg_price * prev_vol + price) / self._total_vol
                        self._last_entry = price
                        self._trade_count += 1

        if self._trade_count == 0:
            if self._prev_close == 0:
                self._prev_close = price
                return

            is_long = self._prev_close <= price
            if is_long:
                self.BuyMarket()
                self._direction = 1
            else:
                self.SellMarket()
                self._direction = -1
            self._avg_price = price
            self._total_vol = 1
            self._last_entry = price
            self._trade_count = 1

        self._prev_close = price

    def _reset_basket(self):
        self._trade_count = 0
        self._avg_price = 0.0
        self._total_vol = 0.0
        self._last_entry = 0.0
        self._direction = 0

    def CreateClone(self):
        return ilan14_grid_strategy()
