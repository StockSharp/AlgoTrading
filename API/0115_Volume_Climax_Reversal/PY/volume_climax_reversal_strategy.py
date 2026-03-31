import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_climax_reversal_strategy(Strategy):
    """
    Volume Climax Reversal strategy.
    Enters counter-trend when volume spikes above average with MA confirmation.
    Uses cooldown and MA cross for exits.
    """

    def __init__(self):
        super(volume_climax_reversal_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candle timeframe", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._volume_multiplier = self.Param("VolumeMultiplier", 2.0).SetDisplay("Volume Multiplier", "Volume spike threshold", "Volume")
        self._cooldown_bars = self.Param("CooldownBars", 400).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._volumes = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_climax_reversal_strategy, self).OnReseted()
        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._volumes = []
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volume_climax_reversal_strategy, self).OnStarted2(time)

        self._prev_ma = 0.0
        self._prev_close = 0.0
        self._volumes = []
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

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        ma = float(ma_val)
        volume = float(candle.TotalVolume)
        ma_period = self._ma_period.Value
        cd = self._cooldown_bars.Value

        # Track volumes for average calculation
        self._volumes.append(volume)
        if len(self._volumes) > ma_period:
            self._volumes.pop(0)

        if len(self._volumes) < ma_period or self._prev_ma == 0:
            self._prev_ma = ma
            self._prev_close = close
            return

        # Calculate average volume
        avg_volume = sum(self._volumes) / len(self._volumes)

        is_volume_climax = avg_volume > 0 and volume > avg_volume * self._volume_multiplier.Value
        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_ma = ma
            self._prev_close = close
            return

        # Exit logic: MA cross
        if self.Position > 0 and close < ma and self._prev_close >= self._prev_ma:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > ma and self._prev_close <= self._prev_ma:
            self.BuyMarket()
            self._cooldown = cd

        # Entry logic: volume climax reversal
        if self.Position == 0 and is_volume_climax:
            # Bullish reversal: high volume bearish candle below MA (selling climax)
            if is_bearish and close < ma:
                self.BuyMarket()
                self._cooldown = cd
            # Bearish reversal: high volume bullish candle above MA (buying climax)
            elif is_bullish and close > ma:
                self.SellMarket()
                self._cooldown = cd

        self._prev_ma = ma
        self._prev_close = close

    def CreateClone(self):
        return volume_climax_reversal_strategy()
