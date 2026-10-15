import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import RelativeStrengthIndex, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ama_trader2_strategy(Strategy):
    def __init__(self):
        super(ama_trader2_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._rsi_length = self.Param("RsiLength", 14)
        self._ema_length = self.Param("EmaLength", 50)
        self._rsi_level_up = self.Param("RsiLevelUp", 60.0)
        self._rsi_level_down = self.Param("RsiLevelDown", 40.0)

        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiLength(self):
        return self._rsi_length.Value

    @RsiLength.setter
    def RsiLength(self, value):
        self._rsi_length.Value = value

    @property
    def EmaLength(self):
        return self._ema_length.Value

    @EmaLength.setter
    def EmaLength(self, value):
        self._ema_length.Value = value

    @property
    def RsiLevelUp(self):
        return self._rsi_level_up.Value

    @RsiLevelUp.setter
    def RsiLevelUp(self, value):
        self._rsi_level_up.Value = value

    @property
    def RsiLevelDown(self):
        return self._rsi_level_down.Value

    @RsiLevelDown.setter
    def RsiLevelDown(self, value):
        self._rsi_level_down.Value = value

    def OnReseted(self):
        super(ama_trader2_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(ama_trader2_strategy, self).OnStarted2(time)
        self._prev_close = 0.0
        self._prev_ema = 0.0
        self._has_prev = False

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiLength
        ema = ExponentialMovingAverage()
        ema.Length = self.EmaLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, ema, self._process_candle).Start()

    def _process_candle(self, candle, rsi_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        rsi_val = float(rsi_value)
        ema_val = float(ema_value)

        if self._has_prev:
            price_above_ema = close > ema_val
            prev_below_ema = self._prev_close < self._prev_ema

            if price_above_ema and prev_below_ema and rsi_val > float(self.RsiLevelUp) and self.Position <= 0:
                self.BuyMarket()
            elif not price_above_ema and not prev_below_ema and rsi_val < float(self.RsiLevelDown) and self.Position >= 0:
                self.SellMarket()

        self._prev_close = close
        self._prev_ema = ema_val
        self._has_prev = True

    def CreateClone(self):
        return ama_trader2_strategy()
