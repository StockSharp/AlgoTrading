import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage, AverageTrueRange

class two_ma_four_level_bands_strategy(Strategy):
    def __init__(self):
        super(two_ma_four_level_bands_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(15))) \
            .SetDisplay("Candle Type", "Timeframe", "General")
        self._fast_period = self.Param("FastPeriod", 14) \
            .SetDisplay("Fast MA", "Fast EMA period", "Indicators")
        self._slow_period = self.Param("SlowPeriod", 180) \
            .SetDisplay("Slow MA", "Slow SMA period", "Indicators")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetDisplay("ATR Length", "ATR for band calculation", "Indicators")
        self._band_multiplier = self.Param("BandMultiplier", 1.5) \
            .SetDisplay("Band Mult", "ATR multiplier for offset bands", "Bands")

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    @property
    def CandleType(self):
        return self._candle_type.Value

    @property
    def FastPeriod(self):
        return self._fast_period.Value

    @property
    def SlowPeriod(self):
        return self._slow_period.Value

    @property
    def AtrLength(self):
        return self._atr_length.Value

    @property
    def BandMultiplier(self):
        return self._band_multiplier.Value

    def OnStarted2(self, time):
        super(two_ma_four_level_bands_strategy, self).OnStarted2(time)

        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

        self._fast = ExponentialMovingAverage()
        self._fast.Length = self.FastPeriod
        self._slow = SimpleMovingAverage()
        self._slow.Length = self.SlowPeriod
        self._atr = AverageTrueRange()
        self._atr.Length = self.AtrLength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self._fast, self._slow, self._atr, self.ProcessCandle).Start()

    def ProcessCandle(self, candle, fast_val, slow_val, atr_val):
        if candle.State != CandleStates.Finished:
            return

        fv = float(fast_val)
        sv = float(slow_val)
        av = float(atr_val)

        if self._prev_fast == 0 or self._prev_slow == 0:
            self._prev_fast = fv
            self._prev_slow = sv
            return

        band_offset = av * float(self.BandMultiplier)

        bullish = False
        bearish = False

        # Main line cross
        if self._prev_fast <= self._prev_slow and fv > sv:
            bullish = True
        if self._prev_fast >= self._prev_slow and fv < sv:
            bearish = True

        # Upper band cross
        if self._prev_fast <= self._prev_slow + band_offset and fv > sv + band_offset:
            bullish = True
        if self._prev_fast >= self._prev_slow + band_offset and fv < sv + band_offset:
            bearish = True

        # Lower band cross
        if self._prev_fast <= self._prev_slow - band_offset and fv > sv - band_offset:
            bullish = True
        if self._prev_fast >= self._prev_slow - band_offset and fv < sv - band_offset:
            bearish = True

        # Upper band 2
        if self._prev_fast <= self._prev_slow + band_offset * 2 and fv > sv + band_offset * 2:
            bullish = True
        if self._prev_fast >= self._prev_slow + band_offset * 2 and fv < sv + band_offset * 2:
            bearish = True

        # Lower band 2
        if self._prev_fast <= self._prev_slow - band_offset * 2 and fv > sv - band_offset * 2:
            bullish = True
        if self._prev_fast >= self._prev_slow - band_offset * 2 and fv < sv - band_offset * 2:
            bearish = True

        # Exit
        if self.Position > 0 and bearish:
            self.SellMarket()
            self._entry_price = 0.0
        elif self.Position < 0 and bullish:
            self.BuyMarket()
            self._entry_price = 0.0

        # Entry
        if self.Position == 0:
            if bullish:
                self._entry_price = float(candle.ClosePrice)
                self.BuyMarket()
            elif bearish:
                self._entry_price = float(candle.ClosePrice)
                self.SellMarket()

        self._prev_fast = fv
        self._prev_slow = sv

    def OnReseted(self):
        super(two_ma_four_level_bands_strategy, self).OnReseted()
        self._prev_fast = 0.0
        self._prev_slow = 0.0
        self._entry_price = 0.0

    def CreateClone(self):
        return two_ma_four_level_bands_strategy()
