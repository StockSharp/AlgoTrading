import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class basic_ma_template_strategy(Strategy):
    def __init__(self):
        super(basic_ma_template_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 49) \
            .SetDisplay("MA Period", "Moving average period", "Indicators")
        self._prev_open = None
        self._prev_close = None

    @property
    def candle_type(self):
        return self._candle_type.Value
    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def ma_period(self):
        return self._ma_period.Value
    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    def OnReseted(self):
        super(basic_ma_template_strategy, self).OnReseted()
        self._prev_open = None
        self._prev_close = None

    def OnStarted(self, time):
        super(basic_ma_template_strategy, self).OnStarted(time)
        self._prev_open = None
        self._prev_close = None
        sma = SimpleMovingAverage()
        sma.Length = self.ma_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if self._prev_open is not None and self._prev_close is not None:
            if self._prev_open > sma_value and self._prev_close < sma_value and self.Position >= 0:
                self.SellMarket()
            elif self._prev_open < sma_value and self._prev_close > sma_value and self.Position <= 0:
                self.BuyMarket()

        self._prev_open = float(candle.OpenPrice)
        self._prev_close = float(candle.ClosePrice)

    def CreateClone(self):
        return basic_ma_template_strategy()
