import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class dark_pool_prints_strategy(Strategy):
    """
    Dark Pool Prints strategy.
    Detects unusually high volume candles and trades in the direction of the candle
    when confirmed by SMA trend direction.
    Uses cooldown to control trade frequency.
    """

    def __init__(self):
        super(dark_pool_prints_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Period for trend SMA", "Indicators")
        self._volume_lookback = self.Param("VolumeLookback", 20).SetDisplay("Volume Lookback", "Bars for volume average", "Volume")
        self._volume_multiplier = self.Param("VolumeMultiplier", 1.5).SetDisplay("Volume Multiplier", "Threshold multiplier for high volume", "Volume")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._volume_history = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dark_pool_prints_strategy, self).OnReseted()
        self._volume_history = []
        self._cooldown = 0

    def OnStarted(self, time):
        super(dark_pool_prints_strategy, self).OnStarted(time)

        self._volume_history = []
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        lookback = self._volume_lookback.Value

        # Track volume history
        self._volume_history.append(float(candle.TotalVolume))
        if len(self._volume_history) > lookback:
            self._volume_history.pop(0)

        if len(self._volume_history) < lookback:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Calculate average volume (excluding last entry)
        avg_volume = sum(self._volume_history[:-1]) / (len(self._volume_history) - 1)

        is_high_volume = float(candle.TotalVolume) > avg_volume * self._volume_multiplier.Value
        if not is_high_volume:
            return

        sv = float(sma_val)
        cd = self._cooldown_bars.Value
        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice
        is_above_sma = float(candle.ClosePrice) > sv
        is_below_sma = float(candle.ClosePrice) < sv

        if self.Position == 0 and is_bullish and is_above_sma:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and is_bearish and is_below_sma:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and is_below_sma:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and is_above_sma:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return dark_pool_prints_strategy()
