import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class inside_bar_breakout_strategy(Strategy):
    """
    Inside Bar Breakout strategy.
    Detects inside bar patterns (high lower than previous high, low higher than previous low).
    Enters on breakout of the inside bar's high (buy) or low (sell).
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(inside_bar_breakout_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_candle = None
        self._inside_bar = None
        self._waiting_for_breakout = False
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(inside_bar_breakout_strategy, self).OnReseted()
        self._prev_candle = None
        self._inside_bar = None
        self._waiting_for_breakout = False
        self._cooldown = 0

    def OnStarted2(self, time):
        super(inside_bar_breakout_strategy, self).OnStarted2(time)

        self._prev_candle = None
        self._inside_bar = None
        self._waiting_for_breakout = False
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_candle = candle
            self._waiting_for_breakout = False
            return

        if self._prev_candle is None:
            self._prev_candle = candle
            return

        cd = self._cooldown_bars.Value

        # Check for breakout of a previously detected inside bar
        if self._waiting_for_breakout and self._inside_bar is not None and self.Position == 0:
            if candle.HighPrice > self._inside_bar.HighPrice:
                self.BuyMarket()
                self._cooldown = cd
                self._waiting_for_breakout = False
            elif candle.LowPrice < self._inside_bar.LowPrice:
                self.SellMarket()
                self._cooldown = cd
                self._waiting_for_breakout = False

        # Check if current candle is an inside bar
        if candle.HighPrice < self._prev_candle.HighPrice and candle.LowPrice > self._prev_candle.LowPrice:
            self._inside_bar = candle
            self._waiting_for_breakout = True

        # Exit logic using SMA
        sv = float(sma_val)
        close = float(candle.ClosePrice)

        if self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_candle = candle

    def CreateClone(self):
        return inside_bar_breakout_strategy()
