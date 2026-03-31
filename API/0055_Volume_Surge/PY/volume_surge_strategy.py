import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class volume_surge_strategy(Strategy):
    """
    Volume Surge strategy.
    Long: Price crosses above MA with volume confirmation.
    Short: Price crosses below MA with volume confirmation.
    Exit: Price crosses MA.
    """

    def __init__(self):
        super(volume_surge_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for price MA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._prev_volume = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(volume_surge_strategy, self).OnReseted()
        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._prev_volume = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(volume_surge_strategy, self).OnStarted2(time)

        self._prev_close = 0.0
        self._prev_ma = 0.0
        self._prev_volume = 0.0
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

        close = float(candle.ClosePrice)
        mv = float(ma_val)
        vol = float(candle.TotalVolume)

        if self._prev_close == 0:
            self._prev_close = close
            self._prev_ma = mv
            self._prev_volume = vol
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_close = close
            self._prev_ma = mv
            self._prev_volume = vol
            return

        cd = self._cooldown_bars.Value
        cross_up = self._prev_close <= self._prev_ma and close > mv
        cross_down = self._prev_close >= self._prev_ma and close < mv
        volume_rising = vol > self._prev_volume

        if self.Position == 0 and cross_up and volume_rising:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and cross_down and volume_rising:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and cross_down:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and cross_up:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_close = close
        self._prev_ma = mv
        self._prev_volume = vol

    def CreateClone(self):
        return volume_surge_strategy()
