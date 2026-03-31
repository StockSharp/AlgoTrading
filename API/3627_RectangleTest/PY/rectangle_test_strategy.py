import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class rectangle_test_strategy(Strategy):
    def __init__(self):
        super(rectangle_test_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5)))
        self._ema_period = self.Param("EmaPeriod", 20)
        self._sma_period = self.Param("SmaPeriod", 50)
        self._range_candles = self.Param("RangeCandles", 10)
        self._rectangle_size_percent = self.Param("RectangleSizePercent", 10.0)

        self._highs = []
        self._lows = []

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def EmaPeriod(self):
        return self._ema_period.Value

    @EmaPeriod.setter
    def EmaPeriod(self, value):
        self._ema_period.Value = value

    @property
    def SmaPeriod(self):
        return self._sma_period.Value

    @SmaPeriod.setter
    def SmaPeriod(self, value):
        self._sma_period.Value = value

    @property
    def RangeCandles(self):
        return self._range_candles.Value

    @RangeCandles.setter
    def RangeCandles(self, value):
        self._range_candles.Value = value

    @property
    def RectangleSizePercent(self):
        return self._rectangle_size_percent.Value

    @RectangleSizePercent.setter
    def RectangleSizePercent(self, value):
        self._rectangle_size_percent.Value = value

    def OnReseted(self):
        super(rectangle_test_strategy, self).OnReseted()
        self._highs = []
        self._lows = []

    def OnStarted2(self, time):
        super(rectangle_test_strategy, self).OnStarted2(time)
        self._highs = []
        self._lows = []

        ema = ExponentialMovingAverage()
        ema.Length = self.EmaPeriod
        sma = SimpleMovingAverage()
        sma.Length = self.SmaPeriod

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, sma, self._process_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _process_candle(self, candle, ema_value, sma_value):
        if candle.State != CandleStates.Finished:
            return

        ema_val = float(ema_value)
        sma_val = float(sma_value)
        close = float(candle.ClosePrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        range_candles = self.RangeCandles

        if len(self._highs) >= range_candles:
            start_idx = len(self._highs) - range_candles
            highest = max(self._highs[start_idx:])
            lowest = min(self._lows[start_idx:])

            if highest > 0 and lowest > 0:
                range_pct = (highest - lowest) / highest * 100.0
                rect_size = float(self.RectangleSizePercent)

                if 0 < range_pct < rect_size:
                    # Breakout above rectangle with bullish trend
                    if close > highest and ema_val > sma_val and self.Position == 0:
                        self.BuyMarket()
                    # Breakout below rectangle with bearish trend
                    elif close < lowest and ema_val < sma_val and self.Position == 0:
                        self.SellMarket()

        self._highs.append(high)
        self._lows.append(low)

        while len(self._highs) > range_candles + 1:
            self._highs.pop(0)
            self._lows.pop(0)

    def CreateClone(self):
        return rectangle_test_strategy()
