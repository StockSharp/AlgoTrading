import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Ichimoku
from StockSharp.Algo.Strategies import Strategy


class ichimoku_cloud_retrace_strategy(Strategy):
    """Ichimoku cloud retrace: trades when price pulls back inside the cloud in direction of kumo slope."""

    def __init__(self):
        super(ichimoku_cloud_retrace_strategy, self).__init__()

        self._tenkan_period = self.Param("TenkanPeriod", 9) \
            .SetGreaterThanZero() \
            .SetDisplay("Tenkan Period", "Tenkan-sen length", "Ichimoku Settings")
        self._kijun_period = self.Param("KijunPeriod", 26) \
            .SetGreaterThanZero() \
            .SetDisplay("Kijun Period", "Kijun-sen length", "Ichimoku Settings")
        self._senkou_span_b_period = self.Param("SenkouSpanBPeriod", 52) \
            .SetGreaterThanZero() \
            .SetDisplay("Senkou Span B Period", "Senkou Span B length", "Ichimoku Settings")
        self._stop_loss_offset = self.Param("StopLossOffset", 0.0) \
            .SetDisplay("Stop Loss Offset", "Distance from entry for stop-loss (price units)", "Risk Management")
        self._take_profit_offset = self.Param("TakeProfitOffset", 0.0) \
            .SetDisplay("Take Profit Offset", "Distance from entry for take-profit (price units)", "Risk Management")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))) \
            .SetDisplay("Candle Type", "Type of candles for analysis", "General")

        self._entry_price = 0.0

    @property
    def TenkanPeriod(self):
        return int(self._tenkan_period.Value)
    @property
    def KijunPeriod(self):
        return int(self._kijun_period.Value)
    @property
    def SenkouSpanBPeriod(self):
        return int(self._senkou_span_b_period.Value)
    @property
    def StopLossOffset(self):
        return float(self._stop_loss_offset.Value)
    @property
    def TakeProfitOffset(self):
        return float(self._take_profit_offset.Value)
    @property
    def CandleType(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(ichimoku_cloud_retrace_strategy, self).OnStarted2(time)

        self._entry_price = 0.0

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanPeriod
        ichimoku.Kijun.Length = self.KijunPeriod
        ichimoku.SenkouB.Length = self.SenkouSpanBPeriod

        self._ichimoku = ichimoku

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(ichimoku, self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ichimoku)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        if not ichimoku_value.IsFinal:
            return

        self._manage_risk(candle)

        if self.Position == 0:
            self._entry_price = 0.0

        senkou_a_val = ichimoku_value.SenkouA
        senkou_b_val = ichimoku_value.SenkouB

        if senkou_a_val is None or senkou_b_val is None:
            return

        senkou_a = float(senkou_a_val)
        senkou_b = float(senkou_b_val)

        open_p = float(candle.OpenPrice)
        close = float(candle.ClosePrice)

        lower_span = min(senkou_a, senkou_b)
        upper_span = max(senkou_a, senkou_b)

        price_inside_cloud = close > lower_span and close < upper_span

        bullish_cloud = senkou_a > senkou_b
        bearish_cloud = senkou_b > senkou_a

        should_buy = bullish_cloud and close > open_p and price_inside_cloud
        should_sell = bearish_cloud and open_p > close and price_inside_cloud

        if should_buy and self.Position <= 0:
            self._entry_price = close
            self.BuyMarket()
        elif should_sell and self.Position >= 0:
            self._entry_price = close
            self.SellMarket()

    def _manage_risk(self, candle):
        if self._entry_price == 0.0:
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if self.StopLossOffset > 0 and close <= self._entry_price - self.StopLossOffset:
                self.SellMarket()
                self._entry_price = 0.0
                return

            if self.TakeProfitOffset > 0 and close >= self._entry_price + self.TakeProfitOffset:
                self.SellMarket()
                self._entry_price = 0.0

        elif self.Position < 0:
            if self.StopLossOffset > 0 and close >= self._entry_price + self.StopLossOffset:
                self.BuyMarket()
                self._entry_price = 0.0
                return

            if self.TakeProfitOffset > 0 and close <= self._entry_price - self.TakeProfitOffset:
                self.BuyMarket()
                self._entry_price = 0.0

    def OnReseted(self):
        super(ichimoku_cloud_retrace_strategy, self).OnReseted()
        self._entry_price = 0.0

    def CreateClone(self):
        return ichimoku_cloud_retrace_strategy()
