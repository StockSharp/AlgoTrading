import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BullPower, BearPower
from StockSharp.Algo.Strategies import Strategy


class rpm5_bulls_bears_eyes_strategy(Strategy):
    """RPM5 Bulls Bears Eyes strategy using Bull/Bear power with EMA filter.
    Buy when price > EMA, bull power positive, bear power recovering.
    Sell when price < EMA, bear power negative, bull power declining."""

    def __init__(self):
        super(rpm5_bulls_bears_eyes_strategy, self).__init__()

        self._ema_period = self.Param("EmaPeriod", 13) \
            .SetDisplay("EMA Period", "EMA trend period", "Indicators")
        self._power_period = self.Param("PowerPeriod", 13) \
            .SetDisplay("Power Period", "Bulls/Bears power period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @property
    def PowerPeriod(self):
        return self._power_period.Value

    def OnReseted(self):
        super(rpm5_bulls_bears_eyes_strategy, self).OnReseted()
        self._prev_bull = 0.0
        self._prev_bear = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(rpm5_bulls_bears_eyes_strategy, self).OnStarted2(time)

        self._has_prev = False

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        bulls = BullPower()
        bulls.Length = self.PowerPeriod
        bears = BearPower()
        bears.Length = self.PowerPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, bulls, bears, self._process_candle).Start()

    def _process_candle(self, candle, ema_value, bull_value, bear_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        bull_val = float(bull_value)
        bear_val = float(bear_value)
        close = float(candle.ClosePrice)

        if not self._has_prev:
            self._prev_bull = bull_val
            self._prev_bear = bear_val
            self._has_prev = True
            return

        # Long: price above EMA, bull power positive, bear power recovering from negative
        long_signal = close > ema_val and bull_val > 0 and self._prev_bear < 0 and bear_val > self._prev_bear
        # Short: price below EMA, bear power negative, bull power declining from positive
        short_signal = close < ema_val and bear_val < 0 and self._prev_bull > 0 and bull_val < self._prev_bull

        if self.Position <= 0 and long_signal:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif self.Position >= 0 and short_signal:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_bull = bull_val
        self._prev_bear = bear_val

    def CreateClone(self):
        return rpm5_bulls_bears_eyes_strategy()
