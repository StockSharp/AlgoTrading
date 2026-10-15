import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Math, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy


class heikin_ashi_consecutive_strategy(Strategy):
    """
    Strategy based on consecutive Heikin Ashi candles.
    It enters long position after a sequence of bullish Heikin Ashi candles and
    short position after a sequence of bearish Heikin Ashi candles,
    and exits on the first opposite candle or at the percent stop.
    """

    def __init__(self):
        super(heikin_ashi_consecutive_strategy, self).__init__()

        self._consecutive_candles = self.Param("ConsecutiveCandles", 3) \
            .SetGreaterThanZero() \
            .SetDisplay("Consecutive Candles", "Number of consecutive candles required for signal", "Trading parameters")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Stop loss as a percentage of entry price", "Risk parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = None
        self._prev_ha_close = Decimal(0)

    def OnStarted2(self, time):
        super(heikin_ashi_consecutive_strategy, self).OnStarted2(time)

        subscription = self.SubscribeCandles(self._candle_type.Value)
        subscription.Bind(self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        ha_close = (candle.OpenPrice + candle.ClosePrice + candle.HighPrice + candle.LowPrice) / Decimal(4)
        if self._prev_ha_open is None:
            ha_open = (candle.OpenPrice + candle.ClosePrice) / Decimal(2)
        else:
            ha_open = (self._prev_ha_open + self._prev_ha_close) / Decimal(2)

        self._prev_ha_open = ha_open
        self._prev_ha_close = ha_close

        is_bullish = ha_close > ha_open
        is_bearish = ha_close < ha_open

        self._bullish_count = self._bullish_count + 1 if is_bullish else 0
        self._bearish_count = self._bearish_count + 1 if is_bearish else 0

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        required = self._consecutive_candles.Value
        position = self.Position

        if position > 0:
            # The first bearish candle ends a long position.
            if is_bearish:
                self.SellMarket(self.Volume + position if self._bearish_count >= required else position)
        elif position < 0:
            if is_bullish:
                self.BuyMarket(self.Volume - position if self._bullish_count >= required else -position)
        elif self._bullish_count >= required:
            self.BuyMarket(self.Volume)
        elif self._bearish_count >= required:
            self.SellMarket(self.Volume)

    def OnReseted(self):
        super(heikin_ashi_consecutive_strategy, self).OnReseted()
        self._bullish_count = 0
        self._bearish_count = 0
        self._prev_ha_open = None
        self._prev_ha_close = Decimal(0)

    def CreateClone(self):
        return heikin_ashi_consecutive_strategy()
