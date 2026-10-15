import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import HullMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class cvd_divergence_volume_hma_rsi_macd_strategy(Strategy):
    """
    CVD divergence strategy with HMA trend, RSI, MACD and volume filters.
    Each candle's volume delta is its volume signed by the candle direction; the running sum is the CVD.
    A long needs HMA20 above HMA50 with price above HMA20, RSI between 40 and RsiOverbought, MACD above its signal with a rising histogram,
    volume above VolumeMultiplier times its average, and a bullish CVD divergence or CVD above its CvdLength average. Shorts mirror this.
    A long exits when price drops below HMA20, RSI rises above RsiOverbought or MACD crosses below its signal; shorts mirror this.
    """

    def __init__(self):
        super(cvd_divergence_volume_hma_rsi_macd_strategy, self).__init__()
        self._hma20_length = self.Param("Hma20Length", 20).SetGreaterThanZero().SetDisplay("HMA20 Length", "Fast HMA length", "Trend")
        self._hma50_length = self.Param("Hma50Length", 50).SetGreaterThanZero().SetDisplay("HMA50 Length", "Slow HMA length", "Trend")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI length", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0).SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._rsi_oversold = self.Param("RsiOversold", 30.0).SetDisplay("RSI Oversold", "RSI oversold level", "RSI")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal period", "MACD")
        self._volume_ma_length = self.Param("VolumeMaLength", 20).SetGreaterThanZero().SetDisplay("Volume MA Length", "Volume average length", "Volume")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5).SetGreaterThanZero().SetDisplay("Volume Multiplier", "Volume must exceed its average times this", "Volume")
        self._cvd_length = self.Param("CvdLength", 14).SetGreaterThanZero().SetDisplay("CVD Length", "Length of the CVD average", "CVD")
        self._divergence_lookback = self.Param("DivergenceLookback", 5).SetGreaterThanZero().SetDisplay("Divergence Lookback", "Bars between the compared divergence points", "CVD")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_sma = None
        self._cvd_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.candle_type)]

    def _reset_state(self):
        self._closes = []
        self._cvds = []
        self._cvd = 0.0
        self._prev_macd = None
        self._prev_signal = None
        self._prev_histogram = None

    def OnReseted(self):
        super(cvd_divergence_volume_hma_rsi_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(cvd_divergence_volume_hma_rsi_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        hma20 = HullMovingAverage()
        hma20.Length = self._hma20_length.Value
        hma50 = HullMovingAverage()
        hma50.Length = self._hma50_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._volume_ma_length.Value
        self._cvd_sma = SimpleMovingAverage()
        self._cvd_sma.Length = self._cvd_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(hma20, hma50, rsi, macd, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, hma20)
            self.DrawIndicator(area, hma50)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, hma20_value, hma50_value, rsi_value, macd_value):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        open_price = float(candle.OpenPrice)
        volume = float(candle.TotalVolume)

        delta = volume if close > open_price else (-volume if close < open_price else 0.0)
        self._cvd += delta

        volume_avg = process_float(self._volume_sma, candle.TotalVolume, candle.ServerTime, True)
        cvd_avg = process_float(self._cvd_sma, Decimal(self._cvd), candle.ServerTime, True)

        self._closes.append(close)
        self._cvds.append(self._cvd)
        max_history = self._divergence_lookback.Value + 1
        if len(self._closes) > max_history:
            self._closes.pop(0)
            self._cvds.pop(0)

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)
        histogram = macd_line - signal_line
        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        prev_histogram = self._prev_histogram
        self._prev_macd = macd_line
        self._prev_signal = signal_line
        self._prev_histogram = histogram

        if not hma20_value.IsFormed or not hma50_value.IsFormed or not rsi_value.IsFormed or not volume_avg.IsFormed or not cvd_avg.IsFormed:
            return

        if prev_macd is None or prev_signal is None or prev_histogram is None or len(self._closes) < max_history:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        hma20 = float(hma20_value.GetValue[Decimal](None))
        hma50 = float(hma50_value.GetValue[Decimal](None))
        rsi = float(rsi_value.GetValue[Decimal](None))
        cvd_ma = float(cvd_avg.GetValue[Decimal](None))
        overbought = float(self._rsi_overbought.Value)
        oversold = float(self._rsi_oversold.Value)

        macd_cross_down = prev_macd >= prev_signal and macd_line < signal_line
        macd_cross_up = prev_macd <= prev_signal and macd_line > signal_line

        if self.Position > 0:
            if close < hma20 or rsi > overbought or macd_cross_down:
                self.SellMarket(self.Position)
            return

        if self.Position < 0:
            if close > hma20 or rsi < oversold or macd_cross_up:
                self.BuyMarket(-self.Position)
            return

        old_close = self._closes[0]
        old_cvd = self._cvds[0]

        # Price makes a lower point while CVD makes a higher one, or the reverse.
        bullish_divergence = close < old_close and self._cvd > old_cvd
        bearish_divergence = close > old_close and self._cvd < old_cvd

        volume_ok = volume > float(volume_avg.GetValue[Decimal](None)) * float(self._volume_multiplier.Value)

        long_signal = (hma20 > hma50 and close > hma20
                       and 40.0 < rsi < overbought
                       and macd_line > signal_line and histogram > prev_histogram
                       and volume_ok
                       and (bullish_divergence or self._cvd > cvd_ma))

        short_signal = (hma20 < hma50 and close < hma20
                        and oversold < rsi < 60.0
                        and macd_line < signal_line and histogram < prev_histogram
                        and volume_ok
                        and (bearish_divergence or self._cvd < cvd_ma))

        if long_signal:
            self.BuyMarket()
        elif short_signal:
            self.SellMarket()

    def CreateClone(self):
        return cvd_divergence_volume_hma_rsi_macd_strategy()
