import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from collections import deque


class twenty_200_expert_strategy(Strategy):
    def __init__(self):
        super(twenty_200_expert_strategy, self).__init__()
        self._shift1 = self.Param("Shift1", 6) \
            .SetDisplay("Shift 1", "First bar shift", "Signals")
        self._shift2 = self.Param("Shift2", 2) \
            .SetDisplay("Shift 2", "Second bar shift", "Signals")
        self._delta_long = self.Param("DeltaLong", 20) \
            .SetDisplay("Delta Long", "Long threshold", "Signals")
        self._delta_short = self.Param("DeltaShort", 40) \
            .SetDisplay("Delta Short", "Short threshold", "Signals")
        self._tp_long = self.Param("TakeProfitLong", 390) \
            .SetDisplay("TP Long", "TP for long", "Risk")
        self._sl_long = self.Param("StopLossLong", 1470) \
            .SetDisplay("SL Long", "SL for long", "Risk")
        self._tp_short = self.Param("TakeProfitShort", 320) \
            .SetDisplay("TP Short", "TP for short", "Risk")
        self._sl_short = self.Param("StopLossShort", 2670) \
            .SetDisplay("SL Short", "SL for short", "Risk")
        self._trade_hour = self.Param("TradeHour", 12) \
            .SetDisplay("Trade Hour", "Hour to open positions", "Signals")
        self._max_open_time = self.Param("MaxOpenTime", 504) \
            .SetDisplay("Max Open Time", "Maximum position time in hours", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._opens = []
        self._entry_price = 0.0
        self._entry_time = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(twenty_200_expert_strategy, self).OnReseted()
        self._opens = []
        self._entry_price = 0.0
        self._entry_time = None

    def OnStarted(self, time):
        super(twenty_200_expert_strategy, self).OnStarted(time)

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._opens.append(float(candle.OpenPrice))
        max_shift = max(self._shift1.Value, self._shift2.Value) + 1
        while len(self._opens) > max_shift:
            self._opens.pop(0)

        step = 1.0
        if self.Security is not None and self.Security.PriceStep is not None:
            step = float(self.Security.PriceStep)
        if step <= 0:
            step = 1.0

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)

        if self.Position > 0:
            tp = self._entry_price + self._tp_long.Value * step
            sl = self._entry_price - self._sl_long.Value * step
            timed_out = self._max_open_time.Value > 0 and self._entry_time is not None and \
                (candle.OpenTime - self._entry_time).TotalHours >= self._max_open_time.Value
            if high >= tp or low <= sl or timed_out:
                self.SellMarket()
                self._entry_price = 0.0
                self._entry_time = None
        elif self.Position < 0:
            tp = self._entry_price - self._tp_short.Value * step
            sl = self._entry_price + self._sl_short.Value * step
            timed_out = self._max_open_time.Value > 0 and self._entry_time is not None and \
                (candle.OpenTime - self._entry_time).TotalHours >= self._max_open_time.Value
            if low <= tp or high >= sl or timed_out:
                self.BuyMarket()
                self._entry_price = 0.0
                self._entry_time = None

        if len(self._opens) < max_shift:
            return

        if self.Position != 0:
            return

        arr = self._opens
        open_s1 = arr[len(arr) - 1 - self._shift1.Value]
        open_s2 = arr[len(arr) - 1 - self._shift2.Value]

        diff_long = open_s2 - open_s1
        diff_short = open_s1 - open_s2
        th_long = self._delta_long.Value * step
        th_short = self._delta_short.Value * step

        if candle.OpenTime.Hour != self._trade_hour.Value:
            return

        if diff_long > th_long and diff_short <= th_short:
            self.BuyMarket()
            self._entry_price = close
            self._entry_time = candle.OpenTime
        elif diff_short > th_short and diff_long <= th_long:
            self.SellMarket()
            self._entry_price = close
            self._entry_time = candle.OpenTime

    def CreateClone(self):
        return twenty_200_expert_strategy()
