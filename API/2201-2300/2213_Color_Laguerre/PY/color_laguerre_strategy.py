import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class color_laguerre_strategy(Strategy):
    def __init__(self):
        super(color_laguerre_strategy, self).__init__()
        self._gamma = self.Param("Gamma", 0.7) \
            .SetDisplay("Gamma", "Laguerre filter gamma", "Indicators")
        self._high_level = self.Param("HighLevel", 85) \
            .SetDisplay("High Level", "Upper oscillator level", "Indicators")
        self._middle_level = self.Param("MiddleLevel", 50) \
            .SetDisplay("Middle Level", "Middle oscillator level", "Indicators")
        self._low_level = self.Param("LowLevel", 15) \
            .SetDisplay("Low Level", "Lower oscillator level", "Indicators")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Buy Open", "Allow opening long positions", "Trading")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Sell Open", "Allow opening short positions", "Trading")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Buy Close", "Allow closing longs", "Trading")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Sell Close", "Allow closing shorts", "Trading")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prev_signal = None

    @property
    def gamma(self):
        return self._gamma.Value

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def middle_level(self):
        return self._middle_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

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
    def stop_loss_percent(self):
        return self._stop_loss_percent.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(color_laguerre_strategy, self).OnReseted()
        self._prev_signal = None

    def OnStarted2(self, time):
        super(color_laguerre_strategy, self).OnStarted2(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = 14
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not rsi_value.IsFormed:
            return
        value = float(rsi_value)
        mid = float(self.middle_level)
        signal = 2 if value >= mid else 1
        if self._prev_signal is None:
            self._prev_signal = signal
            return
        prev = self._prev_signal
        if prev == 1 and signal == 2 and self.Position <= 0:
            self.BuyMarket()
        elif prev == 2 and signal == 1 and self.Position >= 0:
            self.SellMarket()
        self._prev_signal = signal
        if self.Position > 0 and value <= float(self.low_level) and self.sell_close:
            self.SellMarket()
        elif self.Position < 0 and value >= float(self.high_level) and self.buy_close:
            self.BuyMarket()

    def CreateClone(self):
        return color_laguerre_strategy()
