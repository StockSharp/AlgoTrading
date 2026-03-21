import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_trend_cf_strategy(Strategy):

    def __init__(self):
        super(color_trend_cf_strategy, self).__init__()

        self._period = self.Param("Period", 30) \
            .SetDisplay("CF Period", "Base period for fast EMA", "Indicator")
        self._stop_loss = self.Param("StopLoss", 1000.0) \
            .SetDisplay("Stop Loss", "Stop loss in price units", "Risk")
        self._take_profit = self.Param("TakeProfit", 2000.0) \
            .SetDisplay("Take Profit", "Take profit in price units", "Risk")
        self._allow_buy_open = self.Param("AllowBuyOpen", True) \
            .SetDisplay("Allow Buy", "Permission to open long", "Permissions")
        self._allow_sell_open = self.Param("AllowSellOpen", True) \
            .SetDisplay("Allow Sell", "Permission to open short", "Permissions")
        self._allow_buy_close = self.Param("AllowBuyClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "Permissions")
        self._allow_sell_close = self.Param("AllowSellClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "Permissions")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe for indicator", "General")

        self._entry_price = 0.0
        self._is_long = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    @property
    def Period(self):
        return self._period.Value

    @Period.setter
    def Period(self, value):
        self._period.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def AllowBuyOpen(self):
        return self._allow_buy_open.Value

    @AllowBuyOpen.setter
    def AllowBuyOpen(self, value):
        self._allow_buy_open.Value = value

    @property
    def AllowSellOpen(self):
        return self._allow_sell_open.Value

    @AllowSellOpen.setter
    def AllowSellOpen(self, value):
        self._allow_sell_open.Value = value

    @property
    def AllowBuyClose(self):
        return self._allow_buy_close.Value

    @AllowBuyClose.setter
    def AllowBuyClose(self, value):
        self._allow_buy_close.Value = value

    @property
    def AllowSellClose(self):
        return self._allow_sell_close.Value

    @AllowSellClose.setter
    def AllowSellClose(self, value):
        self._allow_sell_close.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(color_trend_cf_strategy, self).OnStarted(time)

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.Period
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.Period * 2

        self.SubscribeCandles(self.CandleType) \
            .Bind(fast_ema, slow_ema, self.ProcessCandle) \
            .Start()

    def ProcessCandle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        fast = float(fast_value)
        slow = float(slow_value)

        if not self._initialized:
            self._prev_fast = fast
            self._prev_slow = slow
            self._initialized = True
            return

        cross_up = self._prev_fast <= self._prev_slow and fast > slow
        cross_down = self._prev_fast >= self._prev_slow and fast < slow

        self._prev_fast = fast
        self._prev_slow = slow

        if cross_up:
            if self.AllowSellClose and self.Position < 0:
                self.BuyMarket(abs(self.Position))

            if self.AllowBuyOpen and self.Position <= 0:
                self._entry_price = float(candle.ClosePrice)
                self._is_long = True
                self.BuyMarket()

        elif cross_down:
            if self.AllowBuyClose and self.Position > 0:
                self.SellMarket(self.Position)

            if self.AllowSellOpen and self.Position >= 0:
                self._entry_price = float(candle.ClosePrice)
                self._is_long = False
                self.SellMarket()

        if self._entry_price != 0.0:
            if self._is_long and self.Position > 0:
                stop = self._entry_price - float(self.StopLoss)
                take = self._entry_price + float(self.TakeProfit)
                if float(candle.LowPrice) <= stop or float(candle.HighPrice) >= take:
                    self.SellMarket(self.Position)
            elif not self._is_long and self.Position < 0:
                stop = self._entry_price + float(self.StopLoss)
                take = self._entry_price - float(self.TakeProfit)
                if float(candle.HighPrice) >= stop or float(candle.LowPrice) <= take:
                    self.BuyMarket(abs(self.Position))

    def OnReseted(self):
        super(color_trend_cf_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._is_long = False
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._initialized = False

    def CreateClone(self):
        return color_trend_cf_strategy()
