import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy

class volume_ma_cross_strategy(Strategy):
    """
    Trades actual fast/slow native TotalVolume SMA crosses with Close/SMA entry confirmation.
    Fully exits on the opposite volume cross or actual-fill percent protection.
    """

    def __init__(self):
        super(volume_ma_cross_strategy, self).__init__()
        self._price_ma_period = self.Param("PriceMaPeriod", 20).SetGreaterThanZero().SetDisplay("Price MA Period", "Current-inclusive Close SMA entry filter", "Indicators")
        self._fast_volume_ma_length = self.Param("FastVolumeMALength", 10).SetGreaterThanZero().SetDisplay("Fast Volume MA Length", "Current-inclusive fast TotalVolume SMA length", "Indicators")
        self._slow_volume_ma_length = self.Param("SlowVolumeMALength", 50).SetGreaterThanZero().SetDisplay("Slow Volume MA Length", "Current-inclusive slow TotalVolume SMA length", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._price_ma = None
        self._fast_volume_ma = None
        self._slow_volume_ma = None
        self._previous_fast = None
        self._previous_slow = None
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _clear_signal_state(self):
        self._price_ma = None
        self._fast_volume_ma = None
        self._slow_volume_ma = None
        self._previous_fast = None
        self._previous_slow = None
        self._pending_order = None

    def OnReseted(self):
        super(volume_ma_cross_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        super(volume_ma_cross_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        self._price_ma = SimpleMovingAverage()
        self._price_ma.Length = self._price_ma_period.Value
        self._price_ma.Name = "Price SMA"
        self._fast_volume_ma = SimpleMovingAverage()
        self._fast_volume_ma.Length = self._fast_volume_ma_length.Value
        self._fast_volume_ma.Name = "Fast volume SMA"
        self._slow_volume_ma = SimpleMovingAverage()
        self._slow_volume_ma.Length = self._slow_volume_ma_length.Value
        self._slow_volume_ma.Name = "Slow volume SMA"
        self.Indicators.Add(self._price_ma)
        self.Indicators.Add(self._fast_volume_ma)
        self.Indicators.Add(self._slow_volume_ma)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._price_ma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        price_value = self._price_ma.Process(CandleIndicatorValue(self._price_ma, candle))
        # Use actual current TotalVolume for both native SMAs, including while unavailable.
        fast_input = DecimalIndicatorValue(self._fast_volume_ma, candle.TotalVolume, candle.OpenTime)
        fast_input.IsFinal = True
        fast_value = self._fast_volume_ma.Process(fast_input)
        slow_input = DecimalIndicatorValue(self._slow_volume_ma, candle.TotalVolume, candle.OpenTime)
        slow_input.IsFinal = True
        slow_value = self._slow_volume_ma.Process(slow_input)
        price_ma = price_value.GetValue[Decimal](None) if not price_value.IsEmpty and self._price_ma.IsFormed else None
        fast = fast_value.GetValue[Decimal](None) if not fast_value.IsEmpty and self._fast_volume_ma.IsFormed else None
        slow = slow_value.GetValue[Decimal](None) if not slow_value.IsEmpty and self._slow_volume_ma.IsFormed else None
        up = self._previous_fast is not None and self._previous_slow is not None and fast is not None and slow is not None and self._previous_fast <= self._previous_slow and fast > slow
        down = self._previous_fast is not None and self._previous_slow is not None and fast is not None and slow is not None and self._previous_fast >= self._previous_slow and fast < slow
        # Formed values seed the next cross even while trading is disabled or pending.
        self._previous_fast = fast
        self._previous_slow = slow
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position > 0 and down:
            self.SellMarket(self.Position)
        elif self.Position < 0 and up:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and price_ma is not None:
            if up and candle.ClosePrice > price_ma:
                self.BuyMarket(self.Volume)
            elif down and candle.ClosePrice < price_ma:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return volume_ma_cross_strategy()
