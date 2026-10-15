import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy


class disturbed_strategy(Strategy):
    def __init__(self):
        super(disturbed_strategy, self).__init__()
        self._ema_period = self.Param("EmaPeriod", 12) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def ema_period(self):
        return self._ema_period.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(disturbed_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(disturbed_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = StandardDeviation()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.on_process).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def on_process(self, candle, ema, atr):
        if candle.State != CandleStates.Finished:
            return
        if not self._has_prev:
            self._prev_ema = ema
            self._has_prev = True
            return
        close = candle.ClosePrice
        # Price crosses above EMA + ATR => buy
        if close > ema + atr and self._prev_ema > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Price crosses below EMA - ATR => sell
        elif close < ema - atr and self._prev_ema > 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        # Exit long when price returns to EMA
        elif self.Position > 0 and close <= ema:
            self.SellMarket()
        # Exit short when price returns to EMA
        elif self.Position < 0 and close >= ema:
            self.BuyMarket()
        self._prev_ema = ema

    def CreateClone(self):
        return disturbed_strategy()
