import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, OrderStates, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy

class rsi_reversion_strategy(Strategy):
    """
    Strategy based on RSI mean reversion.
    Buys when RSI enters oversold, sells when it enters overbought, exits at neutral.
    """

    def __init__(self):
        super(rsi_reversion_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero() \
            .SetDisplay("RSI Period", "Period for RSI calculation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._oversold_threshold = self.Param("OversoldThreshold", 30.0).SetRange(0.0, 100.0) \
            .SetDisplay("Oversold Threshold", "Enter long on a strict downward crossing.", "Indicators")
        self._overbought_threshold = self.Param("OverboughtThreshold", 70.0).SetRange(0.0, 100.0) \
            .SetDisplay("Overbought Threshold", "Enter short on a strict upward crossing.", "Indicators")
        self._exit_level = self.Param("ExitLevel", 50.0).SetRange(0.0, 100.0) \
            .SetDisplay("Exit Level", "Close on return to the neutral level.", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative() \
            .SetDisplay("Stop Loss (%)", "Actual-fill percent stop; zero disables it.", "Protection")

        self._prev_rsi = Decimal.Zero
        self._has_prev_values = False
        self._pending_order = None
        self.OrderRegistering += self._track_pending

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type), (self.Security, DataType.Level1)]

    def _track_pending(self, order):
        self._pending_order = order

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(rsi_reversion_strategy, self).OnReseted()
        self._prev_rsi = Decimal.Zero
        self._has_prev_values = False
        self._pending_order = None

    def OnStarted2(self, time):
        super(rsi_reversion_strategy, self).OnStarted2(time)
        if not (self._oversold_threshold.Value < self._exit_level.Value < self._overbought_threshold.Value):
            raise ValueError("OversoldThreshold must be below ExitLevel, which must be below OverboughtThreshold.")

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, rsi)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # Native protection runs before this callback, including between signal bars.
        pass

    def _process_candle(self, candle, rsi_value):
        if candle.State != CandleStates.Finished or not self.IsFormedAndOnlineAndAllowTrading():
            return

        rv = rsi_value

        if not self._has_prev_values:
            self._has_prev_values = True
            self._prev_rsi = rv
            return

        if self._pending_order is not None and self._pending_order.State not in (OrderStates.Done, OrderStates.Failed):
            self._prev_rsi = rv
            return

        if self._prev_rsi >= Decimal(self._oversold_threshold.Value) and rv < Decimal(self._oversold_threshold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + Math.Abs(self.Position))
        elif self._prev_rsi <= Decimal(self._overbought_threshold.Value) and rv > Decimal(self._overbought_threshold.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif self.Position > 0 and rv >= Decimal(self._exit_level.Value):
            self.SellMarket(self.Position)
        elif self.Position < 0 and rv <= Decimal(self._exit_level.Value):
            self.BuyMarket(Math.Abs(self.Position))

        self._prev_rsi = rv

    def CreateClone(self):
        return rsi_reversion_strategy()
