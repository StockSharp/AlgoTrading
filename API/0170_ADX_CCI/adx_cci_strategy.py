import clr

clr.AddReference("StockSharp.Messages")
clr.AddReference("StockSharp.Algo")

from System import TimeSpan, Math
from StockSharp.Messages import DataType, CandleStates, Unit, UnitTypes
from StockSharp.Algo.Indicators import AverageDirectionalIndex, CommodityChannelIndex
from StockSharp.Algo.Strategies import Strategy

class adx_cci_strategy(Strategy):
    """
    Strategy based on ADX and CCI indicators.
    Enters long when ADX > 25 and CCI is oversold (< -100)
    Enters short when ADX > 25 and CCI is overbought (> 100)
    """

    def __init__(self):
        """Constructor"""
        super(adx_cci_strategy, self).__init__()

        # ADX period
        self._adx_period = self.Param("AdxPeriod", 14) \
            .SetDisplay("ADX Period", "Period for ADX indicator", "Indicators")

        # CCI period
        self._cci_period = self.Param("CciPeriod", 20) \
            .SetDisplay("CCI Period", "Period for CCI indicator", "Indicators")

        # Stop-loss percentage
        self._stop_loss_percent = self.Param("StopLossPercent", 2.0) \
            .SetDisplay("Stop Loss %", "Stop loss as percentage of entry price", "Risk Management")

        # Candle type for strategy calculation
        self._candle_type = self.Param("CandleType", DataType.TimeFrame(TimeSpan.FromMinutes(5))) \
            .SetDisplay("Candle Type", "Timeframe for strategy", "General")

        # Internal state
        self._prev_cci_value = 0.0
        self._is_first_value = True

    @property
    def AdxPeriod(self):
        return self._adx_period.Value

    @AdxPeriod.setter
    def AdxPeriod(self, value):
        self._adx_period.Value = value

    @property
    def CciPeriod(self):
        return self._cci_period.Value

    @CciPeriod.setter
    def CciPeriod(self, value):
        self._cci_period.Value = value

    @property
    def StopLossPercent(self):
        return self._stop_loss_percent.Value

    @StopLossPercent.setter
    def StopLossPercent(self, value):
        self._stop_loss_percent.Value = value

    @property
    def CandleType(self):
        return self._candle_type.Value

    @CandleType.setter
    def CandleType(self, value):
        self._candle_type.Value = value

    def OnStarted(self, time):
        super(adx_cci_strategy, self).OnStarted(time)

        # Create indicators
        adx = AverageDirectionalIndex()
        adx.Length = self.AdxPeriod
        cci = CommodityChannelIndex()
        cci.Length = self.CciPeriod

        # Reset state variables
        self._prev_cci_value = 0.0
        self._is_first_value = True

        # Enable position protection with stop-loss
        self.StartProtection(
            Unit(0),  # No take profit
            Unit(self.StopLossPercent, UnitTypes.Percent)
        )

        # Subscribe to candles and bind indicators
        subscription = self.SubscribeCandles(self.CandleType)
        subscription.Bind(adx, cci, self.ProcessCandle).Start()

        # Setup chart visualization if available
        area = self.CreateChartArea()
        if area is not None:
            self.DrawCandles(area, subscription)
            self.DrawIndicator(area, adx)

            # Create a separate area for CCI
            cci_area = self.CreateChartArea()
            if cci_area is not None:
                self.DrawIndicator(cci_area, cci)

            self.DrawOwnTrades(area)

    def ProcessCandle(self, candle, adx_value, cci_value):
        # Skip unfinished candles
        if candle.State != CandleStates.Finished:
            return

        # Check if strategy is ready to trade
        if not self.IsFormedAndOnlineAndAllowTrading():
            return

        # For the first value, just store and skip trading
        if self._is_first_value:
            self._prev_cci_value = float(cci_value)
            self._is_first_value = False
            return

        # Store for the next iteration
        self._prev_cci_value = float(cci_value)

        # Extract ADX moving average value
        try:
            if hasattr(adx_value, 'MovingAverage') and adx_value.MovingAverage is not None:
                adx_ma = float(adx_value.MovingAverage)
            else:
                adx_ma = float(adx_value)
        except Exception:
            return

        # Trading logic
        if adx_ma > 25:
            if self._prev_cci_value < -100 and self.Position <= 0:
                # Strong trend with oversold CCI - Buy
                self.BuyMarket(self.Volume + Math.Abs(self.Position))
            elif self._prev_cci_value > 100 and self.Position >= 0:
                # Strong trend with overbought CCI - Sell
                self.SellMarket(self.Volume + Math.Abs(self.Position))
        elif adx_ma < 20:
            # Trend is weakening - close any position
            if self.Position > 0:
                self.SellMarket(self.Position)
            elif self.Position < 0:
                self.BuyMarket(Math.Abs(self.Position))

    def CreateClone(self):
        """!! REQUIRED!! Creates a new instance of the strategy."""
        return adx_cci_strategy()
