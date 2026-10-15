import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import Ichimoku, ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class ichimoku_cloud_buy_custom_ema_exit_strategy(Strategy):
    """
    Ichimoku cloud buy with custom EMA exit strategy.
    Long only. A long opens when the close is above the Ichimoku cloud and the candle volume exceeds its VolumeAvgPeriod average,
    optionally also requiring the close above the EmaLength EMA. The position closes when the close falls below the EMA,
    and a percent stop loss limits the loss.
    """

    def __init__(self):
        super(ichimoku_cloud_buy_custom_ema_exit_strategy, self).__init__()
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
        self._senkou_span_period = self.Param("SenkouSpanPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou Span Period", "Senkou Span B period", "Ichimoku")
        self._ema_length = self.Param("EmaLength", 44).SetGreaterThanZero().SetDisplay("EMA Length", "Exit EMA period", "Exit")
        self._volume_avg_period = self.Param("VolumeAvgPeriod", 10).SetGreaterThanZero().SetDisplay("Volume Avg Period", "Volume average period", "Volume")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk")
        self._use_ema_filter = self.Param("UseEmaFilter", True).SetDisplay("Use EMA Filter", "Require the close above the EMA for entries", "Exit")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ichimoku_cloud_buy_custom_ema_exit_strategy, self).OnReseted()
        self._volume_sma = None

    def OnStarted2(self, time):
        super(ichimoku_cloud_buy_custom_ema_exit_strategy, self).OnStarted2(time)

        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_avg_period.Value

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self._tenkan_period.Value
        ichimoku.Kijun.Length = self._kijun_period.Value
        ichimoku.SenkouB.Length = self._senkou_span_period.Value
        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(ichimoku, ema, self._process_candle).Start()

        self.StartProtection(Unit(), Unit(Decimal(self._stop_loss_percent.Value), UnitTypes.Percent), useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ichimoku_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        volume = candle.TotalVolume
        volume_value = process_value(self._volume_sma, volume, candle.OpenTime, True)

        if not self._volume_sma.IsFormed or volume_value.IsEmpty or not ema_value.IsFormed:
            return

        if not ichimoku_value.IsFormed or ichimoku_value.SenkouA is None or ichimoku_value.SenkouB is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        average_volume = to_decimal(volume_value)
        ema = ema_value.GetValue[Decimal](None)
        close = candle.ClosePrice

        if self.Position > 0:
            if close < ema:
                self.SellMarket(self.Position)
            return

        above_cloud = close > max(ichimoku_value.SenkouA, ichimoku_value.SenkouB)
        ema_ok = not self._use_ema_filter.Value or close > ema

        if above_cloud and volume > average_volume and ema_ok:
            self.BuyMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return ichimoku_cloud_buy_custom_ema_exit_strategy()
