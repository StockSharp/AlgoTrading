import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex, AverageDirectionalIndex, AverageDirectionalIndexValue
from StockSharp.Algo.Strategies import Strategy

class momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy(Strategy):
    """
    Parabolic SAR bullish flip filtered by RSI and ADX.
    Closes position 3 bars after a bearish SAR flip.
    """

    def __init__(self):
        super(momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy, self).__init__()
        self._sar_start = self.Param("SarStart", 0.02).SetDisplay("SAR Start", "SAR start", "Indicators")
        self._sar_increment = self.Param("SarIncrement", 0.02).SetDisplay("SAR Increment", "SAR increment", "Indicators")
        self._sar_max = self.Param("SarMax", 0.2).SetDisplay("SAR Max", "SAR max", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14).SetDisplay("RSI Period", "RSI period", "Indicators")
        self._adx_period = self.Param("AdxPeriod", 14).SetDisplay("ADX Period", "ADX period", "Indicators")
        self._rsi_threshold = self.Param("RsiThreshold", 40.0).SetDisplay("RSI Threshold", "Min RSI for entry", "Signals")
        self._adx_threshold = self.Param("AdxThreshold", 18.0).SetDisplay("ADX Threshold", "Min ADX for entry", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._psar_above_prev1 = False
        self._psar_above_prev2 = False
        self._bars_since_bearish_flip = -1

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy, self).OnReseted()
        self._psar_above_prev1 = False
        self._psar_above_prev2 = False
        self._bars_since_bearish_flip = -1

    def OnStarted2(self, time):
        super(momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy, self).OnStarted2(time)
        self._psar_above_prev1 = False
        self._psar_above_prev2 = False
        self._bars_since_bearish_flip = -1
        psar = ParabolicSar()
        psar.Acceleration = self._sar_start.Value
        psar.AccelerationStep = self._sar_increment.Value
        psar.AccelerationMax = self._sar_max.Value
        rsi = RelativeStrengthIndex()
        rsi.Length = self._rsi_period.Value
        adx = AverageDirectionalIndex()
        adx.Length = self._adx_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(psar, rsi, adx, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, psar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, psar_value, rsi_value, adx_value):
        if candle.State != CandleStates.Finished:
            return
        psar_v = float(psar_value)
        rsi_v = float(rsi_value)
        if not hasattr(adx_value, 'MovingAverage'):
            return
        ma_val = adx_value.MovingAverage
        if ma_val is None:
            return
        adx_v = float(ma_val)
        close = float(candle.ClosePrice)
        psar_bullish_flip = psar_v < close and self._psar_above_prev1 and self._psar_above_prev2
        psar_bearish_flip = psar_v > close and not self._psar_above_prev1 and not self._psar_above_prev2
        rsi_adx_ok = rsi_v > float(self._rsi_threshold.Value) and adx_v > float(self._adx_threshold.Value)
        if self.Position == 0 and psar_bullish_flip and rsi_adx_ok:
            self.BuyMarket()
        if self.Position > 0:
            if psar_bearish_flip and self._bars_since_bearish_flip < 0:
                self._bars_since_bearish_flip = 0
            elif self._bars_since_bearish_flip >= 0:
                self._bars_since_bearish_flip += 1
            if self._bars_since_bearish_flip == 3:
                self.SellMarket()
                self._bars_since_bearish_flip = -1
        else:
            self._bars_since_bearish_flip = -1
        self._psar_above_prev2 = self._psar_above_prev1
        self._psar_above_prev1 = psar_v > close

    def CreateClone(self):
        return momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy()
