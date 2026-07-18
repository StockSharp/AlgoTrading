import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, BollingerBands
from StockSharp.Algo.Strategies import Strategy


class rsi_bollinger_bands_ea_strategy(Strategy):
    def __init__(self):
        super(rsi_bollinger_bands_ea_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI length", "Indicators")
        self._bb_period = self.Param("BbPeriod", 20) \
            .SetDisplay("BB Period", "Bollinger Bands length", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35.0) \
            .SetDisplay("RSI Oversold", "Oversold level for buy signal", "Signals")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0) \
            .SetDisplay("RSI Overbought", "Overbought level for sell signal", "Signals")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def BbPeriod(self):
        return self._bb_period.Value

    @property
    def RsiOversold(self):
        return self._rsi_oversold.Value

    @property
    def RsiOverbought(self):
        return self._rsi_overbought.Value

    def OnStarted2(self, time):
        super(rsi_bollinger_bands_ea_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        bb = BollingerBands()
        bb.Length = self.BbPeriod
        bb.Width = 2

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .BindEx(rsi, bb, self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, rsi_value, bb_value):
        if candle.State != CandleStates.Finished:
            return

        rsi_val = float(rsi_value)
        bb_upper = float(bb_value.UpBand)
        bb_lower = float(bb_value.LowBand)
        close = float(candle.ClosePrice)

        if rsi_val < float(self.RsiOversold) and close <= bb_lower and self.Position <= 0:
            self.BuyMarket()
        elif rsi_val > float(self.RsiOverbought) and close >= bb_upper and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return rsi_bollinger_bands_ea_strategy()
