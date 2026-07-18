import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, RelativeStrengthIndex, StochasticOscillator
)


class expert_rsi_stochastic_ma_strategy(Strategy):
    """SMA trend filter + RSI + Stochastic oscillator combined strategy."""

    def __init__(self):
        super(expert_rsi_stochastic_ma_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Time frame for calculations", "General")
        self._rsi_period = self.Param("RsiPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Number of bars for RSI", "RSI")
        self._rsi_upper = self.Param("RsiUpperLevel", 80.0) \
            .SetDisplay("RSI Overbought", "Upper RSI threshold", "RSI")
        self._rsi_lower = self.Param("RsiLowerLevel", 20.0) \
            .SetDisplay("RSI Oversold", "Lower RSI threshold", "RSI")
        self._stoch_k = self.Param("StochKPeriod", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("%K Period", "Length of Stochastic %K", "Stochastic")
        self._stoch_d = self.Param("StochDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("%D Period", "Length of Stochastic %D", "Stochastic")
        self._stoch_upper = self.Param("StochUpperLevel", 70.0) \
            .SetDisplay("Stoch Overbought", "Upper Stochastic threshold", "Stochastic")
        self._stoch_lower = self.Param("StochLowerLevel", 30.0) \
            .SetDisplay("Stoch Oversold", "Lower Stochastic threshold", "Stochastic")
        self._ma_period = self.Param("MaPeriod", 50) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Length of moving average", "Moving Average")

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def RsiPeriod(self):
        return self._rsi_period.Value
    @property
    def RsiUpperLevel(self):
        return self._rsi_upper.Value
    @property
    def RsiLowerLevel(self):
        return self._rsi_lower.Value
    @property
    def StochKPeriod(self):
        return self._stoch_k.Value
    @property
    def StochDPeriod(self):
        return self._stoch_d.Value
    @property
    def StochUpperLevel(self):
        return self._stoch_upper.Value
    @property
    def StochLowerLevel(self):
        return self._stoch_lower.Value
    @property
    def MaPeriod(self):
        return self._ma_period.Value

    def OnStarted2(self, time):
        super(expert_rsi_stochastic_ma_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self.MaPeriod
        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod
        stoch = StochasticOscillator()
        stoch.K.Length = self.StochKPeriod
        stoch.D.Length = self.StochDPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(sma, rsi, stoch, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, sma_value, rsi_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFinal or not rsi_value.IsFinal or not stoch_value.IsFinal:
            return

        if sma_value.IsEmpty or rsi_value.IsEmpty:
            return

        sma_val = float(sma_value)
        rsi_val = float(rsi_value)

        stoch_k = stoch_value.K
        stoch_d = stoch_value.D
        if stoch_k is None or stoch_d is None:
            return

        sk = float(stoch_k)
        sd = float(stoch_d)
        price = float(candle.ClosePrice)

        # Long: price > SMA, RSI oversold, Stoch oversold
        if (price > sma_val and rsi_val < float(self.RsiLowerLevel) and
                sk < float(self.StochLowerLevel) and sd < float(self.StochLowerLevel)):
            if self.Position <= 0:
                self.BuyMarket()
        # Short: price < SMA, RSI overbought, Stoch overbought
        elif (price < sma_val and rsi_val > float(self.RsiUpperLevel) and
                sk > float(self.StochUpperLevel) and sd > float(self.StochUpperLevel)):
            if self.Position >= 0:
                self.SellMarket()
        # Exit long: Stoch overbought
        elif self.Position > 0 and sk > float(self.StochUpperLevel):
            self.SellMarket()
        # Exit short: Stoch oversold
        elif self.Position < 0 and sk < float(self.StochLowerLevel):
            self.BuyMarket()

    def OnReseted(self):
        super(expert_rsi_stochastic_ma_strategy, self).OnReseted()

    def CreateClone(self):
        return expert_rsi_stochastic_ma_strategy()
