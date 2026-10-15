import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class smc_order_block_zones_strategy(Strategy):
    """
    SMC Order Block Zones Strategy.
    The swing high of SwingHighLength bars is the premium zone, the swing low of SwingLowLength bars the discount zone and
    their midpoint the equilibrium. The bullish order block is the lowest low and the bearish order block the highest high of
    the previous OrderBlockLength bars. A long opens when the close is between the discount zone and the equilibrium, above
    the SMA, and the candle touched the bullish order block; a short mirrors this between the equilibrium and the premium zone
    below the SMA after touching the bearish order block. An opposite signal closes or reverses the position and a percent stop
    limits the loss.
    """

    def __init__(self):
        super(smc_order_block_zones_strategy, self).__init__()
        self._swing_high_length = self.Param("SwingHighLength", 8).SetGreaterThanZero().SetDisplay("Swing High Length", "Bars of the swing high", "Zones")
        self._swing_low_length = self.Param("SwingLowLength", 8).SetGreaterThanZero().SetDisplay("Swing Low Length", "Bars of the swing low", "Zones")
        self._sma_length = self.Param("SmaLength", 50).SetGreaterThanZero().SetDisplay("SMA Length", "SMA period of the trend filter", "Indicators")
        self._order_block_length = self.Param("OrderBlockLength", 20).SetGreaterThanZero().SetDisplay("Order Block Length", "Bars searched for order blocks", "Zones")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._allow_long = self.Param("AllowLong", True).SetDisplay("Allow Long", "Allow long trades", "Trading")
        self._allow_short = self.Param("AllowShort", True).SetDisplay("Allow Short", "Allow short trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._swing_high = None
        self._swing_low = None
        self._block_high = None
        self._block_low = None
        self._prev_block_high = None
        self._prev_block_low = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(smc_order_block_zones_strategy, self).OnReseted()
        self._prev_block_high = None
        self._prev_block_low = None

    def OnStarted2(self, time):
        super(smc_order_block_zones_strategy, self).OnStarted2(time)

        self._prev_block_high = None
        self._prev_block_low = None

        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value
        self._swing_high = Highest()
        self._swing_high.Length = self._swing_high_length.Value
        self._swing_low = Lowest()
        self._swing_low.Length = self._swing_low_length.Value
        self._block_high = Highest()
        self._block_high.Length = self._order_block_length.Value
        self._block_low = Lowest()
        self._block_low.Length = self._order_block_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, self._process_candle).Start()

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
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value):
        if candle.State != CandleStates.Finished:
            return

        time = candle.OpenTime
        premium = float(process_float(self._swing_high, candle.HighPrice, time, True).GetValue[Decimal](None))
        discount = float(process_float(self._swing_low, candle.LowPrice, time, True).GetValue[Decimal](None))

        # Order blocks are taken from the candles before this one so the current candle can touch them.
        bull_block = self._prev_block_low
        bear_block = self._prev_block_high
        block_high = float(process_float(self._block_high, candle.HighPrice, time, True).GetValue[Decimal](None))
        block_low = float(process_float(self._block_low, candle.LowPrice, time, True).GetValue[Decimal](None))

        if self._block_high.IsFormed and self._block_low.IsFormed:
            self._prev_block_high = block_high
            self._prev_block_low = block_low

        if not sma_value.IsFormed or not self._swing_high.IsFormed or not self._swing_low.IsFormed or bull_block is None or bear_block is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sma = float(sma_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)
        equilibrium = (premium + discount) / 2.0

        long_signal = close < equilibrium and close > discount and close > sma and float(candle.LowPrice) <= bull_block
        short_signal = close > equilibrium and close < premium and close < sma and float(candle.HighPrice) >= bear_block

        if long_signal:
            if self._allow_long.Value and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
            elif self.Position < 0:
                self.BuyMarket(-self.Position)
        elif short_signal:
            if self._allow_short.Value and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
            elif self.Position > 0:
                self.SellMarket(self.Position)

    def CreateClone(self):
        return smc_order_block_zones_strategy()
