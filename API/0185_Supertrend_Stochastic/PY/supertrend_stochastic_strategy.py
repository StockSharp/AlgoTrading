import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import SuperTrend, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class supertrend_stochastic_strategy(Strategy):
    """
    Supertrend + Stochastic strategy.
    Enters trades when Supertrend indicates trend direction and Stochastic confirms.
    Uses StartProtection for exits.
    """

    def __init__(self):
        super(supertrend_stochastic_strategy, self).__init__()

        self._supertrendPeriod = self.Param("SupertrendPeriod", 10) \
            .SetDisplay("Supertrend Period", "Supertrend ATR period length", "Supertrend")

        self._supertrendMultiplier = self.Param("SupertrendMultiplier", 3.0) \
            .SetDisplay("Supertrend Multiplier", "Supertrend ATR multiplier", "Supertrend")

        self._stochPeriod = self.Param("StochPeriod", 14) \
            .SetDisplay("Stochastic Period", "Stochastic oscillator period", "Stochastic")

        self._stochK = self.Param("StochK", 3) \
            .SetDisplay("Stochastic %K", "Stochastic %K period", "Stochastic")

        self._stochD = self.Param("StochD", 3) \
            .SetDisplay("Stochastic %D", "Stochastic %D period", "Stochastic")

        self._cooldownBars = self.Param("CooldownBars", 8) \
            .SetRange(1, 50) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._candleType = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stopLossPercent = self.Param("StopLossPercent", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management")

        self._supertrend = None
        self._stochastic = None
        self._cooldown = 0

    @property
    def CandleType(self):
        return self._candleType.Value

    def OnReseted(self):
        super(supertrend_stochastic_strategy, self).OnReseted()
        self._supertrend = None
        self._stochastic = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(supertrend_stochastic_strategy, self).OnStarted(time)
        self._cooldown = 0

        self._supertrend = SuperTrend()
        self._supertrend.Length = self._supertrendPeriod.Value
        self._supertrend.Multiplier = self._supertrendMultiplier.Value

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self._stochK.Value
        self._stochastic.D.Length = self._stochD.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(self._supertrend, self._stochastic, self.ProcessCandle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(self._stopLossPercent.Value, UnitTypes.Percent)
        )

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._supertrend)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, st_result, stoch_result):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Check SuperTrend value type
        is_bullish = st_result.IsUpTrend

        # Get stochastic K
        stoch_k_val = stoch_result.K
        if stoch_k_val is None:
            return
        stoch_k = float(stoch_k_val)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if self.Position != 0:
            return

        # Buy: bullish supertrend + stochastic oversold area
        if is_bullish and stoch_k < 30:
            self.BuyMarket()
            self._cooldown = int(self._cooldownBars.Value)
        # Sell: bearish supertrend + stochastic overbought area
        elif not is_bullish and stoch_k > 70:
            self.SellMarket()
            self._cooldown = int(self._cooldownBars.Value)

    def CreateClone(self):
        return supertrend_stochastic_strategy()
