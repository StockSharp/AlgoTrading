import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, MovingAverageConvergenceDivergenceSignal, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class imacd_sniper_strategy(Strategy):
    """
    IMACD Sniper strategy.
    A long opens when the MACD line crosses above its signal line with the close above the EMA, the gap between the lines above MacdDeltaMin,
    both lines at least MacdZeroLimit away from zero, the candle volume above its RangeLength average and a strong bullish candle
    (body at least half of its range). Shorts mirror these rules. An opposite MACD cross closes the position, and the take profit and
    stop loss sit RangeMultiplierTp and RangeMultiplierSl average candle ranges away from the entry.
    """

    def __init__(self):
        super(imacd_sniper_strategy, self).__init__()
        self._fast_length = self.Param("FastLength", 12).SetGreaterThanZero().SetDisplay("MACD Fast Length", "MACD fast period", "MACD")
        self._slow_length = self.Param("SlowLength", 26).SetGreaterThanZero().SetDisplay("MACD Slow Length", "MACD slow period", "MACD")
        self._signal_length = self.Param("SignalLength", 9).SetGreaterThanZero().SetDisplay("MACD Signal Length", "MACD signal smoothing", "MACD")
        self._macd_delta_min = self.Param("MacdDeltaMin", 0.03).SetNotNegative().SetDisplay("Min MACD Delta", "Minimum distance between MACD and signal for entry", "Filters")
        self._macd_zero_limit = self.Param("MacdZeroLimit", 0.05).SetNotNegative().SetDisplay("MACD Zero Limit", "Minimum distance of both lines from zero for entry", "Filters")
        self._range_length = self.Param("RangeLength", 14).SetGreaterThanZero().SetDisplay("Range Length", "Candles for the average range and volume", "Risk")
        self._range_multiplier_tp = self.Param("RangeMultiplierTp", 4.0).SetNotNegative().SetDisplay("Range Multiplier TP", "Take profit in average ranges", "Risk")
        self._range_multiplier_sl = self.Param("RangeMultiplierSl", 1.5).SetNotNegative().SetDisplay("Range Multiplier SL", "Stop loss in average ranges", "Risk")
        self._ema_length = self.Param("EmaLength", 20).SetGreaterThanZero().SetDisplay("EMA Length", "Trend EMA period", "Trend")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles", "General")
        self._range_sma = None
        self._volume_sma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_macd = None
        self._prev_signal = None
        self._take_price = None
        self._stop_price = None

    def OnReseted(self):
        super(imacd_sniper_strategy, self).OnReseted()
        self._range_sma = None
        self._volume_sma = None
        self._reset_state()

    def OnStarted2(self, time):
        super(imacd_sniper_strategy, self).OnStarted2(time)

        self._reset_state()

        self._range_sma = SimpleMovingAverage()
        self._range_sma.Length = self._range_length.Value
        self._volume_sma = SimpleMovingAverage()
        self._volume_sma.Length = self._range_length.Value

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value

        macd = MovingAverageConvergenceDivergenceSignal()
        macd.Macd.ShortMa.Length = self._fast_length.Value
        macd.Macd.LongMa.Length = self._slow_length.Value
        macd.SignalMa.Length = self._signal_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(macd, ema, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)
            macd_area = self.CreateChartArea()
            if macd_area is not None:
                self.DrawIndicator(macd_area, macd)

    def _process_candle(self, candle, macd_value, ema_value):
        if candle.State != CandleStates.Finished:
            return

        candle_range = candle.HighPrice - candle.LowPrice
        range_value = process_value(self._range_sma, candle_range, candle.OpenTime, True)
        volume_value = process_value(self._volume_sma, candle.TotalVolume, candle.OpenTime, True)

        if not macd_value.IsFormed or macd_value.Macd is None or macd_value.Signal is None:
            return

        macd = macd_value.Macd
        signal = macd_value.Signal

        prev_macd = self._prev_macd
        prev_signal = self._prev_signal
        self._prev_macd = macd
        self._prev_signal = signal

        if self._check_protection(candle):
            return

        if prev_macd is None or prev_signal is None:
            return

        if not ema_value.IsFormed or not self._range_sma.IsFormed or not self._volume_sma.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        ema = ema_value.GetValue[Decimal](None)
        avg_range = to_decimal(range_value)
        avg_volume = to_decimal(volume_value)
        close = candle.ClosePrice
        open_price = candle.OpenPrice

        cross_up = prev_macd <= prev_signal and macd > signal
        cross_down = prev_macd >= prev_signal and macd < signal

        delta_ok = abs(macd - signal) > Decimal(self._macd_delta_min.Value)
        zero_limit = Decimal(self._macd_zero_limit.Value)
        far_from_zero = abs(macd) > zero_limit and abs(signal) > zero_limit
        volume_ok = candle.TotalVolume > avg_volume
        body = abs(close - open_price)
        strong_body = candle_range > 0 and body * 2 >= candle_range

        long_entry = cross_up and close > ema and delta_ok and far_from_zero and volume_ok and strong_body and close > open_price
        short_entry = cross_down and close < ema and delta_ok and far_from_zero and volume_ok and strong_body and close < open_price

        if long_entry and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(close, avg_range, True)
        elif short_entry and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(close, avg_range, False)
        elif self.Position > 0 and cross_down:
            self.SellMarket(self.Position)
            self._clear_levels()
        elif self.Position < 0 and cross_up:
            self.BuyMarket(-self.Position)
            self._clear_levels()

    def _set_levels(self, entry, avg_range, is_long):
        take = avg_range * Decimal(self._range_multiplier_tp.Value)
        stop = avg_range * Decimal(self._range_multiplier_sl.Value)
        if is_long:
            self._take_price = entry + take if take > 0 else None
            self._stop_price = entry - stop if stop > 0 else None
        else:
            self._take_price = entry - take if take > 0 else None
            self._stop_price = entry + stop if stop > 0 else None

    def _clear_levels(self):
        self._take_price = None
        self._stop_price = None

    # Returns True when the take profit or stop loss closed the position on this candle.
    def _check_protection(self, candle):
        if self.Position == 0:
            self._clear_levels()
            return False

        sl = self._stop_price
        tp = self._take_price

        if self.Position > 0:
            if (sl is not None and candle.LowPrice <= sl) or (tp is not None and candle.HighPrice >= tp):
                self.SellMarket(self.Position)
                self._clear_levels()
                return True
        else:
            if (sl is not None and candle.HighPrice >= sl) or (tp is not None and candle.LowPrice <= tp):
                self.BuyMarket(-self.Position)
                self._clear_levels()
                return True

        return False

    def CreateClone(self):
        return imacd_sniper_strategy()
