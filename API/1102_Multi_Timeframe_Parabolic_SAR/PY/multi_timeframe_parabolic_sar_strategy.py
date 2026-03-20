import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class multi_timeframe_parabolic_sar_strategy(Strategy):
    def __init__(self):
        super(multi_timeframe_parabolic_sar_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA", "EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI", "RSI period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(2))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(multi_timeframe_parabolic_sar_strategy, self).OnReseted()

    def OnStarted(self, time):
        super(multi_timeframe_parabolic_sar_strategy, self).OnStarted(time)
        self._ema = ExponentialMovingAverage()
        self._ema.Length = self._ema_length.Value
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self._rsi_length.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._ema, self._rsi, self.OnProcess).Start()

    def OnProcess(self, candle, ema_val, rsi_val):
        if candle.State != CandleStates.Finished:
            return
        if not self._ema.IsFormed or not self._rsi.IsFormed:
            return
        ev = float(ema_val)
        rv = float(rsi_val)
        close = float(candle.ClosePrice)
        if close > ev and rv > 50.0 and self.Position <= 0:
            self.BuyMarket()
        elif close < ev and rv < 50.0 and self.Position > 0:
            self.SellMarket()

    def CreateClone(self):
        return multi_timeframe_parabolic_sar_strategy()
