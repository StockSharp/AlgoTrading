import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class outside_bar_reversal_strategy(Strategy):
    """
    Outside Bar Reversal strategy.
    Detects outside bar patterns (higher high and lower low than previous bar).
    Bullish outside bar = buy, bearish outside bar = sell.
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(outside_bar_reversal_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._prev_candle = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(outside_bar_reversal_strategy, self).OnReseted()
        self._prev_candle = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(outside_bar_reversal_strategy, self).OnStarted(time)

        self._prev_candle = None
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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_candle = candle
            return

        if self._prev_candle is not None:
            # Outside bar: higher high AND lower low than previous bar
            is_outside_bar = candle.HighPrice > self._prev_candle.HighPrice and candle.LowPrice < self._prev_candle.LowPrice

            if is_outside_bar:
                is_bullish = candle.ClosePrice > candle.OpenPrice
                is_bearish = candle.ClosePrice < candle.OpenPrice

                if self.Position == 0 and is_bullish:
                    self.BuyMarket()
                    self._cooldown = self._cooldown_bars.Value
                elif self.Position == 0 and is_bearish:
                    self.SellMarket()
                    self._cooldown = self._cooldown_bars.Value

            # Exit logic using SMA
            sv = float(sma_val)
            if self.Position > 0 and float(candle.ClosePrice) < sv:
                self.SellMarket()
                self._cooldown = self._cooldown_bars.Value
            elif self.Position < 0 and float(candle.ClosePrice) > sv:
                self.BuyMarket()
                self._cooldown = self._cooldown_bars.Value

        self._prev_candle = candle

    def CreateClone(self):
        return outside_bar_reversal_strategy()
