import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import MovingAverageConvergenceDivergenceSignal, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class macd_stochastic_trailing_strategy(Strategy):
    """MACD + Stochastic trailing strategy.
    Enters long when MACD histogram positive and Stochastic K crosses above D from oversold.
    Enters short when MACD histogram negative and Stochastic K crosses below D from overbought."""

    def __init__(self):
        super(macd_stochastic_trailing_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._prev_k = None
        self._prev_d = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnReseted(self):
        super(macd_stochastic_trailing_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted2(self, time):
        super(macd_stochastic_trailing_strategy, self).OnStarted2(time)

        self._prev_k = None
        self._prev_d = None

        macd = MovingAverageConvergenceDivergenceSignal()
        stoch = StochasticOscillator()

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(macd, stoch, self._process_candle).Start()

    def _process_candle(self, candle, macd_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not macd_value.IsFinal or not stoch_value.IsFinal:
            return

        macd_raw = macd_value.Macd if hasattr(macd_value, 'Macd') else None
        signal_raw = macd_value.Signal if hasattr(macd_value, 'Signal') else None
        if macd_raw is None or signal_raw is None:
            return

        macd_main = float(macd_raw)
        signal_line = float(signal_raw)

        k_raw = stoch_value.K if hasattr(stoch_value, 'K') else None
        d_raw = stoch_value.D if hasattr(stoch_value, 'D') else None
        if k_raw is None or d_raw is None:
            return

        k = float(k_raw)
        d = float(d_raw)

        if self._prev_k is None or self._prev_d is None:
            self._prev_k = k
            self._prev_d = d
            return

        prev_k = self._prev_k
        prev_d = self._prev_d

        histogram = macd_main - signal_line
        k_cross_up = prev_k <= prev_d and k > d
        k_cross_down = prev_k >= prev_d and k < d

        # Buy: MACD histogram positive + stochastic K crosses above D from oversold
        if histogram > 0 and k_cross_up and k < 50 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Sell: MACD histogram negative + stochastic K crosses below D from overbought
        elif histogram < 0 and k_cross_down and k > 50 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return macd_stochastic_trailing_strategy()
