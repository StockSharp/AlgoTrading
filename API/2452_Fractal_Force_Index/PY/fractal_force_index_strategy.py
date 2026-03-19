import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class fractal_force_index_strategy(Strategy):
    """
    Fractal Force Index: EMA crossover momentum strategy.
    Opens positions when close crosses above/below EMA.
    """

    def __init__(self):
        super(fractal_force_index_strategy, self).__init__()
        self._period = self.Param("Period", 21) \
            .SetDisplay("Period", "EMA length", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle Type", "Timeframe for indicator", "General")

        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fractal_force_index_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted(self, time):
        super(fractal_force_index_strategy, self).OnStarted(time)

        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self._period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        ema = float(ema_value)

        if self._has_prev:
            crossed_above = self._prev_close <= self._prev_ema and close > ema
            crossed_below = self._prev_close >= self._prev_ema and close < ema

            if crossed_above and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif crossed_below and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema
        self._has_prev = True

    def CreateClone(self):
        return fractal_force_index_strategy()
