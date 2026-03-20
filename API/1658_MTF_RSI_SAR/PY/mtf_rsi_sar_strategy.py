import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mtf_rsi_sar_strategy(Strategy):
    def __init__(self):
        super(mtf_rsi_sar_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(mtf_rsi_sar_strategy, self).OnStarted(time)
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        sar = ParabolicSar()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, sar, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, rsi, sar):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        # Buy: RSI oversold + SAR below price
        if rsi < self.rsi_oversold and sar < close:
            if self.Position < 0:
                self.BuyMarket()
            if self.Position <= 0:
                self.BuyMarket()
        # Sell: RSI overbought + SAR above price
        elif rsi > self.rsi_overbought and sar > close:
            if self.Position > 0:
                self.SellMarket()
            if self.Position >= 0:
                self.SellMarket()
        # Exit long when RSI overbought
        elif self.Position > 0 and rsi > self.rsi_overbought:
            self.SellMarket()
        # Exit short when RSI oversold
        elif self.Position < 0 and rsi < self.rsi_oversold:
            self.BuyMarket()

    def CreateClone(self):
        return mtf_rsi_sar_strategy()
