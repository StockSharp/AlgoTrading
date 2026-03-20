import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import WilliamsR
from StockSharp.Algo.Strategies import Strategy


class exp_xwpr_histogram_vol_direct_strategy(Strategy):
    def __init__(self):
        super(exp_xwpr_histogram_vol_direct_strategy, self).__init__()

        self._williams_period = self.Param("WilliamsPeriod", 14) \
            .SetDisplay("Williams %R Period", "Lookback for the Williams %R oscillator", "Indicator")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 5) \
            .SetDisplay("Signal Cooldown", "Bars to wait between new entries", "Trading Rules")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle type used for analysis", "General")

        self._williams = None
        self._previous_zone = None
        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def WilliamsPeriod(self):
        return self._williams_period.Value
    @property
    def SignalCooldownBars(self):
        return self._signal_cooldown_bars.Value

    def OnReseted(self):
        super(exp_xwpr_histogram_vol_direct_strategy, self).OnReseted()
        self._williams = None
        self._previous_zone = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(exp_xwpr_histogram_vol_direct_strategy, self).OnStarted(time)
        self._williams = WilliamsR()
        self._williams.Length = self.WilliamsPeriod
        self._previous_zone = None
        self._cooldown_remaining = 0

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._on_process).Start()
        self.StartProtection(Unit(2, UnitTypes.Percent), Unit(1, UnitTypes.Percent))

    def _on_process(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1

        williams_value = self._williams.Process(candle)
        if not self._williams.IsFormed:
            return

        wpr_value = float(williams_value.ToDecimal())
        normalized = wpr_value + 100.0
        bullish_level = 80.0
        bearish_level = 20.0

        if normalized >= bullish_level:
            zone = 1
        elif normalized <= bearish_level:
            zone = -1
        else:
            zone = 0

        if self._previous_zone is None:
            self._previous_zone = zone
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._previous_zone = zone
            return

        if self._previous_zone != zone and self._cooldown_remaining == 0 and self.Position == 0:
            if zone > 0:
                self.BuyMarket()
                self._cooldown_remaining = self.SignalCooldownBars
            elif zone < 0:
                self.SellMarket()
                self._cooldown_remaining = self.SignalCooldownBars

        self._previous_zone = zone

    def CreateClone(self):
        return exp_xwpr_histogram_vol_direct_strategy()
