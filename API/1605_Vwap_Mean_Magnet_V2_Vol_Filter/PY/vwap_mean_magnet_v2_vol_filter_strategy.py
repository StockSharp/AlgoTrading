import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vwap_mean_magnet_v2_vol_filter_strategy(Strategy):
    def __init__(self):
        super(vwap_mean_magnet_v2_vol_filter_strategy, self).__init__()
        self._vwap_length = self.Param("VwapLength", 20) \
            .SetDisplay("VWAP Length", "VWAP Length", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI Length", "General")
        self._rsi_overbought = self.Param("RsiOverbought", 65) \
            .SetDisplay("RSI Overbought", "RSI Overbought", "General")
        self._rsi_oversold = self.Param("RsiOversold", 35) \
            .SetDisplay("RSI Oversold", "RSI Oversold", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle Type", "General")

    @property
    def vwap_length(self):
        return self._vwap_length.Value

    @property
    def rsi_length(self):
        return self._rsi_length.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(vwap_mean_magnet_v2_vol_filter_strategy, self).OnStarted(time)
        vwap = VolumeWeightedMovingAverage()
        vwap.Length = self.vwap_length
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_length
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwap, rsi, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwap)
            self.DrawOwnTrades(area)

    def on_process(self, candle, vwap_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        # Mean reversion: buy below VWAP with oversold RSI, sell above VWAP with overbought RSI
        if candle.ClosePrice < vwap_value and rsi_value < self.rsi_oversold and self.Position <= 0:
            self.BuyMarket()
        elif candle.ClosePrice > vwap_value and rsi_value > self.rsi_overbought and self.Position >= 0:
            self.SellMarket()
        # Exit on VWAP reversion
        if self.Position > 0 and candle.ClosePrice >= vwap_value:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice <= vwap_value:
            self.BuyMarket()

    def CreateClone(self):
        return vwap_mean_magnet_v2_vol_filter_strategy()
