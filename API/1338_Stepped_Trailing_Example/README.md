# Stepped Trailing Strategy Example
[Русский](README_ru.md) | [中文](README_cn.md)

A sample strategy demonstrating three-step trade management with optional trailing stop.

The strategy enters long when the 14-period SMA crosses above the 28-period SMA. Risk is controlled by a stop-loss and three profit targets:
- After the first target, the stop moves to break-even.
- After the second target, the stop moves to the first target.
- On the third step the position either exits at target three or starts a trailing stop.

This example shows how to stage profits and protect positions as they progress.
