import clr
import math

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import DateTime, TimeSpan, Decimal
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import BollingerBands
from StockSharp.Algo.Strategies import Strategy


class dca_mean_reversion_bollinger_band_strategy(Strategy):
    """Invest a fixed quote amount on Bollinger down-cross or month change."""

    def __init__(self):
        super(dca_mean_reversion_bollinger_band_strategy, self).__init__()

        self._investment_amount = self.Param("InvestmentAmount", 200.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Investment Amount", "Quoted-currency amount for each buy", "Trading")
        self._open_date = self.Param("OpenDate", DateTime.MinValue) \
            .SetDisplay("Open Date", "Inclusive first buying date", "Calendar")
        self._close_date = self.Param("CloseDate", DateTime.MaxValue) \
            .SetDisplay("Close Date", "Exclusive end date for full liquidation", "Calendar")
        self._strategy_mode = self.Param("StrategyMode", "Combined") \
            .SetDisplay("Strategy Mode", "Bollinger, Monthly or Combined", "Signals")
        self._bollinger_period = self.Param("BollingerPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Period", "Bollinger period", "Indicators")
        self._bollinger_multiplier = self.Param("BollingerMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Bollinger Multiplier", "Standard deviations for the lower band", "Indicators")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Completed-candle interval", "General")

        self._bands = None
        self._previous_close = None
        self._previous_lower = None
        self._last_monthly_year = 0
        self._last_monthly_month = 0
        self._liquidated = False

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnReseted(self):
        super(dca_mean_reversion_bollinger_band_strategy, self).OnReseted()
        self._bands = None
        self._previous_close = None
        self._previous_lower = None
        self._last_monthly_year = 0
        self._last_monthly_month = 0
        self._liquidated = False

    def OnStarted2(self, time):
        super(dca_mean_reversion_bollinger_band_strategy, self).OnStarted2(time)

        if self._open_date.Value.Date > self._close_date.Value.Date:
            raise ValueError("OpenDate must not be after CloseDate")
        if str(self._strategy_mode.Value) not in ("Bollinger", "Monthly", "Combined"):
            raise ValueError("StrategyMode must be Bollinger, Monthly or Combined")

        self._bands = BollingerBands()
        self._bands.Length = int(self._bollinger_period.Value)
        self._bands.Width = Decimal(self._bollinger_multiplier.Value)
        self._previous_close = None
        self._previous_lower = None
        self._last_monthly_year = 0
        self._last_monthly_month = 0
        self._liquidated = False

        subscription = self.SubscribeCandles(self.candle_type)
        subscription.BindEx(self._bands, self._process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, self._bands)
            self.DrawOwnTrades(area)

    def _process_candle(self, candle, bands_value):
        if candle.State != CandleStates.Finished:
            return

        date = candle.OpenTime.Date
        if date < self._open_date.Value.Date:
            return

        if date >= self._close_date.Value.Date:
            if not self._liquidated and self.Position > 0 and self.IsFormedAndOnlineAndAllowTrading():
                self.SellMarket(self.Position)
                self._liquidated = True
            return

        lower = bands_value.LowBand
        if lower is None:
            return

        close = float(candle.ClosePrice)
        low = float(lower)
        cross_below = (self._previous_close is not None and
                       self._previous_lower is not None and
                       self._previous_close >= self._previous_lower and close < low)
        self._previous_close = close
        self._previous_lower = low

        if not self._bands.IsFormed or not self.IsFormedAndOnlineAndAllowTrading():
            return

        mode = str(self._strategy_mode.Value)
        monthly = mode != "Bollinger" and (
            self._last_monthly_year != date.Year or self._last_monthly_month != date.Month
        )
        bollinger = mode != "Monthly" and cross_below
        if not monthly and not bollinger:
            return

        price = float(candle.ClosePrice)
        if price <= 0 or self.Security is None:
            return

        step = float(self.Security.VolumeStep)
        if step <= 0:
            step = 0.001
        max_volume = float(self.Security.MaxVolume)
        if max_volume <= 0:
            max_volume = float("inf")
        volume = math.floor(min(float(self._investment_amount.Value) / price, max_volume) / step) * step
        min_volume = float(self.Security.MinVolume)
        if volume <= 0 or (min_volume > 0 and volume < min_volume):
            return

        self.BuyMarket(Decimal.Parse(str(volume)))
        if monthly:
            self._last_monthly_year = date.Year
            self._last_monthly_month = date.Month

    def CreateClone(self):
        return dca_mean_reversion_bollinger_band_strategy()
