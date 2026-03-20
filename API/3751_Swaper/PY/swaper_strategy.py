import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Messages import Sides


class swaper_strategy(Strategy):
    def __init__(self):
        super(swaper_strategy, self).__init__()

        self._experts = self.Param("Experts", 1) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")
        self._begin_price = self.Param("BeginPrice", 1.8014) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")
        self._magic_number = self.Param("MagicNumber", 777) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")
        self._base_units = self.Param("BaseUnits", 1000) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")
        self._contract_multiplier = self.Param("ContractMultiplier", 10) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")
        self._margin_per_lot = self.Param("MarginPerLot", 1000) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")
        self._fallback_spread_steps = self.Param("FallbackSpreadSteps", 1) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")
        self._candle_type = self.Param("CandleType", TimeSpan.FromHours(1) \
            .SetDisplay("Experts", "Weighting coefficient applied to the synthetic fair value.", "General")

        self._initial_capital = 0.0
        self._realized_pn_l = 0.0
        self._position_volume = 0.0
        self._average_price = 0.0
        self._best_bid = None
        self._best_ask = None
        self._previous_candle = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(swaper_strategy, self).OnReseted()
        self._initial_capital = 0.0
        self._realized_pn_l = 0.0
        self._position_volume = 0.0
        self._average_price = 0.0
        self._best_bid = None
        self._best_ask = None
        self._previous_candle = None

    def OnStarted(self, time):
        super(swaper_strategy, self).OnStarted(time)


        candle_subscription = self.SubscribeCandles(self.candle_type)
        candle_subscription.Bind(self._process_candle).Start()

    def _process_candle(self, candle, *args):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        # Trading logic placeholder
        pass

    def CreateClone(self):
        return swaper_strategy()
