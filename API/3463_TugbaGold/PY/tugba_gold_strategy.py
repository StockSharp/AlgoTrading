import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class tugba_gold_strategy(Strategy):
    def __init__(self):
        super(tugba_gold_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._ema_period = self.Param("EmaPeriod", 50)

        self._was_bullish_signal = False
        self._has_prev_signal = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    def OnReseted(self):
        super(tugba_gold_strategy, self).OnReseted()
        self._was_bullish_signal = False
        self._has_prev_signal = False

    def OnStarted2(self, time):
        super(tugba_gold_strategy, self).OnStarted2(time)
        self._was_bullish_signal = False
        self._has_prev_signal = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, self._process_candle).Start()

    def _process_candle(self, candle, ema_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        ema_val = float(ema_value)

        bullish = close > open_price
        bearish = close < open_price
        bullish_signal = bullish and close > ema_val
        bearish_signal = bearish and close < ema_val

        crossed_up = bullish_signal and (not self._has_prev_signal or not self._was_bullish_signal)
        crossed_down = bearish_signal and (not self._has_prev_signal or self._was_bullish_signal)

        if crossed_up and self.Position <= 0:
            self.BuyMarket()
        elif crossed_down and self.Position >= 0:
            self.SellMarket()

        if bullish_signal or bearish_signal:
            self._was_bullish_signal = bullish_signal
            self._has_prev_signal = True

    def CreateClone(self):
        return tugba_gold_strategy()
