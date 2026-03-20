import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, BollingerBands, MovingAverageConvergenceDivergenceSignal, RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class multi_ea_v12_strategy(Strategy):
    def __init__(self):
        super(multi_ea_v12_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(4) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._required_confirmations = self.Param("RequiredConfirmations", 3) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_upper = self.Param("RsiUpper", 65) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._rsi_lower = self.Param("RsiLower", 35) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stoch_k_period = self.Param("StochKPeriod", 10) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stoch_d_period = self.Param("StochDPeriod", 3) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stoch_upper = self.Param("StochUpper", 70) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._stoch_lower = self.Param("StochLower", 30) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._adx_trend_level = self.Param("AdxTrendLevel", 20) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._macd_fast = self.Param("MacdFast", 12) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._macd_slow = self.Param("MacdSlow", 26) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._macd_signal = self.Param("MacdSignal", 9) \
            .SetDisplay("Candle Type", "Timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_ea_v12_strategy, self).OnReseted()
        pass

    def OnStarted(self, time):
        super(multi_ea_v12_strategy, self).OnStarted(time)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        self._bollinger = BollingerBands()
        self._bollinger.Length = self.bollinger_period
        self._bollinger.Width = self.bollinger_deviation
        self._adx = AverageDirectionalIndex()
        self._adx.Length = self.adx_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._rsi, stochastic, self._bollinger, self._adx, macd, self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return multi_ea_v12_strategy()
