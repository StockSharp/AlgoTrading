import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import OnBalanceVolume, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class obv_breakout_strategy(Strategy):
    """
    Trades strict breaks of prior rolling OBV extrema with matching price direction.
    Fully exits on any actual OBV/OBV-SMA crossing or actual-fill percent protection.
    """

    def __init__(self):
        super(obv_breakout_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetGreaterThanZero().SetDisplay("Lookback Period", "Number of PRIOR finished OBV values in the breakout range", "Entry")
        self._obv_ma_period = self.Param("OBVMAPeriod", 20).SetGreaterThanZero().SetDisplay("OBV MA Period", "Current-inclusive SMA length for OBV, not price", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._prior_obvs = []
        self._obv_average = None
        self._previous_price = None
        self._previous_ready_obv = None
        self._previous_mean = Decimal.Zero
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
        self._prior_obvs = []
        self._previous_price = None
        self._previous_ready_obv = None
        self._previous_mean = Decimal.Zero
        self._pending_order = None

    def OnReseted(self):
        super(obv_breakout_strategy, self).OnReseted()
        self._clear_signal_state()
        self._obv_average = None

    def OnStarted2(self, time):
        super(obv_breakout_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        obv = OnBalanceVolume()
        self._obv_average = SimpleMovingAverage()
        self._obv_average.Length = self._obv_ma_period.Value
        self.Indicators.Add(self._obv_average)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(obv, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, obv_value):
        if candle.State != CandleStates.Finished:
            return

        # Average OBV itself, including warmup and pending-order bars.
        mean_value = self._obv_average.Process(obv_value)
        current_obv = obv_value.GetValue[Decimal](None)
        prior_price = self._previous_price
        self._previous_price = candle.ClosePrice

        # Capture the PRIOR range before inserting the breakout candle.
        range_ready = len(self._prior_obvs) == self._lookback_period.Value
        upper = max(self._prior_obvs) if range_ready else Decimal.Zero
        lower = min(self._prior_obvs) if range_ready else Decimal.Zero
        self._prior_obvs.append(current_obv)
        while len(self._prior_obvs) > self._lookback_period.Value:
            del self._prior_obvs[0]

        if not obv_value.Indicator.IsFormed or not self._obv_average.IsFormed:
            return
        mean = mean_value.GetValue[Decimal](None)
        upward_cross = self._previous_ready_obv is not None and self._previous_ready_obv <= self._previous_mean and current_obv > mean
        downward_cross = self._previous_ready_obv is not None and self._previous_ready_obv >= self._previous_mean and current_obv < mean
        # Zero OBV/mean are valid values, not an uninitialized sentinel.
        self._previous_ready_obv = current_obv
        self._previous_mean = mean

        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if self.Position != 0 and (upward_cross or downward_cross):
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and range_ready and prior_price is not None:
            if current_obv > upper and candle.ClosePrice > prior_price:
                self.BuyMarket(self.Volume)
            elif current_obv < lower and candle.ClosePrice < prior_price:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return obv_breakout_strategy()
