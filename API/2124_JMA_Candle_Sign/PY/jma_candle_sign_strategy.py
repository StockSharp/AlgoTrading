import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import JurikMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class jma_candle_sign_strategy(Strategy):
    def __init__(self):
        super(jma_candle_sign_strategy, self).__init__()
        self._jma_length = self.Param("JmaLength", 7) \
            .SetDisplay("JMA Length", "Period for Jurik moving averages", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "Parameters")
        self._jma_open = None
        self._jma_close = None
        self._prev_open_jma = 0.0
        self._prev_close_jma = 0.0
        self._has_prev = False

    @property
    def jma_length(self):
        return self._jma_length.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(jma_candle_sign_strategy, self).OnReseted()
        self._jma_open = None
        self._jma_close = None
        self._prev_open_jma = 0.0
        self._prev_close_jma = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(jma_candle_sign_strategy, self).OnStarted(time)
        self._jma_open = JurikMovingAverage()
        self._jma_open.Length = self.jma_length
        self._jma_close = JurikMovingAverage()
        self._jma_close.Length = self.jma_length
        self._prev_open_jma = 0.0
        self._prev_close_jma = 0.0
        self._has_prev = False
        self.Indicators.Add(self._jma_open)
        self.Indicators.Add(self._jma_close)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        open_input = DecimalIndicatorValue(self._jma_open, candle.OpenPrice, candle.OpenTime)
        open_input.IsFinal = True
        open_result = self._jma_open.Process(open_input)
        close_input = DecimalIndicatorValue(self._jma_close, candle.ClosePrice, candle.OpenTime)
        close_input.IsFinal = True
        close_result = self._jma_close.Process(close_input)
        if not open_result.IsFormed or not close_result.IsFormed:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        open_jma = float(open_result)
        close_jma = float(close_result)
        if not self._has_prev:
            self._prev_open_jma = open_jma
            self._prev_close_jma = close_jma
            self._has_prev = True
            return
        # JMA(close) crosses above JMA(open) - bullish
        if self._prev_close_jma <= self._prev_open_jma and close_jma > open_jma and self.Position <= 0:
            self.BuyMarket()
        # JMA(close) crosses below JMA(open) - bearish
        elif self._prev_close_jma >= self._prev_open_jma and close_jma < open_jma and self.Position >= 0:
            self.SellMarket()
        self._prev_open_jma = open_jma
        self._prev_close_jma = close_jma

    def CreateClone(self):
        return jma_candle_sign_strategy()
