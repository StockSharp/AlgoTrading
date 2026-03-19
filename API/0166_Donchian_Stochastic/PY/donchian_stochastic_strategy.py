import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import DonchianChannels, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class donchian_stochastic_strategy(Strategy):
    """
    Donchian Channel + Stochastic strategy.
    Enters when price breaks Donchian Channel with Stochastic confirmation.
    """

    def __init__(self):
        super(donchian_stochastic_strategy, self).__init__()
        self._donchian_period = self.Param("DonchianPeriod", 20) \
            .SetDisplay("Donchian Period", "Donchian Channel lookback period", "Indicators")
        self._stoch_k = self.Param("StochK", 3) \
            .SetDisplay("Stochastic %K", "Stochastic %K period", "Indicators")
        self._stoch_d = self.Param("StochD", 3) \
            .SetDisplay("Stochastic %D", "Stochastic %D period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(donchian_stochastic_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(donchian_stochastic_strategy, self).OnStarted(time)

        donchian = DonchianChannels()
        donchian.Length = self._donchian_period.Value
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_k.Value
        stochastic.D.Length = self._stoch_d.Value

        sl_pct = self._stop_loss_percent.Value
        self.StartProtection(
            Unit(0.0, UnitTypes.Absolute),
            Unit(float(sl_pct), UnitTypes.Percent)
        )

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(donchian, stochastic, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, donchian)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, donchian_value, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        upper = donchian_value.UpperBand
        lower = donchian_value.LowerBand
        middle = donchian_value.Middle
        if upper is None or lower is None or middle is None:
            return

        upper = float(upper)
        lower = float(lower)
        middle = float(middle)

        stoch_k = stoch_value.K
        if stoch_k is None:
            return
        stoch_k = float(stoch_k)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        price = float(candle.ClosePrice)

        if price >= middle and stoch_k > 55 and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif price <= middle and stoch_k < 45 and self.Position == 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0 and price < middle:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and price > middle:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return donchian_stochastic_strategy()
