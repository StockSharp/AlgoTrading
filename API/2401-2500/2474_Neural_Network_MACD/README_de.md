# MACD-Strategie mit Neuronalem Netz
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen einfachen Vier-Gewicht-Perzeptron-Filter mit einem klassischen MACD-Crossover. Eine Position wird nur eröffnet, wenn sowohl der MACD als auch das neuronale Netz in der gleichen Richtung übereinstimmen.

## Funktionsweise

1. **Perzeptron-Filter**  
   Drei Perzeptronen bewerten den Preismomentum anhand der Differenzen zwischen dem aktuellen Schlusskurs und einer Reihe vergangener Eröffnungskurse. Jeder Perzeptron hat vier ganzzahlige Gewichte (`X11`…`X34`), wobei `0` keinen Einfluss bedeutet. Die Ausgabe des Perzeptrons ist eine gewichtete Summe der Preisdifferenzen.  
   Abhängig vom Parameter `Pass` nehmen ein, zwei oder alle drei Perzeptronen an der Entscheidungsfindung teil. Der Filter definiert auch Stop-Loss- und Take-Profit-Abstände (`Sl1`, `Tp1`, `Sl2`, `Tp2`).
2. **MACD-Bestätigung**  
   Ein Standard-MACD (12, 26, 9) wird berechnet. Ein Kaufsignal erscheint, wenn die MACD-Linie unter null liegt und die Signallinie von unten kreuzt. Ein Verkaufssignal entsteht, wenn die Linie über null liegt und die Signallinie von oben kreuzt.
3. **Handelsausführung**  
   - Eine Long-Position wird eröffnet, wenn sowohl der MACD als auch der Perzeptron-Filter positiv sind.  
   - Eine Short-Position wird eröffnet, wenn beide negativ sind.  
   Die Position wird geschlossen, wenn ein Stop-Loss- oder Take-Profit-Level erreicht wird.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `X11…X34` | Gewichte für Perzeptron-Eingaben. |
| `Tp1`, `Sl1` | Take-Profit und Stop-Loss für den ersten Perzeptron. |
| `Tp2`, `Sl2` | Take-Profit und Stop-Loss für den zweiten Perzeptron. |
| `P1`, `P2`, `P3` | Verschiebungen in Balken zur Berechnung der Perzeptron-Eingaben. |
| `Pass` | Anzahl der zu verwendenden Perzeptronen (1-3). |
| `CandleType` | Kerzenserie für Berechnungen. |

