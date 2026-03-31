import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class martingail_expert_strategy(Strategy):
    def __init__(self):
        super(martingail_expert_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")
        self._buy_level = self.Param("BuyLevel", 35.0) \
            .SetDisplay("Buy Level", "RSI level for longs", "Logic")
        self._sell_level = self.Param("SellLevel", 65.0) \
            .SetDisplay("Sell Level", "RSI level for shorts", "Logic")

        self._prev_rsi = 50.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @property
    def BuyLevel(self):
        return self._buy_level.Value

    @property
    def SellLevel(self):
        return self._sell_level.Value

    def OnReseted(self):
        super(martingail_expert_strategy, self).OnReseted()
        self._prev_rsi = 50.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(martingail_expert_strategy, self).OnStarted2(time)
        self._prev_rsi = 50.0
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        rv = float(rsi_value)
        if not self._has_prev:
            self._prev_rsi = rv
            self._has_prev = True
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_rsi = rv
            return
        if self._prev_rsi < self.BuyLevel and rv >= self.BuyLevel and self.Position <= 0:
            self.BuyMarket()
        elif self._prev_rsi > self.SellLevel and rv <= self.SellLevel and self.Position >= 0:
            self.SellMarket()
        self._prev_rsi = rv

    def CreateClone(self):
        return martingail_expert_strategy()
