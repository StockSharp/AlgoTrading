import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class cm_rsi_strategy(Strategy):
    def __init__(self):
        super(cm_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI calculation period", "Indicators")
        self._buy_level = self.Param("BuyLevel", 30.0) \
            .SetDisplay("Buy Level", "RSI level to enter long", "Indicators")
        self._sell_level = self.Param("SellLevel", 70.0) \
            .SetDisplay("Sell Level", "RSI level to enter short", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_rsi = 0.0
        self._is_first = True

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def buy_level(self):
        return self._buy_level.Value

    @property
    def sell_level(self):
        return self._sell_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cm_rsi_strategy, self).OnReseted()
        self._prev_rsi = 0.0
        self._is_first = True

    def OnStarted2(self, time):
        super(cm_rsi_strategy, self).OnStarted2(time)
        self._is_first = True
        self._prev_rsi = 0.0
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rsi_value = float(rsi_value)
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rsi_value
            self._is_first = False
            return
        if self._is_first:
            self._prev_rsi = rsi_value
            self._is_first = False
            return
        buy_level = float(self.buy_level)
        sell_level = float(self.sell_level)
        if self._prev_rsi < buy_level and rsi_value > buy_level and self.Position <= 0:
            self.BuyMarket()
        if self._prev_rsi > sell_level and rsi_value < sell_level and self.Position >= 0:
            self.SellMarket()
        self._prev_rsi = rsi_value

    def CreateClone(self):
        return cm_rsi_strategy()
