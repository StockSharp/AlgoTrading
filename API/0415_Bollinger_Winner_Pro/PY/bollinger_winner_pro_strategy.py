import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class bollinger_winner_pro_strategy(Strategy):
    """Bollinger Bands Winner PRO Strategy with RSI and MA filters."""

    def __init__(self):
        super(bollinger_winner_pro_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Period", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 1.5) \
            .SetDisplay("BB StdDev", "Bollinger Bands standard deviation multiplier", "Bollinger Bands")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI Filter")
        self._rsi_oversold = self.Param("RSIOversold", 40.0) \
            .SetDisplay("RSI Oversold", "RSI oversold threshold", "RSI Filter")
        self._rsi_overbought = self.Param("RSIOverbought", 60.0) \
            .SetDisplay("RSI Overbought", "RSI overbought threshold", "RSI Filter")
        self._ma_length = self.Param("MALength", 50) \
            .SetDisplay("MA Length", "Moving average period", "Moving Average")
        self._cooldown_bars = self.Param("CooldownBars", 20) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._cooldown_remaining = 0

    @property
    def CandleType(self):
        return self._candle_type.Value
    @property
    def BBLength(self):
        return self._bb_length.Value
    @property
    def BBMultiplier(self):
        return self._bb_multiplier.Value
    @property
    def RSILength(self):
        return self._rsi_length.Value
    @property
    def RSIOversold(self):
        return self._rsi_oversold.Value
    @property
    def RSIOverbought(self):
        return self._rsi_overbought.Value
    @property
    def MALength(self):
        return self._ma_length.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bollinger_winner_pro_strategy, self).OnReseted()
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_winner_pro_strategy, self).OnStarted(time)

        bb = BollingerBands()
        bb.Length = self.BBLength
        bb.Width = self.BBMultiplier

        rsi = RelativeStrengthIndex()
        rsi.Length = self.RSILength

        ma = ExponentialMovingAverage()
        ma.Length = self.MALength

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.BindEx(bb, rsi, ma, self.OnProcess).Start()

    def OnProcess(self, candle, bb_value, rsi_value, ma_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        if rsi_value.IsEmpty or ma_value.IsEmpty:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        rsi = float(IndicatorHelper.ToDecimal(rsi_value))

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)

        if close <= lower and rsi < self.RSIOversold and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars
        elif close >= upper and rsi > self.RSIOverbought and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position > 0 and close >= middle:
            self.SellMarket()
            self._cooldown_remaining = self.CooldownBars
        elif self.Position < 0 and close <= middle:
            self.BuyMarket()
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return bollinger_winner_pro_strategy()
