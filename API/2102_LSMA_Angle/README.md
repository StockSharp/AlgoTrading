# LSMA Angle Strategy

This strategy uses the angle of the Least Squares Moving Average (LSMA) to detect trend direction. The angle is approximated by the difference between two LSMA values separated by a configurable number of bars.

- **Long entry**: LSMA angle rises above the positive threshold.
- **Long exit**: Angle returns below the positive threshold.
- **Short entry**: LSMA angle falls below the negative threshold.
- **Short exit**: Angle returns above the negative threshold.

## Parameters
- `LSMA Period`: Length for LSMA calculation.
- `Angle Threshold`: Absolute value defining the neutral zone around zero.
- `Start Shift`: Older bar used to calculate the angle.
- `End Shift`: Recent bar used to calculate the angle.
- `Candle Type`: Candle data type for calculation.

## Notes
- Angle values are scaled to points depending on the security (1000 for JPY pairs, otherwise 100000).
- Works on completed candles only.
