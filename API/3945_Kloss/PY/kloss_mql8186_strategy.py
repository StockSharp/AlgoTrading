import clr
clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, UnitTypes, Unit
from StockSharp.Algo.Strategies import Strategy
from StockSharp.Algo.Indicators import CommodityChannelIndex, StochasticOscillator, CandleIndicatorValue

class kloss_mql8186_strategy(Strategy):
    def __init__(self):
        super(kloss_mql8186_strategy, self).__init__()

        self._cci_period = self.Param("CciPeriod", 10) \
            .SetDisplay("CCI Period", "Number of candles for the CCI calculation", "Indicators")
        self._cci_threshold = self.Param("CciThreshold", 150.0) \
            .SetDisplay("CCI Threshold", "Absolute CCI level that triggers entries", "Indicators")
        self._stochastic_k_period = self.Param("StochasticKPeriod", 5) \
            .SetDisplay("Stochastic %K", "Period of the %K line", "Indicators")
        self._stochastic_d_period = self.Param("StochasticDPeriod", 3) \
            .SetDisplay("Stochastic %D", "SMA length of the %D line", "Indicators")
        self._stochastic_smooth = self.Param("StochasticSmooth", 3) \
            .SetDisplay("Stochastic Smoothing", "Smoothing applied to the %K calculation", "Indicators")
        self._stochastic_oversold = self.Param("StochasticOversold", 45.0) \
            .SetDisplay("Stochastic Oversold", "Threshold under which %K confirms a long signal", "Signals")
        self._stochastic_overbought = self.Param("StochasticOverbought", 55.0) \
            .SetDisplay("Stochastic Overbought", "Threshold above which %K confirms a short signal", "Signals")
        self._stop_loss_points = self.Param("StopLossPoints", 48.0) \
            .SetDisplay("Stop Loss (pts)", "Stop loss distance expressed in price points", "Risk")
        self._take_profit_points = self.Param("TakeProfitPoints", 152.0) \
            .SetDisplay("Take Profit (pts)", "Take profit distance expressed in price points", "Risk")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe used for indicator calculations", "General")

        self._cci = None
        self._stochastic = None
        self._previous_open = None
        self._previous_close = None
        self._typical_history = [None] * 5

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def CciThreshold(self):
        return self._cci_threshold.Value

    @CciThreshold.setter
    def CciThreshold(self, value):
        self._cci_threshold.Value = value

    @property
    def StochasticKPeriod(self):
        return self._stochastic_k_period.Value

    @StochasticKPeriod.setter
    def StochasticKPeriod(self, value):
        self._stochastic_k_period.Value = value

    @property
    def StochasticDPeriod(self):
        return self._stochastic_d_period.Value

    @StochasticDPeriod.setter
    def StochasticDPeriod(self, value):
        self._stochastic_d_period.Value = value

    @property
    def StochasticSmooth(self):
        return self._stochastic_smooth.Value

    @StochasticSmooth.setter
    def StochasticSmooth(self, value):
        self._stochastic_smooth.Value = value

    @property
    def StochasticOversold(self):
        return self._stochastic_oversold.Value

    @StochasticOversold.setter
    def StochasticOversold(self, value):
        self._stochastic_oversold.Value = value

    @property
    def StochasticOverbought(self):
        return self._stochastic_overbought.Value

    @StochasticOverbought.setter
    def StochasticOverbought(self, value):
        self._stochastic_overbought.Value = value

    @property
    def StopLossPoints(self):
        return self._stop_loss_points.Value

    @StopLossPoints.setter
    def StopLossPoints(self, value):
        self._stop_loss_points.Value = value

    @property
    def TakeProfitPoints(self):
        return self._take_profit_points.Value

    @TakeProfitPoints.setter
    def TakeProfitPoints(self, value):
        self._take_profit_points.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(kloss_mql8186_strategy, self).OnStarted(time)

        self._cci = CommodityChannelIndex()
        self._cci.Length = self.CciPeriod
        self._stochastic = StochasticOscillator()
        self._stochastic.K.Length = self.StochasticKPeriod
        self._stochastic.D.Length = self.StochasticDPeriod

        self._previous_open = None
        self._previous_close = None
        self._typical_history = [None] * 5

        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(self.ProcessCandle).Start()

        tp_points = float(self.TakeProfitPoints)
        sl_points = float(self.StopLossPoints)

        tp_unit = Unit(tp_points, UnitTypes.Absolute) if tp_points > 0 else Unit(0, UnitTypes.Absolute)
        sl_unit = Unit(sl_points, UnitTypes.Absolute) if sl_points > 0 else Unit(0, UnitTypes.Absolute)

        self.StartProtection(takeProfit=tp_unit, stopLoss=sl_unit)

    def ProcessCandle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        cci_input = CandleIndicatorValue(self._cci, candle)
        cci_input.IsFinal = True
        cci_result = self._cci.Process(cci_input)
        stoch_input = CandleIndicatorValue(self._stochastic, candle)
        stoch_input.IsFinal = True
        stoch_result = self._stochastic.Process(stoch_input)

        self._update_history(candle)

        if not cci_result.IsFormed:
            return

        if not self._stochastic.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        cci_val = float(cci_result)
        stoch_k_val = float(stoch_result.K) if stoch_result.K is not None else None
        if stoch_k_val is None:
            return

        if self._previous_open is not None and self._previous_close is not None and self._typical_history[4] is not None:
            prev_open = self._previous_open
            prev_close = self._previous_close
            shifted_typical = self._typical_history[4]

            buy_signal = cci_val <= -float(self.CciThreshold) and stoch_k_val < float(self.StochasticOversold) and prev_open > shifted_typical
            sell_signal = cci_val >= float(self.CciThreshold) and stoch_k_val > float(self.StochasticOverbought) and prev_close < shifted_typical

            if buy_signal and self.Position <= 0:
                self.BuyMarket()
            elif sell_signal and self.Position >= 0:
                self.SellMarket()

    def _update_history(self, candle):
        for i in range(len(self._typical_history) - 1, 0, -1):
            self._typical_history[i] = self._typical_history[i - 1]

        self._typical_history[0] = (float(candle.HighPrice) + float(candle.LowPrice) + float(candle.ClosePrice)) / 3.0

        self._previous_open = float(candle.OpenPrice)
        self._previous_close = float(candle.ClosePrice)

    def OnReseted(self):
        super(kloss_mql8186_strategy, self).OnReseted()
        self._previous_open = None
        self._previous_close = None
        self._typical_history = [None] * 5

    def CreateClone(self):
        return kloss_mql8186_strategy()
