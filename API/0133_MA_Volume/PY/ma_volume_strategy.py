import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_volume_strategy(Strategy):
    """
    MA Volume strategy.
    Buys on MA crossover with volume confirmation, sells on reverse crossover.
    """

    def __init__(self):
        super(ma_volume_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Period for moving average calculation", "MA Settings")
        self._volume_threshold = self.Param("VolumeThreshold", 1.2).SetDisplay("Volume Threshold", "Volume threshold multiplier", "Volume Settings")
        self._cooldown_bars = self.Param("CooldownBars", 150).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._prev_volume = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_volume_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._prev_volume = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(ma_volume_strategy, self).OnStarted(time)

        self._prev_close = 0.0
        self._prev_sma = 0.0
        self._has_prev = False
        self._prev_volume = 0.0
        self._cooldown = 0

        price_sma = SimpleMovingAverage()
        price_sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(price_sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, price_sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        sma = float(sma_val)
        vol = float(candle.TotalVolume)
        cd = self._cooldown_bars.Value
        threshold = float(self._volume_threshold.Value)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            self._prev_sma = sma
            self._prev_volume = vol
            self._has_prev = True
            return

        # Volume confirmation: current volume is above threshold * previous volume
        volume_ok = self._prev_volume > 0 and vol > self._prev_volume * threshold

        if self._has_prev:
            # Price crosses above MA with volume - buy
            if self._prev_close <= self._prev_sma and close > sma and volume_ok and self.Position == 0:
                self.BuyMarket()
                self._cooldown = cd
            # Price crosses below MA with volume - sell
            elif self._prev_close >= self._prev_sma and close < sma and volume_ok and self.Position == 0:
                self.SellMarket()
                self._cooldown = cd
            # Exit long on MA cross down
            elif self._prev_close >= self._prev_sma and close < sma and self.Position > 0:
                self.SellMarket()
                self._cooldown = cd
            # Exit short on MA cross up
            elif self._prev_close <= self._prev_sma and close > sma and self.Position < 0:
                self.BuyMarket()
                self._cooldown = cd

        self._prev_close = close
        self._prev_sma = sma
        self._prev_volume = vol
        self._has_prev = True

    def CreateClone(self):
        return ma_volume_strategy()
