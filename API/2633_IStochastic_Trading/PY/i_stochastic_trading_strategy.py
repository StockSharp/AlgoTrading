import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import StochasticOscillator


class i_stochastic_trading_strategy(Strategy):
    """Stochastic crossover strategy with oversold/overbought zone filtering."""

    def __init__(self):
        super(i_stochastic_trading_strategy, self).__init__()

        self._k_period = self.Param("KPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("K Period", "Number of bars for %K", "Indicators")
        self._d_period = self.Param("DPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("D Period", "Smoothing period for %D", "Indicators")
        self._zone_buy = self.Param("ZoneBuy", 30.0) \
            .SetDisplay("Buy Zone", "Upper boundary for bullish confirmation", "Signals")
        self._zone_sell = self.Param("ZoneSell", 70.0) \
            .SetDisplay("Sell Zone", "Lower boundary for bearish confirmation", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Primary timeframe", "General")

        self._prev_k = None
        self._prev_d = None

    @property
    def KPeriod(self):
        return self._k_period.Value
    @property
    def DPeriod(self):
        return self._d_period.Value
    @property
    def ZoneBuy(self):
        return self._zone_buy.Value
    @property
    def ZoneSell(self):
        return self._zone_sell.Value
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(i_stochastic_trading_strategy, self).OnStarted2(time)

        self._prev_k = None
        self._prev_d = None

        stoch = StochasticOscillator()
        stoch.K.Length = self.KPeriod
        stoch.D.Length = self.DPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(stoch, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stoch)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFinal:
            return

        k_val = stoch_value.K
        d_val = stoch_value.D

        if k_val is None or d_val is None:
            return

        k = float(k_val)
        d = float(d_val)

        if self._prev_k is not None and self._prev_d is not None:
            crossed_up = self._prev_k <= self._prev_d and k > d
            crossed_down = self._prev_k >= self._prev_d and k < d

            if crossed_up and d < float(self.ZoneBuy) and self.Position <= 0:
                self.BuyMarket()
            elif crossed_down and d > float(self.ZoneSell) and self.Position >= 0:
                self.SellMarket()

        self._prev_k = k
        self._prev_d = d

    def OnReseted(self):
        super(i_stochastic_trading_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def CreateClone(self):
        return i_stochastic_trading_strategy()
