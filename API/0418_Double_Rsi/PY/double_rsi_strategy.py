import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *

class double_rsi_strategy(Strategy):
    """Multi-timeframe RSI strategy."""
    def __init__(self):
        super(double_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", tf(5)).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._rsi_length = self.Param("RSILength", 14).SetDisplay("RSI Length", "RSI period", "RSI")
        self._mtf_timeframe = self.Param("MTFTimeframe", tf(15)).SetDisplay("MTF Timeframe", "Higher timeframe", "Multi Timeframe RSI")
        self._use_tp = self.Param("UseTP", False).SetDisplay("Use Take Profit", "Enable take profit", "Take Profit")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(double_rsi_strategy, self).OnStarted(time)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.OnProcess).Start()

        mtf_sub = self.SubscribeCandles(self._mtf_timeframe.Value)
        mtf_sub.Bind(self.OnProcessMtf).Start()

    def OnProcess(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement strategy logic for main timeframe

    def OnProcessMtf(self, candle):
        if candle.State != CandleStates.Finished:
            return
        # TODO: implement logic for higher timeframe

    def CreateClone(self):
        return double_rsi_strategy()
