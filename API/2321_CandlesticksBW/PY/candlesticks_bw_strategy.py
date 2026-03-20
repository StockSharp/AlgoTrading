import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class candlesticks_bw_strategy(Strategy):
    def __init__(self):
        super(candlesticks_bw_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame for analysis", "General")
        self._ao_fast = None
        self._ao_slow = None
        self._ac_ma = None
        self._prev_ao = 0.0
        self._prev_ac = 0.0
        self._has_prev = False
        self._prev_color = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(candlesticks_bw_strategy, self).OnReseted()
        self._ao_fast = None
        self._ao_slow = None
        self._ac_ma = None
        self._prev_ao = 0.0
        self._prev_ac = 0.0
        self._has_prev = False
        self._prev_color = -1

    def OnStarted(self, time):
        super(candlesticks_bw_strategy, self).OnStarted(time)
        self._prev_ao = 0.0
        self._prev_ac = 0.0
        self._has_prev = False
        self._prev_color = -1
        self._ao_fast = SimpleMovingAverage()
        self._ao_fast.Length = 5
        self._ao_slow = SimpleMovingAverage()
        self._ao_slow.Length = 34
        self._ac_ma = SimpleMovingAverage()
        self._ac_ma.Length = 5
        self.Indicators.Add(self._ao_fast)
        self.Indicators.Add(self._ao_slow)
        self.Indicators.Add(self._ac_ma)
        sma = SimpleMovingAverage()
        sma.Length = 1
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _unused):
        if candle.State != CandleStates.Finished:
            return
        hl2 = (float(candle.HighPrice) + float(candle.LowPrice)) / 2.0
        t = candle.CloseTime
        ao_fast_result = self._ao_fast.Process(hl2, t, True)
        ao_slow_result = self._ao_slow.Process(hl2, t, True)
        if not self._ao_fast.IsFormed or not self._ao_slow.IsFormed:
            return
        ao = float(ao_fast_result.GetValue[float]()) - float(ao_slow_result.GetValue[float]())
        ac_ma_result = self._ac_ma.Process(ao, t, True)
        if not self._ac_ma.IsFormed:
            return
        ac = ao - float(ac_ma_result.GetValue[float]())
        open_price = float(candle.OpenPrice)
        close_price = float(candle.ClosePrice)
        if self._has_prev and ao >= self._prev_ao and ac >= self._prev_ac:
            color = 0 if open_price <= close_price else 1
        elif self._has_prev and ao <= self._prev_ao and ac <= self._prev_ac:
            color = 5 if open_price >= close_price else 4
        else:
            color = 2 if open_price <= close_price else 3
        self._prev_ao = ao
        self._prev_ac = ac
        self._has_prev = True
        if not self.IsFormedAndOnline():
            self._prev_color = color
            return
        if self._prev_color < 0:
            self._prev_color = color
            return
        if self._prev_color < 2 and color > 1 and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_color > 3 and color < 4 and self.Position >= 0:
            self.SellMarket()
        self._prev_color = color

    def CreateClone(self):
        return candlesticks_bw_strategy()
