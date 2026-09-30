import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math, Decimal, InvalidOperationException
from StockSharp.Messages import UnitTypes, Unit, DataType, CandleStates, OrderStates, Level1Fields
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from StockSharp.BusinessEntities import Security, Subscription

class iv_spike_strategy(Strategy):
    """
    Implied volatility spike strategy.
    The implied volatility readings are the candle closes of a separate instrument on the same timeframe.
    A spike is a rise to at least IVSpikeThreshold times the previous reading that also leaves the reading
    above the average of the last IVPeriod readings. On a spike the strategy enters against the price move:
    long when the close is below the moving average, short when it is above.
    The position is closed when a reading falls below the previous one or the stop-loss is hit.
    """

    def __init__(self):
        super(iv_spike_strategy, self).__init__()

        self._clear_signal_state()
        self.OrderRegistering += self._track_pending

        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period of the moving average the close is compared with", "Indicators")

        self._iv_period = self.Param("IVPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("IV Period", "Implied volatility readings in the average a spike reading must exceed", "Indicators")

        self._iv_spike_threshold = self.Param("IVSpikeThreshold", 1.5).SetGreaterThanZero() \
            .SetDisplay("IV Spike Threshold", "Multiple of the previous implied volatility reading a spike must reach", "Entry")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles for the traded and the implied volatility instruments", "General")

        self._iv_security = self.Param[Security]("IVSecurity", None) \
            .SetDisplay("IV Security", "Instrument whose candle closes are the implied volatility readings", "Data").SetRequired()

    @property
    def MAPeriod(self):
        return self._ma_period.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._ma_period.Value = value

    @property
    def IVPeriod(self):
        return self._iv_period.Value

    @IVPeriod.setter
    def IVPeriod(self, value):
        self._iv_period.Value = value

    @property
    def IVSpikeThreshold(self):
        return self._iv_spike_threshold.Value

    @IVSpikeThreshold.setter
    def IVSpikeThreshold(self, value):
        self._iv_spike_threshold.Value = value

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

    @property
    def IVSecurity(self):
        return self._iv_security.Value

    @IVSecurity.setter
    def IVSecurity(self, value):
        self._iv_security.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.IVSecurity, self.CandleType), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _clear_signal_state(self):
        self._previous_iv = None
        self._iv_time = None
        self._has_iv_change = False
        self._is_iv_spike = False
        self._is_iv_decline = False
        self._main_time = None
        self._main_price = Decimal.Zero
        self._main_average = Decimal.Zero
        self._processed_pair_time = None
        self._pending_order = None

    def OnReseted(self):
        super(iv_spike_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        # Reject before startup side effects: a missing security silently selects the traded instrument.
        if self.IVSecurity is None or self.Security is None or str(self.IVSecurity.Id).lower() == str(self.Security.Id).lower():
            raise InvalidOperationException("IVSecurity must explicitly identify a different external instrument.")
        super(iv_spike_strategy, self).OnStarted2(time)
        self._clear_signal_state()

        self.StartProtection(Unit(), Unit(Decimal(self.StopLossPercent), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # Bid and ask updates let the native stop react between finished candles.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        price_average = SimpleMovingAverage()
        price_average.Length = self.MAPeriod
        iv_average = SimpleMovingAverage()
        iv_average.Length = self.IVPeriod

        main_subscription = self.SubscribeCandles(self.CandleType)
        # The second positional argument is isFinishedOnly, NOT the instrument.
        iv_subscription = self.SubscribeCandles(self.CandleType, security=self.IVSecurity)

        main_subscription.BindEx(price_average, self._process_main_candle, False).Start()
        iv_subscription.BindEx(iv_average, self._process_iv_candle, False).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, main_subscription)
            self.DrawIndicator(area, price_average)
            self.DrawOwnTrades(area)

            iv_area = self.CreateChartArea()
            self.DrawCandles(iv_area, iv_subscription)
            self.DrawIndicator(iv_area, iv_average)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal pairs.
        pass

    def _process_iv_candle(self, candle, average):
        if candle.State != CandleStates.Finished or (self._iv_time is not None and candle.OpenTime <= self._iv_time):
            return

        reading = candle.ClosePrice
        previous = self._previous_iv

        self._iv_time = candle.OpenTime
        self._has_iv_change = previous is not None
        self._is_iv_decline = previous is not None and reading < previous

        # The jump is measured from the previous reading; the average only confirms the level is raised.
        self._is_iv_spike = previous is not None and previous > Decimal.Zero and reading > previous \
            and reading >= Decimal(self.IVSpikeThreshold) * previous \
            and average.Indicator.IsFormed and reading > average.GetValue[Decimal](None)

        self._previous_iv = reading

        self._process_matched_pair()

    def _process_main_candle(self, candle, average):
        if candle.State != CandleStates.Finished or not average.Indicator.IsFormed or (self._main_time is not None and candle.OpenTime <= self._main_time):
            return

        self._main_time = candle.OpenTime
        self._main_price = candle.ClosePrice
        self._main_average = average.GetValue[Decimal](None)

        self._process_matched_pair()

    def _process_matched_pair(self):
        # Act once per bar time and only when both streams finished it; an unmatched bar is never reused.
        if not self._has_iv_change or self._main_time is None or self._iv_time != self._main_time or self._processed_pair_time == self._main_time:
            return

        self._processed_pair_time = self._main_time

        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return

        if self.Position != 0:
            if not self._is_iv_decline:
                return

            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self._is_iv_spike:
            # A close exactly on the average shows no price move to fade.
            if self._main_price < self._main_average:
                self.BuyMarket(self.Volume)
            elif self._main_price > self._main_average:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return iv_spike_strategy()
