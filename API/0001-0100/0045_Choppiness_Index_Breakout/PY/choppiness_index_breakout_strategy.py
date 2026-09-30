import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, Highest, Lowest, AverageTrueRange, Sum
from StockSharp.Algo.Strategies import Strategy

class choppiness_index_breakout_strategy(Strategy):
    """
    CHOP = 100 log10(sum(TR, N) / (highest High - lowest Low)) / log10(N).
    Trades low-index price/SMA levels and fully exits at a high index or percent stop.
    """

    def __init__(self):
        super(choppiness_index_breakout_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._choppiness_period = self.Param("ChoppinessPeriod", 14).SetRange(2, 2147483647).SetDisplay("Choppiness Period", "Period for Choppiness Index calculation", "Indicators")
        self._choppiness_threshold = self.Param("ChoppinessThreshold", 38.2).SetRange(0.0, 100.0).SetDisplay("Choppiness Threshold", "Threshold below which market is trending", "Entry")
        self._high_choppiness_threshold = self.Param("HighChoppinessThreshold", 61.8).SetRange(0.0, 100.0).SetDisplay("High Choppiness", "Threshold above which to exit positions", "Exit")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._range_sum = None
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    @property
    def MAPeriod(self):
        return self._ma_period.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._ma_period.Value = value

    @property
    def ChoppinessPeriod(self):
        return self._choppiness_period.Value

    @ChoppinessPeriod.setter
    def ChoppinessPeriod(self, value):
        self._choppiness_period.Value = value

    @property
    def ChoppinessThreshold(self):
        return self._choppiness_threshold.Value

    @ChoppinessThreshold.setter
    def ChoppinessThreshold(self, value):
        self._choppiness_threshold.Value = value

    @property
    def HighChoppinessThreshold(self):
        return self._high_choppiness_threshold.Value

    @HighChoppinessThreshold.setter
    def HighChoppinessThreshold(self, value):
        self._high_choppiness_threshold.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def OnReseted(self):
        super(choppiness_index_breakout_strategy, self).OnReseted()
        self._range_sum = None
        self._pending_order = None

    def OnStarted2(self, time):
        if Decimal(self.ChoppinessThreshold) >= Decimal(self.HighChoppinessThreshold):
            raise ValueError("ChoppinessThreshold must be less than HighChoppinessThreshold.")
        super(choppiness_index_breakout_strategy, self).OnStarted2(time)
        self._pending_order = None
        self.StartProtection(Unit(), Unit(Decimal(self.StopLossPercent), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        ma = SimpleMovingAverage()
        ma.Length = self.MAPeriod
        highest = Highest()
        highest.Length = self.ChoppinessPeriod
        lowest = Lowest()
        lowest.Length = self.ChoppinessPeriod
        tr = AverageTrueRange()
        tr.Length = 1
        self._range_sum = Sum()
        self._range_sum.Length = self.ChoppinessPeriod
        self.Indicators.Add(self._range_sum)
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ma, highest, lowest, tr, self.ProcessCandle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def ProcessCandle(self, candle, ma_value, high_value, low_value, tr_value):
        if candle.State != CandleStates.Finished:
            return
        # Compose the documented formula locally; platform code is unchanged.
        sum_value = self._range_sum.Process(tr_value)
        if not ma_value.Indicator.IsFormed or not high_value.Indicator.IsFormed or not low_value.Indicator.IsFormed or not self._range_sum.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        width = high_value.GetValue[Decimal](None) - low_value.GetValue[Decimal](None)
        sum_tr = sum_value.GetValue[Decimal](None)
        if width <= 0 or sum_tr <= 0:
            return
        choppiness = Decimal(100) * Decimal(Math.Log10(float(sum_tr / width))) / Decimal(Math.Log10(float(self.ChoppinessPeriod)))
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position != 0 and choppiness > Decimal(self.HighChoppinessThreshold):
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and choppiness < Decimal(self.ChoppinessThreshold):
            # Formal README specifies a below-threshold LEVEL, not an extra crossing.
            mean = ma_value.GetValue[Decimal](None)
            if candle.ClosePrice > mean:
                self.BuyMarket(self.Volume)
            elif candle.ClosePrice < mean:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return choppiness_index_breakout_strategy()
