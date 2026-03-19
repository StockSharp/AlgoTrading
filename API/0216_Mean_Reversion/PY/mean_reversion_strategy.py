import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class mean_reversion_strategy(Strategy):
    """
    Statistical Mean Reversion: enters when price deviates from mean by k*stddev, exits at mean.
    """

    def __init__(self):
        super(mean_reversion_strategy, self).__init__()
        self._ma_period = self.Param("MovingAveragePeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._dev_mult = self.Param("DeviationMultiplier", 2.0).SetDisplay("Dev Mult", "Stddev multiplier", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 50).SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._was_below_lower = False
        self._was_above_upper = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mean_reversion_strategy, self).OnReseted()
        self._was_below_lower = False
        self._was_above_upper = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(mean_reversion_strategy, self).OnStarted(time)
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        std_dev = StandardDeviation()
        std_dev.Length = self._ma_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, std_dev, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, std_val):
        if candle.State != CandleStates.Finished:
            return
        ma = float(ma_val)
        std = float(std_val)
        close = float(candle.ClosePrice)
        dm = self._dev_mult.Value
        upper = ma + std * dm
        lower = ma - std * dm
        is_below = close < lower
        is_above = close > upper
        self._was_below_lower = is_below
        self._was_above_upper = is_above
        if self._cooldown > 0:
            self._cooldown -= 1
        if self._cooldown == 0 and is_below:
            if self.Position <= 0:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self._cooldown == 0 and is_above:
            if self.Position >= 0:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0 and close > ma:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and close < ma:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return mean_reversion_strategy()
