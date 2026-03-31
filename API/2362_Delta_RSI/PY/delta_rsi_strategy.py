import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

UP_STATE = 0
PASS_STATE = 1
DOWN_STATE = 2


class delta_rsi_strategy(Strategy):
    def __init__(self):
        super(delta_rsi_strategy, self).__init__()
        self._up_state = self.Param("UpState", 0) \
            .SetDisplay("Up State", "Value representing bullish state", "Parameters")
        self._pass_state = self.Param("PassState", 1) \
            .SetDisplay("Neutral State", "Value representing neutral state", "Parameters")
        self._down_state = self.Param("DownState", 2) \
            .SetDisplay("Down State", "Value representing bearish state", "Parameters")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast RSI Period", "Length of fast RSI", "Parameters")
        self._slow_period = self.Param("SlowPeriod", 50) \
            .SetDisplay("Slow RSI Period", "Length of slow RSI", "Parameters")
        self._level = self.Param("Level", 50) \
            .SetDisplay("Signal Level", "RSI threshold level", "Parameters")
        self._buy_pos_open = self.Param("BuyPosOpen", True) \
            .SetDisplay("Open Long", "Allow opening long positions", "Parameters")
        self._sell_pos_open = self.Param("SellPosOpen", True) \
            .SetDisplay("Open Short", "Allow opening short positions", "Parameters")
        self._buy_pos_close = self.Param("BuyPosClose", True) \
            .SetDisplay("Close Long", "Allow closing long positions", "Parameters")
        self._sell_pos_close = self.Param("SellPosClose", True) \
            .SetDisplay("Close Short", "Allow closing short positions", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_color = 1

    @property
    def up_state(self):
        return self._up_state.Value

    @property
    def pass_state(self):
        return self._pass_state.Value

    @property
    def down_state(self):
        return self._down_state.Value

    @property
    def fast_period(self):
        return self._fast_period.Value

    @property
    def slow_period(self):
        return self._slow_period.Value

    @property
    def level(self):
        return self._level.Value

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

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(delta_rsi_strategy, self).OnReseted()
        self._prev_color = int(self.pass_state)

    def OnStarted2(self, time):
        super(delta_rsi_strategy, self).OnStarted2(time)
        self._prev_color = int(self.pass_state)
        rsi_fast = RelativeStrengthIndex()
        rsi_fast.Length = int(self.fast_period)
        rsi_slow = RelativeStrengthIndex()
        rsi_slow.Length = int(self.slow_period)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi_fast, rsi_slow, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi_fast)
            self.DrawIndicator(area, rsi_slow)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_fast, rsi_slow):
        if candle.State != CandleStates.Finished:
            return
        rsi_fast = float(rsi_fast)
        rsi_slow = float(rsi_slow)
        lvl = float(self.level)
        us = int(self.up_state)
        ps = int(self.pass_state)
        ds = int(self.down_state)
        color = ps
        if rsi_slow > lvl and rsi_fast > rsi_slow:
            color = us
        elif rsi_slow < 100 - lvl and rsi_fast < rsi_slow:
            color = ds
        if self._prev_color == us and color != us:
            if self.sell_pos_close and self.Position < 0:
                self.BuyMarket()
            if self.buy_pos_open and self.Position <= 0:
                self.BuyMarket()
        elif self._prev_color == ds and color != ds:
            if self.buy_pos_close and self.Position > 0:
                self.SellMarket()
            if self.sell_pos_open and self.Position >= 0:
                self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return delta_rsi_strategy()
