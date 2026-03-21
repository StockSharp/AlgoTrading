import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy

class ma_stochastic_strategy(Strategy):
    """
    MA + manual Stochastic strategy.
    Enters when price is above MA and Stochastic oversold (longs)
    or below MA and Stochastic overbought (shorts).
    """

    def __init__(self):
        super(ma_stochastic_strategy, self).__init__()
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._ma_period = self.Param("MaPeriod", 20).SetDisplay("MA Period", "Period of the Moving Average", "Indicators")
        self._stoch_period = self.Param("StochPeriod", 14).SetDisplay("Stochastic Period", "Period for %K calculation", "Indicators")
        self._stoch_oversold = self.Param("StochOversold", 20.0).SetDisplay("Stochastic Oversold", "Level considered oversold", "Indicators")
        self._stoch_overbought = self.Param("StochOverbought", 80.0).SetDisplay("Stochastic Overbought", "Level considered overbought", "Indicators")
        self._cooldown_bars = self.Param("CooldownBars", 100).SetDisplay("Cooldown Bars", "Bars between trades", "General")

        self._cooldown = 0
        self._highs = []
        self._lows = []
        self._closes = []

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(ma_stochastic_strategy, self).OnReseted()
        self._cooldown = 0
        self._highs = []
        self._lows = []
        self._closes = []

    def OnStarted(self, time):
        super(ma_stochastic_strategy, self).OnStarted(time)

        self._cooldown = 0
        self._highs = []
        self._lows = []
        self._closes = []

        ma = SimpleMovingAverage()
        ma.Length = self._ma_period.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.Bind(ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, ma_val):
        if candle.State != CandleStates.Finished:
            return

        high = float(candle.HighPrice)
        low = float(candle.LowPrice)
        close = float(candle.ClosePrice)
        ma = float(ma_val)
        cd = self._cooldown_bars.Value
        sp = self._stoch_period.Value
        oversold = float(self._stoch_oversold.Value)
        overbought = float(self._stoch_overbought.Value)

        # Track highs, lows, closes for manual stochastic
        self._highs.append(high)
        self._lows.append(low)
        self._closes.append(close)

        # Keep buffers manageable
        max_buf = sp * 2
        if len(self._highs) > max_buf:
            self._highs = self._highs[-max_buf:]
            self._lows = self._lows[-max_buf:]
            self._closes = self._closes[-max_buf:]

        if len(self._highs) < sp:
            return

        # Calculate %K manually
        recent_h = self._highs[-sp:]
        recent_l = self._lows[-sp:]
        hh = max(recent_h)
        ll = min(recent_l)
        diff = hh - ll
        if diff == 0:
            return

        stoch_k = 100.0 * (close - ll) / diff

        if self._cooldown > 0:
            self._cooldown -= 1
            return

        # Long: price above MA + Stochastic oversold
        if close > ma and stoch_k < oversold and self.Position == 0:
            self.BuyMarket()
            self._cooldown = cd
        # Short: price below MA + Stochastic overbought
        elif close < ma and stoch_k > overbought and self.Position == 0:
            self.SellMarket()
            self._cooldown = cd

        # Exit long: price below MA
        if self.Position > 0 and close < ma:
            self.SellMarket()
            self._cooldown = cd
        # Exit short: price above MA
        elif self.Position < 0 and close > ma:
            self.BuyMarket()
            self._cooldown = cd

    def CreateClone(self):
        return ma_stochastic_strategy()
