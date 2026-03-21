import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class false_breakout_trap_strategy(Strategy):
    """
    False Breakout Trap strategy.
    Detects when price breaks a recent high/low range then reverses back.
    Trades against the failed breakout direction.
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(false_breakout_trap_strategy, self).__init__()
        self._lookback_period = self.Param("LookbackPeriod", 20).SetDisplay("Lookback", "Period for high/low range", "Range")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Period for SMA exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._highs = []
        self._lows = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(false_breakout_trap_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._cooldown = 0

    def OnStarted(self, time):
        super(false_breakout_trap_strategy, self).OnStarted(time)

        self._highs = []
        self._lows = []
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

        lookback = self._lookback_period.Value

        # Maintain rolling high/low window
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))
        if len(self._highs) > lookback + 1:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < lookback + 1:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        cd = self._cooldown_bars.Value
        sv = float(sma_val)

        # Find highest high and lowest low of the previous N bars (excluding current)
        range_high = max(self._highs[:-1])
        range_low = min(self._lows[:-1])

        # False upside breakout: candle broke above range high but closed below it
        false_break_up = float(candle.HighPrice) > range_high and float(candle.ClosePrice) < range_high
        # False downside breakout: candle broke below range low but closed above it
        false_break_down = float(candle.LowPrice) < range_low and float(candle.ClosePrice) > range_low

        if self.Position == 0 and false_break_down:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and false_break_up:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and float(candle.ClosePrice) < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) > sv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return false_breakout_trap_strategy()
