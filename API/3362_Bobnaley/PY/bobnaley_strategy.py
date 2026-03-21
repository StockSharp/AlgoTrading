import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class bobnaley_strategy(Strategy):
    def __init__(self):
        super(bobnaley_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ema_period = self.Param("EmaPeriod", 14) \
            .SetDisplay("EMA Period", "EMA period", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._prev_close = None
        self._prev_ema = None

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def ema_period(self):
        return self._ema_period.Value
    @property
    def atr_period(self):
        return self._atr_period.Value

    def OnReseted(self):
        super(bobnaley_strategy, self).OnReseted()
        self._prev_close = None
        self._prev_ema = None

    def OnStarted(self, time):
        super(bobnaley_strategy, self).OnStarted(time)
        self._prev_close = None
        self._prev_ema = None
        ema = ExponentialMovingAverage()
        ema.Length = self.ema_period
        atr = AverageTrueRange()
        atr.Length = self.atr_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, atr, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def OnProcess(self, candle, ema_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)

        if self._prev_close is not None and self._prev_ema is not None:
            cross_up = self._prev_close <= self._prev_ema and close > float(ema_val)
            cross_down = self._prev_close >= self._prev_ema and close < float(ema_val)

            if cross_up and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = float(ema_val)

    def CreateClone(self):
        return bobnaley_strategy()
