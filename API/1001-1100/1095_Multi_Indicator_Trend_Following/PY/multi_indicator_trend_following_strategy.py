import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, RelativeStrengthIndex, AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

RSI_MIDDLE = 50


class multi_indicator_trend_following_strategy(Strategy):
    """
    Multi indicator trend following strategy.
    Goes long when the fast EMA crosses above the slow EMA with RSI above 50 and volume above VolumeMultiplier times its average,
    and short on the mirrored crossover with RSI below 50, reversing an opposite position. Positions are closed by a stop loss and a
    take profit placed StopLossAtrMultiplier and TakeProfitAtrMultiplier ATRs away from the entry price.
    """

    def __init__(self):
        super(multi_indicator_trend_following_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._fast_ma_length = self.Param("FastMaLength", 10).SetGreaterThanZero().SetDisplay("Fast EMA", "Fast EMA length", "Indicators")
        self._slow_ma_length = self.Param("SlowMaLength", 30).SetGreaterThanZero().SetDisplay("Slow EMA", "Slow EMA length", "Indicators")
        self._rsi_length = self.Param("RsiLength", 14).SetGreaterThanZero().SetDisplay("RSI Length", "RSI period", "Indicators")
        self._volume_ma_length = self.Param("VolumeMaLength", 20).SetGreaterThanZero().SetDisplay("Volume MA Length", "Volume moving average length", "Volume")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5).SetNotNegative().SetDisplay("Volume Multiplier", "Volume must exceed its average times this", "Volume")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._stop_loss_atr_multiplier = self.Param("StopLossAtrMultiplier", 2.0).SetNotNegative().SetDisplay("Stop ATR Mult", "ATR multiple for the stop loss", "Risk")
        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 3.0).SetNotNegative().SetDisplay("Take ATR Mult", "ATR multiple for the take profit", "Risk")
        self._volume_ma = None
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._prev_fast = None
        self._prev_slow = None
        self._stop_price = None
        self._take_price = None

    def OnReseted(self):
        super(multi_indicator_trend_following_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(multi_indicator_trend_following_strategy, self).OnStarted2(time)

        self._reset_state()

        fast = ExponentialMovingAverage()
        fast.Length = self._fast_ma_length.Value
        slow = ExponentialMovingAverage()
        slow.Length = self._slow_ma_length.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value
        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self._volume_ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(fast, slow, rsi, atr, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, fast)
            self.DrawIndicator(area, slow)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, rsi)

    def _process_candle(self, candle, fast_value, slow_value, rsi_value, atr_value):
        if candle.State != CandleStates.Finished:
            return

        volume_average = process_value(self._volume_ma, candle.TotalVolume, candle.OpenTime, True)

        exited = False

        # The ATR levels are checked against the candle range, so exits happen before new signals.
        if self.Position > 0 and self._stop_price is not None and self._take_price is not None:
            if candle.LowPrice <= self._stop_price or candle.HighPrice >= self._take_price:
                self.SellMarket(self.Position)
                self._stop_price = None
                self._take_price = None
                exited = True
        elif self.Position < 0 and self._stop_price is not None and self._take_price is not None:
            if candle.HighPrice >= self._stop_price or candle.LowPrice <= self._take_price:
                self.BuyMarket(-self.Position)
                self._stop_price = None
                self._take_price = None
                exited = True

        if not fast_value.IsFormed or not slow_value.IsFormed or not rsi_value.IsFormed or not atr_value.IsFormed or not self._volume_ma.IsFormed:
            return

        fast = fast_value.GetValue[Decimal](None)
        slow = slow_value.GetValue[Decimal](None)
        prev_fast = self._prev_fast
        prev_slow = self._prev_slow
        self._prev_fast = fast
        self._prev_slow = slow

        if exited or prev_fast is None or prev_slow is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        rsi = rsi_value.GetValue[Decimal](None)
        atr = atr_value.GetValue[Decimal](None)
        high_volume = candle.TotalVolume > volume_average.GetValue[Decimal](None) * Decimal(self._volume_multiplier.Value)

        cross_up = prev_fast <= prev_slow and fast > slow
        cross_down = prev_fast >= prev_slow and fast < slow

        if cross_up and rsi > Decimal(RSI_MIDDLE) and high_volume and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
            self._set_levels(candle.ClosePrice, atr, True)
        elif cross_down and rsi < Decimal(RSI_MIDDLE) and high_volume and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))
            self._set_levels(candle.ClosePrice, atr, False)

    def _set_levels(self, entry, atr, is_long):
        stop_mult = Decimal(self._stop_loss_atr_multiplier.Value)
        take_mult = Decimal(self._take_profit_atr_multiplier.Value)
        stop = atr * stop_mult
        take = atr * take_mult

        # A zero multiplier disables that level.
        if stop_mult > Decimal(0):
            self._stop_price = entry - stop if is_long else entry + stop
        else:
            self._stop_price = Decimal.MinValue if is_long else Decimal.MaxValue
        if take_mult > Decimal(0):
            self._take_price = entry + take if is_long else entry - take
        else:
            self._take_price = Decimal.MaxValue if is_long else Decimal.MinValue

    def CreateClone(self):
        return multi_indicator_trend_following_strategy()
