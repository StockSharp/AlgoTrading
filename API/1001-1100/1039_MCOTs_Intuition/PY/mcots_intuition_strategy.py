import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StandardDeviation, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class mcots_intuition_strategy(Strategy):
    """
    MCOTs Intuition strategy.
    Momentum is the change of RSI from the previous candle and its standard deviation is measured over RsiPeriod values.
    A long opens when momentum exceeds the deviation times StdDevMultiplier while staying below the previous momentum times
    ExhaustionMultiplier (strong but fading); a short opens on the mirrored condition. Exits are a fixed profit target and
    stop loss in ticks.
    """

    def __init__(self):
        super(mcots_intuition_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "RSI period and deviation window", "Indicators")
        self._std_dev_multiplier = self.Param("StdDevMultiplier", 1.0).SetNotNegative().SetDisplay("StdDev Multiplier", "Multiplier of the momentum standard deviation", "Indicators")
        self._exhaustion_multiplier = self.Param("ExhaustionMultiplier", 1.0).SetNotNegative().SetDisplay("Exhaustion Multiplier", "Multiplier of the previous momentum", "Indicators")
        self._profit_target_ticks = self.Param("ProfitTargetTicks", 40).SetNotNegative().SetDisplay("Profit Target Ticks", "Profit target in ticks", "Risk")
        self._stop_loss_ticks = self.Param("StopLossTicks", 160).SetNotNegative().SetDisplay("Stop Loss Ticks", "Stop loss in ticks", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._std_dev = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_rsi = None
        self._prev_momentum = None

    def OnReseted(self):
        super(mcots_intuition_strategy, self).OnReseted()
        self._std_dev = None
        self._reset_state()

    def OnStarted2(self, time):
        super(mcots_intuition_strategy, self).OnStarted2(time)

        self._reset_state()

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        self._std_dev = StandardDeviation()
        self._std_dev.Length = self._rsi_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, self._process_candle).Start()

        step = self.Security.PriceStep if self.Security is not None and self.Security.PriceStep is not None else Decimal(1)
        take_ticks = self._profit_target_ticks.Value
        stop_ticks = self._stop_loss_ticks.Value
        take = Unit(Decimal(take_ticks) * step, UnitTypes.Absolute) if take_ticks > 0 else Unit()
        stop = Unit(Decimal(stop_ticks) * step, UnitTypes.Absolute) if stop_ticks > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True, isLocalStop=True)

        # The target and stop have to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished or not rsi_value.IsFormed:
            return

        rsi = rsi_value.GetValue[Decimal](None)

        if self._prev_rsi is None:
            self._prev_rsi = rsi
            return

        momentum = rsi - self._prev_rsi
        self._prev_rsi = rsi

        indicator_input = DecimalIndicatorValue(self._std_dev, momentum, candle.OpenTime)
        indicator_input.IsFinal = True
        std_dev_value = self._std_dev.Process(indicator_input)

        prev_momentum = self._prev_momentum
        self._prev_momentum = momentum

        if not self._std_dev.IsFormed or prev_momentum is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        band = std_dev_value.GetValue[Decimal](None) * Decimal(self._std_dev_multiplier.Value)
        exhaustion = prev_momentum * Decimal(self._exhaustion_multiplier.Value)

        if momentum > band and momentum < exhaustion and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif momentum < Decimal.Negate(band) and momentum > exhaustion and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return mcots_intuition_strategy()
