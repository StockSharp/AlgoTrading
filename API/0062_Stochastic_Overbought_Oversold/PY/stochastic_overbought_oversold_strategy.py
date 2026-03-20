import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class stochastic_overbought_oversold_strategy(Strategy):
    """
    Stochastic Overbought/Oversold strategy.
    Buys when K is oversold (<20), sells when K is overbought (>80).
    """

    def __init__(self):
        super(stochastic_overbought_oversold_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 3).SetDisplay("K Period", "Smoothing period for %K", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetDisplay("D Period", "Smoothing period for %D", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_overbought_oversold_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(stochastic_overbought_oversold_strategy, self).OnStarted(time)

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

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFormed:
            return

        k_val = stoch_value.K
        if k_val is None:
            return

        kv = float(k_val)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value

        if self.Position == 0 and kv < 20:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and kv > 80:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and kv > 80:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and kv < 20:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return stochastic_overbought_oversold_strategy()
