import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage
from StockSharp.Algo.Strategies import Strategy


class break_revert_pro_strategy(Strategy):

    def __init__(self):
        super(break_revert_pro_strategy, self).__init__()

        self._risk_per_trade = self.Param("RiskPerTrade", 1.0) \
            .SetDisplay("Risk %", "Risk per trade as percentage of portfolio value", "Risk")
        self._lookback_period = self.Param("LookbackPeriod", 20) \
            .SetDisplay("Lookback", "Number of finished candles used for statistics", "Signals")
        self._breakout_threshold = self.Param("BreakoutThreshold", 0.1) \
            .SetDisplay("Breakout Threshold", "Minimum composite probability required for breakout entries", "Signals")
        self._mean_reversion_threshold = self.Param("MeanReversionThreshold", 0.6) \
            .SetDisplay("Reversion Threshold", "Maximum probability that still allows mean-reversion trades", "Signals")
        self._trade_delay_seconds = self.Param("TradeDelaySeconds", 300) \
            .SetDisplay("Trade Delay", "Minimum delay between consecutive entries (seconds)", "Risk")
        self._max_positions = self.Param("MaxPositions", 1) \
            .SetDisplay("Max Positions", "Maximum number of simultaneously open positions", "Risk")
        self._enable_safety_trade = self.Param("EnableSafetyTrade", True) \
            .SetDisplay("Safety Trade", "Allow protective trades", "Safety")
        self._safety_trade_interval_seconds = self.Param("SafetyTradeIntervalSeconds", 900) \
            .SetDisplay("Safety Interval", "Delay between safety trade checks (seconds)", "Safety")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Primary Candles", "Primary timeframe for signal generation", "Data")

        self._m1_atr = None
        self._m1_trend_sma = None

        self._poisson_probability = 0.5
        self._weibull_probability = 0.5
        self._exponential_probability = 0.5
        self._m1_trend = 0.0
        self._m15_trend = 0.0
        self._h1_trend = 0.0
        self._h1_volatility = 0.0
        self._previous_m1_close = None
        self._latest_atr = 0.0
        self._last_trade_time = None
        self._last_safety_check = None
        self._safety_trade_sent = False

        # manual rolling buffers for event_frequency (SMA) and volatility (EMA)
        self._event_buffer = []
        self._vol_ema = None
        self._vol_ema_k = 0.0
        self._lookback = 20

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def RiskPerTrade(self):
        return self._risk_per_trade.Value

    @property
    def LookbackPeriod(self):
        return self._lookback_period.Value

    @property
    def BreakoutThreshold(self):
        return self._breakout_threshold.Value

    @property
    def MeanReversionThreshold(self):
        return self._mean_reversion_threshold.Value

    @property
    def TradeDelaySeconds(self):
        return self._trade_delay_seconds.Value

    @property
    def MaxPositions(self):
        return self._max_positions.Value

    @property
    def EnableSafetyTrade(self):
        return self._enable_safety_trade.Value

    @property
    def SafetyTradeIntervalSeconds(self):
        return self._safety_trade_interval_seconds.Value

    def OnReseted(self):
        super(break_revert_pro_strategy, self).OnReseted()
        self._m1_atr = None
        self._m1_trend_sma = None
        self._poisson_probability = 0.5
        self._weibull_probability = 0.5
        self._exponential_probability = 0.5
        self._m1_trend = 0.0
        self._m15_trend = 0.0
        self._h1_trend = 0.0
        self._h1_volatility = 0.0
        self._previous_m1_close = None
        self._latest_atr = 0.0
        self._last_trade_time = None
        self._last_safety_check = None
        self._safety_trade_sent = False
        self._event_buffer = []
        self._vol_ema = None

    def OnStarted2(self, time):
        super(break_revert_pro_strategy, self).OnStarted2(time)

        self._lookback = max(1, self.LookbackPeriod)

        self._m1_atr = AverageTrueRange()
        self._m1_atr.Length = self._lookback

        self._m1_trend_sma = SimpleMovingAverage()
        self._m1_trend_sma.Length = self._lookback

        self._m15_trend_sma = SimpleMovingAverage()
        self._m15_trend_sma.Length = self._lookback

        self._h1_trend_sma = SimpleMovingAverage()
        self._h1_trend_sma.Length = self._lookback

        # EMA multiplier for volatility EMA
        self._vol_ema_k = 2.0 / (self._lookback + 1.0)
        self._vol_ema = None
        self._event_buffer = []

        # Primary candle subscription with ATR and trend SMA bound
        sub1 = self.SubscribeCandles(self.CandleType)
        sub1.Bind(self._m1_atr, self._m1_trend_sma, self._process_primary_candle).Start()

        # M15 subscription with trend SMA bound
        sub15 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        sub15.Bind(self._m15_trend_sma, self._process_m15_candle).Start()

        # H1 subscription with trend SMA bound
        sub_h1 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(1)))
        sub_h1.Bind(self._h1_trend_sma, self._process_h1_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _get_pip_size(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            s = float(self.Security.PriceStep)
            if s > 0:
                return s
        return 0.0001

    def _clamp(self, value, lo, hi):
        if value < lo:
            return lo
        if value > hi:
            return hi
        return value

    def _process_primary_candle(self, candle, atr_value, trend_sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._latest_atr = float(atr_value)
        close = float(candle.ClosePrice)
        pip = self._get_pip_size()

        # M1 trend: close - SMA(close)
        if self._m1_trend_sma is not None and self._m1_trend_sma.IsFormed:
            self._m1_trend = close - float(trend_sma_value)

        if self._previous_m1_close is not None:
            move = abs(close - self._previous_m1_close)

            # event_frequency: manual SMA of event values
            event_value = 1.0 if move >= pip * 5.0 else 0.0
            self._event_buffer.append(event_value)
            if len(self._event_buffer) > self._lookback:
                self._event_buffer.pop(0)
            if len(self._event_buffer) >= self._lookback:
                avg = sum(self._event_buffer) / len(self._event_buffer)
                self._poisson_probability = self._clamp(avg, 0.0, 1.0)

            # volatility EMA: manual EMA of move
            if self._vol_ema is None:
                self._vol_ema = move
            else:
                self._vol_ema = move * self._vol_ema_k + self._vol_ema * (1.0 - self._vol_ema_k)
            normalized = self._vol_ema / (pip * 10.0) if pip > 0 else 0.0
            self._exponential_probability = self._clamp(normalized, 0.0, 1.0)

        self._previous_m1_close = close

        normalized_atr = self._latest_atr / (pip * 10.0) if pip > 0 else 0.0
        self._weibull_probability = self._clamp(normalized_atr, 0.0, 1.0)

        self._evaluate_signals(candle)

    def _process_m15_candle(self, candle, trend_sma_value):
        if candle.State != CandleStates.Finished:
            return
        if self._m15_trend_sma is not None and self._m15_trend_sma.IsFormed:
            self._m15_trend = float(candle.ClosePrice) - float(trend_sma_value)

    def _process_h1_candle(self, candle, trend_sma_value):
        if candle.State != CandleStates.Finished:
            return

        self._h1_volatility = float(candle.HighPrice) - float(candle.LowPrice)

        if self._h1_trend_sma is not None and self._h1_trend_sma.IsFormed:
            self._h1_trend = float(candle.ClosePrice) - float(trend_sma_value)

    def _evaluate_signals(self, candle):
        now = candle.CloseTime

        if self._last_trade_time is not None:
            diff = now.Subtract(self._last_trade_time)
            if diff.TotalSeconds < self.TradeDelaySeconds:
                return

        if self.Position != 0:
            return

        breakout = self._m1_trend > 0 and (self._poisson_probability >= self.BreakoutThreshold or self._weibull_probability >= self.BreakoutThreshold)
        reversion = self._m1_trend < 0 and (self._weibull_probability <= self.MeanReversionThreshold or self._poisson_probability <= self.MeanReversionThreshold)

        if breakout:
            self.BuyMarket()
            self._last_trade_time = now
        elif reversion:
            self.SellMarket()
            self._last_trade_time = now

    def CreateClone(self):
        return break_revert_pro_strategy()
