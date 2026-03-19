import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class mtf_seconds_values_jd_strategy(Strategy):
    """
    MTF Seconds Values JD: SMA crossover of close price with cooldown.
    """

    def __init__(self):
        super(mtf_seconds_values_jd_strategy, self).__init__()
        self._avg_length = self.Param("AverageLength", 20).SetDisplay("SMA Length", "SMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 4).SetDisplay("Cooldown", "Min bars between entries", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_above = False
        self._has_prev = False
        self._bar_index = 0
        self._last_trade_bar = -1000000

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mtf_seconds_values_jd_strategy, self).OnReseted()
        self._prev_above = False
        self._has_prev = False
        self._bar_index = 0
        self._last_trade_bar = -1000000

    def OnStarted(self, time):
        super(mtf_seconds_values_jd_strategy, self).OnStarted(time)
        sma = SimpleMovingAverage()
        sma.Length = self._avg_length.Value
        ema = ExponentialMovingAverage()
        ema.Length = 20
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val, d2):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        sma = float(sma_val)
        self._bar_index += 1
        above = close > sma
        if not self._has_prev:
            self._prev_above = above
            self._has_prev = True
            return
        can_trade = self._bar_index - self._last_trade_bar >= self._cooldown_bars.Value
        cross_up = not self._prev_above and above
        cross_down = self._prev_above and not above
        if can_trade and cross_up and self.Position <= 0:
            self.BuyMarket()
            self._last_trade_bar = self._bar_index
        elif can_trade and cross_down and self.Position >= 0:
            self.SellMarket()
            self._last_trade_bar = self._bar_index
        self._prev_above = above

    def CreateClone(self):
        return mtf_seconds_values_jd_strategy()
