import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from System.Globalization import CultureInfo
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

_COMPARISON_TOLERANCE = Decimal.Parse("0.00000001", CultureInfo.InvariantCulture)

class stochastic_overbought_oversold_strategy(Strategy):
    """
    Fades confirmed stochastic extremes and exits on the return to neutral.
    The native oscillator's D is the smoothed %K; a second SMA forms %D.
    """

    def __init__(self):
        super(stochastic_overbought_oversold_strategy, self).__init__()
        self._stoch_period = self.Param("StochPeriod", 14).SetGreaterThanZero().SetDisplay("Stochastic Period", "Raw high/low lookback", "Indicators")
        self._k_period = self.Param("KPeriod", 3).SetGreaterThanZero().SetDisplay("K Period", "SMA length of raw %K", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetGreaterThanZero().SetDisplay("D Period", "SMA length of smoothed %K", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Stochastic timeframe", "General")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
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
        self._d_window = []
        self._previous_k = None
        self._pending_order = None

    def OnReseted(self):
        super(stochastic_overbought_oversold_strategy, self).OnReseted()
        self._clear_state()

    def OnStarted2(self, time):
        super(stochastic_overbought_oversold_strategy, self).OnStarted2(time)
        self._clear_state()
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        stochastic = StochasticOscillator()
        stochastic.K.Length = self._stoch_period.Value
        stochastic.D.Length = self._k_period.Value
        candles = self.SubscribeCandles(self.candle_type)
        candles.BindEx(stochastic, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native actual-fill protection evaluates executable quotes between signal candles.
        pass

    def _process_candle(self, candle, output):
        if candle.State != CandleStates.Finished or not output.Indicator.IsFormed:
            return
        k = output.D
        if k is None:
            return
        self._d_window.append(k)
        d_period = self._d_period.Value
        if len(self._d_window) > d_period:
            self._d_window.pop(0)
        if len(self._d_window) < d_period:
            return
        d = sum(self._d_window, Decimal(0)) / Decimal(d_period)
        prior = self._previous_k
        self._previous_k = k
        if prior is None or not self.IsFormedAndOnlineAndAllowTrading():
            return
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        # Native rolling SMA can leave a sub-ulp remainder at exact levels (e.g. 50).
        tolerance = _COMPARISON_TOLERANCE
        if self.Position == 0 and prior < 20 - tolerance and k > prior + tolerance and k > d + tolerance:
            self.BuyMarket(self.Volume)
        elif self.Position == 0 and prior > 80 + tolerance and k < prior - tolerance and k < d - tolerance:
            self.SellMarket(self.Volume)
        # %K crossing 50 either way closes, so a position whose entry bar already passed 50 exits on the cross back.
        elif self.Position > 0 and (prior < 50 - tolerance) != (k < 50 - tolerance):
            self.SellMarket(self.Position)
        elif self.Position < 0 and (prior > 50 + tolerance) != (k > 50 + tolerance):
            self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        return stochastic_overbought_oversold_strategy()
