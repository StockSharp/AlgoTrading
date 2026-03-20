import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class color_zerolag_momentum_x2_strategy(Strategy):
    def __init__(self):
        super(color_zerolag_momentum_x2_strategy, self).__init__()

        self._trend_candle_type = self.Param("TrendCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._trend_momentum_period = self.Param("TrendMomentumPeriod", 14)
        self._trend_ma_length = self.Param("TrendMaLength", 5)
        self._signal_candle_type = self.Param("SignalCandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._signal_momentum_period = self.Param("SignalMomentumPeriod", 20)
        self._signal_ma_length = self.Param("SignalMaLength", 8)
        self._buy_pos_open = self.Param("BuyPosOpen", True)
        self._sell_pos_open = self.Param("SellPosOpen", True)
        self._buy_pos_close = self.Param("BuyPosClose", True)
        self._sell_pos_close = self.Param("SellPosClose", True)

        self._trend = 0
        self._prev_signal_momentum = None
        self._prev_signal_ma = None

    @property
    def TrendCandleType(self):
        return self._trend_candle_type.Value

    @TrendCandleType.setter
    def TrendCandleType(self, value):
        self._trend_candle_type.Value = value

    @property
    def TrendMomentumPeriod(self):
        return self._trend_momentum_period.Value

    @TrendMomentumPeriod.setter
    def TrendMomentumPeriod(self, value):
        self._trend_momentum_period.Value = value

    @property
    def TrendMaLength(self):
        return self._trend_ma_length.Value

    @TrendMaLength.setter
    def TrendMaLength(self, value):
        self._trend_ma_length.Value = value

    @property
    def SignalCandleType(self):
        return self._signal_candle_type.Value

    @SignalCandleType.setter
    def SignalCandleType(self, value):
        self._signal_candle_type.Value = value

    @property
    def SignalMomentumPeriod(self):
        return self._signal_momentum_period.Value

    @SignalMomentumPeriod.setter
    def SignalMomentumPeriod(self, value):
        self._signal_momentum_period.Value = value

    @property
    def SignalMaLength(self):
        return self._signal_ma_length.Value

    @SignalMaLength.setter
    def SignalMaLength(self, value):
        self._signal_ma_length.Value = value

    @property
    def BuyPosOpen(self):
        return self._buy_pos_open.Value

    @BuyPosOpen.setter
    def BuyPosOpen(self, value):
        self._buy_pos_open.Value = value

    @property
    def SellPosOpen(self):
        return self._sell_pos_open.Value

    @SellPosOpen.setter
    def SellPosOpen(self, value):
        self._sell_pos_open.Value = value

    @property
    def BuyPosClose(self):
        return self._buy_pos_close.Value

    @BuyPosClose.setter
    def BuyPosClose(self, value):
        self._buy_pos_close.Value = value

    @property
    def SellPosClose(self):
        return self._sell_pos_close.Value

    @SellPosClose.setter
    def SellPosClose(self, value):
        self._sell_pos_close.Value = value

    def OnStarted(self, time):
        super(color_zerolag_momentum_x2_strategy, self).OnStarted(time)

        self._trend = 0
        self._prev_signal_momentum = None
        self._prev_signal_ma = None

        self.StartProtection(
            Unit(2.0, UnitTypes.Percent),
            Unit(1.0, UnitTypes.Percent))

        trend_fast = ExponentialMovingAverage()
        trend_fast.Length = self.TrendMaLength
        trend_slow = ExponentialMovingAverage()
        trend_slow.Length = self.TrendMomentumPeriod
        trend_sub = self.SubscribeCandles(self.TrendCandleType)
        trend_sub.Bind(trend_fast, trend_slow, self.ProcessTrend).Start()

        signal_fast = ExponentialMovingAverage()
        signal_fast.Length = self.SignalMaLength
        signal_slow = ExponentialMovingAverage()
        signal_slow.Length = self.SignalMomentumPeriod
        signal_sub = self.SubscribeCandles(self.SignalCandleType)
        signal_sub.Bind(signal_fast, signal_slow, self.ProcessSignal).Start()

    def ProcessTrend(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return
        f = float(fast)
        s = float(slow)
        self._trend = 1 if f > s else -1

    def ProcessSignal(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        f = float(fast)
        s = float(slow)

        if self._prev_signal_momentum is None or self._prev_signal_ma is None:
            self._prev_signal_momentum = f
            self._prev_signal_ma = s
            return

        buy_open = self.BuyPosOpen and self._prev_signal_momentum <= self._prev_signal_ma and f > s
        sell_open = self.SellPosOpen and self._prev_signal_momentum >= self._prev_signal_ma and f < s

        if buy_open and self.Position == 0:
            self.BuyMarket()
        elif sell_open and self.Position == 0:
            self.SellMarket()

        self._prev_signal_momentum = f
        self._prev_signal_ma = s

    def OnReseted(self):
        super(color_zerolag_momentum_x2_strategy, self).OnReseted()
        self._trend = 0
        self._prev_signal_momentum = None
        self._prev_signal_ma = None

    def CreateClone(self):
        return color_zerolag_momentum_x2_strategy()
