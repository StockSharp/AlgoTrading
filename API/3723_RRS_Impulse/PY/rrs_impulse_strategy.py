import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class rrs_impulse_strategy(Strategy):
    def __init__(self):
        super(rrs_impulse_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_upper_level = self.Param("RsiUpperLevel", 65) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_lower_level = self.Param("RsiLowerLevel", 35) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 10) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stochastic_upper_level = self.Param("StochasticUpperLevel", 70) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stochastic_lower_level = self.Param("StochasticLowerLevel", 30) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2) \
            .SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rrs_impulse_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(rrs_impulse_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bollinger_period
        self._bollinger.Width = self.bollinger_deviation

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._rsi, stochastic, self._bollinger, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return rrs_impulse_strategy()
