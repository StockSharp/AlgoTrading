import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
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

        self._bollinger = None
        self._rsi = None
        self._ma = None
        self._cooldown_remaining = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(bollinger_winner_pro_strategy, self).OnReseted()
        self._bollinger = None
        self._rsi = None
        self._ma = None
        self._cooldown_remaining = 0

    def OnStarted(self, time):
        super(bollinger_winner_pro_strategy, self).OnStarted(time)

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

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if self._cooldown_remaining > 0:
            self._cooldown_remaining -= 1
            return

        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        close = float(candle.ClosePrice)
        cooldown = int(self._cooldown_bars.Value)

        if close <= lower and rsi < float(self._rsi_oversold.Value) and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))
            self.BuyMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif close >= upper and rsi > float(self._rsi_overbought.Value) and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket(Math.Abs(self.Position))
            self.SellMarket(self.Volume)
            self._cooldown_remaining = cooldown
        elif self.Position > 0 and close >= middle:
            self.SellMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown
        elif self.Position < 0 and close <= middle:
            self.BuyMarket(Math.Abs(self.Position))
            self._cooldown_remaining = cooldown

    def CreateClone(self):
        return bollinger_winner_pro_strategy()
