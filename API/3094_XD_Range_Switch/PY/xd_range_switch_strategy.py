import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest
from StockSharp.Algo.Strategies import Strategy


class xd_range_switch_strategy(Strategy):
    def __init__(self):
        super(xd_range_switch_strategy, self).__init__()

        self._channel_period = self.Param("ChannelPeriod", 200) \
            .SetDisplay("Channel Period", "Lookback for highest/lowest channel", "Indicator")
        self._stop_loss_points = self.Param("StopLossPoints", 200) \
            .SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 400) \
            .SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk")

        self._highest = None
        self._lowest = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def channel_period(self):
        return self._channel_period.Value

    @property
    def stop_loss_points(self):
        return self._stop_loss_points.Value

    @property
    def take_profit_points(self):
        return self._take_profit_points.Value

    def OnReseted(self):
        super(xd_range_switch_strategy, self).OnReseted()
        self._highest = None
        self._lowest = None
        self._prev_high = 0.0
        self._prev_low = 0.0
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(xd_range_switch_strategy, self).OnStarted2(time)

        self._highest = Highest()
        self._highest.Length = self.channel_period
        self._lowest = Lowest()
        self._lowest.Length = self.channel_period

        subscription = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        subscription.Bind(self._highest, self._lowest, self._process_candle)
        subscription.Start()

    def _process_candle(self, candle, high_value, low_value):
        if candle.State != CandleStates.Finished:
            return

        h_val = float(high_value)
        l_val = float(low_value)

        if not self._highest.IsFormed or not self._lowest.IsFormed:
            self._prev_high = h_val
            self._prev_low = l_val
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_high = h_val
            self._prev_low = l_val
            return

        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0

        # Check SL/TP
        if self.Position > 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close <= self._entry_price - self.stop_loss_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 30
                self._prev_high = h_val
                self._prev_low = l_val
                return
            if self.take_profit_points > 0 and close >= self._entry_price + self.take_profit_points * step:
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 30
                self._prev_high = h_val
                self._prev_low = l_val
                return
        elif self.Position < 0 and self._entry_price > 0:
            if self.stop_loss_points > 0 and close >= self._entry_price + self.stop_loss_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 30
                self._prev_high = h_val
                self._prev_low = l_val
                return
            if self.take_profit_points > 0 and close <= self._entry_price - self.take_profit_points * step:
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 30
                self._prev_high = h_val
                self._prev_low = l_val
                return

        # Breakout above previous channel high
        if close > self._prev_high and self._prev_high > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 30
        # Breakout below previous channel low
        elif close < self._prev_low and self._prev_low > 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 30

        self._prev_high = h_val
        self._prev_low = l_val

    def CreateClone(self):
        return xd_range_switch_strategy()
