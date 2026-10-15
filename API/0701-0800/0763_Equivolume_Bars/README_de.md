# Equivolumen-Balken-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf Volumenspitzen im Verhältnis zur Summe der Volumen über einen Rückblickzeitraum.

## Logik
- Berechne das Verhältnis des aktuellen Volumens zur Summe der vorherigen Volumen.
- Long gehen, wenn das Verhältnis den Schwellenwert überschreitet und die Kerze bullisch ist.
- Short gehen, wenn das Verhältnis den Schwellenwert überschreitet und die Kerze bärisch ist.
- Position schließen, wenn das Verhältnis unter den Schwellenwert fällt oder die Kerze dreht.

## Parameter
- `Lookback` – Anzahl der Balken für die Volumensumme.
- `Volume Threshold` – Verhältnisschwellenwert für hohes Volumen.
- `Candle Type` – zu verwendender Kerzentyp.
