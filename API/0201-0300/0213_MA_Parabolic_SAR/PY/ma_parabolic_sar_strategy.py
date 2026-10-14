import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class ma_parabolic_sar_strategy(Strategy):
    """
    MA Parabolic SAR strategy.
    A close above both the MaPeriod simple moving average and the Parabolic SAR goes long and a close below both goes short, reversing
    an opposite position. The SAR is the trailing stop: a long closes when price closes below it and a short when price closes above it,
    and a percent stop limits the loss.
    """

    def __init__(self):
        super(ma_parabolic_sar_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period of the simple moving average", "Indicators")
        self._sar_step = self.Param("SarStep", 0.02).SetGreaterThanZero().SetDisplay("SAR Step", "Acceleration factor of the SAR", "Indicators")
        self._sar_max_step = self.Param("SarMaxStep", 0.2).SetGreaterThanZero().SetDisplay("SAR Max Step", "Maximum acceleration factor of the SAR", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ma_parabolic_sar_strategy, self).OnStarted2(time)

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        sar = ParabolicSar()
        sar.Acceleration = Decimal(self._sar_step.Value)
        sar.AccelerationMax = Decimal(self._sar_max_step.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ma, sar, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, ma_value, sar_value):
        if candle.State != CandleStates.Finished:
            return

        # The first SAR value is formed but empty.
        if not ma_value.IsFormed or not sar_value.IsFormed or sar_value.IsEmpty:
            return

        ma = ma_value.GetValue[Decimal](None)
        sar = sar_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if close > ma and close > sar and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < ma and close < sar and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < sar:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > sar:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ma_parabolic_sar_strategy()
