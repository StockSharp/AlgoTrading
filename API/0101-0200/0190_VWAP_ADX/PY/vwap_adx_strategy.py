import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class vwap_adx_strategy(Strategy):
    """
    VWAP ADX strategy.
    The market trades around the clock, so the session VWAP restarts with each UTC day and weighs each candle's typical price by its volume.
    While ADX is above AdxThreshold, a close above VWAP goes long and a close below it goes short, reversing an opposite position.
    The position closes once ADX drops below AdxExitThreshold, and a percent stop limits the loss.
    """

    def __init__(self):
        super(vwap_adx_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14).SetGreaterThanZero().SetDisplay("ADX Period", "Period of ADX", "ADX")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetDisplay("ADX Threshold", "ADX level required to enter", "ADX")
        self._adx_exit_threshold = self.Param("AdxExitThreshold", 20.0).SetDisplay("ADX Exit Threshold", "ADX level below which the position closes", "ADX")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._day = None
        self._cumulative_price_volume = Decimal(0)
        self._cumulative_volume = Decimal(0)

    def OnReseted(self):
        super(vwap_adx_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(vwap_adx_strategy, self).OnStarted2(time)

        self._reset_state()

        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self._process_candle).Start()

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
                self.DrawIndicator(oscillators, adx)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._cumulative_price_volume = Decimal(0)
            self._cumulative_volume = Decimal(0)

        typical_price = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / Decimal(3)
        self._cumulative_price_volume += typical_price * candle.TotalVolume
        self._cumulative_volume += candle.TotalVolume

        if not adx_value.IsFormed or self._cumulative_volume <= 0 or adx_value.MovingAverage is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        strength = adx_value.MovingAverage
        vwap = self._cumulative_price_volume / self._cumulative_volume
        close = candle.ClosePrice
        strong = strength > Decimal(self._adx_threshold.Value)
        exit_level = Decimal(self._adx_exit_threshold.Value)

        if strong and close > vwap and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif strong and close < vwap and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and strength < exit_level:
            self.SellMarket(self.Position)
        elif self.Position < 0 and strength < exit_level:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return vwap_adx_strategy()
