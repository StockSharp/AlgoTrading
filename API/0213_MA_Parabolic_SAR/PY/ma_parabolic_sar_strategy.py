import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, ExponentialMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_parabolic_sar_strategy(Strategy):
    """
    MA + SAR proxy (EMA). Enters on combined MA/SAR flip signals.
    """

    def __init__(self):
        super(ma_parabolic_sar_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "SMA period", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 20).SetDisplay("Cooldown", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._has_prev = False
        self._prev_above_ma = False
        self._prev_above_sar = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_parabolic_sar_strategy, self).OnReseted()
        self._has_prev = False
        self._prev_above_ma = False
        self._prev_above_sar = False
        self._cooldown = 0

    def OnStarted(self, time):
        super(ma_parabolic_sar_strategy, self).OnStarted(time)
        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        sar_proxy = ExponentialMovingAverage()
        sar_proxy.Length = max(2, self._ma_period.Value // 2)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, sar_proxy, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, sar_proxy)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, sar_val):
        if candle.State != CandleStates.Finished:
            return
        close = float(candle.ClosePrice)
        ma = float(ma_val)
        sar = float(sar_val)
        above_ma = close > ma
        above_sar = close > sar
        if not self._has_prev:
            self._has_prev = True
            self._prev_above_ma = above_ma
            self._prev_above_sar = above_sar
            return
        turned_bull = not self._prev_above_sar and above_sar and above_ma
        turned_bear = self._prev_above_sar and not above_sar and not above_ma
        sar_flip_down = self._prev_above_sar and not above_sar
        sar_flip_up = not self._prev_above_sar and above_sar
        if self._cooldown > 0:
            self._cooldown -= 1
        if self._cooldown == 0 and turned_bull:
            if self.Position <= 0:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self._cooldown == 0 and turned_bear:
            if self.Position >= 0:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
        elif self.Position > 0 and sar_flip_down:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and sar_flip_up:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        self._prev_above_ma = above_ma
        self._prev_above_sar = above_sar

    def CreateClone(self):
        return ma_parabolic_sar_strategy()
