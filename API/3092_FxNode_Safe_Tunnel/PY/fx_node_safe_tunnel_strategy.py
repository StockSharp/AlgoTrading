import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Highest, Lowest, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class fx_node_safe_tunnel_strategy(Strategy):
    def __init__(self):
        super(fx_node_safe_tunnel_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._channel_period = self.Param("ChannelPeriod", 100) \
            .SetGreaterThanZero() \
            .SetDisplay("Channel Period", "Lookback for Highest/Lowest channel", "Indicator")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "ATR lookback for stops", "Indicator")
        self._touch_pct = self.Param("TouchPct", 0.02) \
            .SetDisplay("Touch %", "How close price must be to channel boundary (0-1)", "Indicator")
        self._entry_price = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def channel_period(self):
        return self._channel_period.Value
    @property
    def atr_period(self):
        return self._atr_period.Value
    @property
    def touch_pct(self):
        return self._touch_pct.Value

    def OnReseted(self):
        super(fx_node_safe_tunnel_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(fx_node_safe_tunnel_strategy, self).OnStarted2(time)
        self._entry_price = 0.0

        highest = Highest()
        highest.Length = self.channel_period
        lowest = Lowest()
        lowest.Length = self.channel_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(highest, lowest, atr, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, highest)
            self.DrawIndicator(area, lowest)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, high, low, atr_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        high = float(high)
        low = float(low)
        atr_val = float(atr_val)
        channel_width = high - low
        if channel_width <= 0:
            return

        touch_zone = channel_width * float(self.touch_pct)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            if close >= high - touch_zone or (self._entry_price > 0 and close < self._entry_price - atr_val * 2):
                self.SellMarket()
                self._entry_price = 0.0
                self._cooldown = 10
                return
        elif self.Position < 0:
            if close <= low + touch_zone or (self._entry_price > 0 and close > self._entry_price + atr_val * 2):
                self.BuyMarket()
                self._entry_price = 0.0
                self._cooldown = 10
                return

        if self.Position <= 0 and close <= low + touch_zone:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown = 10
        elif self.Position >= 0 and close >= high - touch_zone:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown = 10

    def CreateClone(self):
        return fx_node_safe_tunnel_strategy()
