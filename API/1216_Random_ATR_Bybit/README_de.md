# Zufällige ATR-Strategie - Bybit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie erzeugt ein deterministisch zufälliges Signal basierend auf den jüngsten Preisspannen und dem aktuellen Datum. Sie geht long, wenn das Signal 1 ist, und short, wenn es 0 ist. Das Risikomanagement verwendet ATR-basierte Stop-Loss- und Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**:
  - **Long**: zufälliges Signal ist gleich 1.
  - **Short**: zufälliges Signal ist gleich 0.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit.
- **Stops**: `SlAtrRatio * ATR` für Stop-Loss, Take-Profit bei `SlAtrRatio * TpSlRatio * ATR`.
- **Standardwerte**:
  - `AtrLength` = 14
  - `SlAtrRatio` = 3
  - `TpSlRatio` = 1
- **Filter**: keine.
- **Komplexität**: einfach.
- **Zeitrahmen**: konfigurierbar.
