import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class stochastic_hook_reversal_strategy(Strategy):
    """
    Stochastic Hook Reversal strategy.
    Enters long when %K hooks up from oversold zone.
    Enters short when %K hooks down from overbought zone.
    Exits when %K reaches neutral zone.
    """

    def __init__(self):
        super(stochastic_hook_reversal_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14).SetDisplay("K Period", "%K period", "Stochastic")
        self._d_period = self.Param("DPeriod", 3).SetDisplay("D Period", "%D period", "Stochastic")
        self._oversold_level = self.Param("OversoldLevel", 20).SetDisplay("Oversold", "Oversold level", "Stochastic")
        self._overbought_level = self.Param("OverboughtLevel", 80).SetDisplay("Overbought", "Overbought level", "Stochastic")
        self._exit_level = self.Param("ExitLevel", 50).SetDisplay("Exit Level", "Neutral exit zone", "Stochastic")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_k = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_hook_reversal_strategy, self).OnReseted()
        self._prev_k = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(stochastic_hook_reversal_strategy, self).OnStarted(time)

        self._prev_k = None
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

        if not stoch_iv.IsFormed:
            return

        k_val = stoch_iv.K
        if k_val is None:
            return

        kv = float(k_val)

        if self._prev_k is None:
            self._prev_k = kv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_k = kv
            return

        cd = self._cooldown_bars.Value
        oversold = self._oversold_level.Value
        overbought = self._overbought_level.Value
        exit_lvl = self._exit_level.Value

        # Hook up from oversold
        oversold_hook_up = self._prev_k < oversold and kv > self._prev_k
        # Hook down from overbought
        overbought_hook_down = self._prev_k > overbought and kv < self._prev_k

        if self.Position == 0 and oversold_hook_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and overbought_hook_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and kv < exit_lvl:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and kv > exit_lvl:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_k = kv

    def CreateClone(self):
        return stochastic_hook_reversal_strategy()
