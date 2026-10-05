import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import AverageTrueRange, SimpleMovingAverage, ExponentialMovingAverage, WeightedMovingAverage
from StockSharp.Algo.Strategies import Strategy


class mikuls_ichimoku_cloud_v2_strategy(Strategy):
    """
    Mikul's Ichimoku Cloud v2 strategy.
    Goes long when Tenkan-sen crosses above Kijun-sen with the close above the cloud, or when the close breaks above a green cloud,
    optionally only above a moving average. The long is protected by a trailing stop built from ATR or a percent below the recent
    swing low (or the close), and can also exit on an Ichimoku reversal and a percent take profit.
    """

    def __init__(self):
        super(mikuls_ichimoku_cloud_v2_strategy, self).__init__()
        self._trail_source = self.Param("TrailSource", "LowsHighs").SetDisplay("Trail Source", "Price the trailing stop is measured from: LowsHighs or Close", "Risk")
        self._trail_method = self.Param("TrailMethod", "Atr").SetDisplay("Trail Method", "Trailing stop method: Atr or Percent", "Risk")
        self._trail_percent = self.Param("TrailPercent", 10.0).SetGreaterThanZero().SetDisplay("Trail %", "Trailing distance in percent for the Percent method", "Risk")
        self._swing_lookback = self.Param("SwingLookback", 7).SetGreaterThanZero().SetDisplay("Swing Lookback", "Candles used for the swing low", "Risk")
        self._atr_period = self.Param("AtrPeriod", 14).SetGreaterThanZero().SetDisplay("ATR Period", "ATR period", "Risk")
        self._atr_multiplier = self.Param("AtrMultiplier", 1.0).SetNotNegative().SetDisplay("ATR Multiplier", "ATR multiplier for the trailing stop", "Risk")
        self._add_ichi_exit = self.Param("AddIchiExit", False).SetDisplay("Ichimoku Exit", "Exit on an Ichimoku reversal", "Risk")
        self._use_take_profit = self.Param("UseTakeProfit", False).SetDisplay("Use Take Profit", "Use the percent take profit", "Risk")
        self._take_profit_percent = self.Param("TakeProfitPercent", 25.0).SetGreaterThanZero().SetDisplay("Take Profit %", "Take profit in percent from the entry price", "Risk")
        self._use_ma_filter = self.Param("UseMaFilter", False).SetDisplay("Use MA Filter", "Require the close above the moving average", "Filters")
        self._ma_type = self.Param("MaType", "Ema").SetDisplay("MA Type", "Moving average type of the filter: Sma, Ema or Wma", "Filters")
        self._ma_length = self.Param("MaLength", 200).SetGreaterThanZero().SetDisplay("MA Length", "Moving average length of the filter", "Filters")
        self._tenkan_period = self.Param("TenkanPeriod", 9).SetGreaterThanZero().SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku")
        self._kijun_period = self.Param("KijunPeriod", 26).SetGreaterThanZero().SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku")
        self._senkou_b_period = self.Param("SenkouBPeriod", 52).SetGreaterThanZero().SetDisplay("Senkou B Period", "Senkou Span B period", "Ichimoku")
        self._displacement = self.Param("Displacement", 26).SetGreaterThanZero().SetDisplay("Displacement", "Forward shift of the cloud in candles", "Ichimoku")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromHours(1))).SetDisplay("Candle Type", "Type of candles to use", "General")
        self._reset_state()

    @property
    def candle_type(self):
        return self._candle_type.Value

    def _reset_state(self):
        self._highs = []
        self._lows = []
        self._senkou_a = []
        self._senkou_b = []
        self._prev_tenkan = None
        self._prev_kijun = None
        self._prev_close = None
        self._prev_cloud_top = None
        self._trail_stop = None
        self._entry_price = Decimal(0)

    def OnReseted(self):
        super(mikuls_ichimoku_cloud_v2_strategy, self).OnReseted()
        self._reset_state()

    def OnStarted2(self, time):
        super(mikuls_ichimoku_cloud_v2_strategy, self).OnStarted2(time)

        self._reset_state()

        atr = AverageTrueRange()
        atr.Length = self._atr_period.Value

        ma_type = str(self._ma_type.Value)
        if ma_type == "Sma":
            ma = SimpleMovingAverage()
        elif ma_type == "Wma":
            ma = WeightedMovingAverage()
        else:
            ma = ExponentialMovingAverage()
        ma.Length = self._ma_length.Value

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(atr, ma, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            if self._use_ma_filter.Value:
                self.DrawIndicator(area, ma)
            self.DrawOwnTrades(area)

    def _midpoint(self, length):
        return (max(self._highs[-length:]) + min(self._lows[-length:])) / Decimal(2)

    def _process_candle(self, candle, atr_value, ma_value):
        if candle.State != CandleStates.Finished:
            return

        max_length = max(self._swing_lookback.Value, self._tenkan_period.Value, self._kijun_period.Value, self._senkou_b_period.Value)

        self._highs.append(candle.HighPrice)
        self._lows.append(candle.LowPrice)
        if len(self._highs) > max_length:
            self._highs.pop(0)
            self._lows.pop(0)

        if len(self._highs) < max_length:
            return

        close = candle.ClosePrice
        tenkan = self._midpoint(self._tenkan_period.Value)
        kijun = self._midpoint(self._kijun_period.Value)

        displacement = self._displacement.Value
        self._senkou_a.append((tenkan + kijun) / Decimal(2))
        self._senkou_b.append(self._midpoint(self._senkou_b_period.Value))
        if len(self._senkou_a) > displacement:
            self._senkou_a.pop(0)
            self._senkou_b.pop(0)

        prev_tenkan = self._prev_tenkan
        prev_kijun = self._prev_kijun
        prev_close = self._prev_close
        prev_cloud_top = self._prev_cloud_top
        self._prev_tenkan = tenkan
        self._prev_kijun = kijun
        self._prev_close = close

        # The cloud plotted on this candle was calculated Displacement - 1 candles earlier.
        if len(self._senkou_a) < displacement or not atr_value.IsFormed or not ma_value.IsFormed:
            return

        cloud_a = self._senkou_a[0]
        cloud_b = self._senkou_b[0]
        cloud_top = max(cloud_a, cloud_b)
        cloud_bottom = min(cloud_a, cloud_b)
        self._prev_cloud_top = cloud_top

        atr = atr_value.GetValue[Decimal](None)
        ma = ma_value.GetValue[Decimal](None)

        if self.Position > 0:
            exit_now = self._trail_stop is not None and candle.LowPrice <= self._trail_stop

            if not exit_now and self._use_take_profit.Value and \
                    candle.HighPrice >= self._entry_price * (Decimal(1) + Decimal(self._take_profit_percent.Value) / Decimal(100)):
                exit_now = True

            if not exit_now and self._add_ichi_exit.Value:
                bearish_cross = prev_tenkan is not None and prev_kijun is not None and prev_tenkan >= prev_kijun and tenkan < kijun
                exit_now = bearish_cross or close < cloud_bottom

            if exit_now:
                self.SellMarket(self.Position)
                self._trail_stop = None
                return

            candidate = self._get_trail_candidate(close, atr)
            if self._trail_stop is None or candidate > self._trail_stop:
                self._trail_stop = candidate
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        tk_cross = prev_tenkan is not None and prev_kijun is not None and prev_tenkan <= prev_kijun and tenkan > kijun and close > cloud_top
        breakout = prev_close is not None and prev_cloud_top is not None and prev_close <= prev_cloud_top and close > cloud_top and cloud_a > cloud_b
        ma_ok = not self._use_ma_filter.Value or close > ma

        if self.Position == 0 and (tk_cross or breakout) and ma_ok:
            self.BuyMarket(self.Volume)
            self._entry_price = close
            self._trail_stop = self._get_trail_candidate(close, atr)

    def _get_trail_candidate(self, close, atr):
        if str(self._trail_source.Value) == "LowsHighs":
            source = min(self._lows[-self._swing_lookback.Value:])
        else:
            source = close

        if str(self._trail_method.Value) == "Atr":
            return source - atr * Decimal(self._atr_multiplier.Value)
        return source * (Decimal(1) - Decimal(self._trail_percent.Value) / Decimal(100))

    def CreateClone(self):
        return mikuls_ichimoku_cloud_v2_strategy()
