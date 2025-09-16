[Русский](README_ru.md) | [中文](README_cn.md)

The strategy rebuilds the Center of Gravity channel used by the original MQL4 expert by solving a polynomial regression on the most recent candles. The regression center is computed from the intercept of the least squares fit, while the band width is derived from the standard deviation of close prices over the same lookback horizon. The lower band equals the regression center minus the scaled deviation, reproducing the `stdl` buffer accessed in the source robot.

During live processing the algorithm maintains a rolling queue of closes with the length defined by the **Bars Back** parameter. Each finished candle triggers a recalculation of the regression coefficients through Gaussian elimination on the normal equation system. This avoids storing full candle histories yet yields the same channel geometry as the custom indicator. If the matrix becomes ill-conditioned the update is skipped, preventing unstable trading decisions.

Trading logic mirrors the original expert: when the current candle low stays above the lower deviation band (`lowerBand < Low` in MQL notation) the strategy considers this a mean-reversion bounce. If no long position is open, any short exposure is closed and a market buy order is issued using the strategy volume. The most recent lower, upper and center values are exposed via read-only properties for charting or diagnostics.

Risk management is optional. **Stop Loss Distance** and **Take Profit Distance** are specified in absolute price units. When non-zero, the strategy records the entry price of the active long position and checks candle extremes to determine whether a stop or profit target has been touched. If neither parameter is provided the position can be managed manually or by external modules.

### Parameters
- **Candle Type** – timeframe of the candle subscription feeding the regression.
- **Bars Back** – number of historical bars used to compute the regression channel (default 125).
- **Polynomial Degree** – degree of the polynomial regression (default 2) governing channel curvature.
- **Std Multiplier** – multiplier applied to the standard deviation when forming the envelope (default 1).
- **Stop Loss Distance** – optional long stop loss offset in price units (default 0 disables it).
- **Take Profit Distance** – optional long take profit offset in price units (default 0 disables it).

The strategy operates on completed candles only, uses market orders for entries and performs no automatic short selling because the sell branch of the original expert was commented out.
