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

class stochastic_failure_swing_strategy(Strategy):
    """
    K Failure Swing strategy.
    A trough of %K below OversoldLevel that is higher than the previous such trough arms a long, which opens when %K then crosses
    above %D; peaks above OverboughtLevel arm shorts the same way. A position closes when %K crosses back through the swing that armed it,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(stochastic_failure_swing_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14).SetGreaterThanZero().SetDisplay("K Period", "Period for %K", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetGreaterThanZero().SetDisplay("D Period", "Period for %D", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 20.0).SetDisplay("Oversold Level", "%K level below which troughs count", "Levels")
        self._overbought_level = self.Param("OverboughtLevel", 80.0).SetDisplay("Overbought Level", "%K level above which peaks count", "Levels")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        # The last two values, the last trough and peak in the extreme zones, and the swings that arm an entry.
        self._last = None
        self._before_last = None
        self._last_trough = None
        self._last_peak = None
        self._armed_long = None
        self._armed_short = None
        self._exit_level = Decimal(0)
        self._prev_k = None
        self._prev_d = None

    def OnReseted(self):
        super(stochastic_failure_swing_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(stochastic_failure_swing_strategy, self).OnStarted2(time)

        self._reset_state()

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
        if candle.State != CandleStates.Finished or not value.IsFormed or value.IsEmpty or value.K is None or value.D is None:
            return

        current = value.K
        signal = value.D
        os_level = Decimal(self._oversold_level.Value)
        ob_level = Decimal(self._overbought_level.Value)
        last = self._last

        if last is not None:
            # The previous value is a trough or a peak once the current one turns away from it.
            before_last = self._before_last
            if before_last is not None:
                if last < before_last and last < current and last < os_level:
                    self._armed_long = last if self._last_trough is not None and last > self._last_trough else None
                    self._last_trough = last
                elif last > before_last and last > current and last > ob_level:
                    self._armed_short = last if self._last_peak is not None and last < self._last_peak else None
                    self._last_peak = last

            long_trigger = self._prev_k is not None and self._prev_d is not None and self._prev_k <= self._prev_d and current > signal
            short_trigger = self._prev_k is not None and self._prev_d is not None and self._prev_k >= self._prev_d and current < signal
            armed_long = self._armed_long if long_trigger else None
            armed_short = self._armed_short if short_trigger else None
            if long_trigger:
                self._armed_long = None
            if short_trigger:
                self._armed_short = None

            if self.IsFormedAndOnlineAndAllowTrading():
                if self.Position > 0:
                    if current < self._exit_level:
                        self.SellMarket(self.Position)
                elif self.Position < 0:
                    if current > self._exit_level:
                        self.BuyMarket(-self.Position)
                elif armed_long is not None:
                    self.BuyMarket(self.Volume)
                    self._exit_level = armed_long
                elif armed_short is not None:
                    self.SellMarket(self.Volume)
                    self._exit_level = armed_short

        self._before_last = self._last
        self._last = current
        self._prev_k = current
        self._prev_d = signal

    def CreateClone(self):
        return stochastic_failure_swing_strategy()
