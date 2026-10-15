import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class starter_v6_mod_e_strategy(Strategy):
    """Dual EMA crossover strategy with Laguerre RSI filter.
    Buy when fast EMA crosses above slow EMA and Laguerre was oversold.
    Sell when fast EMA crosses below slow EMA and Laguerre was overbought."""

    def __init__(self):
        super(starter_v6_mod_e_strategy, self).__init__()

        self._slow_ema_period = self.Param("SlowEmaPeriod", 26) \
            .SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._fast_ema_period = self.Param("FastEmaPeriod", 12) \
            .SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._laguerre_gamma = self.Param("LaguerreGamma", 0.7) \
            .SetDisplay("Laguerre Gamma", "Smoothing factor for Laguerre RSI", "Indicators")
        self._laguerre_oversold = self.Param("LaguerreOversold", 0.5) \
            .SetDisplay("Laguerre Oversold", "Oversold level (0-1)", "Indicators")
        self._laguerre_overbought = self.Param("LaguerreOverbought", 0.5) \
            .SetDisplay("Laguerre Overbought", "Overbought level (0-1)", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_laguerre = 0.0
        self._has_prev = False

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def SlowEmaPeriod(self):
        return self._slow_ema_period.Value

    @property
    def FastEmaPeriod(self):
        return self._fast_ema_period.Value

    @property
    def LaguerreGamma(self):
        return self._laguerre_gamma.Value

    @property
    def LaguerreOversold(self):
        return self._laguerre_oversold.Value

    @property
    def LaguerreOverbought(self):
        return self._laguerre_overbought.Value

    def OnReseted(self):
        super(starter_v6_mod_e_strategy, self).OnReseted()
        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._prev_laguerre = 0.0
        self._has_prev = False

    def OnStarted2(self, time):
        super(starter_v6_mod_e_strategy, self).OnStarted2(time)

        self._has_prev = False
        self._lag_l0 = 0.0
        self._lag_l1 = 0.0
        self._lag_l2 = 0.0
        self._lag_l3 = 0.0

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self.FastEmaPeriod
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self.SlowEmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(fast_ema, slow_ema, self._process_candle).Start()

    def _calculate_laguerre(self, price):
        gamma = float(self.LaguerreGamma)

        l0_prev = self._lag_l0
        l1_prev = self._lag_l1
        l2_prev = self._lag_l2
        l3_prev = self._lag_l3

        self._lag_l0 = (1.0 - gamma) * price + gamma * l0_prev
        self._lag_l1 = -gamma * self._lag_l0 + l0_prev + gamma * l1_prev
        self._lag_l2 = -gamma * self._lag_l1 + l1_prev + gamma * l2_prev
        self._lag_l3 = -gamma * self._lag_l2 + l2_prev + gamma * l3_prev

        cu = 0.0
        cd = 0.0

        if self._lag_l0 >= self._lag_l1:
            cu = self._lag_l0 - self._lag_l1
        else:
            cd = self._lag_l1 - self._lag_l0

        if self._lag_l1 >= self._lag_l2:
            cu += self._lag_l1 - self._lag_l2
        else:
            cd += self._lag_l2 - self._lag_l1

        if self._lag_l2 >= self._lag_l3:
            cu += self._lag_l2 - self._lag_l3
        else:
            cd += self._lag_l3 - self._lag_l2

        denom = cu + cd
        if denom == 0.0:
            return 0.0
        return cu / denom

    def _process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        fast_val = float(fast)
        slow_val = float(slow)
        close = float(candle.ClosePrice)

        laguerre = self._calculate_laguerre(close)

        if not self._has_prev:
            self._prev_fast = fast_val
            self._prev_slow = slow_val
            self._prev_laguerre = laguerre
            self._has_prev = True
            return

        # EMA crossover signals
        bullish_cross = self._prev_fast <= self._prev_slow and fast_val > slow_val
        bearish_cross = self._prev_fast >= self._prev_slow and fast_val < slow_val

        # Long: fast EMA crosses above slow + Laguerre was oversold
        if self.Position <= 0 and bullish_cross and self._prev_laguerre <= float(self.LaguerreOversold):
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        # Short: fast EMA crosses below slow + Laguerre was overbought
        elif self.Position >= 0 and bearish_cross and self._prev_laguerre >= float(self.LaguerreOverbought):
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

        self._prev_fast = fast_val
        self._prev_slow = slow_val
        self._prev_laguerre = laguerre

    def CreateClone(self):
        return starter_v6_mod_e_strategy()
