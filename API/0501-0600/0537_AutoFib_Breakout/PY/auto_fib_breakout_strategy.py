import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange, Highest, Lowest
from StockSharp.Algo.Strategies import Strategy

STOP_ATR_MULTIPLE = 1.5
TAKE_ATR_MULTIPLE = 3.0


class auto_fib_breakout_strategy(Strategy):
    """
    AutoFib breakout strategy.
    The swing low and high of the previous PivotPeriod candles span a Fibonacci extension at low + (high - low) * FibLevel. A
    close above that level while the close is above the EMA opens a long. Each entry fixes a 1.5 ATR stop-loss and a 3 ATR
    take-profit from the entry close. Long only.
    """

    def __init__(self):
        super(auto_fib_breakout_strategy, self).__init__()
        self._ema_length = self.Param("EmaLength", 200) \
            .SetGreaterThanZero() \
            .SetDisplay("EMA Length", "Trend EMA period", "Trend")
        self._atr_length = self.Param("AtrLength", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ATR Length", "ATR period of the stop-loss and take-profit", "Risk")
        self._fib_level = self.Param("FibLevel", 1.618) \
            .SetGreaterThanZero() \
            .SetDisplay("Fib Level", "Fibonacci extension level of the breakout", "Fibonacci")
        self._pivot_period = self.Param("PivotPeriod", 10) \
            .SetGreaterThanZero() \
            .SetDisplay("Pivot Period", "Candles that define the swing high and low", "Fibonacci")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._reset_state()

    @property
    def CandleType(self):
        return self._candle_type.Value

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _reset_state(self):
        self._prev_high = None
        self._prev_low = None
        self._stop_price = 0.0
        self._take_price = 0.0

    def OnReseted(self):
        super(auto_fib_breakout_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(auto_fib_breakout_strategy, self).OnStarted2(time)

        self._reset_state()

        ema = ExponentialMovingAverage()
        ema.Length = self._ema_length.Value
        atr = AverageTrueRange()
        atr.Length = self._atr_length.Value
        highest = Highest()
        highest.Length = self._pivot_period.Value
        lowest = Lowest()
        lowest.Length = self._pivot_period.Value

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(ema, atr, highest, lowest, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ema_value, atr_value, highest_value, lowest_value):
        if candle.State != CandleStates.Finished:
            return

        # The swing range comes from the candles before this one.
        swing_high = self._prev_high
        swing_low = self._prev_low
        self._prev_high = float(highest_value)
        self._prev_low = float(lowest_value)

        if swing_high is None or swing_low is None:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        close = float(candle.ClosePrice)

        if self.Position > 0:
            if float(candle.LowPrice) <= self._stop_price or float(candle.HighPrice) >= self._take_price:
                self.SellMarket(self.Position)
            return

        extension = swing_low + (swing_high - swing_low) * float(self._fib_level.Value)
        atr = float(atr_value)

        if self.Position == 0 and close > extension and close > float(ema_value):
            self.BuyMarket(self.Volume)
            self._stop_price = close - STOP_ATR_MULTIPLE * atr
            self._take_price = close + TAKE_ATR_MULTIPLE * atr

    def CreateClone(self):
        return auto_fib_breakout_strategy()
