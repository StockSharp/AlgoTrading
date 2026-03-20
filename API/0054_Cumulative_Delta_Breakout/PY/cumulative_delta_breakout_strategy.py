import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class cumulative_delta_breakout_strategy(Strategy):
    """
    Cumulative Delta Breakout strategy.
    Estimates delta from candle direction and volume.
    Long: Cumulative delta rising and price above SMA.
    Short: Cumulative delta falling and price below SMA.
    """

    def __init__(self):
        super(cumulative_delta_breakout_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cumulative_delta = 0.0
        self._prev_delta = 0.0
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(cumulative_delta_breakout_strategy, self).OnReseted()
        self._cumulative_delta = 0.0
        self._prev_delta = 0.0
        self._cooldown = 0

    def OnStarted(self, time):
        super(cumulative_delta_breakout_strategy, self).OnStarted(time)

        self._cumulative_delta = 0.0
        self._prev_delta = 0.0
        self._cooldown = 0

        sma = SimpleMovingAverage()
        sma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(sma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, sma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, sma_val):
        if candle.State != CandleStates.Finished:
            return
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # Estimate delta from candle: bullish adds volume, bearish subtracts
        vol = float(candle.TotalVolume)
        if float(candle.ClosePrice) >= float(candle.OpenPrice):
            delta = vol
        else:
            delta = -vol
        self._cumulative_delta += delta

        if self._prev_delta == 0:
            self._prev_delta = self._cumulative_delta
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            self._prev_delta = self._cumulative_delta
            return

        delta_rising = self._cumulative_delta > self._prev_delta
        close = float(candle.ClosePrice)
        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0:
            if delta_rising and close > sv:
                self.BuyMarket()
                self._cooldown = cd
            elif not delta_rising and close < sv:
                self.SellMarket()
                self._cooldown = cd
        elif self.Position > 0 and not delta_rising:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and delta_rising:
            self.BuyMarket()
            self._cooldown = cd

        self._prev_delta = self._cumulative_delta

    def CreateClone(self):
        return cumulative_delta_breakout_strategy()
