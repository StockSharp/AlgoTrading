import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import VolumeWeightedMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class vwap_bounce_strategy(Strategy):
    """
    VWAP Bounce strategy.
    Enters long when price bounces off VWAP from below with a bullish candle.
    Enters short when price bounces off VWAP from above with a bearish candle.
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(vwap_bounce_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_close = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(vwap_bounce_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(vwap_bounce_strategy, self).OnStarted2(time)

        self._prev_close = 0.0
        self._cooldown = 0

        vwma = VolumeWeightedMovingAverage()
        vwma.Length = 20

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(vwma, sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, vwma)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, vwma_val, sma_val):
        if candle.State != CandleStates.Finished:
            return

        close = float(candle.ClosePrice)
        vv = float(vwma_val)
        sv = float(sma_val)

        if self._prev_close == 0:
            self._prev_close = close
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            return

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        cd = self._cooldown_bars.Value

        # Bounce off VWAP from below (bullish): prev close was below VWAP, now above or near, bullish candle
        bounced_up = self._prev_close < vv and close >= vv and is_bullish
        # Bounce off VWAP from above (bearish): prev close was above VWAP, now below or near, bearish candle
        bounced_down = self._prev_close > vv and close <= vv and is_bearish

        if self.Position == 0 and bounced_up:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bounced_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_close = close

    def CreateClone(self):
        return vwap_bounce_strategy()
