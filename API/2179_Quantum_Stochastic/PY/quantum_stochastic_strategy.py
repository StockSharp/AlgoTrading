import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class quantum_stochastic_strategy(Strategy):
    def __init__(self):
        super(quantum_stochastic_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14) \
            .SetDisplay("%K Period", "Period of %K line", "Stochastic")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("%D Period", "Period of %D line", "Stochastic")
        self._high_level = self.Param("HighLevel", 80.0) \
            .SetDisplay("High Level", "Bottom of overbought zone", "Levels")
        self._low_level = self.Param("LowLevel", 20.0) \
            .SetDisplay("Low Level", "Top of oversold zone", "Levels")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles", "General")
        self._previous_k = None

    @property
    def k_period(self):
        return self._k_period.Value

    @property
    def d_period(self):
        return self._d_period.Value

    @property
    def high_level(self):
        return self._high_level.Value

    @property
    def low_level(self):
        return self._low_level.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(quantum_stochastic_strategy, self).OnReseted()
        self._previous_k = None

    def OnStarted(self, time):
        super(quantum_stochastic_strategy, self).OnStarted(time)

        stochastic = StochasticOscillator()
        stochastic.K.Length = self.k_period
        stochastic.D.Length = self.d_period

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        k = stoch_value.K
        if k is None:
            return
        k_value = float(k)

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_k = k_value
            return

        if self._previous_k is None:
            self._previous_k = k_value
            return

        prev_k = self._previous_k
        low = float(self.low_level)
        high = float(self.high_level)

        # Buy when %K crosses above oversold level
        if prev_k < low and k_value >= low and self.Position <= 0:
            self.BuyMarket()

        # Sell when %K crosses below overbought level
        if prev_k > high and k_value <= high and self.Position >= 0:
            self.SellMarket()

        self._previous_k = k_value

    def CreateClone(self):
        return quantum_stochastic_strategy()
