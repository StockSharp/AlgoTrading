import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class digital_cci_woodies_strategy(Strategy):
    def __init__(self):
        super(digital_cci_woodies_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for candles", "General")
        self._fast_length = self.Param("FastLength", 14) \
            .SetDisplay("Fast CCI Length", "Length of the fast CCI", "Indicators")
        self._slow_length = self.Param("SlowLength", 6) \
            .SetDisplay("Slow CCI Length", "Length of the slow CCI", "Indicators")
        self._buy_open = self.Param("BuyOpen", True) \
            .SetDisplay("Buy Open", "Allow long entries", "Trading")
        self._sell_open = self.Param("SellOpen", True) \
            .SetDisplay("Sell Open", "Allow short entries", "Trading")
        self._buy_close = self.Param("BuyClose", True) \
            .SetDisplay("Buy Close", "Allow closing longs", "Trading")
        self._sell_close = self.Param("SellClose", True) \
            .SetDisplay("Sell Close", "Allow closing shorts", "Trading")
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def fast_length(self):
        return self._fast_length.Value

    @property
    def slow_length(self):
        return self._slow_length.Value

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

    def OnReseted(self):
        super(digital_cci_woodies_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._is_first = True

    def OnStarted(self, time):
        super(digital_cci_woodies_strategy, self).OnStarted(time)

        fast_cci = CommodityChannelIndex()
        fast_cci.Length = self.fast_length
        slow_cci = CommodityChannelIndex()
        slow_cci.Length = self.slow_length

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(fast_cci, slow_cci, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        indicator_area = self.CreateChartArea()
        if indicator_area is not None:
            self.DrawIndicator(indicator_area, fast_cci)
            self.DrawIndicator(indicator_area, slow_cci)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast_val = float(fast)
        slow_val = float(slow)

        if self._is_first:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._is_first = False
            return

        cross_up = self._prev_fast <= self._prev_slow and fast_val > slow_val
        cross_down = self._prev_fast >= self._prev_slow and fast_val < slow_val

        if cross_up:
            if self.Position < 0 and self.sell_close:
                self.BuyMarket()
            if self.buy_open and self.Position <= 0:
                self.BuyMarket()
        elif cross_down:
            if self.Position > 0 and self.buy_close:
                self.SellMarket()
            if self.sell_open and self.Position >= 0:
                self.SellMarket()
        else:
            if fast_val > slow_val and self.sell_close and self.Position < 0:
                self.BuyMarket()
            if fast_val < slow_val and self.buy_close and self.Position > 0:
                self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val

    def CreateClone(self):
        return digital_cci_woodies_strategy()
