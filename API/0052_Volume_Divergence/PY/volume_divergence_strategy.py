import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_divergence_strategy(Strategy):
    """
    Volume Divergence strategy.
    Long entry: Price falls but volume increases (possible accumulation).
    Short entry: Price rises but volume increases (possible distribution).
    Exit: Price crosses MA.
    """

    def __init__(self):
        super(volume_divergence_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._previous_close = 0.0
        self._previous_volume = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_divergence_strategy, self).OnReseted()
        self._previous_close = 0.0
        self._previous_volume = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(volume_divergence_strategy, self).OnStarted(time)

        self._previous_close = 0.0
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
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)
        vol = float(candle.TotalVolume)

        if self._previous_close == 0:
            self._previous_close = close
            self._previous_volume = vol
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_close = close
            self._previous_volume = vol
            return

        mv = float(ma_val)
        cd = self._cooldown_bars.Value

        price_down = close < self._previous_close
        price_up = close > self._previous_close
        volume_up = vol > self._previous_volume

        bullish_divergence = price_down and volume_up
        bearish_divergence = price_up and volume_up

        if self.Position == 0:
            if bullish_divergence and close < mv:
                self.BuyMarket()
                self._cooldown = cd
            elif bearish_divergence and close > mv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and close < mv and not bullish_divergence:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > mv and not bearish_divergence:
            self.BuyMarket()
            self._cooldown = cd

        self._previous_close = close
        self._previous_volume = vol

    def CreateClone(self):
        return volume_divergence_strategy()
