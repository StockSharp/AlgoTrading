import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class lanz_1_0_backtest_strategy(Strategy):
    def __init__(self):
        super(lanz_1_0_backtest_strategy, self).__init__()
        self._enable_buy = self.Param("EnableBuy", True) \
            .SetDisplay("Enable Buy", "Allow long entries", "Signals")
        self._enable_sell = self.Param("EnableSell", True) \
            .SetDisplay("Enable Sell", "Allow short entries", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._open_at_0800 = 0.0
        self._prev_price_direction = 0
        self._today_price_direction = 0
        self._final_signal_direction = 0
        self._order_sent = False
        self._bar_count = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(lanz_1_0_backtest_strategy, self).OnReseted()
        self._open_at_0800 = 0.0
        self._prev_price_direction = 0
        self._today_price_direction = 0
        self._final_signal_direction = 0
        self._order_sent = False
        self._bar_count = 0

    def OnStarted2(self, time):
        super(lanz_1_0_backtest_strategy, self).OnStarted2(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        self._bar_count += 1
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        hour = candle.OpenTime.Hour
        if hour == 8:
            self._open_at_0800 = open_p
        if hour == 18 and self._open_at_0800 > 0.0:
            if close > self._open_at_0800:
                pd = 1
            elif close < self._open_at_0800:
                pd = -1
            else:
                pd = 0
            self._prev_price_direction = self._today_price_direction
            self._today_price_direction = pd
            coinciden = pd == self._prev_price_direction and self._prev_price_direction != 0
            self._final_signal_direction = pd if coinciden else -pd
            self._order_sent = False
        if hour == 9 and self.Position != 0:
            if self.Position > 0:
                self.SellMarket()
            elif self.Position < 0:
                self.BuyMarket()
            self._order_sent = False
            return
        entry_window = hour >= 18 or hour < 8
        can_place = not self._order_sent and self.Position == 0 and entry_window
        if can_place and self._bar_count > 48:
            is_long = self._final_signal_direction == -1
            if is_long and self._enable_buy.Value:
                self.BuyMarket()
                self._order_sent = True
            elif not is_long and self._enable_sell.Value:
                self.SellMarket()
                self._order_sent = True

    def CreateClone(self):
        return lanz_1_0_backtest_strategy()
