import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy

class bollinger_reversion_strategy(Strategy):
    """
    Bollinger Bands mean reversion strategy.
    Enters when price touches bands, exits when price returns to middle.
    """

    def __init__(self):
        super(bollinger_reversion_strategy, self).__init__()
        self._bollinger_period = self.Param("BollingerPeriod", 20).SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0).SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Number of bars to wait between trades", "General")
        self._max_hold_bars = self.Param("MaxHoldBars", 300).SetDisplay("Max Hold Bars", "Maximum bars to hold a position", "General")

        self._cooldown = 0
        self._hold_bars = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_reversion_strategy, self).OnReseted()
        self._cooldown = 0
        self._hold_bars = 0

    def OnStarted2(self, time):
        super(bollinger_reversion_strategy, self).OnStarted2(time)

        self._cooldown = 0
        self._hold_bars = 0

        bb = BollingerBands()
        bb.Length = self._bollinger_period.Value
        bb.Width = self._bollinger_deviation.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(bb, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bb)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bb_val):
        if candle.State != CandleStates.Finished:
            return

        if not bb_val.IsFormed:
            return

        # Track hold duration
        if self.Position != 0:
            self._hold_bars += 1
        else:
            self._hold_bars = 0

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if bb_val.UpBand is None or bb_val.LowBand is None or bb_val.MovingAverage is None:
            return

        upper = float(bb_val.UpBand)
        lower = float(bb_val.LowBand)
        middle = float(bb_val.MovingAverage)
        close = float(candle.ClosePrice)
        max_hold = self._max_hold_bars.Value
        cd = self._cooldown_bars.Value

        # Exit logic: revert to middle or time-based forced exit
        if self.Position > 0 and (close >= middle or self._hold_bars >= max_hold):
            self.SellMarket()
            self._cooldown = cd
            self._hold_bars = 0
            return

        if self.Position < 0 and (close <= middle or self._hold_bars >= max_hold):
            self.BuyMarket()
            self._cooldown = cd
            self._hold_bars = 0
            return

        # Entry logic
        if self.Position == 0 and close < lower:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and close > upper:
            self.SellMarket()
            self._cooldown = cd

    def CreateClone(self):
        return bollinger_reversion_strategy()
