import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ParabolicSar, SuperTrend, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy


class multi_indicator_swing_strategy(Strategy):
    """
    Multi indicator swing strategy.
    A long needs the close above Parabolic SAR, an up-trending SuperTrend, ADX above AdxThreshold with +DI above -DI, and a volume
    delta ratio above DeltaThreshold; a short needs all of the mirrored conditions. An opposite signal reverses the position and
    percent stop loss / take profit levels protect it.
    """

    def __init__(self):
        super(multi_indicator_swing_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(2))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._psar_start = self.Param("PsarStart", 0.02).SetGreaterThanZero().SetDisplay("PSAR Start", "Parabolic SAR start acceleration", "PSAR")
        self._psar_increment = self.Param("PsarIncrement", 0.02).SetGreaterThanZero().SetDisplay("PSAR Increment", "Parabolic SAR acceleration increment", "PSAR")
        self._psar_maximum = self.Param("PsarMaximum", 0.2).SetGreaterThanZero().SetDisplay("PSAR Maximum", "Parabolic SAR maximum acceleration", "PSAR")
        self._atr_period = self.Param("AtrPeriod", 10).SetGreaterThanZero().SetDisplay("ATR Period", "SuperTrend ATR period", "SuperTrend")
        self._atr_multiplier = self.Param("AtrMultiplier", 3.0).SetGreaterThanZero().SetDisplay("ATR Multiplier", "SuperTrend ATR multiplier", "SuperTrend")
        self._adx_length = self.Param("AdxLength", 14).SetGreaterThanZero().SetDisplay("ADX Length", "ADX period", "ADX")
        self._adx_threshold = self.Param("AdxThreshold", 25.0).SetNotNegative().SetDisplay("ADX Threshold", "Minimum ADX for a trend", "ADX")
        self._delta_length = self.Param("DeltaLength", 14).SetGreaterThanZero().SetDisplay("Delta Length", "Candles for the typical absolute volume delta", "Volume Delta")
        self._delta_smooth = self.Param("DeltaSmooth", 3).SetGreaterThanZero().SetDisplay("Delta Smooth", "Candles for smoothing the volume delta", "Volume Delta")
        self._delta_threshold = self.Param("DeltaThreshold", 0.5).SetNotNegative().SetDisplay("Delta Threshold", "Minimum normalized volume delta", "Volume Delta")
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0).SetNotNegative().SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 4.0).SetNotNegative().SetDisplay("Take Profit %", "Take profit percentage", "Risk")
        self._deltas = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(multi_indicator_swing_strategy, self).OnReseted()
        self._deltas = []

    def OnStarted2(self, time):
        super(multi_indicator_swing_strategy, self).OnStarted2(time)

        self._deltas = []

        psar = ParabolicSar()
        psar.Acceleration = Decimal(self._psar_start.Value)
        psar.AccelerationStep = Decimal(self._psar_increment.Value)
        psar.AccelerationMax = Decimal(self._psar_maximum.Value)
        supertrend = SuperTrend()
        supertrend.Length = self._atr_period.Value
        supertrend.Multiplier = Decimal(self._atr_multiplier.Value)
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(psar, supertrend, adx, self._process_candle).Start()

        tp = float(self._take_profit_percent.Value)
        sl = float(self._stop_loss_percent.Value)
        self.StartProtection(
            Unit(Decimal(tp), UnitTypes.Percent) if tp > 0 else Unit(),
            Unit(Decimal(sl), UnitTypes.Percent) if sl > 0 else Unit(),
            useMarketOrders=True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, psar)
            self.DrawIndicator(area, supertrend)
            self.DrawOwnTrades(area)
            oscillators = self.CreateChartArea()
            if oscillators is not None:
                self.DrawIndicator(oscillators, adx)

    def _update_delta_ratio(self, candle):
        # Volume delta estimated from where the candle closed inside its range.
        rng = candle.HighPrice - candle.LowPrice
        if rng > Decimal(0):
            delta = candle.TotalVolume * (candle.ClosePrice - candle.OpenPrice) / rng
        else:
            delta = Decimal(0)

        length = self._delta_length.Value
        smooth = self._delta_smooth.Value
        capacity = max(length, smooth)

        self._deltas.append(delta)
        while len(self._deltas) > capacity:
            self._deltas.pop(0)

        if len(self._deltas) < capacity:
            return None

        smoothed = sum(self._deltas[-smooth:], Decimal(0)) / Decimal(smooth)
        typical = sum((abs(d) for d in self._deltas[-length:]), Decimal(0)) / Decimal(length)

        # The smoothed delta is expressed in units of the typical absolute delta.
        return smoothed / typical if typical > Decimal(0) else Decimal(0)

    def _process_candle(self, candle, psar_value, supertrend_value, adx_value):
        if candle.State != CandleStates.Finished:
            return

        ratio = self._update_delta_ratio(candle)

        if not psar_value.IsFormed or not supertrend_value.IsFormed or not adx_value.IsFormed or ratio is None:
            return

        if adx_value.MovingAverage is None or adx_value.Dx.Plus is None or adx_value.Dx.Minus is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        sar = psar_value.GetValue[Decimal](None)
        adx = adx_value.MovingAverage
        plus_di = adx_value.Dx.Plus
        minus_di = adx_value.Dx.Minus
        is_up_trend = supertrend_value.IsUpTrend
        close = candle.ClosePrice
        threshold = Decimal(self._delta_threshold.Value)
        strong_trend = adx > Decimal(self._adx_threshold.Value)

        long_signal = close > sar and is_up_trend and strong_trend and plus_di > minus_di and ratio > threshold
        short_signal = close < sar and not is_up_trend and strong_trend and minus_di > plus_di and ratio < -threshold

        if long_signal and self.Position <= 0:
            self.BuyMarket(self.Volume + abs(self.Position))
        elif short_signal and self.Position >= 0:
            self.SellMarket(self.Volume + abs(self.Position))

    def CreateClone(self):
        return multi_indicator_swing_strategy()
