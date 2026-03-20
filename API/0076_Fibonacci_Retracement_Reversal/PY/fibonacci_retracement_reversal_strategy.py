import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class fibonacci_retracement_reversal_strategy(Strategy):
    """
    Fibonacci Retracement Reversal strategy.
    Identifies swing high/low over a lookback window and enters at key Fibonacci retracement levels.
    Bullish reversal at 61.8% retracement from swing low.
    Bearish reversal at 61.8% retracement from swing high.
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(fibonacci_retracement_reversal_strategy, self).__init__()
        self._swing_lookback = self.Param("SwingLookback", 20).SetDisplay("Swing Lookback", "Lookback for swing high/low", "Indicators")
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._highs = []
        self._lows = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(fibonacci_retracement_reversal_strategy, self).OnReseted()
        self._highs = []
        self._lows = []
        self._cooldown = 0

    def OnStarted(self, time):
        super(fibonacci_retracement_reversal_strategy, self).OnStarted(time)

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

        # Track highs and lows
        self._highs.append(float(candle.HighPrice))
        self._lows.append(float(candle.LowPrice))

        lb = self._swing_lookback.Value

        if len(self._highs) > lb:
            self._highs.pop(0)
            self._lows.pop(0)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if len(self._highs) < lb:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Find swing high and swing low from lookback
        swing_high = max(self._highs)
        swing_low = min(self._lows)

        rng = swing_high - swing_low
        if rng <= 0:
            return

        # Fibonacci 61.8% retracement levels
        fib618_from_high = swing_high - rng * 0.618
        fib618_from_low = swing_low + rng * 0.618
        buffer = rng * 0.02  # 2% buffer

        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        is_bullish = candle.ClosePrice > candle.OpenPrice
        is_bearish = candle.ClosePrice < candle.OpenPrice

        # Buy at 61.8% retracement from high (near swing low area) with bullish candle
        if self.Position == 0 and abs(close - fib618_from_high) < buffer and is_bullish:
            self.BuyMarket()
            self._cooldown = cd
        # Sell at 61.8% retracement from low (near swing high area) with bearish candle
        elif self.Position == 0 and abs(close - fib618_from_low) < buffer and is_bearish:
            self.SellMarket()
            self._cooldown = cd
        # Exit long below SMA
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        # Exit short above SMA
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return fibonacci_retracement_reversal_strategy()
