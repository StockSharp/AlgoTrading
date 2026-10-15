# Angry Bird Scalping-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader Expert Advisor "Angry Bird (Scalping)" mit der High-Level-API von StockSharp.

## Logik
- Beobachtet 15-Minuten-Kerzen und berechnet das höchste Hoch und tiefste Tief über die letzten `Depth` Kerzen, um einen dynamischen Grid-Schritt abzuleiten.
- Wenn keine Position offen ist und die vorherige Kerze über der aktuellen schließt, löst der RSI auf dem Stundenrahmen Einstiege aus: Werte über `RsiMin` öffnen Short-Positionen, Werte unter `RsiMax` öffnen Long-Positionen.
- Wenn eine Position existiert und der Kurs sich um mindestens den Grid-Schritt dagegen bewegt, wird eine neue Position in dieselbe Richtung mit seinem Volumen, multipliziert mit `LotExponent`, geöffnet, bis `MaxTrades` erreicht ist.
- Ein starker CCI-Wert über `CciDrop` für Shorts oder unter `-CciDrop` für Longs erzwingt das Schließen aller Positionen.
- Positionen werden auch geschlossen, wenn der Gewinn `TakeProfit` oder der Verlust `StopLoss` relativ zum durchschnittlichen Einstiegspreis erreicht.

## Parameter
- `StopLoss` – Stop-Loss in Punkten.
- `TakeProfit` – Take-Profit in Punkten.
- `DefaultPips` – minimaler Abstand zwischen Grid-Orders in Pips.
- `Depth` – Anzahl der Kerzen für die Hoch/Tief-Berechnung.
- `LotExponent` – Multiplikator für das nachfolgende Order-Volumen.
- `MaxTrades` – maximale Anzahl der Averaging-Positionen.
- `RsiMin` / `RsiMax` – RSI-Schwellenwerte für den Einstieg.
- `CciDrop` – absoluter CCI-Wert, der das Schließen von Positionen erzwingt.
- `Volume` – anfängliches Order-Volumen.
- `CandleType` – Zeitrahmen der Arbeitskerzen (Standard 15 Minuten).

## Verwendung
Strategie einem Instrument zuweisen und starten. Die Strategie verwendet Marktorders und verwaltet eine einzige Nettoposition, die gemittelt wird, wenn der Kurs dagegen läuft.
