import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageDirectionalIndex, StochasticOscillator
from StockSharp.Algo.Strategies import Strategy


class accelerator_bot_usd_jpy_h4_strategy(Strategy):
    def __init__(self):
        super(accelerator_bot_usd_jpy_h4_strategy, self).__init__()
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators")
        self._adx_threshold = self.Param("AdxThreshold", 20.0) \
            .SetDisplay("ADX Threshold", "Minimum ADX to use trend rules", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for calculations", "General")
        self._prev_k = None
        self._prev_d = None

    @property
    def adx_period(self):
        return self._adx_period.Value

    @property
    def adx_threshold(self):
        return self._adx_threshold.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(accelerator_bot_usd_jpy_h4_strategy, self).OnReseted()
        self._prev_k = None
        self._prev_d = None

    def OnStarted(self, time):
        super(accelerator_bot_usd_jpy_h4_strategy, self).OnStarted(time)
        self._prev_k = None
        self._prev_d = None
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        stochastic = StochasticOscillator()
        stochastic.K.Length = 8
        stochastic.D.Length = 3
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(adx, stochastic, self.process_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)
            self.DrawIndicator(area, stochastic)
            self.DrawOwnTrades(area)

    def process_candle(self, candle, adx_val, stoch_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return
        adx_ma = adx_val.MovingAverage
        if adx_ma is None:
            return
        adx_ma = float(adx_ma)
        stoch_k = stoch_val.K
        stoch_d = stoch_val.D
        if stoch_k is None or stoch_d is None:
            return
        stoch_k = float(stoch_k)
        stoch_d = float(stoch_d)
        if self._prev_k is None or self._prev_d is None:
            self._prev_k = stoch_k
            self._prev_d = stoch_d
            return
        bull_cross = self._prev_k <= self._prev_d and stoch_k > stoch_d
        bear_cross = self._prev_k >= self._prev_d and stoch_k < stoch_d
        adx_threshold = float(self.adx_threshold)
        if adx_ma > adx_threshold:
            if float(candle.ClosePrice) > float(candle.OpenPrice) and bull_cross and self.Position <= 0:
                self.BuyMarket()
            elif float(candle.ClosePrice) < float(candle.OpenPrice) and bear_cross and self.Position >= 0:
                self.SellMarket()
        else:
            if bull_cross and self.Position <= 0:
                self.BuyMarket()
            elif bear_cross and self.Position >= 0:
                self.SellMarket()
        self._prev_k = stoch_k
        self._prev_d = stoch_d

    def CreateClone(self):
        return accelerator_bot_usd_jpy_h4_strategy()
