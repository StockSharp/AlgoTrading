import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class avalanche_strategy(Strategy):
    def __init__(self):
        super(avalanche_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 20) \
            .SetDisplay("EMA Period", "EMA period for equilibrium", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "RSI period", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def ema_period(self):
        return self._ema_period.Value

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

    def OnReseted(self):
        super(avalanche_strategy, self).OnReseted()

    def OnStarted2(self, time):
        super(avalanche_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, rsi, self.process_candle).Start()

    def process_candle(self, candle, ema_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ema_val = float(ema_value)
        rsi_val = float(rsi_value)
        if close < ema_val and rsi_val <= self.rsi_oversold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif close > ema_val and rsi_val >= self.rsi_overbought and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return avalanche_strategy()
