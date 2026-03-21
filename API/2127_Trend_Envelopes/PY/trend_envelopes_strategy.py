import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import ExponentialMovingAverage, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy


class trend_envelopes_strategy(Strategy):
    def __init__(self):
        super(trend_envelopes_strategy, self).__init__()
        self._ma_period = self.Param("MaPeriod", 14) \
            .SetDisplay("MA Period", "Moving average length", "Indicator")
        self._deviation = self.Param("Deviation", 0.2) \
            .SetDisplay("Deviation", "Percent offset for envelopes", "Indicator")
        self._atr_period = self.Param("AtrPeriod", 15) \
            .SetDisplay("ATR Period", "ATR calculation length", "Indicator")
        self._atr_sensitivity = self.Param("AtrSensitivity", 0.5) \
            .SetDisplay("ATR Sensitivity", "Multiplier for signal shift", "Indicator")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(4))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")
        self._buy_entry = self.Param("BuyEntry", True) \
            .SetDisplay("Enable Long Entry", "Allow opening long positions", "Trading")
        self._sell_entry = self.Param("SellEntry", True) \
            .SetDisplay("Enable Short Entry", "Allow opening short positions", "Trading")
        self._buy_exit = self.Param("BuyExit", True) \
            .SetDisplay("Enable Long Exit", "Allow closing long positions", "Trading")
        self._sell_exit = self.Param("SellExit", True) \
            .SetDisplay("Enable Short Exit", "Allow closing short positions", "Trading")
        self._take_profit = self.Param("TakeProfit", 2000) \
            .SetDisplay("Take Profit", "Target in points", "Protection")
        self._stop_loss = self.Param("StopLoss", 1000) \
            .SetDisplay("Stop Loss", "Loss limit in points", "Protection")
        self._ma = None
        self._atr = None
        self._prev_smax = 0.0
        self._prev_smin = 0.0
        self._prev_trend = 0
        self._initialized = False

    @property
    def ma_period(self):
        return self._ma_period.Value

    @property
    def deviation(self):
        return self._deviation.Value

    @property
    def atr_period(self):
        return self._atr_period.Value

    @property
    def atr_sensitivity(self):
        return self._atr_sensitivity.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    @property
    def buy_entry(self):
        return self._buy_entry.Value

    @property
    def sell_entry(self):
        return self._sell_entry.Value

    @property
    def buy_exit(self):
        return self._buy_exit.Value

    @property
    def sell_exit(self):
        return self._sell_exit.Value

    @property
    def take_profit(self):
        return self._take_profit.Value

    @property
    def stop_loss(self):
        return self._stop_loss.Value

    def OnReseted(self):
        super(trend_envelopes_strategy, self).OnReseted()
        self._ma = None
        self._atr = None
        self._prev_smax = 0.0
        self._prev_smin = 0.0
        self._prev_trend = 0
        self._initialized = False

    def OnStarted(self, time):
        super(trend_envelopes_strategy, self).OnStarted(time)
        self._ma = ExponentialMovingAverage()
        self._ma.Length = self.ma_period
        self._atr = AverageTrueRange()
        self._atr.Length = self.atr_period
        self.Indicators.Add(self._ma)
        self.Indicators.Add(self._atr)
        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(self.process_candle).Start()
        step = self.Security.PriceStep
        if step is None or float(step) == 0:
            step = 1.0
        else:
            step = float(step)
        self.StartProtection(
            takeProfit=Unit(float(self.take_profit) * step, UnitTypes.Absolute),
            stopLoss=Unit(float(self.stop_loss) * step, UnitTypes.Absolute))
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._ma)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        ma_result = self._ma.Process(candle.ClosePrice, candle.OpenTime, True)
        atr_result = self._atr.Process(candle)
        if not ma_result.IsFormed or not atr_result.IsFormed:
            return
        ma_value = float(ma_result)
        atr_value = float(atr_result)
        dev = float(self.deviation)
        smax = (1.0 + dev / 100.0) * ma_value
        smin = (1.0 - dev / 100.0) * ma_value
        trend = self._prev_trend
        close = float(candle.ClosePrice)
        if self._initialized:
            if close > self._prev_smax:
                trend = 1
            if close < self._prev_smin:
                trend = -1
        if not self._initialized:
            self._prev_smax = smax
            self._prev_smin = smin
            self._prev_trend = 0
            self._initialized = True
            return
        up_signal = False
        down_signal = False
        up_trend = False
        down_trend = False
        atr_sens = float(self.atr_sensitivity)
        if trend > 0:
            if smin < self._prev_smin:
                smin = self._prev_smin
            up_trend = True
            if self._prev_trend <= 0:
                up_signal = True
        elif trend < 0:
            if smax > self._prev_smax:
                smax = self._prev_smax
            down_trend = True
            if self._prev_trend >= 0:
                down_signal = True
        self._prev_smax = smax
        self._prev_smin = smin
        self._prev_trend = trend
        if self.buy_exit and down_trend and self.Position > 0:
            self.SellMarket()
        if self.sell_exit and up_trend and self.Position < 0:
            self.BuyMarket()
        if self.buy_entry and up_signal and self.Position <= 0:
            self.BuyMarket()
        if self.sell_entry and down_signal and self.Position >= 0:
            self.SellMarket()

    def CreateClone(self):
        return trend_envelopes_strategy()
