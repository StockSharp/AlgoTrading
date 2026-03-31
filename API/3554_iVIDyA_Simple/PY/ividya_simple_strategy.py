import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ChandeMomentumOscillator
from StockSharp.Algo.Strategies import Strategy

class ividya_simple_strategy(Strategy):
    def __init__(self):
        super(ividya_simple_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._cmo_period = self.Param("CmoPeriod", 20)
        self._ema_period = self.Param("EmaPeriod", 30)

        self._prev_vidya = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def CmoPeriod(self):
        return self._cmo_period.Value

    @CmoPeriod.setter
    def CmoPeriod(self, value):
        self._cmo_period.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    def OnReseted(self):
        super(ividya_simple_strategy, self).OnReseted()
        self._prev_vidya = 0.0
        self._prev_close = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(ividya_simple_strategy, self).OnStarted2(time)
        self._prev_vidya = 0.0
        self._prev_close = 0.0
        self._has_prev = False

        cmo = ChandeMomentumOscillator()
        cmo.Length = self.CmoPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(cmo, self._process_candle).Start()

    def _process_candle(self, candle, cmo_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        cmo_val = float(cmo_value)

        # VIDYA = alpha * |CMO/100| * price + (1 - alpha * |CMO/100|) * prevVidya
        alpha = 2.0 / (self.EmaPeriod + 1.0)
        momentum_factor = abs(cmo_val) / 100.0
        sf = alpha * momentum_factor

        prev_vidya = self._prev_vidya if self._has_prev else close
        current_vidya = sf * close + (1.0 - sf) * prev_vidya

        if self._has_prev:
            cross_up = self._prev_close <= self._prev_vidya and close > current_vidya
            cross_down = self._prev_close >= self._prev_vidya and close < current_vidya
            min_distance = close * 0.001

            if cross_up and abs(close - current_vidya) >= min_distance and self.Position <= 0:
                self.BuyMarket()
            elif cross_down and abs(close - current_vidya) >= min_distance and self.Position >= 0:
                self.SellMarket()

        self._prev_vidya = current_vidya
        self._prev_close = close
        self._has_prev = True

    def CreateClone(self):
        return ividya_simple_strategy()
