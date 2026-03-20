import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class master_exit_plan_strategy(Strategy):
    def __init__(self):
        super(master_exit_plan_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(60)))
        self._fast_period = self.Param("FastPeriod", 20)
        self._slow_period = self.Param("SlowPeriod", 60)
        self._atr_period = self.Param("AtrPeriod", 14)
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0)

        self._entry_price = 0.0
        self._trail_stop = 0.0
        self._was_bullish = False
        self._has_trend_state = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @FastPeriod.setter
    def FastPeriod(self, value):
        self._fast_period.Value = value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @SlowPeriod.setter
    def SlowPeriod(self, value):
        self._slow_period.Value = value

    @property
    def AtrPeriod(self):
        return self._atr_period.Value

    @AtrPeriod.setter
    def AtrPeriod(self, value):
        self._atr_period.Value = value

    @property
    def AtrMultiplier(self):
        return self._atr_multiplier.Value

    @AtrMultiplier.setter
    def AtrMultiplier(self, value):
        self._atr_multiplier.Value = value

    def OnReseted(self):
        super(master_exit_plan_strategy, self).OnReseted()
        self._entry_price = 0.0
        self._trail_stop = 0.0
        self._was_bullish = False
        self._has_trend_state = False

    def OnStarted(self, time):
        super(master_exit_plan_strategy, self).OnStarted(time)
        self._entry_price = 0.0
        self._trail_stop = 0.0
        self._was_bullish = False
        self._has_trend_state = False

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

    def _process_candle(self, candle, fast_value, slow_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        rng = float(candle.HighPrice) - float(candle.LowPrice)
        if rng <= 0:
            return

        trail_dist = rng * float(self.AtrMultiplier)
        is_bullish = float(fast_value) > float(slow_value)
        crossed_up = self._has_trend_state and not self._was_bullish and is_bullish
        crossed_down = self._has_trend_state and self._was_bullish and not is_bullish

        if self.Position > 0:
            new_stop = close - trail_dist
            if new_stop > self._trail_stop:
                self._trail_stop = new_stop
            if close < self._trail_stop:
                self.SellMarket()
                self._trail_stop = 0.0
                self._entry_price = 0.0
                self._was_bullish = is_bullish
                self._has_trend_state = True
                return
            elif crossed_down:
                self.SellMarket()
                self._trail_stop = 0.0
                self._entry_price = 0.0
                self._was_bullish = is_bullish
                self._has_trend_state = True
                return
        elif self.Position < 0:
            new_stop = close + trail_dist
            if self._trail_stop == 0 or new_stop < self._trail_stop:
                self._trail_stop = new_stop
            if close > self._trail_stop:
                self.BuyMarket()
                self._trail_stop = 0.0
                self._entry_price = 0.0
                self._was_bullish = is_bullish
                self._has_trend_state = True
                return
            elif crossed_up:
                self.BuyMarket()
                self._trail_stop = 0.0
                self._entry_price = 0.0
                self._was_bullish = is_bullish
                self._has_trend_state = True
                return

        if self.Position == 0:
            if crossed_up:
                self.BuyMarket()
                self._entry_price = close
                self._trail_stop = close - trail_dist
            elif crossed_down:
                self.SellMarket()
                self._entry_price = close
                self._trail_stop = close + trail_dist

        self._was_bullish = is_bullish
        self._has_trend_state = True

    def CreateClone(self):
        return master_exit_plan_strategy()
