import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class ma_cci_strategy(Strategy):
    """
    MA CCI strategy.
    A close above the MaPeriod SMA with CCI below OversoldLevel goes long and a close below it with CCI above OverboughtLevel goes short,
    reversing an opposite position. The position closes once CCI returns to the zero line, and a percent stop limits the loss.
    """

    def __init__(self):
        super(ma_cci_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 50).SetGreaterThanZero().SetDisplay("MA Period", "Period of the trend SMA", "Indicators")
        self._cci_period = self.Param("CciPeriod", 20).SetGreaterThanZero().SetDisplay("CCI Period", "Period of CCI", "Indicators")
        self._overbought_level = self.Param("OverboughtLevel", 100.0).SetDisplay("Overbought Level", "CCI level for shorts", "Indicators")
        self._oversold_level = self.Param("OversoldLevel", -100.0).SetDisplay("Oversold Level", "CCI level for longs", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ma_cci_strategy, self).OnStarted2(time)

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, cci, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, cci)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_value, cci_value):
        if candle.State != CandleStates.Finished:
            return

        if not sma_value.IsFormed or not cci_value.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ma = sma_value.GetValue[Decimal](None)
        cci = cci_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        zero = Decimal(0)
        if close > ma and cci < Decimal(self._oversold_level.Value) and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < ma and cci > Decimal(self._overbought_level.Value) and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and cci >= zero:
            self.SellMarket(self.Position)
        elif self.Position < 0 and cci <= zero:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ma_cci_strategy()
