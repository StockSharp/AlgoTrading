import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, ParabolicSar, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class trend_catcher_strategy(Strategy):

    def __init__(self):
        super(trend_catcher_strategy, self).__init__()

        self._slow_ma_period = self.Param("SlowMaPeriod", 200) \
            .SetDisplay("Slow MA Period", "Period of the slow moving average", "Moving Averages")
        self._fast_ma_period = self.Param("FastMaPeriod", 50) \
            .SetDisplay("Fast MA Period", "Period of the fast moving average", "Moving Averages")
        self._sar_step = self.Param("SarStep", 0.004) \
            .SetDisplay("SAR Step", "Parabolic SAR acceleration step", "Parabolic SAR")
        self._sar_max = self.Param("SarMax", 0.2) \
            .SetDisplay("SAR Max", "Parabolic SAR maximum acceleration", "Parabolic SAR")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._slow_ma = None
        self._is_initialized = False
        self._is_price_above_sar_prev = False

    @property
    def SlowMaPeriod(self):
        return self._slow_ma_period.Value

    @SlowMaPeriod.setter
    def SlowMaPeriod(self, value):
        self._slow_ma_period.Value = value

    @property
    def FastMaPeriod(self):
        return self._fast_ma_period.Value

    @FastMaPeriod.setter
    def FastMaPeriod(self, value):
        self._fast_ma_period.Value = value

    @property
    def SarStep(self):
        return self._sar_step.Value

    @SarStep.setter
    def SarStep(self, value):
        self._sar_step.Value = value

    @property
    def SarMax(self):
        return self._sar_max.Value

    @SarMax.setter
    def SarMax(self, value):
        self._sar_max.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(trend_catcher_strategy, self).OnStarted(time)

        self._is_initialized = False

        sar = ParabolicSar()
        sar.Acceleration = self.SarStep
        sar.AccelerationStep = self.SarStep
        sar.AccelerationMax = self.SarMax

        fast_ma = ExponentialMovingAverage()
        fast_ma.Length = self.FastMaPeriod
        self._slow_ma = ExponentialMovingAverage()
        self._slow_ma.Length = self.SlowMaPeriod

        self.SubscribeCandles(self.CandleType) \
            .BindEx(sar, fast_ma, self.ProcessCandle) \
            .Start()

        self.StartProtection(
            takeProfit=Unit(3.0, UnitTypes.Percent),
            stopLoss=Unit(2.0, UnitTypes.Percent),
            isStopTrailing=True
        )

    def ProcessCandle(self, candle, sar_value, fast_ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not sar_value.IsFormed or not fast_ma_value.IsFormed:
            return

        sar = float(sar_value)
        fast_val = float(fast_ma_value)

        close = float(candle.ClosePrice)
        t = candle.OpenTime

        slow_input = DecimalIndicatorValue(self._slow_ma, close, t)
        slow_input.IsFinal = True
        slow_result = self._slow_ma.Process(slow_input)
        if not slow_result.IsFormed:
            return

        slow_val = float(slow_result)

        is_price_above_sar = close > sar

        if not self._is_initialized:
            self._is_price_above_sar_prev = is_price_above_sar
            self._is_initialized = True
            return

        buy_signal = is_price_above_sar and not self._is_price_above_sar_prev and fast_val > slow_val
        sell_signal = not is_price_above_sar and self._is_price_above_sar_prev and fast_val < slow_val

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._is_price_above_sar_prev = is_price_above_sar

    def OnReseted(self):
        super(trend_catcher_strategy, self).OnReseted()
        self._slow_ma = None
        self._is_initialized = False
        self._is_price_above_sar_prev = False

    def CreateClone(self):
        return trend_catcher_strategy()
