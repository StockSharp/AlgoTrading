import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ParabolicSar
from StockSharp.Algo.Strategies import Strategy

class parabolic_sar_reversal_strategy(Strategy):
    """
    Parabolic SAR Reversal strategy.
    Enters long when SAR switches from above to below price.
    Enters short when SAR switches from below to above price.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(parabolic_sar_reversal_strategy, self).__init__()
        self._acceleration = self.Param("Acceleration", 0.02).SetDisplay("Acceleration", "Initial acceleration factor", "SAR")
        self._acceleration_max = self.Param("AccelerationMax", 0.2).SetDisplay("Max Acceleration", "Maximum acceleration factor", "SAR")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_sar_above = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(parabolic_sar_reversal_strategy, self).OnReseted()
        self._prev_sar_above = None
        self._cooldown = 0

    def OnStarted2(self, time):
        super(parabolic_sar_reversal_strategy, self).OnStarted2(time)

        self._prev_sar_above = None
        self._cooldown = 0

        sar = ParabolicSar()
        sar.Acceleration = self._acceleration.Value
        sar.AccelerationMax = self._acceleration_max.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sar, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sar)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sar_val):
        if candle.State != CandleStates.Finished:
            return

        sv = float(sar_val)
        close = float(candle.ClosePrice)
        is_sar_above = sv > close

        if self._prev_sar_above is None:
            self._prev_sar_above = is_sar_above
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_sar_above = is_sar_above
            return

        cd = self._cooldown_bars.Value

        # SAR switched from above to below = bullish signal
        sar_switched_below = self._prev_sar_above == True and not is_sar_above
        # SAR switched from below to above = bearish signal
        sar_switched_above = self._prev_sar_above == False and is_sar_above

        if self.Position == 0 and sar_switched_below:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and sar_switched_above:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and sar_switched_above:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and sar_switched_below:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_sar_above = is_sar_above

    def CreateClone(self):
        return parabolic_sar_reversal_strategy()
