import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import Decimal, TimeSpan
from StockSharp.Messages import DataType, CandleStates, OrderStates, Unit, UnitTypes, Level1Fields
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription


class exodus_strategy(Strategy):
    """Normalized volume-weighted momentum with ADX filtering and native ATR protection."""

    def __init__(self):
        super(exodus_strategy, self).__init__()
        self._vwmo_momentum = self.Param("VwmoMomentum", 10).SetGreaterThanZero() \
            .SetDisplay("VWMO Momentum", "Lag for price momentum.", "VWMO")
        self._vwmo_volume = self.Param("VwmoVolume", 20).SetGreaterThanZero() \
            .SetDisplay("VWMO Volume", "Volume-weighted momentum window.", "VWMO")
        self._vwmo_smooth = self.Param("VwmoSmooth", 3).SetGreaterThanZero() \
            .SetDisplay("VWMO Smooth", "SMA smoothing period of normalized oscillator.", "VWMO")
        self._vwmo_threshold = self.Param("VwmoThreshold", 25.0).SetNotNegative() \
            .SetDisplay("VWMO Threshold", "Absolute normalized oscillator threshold.", "VWMO")
        self._adx_length = self.Param("AdxLength", 14).SetGreaterThanZero() \
            .SetDisplay("ADX Length", "Average Directional Index period.", "Strength")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetNotNegative() \
            .SetDisplay("ADX Threshold", "Minimum trend strength.", "Strength")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero() \
            .SetDisplay("ATR Length", "Wilder ATR period for entry protection.", "Protection")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero() \
            .SetDisplay("ATR Multiplier", "Initial stop as a multiple of ATR.", "Protection")
        self._tp_multiplier = self.Param("TpMultiplier", 3.0).SetGreaterThanZero() \
            .SetDisplay("TP Multiplier", "Take-profit distance as a stop multiple.", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))) \
            .SetDisplay("Candle Type", "Candle timeframe.", "General")
        self._history = []
        self._oscillator = []
        self._previous_vwmo = None
        self._previous_close = None
        self._atr = 0.0
        self._atr_samples = 0
        self._pending_order = None
        self._protection_started = False
        self._stop_distance = None
        self._target_distance = None

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Level1)]

    def OnReseted(self):
        super(exodus_strategy, self).OnReseted()
        self._history = []
        self._oscillator = []
        self._previous_vwmo = None
        self._previous_close = None
        self._atr = 0.0
        self._atr_samples = 0
        self._pending_order = None
        self._protection_started = False
        self._stop_distance = None
        self._target_distance = None

    def OnStarted2(self, time):
        super(exodus_strategy, self).OnStarted2(time)
        self._adx = AverageDirectionalIndex()
        self._adx.Length = int(self._adx_length.Value)
        self._stop_distance = Unit(Decimal.Zero, UnitTypes.Absolute)
        self._target_distance = Unit(Decimal.Zero, UnitTypes.Absolute)
        self.OrderRegistering += self._on_order_registering
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self.on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._adx)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection is evaluated on incoming bid/ask updates between bars.
        pass

    def _on_order_registering(self, order):
        self._pending_order = order

    def _configure_protection(self, atr):
        if atr <= 0.0:
            raise ValueError("A positive ATR is required for protected entries")
        stop = atr * float(self._atr_multiplier.Value)
        target = stop * float(self._tp_multiplier.Value)
        self._stop_distance.Value = Decimal(stop)
        self._target_distance.Value = Decimal(target)
        if not self._protection_started:
            self.StartProtection(self._target_distance, self._stop_distance,
                                 useMarketOrders=True, isLocalStop=True)
            self._protection_started = True

    def on_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        value = self._adx.Process(candle)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        volume = float(candle.TotalVolume)
        true_range = (max(high - low, abs(high - self._previous_close),
                          abs(low - self._previous_close))
                      if self._previous_close is not None else high - low)
        self._atr_samples += 1
        atr_count = min(self._atr_samples, int(self._atr_length.Value))
        self._atr = (self._atr * (atr_count - 1) + true_range) / atr_count
        self._previous_close = close

        momentum_period = int(self._vwmo_momentum.Value)
        volume_period = int(self._vwmo_volume.Value)
        smooth = int(self._vwmo_smooth.Value)
        need = momentum_period + volume_period
        self._history.append((close, volume))
        if len(self._history) > need:
            self._history.pop(0)

        strength = value.MovingAverage
        if len(self._history) < need or not value.IsFormed or strength is None:
            return

        numerator = 0.0
        denominator = 0.0
        for i in range(len(self._history) - volume_period, len(self._history)):
            weighted_delta = (self._history[i][0] - self._history[i - momentum_period][0]) * self._history[i][1]
            numerator += weighted_delta
            denominator += abs(weighted_delta)
        self._oscillator.append(100.0 * numerator / denominator if denominator > 0.0 else 0.0)
        if len(self._oscillator) > smooth:
            self._oscillator.pop(0)
        if len(self._oscillator) < smooth:
            return

        oscillator = sum(self._oscillator) / smooth
        active = self._pending_order is not None and self._pending_order.State not in (
            OrderStates.Done, OrderStates.Failed)
        if not active:
            strong = float(strength) > float(self._adx_threshold.Value)
            long_signal = strong and oscillator > float(self._vwmo_threshold.Value)
            short_signal = strong and oscillator < -float(self._vwmo_threshold.Value)
            position = self.Position
            zero_exit = (position > 0 and self._previous_vwmo is not None
                         and self._previous_vwmo > 0 and oscillator <= 0) or (
                         position < 0 and self._previous_vwmo is not None
                         and self._previous_vwmo < 0 and oscillator >= 0)
            if long_signal and position <= 0:
                self._configure_protection(self._atr)
                self.BuyMarket(self.Volume + abs(position))
            elif short_signal and position >= 0:
                self._configure_protection(self._atr)
                self.SellMarket(self.Volume + abs(position))
            elif zero_exit and position != 0:
                if position > 0:
                    self.SellMarket(position)
                else:
                    self.BuyMarket(abs(position))
        self._previous_vwmo = oscillator

    def CreateClone(self):
        return exodus_strategy()
