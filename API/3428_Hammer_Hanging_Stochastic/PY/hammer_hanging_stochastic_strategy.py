import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class hammer_hanging_stochastic_strategy(Strategy):
    """
    Hammer/Hanging Man + Stochastic strategy.
    Buys on hammer candle in oversold stochastic.
    Sells on hanging man candle in overbought stochastic.
    """

    def __init__(self):
        super(hammer_hanging_stochastic_strategy, self).__init__()
        self._stoch_period = self.Param("StochPeriod", 14) \
            .SetDisplay("Stoch Period", "Stochastic K period", "Indicators")
        self._oversold = self.Param("Oversold", 30.0) \
            .SetDisplay("Oversold", "Stochastic oversold level", "Signals")
        self._overbought = self.Param("Overbought", 70.0) \
            .SetDisplay("Overbought", "Stochastic overbought level", "Signals")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Candle timeframe", "General")

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted(self, time):
        super(hammer_hanging_stochastic_strategy, self).OnStarted(time)

        stoch = StochasticOscillator()
        stoch.K.Length = self._stoch_period.Value
        stoch.D.Length = 3

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, self._process_candle).Start()

    def _process_candle(self, candle, stoch_val):
        if candle.State != CandleStates.Finished:
            return

        k_value = stoch_val.K
        if k_value is None:
            return
        k_value = float(k_value)

        close = float(candle.ClosePrice)
        open_p = float(candle.OpenPrice)
        high = float(candle.HighPrice)
        low = float(candle.LowPrice)

        body = abs(close - open_p)
        range_val = high - low
        if range_val <= 0 or body <= 0:
            return

        upper_shadow = high - max(open_p, close)
        lower_shadow = min(open_p, close) - low

        is_hammer = lower_shadow > body * 2 and upper_shadow < body
        is_hanging_man = upper_shadow > body * 2 and lower_shadow < body

        if is_hammer and k_value < self._oversold.Value and self.Position <= 0:
            self.BuyMarket()
        elif is_hanging_man and k_value > self._overbought.Value and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return hammer_hanging_stochastic_strategy()
