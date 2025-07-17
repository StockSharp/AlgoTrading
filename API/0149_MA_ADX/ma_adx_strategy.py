import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, UnitTypes, Unit, CandleStates
from StockSharp.Algo.Indicators import SimpleMovingAverage, AverageDirectionalIndex, AverageTrueRange
from StockSharp.Algo.Strategies import Strategy
from datatype_extensions import *
from indicator_extensions import *

class ma_adx_strategy(Strategy):
    """
    Strategy based on MA and ADX indicators.
    Enters position when price crosses MA with strong trend.
    """

    def __init__(self):
        super(ma_adx_strategy, self).__init__()

        # Initialize strategy parameters
        self._ma_period = self.Param("MaPeriod", 20) \
            .SetGreaterThanZero() \
            .SetDisplay("MA Period", "Period for Moving Average calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(10, 50, 5)

        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetGreaterThanZero() \
            .SetDisplay("ADX Period", "Period for ADX calculation", "Indicators") \
            .SetCanOptimize(True) \
            .SetOptimize(7, 28, 7)

        self._candle_type = self.Param("CandleType", tf(5)) \
            .SetDisplay("Candle Type", "Type of candles to use", "General")

        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        self._take_profit_atr_multiplier = self.Param("TakeProfitAtrMultiplier", 2.0) \
            .SetGreaterThanZero() \
            .SetDisplay("TP ATR Multiplier", "Take profit as ATR multiplier", "Risk Management") \
            .SetCanOptimize(True) \
            .SetOptimize(1.0, 5.0, 0.5)

        # Current state
        self._atr_value = 0.0
        self._is_first_candle = True

    @property
    def ma_period(self):
        """MA period."""
        return self._ma_period.Value

    @ma_period.setter
    def ma_period(self, value):
        self._ma_period.Value = value

    @property
    def adx_period(self):
        """ADX period."""
        return self._adx_period.Value

    @adx_period.setter
    def adx_period(self, value):
        self._adx_period.Value = value

    @property
    def candle_type(self):
        """Candle type for strategy calculation."""
        return self._candle_type.Value

    @candle_type.setter
    def candle_type(self, value):
        self._candle_type.Value = value

    @property
    def stop_loss_percent(self):
        """Stop loss percentage."""
        return self._stop_loss_percent.Value

    @stop_loss_percent.setter
    def stop_loss_percent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def take_profit_atr_multiplier(self):
        """Take profit ATR multiplier."""
        return self._take_profit_atr_multiplier.Value

    @take_profit_atr_multiplier.setter
    def take_profit_atr_multiplier(self, value):
        self._take_profit_atr_multiplier.Value = value

    def OnReseted(self):
        """
        Resets internal state when strategy is reset.
        """
        super(ma_adx_strategy, self).OnReseted()
        self._atr_value = 0.0
        self._is_first_candle = True

    def OnStarted(self, time):
        """
        Called when the strategy starts. Sets up indicators, subscriptions, and charting.

        :param time: The time when the strategy started.
        """
        super(ma_adx_strategy, self).OnStarted(time)

        # Initialize state
        self._atr_value = 0.0
        self._is_first_candle = True

        # Create indicators
        ma = SimpleMovingAverage()
        ma.Length = self.ma_period
        adx = AverageDirectionalIndex()
        adx.Length = self.adx_period
        atr = AverageTrueRange()
        atr.Length = self.adx_period

        # Create subscription
        subscription = self.SubscribeCandles(self.candle_type)

        # Bind indicators to candles
        subscription.BindEx(ma, adx, atr, self.ProcessCandle).Start()

        # Enable stop-loss and take-profit
        self.StartProtection(
            takeProfit=Unit(self.take_profit_atr_multiplier, UnitTypes.Absolute),
            stopLoss=Unit(self.stop_loss_percent, UnitTypes.Percent)
        )
        # Setup chart if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, ma)
            self.DrawIndicator(area, adx)
            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, ma_value, adx_value, atr_value):
        """
        Processes each finished candle and executes MA + ADX trading logic.
        """
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Save ATR value for stop-loss calculation
        self._atr_value = to_float(atr_value)

        # Skip the first candle to have previous values to compare
        if self._is_first_candle:
            self._is_first_candle = False
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        if adx_value.MovingAverage is None:
            return
        adx_ma = float(adx_value.MovingAverage)

        # Trading logic
        if adx_ma > 25:
            ma_dec = to_float(ma_value)

            # Strong trend detected
            if candle.ClosePrice > ma_dec and self.Position <= 0:
                # Price above MA and no long position - Buy
                volume = self.Volume + Math.Abs(self.Position)
                self.BuyMarket(volume)
            elif candle.ClosePrice < ma_dec and self.Position >= 0:
                # Price below MA and no short position - Sell
                volume = self.Volume + Math.Abs(self.Position)
                self.SellMarket(volume)

    def CreateClone(self):
        """
        !! REQUIRED!! Creates a new instance of the strategy.
        """
        return ma_adx_strategy()
