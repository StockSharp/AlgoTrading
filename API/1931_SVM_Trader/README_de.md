# SVM-Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die SVM-Trader-Strategie demonstriert, wie eine Kombination klassischer technischer Indikatoren das Verhalten eines Support Vector Machine (SVM) Modells zur Generierung von Handelssignalen approximieren kann. Das ursprüngliche MQL-Beispiel trainierte zwei separate SVMs für Kauf- und Verkaufsentscheidungen. In dieser StockSharp-Konvertierung emulieren wir den Entscheidungsprozess mit einem einfachen Bewertungssystem aus sieben Indikatoren:

- **Bears Power** und **Bulls Power** – messen das Gleichgewicht zwischen Verkäufern und Käufern.
- **Average True Range (ATR)** – erfasst die aktuelle Volatilität.
- **Momentum** – prüft die Preisbeschleunigung.
- **Moving Average Convergence Divergence (MACD)** – identifiziert die Trendrichtung.
- **Stochastic Oscillator** – erkennt überkaufte und überverkaufte Niveaus.
- **Force Index** – kombiniert Preisbewegung und Volumen.

Jeder Indikator trägt zu einem kumulativen Score bei. Wenn der Score einen Schwellenwert überschreitet, eröffnet die Strategie eine Long-Position; wenn der Score unter den entgegengesetzten Schwellenwert fällt, wird eine Short-Position eröffnet. Dieses Setup spiegelt den Klassifizierungsschritt des ursprünglichen SVM-Ansatzes wider und hält die Implementierung leichtgewichtig und transparent.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `CandleType` | Kerzen-Zeitrahmen für Berechnungen. |
| `Volume` | Ordervolumen für neue Trades. |
| `TakeProfit` | Abstand für Take-Profit in absoluten Preiseinheiten. |
| `StopLoss` | Abstand für Stop-Loss in absoluten Preiseinheiten. |
| `RiskExposure` | Maximal erlaubtes kumulatives Positionsvolumen. |

## Handelslogik

1. Kerzen des angegebenen Typs abonnieren und alle Indikatoren mit der High-Level-API binden.
2. Für jede abgeschlossene Kerze Indikatorwerte aus dem Binding-Callback abrufen.
3. Score berechnen:
   - Bulls Power größer als Bears Power
   - Momentum über null
   - MACD-Linie über ihrer Signallinie
   - Stochastik %K über %D
   - Force Index über null
4. Wenn mindestens drei Bedingungen erfüllt sind und die aktuelle Position nicht positiv ist, wird eine Market-Buy-Order platziert.
5. Wenn zwei oder weniger Bedingungen erfüllt sind und die aktuelle Position nicht negativ ist, wird eine Market-Sell-Order platziert.
6. `StartProtection` wendet sowohl Stop-Loss als auch Take-Profit auf jede eröffnete Position an.

## Hinweise

- Indikatorperioden sind auf Werte aus dem ursprünglichen MQL-Beispiel festgelegt (hauptsächlich 13 für Symmetrie und Glätte).
- Das Bewertungssystem ist ein vereinfachter Proxy für SVM-Klassifikation und kann bei Bedarf durch ein fortschrittlicheres Modell ersetzt werden.
- `RiskExposure` verhindert Überallokation durch Begrenzung der Gesamtpositionsgröße.
- Die Strategie verwendet Tabs für Einrückungen und englische Kommentare gemäß Projektkonventionen.

## Haftungsausschluss

Diese Strategie wird zu Bildungszwecken bereitgestellt. Sie demonstriert Indikator-Binding und grundlegendes Risikomanagement in StockSharp. Verwendung und Modifikation auf eigene Gefahr.
