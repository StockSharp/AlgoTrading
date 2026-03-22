import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    MovingAverageConvergenceDivergenceSignal,
    AverageDirectionalIndex,
    AverageTrueRange,
    SimpleMovingAverage,
    DecimalIndicatorValue,
)
from StockSharp.Algo.Strategies import Strategy

class liquid_pulse_strategy(Strategy):
    """
    Liquid Pulse strategy.
    Detects high volume spikes confirmed by MACD and ADX.
    ATR defines stop and take profit; limits trades per day.
    """

    def __init__(self):
        super(liquid_pulse_strategy, self).__init__()
        # 0=Low, 1=Medium, 2=High
        self._volume_sensitivity = self.Param("VolumeSensitivity", 1) \
            .SetDisplay("Volume Sensitivity", "Volume sensitivity", "General")
        # 0=Fast, 1=Medium, 2=Slow
        self._macd_speed = self.Param("MacdSpeed", 1) \
            .SetDisplay("MACD Speed", "MACD speed", "General")
        self._daily_trade_limit = self.Param("DailyTradeLimit", 20) \
            .SetDisplay("Daily Trade Limit", "Max trades per day", "Risk")
        self._adx_trend_threshold = self.Param("AdxTrendThreshold", 20) \
            .SetDisplay("ADX Trend Threshold", "Trend threshold", "Indicators")
        self._atr_period = self.Param("AtrPeriod", 9) \
            .SetDisplay("ATR Period", "ATR period", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe", "General")

        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._entry_price = 0.0
        self._stop = 0.0
        self._tp = 0.0
        self._day = None
        self._daily_trades = 0
        self._vol_lookback = 0
        self._vol_threshold = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(liquid_pulse_strategy, self).OnReseted()
        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._entry_price = 0.0
        self._stop = 0.0
        self._tp = 0.0
        self._day = None
        self._daily_trades = 0
        self._vol_lookback = 0
        self._vol_threshold = 0.0
        self._macd = None
        self._adx = None
        self._atr = None
        self._vol_sma = None

    def OnStarted(self, time):
        super(liquid_pulse_strategy, self).OnStarted(time)

        self._prev_macd = 0.0
        self._prev_signal = 0.0
        self._entry_price = 0.0
        self._stop = 0.0
        self._tp = 0.0
        self._day = None
        self._daily_trades = 0

        vol_sens = self._volume_sensitivity.Value
        if vol_sens == 0:  # Low
            self._vol_lookback = 30
            self._vol_threshold = 1.5
        elif vol_sens == 1:  # Medium
            self._vol_lookback = 20
            self._vol_threshold = 1.2
        else:  # High
            self._vol_lookback = 11
            self._vol_threshold = 1.0

        self._vol_sma = SimpleMovingAverage()
        self._vol_sma.Length = self._vol_lookback

        macd_spd = self._macd_speed.Value
        if macd_spd == 0:  # Fast
            fast_len, slow_len, sig_len = 2, 7, 5
        elif macd_spd == 1:  # Medium
            fast_len, slow_len, sig_len = 5, 13, 9
        else:  # Slow
            fast_len, slow_len, sig_len = 12, 26, 9

        self._macd = MovingAverageConvergenceDivergenceSignal()
        self._macd.Macd.ShortMa.Length = fast_len
        self._macd.Macd.LongMa.Length = slow_len
        self._macd.SignalMa.Length = sig_len

        self._adx = AverageDirectionalIndex()
        self._adx.Length = 14

        self._atr = AverageTrueRange()
        self._atr.Length = self._atr_period.Value

        sub = self.SubscribeCandles(self.candle_type)
        sub.BindEx(self._macd, self._adx, self._atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, macd_val, adx_val, atr_val):
        if candle.State != CandleStates.Finished:
            return
        if not macd_val.IsFinal or not adx_val.IsFinal or not atr_val.IsFinal:
            return

        day = candle.OpenTime.Date
        if self._day is None or self._day != day:
            self._day = day
            self._daily_trades = 0

        # Volume spike detection
        vol_input = DecimalIndicatorValue(self._vol_sma, candle.TotalVolume, candle.ServerTime)
        vol_input.IsFinal = True
        avg_vol_result = self._vol_sma.Process(vol_input)
        avg_vol = 0.0 if avg_vol_result.IsEmpty else float(avg_vol_result)
        high_vol = avg_vol > 0 and float(candle.TotalVolume) >= self._vol_threshold * avg_vol

        # MACD values
        macd_typed = macd_val
        macd_m = macd_typed.Macd
        signal_s = macd_typed.Signal
        if macd_m is None or signal_s is None:
            return
        macd_m = float(macd_m)
        signal_s = float(signal_s)

        # ADX values
        adx_typed = adx_val
        adx_ma = adx_typed.MovingAverage
        if adx_ma is None:
            return
        adx_ma = float(adx_ma)

        dx_val = adx_typed.Dx
        if dx_val is None:
            return
        plus_di = dx_val.Plus
        minus_di = dx_val.Minus
        if plus_di is None or minus_di is None:
            return
        plus_di = float(plus_di)
        minus_di = float(minus_di)

        atr = 0.0 if atr_val.IsEmpty else float(atr_val)

        adx_threshold = self._adx_trend_threshold.Value
        bull = self._prev_macd <= self._prev_signal and macd_m > signal_s and plus_di > minus_di and adx_ma >= adx_threshold
        bear = self._prev_macd >= self._prev_signal and macd_m < signal_s and minus_di > plus_di and adx_ma >= adx_threshold

        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Check stops/TP for existing position
        if self.Position > 0 and self._stop > 0 and (low <= self._stop or high >= self._tp):
            self.SellMarket(abs(self.Position))
            self._entry_price = 0.0
            self._stop = 0.0
            self._tp = 0.0
        elif self.Position < 0 and self._stop > 0 and (high >= self._stop or low <= self._tp):
            self.BuyMarket(abs(self.Position))
            self._entry_price = 0.0
            self._stop = 0.0
            self._tp = 0.0

        if high_vol and self._daily_trades < self._daily_trade_limit.Value and atr > 0:
            if bull and self.Position <= 0:
                self.BuyMarket(self.Volume + abs(self.Position))
                self._entry_price = close
                self._stop = self._entry_price - atr * 1.5
                self._tp = self._entry_price + atr * 2.0
                self._daily_trades += 1
            elif bear and self.Position >= 0:
                self.SellMarket(self.Volume + abs(self.Position))
                self._entry_price = close
                self._stop = self._entry_price + atr * 1.5
                self._tp = self._entry_price - atr * 2.0
                self._daily_trades += 1

        self._prev_macd = macd_m
        self._prev_signal = signal_s

    def CreateClone(self):
        return liquid_pulse_strategy()
