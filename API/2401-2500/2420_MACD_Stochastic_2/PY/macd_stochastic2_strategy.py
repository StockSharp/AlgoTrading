import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import (
    RelativeStrengthIndex,
    MovingAverageConvergenceDivergence,
    ExponentialMovingAverage,
)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class macd_stochastic2_strategy(Strategy):
    def __init__(self):
        super(macd_stochastic2_strategy, self).__init__()

        self._oversold_threshold = self.Param("OversoldThreshold", 20.0)
        self._overbought_threshold = self.Param("OverboughtThreshold", 80.0)
        self._macd_fast_period = self.Param("MacdFastPeriod", 12)
        self._macd_slow_period = self.Param("MacdSlowPeriod", 26)
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5)
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3)
        self._stop_loss = self.Param("StopLoss", 1000.0)
        self._take_profit = self.Param("TakeProfit", 2000.0)
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))

        self._macd_prev1 = 0.0
        self._macd_prev2 = 0.0
        self._macd_count = 0
        self._macd_ind = None

    @property
    def OversoldThreshold(self):
        return self._oversold_threshold.Value

    @OversoldThreshold.setter
    def OversoldThreshold(self, value):
        self._oversold_threshold.Value = value

    @property
    def OverboughtThreshold(self):
        return self._overbought_threshold.Value

    @OverboughtThreshold.setter
    def OverboughtThreshold(self, value):
        self._overbought_threshold.Value = value

    @property
    def MacdFastPeriod(self):
        return self._macd_fast_period.Value

    @MacdFastPeriod.setter
    def MacdFastPeriod(self, value):
        self._macd_fast_period.Value = value

    @property
    def MacdSlowPeriod(self):
        return self._macd_slow_period.Value

    @MacdSlowPeriod.setter
    def MacdSlowPeriod(self, value):
        self._macd_slow_period.Value = value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @StochasticKPeriod.setter
    def StochasticKPeriod(self, value):
        self._stochastic_k_period.Value = value

    @property
    def StochasticDPeriod(self):
        return self._stochastic_d_period.Value

    @StochasticDPeriod.setter
    def StochasticDPeriod(self, value):
        self._stochastic_d_period.Value = value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    @StopLoss.setter
    def StopLoss(self, value):
        self._stop_loss.Value = value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @TakeProfit.setter
    def TakeProfit(self, value):
        self._take_profit.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted2(self, time):
        super(macd_stochastic2_strategy, self).OnStarted2(time)

        self._macd_prev1 = 0.0
        self._macd_prev2 = 0.0
        self._macd_count = 0

        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.MacdSlowPeriod
        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.MacdFastPeriod
        self._macd_ind = MovingAverageConvergenceDivergence(slow_ema, fast_ema)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.StochasticKPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(rsi, self.ProcessCandle).Start()

        self.StartProtection(
            Unit(self.TakeProfit, UnitTypes.Absolute),
            Unit(self.StopLoss, UnitTypes.Absolute))

    def ProcessCandle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        macd_result = process_float(self._macd_ind, candle.ClosePrice, candle.OpenTime, True)
        if not self._macd_ind.IsFormed:
            return

        macd_value = float(macd_result)
        rsi_val = float(rsi_value)

        self._macd_count += 1
        if self._macd_count < 3:
            self._macd_prev2 = self._macd_prev1
            self._macd_prev1 = macd_value
            return

        macd0 = macd_value
        macd1 = self._macd_prev1
        macd2 = self._macd_prev2

        long_signal = (macd0 < 0.0 and macd1 < 0.0 and macd2 < 0.0
                       and macd0 > macd1 and macd1 < macd2
                       and rsi_val < float(self.OversoldThreshold))

        short_signal = (macd0 > 0.0 and macd1 > 0.0 and macd2 > 0.0
                        and macd0 < macd1 and macd1 > macd2
                        and rsi_val > float(self.OverboughtThreshold))

        if long_signal and self.Position <= 0:
            self.BuyMarket()
        elif short_signal and self.Position >= 0:
            self.SellMarket()

        self._macd_prev2 = self._macd_prev1
        self._macd_prev1 = macd_value

    def OnReseted(self):
        super(macd_stochastic2_strategy, self).OnReseted()
        self._macd_prev1 = 0.0
        self._macd_prev2 = 0.0
        self._macd_count = 0

    def CreateClone(self):
        return macd_stochastic2_strategy()
