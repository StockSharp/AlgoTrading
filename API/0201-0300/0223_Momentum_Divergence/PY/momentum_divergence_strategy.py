import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import Momentum
from StockSharp.Algo.Strategies import Strategy

class momentum_divergence_strategy(Strategy):
    """
    Momentum Divergence strategy.
    A close below the previous close while MomentumPeriod momentum rises above its previous value is a bullish divergence and goes long;
    a close above the previous close while momentum falls is a bearish one and goes short, reversing an opposite position. A long closes
    when momentum crosses below zero and a short when it crosses above zero, and a percent stop limits the loss.
    """

    def __init__(self):
        super(momentum_divergence_strategy, self).__init__()
        self._momentum_period = self.Param("MomentumPeriod", 14).SetGreaterThanZero().SetDisplay("Momentum Period", "Period of the momentum indicator", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_close = None
        self._prev_momentum = None

    def OnReseted(self):
        super(momentum_divergence_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(momentum_divergence_strategy, self).OnStarted2(time)

        self._reset_state()

        momentum = Momentum()
        momentum.Length = self._momentum_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(momentum, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, momentum)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, momentum_value):
        if candle.State != CandleStates.Finished:
            return

        if not momentum_value.IsFormed:
            return

        close = candle.ClosePrice
        value = momentum_value.GetValue[Decimal](None)
        last_close = self._prev_close
        last_momentum = self._prev_momentum
        self._prev_close = close
        self._prev_momentum = value

        if last_close is None or last_momentum is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        bullish = close < last_close and value > last_momentum
        bearish = close > last_close and value < last_momentum

        if bullish and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif bearish and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and last_momentum >= 0 and value < 0:
            self.SellMarket(self.Position)
        elif self.Position < 0 and last_momentum <= 0 and value > 0:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return momentum_divergence_strategy()
