import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class rrs_impulse_strategy(Strategy):
    """RSI + Stochastic + Bollinger Bands counter-trend strategy."""

    def __init__(self):
        super(rrs_impulse_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI length", "RSI")
        self._rsi_upper_level = self.Param("RsiUpperLevel", 65.0) \
            .SetDisplay("RSI Upper", "Overbought", "RSI")
        self._rsi_lower_level = self.Param("RsiLowerLevel", 35.0) \
            .SetDisplay("RSI Lower", "Oversold", "RSI")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %K", "%K period", "Stochastic")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Stochastic %D", "%D period", "Stochastic")
        self._stochastic_upper_level = self.Param("StochasticUpperLevel", 70.0) \
            .SetDisplay("Stochastic Upper", "Overbought", "Stochastic")
        self._stochastic_lower_level = self.Param("StochasticLowerLevel", 30.0) \
            .SetDisplay("Stochastic Lower", "Oversold", "Stochastic")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "BB length", "Bollinger")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Deviation", "BB deviation", "Bollinger")

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RsiPeriod(self):
        return self._rsi_period.Value

    @property
    def RsiUpperLevel(self):
        return self._rsi_upper_level.Value

    @property
    def RsiLowerLevel(self):
        return self._rsi_lower_level.Value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @property
    def StochasticDPeriod(self):
        return self._stochastic_d_period.Value

    @property
    def StochasticUpperLevel(self):
        return self._stochastic_upper_level.Value

    @property
    def StochasticLowerLevel(self):
        return self._stochastic_lower_level.Value

    @property
    def BollingerPeriod(self):
        return self._bollinger_period.Value

    @property
    def BollingerDeviation(self):
        return self._bollinger_deviation.Value

    def OnStarted(self, time):
        super(rrs_impulse_strategy, self).OnStarted(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RsiPeriod

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.StochasticKPeriod
        stochastic.D.Length = self.StochasticDPeriod

        bollinger = BollingerBands()
        bollinger.Length = self.BollingerPeriod
        bollinger.Width = self.BollingerDeviation

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(rsi, stochastic, bollinger, self._process_candle).Start()

    def _process_candle(self, candle, rsi_val, stoch_val, bb_val):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_val.IsFinal or not stoch_val.IsFinal or not bb_val.IsFinal:
            return

        if not rsi_val.IsFormed or not stoch_val.IsFormed or not bb_val.IsFormed:
            return

        rsi = float(rsi_val.GetValue[float]())
        stoch_k = stoch_val.K if stoch_val.K is not None else 50.0
        stoch_k = float(stoch_k)

        bb_upper = bb_val.UpBand
        bb_lower = bb_val.LowBand
        upper = float(bb_upper) if bb_upper is not None else float(candle.ClosePrice)
        lower = float(bb_lower) if bb_lower is not None else float(candle.ClosePrice)

        close = float(candle.ClosePrice)

        ob_signals = 0
        os_signals = 0

        if rsi >= float(self.RsiUpperLevel):
            ob_signals += 1
        if rsi <= float(self.RsiLowerLevel):
            os_signals += 1

        if stoch_k >= float(self.StochasticUpperLevel):
            ob_signals += 1
        if stoch_k <= float(self.StochasticLowerLevel):
            os_signals += 1

        if close >= upper:
            ob_signals += 1
        if close <= lower:
            os_signals += 1

        if os_signals >= 2 and self.Position <= 0:
            self.BuyMarket()
        elif ob_signals >= 2 and self.Position >= 0:
            self.SellMarket()
        elif self.Position > 0 and rsi > 50.0 and stoch_k > 50.0:
            self.SellMarket()
        elif self.Position < 0 and rsi < 50.0 and stoch_k < 50.0:
            self.BuyMarket()

    def CreateClone(self):
        return rrs_impulse_strategy()
