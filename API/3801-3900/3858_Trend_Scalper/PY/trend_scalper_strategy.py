import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import (
    DataType, CandleStates, Level1Fields, OrderTypes, OrderStates, Sides,
    Unit, UnitTypes,
)
from StockSharp.BusinessEntities import Order, Subscription
from StockSharp.Algo.Strategies import Strategy


class trend_scalper_strategy(Strategy):
    """Fast close EMA, slow close SMA, and previous-bar high/low breakout bands."""

    def __init__(self):
        super(trend_scalper_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 6).SetGreaterThanZero()
        self._slow_length = self.Param("SlowLength", 12).SetGreaterThanZero()
        self._breakout_window = self.Param("BreakoutWindow", 6).SetGreaterThanZero()
        self._fixed_volume = self.Param("FixedVolume", 0.1).SetGreaterThanZero()
        self._money_management_mode = self.Param("MoneyManagementMode", 0)
        self._money_management_risk = self.Param("MoneyManagementRisk", 40.0).SetNotNegative()
        self._stop_loss_points = self.Param("StopLossPoints", 50.0).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._clear_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _clear_state(self):
        self._fast_seed = []
        self._slow_closes = []
        self._highs = []
        self._lows = []
        self._fast_ema = None
        self._pending_order = None

    def OnReseted(self):
        super(trend_scalper_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        super(trend_scalper_strategy, self).OnStarted2(time)
        self._clear_state()
        step = float(self.Security.PriceStep or 0)
        if step <= 0:
            raise ValueError("Trend Scalper requires a positive PriceStep.")
        if self._stop_loss_points.Value > 0:
            self.StartProtection(
                Unit(), Unit(Decimal(float(self._stop_loss_points.Value) * step), UnitTypes.Absolute),
                useMarketOrders=True, isLocalStop=True)
            for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
                quotes = Subscription(DataType.Level1, self.Security)
                quotes.MarketData.BuildField = field
                self.SubscribeLevel1(quotes).Bind(self._observe_quote).Start()
        self.SubscribeCandles(self.candle_type).Bind(self._process_candle).Start()

    def _observe_quote(self, message):
        # Keeps native protection informed between completed candles.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        has_band = len(self._highs) == self._breakout_window.Value
        band_high = max(self._highs) if has_band else 0.0
        band_low = min(self._lows) if has_band else 0.0
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > self._breakout_window.Value:
            self._highs.pop(0)
            self._lows.pop(0)

        close = float(candle.ClosePrice)
        fast_length = self._fast_length.Value
        if self._fast_ema is None:
            self._fast_seed.append(close)
            if len(self._fast_seed) == fast_length:
                self._fast_ema = sum(self._fast_seed) / fast_length
        else:
            self._fast_ema += (close - self._fast_ema) * 2.0 / (fast_length + 1)

        self._slow_closes.append(close)
        if len(self._slow_closes) > self._slow_length.Value:
            self._slow_closes.pop(0)

        if not has_band or self._fast_ema is None or len(self._slow_closes) < self._slow_length.Value:
            return
        if (self._pending_order is not None and
                self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return

        if self.Position > 0 and float(candle.HighPrice) > band_high:
            self._submit(Sides.Sell, float(self.Position), "Trend scalper band exit")
            return
        if self.Position < 0 and float(candle.LowPrice) < band_low:
            self._submit(Sides.Buy, abs(float(self.Position)), "Trend scalper band exit")
            return
        if self.Position != 0:
            return

        slow = sum(self._slow_closes) / len(self._slow_closes)
        if self._fast_ema > slow and float(candle.LowPrice) <= band_low:
            self._enter(Sides.Buy)
        elif self._fast_ema < slow and float(candle.HighPrice) >= band_high:
            self._enter(Sides.Sell)

    def _enter(self, side):
        volume = self._calculate_volume()
        if volume > 0:
            self._submit(side, volume, "Trend scalper entry")

    def _calculate_volume(self):
        volume = float(self._fixed_volume.Value)
        mode = self._money_management_mode.Value
        if mode != 0:
            balance = (self.Portfolio.CurrentValue or self.Portfolio.BeginValue
                       if self.Portfolio is not None else 0)
            balance = float(balance or 0)
            if balance > 0:
                fraction = math.ceil(balance * float(self._money_management_risk.Value) / 10000.0) / 10.0
                volume = min(100.0, max(1.0, math.ceil(fraction)) if mode > 0 else fraction)

        step = float(self.Security.VolumeStep or 0)
        if step > 0:
            volume = math.floor(volume / step) * step
        minimum = float(self.Security.MinVolume or 0)
        maximum = float(self.Security.MaxVolume or 0)
        if minimum > 0:
            volume = max(volume, minimum)
        if maximum > 0:
            volume = min(volume, maximum)
        return volume

    def _submit(self, side, volume, comment):
        order = Order()
        order.Security = self.Security
        order.Portfolio = self.Portfolio
        order.Type = OrderTypes.Market
        order.Side = side
        order.Volume = Decimal(volume)
        order.Comment = comment
        self._pending_order = order
        self.RegisterOrder(order)

    def CreateClone(self):
        return trend_scalper_strategy()
