import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class marneni_money_tree_strategy(Strategy):
    def __init__(self):
        super(marneni_money_tree_strategy, self).__init__()
        self._sma_period = self.Param("SmaPeriod", 20) \
            .SetDisplay("SMA Period", "SMA length", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR for stops", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._sma_buffer = [0.0] * 31
        self._buffer_index = 0
        self._values_count = 0

    @property
    def sma_period(self):
        return self._sma_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(marneni_money_tree_strategy, self).OnReseted()
        self._sma_buffer = [0.0] * 31
        self._buffer_index = 0
        self._values_count = 0

    def OnStarted2(self, time):
        super(marneni_money_tree_strategy, self).OnStarted2(time)
        sma = SimpleMovingAverage()
        sma.Length = self.sma_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, sma_value, atr_value):
        if candle.State != CandleStates.Finished:
            return
        buf_len = len(self._sma_buffer)
        self._sma_buffer[self._buffer_index] = sma_value
        self._buffer_index = (self._buffer_index + 1) % buf_len
        if self._values_count < buf_len:
            self._values_count += 1
        if self._values_count < buf_len:
            return
        if atr_value <= 0:
            return
        idx_current = (self._buffer_index - 1 + buf_len) % buf_len
        idx_shift4 = (self._buffer_index - 5 + buf_len) % buf_len
        idx_shift30 = self._buffer_index % buf_len
        ma = self._sma_buffer[idx_shift4]
        ma1 = self._sma_buffer[idx_current]
        ma2 = self._sma_buffer[idx_shift30]
        if self.Position == 0:
            if ma > ma1 and ma < ma2:
                self.SellMarket()
            elif ma < ma1 and ma > ma2:
                self.BuyMarket()
        elif self.Position > 0 and ma > ma1:
            self.SellMarket()
        elif self.Position < 0 and ma < ma1:
            self.BuyMarket()

    def CreateClone(self):
        return marneni_money_tree_strategy()
