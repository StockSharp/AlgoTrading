import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import Momentum, DecimalIndicatorValue
from StockSharp.Algo.Strategies import Strategy


class momentum_candle_sign_strategy(Strategy):
    def __init__(self):
        super(momentum_candle_sign_strategy, self).__init__()
        self._momentum_period = self.Param("MomentumPeriod", 12) \
            .SetDisplay("Momentum Period", "Indicator period", "General")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Time frame of candles", "General")
        self._open_momentum = None
        self._close_momentum = None
        self._prev_open_momentum = 0.0
        self._prev_close_momentum = 0.0
        self._is_formed = False

    @property
    def momentum_period(self):
        return self._momentum_period.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(momentum_candle_sign_strategy, self).OnReseted()
        self._open_momentum = None
        self._close_momentum = None
        self._prev_open_momentum = 0.0
        self._prev_close_momentum = 0.0
        self._is_formed = False

    def OnStarted(self, time):
        super(momentum_candle_sign_strategy, self).OnStarted(time)
        self._open_momentum = Momentum()
        self._open_momentum.Length = self.momentum_period
        self._close_momentum = Momentum()
        self._close_momentum.Length = self.momentum_period
        self._is_formed = False
        self.Indicators.Add(self._open_momentum)
        self.Indicators.Add(self._close_momentum)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._open_momentum)
            self.DrawIndicator(area, self._close_momentum)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        open_input = DecimalIndicatorValue(self._open_momentum, candle.OpenPrice, candle.OpenTime)
        open_input.IsFinal = True
        open_mom = float(self._open_momentum.Process(open_input))
        close_input = DecimalIndicatorValue(self._close_momentum, candle.ClosePrice, candle.OpenTime)
        close_input.IsFinal = True
        close_mom = float(self._close_momentum.Process(close_input))
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        if not self._is_formed:
            self._prev_open_momentum = open_mom
            self._prev_close_momentum = close_mom
            self._is_formed = True
            return
        buy_signal = self._prev_open_momentum >= self._prev_close_momentum and open_mom < close_mom
        sell_signal = self._prev_open_momentum <= self._prev_close_momentum and open_mom > close_mom
        if buy_signal and self.Position <= 0:
            self.BuyMarket()
        elif sell_signal and self.Position >= 0:
            self.SellMarket()
        self._prev_open_momentum = open_mom
        self._prev_close_momentum = close_mom

    def CreateClone(self):
        return momentum_candle_sign_strategy()
