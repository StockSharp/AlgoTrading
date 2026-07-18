import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class bb_squeeze_strategy(Strategy):
    def __init__(self):
        super(bb_squeeze_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period of Bollinger Bands", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used", "General")
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False

    @property
    def bollinger_period(self):
        return self._bollinger_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bb_squeeze_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(bb_squeeze_strategy, self).OnStarted2(time)
        bb = BollingerBands()
        bb.Length = self.bollinger_period
        bb.Width = 2.0
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def on_process(self, candle, value):
        if candle.State != CandleStates.Finished:
            return
        upper = float(value.UpBand)
        lower = float(value.LowBand)
        middle = float(value.MovingAverage)
        if upper == 0 or lower == 0:
            return
        close = float(candle.ClosePrice)
        if not self._has_prev:
            self._prev_close = close
            self._prev_upper = upper
            self._prev_lower = lower
            self._has_prev = True
            return
        # Cross above upper band => buy
        if self._prev_close <= self._prev_upper and close > upper and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Cross below lower band => sell
        elif self._prev_close >= self._prev_lower and close < lower and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long at middle
        elif self.Position > 0 and close < middle:
            self.SellMarket()
        # Exit short at middle
        elif self.Position < 0 and close > middle:
            self.BuyMarket()
        self._prev_close = close
        self._prev_upper = upper
        self._prev_lower = lower

    def CreateClone(self):
        return bb_squeeze_strategy()
