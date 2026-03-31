import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class z_score_strategy(Strategy):
    """
    Z-Score mean reversion strategy.
    Buys when Z-Score is below negative threshold, sells when above positive threshold.
    """

    def __init__(self):
        super(z_score_strategy, self).__init__()
        self._z_entry = self.Param("ZScoreEntryThreshold", 1.5).SetDisplay("Z-Score Entry", "Distance from mean in std devs for entry", "Z-Score")
        self._z_exit = self.Param("ZScoreExitThreshold", 0.5).SetDisplay("Z-Score Exit", "Distance from mean in std devs for exit", "Z-Score")
        self._ma_period = self.Param("MAPeriod", 10).SetDisplay("MA Period", "Period for Moving Average", "Indicators")
        self._std_period = self.Param("StdDevPeriod", 10).SetDisplay("StdDev Period", "Period for Standard Deviation", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_z = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(z_score_strategy, self).OnReseted()
        self._prev_z = 0.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(z_score_strategy, self).OnStarted2(time)

        self._prev_z = 0.0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value
        std = StandardDeviation()
        std.Length = self._std_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, std, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, std_val):
        if candle.State != CandleStates.Finished:
            return

        sv = float(std_val)
        if sv == 0:
            return

        mv = float(ma_val)
        z = (float(candle.ClosePrice) - mv) / sv
        cd = self._cooldown_bars.Value
        entry = float(self._z_entry.Value)
        exit_t = float(self._z_exit.Value)

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_z = z
            return

        if self.Position == 0:
            if z < -entry:
                self.BuyMarket()
                self._cooldown = cd
            elif z > entry:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0:
            if z > exit_t:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position < 0:
            if z < -exit_t:
                self.BuyMarket()
                self._cooldown = cd

        self._prev_z = z

    def CreateClone(self):
        return z_score_strategy()
