import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy


class three_ema_cross_strategy(Strategy):
    """
    Three EMA Cross Strategy.
    After the fast EMA crosses above the slow EMA, a long opens within CrossBackBars bars on a pullback whose low touches the
    fast EMA while the close stays at or above both the fast EMA and the trend EMA. The long closes when the fast EMA drops
    below the slow EMA, and a percent stop limits the loss.
    """

    def __init__(self):
        super(three_ema_cross_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 20).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._trend_ema_length = self.Param("TrendEmaLength", 100).SetGreaterThanZero().SetDisplay("Trend EMA", "Trend EMA period", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._cross_back_bars = self.Param("CrossBackBars", 10).SetGreaterThanZero().SetDisplay("Cross Back Bars", "Bars after the cross during which a pullback entry is allowed", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._bars_since_cross = None

    def OnReseted(self):
        super(three_ema_cross_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(three_ema_cross_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self._trend_ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, trend_ema, self._process_candle).Start()

        stop = float(self._stop_loss_percent.Value)
        if stop > 0:
            self.StartProtection(Unit(), Unit(Decimal(stop), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

            # The stop has to see prices between candles, not only at their close.
            for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
                quotes = Subscription(DataType.Level1, self.Security)
                quotes.MarketData.BuildField = field
                self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawIndicator(area, trend_ema)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, fast_value, slow_value, trend_value):
        if candle.State != CandleStates.Finished:
            return

        if not fast_value.IsFormed or not slow_value.IsFormed:
            return

        fast = float(fast_value.GetValue[Decimal](None))
        slow = float(slow_value.GetValue[Decimal](None))

        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if prev_fast is not None and prev_slow is not None and prev_fast <= prev_slow and fast > slow:
            self._bars_since_cross = 0
        elif self._bars_since_cross is not None:
            self._bars_since_cross += 1

        if not trend_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        trend = float(trend_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)

        if self.Position > 0:
            if fast < slow:
                self.SellMarket(self.Position)
            return

        recent_cross = self._bars_since_cross is not None and self._bars_since_cross < self._cross_back_bars.Value

        if self.Position == 0 and recent_cross and close >= fast and low <= fast and trend <= close:
            self.BuyMarket(self.Volume)

    def CreateClone(self):
        return three_ema_cross_strategy()
