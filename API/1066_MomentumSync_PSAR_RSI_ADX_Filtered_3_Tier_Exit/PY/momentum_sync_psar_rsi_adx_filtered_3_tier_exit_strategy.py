import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar, RelativeStrengthIndex, AverageDirectionalIndex
from StockSharp.Algo.Strategies import Strategy

class momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy(Strategy):
    """
    Parabolic SAR bullish flip filtered by RSI and ADX.
    Closes position 3 bars after a bearish SAR flip.
    """

    def __init__(self):
        super(momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy, self).__init__()
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

    def OnStarted(self, time):
        super(momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy, self).OnStarted(time)
        psar = ParabolicSar()
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
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        psar = float(psar_value.ToDecimal())
        rsi = float(rsi_value.ToDecimal())
        adx_typed = adx_value
        ma_val = adx_typed.MovingAverage
        if ma_val is None:
            return
        adx = float(ma_val)
        close = float(candle.ClosePrice)
        psar_bullish_flip = psar < close and self._psar_above_prev1 and self._psar_above_prev2
        psar_bearish_flip = psar > close and not self._psar_above_prev1 and not self._psar_above_prev2
        rsi_adx_ok = rsi > float(self._rsi_threshold.Value) and adx > float(self._adx_threshold.Value)
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
        self._psar_above_prev1 = psar > close

    def CreateClone(self):
        return momentum_sync_psar_rsi_adx_filtered_3_tier_exit_strategy()
