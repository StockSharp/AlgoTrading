import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticK, SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class stochastic_with_dynamic_zones_strategy(Strategy):
    """
    Stochastic Oscillator with dynamic overbought and oversold zones.
    The zones are the average of %K over LookbackPeriod bars plus/minus StandardDeviationFactor
    standard deviations. Buys when %K crosses above %D inside the oversold zone and sells when %K
    crosses below %D inside the overbought zone; the opposite signal reverses the position.
    """

    def __init__(self):
        super(stochastic_with_dynamic_zones_strategy, self).__init__()

        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic Period", "Stochastic lookback period", "Indicators")
        self._stoch_k_period = self.Param("StochKPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %K Period", "Smoothing period of %K", "Indicators")
        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stoch %D Period", "Period of %D", "Indicators")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Lookback Period", "Period for dynamic zones", "Indicators")
        self._standard_deviation_factor = self.Param("StandardDeviationFactor", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("StdDev Factor", "Standard deviation factor for dynamic zones", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._k_smoothing = None
        self._d_line = None
        self._zone_average = None
        self._zone_std_dev = None
        self._prev_k = None
        self._prev_d = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def OnReseted(self):
        super(stochastic_with_dynamic_zones_strategy, self).OnReseted()
        self._k_smoothing = None
        self._d_line = None
        self._zone_average = None
        self._zone_std_dev = None
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(stochastic_with_dynamic_zones_strategy, self).OnStarted2(time)

        raw_k = StochasticK()
        raw_k.Length = self._stoch_period.Value
        self._k_smoothing = SimpleMovingAverage()
        self._k_smoothing.Length = self._stoch_k_period.Value
        self._d_line = SimpleMovingAverage()
        self._d_line.Length = self._stoch_d_period.Value
        self._zone_average = SimpleMovingAverage()
        self._zone_average.Length = self._lookback_period.Value
        self._zone_std_dev = StandardDeviation()
        self._zone_std_dev.Length = self._lookback_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(raw_k, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, raw_k_value):
        if candle.State != CandleStates.Finished:
            return

        t = candle.ServerTime
        k = float(process_float(self._k_smoothing, raw_k_value, t, True))
        if not self._k_smoothing.IsFormed:
            return

        d = float(process_float(self._d_line, k, t, True))
        average = float(process_float(self._zone_average, k, t, True))
        std_dev = float(process_float(self._zone_std_dev, k, t, True))

        prev_k = self._prev_k
        prev_d = self._prev_d
        self._prev_k = k
        self._prev_d = d

        if not self._d_line.IsFormed or not self._zone_average.IsFormed or not self._zone_std_dev.IsFormed:
            return

        if prev_k is None or prev_d is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        factor = float(self._standard_deviation_factor.Value)
        oversold = average - factor * std_dev
        overbought = average + factor * std_dev

        cross_up = prev_k <= prev_d and k > d
        cross_down = prev_k >= prev_d and k < d

        if cross_up and k < oversold and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif cross_down and k > overbought and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return stochastic_with_dynamic_zones_strategy()
