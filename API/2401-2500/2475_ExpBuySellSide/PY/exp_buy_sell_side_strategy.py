import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class exp_buy_sell_side_strategy(Strategy):
    def __init__(self):
        super(exp_buy_sell_side_strategy, self).__init__()

        self._atr_period = self.Param("AtrPeriod", 5)
        self._atr_multiplier = self.Param("AtrMultiplier", 2.5)
        self._fast_period = self.Param("FastPeriod", 5)
        self._slow_period = self.Param("SlowPeriod", 30)
        self._close_by_opposite_signal = self.Param("CloseByOppositeSignal", True)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._prev_diff = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._atr_trend = 0

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
    def CloseByOppositeSignal(self):
        return self._close_by_opposite_signal.Value

    @CloseByOppositeSignal.setter
    def CloseByOppositeSignal(self, value):
        self._close_by_opposite_signal.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(exp_buy_sell_side_strategy, self).OnStarted2(time)

        self._prev_diff = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._atr_trend = 0

        atr = AverageTrueRange()
        atr.Length = self.AtrPeriod
        fast = SimpleMovingAverage()
        fast.Length = self.FastPeriod
        slow = SimpleMovingAverage()
        slow.Length = self.SlowPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast, slow, atr, self.ProcessCandle).Start()

        self.StartProtection(None, None)

    def ProcessCandle(self, candle, fast_value, slow_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        fast = float(fast_value)
        slow = float(slow_value)
        atr = float(atr_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        mult = float(self.AtrMultiplier)

        diff = fast - slow
        step_signal = 0
        if fast > slow and diff > self._prev_diff:
            step_signal = 1
        elif fast < slow and diff < self._prev_diff:
            step_signal = -1

        self._prev_diff = diff

        upper = high - mult * atr
        lower = low + mult * atr
        atr_signal = 0

        if close > self._prev_upper and self._atr_trend <= 0:
            self._atr_trend = 1
            atr_signal = 1
        elif close < self._prev_lower and self._atr_trend >= 0:
            self._atr_trend = -1
            atr_signal = -1

        self._prev_upper = upper
        self._prev_lower = lower

        trade_signal = 0
        if atr_signal == 1 and step_signal == 1:
            trade_signal = 1
        elif atr_signal == -1 and step_signal == -1:
            trade_signal = -1

        pos = float(self.Position)
        vol = float(self.Volume)

        if trade_signal == 1:
            if self.CloseByOppositeSignal and pos < 0:
                self.BuyMarket(vol + abs(pos))
            elif pos <= 0:
                self.BuyMarket(vol + abs(pos))
        elif trade_signal == -1:
            if self.CloseByOppositeSignal and pos > 0:
                self.SellMarket(vol + abs(pos))
            elif pos >= 0:
                self.SellMarket(vol + abs(pos))

    def OnReseted(self):
        super(exp_buy_sell_side_strategy, self).OnReseted()
        self._prev_diff = 0.0
        self._prev_upper = 0.0
        self._prev_lower = 0.0
        self._atr_trend = 0

    def CreateClone(self):
        return exp_buy_sell_side_strategy()
