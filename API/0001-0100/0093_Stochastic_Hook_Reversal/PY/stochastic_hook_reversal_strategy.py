import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class stochastic_hook_reversal_strategy(Strategy):
    """
    Stochastic Hook Reversal strategy.
    A long opens when %K was below the oversold level on the previous candle and turns up while the low makes a new low
    against the previous candle; a short opens when it was above the overbought level and turns down while the high makes a new high.
    The position closes when %K turns the other way, reversing on the opposite signal, and a percent stop limits the loss.
    """

    def __init__(self):
        super(stochastic_hook_reversal_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14).SetGreaterThanZero().SetDisplay("K Period", "Period for %K", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetGreaterThanZero().SetDisplay("D Period", "Period for %D", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 20).SetDisplay("Oversold Level", "%K level of the oversold zone", "Levels")
        self._overbought_level = self.Param("OverboughtLevel", 80).SetDisplay("Overbought Level", "%K level of the overbought zone", "Levels")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_value = None
        self._prev_high = Decimal(0)
        self._prev_low = Decimal(0)

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(stochastic_hook_reversal_strategy, self).OnReseted()
        self._prev_value = None
        self._prev_high = Decimal(0)
        self._prev_low = Decimal(0)

    def OnStarted2(self, time):
        super(stochastic_hook_reversal_strategy, self).OnStarted2(time)

        self._prev_value = None
        self._prev_high = Decimal(0)
        self._prev_low = Decimal(0)

        oscillator = StochasticOscillator()
        oscillator.K.Length = self._k_period.Value
        oscillator.D.Length = self._d_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(oscillator, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, oscillator)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, value):
        if candle.State != CandleStates.Finished or not value.IsFormed or value.IsEmpty or value.K is None:
            return

        current = value.K
        previous = self._prev_value
        prev_high = self._prev_high
        prev_low = self._prev_low

        self._prev_value = current
        self._prev_high = candle.HighPrice
        self._prev_low = candle.LowPrice

        if previous is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        hooks_up = current > previous
        hooks_down = current < previous
        long_signal = previous < Decimal(self._oversold_level.Value) and hooks_up and candle.LowPrice < prev_low
        short_signal = previous > Decimal(self._overbought_level.Value) and hooks_down and candle.HighPrice > prev_high

        if self.Position > 0:
            if hooks_down:
                self.SellMarket(self.Volume + self.Position if short_signal else self.Position)
        elif self.Position < 0:
            if hooks_up:
                self.BuyMarket(self.Volume - self.Position if long_signal else -self.Position)
        elif long_signal:
            self.BuyMarket(self.Volume)
        elif short_signal:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return stochastic_hook_reversal_strategy()
