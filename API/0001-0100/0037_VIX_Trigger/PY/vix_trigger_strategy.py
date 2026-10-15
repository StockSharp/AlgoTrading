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

class vix_trigger_strategy(Strategy):
    """
    Strategy that trades based on VIX (Volatility Index) movements.
    It enters positions when VIX is rising (indicating increasing fear/volatility in the market)
    and price is moving in an expected direction relative to its moving average.
    
    """
    def __init__(self):
        super(vix_trigger_strategy, self).__init__()
        
        self._clear_signal_state()
        self.OrderRegistering += self._track_pending

        # Initialize strategy parameters
        self._maPeriod = self.Param("MAPeriod", 20).SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Technical Parameters")

        self._stopLossPercent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss %", "Stop loss as percentage from entry price", "Risk Management")

        self._candleType = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "Data")

        self._vixSecurity = self.Param[Security]("VixSecurity", None) \
            .SetDisplay("VIX Security", "VIX Security to use for signals", "Data").SetRequired()

    @property
    def MAPeriod(self):
        return self._maPeriod.Value

    @MAPeriod.setter
    def MAPeriod(self, value):
        self._maPeriod.Value = value

    @property
    def StopLossPercent(self):
        return self._stopLossPercent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stopLossPercent.Value = value

    @property
    def CandleType(self):
        return self._candleType.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candleType.Value = value

    @property
    def VixSecurity(self):
        return self._vixSecurity.Value

    @VixSecurity.setter
    def VixSecurity(self, value):
        self._vixSecurity.Value = value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType), (self.VixSecurity, self.CandleType), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    def _clear_signal_state(self):
        self._previous_vix = None
        self._vix_time = None
        self._vix_direction = 0
        self._has_vix_direction = False
        self._main_time = None
        self._main_price = Decimal.Zero
        self._main_average = Decimal.Zero
        self._processed_pair_time = None
        self._pending_order = None

    def OnReseted(self):
        super(vix_trigger_strategy, self).OnReseted()
        self._clear_signal_state()

    def OnStarted2(self, time):
        # Reject before startup side effects: a missing security silently selects the primary.
        if self.VixSecurity is None or self.Security is None or str(self.VixSecurity.Id).lower() == str(self.Security.Id).lower():
            raise InvalidOperationException("VixSecurity must explicitly identify a different external instrument.")
        super(vix_trigger_strategy, self).OnStarted2(time)
        self._clear_signal_state()
        self.StartProtection(Unit(), Unit(Decimal(self.StopLossPercent), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        sma = SimpleMovingAverage()
        sma.Length = self.MAPeriod
        main_subscription = self.SubscribeCandles(self.CandleType)
        # The second positional argument is isFinishedOnly, NOT the instrument.
        index_subscription = self.SubscribeCandles(self.CandleType, security=self.VixSecurity)
        main_subscription.BindEx(sma, self._process_main_candle, False).Start()
        index_subscription.Bind(self._process_vix_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, main_subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal pairs.
        pass

    def _process_vix_candle(self, candle):
        if candle.State != CandleStates.Finished or (self._vix_time is not None and candle.OpenTime <= self._vix_time):
            return
        self._vix_time = candle.OpenTime
        self._has_vix_direction = self._previous_vix is not None
        self._vix_direction = Math.Sign(candle.ClosePrice - self._previous_vix) if self._previous_vix is not None else 0
        self._previous_vix = candle.ClosePrice
        self._process_matched_pair()

    def _process_main_candle(self, candle, average):
        if candle.State != CandleStates.Finished or not average.Indicator.IsFormed or (self._main_time is not None and candle.OpenTime <= self._main_time):
            return
        self._main_time = candle.OpenTime
        self._main_price = candle.ClosePrice
        self._main_average = average.GetValue[Decimal](None)
        self._process_matched_pair()

    def _process_matched_pair(self):
        # Keep only each source's latest bar; stale unmatched bars are not reused.
        if not self._has_vix_direction or self._main_time is None or self._vix_time != self._main_time or self._processed_pair_time == self._main_time:
            return
        self._processed_pair_time = self._main_time
        if not self.IsFormedAndOnlineAndAllowTrading() or (self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed)):
            return
        if self.Position != 0 and self._vix_direction < 0:
            if self.Position > 0:
                self.SellMarket(self.Position)
            else:
                self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0 and self._vix_direction > 0:
            if self._main_price < self._main_average:
                self.BuyMarket(self.Volume)
            elif self._main_price > self._main_average:
                self.SellMarket(self.Volume)
        # Equal index is not falling; equal price/MA is not a short signal.

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return vix_trigger_strategy()
