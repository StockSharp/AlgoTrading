import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class pinbar_reversal_strategy(Strategy):
    """
    Enters in the SMA-aligned direction of a pinbar and exits on the opposite pinbar or stop.
    """

    def __init__(self):
        super(pinbar_reversal_strategy, self).__init__()
        self._tail_to_body_ratio = self.Param("TailToBodyRatio", 2.0).SetRange(1.0, 10.0).SetDisplay("Tail/Body Ratio", "Minimum dominant shadow relative to the body", "Pattern")
        self._opposite_tail_ratio = self.Param("OppositeTailRatio", 0.5).SetRange(0.0, 2.0).SetDisplay("Opposite Tail Ratio", "Maximum opposite shadow relative to the body", "Pattern")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Close SMA trend filter", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Pinbar and MA timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 1.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def OnReseted(self):
        super(pinbar_reversal_strategy, self).OnReseted()
        self._pending_order = None

    def OnStarted2(self, time):
        super(pinbar_reversal_strategy, self).OnStarted2(time)
        self._pending_order = None
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        candles = self.SubscribeCandles(self.candle_type)
        candles.Bind(sma, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native actual-fill protection evaluates executable quotes between signal candles.
        pass

    def _process_candle(self, candle, sma):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        body = Math.Abs(candle.ClosePrice - candle.OpenPrice)
        if body <= Decimal(0):
            return
        lower = Math.Min(candle.OpenPrice, candle.ClosePrice) - candle.LowPrice
        upper = candle.HighPrice - Math.Max(candle.OpenPrice, candle.ClosePrice)
        tail_ratio = Decimal(self._tail_to_body_ratio.Value)
        opposite_ratio = Decimal(self._opposite_tail_ratio.Value)
        bullish = lower >= body * tail_ratio and upper <= body * opposite_ratio
        bearish = upper >= body * tail_ratio and lower <= body * opposite_ratio
        if self.Position > 0 and bearish:
            self.SellMarket(self.Position)
        elif self.Position < 0 and bullish:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and bullish and candle.ClosePrice > sma:
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and bearish and candle.ClosePrice < sma:
            self.SellMarket(self.Volume)

    def CreateClone(self):
        return pinbar_reversal_strategy()
