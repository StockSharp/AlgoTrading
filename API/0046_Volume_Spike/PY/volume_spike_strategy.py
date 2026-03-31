import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_spike_strategy(Strategy):
    """
    Volume Spike strategy.
    Enters when volume spikes above average and price is above/below MA.
    """

    def __init__(self):
        super(volume_spike_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._volume_spike_multiplier = self.Param("VolumeSpikeMultiplier", 2.0).SetDisplay("Volume Spike Multiplier", "Minimum volume increase for signal", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._previous_volume = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_spike_strategy, self).OnReseted()
        self._previous_volume = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volume_spike_strategy, self).OnStarted2(time)

        self._previous_volume = 0.0
        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        vol = float(candle.TotalVolume)

        if self._previous_volume == 0:
            self._previous_volume = vol
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_volume = vol
            return

        volume_change = vol / self._previous_volume if self._previous_volume > 0 else 0.0
        close = float(candle.ClosePrice)
        mv = float(ma_val)
        mult = float(self._volume_spike_multiplier.Value)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and volume_change >= mult:
            if close > mv:
                self.BuyMarket()
                self._cooldown = cd
            elif close < mv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and vol < self._previous_volume:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and vol < self._previous_volume:
            self.BuyMarket()
            self._cooldown = cd

        self._previous_volume = vol

    def CreateClone(self):
        return volume_spike_strategy()
