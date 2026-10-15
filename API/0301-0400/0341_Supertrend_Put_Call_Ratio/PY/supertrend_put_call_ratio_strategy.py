import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("Ecng.Common")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal
from Ecng.Common import RandomGen
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SuperTrend
from StockSharp.Algo.Strategies import Strategy


class supertrend_put_call_ratio_strategy(Strategy):
    """
    Supertrend with Put/Call Ratio strategy.
    """

    def __init__(self):
        super(supertrend_put_call_ratio_strategy, self).__init__()

        self._period = self.Param("Period", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Period", "Supertrend ATR period", "Supertrend Settings")

        self._multiplier = self.Param("Multiplier", 3.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend Settings")

        self._pcr_period = self.Param("PCRPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("PCR Period", "Put/Call Ratio averaging period", "PCR Settings")

        self._pcr_multiplier = self.Param("PCRMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("PCR Std Dev Multiplier", "Multiplier for PCR standard deviation", "PCR Settings")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._pcr_history = []
        self._pcr_average = 0.0
        self._pcr_std_dev = 0.0
        self._is_long = False
        self._is_short = False
        self._current_pcr = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def OnReseted(self):
        super(supertrend_put_call_ratio_strategy, self).OnReseted()
        self._pcr_history = []
        self._is_long = False
        self._is_short = False
        self._current_pcr = 0.0
        self._pcr_average = 0.0
        self._pcr_std_dev = 0.0

    def OnStarted2(self, time):
        super(supertrend_put_call_ratio_strategy, self).OnStarted2(time)

        supertrend = SuperTrend()
        supertrend.Length = int(self._period.Value)
        supertrend.Multiplier = Decimal(float(self._multiplier.Value))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(supertrend, self.ProcessCandle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, supertrend_value):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        self.UpdatePCR(candle)

        pcr_mult = float(self._pcr_multiplier.Value)
        bullish_threshold = self._pcr_average - pcr_mult * self._pcr_std_dev
        bearish_threshold = self._pcr_average + pcr_mult * self._pcr_std_dev

        price = float(candle.ClosePrice)
        st = float(supertrend_value)
        price_above = price > st
        price_below = price < st

        if price_above and self._current_pcr < bullish_threshold and not self._is_long and self.Position <= 0:
            self.BuyMarket(self.Volume)
            self._is_long = True
            self._is_short = False
        elif price_below and self._current_pcr > bearish_threshold and not self._is_short and self.Position >= 0:
            self.SellMarket(self.Volume)
            self._is_short = True
            self._is_long = False

        if self._is_long and price_below and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._is_long = False
        elif self._is_short and price_above and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._is_short = False

    def UpdatePCR(self, candle):
        if candle.ClosePrice > candle.OpenPrice:
            pcr = 0.7 + RandomGen.GetDouble() * 0.3
        else:
            pcr = 1.0 + RandomGen.GetDouble() * 0.5

        self._current_pcr = pcr

        self._pcr_history.append(self._current_pcr)
        pcr_period = int(self._pcr_period.Value)
        if len(self._pcr_history) > pcr_period:
            self._pcr_history.pop(0)

        total = 0.0
        for v in self._pcr_history:
            total += v

        if len(self._pcr_history) > 0:
            self._pcr_average = total / len(self._pcr_history)
        else:
            self._pcr_average = 1.0

        if len(self._pcr_history) > 1:
            sum_sq = 0.0
            for v in self._pcr_history:
                diff = v - self._pcr_average
                sum_sq += diff * diff
            self._pcr_std_dev = Math.Sqrt(sum_sq / (len(self._pcr_history) - 1))
        else:
            self._pcr_std_dev = 0.1

        self.LogInfo("PCR: {0}, Avg: {1}, StdDev: {2}".format(self._current_pcr, self._pcr_average, self._pcr_std_dev))

    def CreateClone(self):
        return supertrend_put_call_ratio_strategy()
