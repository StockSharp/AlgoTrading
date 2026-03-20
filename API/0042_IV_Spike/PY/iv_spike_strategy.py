import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, StandardDeviation
from StockSharp.Algo.Strategies import Strategy

class iv_spike_strategy(Strategy):
    """
    IV Spike strategy based on implied volatility spikes.
    Enters long when IV increases above threshold and price is below MA,
    or short when IV increases and price is above MA.
    """

    def __init__(self):
        super(iv_spike_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators")
        self._iv_period = self.Param("IVPeriod", 20).SetDisplay("IV Period", "Period for volatility calculation", "Indicators")
        self._iv_spike_threshold = self.Param("IVSpikeThreshold", 1.5).SetDisplay("IV Spike Threshold", "Minimum IV increase multiplier", "Entry")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._previous_iv = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(iv_spike_strategy, self).OnReseted()
        self._previous_iv = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(iv_spike_strategy, self).OnStarted(time)

        self._previous_iv = 0.0
        self._cooldown = 0

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value
        hv = StandardDeviation()
        hv.Length = self._iv_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, hv, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val, iv_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        iv = float(iv_val)

        if self._previous_iv == 0 and iv > 0:
            self._previous_iv = iv
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._previous_iv = iv
            return

        iv_change = iv / self._previous_iv if self._previous_iv > 0 else 0.0
        close = float(candle.ClosePrice)
        mv = float(ma_val)
        threshold = float(self._iv_spike_threshold.Value)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and iv_change >= threshold:
            if close < mv:
                self.BuyMarket()
                self._cooldown = cd
            elif close > mv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and iv < self._previous_iv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and iv < self._previous_iv:
            self.BuyMarket()
            self._cooldown = cd

        self._previous_iv = iv

    def CreateClone(self):
        return iv_spike_strategy()
