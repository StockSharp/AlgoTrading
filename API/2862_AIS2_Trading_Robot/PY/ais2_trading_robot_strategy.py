import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy


class ais2_trading_robot_strategy(Strategy):
    def __init__(self):
        super(ais2_trading_robot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Source candles", "General")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period for volatility", "Indicators")
        self._breakout_threshold = self.Param("BreakoutThreshold", 0.85) \
            .SetDisplay("Breakout Threshold", "Candle body ratio threshold (0-1)", "Signals")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @property
    def BreakoutThreshold(self):
        return self._breakout_threshold.Value

    def OnStarted(self, time):
        super(ais2_trading_robot_strategy, self).OnStarted(time)

        subscription = self.SubscribeCandles(self.CandleType)
        subscription \
            .Bind(self._on_process) \
            .Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        rng = high - low

        if rng <= 0:
            return

        body_ratio = (close - low) / rng
        threshold = float(self.BreakoutThreshold)

        if body_ratio > threshold and close > open_p and self.Position <= 0:
            self.BuyMarket()
        elif body_ratio < (1.0 - threshold) and close < open_p and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return ais2_trading_robot_strategy()
