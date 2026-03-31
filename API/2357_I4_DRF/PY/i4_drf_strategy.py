import clr
from collections import deque

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy

MODE_DIRECT = 0
MODE_NOT_DIRECT = 1


class i4_drf_strategy(Strategy):
    def __init__(self):
        super(i4_drf_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe of candles", "General")
        self._period = self.Param("Period", 11) \
            .SetDisplay("Period", "Indicator period", "Parameters")
        self._signal_bar = self.Param("SignalBar", 1) \
            .SetDisplay("Signal Bar", "Shift for signal", "Parameters")
        self._trend_mode = self.Param("TrendMode", 0) \
            .SetDisplay("Trend Mode", "0=Direct, 1=NotDirect", "Parameters")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Open Long", "Allow opening long positions", "Switches")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Open Short", "Allow opening short positions", "Switches")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "Switches")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "Switches")
        self._diffs = deque()
        self._sum = 0
        self._prev_price = None
        self._is_formed = False
        self._prev_color = 0.0
        self._prev_prev_color = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def period(self):
        return self._period.Value

    @property
    def signal_bar(self):
        return self._signal_bar.Value

    @property
    def trend_mode(self):
        return self._trend_mode.Value

    @property
    def buy_pos_open(self):
        return self._buy_pos_open.Value

    @property
    def sell_pos_open(self):
        return self._sell_pos_open.Value

    @property
    def buy_pos_close(self):
        return self._buy_pos_close.Value

    @property
    def sell_pos_close(self):
        return self._sell_pos_close.Value

    def OnReseted(self):
        super(i4_drf_strategy, self).OnReseted()
        self._diffs = deque()
        self._sum = 0
        self._prev_price = None
        self._is_formed = False
        self._prev_color = 0.0
        self._prev_prev_color = 0.0

    def OnStarted2(self, time):
        super(i4_drf_strategy, self).OnStarted2(time)
        self._diffs = deque()
        self._sum = 0
        self._prev_price = None
        self._is_formed = False
        self._prev_color = 0.0
        self._prev_prev_color = 0.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _compute_i4drf(self, price):
        length = int(self.period)
        if self._prev_price is None:
            self._prev_price = price
            self._is_formed = False
            return 0.0
        if price > self._prev_price:
            diff = 1
        elif price < self._prev_price:
            diff = -1
        else:
            diff = 0
        self._prev_price = price
        self._sum += diff
        self._diffs.append(diff)
        if len(self._diffs) > length:
            self._sum -= self._diffs.popleft()
        if len(self._diffs) < length:
            self._is_formed = False
            return 0.0
        self._is_formed = True
        return float(self._sum) / length * 100.0

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price = float(candle.ClosePrice)
        value = self._compute_i4drf(price)
        color = 1.0 if value > 0 else 0.0
        if not self._is_formed:
            self._prev_prev_color = self._prev_color
            self._prev_color = color
            return
        buy_open = False
        sell_open = False
        buy_close = False
        sell_close = False
        tm = int(self.trend_mode)
        if tm == MODE_DIRECT:
            if self._prev_prev_color == 1.0:
                if self.buy_pos_open and self._prev_color < 1.0:
                    buy_open = True
                if self.sell_pos_close:
                    sell_close = True
            if self._prev_prev_color == 0.0:
                if self.sell_pos_open and self._prev_color > 0.0:
                    sell_open = True
                if self.buy_pos_close:
                    buy_close = True
        else:
            if self._prev_prev_color == 0.0:
                if self.buy_pos_open and self._prev_color > 0.0:
                    buy_open = True
                if self.sell_pos_close:
                    sell_close = True
            if self._prev_prev_color == 1.0:
                if self.sell_pos_open and self._prev_color < 1.0:
                    sell_open = True
                if self.buy_pos_close:
                    buy_close = True
        if buy_close and self.Position > 0:
            self.SellMarket()
        if sell_close and self.Position < 0:
            self.BuyMarket()
        if buy_open and self.Position <= 0:
            self.BuyMarket()
        if sell_open and self.Position >= 0:
            self.SellMarket()
        self._prev_prev_color = self._prev_color
        self._prev_color = color

    def CreateClone(self):
        return i4_drf_strategy()
