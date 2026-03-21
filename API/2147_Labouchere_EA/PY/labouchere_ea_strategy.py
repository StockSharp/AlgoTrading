import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class labouchere_ea_strategy(Strategy):
    """
    Labouchere strategy using Stochastic K/D crossover for entry signals.
    Uses StartProtection for percentage-based SL/TP.
    """

    def __init__(self):
        super(labouchere_ea_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 10) \
            .SetDisplay("K Period", "Stochastic %K period", "Indicator")
        self._d_period = self.Param("DPeriod", 3) \
            .SetDisplay("D Period", "Stochastic %D period", "Indicator")
        self._stop_loss_pct = self.Param("StopLossPct", 1.0) \
            .SetDisplay("Stop Loss %", "Stop loss percent", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 1.5) \
            .SetDisplay("Take Profit %", "Take profit percent", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles used", "General")

        self._prev_k = None
        self._prev_d = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(labouchere_ea_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None
        self._entry_price = 0.0

    def OnStarted(self, time):
        super(labouchere_ea_strategy, self).OnStarted(time)

        stoch = StochasticOscillator()
        stoch.K.Length = self._k_period.Value
        stoch.D.Length = self._d_period.Value

        self.StartProtection(
            Unit(self._take_profit_pct.Value, UnitTypes.Percent),
            Unit(self._stop_loss_pct.Value, UnitTypes.Percent))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, self._process_candle).Start()

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        if not stoch_value.IsFormed:
            return

        k = stoch_value.K
        d = stoch_value.D
        if k is None or d is None:
            return

        k = float(k)
        d = float(d)

        signal = 0

        if self._prev_k is not None and self._prev_d is not None:
            if self._prev_k <= self._prev_d and k > d:
                signal = 1
            elif self._prev_k >= self._prev_d and k < d:
                signal = -1

        self._prev_k = k
        self._prev_d = d

        if signal == 0:
            return

        if signal > 0 and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = float(candle.ClosePrice)
        elif signal < 0 and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = float(candle.ClosePrice)

    def CreateClone(self):
        return labouchere_ea_strategy()
