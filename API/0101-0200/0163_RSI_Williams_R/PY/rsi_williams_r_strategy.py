import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import RelativeStrengthIndex, WilliamsR
from StockSharp.Algo.Strategies import Strategy

class rsi_williams_r_strategy(Strategy):
    """
    RSI Williams %R strategy.
    RSI below RsiOversold together with Williams %R below WilliamsROversold goes long, RSI above RsiOverbought together with %R above
    WilliamsROverbought goes short, reversing an opposite position. A long closes once RSI returns to the neutral 50 level from below
    and a short once it returns from above, and a percent stop limits the loss.
    """

    def __init__(self):
        super(rsi_williams_r_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14).SetGreaterThanZero().SetDisplay("RSI Period", "Period of RSI", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI level for longs", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI level for shorts", "RSI")
        self._williams_r_period = self.Param("WilliamsRPeriod", 14).SetGreaterThanZero().SetDisplay("Williams %R Period", "Period of Williams %R", "Williams %R")
        self._williams_r_oversold = self.Param("WilliamsROversold", -80.0).SetDisplay("Williams %R Oversold", "Williams %R level for longs", "Williams %R")
        self._williams_r_overbought = self.Param("WilliamsROverbought", -20.0).SetDisplay("Williams %R Overbought", "Williams %R level for shorts", "Williams %R")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(rsi_williams_r_strategy, self).OnStarted2(time)

        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        williams = WilliamsR()
        williams.Length = self._williams_r_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(rsi, williams, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, williams)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, rsi_value, williams_value):
        if candle.State != CandleStates.Finished:
            return

        if not rsi_value.IsFormed or not williams_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)
        williams = williams_value.GetValue[Decimal](None)

        middle = Decimal(50)
        if rsi < Decimal(self._rsi_oversold.Value) and williams < Decimal(self._williams_r_oversold.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif rsi > Decimal(self._rsi_overbought.Value) and williams > Decimal(self._williams_r_overbought.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and rsi >= middle:
            self.SellMarket(self.Position)
        elif self.Position < 0 and rsi <= middle:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return rsi_williams_r_strategy()
