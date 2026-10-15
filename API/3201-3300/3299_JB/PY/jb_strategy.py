import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Sides
from StockSharp.Algo.Strategies import Strategy


class jb_strategy(Strategy):
    def __init__(self):
        super(jb_strategy, self).__init__()

        self._sma_period = self.Param("SmaPeriod", 100).SetGreaterThanZero()
        self._force_period = self.Param("ForcePeriod", 100).SetGreaterThanZero()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetGreaterThanZero()
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetGreaterThanZero()
        self._base_volume = self.Param("BaseVolume", 0.1).SetGreaterThanZero()
        self._loss_multiplier = self.Param("LossMultiplier", 1.55).SetGreaterThanZero()
        self._average_profit_target = self.Param("AverageProfitTarget", 2.8).SetNotNegative()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1)))

        self._closes = []
        self._force_ema = None
        self._last_close = None
        self._prev_close = None
        self._prev_sma = None
        self._prev_force = None
        self._prev_lower = None
        self._prev_upper = None
        self._next_volume = 0.1
        self._cycle_realized_start = 0.0
        self._cycle_active = False

    def GetWorkingSecurities(self):
        return [(self.Security, self._candle_type.Value)]

    def OnReseted(self):
        super(jb_strategy, self).OnReseted()
        self._closes = []
        self._force_ema = None
        self._last_close = None
        self._prev_close = None
        self._prev_sma = None
        self._prev_force = None
        self._prev_lower = None
        self._prev_upper = None
        self._next_volume = float(self._base_volume.Value)
        self._cycle_realized_start = 0.0
        self._cycle_active = False

    def OnStarted2(self, time):
        super(jb_strategy, self).OnStarted2(time)
        self._next_volume = self._normalize_volume(float(self._base_volume.Value))
        self.SubscribeCandles(self._candle_type.Value).Bind(self._process_candle).Start()

    def _process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        if self.Position != 0:
            abs_position = abs(float(self.Position))
            per_contract = float(self.PnLManager.UnrealizedPnL) / abs_position if abs_position > 0 else 0.0
            if per_contract >= float(self._average_profit_target.Value):
                if self.Position > 0:
                    self.SellMarket(Math.Abs(self.Position))
                else:
                    self.BuyMarket(Math.Abs(self.Position))
                self._update_indicators(candle)
                return
        elif all(v is not None for v in [self._prev_close, self._prev_sma, self._prev_force, self._prev_lower, self._prev_upper]):
            signal = self.get_signal(self._prev_close, self._prev_sma, self._prev_force, self._prev_lower, self._prev_upper)
            if signal > 0:
                self._enter(Sides.Buy)
            elif signal < 0:
                self._enter(Sides.Sell)

        self._update_indicators(candle)

    def _update_indicators(self, candle):
        close = float(candle.ClosePrice)
        if self._last_close is not None:
            raw_force = (close - self._last_close) * float(candle.TotalVolume)
            self._force_ema = self._ema(self._force_ema, raw_force, int(self._force_period.Value))

        self._last_close = close
        self._closes.append(close)

        keep = max(int(self._sma_period.Value), int(self._bollinger_period.Value))
        if len(self._closes) > keep:
            del self._closes[:-keep]

        sma_period = int(self._sma_period.Value)
        bb_period = int(self._bollinger_period.Value)
        if len(self._closes) < sma_period or len(self._closes) < bb_period or self._force_ema is None:
            return

        sma = sum(self._closes[-sma_period:]) / float(sma_period)
        bb = self._closes[-bb_period:]
        mean = sum(bb) / float(bb_period)
        variance = sum((v - mean) ** 2 for v in bb) / float(bb_period)
        std = math.sqrt(variance)

        self._prev_close = close
        self._prev_sma = sma
        self._prev_force = self._force_ema
        self._prev_lower = mean - float(self._bollinger_deviation.Value) * std
        self._prev_upper = mean + float(self._bollinger_deviation.Value) * std

    def _enter(self, side):
        volume = self._normalize_volume(self._next_volume)
        if volume <= 0:
            return

        self._cycle_realized_start = float(self.PnLManager.RealizedPnL)
        self._cycle_active = True

        if side == Sides.Buy:
            self.BuyMarket(volume)
        else:
            self.SellMarket(volume)

    def OnOwnTradeReceived(self, trade):
        super(jb_strategy, self).OnOwnTradeReceived(trade)

        if not self._cycle_active or self.Position != 0:
            return

        cycle_pnl = float(self.PnLManager.RealizedPnL) - self._cycle_realized_start
        if cycle_pnl < 0:
            self._next_volume = self._normalize_volume(self._next_volume * float(self._loss_multiplier.Value))
        else:
            self._next_volume = self._normalize_volume(float(self._base_volume.Value))
        self._cycle_active = False

    @staticmethod
    def get_signal(previous_close, sma, force, lower_band, upper_band):
        if previous_close <= lower_band and previous_close > sma and force > 0:
            return 1
        if previous_close >= upper_band and previous_close < sma and force < 0:
            return -1
        return 0

    def _normalize_volume(self, volume):
        if self.Security is not None:
            if self.Security.MaxVolume is not None and float(self.Security.MaxVolume) > 0:
                volume = min(volume, float(self.Security.MaxVolume))
            if self.Security.MinVolume is not None and float(self.Security.MinVolume) > 0:
                volume = max(volume, float(self.Security.MinVolume))
            if self.Security.VolumeStep is not None and float(self.Security.VolumeStep) > 0:
                step = float(self.Security.VolumeStep)
                volume = math.floor(volume / step) * step
        return volume

    @staticmethod
    def _ema(previous, value, period):
        if previous is None:
            return value
        alpha = 2.0 / (period + 1.0)
        return previous + alpha * (value - previous)

    def CreateClone(self):
        return jb_strategy()
