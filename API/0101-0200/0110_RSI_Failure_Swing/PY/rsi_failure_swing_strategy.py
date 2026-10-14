import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_failure_swing_strategy(Strategy):
    """
    RSI Failure Swing strategy.
    A trough of RSI below OversoldLevel that is higher than the previous such trough arms a long, which opens when RSI then crosses
    back above OversoldLevel; peaks above OverboughtLevel arm shorts the same way. A long closes when RSI crosses above OverboughtLevel,
    a short when it crosses below OversoldLevel, and a percent stop limits the loss.
    """

    def __init__(self):
        super(rsi_failure_swing_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period for RSI", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", 30.0).SetDisplay("Oversold Level", "RSI level below which troughs count", "Levels")
        self._overbought_level = self.Param("OverboughtLevel", 70.0).SetDisplay("Overbought Level", "RSI level above which peaks count", "Levels")
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

    def OnReseted(self):
        super(rsi_failure_swing_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(rsi_failure_swing_strategy, self).OnStarted2(time)

        self._reset_state()

        oscillator = RelativeStrengthIndex()
        oscillator.Length = self._rsi_period.Value

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
        if candle.State != CandleStates.Finished or not value.IsFormed or value.IsEmpty:
            return

        current = value.GetValue[Decimal](None)
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

            long_trigger = last <= os_level and current > os_level
            short_trigger = last >= ob_level and current < ob_level
            armed_long = self._armed_long if long_trigger else None
            armed_short = self._armed_short if short_trigger else None
            if long_trigger:
                self._armed_long = None
            if short_trigger:
                self._armed_short = None

            if self.IsFormedAndOnlineAndAllowTrading():
                if self.Position > 0:
                    if last <= ob_level and current > ob_level:
                        self.SellMarket(self.Position)
                elif self.Position < 0:
                    if last >= os_level and current < os_level:
                        self.BuyMarket(-self.Position)
                elif armed_long is not None:
                    self.BuyMarket(self.Volume)
                    self._exit_level = armed_long
                elif armed_short is not None:
                    self.SellMarket(self.Volume)
                    self._exit_level = armed_short

        self._before_last = self._last
        self._last = current

    def CreateClone(self):
        return rsi_failure_swing_strategy()
