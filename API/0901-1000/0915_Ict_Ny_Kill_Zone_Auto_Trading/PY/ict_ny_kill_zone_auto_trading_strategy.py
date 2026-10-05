import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

KILL_ZONE_START_HOUR = 11
KILL_ZONE_END_HOUR = 14


class ict_ny_kill_zone_auto_trading_strategy(Strategy):
    """
    ICT NY Kill Zone Auto Trading strategy.
    Inside the New York kill zone (07:00-10:00 New York time, taken as 11:00-14:00 UTC) a bullish fair value gap, where the low of the
    current candle stays above the high of the candle two bars back, whose first candle is a bearish order block goes long. A bearish
    fair value gap whose first candle is a bullish order block goes short. An opposite signal reverses the position and every position
    is protected by a stop loss and take profit in price steps.
    """

    def __init__(self):
        super(ict_ny_kill_zone_auto_trading_strategy, self).__init__()
        self._stop_loss = self.Param("StopLoss", 30.0).SetNotNegative().SetDisplay("Stop Loss", "Stop loss in price steps", "Risk Management")
        self._take_profit = self.Param("TakeProfit", 60.0).SetNotNegative().SetDisplay("Take Profit", "Take profit in price steps", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles", "General")
        self._prev1 = None
        self._prev2 = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ict_ny_kill_zone_auto_trading_strategy, self).OnReseted()
        self._prev1 = None
        self._prev2 = None

    def OnStarted2(self, time):
        super(ict_ny_kill_zone_auto_trading_strategy, self).OnStarted2(time)

        self._prev1 = None
        self._prev2 = None

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()

        step = Decimal(1)
        if self.Security is not None and self.Security.PriceStep is not None:
            step = self.Security.PriceStep

        tp = Decimal(self._take_profit.Value)
        sl = Decimal(self._stop_loss.Value)
        take = Unit(tp * step, UnitTypes.Absolute) if tp > 0 else Unit()
        stop = Unit(sl * step, UnitTypes.Absolute) if sl > 0 else Unit()
        self.StartProtection(take, stop, useMarketOrders=True, isLocalStop=True)

        # The stop and take have to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        first = self._prev2

        self._prev2 = self._prev1
        self._prev1 = candle

        if first is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        hour = candle.OpenTime.Hour
        if hour < KILL_ZONE_START_HOUR or hour >= KILL_ZONE_END_HOUR:
            return

        bullish_fvg = first.HighPrice < candle.LowPrice
        bearish_fvg = first.LowPrice > candle.HighPrice
        bearish_order_block = first.ClosePrice < first.OpenPrice
        bullish_order_block = first.ClosePrice > first.OpenPrice

        if bullish_fvg and bearish_order_block and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish_fvg and bullish_order_block and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ict_ny_kill_zone_auto_trading_strategy()
