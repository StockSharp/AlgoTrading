import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class atr_reversion_strategy(Strategy):
    """
    ATR Reversion strategy. Trades when price moves N*ATR in one direction.
    """

    def __init__(self):
        super(atr_reversion_strategy, self).__init__()
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "Period for ATR calculation", "Indicators")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier for entry signal", "Entry")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for MA calculation for exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_close = Decimal.Zero
        self._prev_mean = Decimal.Zero
        self._has_previous = False
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(atr_reversion_strategy, self).OnReseted()
        self._prev_close = Decimal.Zero
        self._prev_mean = Decimal.Zero
        self._has_previous = False
        self._pending_order = None

    def OnStarted2(self, time):
        super(atr_reversion_strategy, self).OnStarted2(time)

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, sma, self._process_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawIndicator(area, atr)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, atr_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if not atr_val.IsFormed or not sma_val.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        av = atr_val.GetValue[Decimal](None)
        mean = sma_val.GetValue[Decimal](None)
        if not self._has_previous:
            self._has_previous = True
            self._prev_close = close
            self._prev_mean = mean
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_close = close
            self._prev_mean = mean
            return

        mult = Decimal(self._atr_multiplier.Value)
        # A current level is not a crossing: compare each close against its own SMA.
        if self.Position > 0 and self._prev_close <= self._prev_mean and close > mean:
            self.SellMarket(self.Position)
        elif self.Position < 0 and self._prev_close >= self._prev_mean and close < mean:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and av > 0 and close - self._prev_close < -mult * av:
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and av > 0 and close - self._prev_close > mult * av:
            self.SellMarket(self.Volume)
        self._prev_close = close
        self._prev_mean = mean

    def CreateClone(self):
        return atr_reversion_strategy()
