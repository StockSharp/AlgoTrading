import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class karacatica_strategy(Strategy):
    def __init__(self):
        super(karacatica_strategy, self).__init__()
        self._period = self.Param("Period", 30) \
            .SetDisplay("Period", "ADX period and lookback for close comparison", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._close_queue = []
        self._last_signal = 0

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(karacatica_strategy, self).OnReseted()
        self._close_queue = []
        self._last_signal = 0

    def OnStarted2(self, time):
        super(karacatica_strategy, self).OnStarted2(time)
        adx = AverageDirectionalIndex()
        adx.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        self._close_queue.append(close)
        if len(self._close_queue) > self.period + 1:
            self._close_queue.pop(0)
        if len(self._close_queue) <= self.period:
            return
        if not adx_value.IsFormed:
            return
        past_close = self._close_queue[0]
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        if plus_di is None or minus_di is None:
            return
        plus_di = float(plus_di)
        minus_di = float(minus_di)
        buy_signal = close > past_close and plus_di > minus_di and self._last_signal != 1
        sell_signal = close < past_close and minus_di > plus_di and self._last_signal != -1
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
            self._last_signal = 1
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
            self._last_signal = -1

    def CreateClone(self):
        return karacatica_strategy()
