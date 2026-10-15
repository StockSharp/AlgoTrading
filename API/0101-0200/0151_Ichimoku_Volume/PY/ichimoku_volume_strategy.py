import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes, Level1Fields
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy

class ichimoku_volume_strategy(Strategy):
    """
    Ichimoku Volume strategy.
    A close above the cloud with Tenkan-sen above Kijun-sen on volume above the average of the previous VolumeAvgPeriod candles goes long;
    a close below the cloud with Tenkan-sen below Kijun-sen on such volume goes short, reversing an opposite position.
    A long closes when price closes below the cloud and a short when it closes above it, and a percent stop limits the loss.
    """

    def __init__(self):
        super(ichimoku_volume_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Period of Tenkan-sen", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Period of Kijun-sen", "Ichimoku")
        self._senkou_span_period = self.Param("SenkouSpanPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span Period", "Period of Senkou Span B", "Ichimoku")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 20).SetGreaterThanZero().SetDisplay("Volume Average Period", "Previous candles the volume is averaged over", "Volume")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._volumes = []

    def OnReseted(self):
        super(ichimoku_volume_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(ichimoku_volume_strategy, self).OnStarted2(time)

        self._reset_state()

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True, isLocalStop=True)

        # The stop has to see prices between candles, not only at their close.
        for field in (Level1Fields.BestBidPrice, Level1Fields.BestAskPrice):
            quotes = Subscription(DataType.Level1, self.Security)
            quotes.MarketData.BuildField = field
            self.SubscribeLevel1(quotes).Bind(self._observe_protection_quote).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def _observe_protection_quote(self, quote):
        # The high-level handler activates native protection before the callback, including between bars.
        pass

    def _process_candle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        # Volume is compared with the candles before this one.
        period = self._volume_avg_period.Value
        average = None
        if len(self._volumes) == period:
            total = Decimal(0)
            for volume in self._volumes:
                total += volume
            average = total / Decimal(period)

        self._volumes.append(candle.TotalVolume)
        if len(self._volumes) > period:
            self._volumes.pop(0)

        tenkan = ichimoku_value.Tenkan
        kijun = ichimoku_value.Kijun
        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB
        if tenkan is None or kijun is None or senkou_a is None or senkou_b is None:
            return

        if average is None or not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = candle.ClosePrice
        cloud_top = senkou_a if senkou_a > senkou_b else senkou_b
        cloud_bottom = senkou_b if senkou_a > senkou_b else senkou_a
        surge = candle.TotalVolume > average

        if close > cloud_top and tenkan > kijun and surge and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif close < cloud_bottom and tenkan < kijun and surge and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
        elif self.Position > 0 and close < cloud_bottom:
            self.SellMarket(self.Position)
        elif self.Position < 0 and close > cloud_top:
            self.BuyMarket(-self.Position)

    def CreateClone(self):
        return ichimoku_volume_strategy()
