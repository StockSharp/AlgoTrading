import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, ExponentialMovingAverage, DecimalIndicatorValue
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
        self._trade_delay_seconds = self.Param("TradeDelaySeconds", 86400) \
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
        self._m1_trend_average = None
        self._m15_trend_average = None
        self._h1_trend_average = None
        self._event_frequency = None
        self._volatility_ema = None

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
        self._m1_trend_average = None
        self._m15_trend_average = None
        self._h1_trend_average = None
        self._event_frequency = None
        self._volatility_ema = None
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

    def OnStarted2(self, time):
        super(break_revert_pro_strategy, self).OnStarted2(time)

        lookback = max(1, self.LookbackPeriod)

        self._m1_atr = AverageTrueRange()
        self._m1_atr.Length = lookback
        self._m1_trend_average = SimpleMovingAverage()
        self._m1_trend_average.Length = lookback
        self._m15_trend_average = SimpleMovingAverage()
        self._m15_trend_average.Length = lookback
        self._h1_trend_average = SimpleMovingAverage()
        self._h1_trend_average.Length = lookback
        self._event_frequency = SimpleMovingAverage()
        self._event_frequency.Length = lookback
        self._volatility_ema = ExponentialMovingAverage()
        self._volatility_ema.Length = lookback

        sub1 = self.SubscribeCandles(self.CandleType)
        sub1.Bind(self._m1_atr, self._process_primary_candle).Start()

        sub15 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromMinutes(15)))
        sub15.Bind(self._process_m15_candle).Start()

        sub_h1 = self.SubscribeCandles(DataType.TimeFrame(TimeSpan.FromHours(1)))
        sub_h1.Bind(self._process_h1_candle).Start()

        self.StartProtection(
            takeProfit=Unit(2, UnitTypes.Percent),
            stopLoss=Unit(1, UnitTypes.Percent))

    def _get_pip_size(self):
        if self.Security is not None and self.Security.PriceStep is not None:
            s = float(self.Security.PriceStep)
            if s > 0:
                return s
        return 0.0001

    def _process_primary_candle(self, candle, atr_value):
        if candle.State != CandleStates.Finished:
            return

        self._latest_atr = float(atr_value)
        close = float(candle.ClosePrice)
        ctime = candle.CloseTime
        pip = self._get_pip_size()

        if self._m1_trend_average is not None:
            trend_val = self._m1_trend_average.Process(
                DecimalIndicatorValue(self._m1_trend_average, candle.ClosePrice, ctime))
            tv = float(trend_val)
            if self._m1_trend_average.IsFormed:
                self._m1_trend = close - tv

        if self._previous_m1_close is not None:
            move = abs(close - self._previous_m1_close)
            event_value = 1.0 if move >= pip * 5.0 else 0.0

            if self._event_frequency is not None:
                from decimal import Decimal
                avg_result = self._event_frequency.Process(
                    DecimalIndicatorValue(self._event_frequency, event_value, ctime))
                avg = float(avg_result)
                if self._event_frequency.IsFormed:
                    self._poisson_probability = max(0.0, min(1.0, avg))

            if self._volatility_ema is not None:
                ema_result = self._volatility_ema.Process(
                    DecimalIndicatorValue(self._volatility_ema, move, ctime))
                ema = float(ema_result)
                if self._volatility_ema.IsFormed:
                    normalized = ema / (pip * 10.0) if pip > 0 else 0.0
                    self._exponential_probability = max(0.0, min(1.0, normalized))

        self._previous_m1_close = close

        normalized_atr = self._latest_atr / (pip * 10.0) if pip > 0 else 0.0
        self._weibull_probability = max(0.0, min(1.0, normalized_atr))

        self._evaluate_signals(candle)

    def _process_m15_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return
        if self._m15_trend_average is None:
            return

        close = float(candle.ClosePrice)
        trend_val = self._m15_trend_average.Process(
            DecimalIndicatorValue(self._m15_trend_average, candle.ClosePrice, candle.CloseTime))
        tv = float(trend_val)
        if self._m15_trend_average.IsFormed:
            self._m15_trend = close - tv

    def _process_h1_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        self._h1_volatility = float(candle.HighPrice) - float(candle.LowPrice)

        if self._h1_trend_average is None:
            return

        close = float(candle.ClosePrice)
        trend_val = self._h1_trend_average.Process(
            DecimalIndicatorValue(self._h1_trend_average, candle.ClosePrice, candle.CloseTime))
        tv = float(trend_val)
        if self._h1_trend_average.IsFormed:
            self._h1_trend = close - tv

    def _evaluate_signals(self, candle):
        now = candle.CloseTime

        if self._last_trade_time is not None:
            diff = now - self._last_trade_time
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
