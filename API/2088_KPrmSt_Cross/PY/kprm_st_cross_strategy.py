import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class kprm_st_cross_strategy(Strategy):
    """
    KPrmSt cross strategy using Stochastic K/D crossover.
    Buys when K crosses above D, sells when K crosses below D.
    Uses StartProtection for percentage-based SL/TP.
    """

    def __init__(self):
        super(kprm_st_cross_strategy, self).__init__()
        self._stop_loss_pct = self.Param("StopLossPct", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_pct = self.Param("TakeProfitPct", 3.0) \
            .SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame", "General")

        self._stochastic = None
        self._prev_k = None
        self._prev_d = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(kprm_st_cross_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted(self, time):
        super(kprm_st_cross_strategy, self).OnStarted(time)

        self._stochastic = StochasticOscillator()
        self.Indicators.Add(self._stochastic)

        self.StartProtection(
            Unit(self._take_profit_pct.Value, UnitTypes.Percent),
            Unit(self._stop_loss_pct.Value, UnitTypes.Percent))

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        stoch_result = self._stochastic.Process(candle)
        if not stoch_result.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        k = stoch_result.K
        d = stoch_result.D
        if k is None or d is None:
            return

        k = float(k)
        d = float(d)

        if self._prev_k is not None and self._prev_d is not None:
            was_below = self._prev_k < self._prev_d
            is_above = k > d

            if was_below and is_above and self.Position <= 0:
                if self.Position < 0:
                    self.BuyMarket()
                self.BuyMarket()
            elif not was_below and not is_above and self._prev_k > self._prev_d and self.Position >= 0:
                if self.Position > 0:
                    self.SellMarket()
                self.SellMarket()

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return kprm_st_cross_strategy()
