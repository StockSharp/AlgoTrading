import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex, ExponentialMovingAverage, IndicatorHelper
from StockSharp.Algo.Strategies import Strategy


class bollinger_breakout_strategy(Strategy):
    """Bollinger Breakout Strategy with RSI and MA filters."""

    def __init__(self):
        super(bollinger_breakout_strategy, self).__init__()

        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(30))) \
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")
        self._bb_length = self.Param("BBLength", 20) \
            .SetDisplay("BB Length", "Bollinger Bands period", "Bollinger Bands")
        self._bb_multiplier = self.Param("BBMultiplier", 1.5) \
            .SetDisplay("BB StdDev", "Standard deviation multiplier", "Bollinger Bands")
        self._rsi_length = self.Param("RSILength", 14) \
            .SetDisplay("RSI Length", "RSI period", "RSI Filter")
        self._rsi_oversold = self.Param("RSIOversold", 45) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "RSI Filter")
        self._rsi_overbought = self.Param("RSIOverbought", 55) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "RSI Filter")
        self._ma_length = self.Param("MALength", 50) \
            .SetDisplay("MA Length", "Moving Average period", "Moving Average")
        self._candle_percent = self.Param("CandlePercent", 0.3) \
            .SetDisplay("Candle %", "Candle body penetration percentage", "Strategy")
        self._cooldown_bars = self.Param("CooldownBars", 15) \
            .SetDisplay("Cooldown Bars", "Bars to wait between trades", "Risk")

        self._entry_price = None
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
    def CandlePercent(self):
        return self._candle_percent.Value
    @property
    def CooldownBars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bollinger_breakout_strategy, self).OnReseted()
        self._entry_price = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_breakout_strategy, self).OnStarted(time)

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
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        if rsi_value.IsEmpty or ma_value.IsEmpty:
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        rsi = float(IndicatorHelper.ToDecimal(rsi_value))
        ma_val = float(IndicatorHelper.ToDecimal(ma_value))

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        candle_size = high - low
        if candle_size <= 0:
            return

        buy_zone = candle_size * self.CandlePercent + low
        sell_zone = high - candle_size * self.CandlePercent

        buy_signal = buy_zone < lower and close < opn and rsi < self.RSIOversold and close > ma_val
        sell_signal = sell_zone > upper and close > opn and rsi > self.RSIOverbought and close < ma_val

        if self.Position > 0 and close >= middle:
            self.SellMarket()
            self._entry_price = None
            self._cooldown_remaining = self.CooldownBars
            return
        elif self.Position < 0 and close <= middle:
            self.BuyMarket()
            self._entry_price = None
            self._cooldown_remaining = self.CooldownBars
            return

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
            self._cooldown_remaining = self.CooldownBars
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
            self._entry_price = close
            self._cooldown_remaining = self.CooldownBars

    def CreateClone(self):
        return bollinger_breakout_strategy()
