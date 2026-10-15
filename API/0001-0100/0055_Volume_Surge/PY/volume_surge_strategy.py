import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class volume_surge_strategy(Strategy):
    """
    Trades strict volume surges above a rolling volume SMA in price/SMA direction.
    Fully exits below the volume mean or through actual-fill percent protection.
    """

    def __init__(self):
        super(volume_surge_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20).SetGreaterThanZero().SetDisplay("Volume Average Period", "Current-inclusive rolling TotalVolume SMA length", "Indicators")
        self._volume_surge_multiplier = self.Param("VolumeSurgeMultiplier", 2.0).SetGreaterThanZero().SetDisplay("Volume Surge Multiplier", "Current volume must strictly exceed volume SMA times multiplier", "Entry")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_average = None
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
        super(volume_surge_strategy, self).OnReseted()
        self._volume_average = None
        self._pending_order = None

    def OnStarted2(self, time):
        super(volume_surge_strategy, self).OnStarted2(time)
        self._pending_order = None
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        self._volume_average = SimpleMovingAverage()
        self._volume_average.Length = self._volume_avg_period.Value
        self._volume_average.Name = "Volume average"
        self.Indicators.Add(self._volume_average)
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, ma_value):
        if candle.State != CandleStates.Finished:
            return
        # Feed current actual volume, including during price-SMA warmup.
        volume_input = DecimalIndicatorValue(self._volume_average, candle.TotalVolume, candle.OpenTime)
        volume_input.IsFinal = True
        volume_value = self._volume_average.Process(volume_input)
        if not ma_value.Indicator.IsFormed or not self._volume_average.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return
        average = volume_value.GetValue[Decimal](None)
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position != 0 and candle.TotalVolume < average:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and average > 0 and candle.TotalVolume > average * Decimal(self._volume_surge_multiplier.Value):
            mean = ma_value.GetValue[Decimal](None)
            if candle.ClosePrice > mean:
                self.BuyMarket(self.Volume)
            elif candle.ClosePrice < mean:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return volume_surge_strategy()
