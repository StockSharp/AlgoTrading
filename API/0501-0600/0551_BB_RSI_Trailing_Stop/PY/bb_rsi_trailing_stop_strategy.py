import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import Decimal, Math, TimeSpan, Convert, InvalidOperationException
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, AverageTrueRange, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Subscription
from StockSharp.Messages import DataType, CandleStates, Level1Fields, OrderStates, OrderTypes, Sides, Unit, UnitTypes, ITickTradeMessage

class bb_rsi_trailing_stop_strategy(Strategy):
    """Bollinger excursions, RSI recovery, and executable-quote conditional trailing."""

    def __init__(self):
        super(bb_rsi_trailing_stop_strategy, self).__init__()
        self._bollingerPeriod = self.Param("BollingerPeriod", 25).SetGreaterThanZero() \
            .SetDisplay("BollingerPeriod", "Bollinger close window", "Parameters")
        self._bollingerDeviation = self.Param("BollingerDeviation", Decimal(2)).SetGreaterThanZero() \
            .SetDisplay("BollingerDeviation", "Bollinger standard deviation multiplier", "Parameters")
        self._rsiPeriod = self.Param("RsiPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("RsiPeriod", "Wilder RSI window", "Parameters")
        self._rsiOverbought = self.Param("RsiOverbought", Decimal(60)).SetGreaterThanZero() \
            .SetDisplay("RsiOverbought", "RSI upper recovery threshold", "Parameters")
        self._rsiOversold = self.Param("RsiOversold", Decimal(33)).SetGreaterThanZero() \
            .SetDisplay("RsiOversold", "RSI lower recovery threshold", "Parameters")
        self._stopLossPoints = self.Param("StopLossPoints", Decimal(50)).SetGreaterThanZero() \
            .SetDisplay("StopLossPoints", "Initial stop distance in price units", "Parameters")
        self._trailOffsetPoints = self.Param("TrailOffsetPoints", Decimal(99)).SetGreaterThanZero() \
            .SetDisplay("TrailOffsetPoints", "Favorable price movement required to activate trailing", "Parameters")
        self._trailStopPoints = self.Param("TrailStopPoints", Decimal(40)).SetGreaterThanZero() \
            .SetDisplay("TrailStopPoints", "Trailing distance in price units", "Parameters")
        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("CandleType", "Finished signal candle interval", "Parameters")
        self._clear_state()

    @property
    def BollingerPeriod(self):
        return self._bollingerPeriod.Value

    @BollingerPeriod.setter
    def BollingerPeriod(self, value):
        self._bollingerPeriod.Value = value

    @property
    def BollingerDeviation(self):
        return self._bollingerDeviation.Value

    @BollingerDeviation.setter
    def BollingerDeviation(self, value):
        self._bollingerDeviation.Value = value

    @property
    def RsiPeriod(self):
        return self._rsiPeriod.Value

    @RsiPeriod.setter
    def RsiPeriod(self, value):
        self._rsiPeriod.Value = value

    @property
    def RsiOverbought(self):
        return self._rsiOverbought.Value

    @RsiOverbought.setter
    def RsiOverbought(self, value):
        self._rsiOverbought.Value = value

    @property
    def RsiOversold(self):
        return self._rsiOversold.Value

    @RsiOversold.setter
    def RsiOversold(self, value):
        self._rsiOversold.Value = value

    @property
    def StopLossPoints(self):
        return self._stopLossPoints.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stopLossPoints.Value = value

    @property
    def TrailOffsetPoints(self):
        return self._trailOffsetPoints.Value

    @TrailOffsetPoints.setter
    def TrailOffsetPoints(self, value):
        self._trailOffsetPoints.Value = value

    @property
    def TrailStopPoints(self):
        return self._trailStopPoints.Value

    @TrailStopPoints.setter
    def TrailStopPoints(self, value):
        self._trailStopPoints.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    def _clear_state(self):
        self._pending = None
        self._previous_rsi = None
        self._entry_cost = Decimal(0)
        self._entry_quantity = Decimal(0)
        self._extreme = Decimal(0)
        self._stop = Decimal(0)
        self._trailing = False

    def OnReseted(self):
        super(bb_rsi_trailing_stop_strategy, self).OnReseted()
        self._clear_state()

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def OnStarted2(self, time):
        if self.RsiOversold >= self.RsiOverbought or self.RsiOverbought >= 100:
            raise InvalidOperationException("RSI thresholds must satisfy 0 < oversold < overbought < 100.")
        super(bb_rsi_trailing_stop_strategy, self).OnStarted2(time)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quote = Subscription(DataType.Level1, self.Security)
            quote.MarketData.BuildField = field
            self.SubscribeLevel1(quote).Bind(self._process_quote).Start()
        self._bb = BollingerBands()
        self._bb.Length = self.BollingerPeriod
        self._bb.Width = self.BollingerDeviation
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.RsiPeriod
        candles = self.SubscribeCandles(self.CandleType)
        candles.BindEx(self._bb, self._rsi, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, self._bb)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def _is_pending(self):
        return self._pending is not None and self._pending.State not in (OrderStates.Done, OrderStates.Failed)

    def _submit(self, side, quantity, reason):
        self._pending = self.CreateOrder(side, Decimal(0), quantity)
        self._pending.Type = OrderTypes.Market
        self._pending.Comment = "0551" + reason
        self.RegisterOrder(self._pending)

    def _process_candle(self, candle, bb, rsi_value):
        if candle.State != CandleStates.Finished or not self._bb.IsFormed or not self._rsi.IsFormed or bb.LowBand is None or bb.UpBand is None:
            return
        rsi = rsi_value.GetValue[Decimal](None)
        previous = self._previous_rsi
        self._previous_rsi = rsi
        if previous is None or self.Position != 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        side = Sides.Buy if candle.LowPrice < bb.LowBand and previous <= self.RsiOversold and rsi > self.RsiOversold else Sides.Sell if candle.HighPrice > bb.UpBand and previous >= self.RsiOverbought and rsi < self.RsiOverbought else None
        if side is None:
            return
        self._entry_cost = Decimal(0)
        self._entry_quantity = Decimal(0)
        self._trailing = False
        self._initial_distance = self.StopLossPoints
        self._activation_distance = self.TrailOffsetPoints
        self._trailing_distance = self.TrailStopPoints
        self._submit(side, self.Volume, "Entry")

    def OnOwnTradeReceived(self, trade):
        super(bb_rsi_trailing_stop_strategy, self).OnOwnTradeReceived(trade)
        if trade.Order.Comment == "0551Entry":
            price = ITickTradeMessage.Price.__get__(trade.Trade)
            quantity = ITickTradeMessage.Volume.__get__(trade.Trade)
            self._entry_cost += price * quantity
            self._entry_quantity += quantity
            entry = self._entry_cost / self._entry_quantity
            self._extreme = entry
            self._stop = entry - self._initial_distance if trade.Order.Side == Sides.Buy else entry + self._initial_distance

    def _process_quote(self, quote):
        if self.Position == 0 or self._entry_quantity == 0 or self._is_pending() or not self.IsFormedAndOnlineAndAllowTrading():
            return
        field = Level1Fields.BestBidPrice if self.Position > 0 else Level1Fields.BestAskPrice
        if not quote.Changes.ContainsKey(field):
            return
        price = Convert.ToDecimal(quote.Changes[field])
        if price <= 0:
            return
        entry = self._entry_cost / self._entry_quantity
        self._extreme = max(self._extreme, price) if self.Position > 0 else min(self._extreme, price)
        if (self._extreme - entry >= self._activation_distance if self.Position > 0 else entry - self._extreme >= self._activation_distance):
            self._trailing = True
        if self._trailing:
            self._stop = max(self._stop, self._extreme - self._trailing_distance) if self.Position > 0 else min(self._stop, self._extreme + self._trailing_distance)
        if (price <= self._stop if self.Position > 0 else price >= self._stop):
            self._submit(Sides.Sell if self.Position > 0 else Sides.Buy, abs(self.Position), "Stop")

    def CreateClone(self):
        return bb_rsi_trailing_stop_strategy()
