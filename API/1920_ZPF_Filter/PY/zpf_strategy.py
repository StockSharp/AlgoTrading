import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class zpf_strategy(Strategy):
    def __init__(self):
        super(zpf_strategy, self).__init__()
        self._length = self.Param("Length", 12) \
            .SetDisplay("Length", "Base moving average length", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 6) \
            .SetGreaterThanZero() \
            .SetDisplay("Cooldown Bars", "Bars between trades", "Trading")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._volume_ma = None
        self._bars_since_trade = 6

    @property
    def length(self):
        return self._length.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(zpf_strategy, self).OnReseted()
        self._volume_ma = None
        self._bars_since_trade = self.cooldown_bars

    def OnStarted2(self, time):
        super(zpf_strategy, self).OnStarted2(time)
        self._bars_since_trade = self.cooldown_bars
        fast_ma = SimpleMovingAverage()
        fast_ma.Length = self.length
        slow_ma = SimpleMovingAverage()
        slow_ma.Length = self.length * 2
        self._volume_ma = SimpleMovingAverage()
        self._volume_ma.Length = self.length
        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(fast_ma, slow_ma, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawIndicator(area, fast_ma)
            self.DrawIndicator(area, slow_ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, fast, slow):
        if candle.State != CandleStates.Finished:
            return

        self._bars_since_trade += 1

        vol_input = DecimalIndicatorValue(self._volume_ma, candle.TotalVolume, candle.OpenTime)
        vol_input.IsFinal = True
        vol_result = self._volume_ma.Process(vol_input)
        if not vol_result.IsFormed:
            return

        volume_avg = float(vol_result)
        zpf = volume_avg * (float(fast) - float(slow)) / 2.0

        if zpf > 0 and self.Position <= 0 and self._bars_since_trade >= self.cooldown_bars:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._bars_since_trade = 0
        elif zpf < 0 and self.Position >= 0 and self._bars_since_trade >= self.cooldown_bars:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._bars_since_trade = 0

    def CreateClone(self):
        return zpf_strategy()
