import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import ExponentialMovingAverage, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class ma_cci_strategy(Strategy):
    """
    MA + CCI: buy when price above MA and CCI oversold, sell when below MA and CCI overbought.
    """

    def __init__(self):
        super(ma_cci_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "EMA period", "Indicators")
        self._cci_period = self.Param("CciPeriod", 20).SetDisplay("CCI Period", "CCI period", "Indicators")
        self._overbought = self.Param("OverboughtLevel", 100.0).SetDisplay("Overbought", "CCI overbought", "Levels")
        self._oversold = self.Param("OversoldLevel", -100.0).SetDisplay("Oversold", "CCI oversold", "Levels")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown", "Bars between trades", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Timeframe", "General")

        self._cci_value = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_cci_strategy, self).OnReseted()
        self._cci_value = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(ma_cci_strategy, self).OnStarted(time)
        ema = ExponentialMovingAverage()
        ema.Length = self._ma_period.Value
        cci = CommodityChannelIndex()
        cci.Length = self._cci_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(cci, self._on_cci)
        subscription.Bind(ema, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ema)
            self.DrawOwnTrades(area)

    def _on_cci(self, candle, cci_val):
        if not cci_val.IsEmpty:
            self._cci_value = float(cci_val.ToDecimal())

    def _process_candle(self, candle, ema_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        close = float(candle.ClosePrice)
        ma = float(ema_val)
        if self._cooldown > 0:
            self._cooldown -= 1
            return
        if close > ma and self._cci_value < self._oversold.Value and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value
        elif close < ma and self._cci_value > self._overbought.Value and self.Position == 0:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        if self.Position > 0 and close < ma:
            self.SellMarket()
            self._cooldown = self._cooldown_bars.Value
        elif self.Position < 0 and close > ma:
            self.BuyMarket()
            self._cooldown = self._cooldown_bars.Value

    def CreateClone(self):
        return ma_cci_strategy()
