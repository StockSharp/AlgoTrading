import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class universal_investor_strategy(Strategy):
    def __init__(self):
        super(universal_investor_strategy, self).__init__()
        self._moving_period = self.Param("MovingPeriod", 23) \
            .SetDisplay("Moving Period", "Smoothing period for EMA and WMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles for calculations", "General")
        self._prev_ema = 0.0
        self._prev_lwma = 0.0
        self._has_prev = False

    @property
    def moving_period(self):
        return self._moving_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(universal_investor_strategy, self).OnReseted()
        self._prev_ema = 0.0
        self._prev_lwma = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(universal_investor_strategy, self).OnStarted2(time)
        ema = ExponentialMovingAverage()
        ema.Length = self.moving_period
        lwma = WeightedMovingAverage()
        lwma.Length = self.moving_period
        self.SubscribeCandles(self.candle_type).Bind(ema, lwma, self.process_candle).Start()

    def process_candle(self, candle, ema_value, lwma_value):
        if candle.State != CandleStates.Finished:
            return

        ev = float(ema_value)
        lv = float(lwma_value)

        if not self._has_prev:
            self._prev_ema = ev
            self._prev_lwma = lv
            self._has_prev = True
            return

        open_buy = lv > ev and lv > self._prev_lwma and ev > self._prev_ema
        open_sell = lv < ev and lv < self._prev_lwma and ev < self._prev_ema
        close_buy = lv < ev
        close_sell = lv > ev

        if self.Position > 0 and close_buy:
            self.SellMarket()
        elif self.Position < 0 and close_sell:
            self.BuyMarket()
        elif self.Position == 0:
            if open_buy and not close_buy:
                self.BuyMarket()
            elif open_sell and not close_sell:
                self.SellMarket()

        self._prev_ema = ev
        self._prev_lwma = lv

    def CreateClone(self):
        return universal_investor_strategy()
