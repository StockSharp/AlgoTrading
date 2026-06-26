# WE TRUST Channel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **WE TRUST Channel-Strategie** ist ein High-Level-StockSharp-Port des MetaTrader 5 Expert Advisors „WE TRUST". Das System handelt Pullbacks in Richtung eines linear gewichteten Moving Average, der von Standardabweichungsbändern umgeben ist. Wenn der Preis außerhalb der Bänder schließt, antizipiert die Strategie eine Mean Reversion und eröffnet eine Marktposition zurück zur Mitte des Kanals. Signalumkehr, optionales Schließen entgegengesetzter Trades und pip-basierte Geldverwaltungsparameter spiegeln den originalen Experten wider.

## Handelslogik
1. Abonnieren des konfigurierten Kerzentyps (standardmäßig stündliche Kerzen) und Berechnen zweier Indikatoren an der ausgewählten Preisquelle:
   - Ein linear gewichteter Moving Average (**LWMA**) mit konfigurierbarer Periode und Verschiebung.
   - Eine Standardabweichungshülle mit eigener Periode und Verschiebung.
2. Konvertieren pip-basierter Offsets in absolute Preisabstände unter Verwendung des Instrument-`PriceStep`. Fünf- und dreistellige Kurse multiplizieren den Schritt mit 10, um die MetaTrader-Pip-Definition zu emulieren.
3. Berechnen der oberen und unteren Kanalgrenzen: `LWMA ± StdDev ± ChannelIndentPips` (in Preiseinheiten umgerechnet).
4. Nur abgeschlossene Kerzen auswerten. Wenn der gewählte Kerzenkurs unterhalb des unteren Kanals schließt, generiert die Strategie ein **Kauf**-Signal. Wenn er oberhalb des oberen Kanals schließt, generiert er ein **Verkaufs**-Signal.
5. Optional Signale invertieren, wenn **ReverseSignals** aktiviert ist. Optional eine entgegengesetzte Position flatten, bevor auf ein neues Signal reagiert wird, wenn **CloseOpposite** aktiviert ist.
6. Marktorders mit dem konfigurierten Volumen senden, wenn die aktuelle Position flat oder mit der Signalrichtung ausgerichtet ist.

## Risikomanagement
- **StopLossPips** und **TakeProfitPips** übersetzen Pip-Abstände in absolute Schutzorders über `StartProtection`. Auf `0` setzen, um das jeweilige Niveau zu deaktivieren.
- **TrailingStopPips** und **TrailingStepPips** steuern einen pip-basierten Trailing Stop, der profitable Trades verfolgt. Beide Parameter werden mit derselben Pip-Größen-Logik in Preisabstände umgerechnet.
- Alle Ausstiege werden mit Marktorders ausgeführt, um nah an der MQL5-Implementierung zu bleiben.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `OrderVolume` | Trade-Volumen mit jeder Marktorder. | `0.1` |
| `StopLossPips` | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). | `40` |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). | `60` |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. | `10` |
| `TrailingStepPips` | Trailing-Schritt in Pips zwischen Stop-Anpassungen. | `10` |
| `MaPeriod` | Periode des linear gewichteten Moving Average. | `60` |
| `MaShift` | Anzahl der Balken, die der Moving Average vorwärts verschoben wird. | `0` |
| `StdDevPeriod` | Periode der Standardabweichungsberechnung. | `50` |
| `StdDevShift` | Anzahl der Balken, die der Abweichungswert verschoben wird. | `0` |
| `SignalBarOffset` | Anzahl abgeschlossener Balken zurück bei der Signalauswertung. | `1` |
| `ChannelIndentPips` | Zusätzlicher Puffer außerhalb der Abweichungsbänder. | `1` |
| `ReverseSignals` | Kauf/Verkauf-Logik des Kanal-Ausbruchs invertieren. | `false` |
| `CloseOpposite` | Entgegengesetzte Position schließen, bevor ein neuer Trade eingegangen wird. | `false` |
| `AppliedPrice` | Kerzenpreiskomponente, die in beide Indikatoren einfließt. | `Weighted` |
| `CandleType` | Vom Connector angeforderter Kerzendatentyp. | `1-Stunden-Zeitrahmen` |

## Hinweise
- Die Strategie verlässt sich auf gültige `PriceStep`-Metadaten. Wenn das Exchange diese nicht bereitstellt, fällt der Code auf `Security.Step` und schließlich auf `1` zurück.
- Nur die C#-Implementierung ist in diesem Verzeichnis enthalten. Der Python-Port wird gemäß den Anweisungen absichtlich weggelassen.
- Die Logik verarbeitet nur abgeschlossene Kerzen und versucht nicht, partielle Balkendaten zu akkumulieren.
