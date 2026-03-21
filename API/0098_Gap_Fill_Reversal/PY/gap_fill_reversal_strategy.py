import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class gap_fill_reversal_strategy(Strategy):
    """
    Gap Fill Reversal strategy.
    Enters when a gap between candles is followed by a reversal candle.
    Gap up + bearish candle = short, gap down + bullish candle = long.
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(gap_fill_reversal_strategy, self).__init__()
        self._min_gap_percent = self.Param("MinGapPercent", 0.02).SetDisplay("Min Gap %", "Minimum gap size percentage", "Trading")
        self._ma_length = self.Param("MaLength", 20).SetDisplay("MA Length", "Period of SMA for exit", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_candle = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(gap_fill_reversal_strategy, self).OnReseted()
        self._prev_candle = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(gap_fill_reversal_strategy, self).OnStarted(time)

        self._prev_candle = None
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_length.Value

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

        if self._prev_candle is None:
            self._prev_candle = candle
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_candle = candle
            return

        prev_close = float(self._prev_candle.ClosePrice)
        open_price = float(candle.OpenPrice)

        # Gap detection
        gap_up = open_price > prev_close
        gap_down = open_price < prev_close

        gap_percent = 0.0
        if gap_up and prev_close > 0:
            gap_percent = (open_price - prev_close) / prev_close * 100.0
        elif gap_down and prev_close > 0:
            gap_percent = (prev_close - open_price) / prev_close * 100.0

        is_bearish = candle.ClosePrice < candle.OpenPrice
        is_bullish = candle.ClosePrice > candle.OpenPrice

        sv = float(sma_val)
        cd = self._cooldown_bars.Value
        min_gap = self._min_gap_percent.Value

        if gap_percent >= min_gap:
            # Gap down + bullish reversal = long
            if self.Position == 0 and gap_down and is_bullish:
                self.BuyMarket()
                self._cooldown = cd
            # Gap up + bearish reversal = short
            elif self.Position == 0 and gap_up and is_bearish:
                self.SellMarket()
                self._cooldown = cd

        # Exit on SMA cross
        if self.Position > 0 and float(candle.ClosePrice) < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and float(candle.ClosePrice) > sv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_candle = candle

    def CreateClone(self):
        return gap_fill_reversal_strategy()
