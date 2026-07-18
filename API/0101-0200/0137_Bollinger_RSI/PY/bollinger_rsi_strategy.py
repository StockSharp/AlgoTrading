import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, RelativeStrengthIndex
from StockSharp.Algo.Strategies import Strategy


class bollinger_rsi_strategy(Strategy):
    def __init__(self):
        super(bollinger_rsi_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("Bollinger Period", "Period of the Bollinger Bands indicator", "Indicators")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetDisplay("RSI Period", "Period of the RSI indicator", "Indicators")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Indicators")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._rsi_value = 50.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value
    @property
    def bollinger_period(self):
        return self._bollinger_period.Value
    @property
    def bollinger_deviation(self):
        return self._bollinger_deviation.Value
    @property
    def rsi_period(self):
        return self._rsi_period.Value
    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value
    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bollinger_rsi_strategy, self).OnReseted()
        self._rsi_value = 50.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bollinger_rsi_strategy, self).OnStarted2(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation
        rsi = RelativeStrengthIndex()
        rsi.Length = self.rsi_period
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(rsi, self._on_rsi)
        subscription.BindEx(bollinger, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            rsi_area = self.CreateChartArea()
            if rsi_area is not None:
                self.DrawIndicator(rsi_area, rsi)

    def _on_rsi(self, candle, rsi_val):
        self._rsi_value = float(rsi_val)

    def OnProcess(self, candle, bollinger_value):
        if candle.State != CandleStates.Finished:
            return
        bb = bollinger_value
        if bb.UpBand is None or bb.LowBand is None or bb.MovingAverage is None:
            return
        upper_band = float(bb.UpBand)
        lower_band = float(bb.LowBand)
        middle_band = float(bb.MovingAverage)
        close = float(candle.ClosePrice)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if close < lower_band and self._rsi_value < self.rsi_oversold and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        elif close > upper_band and self._rsi_value > self.rsi_overbought and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        if self.Position > 0 and close > middle_band:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        elif self.Position < 0 and close < middle_band:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def CreateClone(self):
        return bollinger_rsi_strategy()
