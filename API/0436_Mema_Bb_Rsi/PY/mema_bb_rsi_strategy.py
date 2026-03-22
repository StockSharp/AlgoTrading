import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, BollingerBands, RelativeStrengthIndex, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class mema_bb_rsi_strategy(Strategy):
    """Multi EMA + Bollinger Bands + RSI Strategy."""

    def __init__(self):
        super(mema_bb_rsi_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._ma1_period = self.Param("Ma1Period", 10) \
            .SetDisplay("MA1 Period", "Fast EMA period", "Moving Average")
        self._ma2_period = self.Param("Ma2Period", 55) \
            .SetDisplay("MA2 Period", "Slow EMA period", "Moving Average")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 2.0) \
            .SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI")
        self._rsi_overbought = self.Param("RsiOverbought", 70) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI")
        self._cooldown_bars = self.Param("CooldownBars", 10) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._ma1 = None
        self._ma2 = None
        self._bollinger = None
        self._rsi = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(mema_bb_rsi_strategy, self).OnReseted()
        self._ma1 = None
        self._ma2 = None
        self._bollinger = None
        self._rsi = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(mema_bb_rsi_strategy, self).OnStarted(time)

        self._ma1 = ExponentialMovingAverage()
        self._ma1.Length = int(self._ma1_period.Value)

        self._ma2 = ExponentialMovingAverage()
        self._ma2.Length = int(self._ma2_period.Value)

        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bb_length.Value)
        self._bollinger.Width = float(self._bb_multiplier.Value)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._ma1, self._ma2, self._bollinger, self._rsi, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma1)
            self.DrawIndicator(area, self._ma2)
            self.DrawIndicator(area, self._bollinger)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, ma1_value, ma2_value, bb_value, rsi_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._ma1.IsFormed or not self._ma2.IsFormed or not self._bollinger.IsFormed or not self._rsi.IsFormed:
            return

        if ma1_value.IsEmpty or ma2_value.IsEmpty or bb_value.IsEmpty or rsi_value.IsEmpty:
            return

        ma1 = float(IndicatorHelper.ToDecimal(ma1_value))
        rsi = float(IndicatorHelper.ToDecimal(rsi_value))

        if bb_value.UpBand is None or bb_value.LowBand is None:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        low = float(candle.LowPrice)
        high = float(candle.HighPrice)
        cooldown = int(self._cooldown_bars.Value)
        rsi_ob = int(self._rsi_overbought.Value)

        entry_long = close > ma1 and low <= lower
        entry_short = close < ma1 and high >= upper
        exit_long = rsi > rsi_ob
        exit_short = close < lower

        if exit_long and self.Position > 0:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif exit_short and self.Position < 0:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif entry_long and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif entry_short and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return mema_bb_rsi_strategy()
