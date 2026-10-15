# Tipu EA Multi-Zeitrahmen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie rekreiert die Kernlogik des Tipu Expertenberaters in StockSharp. Sie ersetzt die proprietären Tipu-Trend- und Tipu-Stops-Indikatoren durch eine Kombination aus exponentiellen gleitenden Durchschnitten (EMA), Average Directional Index (ADX)-Filterung und Average True Range (ATR)-Risikokontrollen. Das System sucht nach Trendausrichtung zwischen einem höheren Zeitrahmen (Standard 1 Stunde) und einem Signalzeitrahmen (Standard 15 Minuten) und verwaltet dann die Position mit einem Break-Even-Pyramiding-Modul, Trailing-Stop-Logik und optionalem festem Take Profit.

Die Implementierung konzentriert sich auf liquide, trendende Instrumente, bei denen Multi-Zeitrahmen-Momentum-Signale zuverlässig sind. Der höhere Zeitrahmen definiert den Kontext und filtert Ranging-Phasen heraus, während der Signalzeitrahmen die eigentlichen Einstiege liefert.

## Datenabonnements
- Höherer Zeitrahmen-Kerzen (Standard 1 Stunde) für EMA-Trend und ADX-Rangerkennung.
- Signalzeitrahmen-Kerzen (Standard 15 Minuten) für Einstiegssignale, ATR-Stop-Platzierung und Trade-Management-Updates.

## Handelslogik
1. **Höherer Zeitrahmen-Kontext**
   - Schnelle und langsame EMAs berechnen und Kreuzungen erkennen. Eine bullische Kreuzung erzeugt ein Aufwärtstrend-Signal; eine bärische Kreuzung erzeugt ein Abwärtstrend-Signal.
   - Trendstärke mit ADX messen. Liegt ADX unter dem konfigurierten Schwellenwert, wird der Markt als rangend markiert und keine neuen Trades erlaubt.
   - Den Zeitstempel des letzten höheren Zeitrahmen-Signals speichern. Die Signalgültigkeit läuft nach einer konfigurierbaren Anzahl von Minuten ab.
2. **Signalzeitrahmen-Einstiege**
   - Auf eine EMA-Kreuzung im Signalzeitrahmen **und** ein frisches höheres Zeitrahmen-Signal in derselben Richtung warten, während der höhere Zeitrahmen nicht rangend ist.
   - Long-Einstiege erfordern, dass die schnelle EMA die langsame EMA überkreuzt; Short-Einstiege erfordern das Gegenteil.
   - Vor dem Senden einer neuen Order schließt die Strategie optional die entgegengesetzte Position (Reverse-on-Signal-Verhalten) und respektiert das Hedging-Flag.
   - Der anfängliche Stop-Abstand wird auf `ATR * AtrMultiplier` gesetzt und durch den Parameter `MaxRiskPips` begrenzt. Orders werden übersprungen, wenn das erforderliche Risiko diesen Schwellenwert übersteigt.
3. **Risikomanagement**
   - **Take Profit**: optionales festes Ziel basierend auf `TakeProfitPips`.
   - **Trailing Stop**: sobald sich der Preis um `TrailingStartPips` zugunsten bewegt, folgt der Stop dem Markt mit einem `TrailingCushionPips`-Offset.
   - **Risikofreier Modus**: wenn aktiviert, bewegt die Strategie den Stop nach `RiskFreeStepPips` Gewinn auf Break-Even und fügt in `PyramidIncrementVolume`-Schritten zusätzliches Volumen hinzu, bis `PyramidMaxVolume` erreicht ist. Jeder Pyramiding-Schritt zieht auch den Schutz-Stop enger.
   - Positionen werden sofort beim entgegengesetzten Signal geschlossen, wenn `CloseOnReverseSignal` wahr ist.

## Parameter
- `AllowHedging` – Positionen hinzufügen erlauben, ohne zuerst die entgegengesetzte Seite zu schließen.
- `CloseOnReverseSignal` – Die aktuelle Position glätten, wenn ein entgegengesetztes Signal eintrifft.
- `EnableTakeProfit`, `TakeProfitPips` – Festen Take-Profit-Abstand in Pips aktivieren und konfigurieren.
- `MaxRiskPips` – Maximaler erlaubter Stop-Abstand in Pips. Verhindert Einstiege mit übermäßigem Anfangsrisiko.
- `TradeVolume` – Basis-Ordergröße für die erste Position.
- `EnableRiskFreePyramiding`, `RiskFreeStepPips`, `PyramidIncrementVolume`, `PyramidMaxVolume` – Steuerung der risikofreien Pyramiding-Logik.
- `EnableTrailingStop`, `TrailingStartPips`, `TrailingCushionPips` – Konfigurierung des Trailing-Stop-Verhaltens.
- `HigherFastLength`, `HigherSlowLength`, `LowerFastLength`, `LowerSlowLength` – EMA-Längen zur Trenderkennung auf beiden Zeitrahmen.
- `AdxLength`, `AdxThreshold` – ADX-Parameter zum Filtern von Range-Märkten im höheren Zeitrahmen.
- `AtrLength`, `AtrMultiplier` – ATR-Parameter für die anfängliche Stop-Berechnung.
- `HigherSignalWindowMinutes` – Gültigkeitszeitraum für das höhere Zeitrahmen-Signal.
- `HigherCandleType`, `LowerCandleType` – Kerzentypen/Zeitrahmen für Kontext- und Signalverarbeitung.

## Verhaltenshinweise
- Der durchschnittliche Einstiegspreis wird neu berechnet, wenn neues Volumen hinzugefügt wird, um sicherzustellen, dass Trailing Stops und das risikofreie Modul die tatsächliche Kostenbasis der Position referenzieren.
- Alle Handelsentscheidungen werden nur auf abgeschlossenen Kerzen getroffen; unfertige Kerzen werden ignoriert, um vorzeitige Signale zu vermeiden.
- Die Strategie gibt Marktorders (`BuyMarket`/`SellMarket`) aus und verwaltet Positionen intern, ohne sich auf ausstehende Stop-Orders zu verlassen.
- Da die ursprünglichen Tipu-Indikatoren proprietär sind, werden EMA/ADX/ATR-Kombinationen als treue Annäherung verwendet, wobei die ursprünglichen Trade-Management-Funktionen (Reverse-on-Signal, Break-Even-Pyramiding und Trailing Stop) beibehalten werden.

## Verwendungstipps
- EMA-Längen, ATR-Multiplikator und ADX-Schwellenwert für das Zielinstrument optimieren; die bereitgestellten Standardwerte funktionieren als generischer Ausgangspunkt für FX-Majors.
- `HigherSignalWindowMinutes` nahe an der Dauer des höheren Zeitrahmens setzen, um nahezu synchrone Ausrichtung zu erfordern, oder erhöhen, um mehr Verzögerung zwischen höheren und niedrigeren Zeitrahmen-Signalen zu erlauben.
- Wenn Pyramiding deaktiviert ist, bewegt die Strategie den Stop trotzdem auf Break-Even, sobald die Distanz `RiskFreeStepPips` erreicht ist, und bietet damit grundlegenden Risikoschutz.
- `CloseOnReverseSignal` deaktivieren, wenn Ausstiege lieber manuell verwaltet oder der Trailing Stop den gesamten Trade verwalten soll.
