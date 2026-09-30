import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import StandardDeviation, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class hv_breakout_strategy(Strategy):
    """
    Breakouts beyond SMA-anchored levels widened by relative population price dispersion (StdDev / Close).
    Exits on an adverse price/SMA crossing or actual-fill percent protection.
    """

    def __init__(self):
        super(hv_breakout_strategy, self).__init__()
        self._hv_period = self.Param("HvPeriod", 20).SetGreaterThanZero().SetDisplay("HV Period", "Period for Historical Volatility calculation", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for the Moving Average that anchors the breakout levels and exits", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._previous_close = None
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

    def OnReseted(self):
        super(hv_breakout_strategy, self).OnReseted()
        self._previous_close = None
        self._previous_mean = Decimal.Zero
        self._pending_order = None

    def OnStarted2(self, time):
        super(hv_breakout_strategy, self).OnStarted2(time)
        self._previous_close = None
        self._pending_order = None
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()
        deviation = StandardDeviation()
        deviation.Length = self._hv_period.Value
        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(deviation, sma, self._process_candle, False).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, deviation)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between finished candles.
        pass

    def _process_candle(self, candle, deviation_value, sma_value):
        if candle.State != CandleStates.Finished or not deviation_value.Indicator.IsFormed or not sma_value.Indicator.IsFormed or not self.IsFormedAndOnlineAndAllowTrading() or candle.ClosePrice <= 0:
            return
        close = candle.ClosePrice
        mean = sma_value.GetValue[Decimal](None)
        if self._previous_close is None:
            self._previous_close = close
            self._previous_mean = mean
            return
        upward_cross = self._previous_close <= self._previous_mean and close > mean
        downward_cross = self._previous_close >= self._previous_mean and close < mean
        self._previous_close = close
        self._previous_mean = mean
        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            return
        if self.Position > 0 and downward_cross:
            self.SellMarket(self.Position)
        elif self.Position < 0 and upward_cross:
            self.BuyMarket(Math.Abs(self.Position))
        elif self.Position == 0:
            # HV is the relative price dispersion StdDev / Close.
            relative_deviation = deviation_value.GetValue[Decimal](None) / close
            if close > mean * (Decimal.One + relative_deviation):
                self.BuyMarket(self.Volume)
            elif close < mean * (Decimal.One - relative_deviation):
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return hv_breakout_strategy()
