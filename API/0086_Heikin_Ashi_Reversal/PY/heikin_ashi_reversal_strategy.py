import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class heikin_ashi_reversal_strategy(Strategy):
    """
    Heikin Ashi Reversal strategy.
    Computes Heikin-Ashi candles from regular candles.
    Enters long when HA switches from bearish to bullish.
    Enters short when HA switches from bullish to bearish.
    Uses SMA for exit confirmation.
    """

    def __init__(self):
        super(heikin_ashi_reversal_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._ha_open = 0.0
        self._ha_close = 0.0
        self._prev_bullish = None
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(heikin_ashi_reversal_strategy, self).OnReseted()
        self._ha_open = 0.0
        self._ha_close = 0.0
        self._prev_bullish = None
        self._cooldown = 0

    def OnStarted(self, time):
        super(heikin_ashi_reversal_strategy, self).OnStarted(time)

        self._ha_open = 0.0
        self._ha_close = 0.0
        self._prev_bullish = None
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

        # Compute Heikin-Ashi values
        new_ha_close = (float(candle.OpenPrice) + float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 4.0

        if self._ha_open == 0:
            # First candle
            new_ha_open = (float(candle.OpenPrice) + float(candle.ClosePrice)) / 2.0
        else:
            new_ha_open = (self._ha_open + self._ha_close) / 2.0

        self._ha_open = new_ha_open
        self._ha_close = new_ha_close

        is_bullish = new_ha_close > new_ha_open

        if not self.IsFormedAndOnlineAndAllowTrading():
            self._prev_bullish = is_bullish
            return

        if self._prev_bullish is None:
            self._prev_bullish = is_bullish
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_bullish = is_bullish
            return

        # Reversal detection
        bullish_reversal = self._prev_bullish == False and is_bullish
        bearish_reversal = self._prev_bullish == True and not is_bullish

        sv = float(sma_val)
        close = float(candle.ClosePrice)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and bullish_reversal:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bearish_reversal:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_bullish = is_bullish

    def CreateClone(self):
        return heikin_ashi_reversal_strategy()
