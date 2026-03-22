import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
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

        self._bollinger = None
        self._rsi = None
        self._ma = None
        self._entry_price = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_breakout_strategy, self).OnReseted()
        self._bollinger = None
        self._rsi = None
        self._ma = None
        self._entry_price = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_breakout_strategy, self).OnStarted(time)

        self._bollinger = BollingerBands()
        self._bollinger.Length = int(self._bb_length.Value)
        self._bollinger.Width = float(self._bb_multiplier.Value)

        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = int(self._rsi_length.Value)

        self._ma = ExponentialMovingAverage()
        self._ma.Length = int(self._ma_length.Value)

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bollinger, self._rsi, self._ma, self._on_process).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bollinger)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def _on_process(self, candle, bb_value, rsi_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        if not self._bollinger.IsFormed or not self._rsi.IsFormed or not self._ma.IsFormed:
            return

        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        if rsi_value.IsEmpty or ma_value.IsEmpty:
            return

        rsi = float(IndicatorHelper.ToDecimal(rsi_value))
        ma_val = float(IndicatorHelper.ToDecimal(ma_value))

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        close = float(candle.ClosePrice)
        opn = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)

        candle_size = high - low
        if candle_size <= 0:
            return

        candle_pct = float(self._candle_percent.Value)
        cooldown = int(self._cooldown_bars.Value)

        buy_zone = candle_size * candle_pct + low
        sell_zone = high - candle_size * candle_pct

        buy_signal = buy_zone < lower and close < opn and rsi < float(self._rsi_oversold.Value) and close > ma_val
        sell_signal = sell_zone > upper and close > opn and rsi > float(self._rsi_overbought.Value) and close < ma_val

        if self.Position > 0 and close >= middle:
            self.SellMarket(Math.Abs(self.Position))
            self._entry_price = None
            self._cooldown_remaining = cooldown
            return
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(Math.Abs(self.Position))
            self._entry_price = None
            self._cooldown_remaining = cooldown
            return

        if buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown
        elif sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._entry_price = close
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return bollinger_breakout_strategy()
