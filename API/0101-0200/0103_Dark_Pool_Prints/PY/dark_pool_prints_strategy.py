import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, InvalidOperationException
from StockSharp.Messages import DataType, CandleStates, OrderStates, Sides, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Security, Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class dark_pool_prints_strategy(Strategy):
    """Trades classified off-exchange prints from an explicitly selected source."""

    def __init__(self):
        super(dark_pool_prints_strategy, self).__init__()
        self._dark_pool_security = self.Param[Security]("DarkPoolSecurity", None).SetRequired().SetDisplay(
            "Dark Pool Security", "Dedicated source of off-exchange trade reports", "Data")
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero()
        self._volume_lookback = self.Param("VolumeLookback", 20).SetGreaterThanZero()
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5).SetGreaterThanZero()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        self._cooldown_bars = self.Param("CooldownBars", 0).SetNotNegative()
        self._stop_loss = self.Param("StopLoss", 2.0).SetGreaterThanZero()
        self._clear_runtime()
        self.OrderRegistering += self._on_order

    def _clear_runtime(self):
        self._report_volumes = []
        self._report_ids = set()
        self._report_time = None
        self._average = None
        self._cooldown = 0
        self._pending_order = None

    def _on_order(self, order):
        self._pending_order = order

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value), (self.Security, DataType.Level1),
                (self._dark_pool_security.Value, DataType.Ticks)]

    def OnReseted(self):
        super(dark_pool_prints_strategy, self).OnReseted()
        self._clear_runtime()

    def OnStarted2(self, time):
        source = self._dark_pool_security.Value
        if source is None or self.Security is None or source.Id.lower() == self.Security.Id.lower():
            raise InvalidOperationException("DarkPoolSecurity must explicitly identify a separate off-exchange report source")
        super(dark_pool_prints_strategy, self).OnStarted2(time)
        self._clear_runtime()
        self.StartProtection(Unit(), Unit(float(self._stop_loss.Value), UnitTypes.Percent),
                             useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Start()
        sma = SimpleMovingAverage()
        sma.Length = int(self._ma_period.Value)
        candles = self.SubscribeCandles(self._candle_type.Value)
        candles.Bind(sma, self._process_candle).Start()
        self.SubscribeTicks(source).Bind(self._process_report).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _has_pending_order(self):
        return self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)

    def _process_candle(self, candle, average):
        if candle.State != CandleStates.Finished:
            return
        self._average = float(average)
        if self._cooldown > 0:
            self._cooldown -= 1
        if not self.IsFormedAndOnlineAndAllowTrading() or self._has_pending_order():
            return
        if self.Position > 0 and float(candle.ClosePrice) < self._average:
            self.SellMarket(self.Position)
        elif self.Position < 0 and float(candle.ClosePrice) > self._average:
            self.BuyMarket(-self.Position)

    def _process_report(self, report):
        if report.IsSystem != False:
            return
        price, volume = float(report.TradePrice), float(report.TradeVolume)
        identity = (report.TradeId, report.TradeStringId)
        if price <= 0 or volume <= 0 or (identity[0] is None and not identity[1]):
            return
        if self._report_time is not None and report.ServerTime < self._report_time:
            return
        if self._report_time is None or report.ServerTime > self._report_time:
            self._report_time = report.ServerTime
            self._report_ids.clear()
        if identity in self._report_ids:
            return
        self._report_ids.add(identity)
        lookback = int(self._volume_lookback.Value)
        baseline = sum(self._report_volumes) / lookback if len(self._report_volumes) == lookback else None
        self._report_volumes.append(volume)
        if len(self._report_volumes) > lookback:
            self._report_volumes.pop(0)
        if (baseline is None or volume <= baseline * float(self._volume_multiplier.Value)
                or self._average is None or self._cooldown > 0 or self._has_pending_order()
                or not self.IsFormedAndOnlineAndAllowTrading()):
            return
        if report.OriginSide == Sides.Buy and price > self._average and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)
        elif report.OriginSide == Sides.Sell and price < self._average and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._cooldown = int(self._cooldown_bars.Value)

    def CreateClone(self):
        return dark_pool_prints_strategy()