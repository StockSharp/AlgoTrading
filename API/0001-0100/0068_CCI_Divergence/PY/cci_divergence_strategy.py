import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields, Sides
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy


class cci_divergence_strategy(Strategy):
    """Trade confirmed price/CCI pivot divergence in the CCI extreme zone.

    A detected divergence stays valid for DivergencePeriod bars.
    """

    def __init__(self):
        super(cci_divergence_strategy, self).__init__()
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "Typical-price CCI length", "Indicators")
        self._divergence_period = self.Param("DivergencePeriod", 5).SetRange(3, 31).SetDisplay("Divergence Period", "Odd-width confirmed price pivot window and bars a detected divergence stays valid", "Pattern")
        self._overbought_level = self.Param("OverboughtLevel", 100.0).SetDisplay("Overbought Level", "CCI at the later high must exceed this level", "Pattern")
        self._oversold_level = self.Param("OversoldLevel", -100.0).SetDisplay("Oversold Level", "CCI at the later low must be below this level", "Pattern")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "CCI and price pivot timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it", "Protection")
        self._clear_state()
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _clear_state(self):
        self._window = []
        self._last_low = None
        self._last_high = None
        self._previous_cci = None
        self._signal = None
        self._signal_age = 0
        self._pending_order = None
        self._cci = None

    def OnReseted(self):
        super(cci_divergence_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(cci_divergence_strategy, self).OnStarted2(time)
        if self._divergence_period.Value % 2 != 1 or self._overbought_level.Value <= 0 or self._oversold_level.Value >= 0:
            raise ValueError("DivergencePeriod must be odd, OverboughtLevel positive, and OversoldLevel negative.")
        self._clear_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        self._cci = CommodityChannelIndex()
        self._cci.Length = self._cci_period.Value
        candles = self.SubscribeCandles(self.candle_type)
        candles.Bind(self._cci, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, self._cci)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native actual-fill protection evaluates executable quotes between signal candles.
        pass

    def _process_candle(self, candle, cci_value):
        if candle.State != CandleStates.Finished or not self._cci.IsFormed:
            return

        up_cross = self._previous_cci is not None and self._previous_cci <= 0 and cci_value > 0
        down_cross = self._previous_cci is not None and self._previous_cci >= 0 and cci_value < 0
        self._previous_cci = cci_value

        width = self._divergence_period.Value
        if self._signal is not None:
            self._signal_age += 1
            if self._signal_age > width:
                self._signal = None

        self._window.append((candle.HighPrice, candle.LowPrice, cci_value))
        if len(self._window) == width:
            middle = width // 2
            pivot = self._window[middle]
            if all(pivot[1] < bar[1] for i, bar in enumerate(self._window) if i != middle):
                if (self._last_low is not None and pivot[1] < self._last_low[0] and
                        pivot[2] > self._last_low[1] and pivot[2] < self._oversold_level.Value):
                    self._signal = Sides.Buy
                    self._signal_age = 0
                self._last_low = (pivot[1], pivot[2])
            if all(pivot[0] > bar[0] for i, bar in enumerate(self._window) if i != middle):
                if (self._last_high is not None and pivot[0] > self._last_high[0] and
                        pivot[2] < self._last_high[1] and pivot[2] > self._overbought_level.Value):
                    self._signal = Sides.Sell
                    self._signal_age = 0
                self._last_high = (pivot[0], pivot[2])
            self._window.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position > 0 and up_cross:
            self.SellMarket(self.Position)
        elif self.Position < 0 and down_cross:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and self._signal is not None:
            if self._signal == Sides.Buy:
                self.BuyMarket(self.Volume)
            else:
                self.SellMarket(self.Volume)
            self._signal = None

    def CreateClone(self):
        return cci_divergence_strategy()
