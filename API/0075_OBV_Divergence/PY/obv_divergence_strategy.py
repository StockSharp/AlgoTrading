import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class obv_divergence_strategy(Strategy):
    """
    OBV (On-Balance Volume) Divergence strategy.
    Tracks OBV direction vs price direction over a lookback window.
    Bullish divergence: price trending down but OBV trending up.
    Bearish divergence: price trending up but OBV trending down.
    Uses SMA for exit signals.
    """

    def __init__(self):
        super(obv_divergence_strategy, self).__init__()
        self._ma_period = self.Param("MAPeriod", 20).SetDisplay("MA Period", "Period for SMA exit signal", "Indicators")
        self._lookback = self.Param("Lookback", 10).SetDisplay("Lookback", "Lookback period for divergence detection", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._cooldown_bars = self.Param("CooldownBars", 500).SetDisplay("Cooldown Bars", "Bars to wait between trades", "General")

        self._cumulative_obv = 0.0
        self._prev_close = 0.0
        self._price_history = []
        self._obv_history = []
        self._cooldown = 0

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(obv_divergence_strategy, self).OnReseted()
        self._cumulative_obv = 0.0
        self._prev_close = 0.0
        self._price_history = []
        self._obv_history = []
        self._cooldown = 0

    def OnStarted2(self, time):
        super(obv_divergence_strategy, self).OnStarted2(time)

        self._cumulative_obv = 0.0
        self._prev_close = 0.0
        self._price_history = []
        self._obv_history = []
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

        close = float(candle.ClosePrice)
        vol = float(candle.TotalVolume)

        # Calculate OBV manually
        if self._prev_close > 0:
            if close > self._prev_close:
                self._cumulative_obv += vol
            elif close < self._prev_close:
                self._cumulative_obv -= vol
        self._prev_close = close

        # Store history
        self._price_history.append(close)
        self._obv_history.append(self._cumulative_obv)

        lb = self._lookback.Value

        # Keep only what we need
        if len(self._price_history) > lb + 1:
            self._price_history.pop(0)
            self._obv_history.pop(0)

        if len(self._price_history) <= lb:
            return

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Compare current values to lookback-period-ago values
        price_change = self._price_history[-1] - self._price_history[0]
        obv_change = self._obv_history[-1] - self._obv_history[0]

        # Bullish divergence: price down but OBV up
        bullish_div = price_change < 0 and obv_change > 0
        # Bearish divergence: price up but OBV down
        bearish_div = price_change > 0 and obv_change < 0

        sv = float(sma_val)
        cd = self._cooldown_bars.Value

        if self.Position == 0 and bullish_div:
            self.BuyMarket()
            self._cooldown = cd
        elif self.Position == 0 and bearish_div:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position > 0 and close < sv:
            self.SellMarket()
            self._cooldown = cd
        elif self.Position < 0 and close > sv:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return obv_divergence_strategy()
