import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    ExponentialMovingAverage, SmoothedMovingAverage,
    MovingAverageConvergenceDivergenceSignal, Ichimoku
)
from StockSharp.Algo.Strategies import Strategy


class mc_valute_cloud_strategy(Strategy):
    """Trend-following strategy combining smoothed MAs, Ichimoku cloud filter and MACD confirmation."""

    def __init__(self):
        super(mc_valute_cloud_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Primary timeframe used for signals", "General")
        self._filter_ma_length = self.Param("FilterMaLength", 3) \
            .SetDisplay("Filter EMA", "Length of the trend filter EMA", "Trend")
        self._blue_ma_length = self.Param("BlueMaLength", 13) \
            .SetDisplay("Blue SMMA", "Length of the slower smoothed MA", "Trend")
        self._lime_ma_length = self.Param("LimeMaLength", 5) \
            .SetDisplay("Lime SMMA", "Length of the faster smoothed MA", "Trend")
        self._macd_fast_length = self.Param("MacdFastLength", 12) \
            .SetDisplay("MACD Fast", "Short EMA length for the MACD", "Momentum")
        self._macd_slow_length = self.Param("MacdSlowLength", 26) \
            .SetDisplay("MACD Slow", "Long EMA length for the MACD", "Momentum")
        self._macd_signal_length = self.Param("MacdSignalLength", 9) \
            .SetDisplay("MACD Signal", "Signal EMA length for the MACD", "Momentum")
        self._tenkan_length = self.Param("TenkanLength", 12) \
            .SetDisplay("Tenkan", "Tenkan-sen length for the Ichimoku cloud", "Ichimoku")
        self._kijun_length = self.Param("KijunLength", 20) \
            .SetDisplay("Kijun", "Kijun-sen length for the Ichimoku cloud", "Ichimoku")
        self._senkou_length = self.Param("SenkouLength", 40) \
            .SetDisplay("Senkou Span B", "Span B length for the Ichimoku cloud", "Ichimoku")
        self._take_profit = self.Param("TakeProfit", 30) \
            .SetDisplay("Take Profit", "Take profit distance in points", "Risk")
        self._stop_loss = self.Param("StopLoss", 350) \
            .SetDisplay("Stop Loss", "Stop loss distance in points", "Risk")

        self._filter_value = None
        self._blue_value = None
        self._lime_value = None
        self._senkou_a_value = None
        self._senkou_b_value = None
        self._macd_main_value = None
        self._macd_signal_value = None
        self._last_processed_time = None

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def FilterMaLength(self):
        return self._filter_ma_length.Value

    @property
    def BlueMaLength(self):
        return self._blue_ma_length.Value

    @property
    def LimeMaLength(self):
        return self._lime_ma_length.Value

    @property
    def MacdFastLength(self):
        return self._macd_fast_length.Value

    @property
    def MacdSlowLength(self):
        return self._macd_slow_length.Value

    @property
    def MacdSignalLength(self):
        return self._macd_signal_length.Value

    @property
    def TenkanLength(self):
        return self._tenkan_length.Value

    @property
    def KijunLength(self):
        return self._kijun_length.Value

    @property
    def SenkouLength(self):
        return self._senkou_length.Value

    @property
    def TakeProfit(self):
        return self._take_profit.Value

    @property
    def StopLoss(self):
        return self._stop_loss.Value

    def OnReseted(self):
        super(mc_valute_cloud_strategy, self).OnReseted()
        self._filter_value = None
        self._blue_value = None
        self._lime_value = None
        self._senkou_a_value = None
        self._senkou_b_value = None
        self._macd_main_value = None
        self._macd_signal_value = None
        self._last_processed_time = None

    def OnStarted(self, time):
        super(mc_valute_cloud_strategy, self).OnStarted(time)

        filter_ma = ExponentialMovingAverage()
        filter_ma.Length = self.FilterMaLength

        blue_ma = SmoothedMovingAverage()
        blue_ma.Length = self.BlueMaLength

        lime_ma = SmoothedMovingAverage()
        lime_ma.Length = self.LimeMaLength

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self.MacdFastLength
        macd.Macd.LongMa.Length = self.MacdSlowLength
        macd.SignalMa.Length = self.MacdSignalLength

        ichimoku = Ichimoku()
        ichimoku.Tenkan.Length = self.TenkanLength
        ichimoku.Kijun.Length = self.KijunLength
        ichimoku.SenkouB.Length = self.SenkouLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(filter_ma, blue_ma, lime_ma, self._process_moving_averages)
        subscription.BindEx(macd, self._process_macd)
        subscription.BindEx(ichimoku, self._process_ichimoku)
        subscription.Start()

    def _process_moving_averages(self, candle, filter_val, blue_val, lime_val):
        if candle.State != CandleStates.Finished:
            return

        self._filter_value = float(filter_val)
        self._blue_value = float(blue_val)
        self._lime_value = float(lime_val)

        self._try_trade(candle)

    def _process_macd(self, candle, macd_value):
        if candle.State != CandleStates.Finished:
            return

        macd_raw = macd_value.Macd
        signal_raw = macd_value.Signal

        if macd_raw is None or signal_raw is None:
            return

        self._macd_main_value = float(macd_raw)
        self._macd_signal_value = float(signal_raw)

        self._try_trade(candle)

    def _process_ichimoku(self, candle, ichimoku_value):
        if candle.State != CandleStates.Finished:
            return

        senkou_a = ichimoku_value.SenkouA
        senkou_b = ichimoku_value.SenkouB

        if senkou_a is None or senkou_b is None:
            return

        self._senkou_a_value = float(senkou_a)
        self._senkou_b_value = float(senkou_b)

        self._try_trade(candle)

    def _try_trade(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self._last_processed_time == candle.OpenTime:
            return

        if self._filter_value is None or self._blue_value is None or self._lime_value is None:
            return
        if self._senkou_a_value is None or self._senkou_b_value is None:
            return
        if self._macd_main_value is None or self._macd_signal_value is None:
            return

        self._last_processed_time = candle.OpenTime

        filter_val = self._filter_value
        blue = self._blue_value
        lime = self._lime_value
        span_a = self._senkou_a_value
        span_b = self._senkou_b_value
        macd = self._macd_main_value
        signal = self._macd_signal_value

        cloud_top = max(span_a, span_b)
        cloud_bottom = min(span_a, span_b)
        ma_upper = max(blue, lime)
        ma_lower = min(blue, lime)

        allow_long = filter_val > ma_upper and filter_val > cloud_top and macd > signal
        allow_short = filter_val < ma_lower and filter_val < cloud_bottom and macd < signal

        if allow_long and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
        elif allow_short and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()

    def CreateClone(self):
        return mc_valute_cloud_strategy()
