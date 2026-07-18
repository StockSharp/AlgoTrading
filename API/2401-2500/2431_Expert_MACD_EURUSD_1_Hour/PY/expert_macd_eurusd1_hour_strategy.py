import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class expert_macd_eurusd1_hour_strategy(Strategy):
    def __init__(self):
        super(expert_macd_eurusd1_hour_strategy, self).__init__()

        self._fast_length = self.Param("FastLength", 12)
        self._slow_length = self.Param("SlowLength", 26)
        self._signal_length = self.Param("SignalLength", 9)
        self._trailing_points = self.Param("TrailingPoints", 25.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1)))

        self._fast_ema = None
        self._slow_ema = None
        self._signal_ema = None
        self._main0 = 0.0
        self._main1 = 0.0
        self._signal0 = 0.0
        self._signal1 = 0.0
        self._counter = 0

    @property
    def FastLength(self):
        return self._fast_length.Value

    @FastLength.setter
    def FastLength(self, value):
        self._fast_length.Value = value

    @property
    def SlowLength(self):
        return self._slow_length.Value

    @SlowLength.setter
    def SlowLength(self, value):
        self._slow_length.Value = value

    @property
    def SignalLength(self):
        return self._signal_length.Value

    @SignalLength.setter
    def SignalLength(self, value):
        self._signal_length.Value = value

    @property
    def TrailingPoints(self):
        return self._trailing_points.Value

    @TrailingPoints.setter
    def TrailingPoints(self, value):
        self._trailing_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(expert_macd_eurusd1_hour_strategy, self).OnStarted2(time)

        self._main0 = 0.0
        self._main1 = 0.0
        self._signal0 = 0.0
        self._signal1 = 0.0
        self._counter = 0

        self._fast_ema = ExponentialMovingAverage()
        self._fast_ema.Length = self.FastLength
        self._slow_ema = ExponentialMovingAverage()
        self._slow_ema.Length = self.SlowLength
        self._signal_ema = ExponentialMovingAverage()
        self._signal_ema.Length = self.SignalLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        self.StartProtection(
            Unit(2000.0, UnitTypes.Absolute),
            Unit(1000.0, UnitTypes.Absolute))

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        t = candle.OpenTime
        fast_result = process_float(self._fast_ema, candle.ClosePrice, t, True)
        slow_result = process_float(self._slow_ema, candle.ClosePrice, t, True)
        if not self._fast_ema.IsFormed or not self._slow_ema.IsFormed:
            return

        fast_val = float(fast_result)
        slow_val = float(slow_result)
        main = fast_val - slow_val

        signal_result = process_float(self._signal_ema, main, t, True)
        if not self._signal_ema.IsFormed:
            return

        signal = float(signal_result)

        self._main1 = self._main0
        self._main0 = main
        self._signal1 = self._signal0
        self._signal0 = signal

        if self._counter < 3:
            self._counter += 1
            return

        buy_signal = self._main1 <= self._signal1 and self._main0 > self._signal0 and self._main0 < 0.0
        sell_signal = self._main1 >= self._signal1 and self._main0 < self._signal0 and self._main0 > 0.0

        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()

    def OnReseted(self):
        super(expert_macd_eurusd1_hour_strategy, self).OnReseted()
        self._main0 = 0.0
        self._main1 = 0.0
        self._signal0 = 0.0
        self._signal1 = 0.0
        self._counter = 0
        self._fast_ema = None
        self._slow_ema = None
        self._signal_ema = None

    def CreateClone(self):
        return expert_macd_eurusd1_hour_strategy()
