import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import StochasticOscillator, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class stochastic_martingale_grid_strategy(Strategy):
    """Stochastic-based martingale averaging strategy. Enters on K/D crossover
    in oversold/overbought zones. Uses StartProtection for SL/TP."""

    def __init__(self):
        super(stochastic_martingale_grid_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe used to evaluate stochastic values", "General")
        self._base_volume = self.Param("BaseVolume", 0.1) \
            .SetGreaterThanZero() \
            .SetDisplay("Base Volume", "Initial order volume", "Trading")
        self._take_profit_pips = self.Param("TakeProfitPips", 50.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Take Profit (pips)", "Distance to the take profit target for each entry", "Risk")
        self._trailing_stop_pips = self.Param("TrailingStopPips", 20.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Trailing Stop (pips)", "Trailing stop distance applied per entry", "Risk")
        self._max_orders = self.Param("MaxOrders", 2) \
            .SetGreaterThanZero() \
            .SetDisplay("Max Orders", "Maximum number of simultaneous averaging entries", "Martingale")
        self._step_pips = self.Param("StepPips", 7.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Step (pips)", "Adverse move required before adding a new entry", "Martingale")
        self._k_period = self.Param("KPeriod", 5) \
            .SetGreaterThanZero() \
            .SetDisplay("%K Period", "Stochastic %K lookback length", "Indicators")
        self._d_period = self.Param("DPeriod", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("%D Period", "Stochastic %D smoothing length", "Indicators")
        self._slowing = self.Param("Slowing", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Slowing", "Additional smoothing applied to %K", "Indicators")
        self._zone_buy = self.Param("ZoneBuy", 50.0) \
            .SetDisplay("Buy Zone", "Upper limit that allows long setups when %K is above %D", "Indicators")
        self._zone_sell = self.Param("ZoneSell", 50.0) \
            .SetDisplay("Sell Zone", "Lower limit that allows short setups when %K is below %D", "Indicators")

        self._stochastic = None
        self._previous_main = None
        self._previous_signal = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def BaseVolume(self):
        return self._base_volume.Value

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

    def OnReseted(self):
        super(stochastic_martingale_grid_strategy, self).OnReseted()
        self._stochastic = None
        self._previous_main = None
        self._previous_signal = None

    def OnStarted2(self, time):
        super(stochastic_martingale_grid_strategy, self).OnStarted2(time)

        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.KPeriod
        self._stochastic.D.Length = self.DPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._process_candle).Start()

        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        stoch_result = self._stochastic.Process(CandleIndicatorValue(self._stochastic, candle))
        if not self._stochastic.IsFormed:
            return

        k_raw = stoch_result.K if hasattr(stoch_result, 'K') else None
        d_raw = stoch_result.D if hasattr(stoch_result, 'D') else None

        if k_raw is None or d_raw is None:
            return

        current_main = float(k_raw)
        current_signal = float(d_raw)

        if self.Position != 0:
            self._previous_main = current_main
            self._previous_signal = current_signal
            return

        if self._previous_main is not None and self._previous_signal is not None:
            prev_main = self._previous_main
            prev_signal = self._previous_signal

            # Buy: K crosses above D in oversold zone
            if prev_main <= prev_signal and current_main > current_signal and \
                    current_signal < float(self.ZoneBuy):
                self.BuyMarket()
            # Sell: K crosses below D in overbought zone
            elif prev_main >= prev_signal and current_main < current_signal and \
                    current_signal > float(self.ZoneSell):
                self.SellMarket()

        self._previous_main = current_main
        self._previous_signal = current_signal

    def CreateClone(self):
        return stochastic_martingale_grid_strategy()
