import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import StochasticOscillator
from StockSharp.Algo.Strategies import Strategy

class martingail_expert_sequence_strategy(Strategy):
    """
    Martingale Expert Sequence: stochastic K/D crossover with take profit.
    """

    def __init__(self):
        super(martingail_expert_sequence_strategy, self).__init__()
        self._k_period = self.Param("KPeriod", 14).SetDisplay("K Period", "Stochastic K", "Indicators")
        self._d_period = self.Param("DPeriod", 3).SetDisplay("D Period", "Stochastic D", "Indicators")
        self._take_profit_points = self.Param("TakeProfitPoints", 200.0).SetDisplay("Take Profit", "TP in price steps", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))).SetDisplay("Candle Type", "Candles", "General")

        self._prev_k = None
        self._prev_d = None
        self._entry_price = 0.0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(martingail_expert_sequence_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None
        self._entry_price = 0.0

    def OnStarted2(self, time):
        super(martingail_expert_sequence_strategy, self).OnStarted2(time)
        stoch = StochasticOscillator()
        stoch.K.Length = self._k_period.Value
        stoch.D.Length = self._d_period.Value
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(stoch, self._process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, stoch)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, stoch_value):
        if candle.State != CandleStates.Finished:
            return
        k_val = stoch_value.K
        d_val = stoch_value.D
        if k_val is None or d_val is None:
            return
        k = float(k_val)
        d = float(d_val)
        close = float(candle.ClosePrice)
        step = float(self.Security.PriceStep) if self.Security is not None and self.Security.PriceStep is not None else 1.0
        tp = float(self._take_profit_points.Value)
        if self.Position > 0 and close >= self._entry_price + tp * step:
            self.SellMarket()
            self._prev_k = k
            self._prev_d = d
            return
        elif self.Position < 0 and close <= self._entry_price - tp * step:
            self.BuyMarket()
            self._prev_k = k
            self._prev_d = d
            return
        if self._prev_k is None or self._prev_d is None:
            self._prev_k = k
            self._prev_d = d
            return
        bull_cross = self._prev_k <= self._prev_d and k > d
        bear_cross = self._prev_k >= self._prev_d and k < d
        if self.Position == 0:
            if bull_cross:
                self.BuyMarket()
                self._entry_price = close
            elif bear_cross:
                self.SellMarket()
                self._entry_price = close
        elif self.Position > 0 and bear_cross:
            self.SellMarket()
            self.SellMarket()
            self._entry_price = close
        elif self.Position < 0 and bull_cross:
            self.BuyMarket()
            self.BuyMarket()
            self._entry_price = close
        self._prev_k = k
        self._prev_d = d

    def CreateClone(self):
        return martingail_expert_sequence_strategy()
