import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, MovingAverageConvergenceDivergenceSignal, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *


class scalping_ema_rsi_macd_strategy(Strategy):
    """
    Scalping EMA RSI MACD Strategy.
    A long opens when the fast EMA crosses above the slow EMA with the close above the trend EMA, RSI between RsiOversold and
    RsiOverbought, the MACD line above its signal and volume above VolumeThreshold times its VolumeMaLength average; a short
    mirrors this. The stop is AtrMultiplier ATRs from the entry and the target RiskReward times that distance. An opposite
    signal reverses the position.
    """

    def __init__(self):
        super(scalping_ema_rsi_macd_strategy, self).__init__()
        self._fast_ema_length = self.Param("FastEmaLength", 12).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
        self._slow_ema_length = self.Param("SlowEmaLength", 26).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
        self._trend_ema_length = self.Param("TrendEmaLength", 55).SetGreaterThanZero().SetDisplay("Trend EMA", "Trend EMA period", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 65.0).SetDisplay("RSI Overbought", "Upper RSI bound for entries", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 35.0).SetDisplay("RSI Oversold", "Lower RSI bound for entries", "Indicators")
        self._macd_fast = self.Param("MacdFast", 12).SetGreaterThanZero().SetDisplay("MACD Fast", "MACD fast EMA period", "MACD")
        self._macd_slow = self.Param("MacdSlow", 26).SetGreaterThanZero().SetDisplay("MACD Slow", "MACD slow EMA period", "MACD")
        self._macd_signal = self.Param("MacdSignal", 9).SetGreaterThanZero().SetDisplay("MACD Signal", "MACD signal line period", "MACD")
        self._atr_length = self.Param("AtrLength", 14).SetGreaterThanZero().SetDisplay("ATR Length", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 2.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "ATR multiplier of the stop distance", "Risk")
        self._risk_reward = self.Param("RiskReward", 2.0).SetGreaterThanZero().SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk")
        self._volume_ma_length = self.Param("VolumeMaLength", 20).SetGreaterThanZero().SetDisplay("Volume MA Length", "Period of the volume average", "Volume")
        self._volume_threshold = self.Param("VolumeThreshold", 1.3).SetGreaterThanZero().SetDisplay("Volume Threshold", "Volume multiple of its average required for entries", "Volume")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_ma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._stop_price = None
        self._target_price = None

    def OnReseted(self):
        super(scalping_ema_rsi_macd_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(scalping_ema_rsi_macd_strategy, self).OnStarted2(time)

        self._reset_state()

        fast_ema = ExponentialMovingAverage()
        fast_ema.Length = self._fast_ema_length.Value
        slow_ema = ExponentialMovingAverage()
        slow_ema.Length = self._slow_ema_length.Value
        trend_ema = ExponentialMovingAverage()
        trend_ema.Length = self._trend_ema_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._macd_fast.Value
        macd.Macd.LongMa.Length = self._macd_slow.Value
        macd.SignalMa.Length = self._macd_signal.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self._volume_ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast_ema, slow_ema, trend_ema, rsi, macd, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast_ema)
            self.DrawIndicator(area, slow_ema)
            self.DrawIndicator(area, trend_ema)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, macd)

    def _process_candle(self, candle, fast_value, slow_value, trend_value, rsi_value, macd_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        volume_average = float(process_float(self._volume_ma, candle.TotalVolume, candle.OpenTime, True).GetValue[Decimal](None))

        if not fast_value.IsFormed or not slow_value.IsFormed:
            return

        fast = float(fast_value.GetValue[Decimal](None))
        slow = float(slow_value.GetValue[Decimal](None))
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        # Protective exits are checked first: the stop or the target of the open position.
        if self.Position > 0 and ((self._stop_price is not None and low <= self._stop_price) or (self._target_price is not None and high >= self._target_price)):
            self.SellMarket(self.Position)
            self._stop_price = None
            self._target_price = None
            return

        if self.Position < 0 and ((self._stop_price is not None and high >= self._stop_price) or (self._target_price is not None and low <= self._target_price)):
            self.BuyMarket(-self.Position)
            self._stop_price = None
            self._target_price = None
            return

        if prev_fast is None or prev_slow is None or not self._volume_ma.IsFormed:
            return

        if not trend_value.IsFormed or not rsi_value.IsFormed or not atr_value.IsFormed or not macd_value.IsFormed:
            return

        if macd_value.Macd is None or macd_value.Signal is None:
            return

        macd_line = float(macd_value.Macd)
        signal_line = float(macd_value.Signal)
        trend = float(trend_value.GetValue[Decimal](None))
        rsi = float(rsi_value.GetValue[Decimal](None))
        atr = float(atr_value.GetValue[Decimal](None))
        close = float(candle.ClosePrice)

        rsi_in_bounds = float(self._rsi_oversold.Value) < rsi < float(self._rsi_overbought.Value)
        high_volume = float(candle.TotalVolume) > volume_average * float(self._volume_threshold.Value)
        stop_distance = atr * float(self._atr_multiplier.Value)
        risk_reward = float(self._risk_reward.Value)

        long_signal = prev_fast <= prev_slow and fast > slow and close > trend and rsi_in_bounds and macd_line > signal_line and high_volume
        short_signal = prev_fast >= prev_slow and fast < slow and close < trend and rsi_in_bounds and macd_line < signal_line and high_volume

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._stop_price = close - stop_distance
            self._target_price = close + stop_distance * risk_reward
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._stop_price = close + stop_distance
            self._target_price = close - stop_distance * risk_reward

    def CreateClone(self):
        return scalping_ema_rsi_macd_strategy()
