import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class ma_macd_bb_back_tester_strategy(Strategy):
    def __init__(self):
        super(ma_macd_bb_back_tester_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._ma_length = self.Param("MaLength", 20) \
            .SetDisplay("MA Length", "MA period", "Indicators")
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._initialized = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(ma_macd_bb_back_tester_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._initialized = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(ma_macd_bb_back_tester_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._initialized = False
        self._cooldown = 0
        self._ma = ExponentialMovingAverage()
        self._ma.Length = self._ma_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ma, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ma.IsFormed:
            return
        mv = float(ma_val)
        close = float(candle.ClosePrice)
        if not self._initialized:
            self._prev_close = close
            self._prev_ma = mv
            self._initialized = True
            return
        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            self._prev_ma = mv
            return
        cross_up = self._prev_close <= self._prev_ma and close > mv
        cross_down = self._prev_close >= self._prev_ma and close < mv
        if cross_up and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown = 10
        elif cross_down and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown = 10
        self._prev_close = close
        self._prev_ma = mv

    def CreateClone(self):
        return ma_macd_bb_back_tester_strategy()
