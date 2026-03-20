import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import RelativeStrengthIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class genie_stoch_rsi_strategy(Strategy):
    def __init__(self):
        super(genie_stoch_rsi_strategy, self).__init__()
        self._rsi_period = self.Param("RsiPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("RSI Period", "RSI calculation length", "Parameters")
        self._rsi_overbought = self.Param("RsiOverbought", 70.0) \
            .SetDisplay("RSI Overbought", "RSI overbought level", "Signals")
        self._rsi_oversold = self.Param("RsiOversold", 30.0) \
            .SetDisplay("RSI Oversold", "RSI oversold level", "Signals")
        self._stoch_overbought = self.Param("StochOverbought", 80.0) \
            .SetDisplay("Stoch Overbought", "Stochastic overbought level", "Signals")
        self._stoch_oversold = self.Param("StochOversold", 20.0) \
            .SetDisplay("Stoch Oversold", "Stochastic oversold level", "Signals")
        self._take_profit_param = self.Param("TakeProfit", 500.0) \
            .SetDisplay("Take Profit", "Take profit in price points", "Risk")
        self._trailing_stop = self.Param("TrailingStop", 200.0) \
            .SetDisplay("Trailing Stop", "Trailing stop in price points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "General")

        self._rsi = None
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._initialized = False

    @property
    def rsi_period(self):
        return self._rsi_period.Value

    @property
    def rsi_overbought(self):
        return self._rsi_overbought.Value

    @property
    def rsi_oversold(self):
        return self._rsi_oversold.Value

    @property
    def stoch_overbought(self):
        return self._stoch_overbought.Value

    @property
    def stoch_oversold(self):
        return self._stoch_oversold.Value

    @property
    def take_profit(self):
        return self._take_profit_param.Value

    @property
    def trailing_stop(self):
        return self._trailing_stop.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(genie_stoch_rsi_strategy, self).OnReseted()
        self._prev_k = 0.0
        self._prev_d = 0.0
        self._initialized = False

    def OnStarted(self, time):
        super(genie_stoch_rsi_strategy, self).OnStarted(time)
        self._rsi = RelativeStrengthIndex()
        self._rsi.Length = self.rsi_period
        stochastic = StochasticOscillator()

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stochastic, self.process_candle).Start()

        self.StartProtection(
            Unit(float(self.take_profit), UnitTypes.Absolute),
            Unit(float(self.trailing_stop), UnitTypes.Absolute),
            True)

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._rsi)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return

        # Process RSI manually
        rsi_result = self._rsi.Process(candle.ClosePrice, candle.OpenTime, True)

        if not self._rsi.IsFormed:
            return

        rsi_val = float(rsi_result.ToDecimal())

        k_val = stoch_value.K
        d_val = stoch_value.D
        if k_val is None or d_val is None:
            return
        k = float(k_val)
        d = float(d_val)

        if not self._initialized:
            self._prev_k = k
            self._prev_d = d
            self._initialized = True
            return

        # Sell when RSI overbought + stochastic K crosses below D in overbought zone
        sell_signal = (rsi_val > float(self.rsi_overbought) and
                       k > float(self.stoch_overbought) and
                       self._prev_k > self._prev_d and
                       k < d)

        # Buy when RSI oversold + stochastic K crosses above D in oversold zone
        buy_signal = (rsi_val < float(self.rsi_oversold) and
                      k < float(self.stoch_oversold) and
                      self._prev_k < self._prev_d and
                      k > d)

        if sell_signal and self.Position >= 0:
            if self.Position > 0:
                self.SellMarket()
            self.SellMarket()
        elif buy_signal and self.Position <= 0:
            if self.Position < 0:
                self.BuyMarket()
            self.BuyMarket()

        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return genie_stoch_rsi_strategy()
