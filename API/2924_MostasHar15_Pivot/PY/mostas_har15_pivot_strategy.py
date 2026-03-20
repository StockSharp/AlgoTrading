import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class mostas_har15_pivot_strategy(Strategy):
    def __init__(self):
        super(mostas_har15_pivot_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Trading candles", "General")
        self._daily_candle_type = self.Param("DailyCandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Daily Candle Type", "Higher timeframe for pivots", "General")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetDisplay("RSI Length", "RSI period", "Indicators")

        self._pivot_mid = 0.0
        self._has_pivot = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def DailyCandleType(self):
        return self._daily_candle_type.Value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    def OnReseted(self):
        super(mostas_har15_pivot_strategy, self).OnReseted()
        self._pivot_mid = 0.0
        self._has_pivot = False

    def OnStarted(self, time):
        super(mostas_har15_pivot_strategy, self).OnStarted(time)
        self._pivot_mid = 0.0
        self._has_pivot = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength

        daily_sub = self.SubscribeCandles(self.DailyCandleType)
        daily_sub.Bind(self._on_daily_candle).Start()

        trade_sub = self.SubscribeCandles(self.CandleType)
        trade_sub.Bind(rsi, self._on_trade_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, trade_sub)
            self.DrawOwnTrades(area)

    def _on_daily_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        self._pivot_mid = (high + low + close) / 3.0
        self._has_pivot = True

    def _on_trade_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if not self._has_pivot:
            return
        close = float(candle.ClosePrice)
        rv = float(rsi_value)
        if close > self._pivot_mid and rv > 55 and self.Position <= 0:
            self.BuyMarket()
        elif close < self._pivot_mid and rv < 45 and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return mostas_har15_pivot_strategy()
