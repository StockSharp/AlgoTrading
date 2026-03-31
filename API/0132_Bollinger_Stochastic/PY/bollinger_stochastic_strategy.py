import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class bollinger_stochastic_strategy(Strategy):
    def __init__(self):
        super(bollinger_stochastic_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetDisplay("BB Period", "Period for Bollinger Bands", "Bollinger Settings")
        self._bollinger_deviation = self.Param("BollingerDeviation", 2.0) \
            .SetDisplay("BB Deviation", "Standard deviation multiplier", "Bollinger Settings")
        self._stoch_oversold = self.Param("StochOversold", 20.0) \
            .SetDisplay("Oversold Level", "Stochastic oversold level", "Stochastic Settings")
        self._stoch_overbought = self.Param("StochOverbought", 80.0) \
            .SetDisplay("Overbought Level", "Stochastic overbought level", "Stochastic Settings")
        self._cooldown_bars = self.Param("CooldownBars", 50) \
            .SetDisplay("Cooldown Bars", "Bars between trades", "General")
        self._stoch_k = 50.0
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
    def stoch_oversold(self):
        return self._stoch_oversold.Value
    @property
    def stoch_overbought(self):
        return self._stoch_overbought.Value
    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    def OnReseted(self):
        super(bollinger_stochastic_strategy, self).OnReseted()
        self._stoch_k = 50.0
        self._cooldown = 0

    def OnStarted2(self, time):
        super(bollinger_stochastic_strategy, self).OnStarted2(time)
        bollinger = BollingerBands()
        bollinger.Length = self.bollinger_period
        bollinger.Width = self.bollinger_deviation
        stochastic = StochasticOscillator()
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self._on_stochastic)
        subscription.BindEx(bollinger, self.OnProcess).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, bollinger)
            self.DrawOwnTrades(area)
            stoch_area = self.CreateChartArea()
            if stoch_area is not None:
                self.DrawIndicator(stoch_area, stochastic)

    def _on_stochastic(self, candle, stoch_value):
        if stoch_value.K is not None:
            self._stoch_k = float(stoch_value.K)

    def OnProcess(self, candle, bb_value):
        if candle.State != CandleStates.Finished:
            return
        if bb_value.UpBand is None or bb_value.LowBand is None or bb_value.MovingAverage is None:
            return
        upper = float(bb_value.UpBand)
        lower = float(bb_value.LowBand)
        middle = float(bb_value.MovingAverage)
        close = float(candle.ClosePrice)

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        if close <= lower and self._stoch_k < self.stoch_oversold and self.Position == 0:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars
        elif close >= upper and self._stoch_k > self.stoch_overbought and self.Position == 0:
            self.SellMarket()
            self._cooldown = self.cooldown_bars

        if self.Position > 0 and close > middle:
            self.SellMarket()
            self._cooldown = self.cooldown_bars
        elif self.Position < 0 and close < middle:
            self.BuyMarket()
            self._cooldown = self.cooldown_bars

    def CreateClone(self):
        return bollinger_stochastic_strategy()
