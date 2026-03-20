import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class coin_flip_martingale_strategy(Strategy):
    def __init__(self):
        super(coin_flip_martingale_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candles", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    def OnStarted(self, time):
        super(coin_flip_martingale_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(rsi, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rv = float(rsi_value)

        if rv < 35 and self.Position <= 0:
            self.BuyMarket()
        elif rv > 65 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return coin_flip_martingale_strategy()
