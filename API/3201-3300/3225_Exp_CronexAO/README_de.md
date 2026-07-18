# Exp Cronex AO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader Experten-Advisor **Exp_CronexAO** auf die StockSharp High-Level-API. Der ursprüngliche Roboter handelt Crossover zwischen den zwei Linien des Cronex Awesome Oscillator (AO). Die StockSharp-Version abonniert eine konfigurierbare Kerzenserie, berechnet den AO, glättet ihn zweimal mit gleitenden Durchschnitten, um die Cronex-Linien zu reproduzieren, und öffnet oder schließt Positionen, wenn die schnelle Linie die langsame Linie kreuzt.

## Handelslogik

1. Den Awesome Oscillator aus den ausgewählten Kerzen aufbauen.
2. Den Oszillator zweimal mit einfachen gleitenden Durchschnitten glätten. Die erste Glättung erstellt die "schnelle" Cronex-Linie, die zweite Glättung erzeugt die "Signal"-Linie.
3. `SignalBar` abgeschlossene Kerzen zurückblicken und die Cronex-Linien auf diesem Balken und dem vorherigen vergleichen.
4. Ein **Kauf**-Signal erscheint, wenn die schnelle Linie über der langsamen Linie liegt und auf dem Rückblick-Balken einen Aufwärts-Crossover vollzogen hat. Die Strategie schließt optional eine Short-Position und öffnet, wenn erlaubt, eine Long-Market-Order.
5. Ein **Verkaufs**-Signal spiegelt die vorherige Regel wider: Die schnelle Linie muss unter der langsamen Linie liegen und muss auf dem Rückblick-Balken nach unten gekreuzt haben. Die Strategie schließt optional eine Long-Position und öffnet, wenn erlaubt, eine Short-Market-Order.
6. Stop-Loss- und Take-Profit-Levels, ausgedrückt in Instrument-Punkten, werden an die resultierende Position angehängt, wenn ein neuer Trade eröffnet wird.

Es wird nur eine Nettoposition gehalten. Wenn die Richtung wechselt, kombiniert die Strategie das zum Schließen der entgegengesetzten Position benötigte Volumen mit dem neuen Trade-Volumen, um den Netting-Modus von MetaTrader zu emulieren.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `CandleType` | Datentyp der für Cronex-AO-Berechnungen verwendeten Kerzen. Standard ist ein 8-Stunden-Zeitrahmen. |
| `FastPeriod` | Länge der ersten Glättung, die auf den Awesome Oscillator angewendet wird. |
| `SlowPeriod` | Länge der zweiten Glättung, die auf die schnelle Linie angewendet wird. |
| `SignalBar` | Anzahl abgeschlossener Balken zurück, die das Kreuzsignal enthalten müssen. Die Strategie prüft auch den folgenden Balken zur Richtungsbestätigung. |
| `BuyOpenEnabled` / `SellOpenEnabled` | Öffnen von Long- oder Short-Positionen aktivieren oder deaktivieren. |
| `BuyCloseEnabled` / `SellCloseEnabled` | Steuern, ob entgegengesetzte Positionen bei einem inversen Signal geschlossen werden können. |
| `TakeProfit` | Gewinnziel in Punkten, nach jedem neuen Einstieg angewendet wenn größer als null. |
| `StopLoss` | Schutz-Stop in Punkten, auch nach jedem neuen Einstieg angewendet wenn größer als null. |

## Risikomanagement

Die Stop-Loss- und Take-Profit-Abstände ahmen die punktbasierten Eingaben der MetaTrader-Version nach. Sie werden bei jedem neuen Trade neu berechnet, damit Schutzorders immer mit der aktuellen Nettopositionsgröße übereinstimmen.

## Unterschiede zur MetaTrader-Version

- Die StockSharp-Implementierung verwendet einfache gleitende Durchschnitte für beide Cronex-Glättungsstufen. Die ursprüngliche XMA-Implementierung erlaubt mehrere Glättungsmethoden, aber die Standardkonfiguration entspricht dem hier reproduzierten einfachen Durchschnitt.
- Slippage- und Geldmanagement-Routinen aus der `TradeAlgorithms`-Bibliothek werden nicht repliziert. Positionsgrößen werden über die Standard-`Volume`-Eigenschaft gesteuert.
- Die Trade-Ausführung basiert auf dem Netting-Verhalten von StockSharp. Wenn die Richtung umgekehrt wird, wird eine einzelne Market-Order mit ausreichend Volumen ausgegeben, um die Position in einem Schritt zu glätten und umzukehren, was der MT5-Netting-Konto-Logik entspricht.
