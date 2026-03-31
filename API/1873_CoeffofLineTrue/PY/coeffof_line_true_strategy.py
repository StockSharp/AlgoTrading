import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class coeffof_line_true_strategy(Strategy):
    def __init__(self):
        super(coeffof_line_true_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._slope_period = self.Param("SlopePeriod", 5) \
            .SetDisplay("Slope Period", "Slope proxy length", "Parameters")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Historical bar index for signal", "Parameters")
        self._buy_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Buy Open", "Allow opening long positions", "Trading")
        self._sell_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Sell Open", "Allow opening short positions", "Trading")
        self._buy_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Buy Close", "Allow closing long positions", "Trading")
        self._sell_close = self.Param("SellPosClose", True) \
            .SetDisplay("Sell Close", "Allow closing short positions", "Trading")
        self._cooldown_bars = self.Param("CooldownBars", 4) \
            .SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading")
        self._slopes = []
        self._prev_value = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def slope_period(self):
        return self._slope_period.Value
    @property
    def signal_bar(self):
        return self._signal_bar.Value
    @property
    def buy_open(self):
        return self._buy_open.Value
    @property
    def sell_open(self):
        return self._sell_open.Value
    @property
    def buy_close(self):
        return self._buy_close.Value
    @property
    def sell_close(self):
        return self._sell_close.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(coeffof_line_true_strategy, self).OnReseted()
        self._slopes = []
        self._prev_value = None
        self._cooldown_remaining = 0

    def OnStarted2(self, time):
        super(coeffof_line_true_strategy, self).OnStarted2(time)
        slope_proxy = ExponentialMovingAverage()
        slope_proxy.Length = self.slope_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(slope_proxy, self.process_candle).Start()

    def process_candle(self, candle, proxy_value):
        if candle.State != CandleStates.Finished:
            return
        proxy_value = float(proxy_value)
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._prev_value is None:
            self._prev_value = proxy_value
            return
        slope = proxy_value - self._prev_value
        self._prev_value = proxy_value
        self._slopes.append(slope)
        sb = self.signal_bar
        if len(self._slopes) > sb + 2:
            self._slopes.pop(0)
        if len(self._slopes) <= sb + 1:
            return
        prev = self._slopes[-(sb + 1)]
        prev2 = self._slopes[-(sb + 2)]
        buy_open_sig = self.buy_open and prev2 <= 0 and prev > 0
        sell_open_sig = self.sell_open and prev2 >= 0 and prev < 0
        buy_close_sig = self.buy_close and prev2 >= 0 and prev < 0
        sell_close_sig = self.sell_close and prev2 <= 0 and prev > 0
        if buy_close_sig and self.Position > 0:
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars
        if sell_close_sig and self.Position < 0:
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        if self._cooldown_remaining == 0 and buy_open_sig:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.cooldown_bars
        if self._cooldown_remaining == 0 and sell_open_sig:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.cooldown_bars

    def CreateClone(self):
        return coeffof_line_true_strategy()
