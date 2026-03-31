import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_exhaustion_strategy(Strategy):
    """
    Volume Exhaustion strategy.
    Looks for volume spikes (current volume much higher than previous) with directional candles.
    High volume + bullish above SMA = buy.
    High volume + bearish below SMA = sell.
    Exits when price crosses SMA in opposite direction.
    """

    def __init__(self):
        super(volume_exhaustion_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_volume = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_exhaustion_strategy, self).OnReseted()
        self._prev_volume = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volume_exhaustion_strategy, self).OnStarted2(time)

        self._prev_volume = 0.0
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
            self._prev_volume = float(candle.TotalVolume)
            return

        vol = float(candle.TotalVolume)

        if self._prev_volume == 0:
            self._prev_volume = vol
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_volume = vol
            return

        # Volume spike: current volume significantly higher than previous
        volume_spike = self._prev_volume > 0 and vol > self._prev_volume * 1.5

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and volume_spike and is_bullish and close > sv:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and volume_spike and is_bearish and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_volume = vol

    def CreateClone(self):
        return volume_exhaustion_strategy()
