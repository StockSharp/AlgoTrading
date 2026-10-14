import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import BollingerBands, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class bollinger_bands_strategy(Strategy):
    """
    Bollinger Bands strategy.
    A close above the upper Bollinger band goes long and a close below the lower band goes short, reversing an opposite position.
    A long closes when the close falls below SMA(SmaLength) and a short when it rises above it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(bollinger_bands_strategy, self).__init__()
        self._bb_length = self.Param("BbLength", 120).SetGreaterThanZero().SetDisplay("BB Length", "Bollinger period", "Bollinger")
        self._bb_deviation = self.Param("BbDeviation", 2.0).SetGreaterThanZero().SetDisplay("BB Deviation", "Bollinger standard deviation multiplier", "Bollinger")
        self._sma_length = self.Param("SmaLength", 110).SetGreaterThanZero().SetDisplay("SMA Length", "Period of the exit SMA", "Exit")
        self._stop_loss_percent = self.Param("StopLossPercent", 6.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(bollinger_bands_strategy, self).OnStarted2(time)

        bollinger = BollingerBands()
        bollinger.Length = self._bb_length.Value
        bollinger.Width = Decimal(self._bb_deviation.Value)
        sma = SimpleMovingAverage()
        sma.Length = self._sma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bollinger, sma, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, bollinger_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        if not bollinger_value.IsFormed or not sma_value.IsFormed:
            return

        upper = bollinger_value.UpBand
        lower = bollinger_value.LowBand
        if upper is None or lower is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        sma = sma_value.GetValue[Decimal](None)

        if close > upper and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < lower and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < sma:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > sma:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return bollinger_bands_strategy()
