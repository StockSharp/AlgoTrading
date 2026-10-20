import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import ArgumentOutOfRangeException, Decimal, InvalidOperationException, Math, TimeSpan
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import CandleStates, DataType, Level1Fields, OrderStates, OrderTypes, Sides, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange, HurstExponent, Ichimoku, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class ichimoku_hurst_exponent_strategy(Strategy):
    """Ichimoku cloud crossings, formed log-return Hurst filtering and frozen ATR protection."""

    def __init__(self):
        super(ichimoku_hurst_exponent_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetDisplay("Tenkan Period", "Tenkan rolling high/low midpoint period", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetDisplay("Kijun Period", "Kijun midpoint period and cloud displacement", "Ichimoku")
        self._senkou_spanb_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetDisplay("Senkou Span B Period", "Senkou B rolling high/low midpoint period", "Ichimoku")
        self._hurst_period = self.Param("HurstPeriod", 100) \
            .SetDisplay("Hurst Period", "Closes in the Hurst log-return window", "Hurst Exponent")
        self._hurst_threshold = self.Param("HurstThreshold", 0.5) \
            .SetDisplay("Hurst Threshold", "Strict lower threshold for the Hurst estimate", "Hurst Exponent")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Signal candle type", "General")
        self._signal_cooldown_bars = self.Param("SignalCooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Signal Cooldown", "Finished candles between signal orders; exits remain active", "Trading")
        self._atr_period = self.Param("AtrPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Period", "Wilder ATR period", "Risk")
        self._stop_loss_atr = self.Param("StopLossAtr", 2.0) \
            .SetNotNegative() \
            .SetDisplay("ATR Stop", "Entry ATR multiplier; zero disables the stop for the run", "Risk")
        self._ichimoku = None
        self._hurst = None
        self._atr = None
        self._previous_close = None
        self._previous_tenkan = None
        self._previous_kijun = None
        self._cooldown_remaining = 0
        self._pending_order = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)] if self.Security is not None else []

    def OnReseted(self):
        super(ichimoku_hurst_exponent_strategy, self).OnReseted()
        self._ichimoku = None
        self._hurst = None
        self._atr = None
        self._previous_close = None
        self._previous_tenkan = None
        self._previous_kijun = None
        self._cooldown_remaining = 0
        self._pending_order = None

    def OnStarted2(self, time):
        super(ichimoku_hurst_exponent_strategy, self).OnStarted2(time)
        if self.Security is None:
            raise InvalidOperationException("Security is not specified.")
        period = int(self._hurst_period.Value)
        if period < 11:
            raise ArgumentOutOfRangeException("HurstPeriod", "At least eleven closes are required for ten log returns.")
        self._ichimoku = Ichimoku()
        self._ichimoku.Tenkan.Length = int(self._tenkan_period.Value)
        self._ichimoku.Kijun.Length = int(self._kijun_period.Value)
        self._ichimoku.SenkouB.Length = int(self._senkou_spanb_period.Value)
        self._hurst = HurstExponent()
        self._hurst.Length = period - 1
        self._atr = AverageTrueRange()
        self._atr.Length = int(self._atr_period.Value)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ichimoku, self._atr, self._process_candle, True).Start()
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ichimoku)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, cloud, atr_value):
        if candle.State != CandleStates.Finished:
            return
        close = candle.ClosePrice
        hurst = None
        if close > 0:
            if self._previous_close is not None:
                log_return = Decimal(Math.Log(float(close / self._previous_close)))
                value = IndicatorHelper.Process(self._hurst, log_return, candle.OpenTime, True)
                if not value.IsEmpty:
                    hurst = value.GetValue[Decimal](None)
            self._previous_close = close
        else:
            self._previous_close = None
            self._hurst.Reset()
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        tenkan, kijun, span_a, span_b = cloud.Tenkan, cloud.Kijun, cloud.SenkouA, cloud.SenkouB
        if any(value is None for value in (tenkan, kijun, span_a, span_b)):
            return
        cross_up = self._previous_tenkan is not None and self._previous_kijun is not None \
            and self._previous_tenkan <= self._previous_kijun and tenkan > kijun
        cross_down = self._previous_tenkan is not None and self._previous_kijun is not None \
            and self._previous_tenkan >= self._previous_kijun and tenkan < kijun
        self._previous_tenkan, self._previous_kijun = tenkan, kijun
        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        above = close > max(span_a, span_b)
        below = close < min(span_a, span_b)
        if self.Position > 0 and (below or cross_down):
            self._submit_order(Sides.Sell, self.Position, "Ichimoku Hurst exit")
        elif self.Position < 0 and (above or cross_up):
            self._submit_order(Sides.Buy, Math.Abs(self.Position), "Ichimoku Hurst exit")
        elif self.Position == 0 and self._cooldown_remaining == 0 \
                and hurst is not None and hurst > Decimal(self._hurst_threshold.Value) \
                and not atr_value.IsEmpty and atr_value.GetValue[Decimal](None) > 0 \
                and ((above and cross_up) or (below and cross_down)):
            distance = atr_value.GetValue[Decimal](None) * Decimal(self._stop_loss_atr.Value)
            self.StartProtection(Unit(), Unit(distance, UnitTypes.Absolute), useMarketOrders=True, isLocalStop=True)
            self._submit_order(Sides.Buy if cross_up else Sides.Sell, self.Volume, "Ichimoku Hurst entry")
        else:
            return
        self._cooldown_remaining = int(self._signal_cooldown_bars.Value)

    def _submit_order(self, side, volume, comment):
        self._pending_order = self.CreateOrder(side, Decimal(0), volume)
        self._pending_order.Type = OrderTypes.Market
        self._pending_order.Comment = comment
        self.RegisterOrder(self._pending_order)

    def CreateClone(self):
        return ichimoku_hurst_exponent_strategy()
