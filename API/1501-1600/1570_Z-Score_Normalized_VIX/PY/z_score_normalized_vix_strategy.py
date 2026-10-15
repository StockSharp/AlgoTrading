import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.BusinessEntities")
clr.AddReference("StockSharp.Algo")
clr.AddReference("StockSharp.Algo.Indicators")
clr.AddReference("StockSharp.Algo.Strategies")

from System import TimeSpan
from StockSharp.Messages import DataType, CandleStates
from StockSharp.Algo.Indicators import (
    SimpleMovingAverage, StandardDeviation,
    CandleIndicatorValue
)
from StockSharp.Algo.Strategies import Strategy
from indicator_extensions import *

class z_score_normalized_vix_strategy(Strategy):
    """Z-Score Normalized VIX strategy.
    Computes a Z-score of the instrument's own volatility (StdDev of close prices)
    and trades mean-reversion: buy when Z-score drops below -threshold (low vol),
    sell when Z-score rises above +threshold (high vol).
    """
    def __init__(self):
        super(z_score_normalized_vix_strategy, self).__init__()
        self._z_score_length = self.Param("ZScoreLength", 50) \
            .SetDisplay("Z-Score Length", "Lookback period for z-score of volatility", "Parameters")
        self._volatility_length = self.Param("VolatilityLength", 20) \
            .SetDisplay("Volatility Length", "Period for StdDev volatility measure", "Parameters")
        self._buy_threshold = self.Param("BuyThreshold", -1.5) \
            .SetDisplay("Buy Threshold", "Z-score below this triggers buy", "Parameters")
        self._sell_threshold = self.Param("SellThreshold", 1.5) \
            .SetDisplay("Sell Threshold", "Z-score above this triggers sell/exit", "Parameters")
        self._cooldown_bars = self.Param("CooldownBars", 100) \
            .SetDisplay("Cooldown Bars", "Minimum bars between trades", "Parameters")
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Type of candles", "Data")

        self._volatility = None
        self._z_mean = None
        self._z_std = None
        self._entry_price = 0
        self._bars_since_last_trade = 0

    @property
    def z_score_length(self):
        return self._z_score_length.Value

    @property
    def volatility_length(self):
        return self._volatility_length.Value

    @property
    def buy_threshold(self):
        return self._buy_threshold.Value

    @property
    def sell_threshold(self):
        return self._sell_threshold.Value

    @property
    def cooldown_bars(self):
        return self._cooldown_bars.Value

    @property
    def candle_type(self):
        return self._candle_type.Value

    def OnStarted2(self, time):
        super(z_score_normalized_vix_strategy, self).OnStarted2(time)

        self._volatility = StandardDeviation()
        self._volatility.Length = self.volatility_length

        self._z_mean = SimpleMovingAverage()
        self._z_mean.Length = self.z_score_length

        self._z_std = StandardDeviation()
        self._z_std.Length = self.z_score_length

        self._entry_price = 0
        self._bars_since_last_trade = 0

        sub = self.SubscribeCandles(self.candle_type)
        sub.Bind(self.process_candle).Start()

        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, sub)
            self.DrawOwnTrades(area)

    def process_candle(self, candle):
        if candle.State != CandleStates.Finished:
            return

        # Feed close price into volatility indicator
        vol_result = self._volatility.Process(CandleIndicatorValue(self._volatility, candle))

        if not self._volatility.IsFormed:
            return

        vol_value = float(vol_result)

        # Feed volatility value into z-score components (SMA and StdDev of volatility)
        mean_result = process_float(self._z_mean, vol_value, candle.OpenTime, True)

        std_result = process_float(self._z_std, vol_value, candle.OpenTime, True)

        if not self._z_mean.IsFormed or not self._z_std.IsFormed:
            return

        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        mean = float(mean_result)
        std = float(std_result)

        if std == 0:
            return

        z_score = (vol_value - mean) / std

        self._bars_since_last_trade += 1

        # Low volatility (z-score below buy threshold) => buy (expect breakout)
        if self.Position == 0 and z_score < self.buy_threshold and self._bars_since_last_trade >= self.cooldown_bars:
            self.BuyMarket()
            self._entry_price = candle.ClosePrice
            self._bars_since_last_trade = 0
        # Z-score reverts toward mean => close long
        elif self.Position > 0 and z_score > 0 and self._bars_since_last_trade >= self.cooldown_bars:
            self.SellMarket()
            self._entry_price = 0
            self._bars_since_last_trade = 0
        # High volatility => short (expect mean reversion in vol)
        elif self.Position == 0 and z_score > self.sell_threshold and self._bars_since_last_trade >= self.cooldown_bars:
            self.SellMarket()
            self._entry_price = candle.ClosePrice
            self._bars_since_last_trade = 0
        # Z-score reverts toward mean => close short
        elif self.Position < 0 and z_score < 0 and self._bars_since_last_trade >= self.cooldown_bars:
            self.BuyMarket()
            self._entry_price = 0
            self._bars_since_last_trade = 0

    def CreateClone(self):
        return z_score_normalized_vix_strategy()
