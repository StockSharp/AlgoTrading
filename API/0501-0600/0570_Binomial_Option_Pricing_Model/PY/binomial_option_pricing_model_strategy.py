import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")
clr.AddReference("StockSharp.BusinessEntities")

import math
from collections import deque
from System import TimeSpan, DateTime, DateTimeKind, Decimal, Nullable
from StockSharp.Messages import DataType, CandleStates, Sides, OrderStates, Unit, UnitTypes
from StockSharp.BusinessEntities import Subscription
from StockSharp.Algo.Strategies import Strategy

clr.AddReference("Ecng.Logging")
import Ecng.Logging
clr.ImportExtensions(Ecng.Logging)
from StockSharp.Messages import SecurityTypes, OptionTypes, OptionStyles
from StockSharp.Algo.Indicators import StandardDeviation, SimpleMovingAverage, DecimalIndicatorValue

class binomial_option_pricing_model_strategy(Strategy):
    def __init__(self):
        super(binomial_option_pricing_model_strategy, self).__init__()
        self._strike_price = self.Param("StrikePrice", 100.0).SetGreaterThanZero().SetDisplay(
            "StrikePrice", "Positive option strike", "Parameters")
        self._dividend_yield = self.Param("DividendYield", 0.0).SetDisplay(
            "DividendYield", "Continuous dividend or foreign interest rate as a fraction", "Parameters")
        self._asset_class = self.Param("AssetClass", SecurityTypes.Stock).SetDisplay(
            "AssetClass", "Stock, Index, Currency or Future carry convention", "Parameters")
        self._option_style = self.Param("OptionStyle", OptionStyles.European).SetDisplay(
            "OptionStyle", "European maturity exercise or American tree-node exercise", "Parameters")
        self._option_type = self.Param("OptionType", OptionTypes.Call).SetDisplay(
            "OptionType", "Call or put payoff", "Parameters")
        self._time_to_expiry = self.Param("TimeToExpiry", TimeSpan.FromDays(30)).SetDisplay(
            "TimeToExpiry", "Remaining tenor; minutes, hours and days are supported", "Parameters")
        self._length = self.Param("Length", 20).SetGreaterThanZero().SetDisplay(
            "Length", "Population close deviation and mean window", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(1))).SetDisplay(
            "CandleType", "Finished source candle interval", "Parameters")
        self.RiskFreeRate = Decimal(0.05)
        self._deviation = self._average = None
        self._last_time = DateTime.MinValue
        self._last_option_price = None

    @property
    def StrikePrice(self):
        return self._strike_price.Value

    @StrikePrice.setter
    def StrikePrice(self, value):
        self._strike_price.Value = value

    @property
    def DividendYield(self):
        return self._dividend_yield.Value

    @DividendYield.setter
    def DividendYield(self, value):
        self._dividend_yield.Value = value

    @property
    def AssetClass(self):
        return self._asset_class.Value

    @AssetClass.setter
    def AssetClass(self, value):
        self._asset_class.Value = value

    @property
    def OptionStyle(self):
        return self._option_style.Value

    @OptionStyle.setter
    def OptionStyle(self, value):
        self._option_style.Value = value

    @property
    def OptionType(self):
        return self._option_type.Value

    @OptionType.setter
    def OptionType(self, value):
        self._option_type.Value = value

    @property
    def TimeToExpiry(self):
        return self._time_to_expiry.Value

    @TimeToExpiry.setter
    def TimeToExpiry(self, value):
        self._time_to_expiry.Value = value

    @property
    def Length(self):
        return self._length.Value

    @Length.setter
    def Length(self, value):
        self._length.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    @property
    def LastOptionPrice(self):
        return self._last_option_price

    def OnReseted(self):
        super(binomial_option_pricing_model_strategy, self).OnReseted()
        self._deviation = self._average = None
        self._last_time = DateTime.MinValue
        self._last_option_price = None

    def GetWorkingSecurities(self):
        return [(self.Security, self.CandleType)]

    def _ensure_indicators(self):
        if self._deviation is not None and self._deviation.Length == self.Length:
            return
        self._deviation, self._average = StandardDeviation(), SimpleMovingAverage()
        self._deviation.Length = self._average.Length = self.Length

    def OnStarted2(self, time):
        self._ensure_indicators()
        super(binomial_option_pricing_model_strategy, self).OnStarted2(time)
        candles = self.SubscribeCandles(self.CandleType)
        candles.Bind(self._on_candle).Start()
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, candles)
            self.DrawIndicator(area, self._deviation)

    def _on_candle(self, candle):
        self.EvaluatePrice(candle)

    def EvaluatePrice(self, candle):
        if candle is None:
            raise ValueError("A candle is required.")
        if candle.State != CandleStates.Finished or candle.OpenTime <= self._last_time:
            return None
        if candle.ClosePrice <= 0:
            raise ValueError("A positive underlying close is required.")
        frame = self.CandleType.Arg
        if not isinstance(frame, TimeSpan) or frame <= TimeSpan.Zero:
            raise ValueError("A positive time-frame candle interval is required for annualization.")
        self._ensure_indicators()
        self._last_time = candle.OpenTime
        deviation_input = DecimalIndicatorValue(self._deviation, candle.ClosePrice, candle.OpenTime)
        deviation_input.IsFinal = True
        average_input = DecimalIndicatorValue(self._average, candle.ClosePrice, candle.OpenTime)
        average_input.IsFinal = True
        deviation = self._deviation.Process(deviation_input)
        average = self._average.Process(average_input)
        if not self._deviation.IsFormed or not self._average.IsFormed:
            return None
        sigma = float(deviation.GetValue[Decimal](None) / average.GetValue[Decimal](None)) * math.sqrt(TimeSpan.FromDays(365).TotalSeconds / frame.TotalSeconds)
        self._last_option_price = self.CalculatePrice(candle.ClosePrice, Decimal(sigma), self.TimeToExpiry)
        self.AddInfoLog("Option price at {0:O}: {1}", candle.OpenTime, self._last_option_price)
        return self._last_option_price

    def CalculatePrice(self, spot, volatility, timeToExpiry):
        if spot <= 0 or volatility < 0 or timeToExpiry < TimeSpan.Zero or self.StrikePrice <= 0:
            raise ValueError("Spot and strike must be positive; volatility and tenor must be nonnegative.")
        if self.OptionStyle not in (OptionStyles.European, OptionStyles.American) or self.OptionType not in (OptionTypes.Call, OptionTypes.Put) or self.AssetClass not in (SecurityTypes.Stock, SecurityTypes.Index, SecurityTypes.Currency, SecurityTypes.Future):
            raise ValueError("Unsupported option style, payoff or underlying asset class.")
        s, strike = float(spot), float(self.StrikePrice)
        def payoff(value):
            return max(value - strike if self.OptionType == OptionTypes.Call else strike - value, 0.0)
        if timeToExpiry == TimeSpan.Zero:
            return Decimal(payoff(s))
        years = timeToExpiry.TotalDays / 365.0
        dt = years / 2.0
        rate = float(self.RiskFreeRate)
        carry = 0.0 if self.AssetClass == SecurityTypes.Future else rate - float(self.DividendYield)
        if volatility == 0:
            final = payoff(s * math.exp(carry * years)) * math.exp(-rate * years)
            if self.OptionStyle == OptionStyles.American:
                final = max(payoff(s), final, payoff(s * math.exp(carry * dt)) * math.exp(-rate * dt))
            return Decimal(final)
        up = math.exp(float(volatility) * math.sqrt(dt))
        down = 1.0 / up
        probability = (math.exp(carry * dt) - down) / (up - down)
        discount = math.exp(-rate * dt)
        if not math.isfinite(probability) or probability < 0.0 or probability > 1.0 or not math.isfinite(up) or not math.isfinite(discount):
            raise ValueError("Two-step inputs do not admit a finite risk-neutral probability in [0,1].")
        upper = discount * (probability * payoff(s * up * up) + (1.0 - probability) * payoff(s))
        lower = discount * (probability * payoff(s) + (1.0 - probability) * payoff(s * down * down))
        if self.OptionStyle == OptionStyles.American:
            upper = max(upper, payoff(s * up))
            lower = max(lower, payoff(s * down))
        price = discount * (probability * upper + (1.0 - probability) * lower)
        if self.OptionStyle == OptionStyles.American:
            price = max(price, payoff(s))
        return Decimal(price)

    def CreateClone(self):
        return binomial_option_pricing_model_strategy()
