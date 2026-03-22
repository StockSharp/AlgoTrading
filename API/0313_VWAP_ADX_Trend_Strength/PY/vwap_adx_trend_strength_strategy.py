import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex, VolumeWeightedMovingAverage, CandleIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class vwap_adx_trend_strength_strategy(Strategy):
    """
    Trend-following strategy that trades only when ADX confirms strong directional pressure around VWAP.
    """

    def __init__(self):
        super(vwap_adx_trend_strength_strategy, self).__init__()

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetRange(2, 100) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")

        self._adx_threshold = self.Param("AdxThreshold", 23.0) \
            .SetRange(1.0, 100.0) \
            .SetDisplay("ADX Threshold", "Threshold for strong trend identification", "Signals")

        self._cooldown_bars = self.Param("CooldownBars", 72) \
            .SetRange(1, 500) \
            .SetDisplay("Cooldown Bars", "Bars to wait after each order", "Risk")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetRange(0.5, 10.0) \
            .SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_adx_trend_strength_strategy, self).OnReseted()
        self._cooldown = 0

    def OnStarted(self, time):
        super(vwap_adx_trend_strength_strategy, self).OnStarted(time)

        adx = AverageDirectionalIndex()
        adx.Length = int(self._adx_period.Value)
        self._vwap = VolumeWeightedMovingAverage()
        self._cooldown = 0

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

        self.StartProtection(
            Unit(0, UnitTypes.Absolute),
            Unit(self._stop_loss_percent.Value, UnitTypes.Percent),
            False
        )

    def _process_candle(self, candle, adx_value):
        if candle.State != CandleStates.Finished:
            return

        adx_typed = adx_value

        adx_ma = adx_typed.MovingAverage
        di_plus = adx_typed.Dx.Plus
        di_minus = adx_typed.Dx.Minus

        if adx_ma is None or di_plus is None or di_minus is None:
            return

        adx_val = float(adx_ma)
        di_plus_val = float(di_plus)
        di_minus_val = float(di_minus)

        vwap_result = self._vwap.Process(CandleIndicatorValue(self._vwap, candle))
        vwap_val = float(vwap_result)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if not self._vwap.IsFormed:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        price = float(candle.ClosePrice)
        threshold = float(self._adx_threshold.Value)
        is_strong_trend = adx_val >= threshold
        bullish_trend = di_plus_val > di_minus_val
        bearish_trend = di_minus_val > di_plus_val
        above_vwap = price > vwap_val
        below_vwap = price < vwap_val

        cd = int(self._cooldown_bars.Value)

        if self.Position == 0:
            if is_strong_trend and bullish_trend and above_vwap:
                self.BuyMarket()
                self._cooldown = cd
            elif is_strong_trend and bearish_trend and below_vwap:
                self.SellMarket()
                self._cooldown = cd
            return

        if self.Position > 0 and (not above_vwap or adx_val < threshold * 0.8 or bearish_trend):
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown = cd
        elif self.Position < 0 and (not below_vwap or adx_val < threshold * 0.8 or bullish_trend):
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown = cd

    def CreateClone(self):
        return vwap_adx_trend_strength_strategy()
