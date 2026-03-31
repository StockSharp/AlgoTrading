import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class go_candle_body_reversal_strategy(Strategy):
    def __init__(self):
        super(go_candle_body_reversal_strategy, self).__init__()
        self._period = self.Param("Period", 30) \
            .SetDisplay("Period", "SMA period for candle body", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "Parameters")
        self._body_sma = None
        self._prev_sign = 0

    @property
    def period(self):
        return self._period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(go_candle_body_reversal_strategy, self).OnReseted()
        self._body_sma = None
        self._prev_sign = 0

    def OnStarted2(self, time):
        super(go_candle_body_reversal_strategy, self).OnStarted2(time)
        self._body_sma = ExponentialMovingAverage()
        self._body_sma.Length = self.period
        self.Indicators.Add(self._body_sma)
        warmup = ExponentialMovingAverage()
        warmup.Length = self.period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(warmup, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, _warmup_val):
        if candle.State != CandleStates.Finished:
            return
        body = float(candle.ClosePrice) - float(candle.OpenPrice)
        div = DecimalIndicatorValue(self._body_sma, body, candle.OpenTime)
        div.IsFinal = True
        ma_result = self._body_sma.Process(div)
        if not ma_result.IsFormed:
            return
        value = float(ma_result)
        if value > 0:
            sign = 1
        elif value < 0:
            sign = -1
        else:
            sign = 0
        if self._prev_sign == 0:
            self._prev_sign = sign
            return
        if sign < 0 and self._prev_sign > 0 and self.Position >= 0:
            self.SellMarket()
        elif sign > 0 and self._prev_sign < 0 and self.Position <= 0:
            self.BuyMarket()
        self._prev_sign = sign

    def CreateClone(self):
        return go_candle_body_reversal_strategy()
