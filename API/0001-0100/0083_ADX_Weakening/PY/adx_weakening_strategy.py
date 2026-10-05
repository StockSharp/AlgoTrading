import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class adx_weakening_strategy(Strategy):
    """
    ADX Weakening strategy.
    While flat, a fall of ADX from the previous candle buys when the close is above the SMA and sells when it is below.
    The position is held until ADX rises again or the percent stop is hit.
    """

    def __init__(self):
        super(adx_weakening_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period for ADX", "Indicators")
        self._ma_period = self.Param("MaPeriod", 20).SetGreaterThanZero().SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))).SetDisplay("Candle Type", "Type of candles to use", "General")

        self._prev_adx = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(adx_weakening_strategy, self).OnReseted()
        self._prev_adx = None

    def OnStarted2(self, time):
        super(adx_weakening_strategy, self).OnStarted2(time)

        self._prev_adx = None

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(sma, adx, self._process_candle).Start()

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
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, sma_iv, adx_iv):
        if candle.State != CandleStates.Finished or not adx_iv.IsFormed or not sma_iv.IsFormed:
            return
        if adx_iv.MovingAverage is None:
            return

        adx = adx_iv.MovingAverage
        prev_adx = self._prev_adx
        self._prev_adx = adx

        if prev_adx is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        sma = sma_iv.GetValue[Decimal](None)
        close = candle.ClosePrice

        if self.Position > 0:
            if adx > prev_adx:
                self.SellMarket(self.Position)
        elif self.Position < 0:
            if adx > prev_adx:
                self.BuyMarket(-self.Position)
        elif adx < prev_adx:
            if close > sma:
                self.BuyMarket(self.Volume)
            elif close < sma:
                self.SellMarket(self.Volume)

    def CreateClone(self):
        return adx_weakening_strategy()
