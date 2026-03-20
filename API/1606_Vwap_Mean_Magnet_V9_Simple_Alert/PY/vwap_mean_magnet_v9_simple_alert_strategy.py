import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, VolumeWeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class vwap_mean_magnet_v9_simple_alert_strategy(Strategy):
    def __init__(self):
        super(vwap_mean_magnet_v9_simple_alert_strategy, self).__init__()

    def OnStarted(self, time):
        super(vwap_mean_magnet_v9_simple_alert_strategy, self).OnStarted(time)
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
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def on_process(self, candle, vwap_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if candle.ClosePrice < vwap_value and rsi_value < self.rsi_oversold and self.Position <= 0:
            self.BuyMarket()
        elif candle.ClosePrice > vwap_value and rsi_value > self.rsi_overbought and self.Position >= 0:
            self.SellMarket()
        if self.Position > 0 and candle.ClosePrice >= vwap_value:
            self.SellMarket()
        elif self.Position < 0 and candle.ClosePrice <= vwap_value:
            self.BuyMarket()

    def CreateClone(self):
        return vwap_mean_magnet_v9_simple_alert_strategy()
