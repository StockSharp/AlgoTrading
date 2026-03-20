import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class stochastic_failure_swing_strategy(Strategy):
    """
    Strategy that trades based on Stochastic Oscillator Failure Swing pattern.
    A failure swing occurs when Stochastic reverses direction without crossing through centerline.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(stochastic_failure_swing_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._k_period = self.Param("KPeriod", 14).SetDisplay("K Period", "%K period", "Stochastic")
        self._d_period = self.Param("DPeriod", 3).SetDisplay("D Period", "%D period", "Stochastic")
        self._oversold_level = self.Param("OversoldLevel", 30.0).SetDisplay("Oversold Level", "Stochastic oversold", "Stochastic")
        self._overbought_level = self.Param("OverboughtLevel", 70.0).SetDisplay("Overbought Level", "Stochastic overbought", "Stochastic")
        self._cooldown_bars = self.Param("CooldownBars", 250).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_k = 0.0
        self._prev_prev_k = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_failure_swing_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_prev_k = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(stochastic_failure_swing_strategy, self).OnStarted(time)

        self._prev_k = 0.0
        self._prev_prev_k = 0.0
        self._cooldown = 0

        stochastic = StochasticOscillator()
        stochastic.K.Length = self._k_period.Value
        stochastic.D.Length = self._d_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, stoch_iv):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k_val = stoch_iv.K
        if k_val is None:
            return
        kv = float(k_val)

        # Need at least 2 previous values
        if self._prev_k == 0 or self._prev_prev_k == 0:
            self._prev_prev_k = self._prev_k
            self._prev_k = kv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_prev_k = self._prev_k
            self._prev_k = kv
            return

        cd = self._cooldown_bars.Value
        oversold = self._oversold_level.Value
        overbought = self._overbought_level.Value

        # Bullish Failure Swing: K was oversold, rose, pulled back but stayed above prior low
        is_bullish = (
            self._prev_prev_k < oversold and
            self._prev_k > self._prev_prev_k and
            kv < self._prev_k and
            kv > self._prev_prev_k
        )

        # Bearish Failure Swing: K was overbought, fell, bounced but stayed below prior high
        is_bearish = (
            self._prev_prev_k > overbought and
            self._prev_k < self._prev_prev_k and
            kv > self._prev_k and
            kv < self._prev_prev_k
        )

        if self.Position == 0:
            if is_bullish:
                self.BuyMarket()
                self._cooldown = cd
            elif is_bearish:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            # Exit long when K crosses above overbought
            if kv > overbought:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            # Exit short when K crosses below oversold
            if kv < oversold:
                self.BuyMarket()
                self._cooldown = cd

        self._prev_prev_k = self._prev_k
        self._prev_k = kv

    def CreateClone(self):
        return stochastic_failure_swing_strategy()
