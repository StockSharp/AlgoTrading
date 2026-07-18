# Strategie Adaptive Grid MT4 (StockSharp Port)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Strategie erstellt den Expertenberater „Adaptive Grid Mt4“ für StockSharps hohes Niveau API neu. Es lässt ein symmetrisches Gitter fallen
Kauf-Stopp- und Verkaufsstopp-Aufträge rund um den aktuellen Kerzenschluss. Gitterentfernungen werden aus der durchschnittlichen wahren Reichweite (ATR) und abgeleitet
sind daher anpassungsfähig an die Marktvolatilität. Jede ausstehende Bestellung läuft nach einer konfigurierbaren Anzahl von Kerzen ab und behält die
Ordentliches Auftragsbuch in Seitwärtsmärkten.

Wenn eine Einstiegsorder ausgeführt wird, registriert die Strategie sofort die passenden Take-Profit- und Stop-Loss-Orders zu den berechneten Preisen
aus dem ATR-Snapshot, der das Raster erstellt hat. Schutzbefehle sind eins zu eins mit dem ausgefüllten Eintrag und bleiben bestehen, bis sie ausgeführt werden
oder manuell storniert werden.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `GridLevels` | Anzahl der Stop-Orders über und unter dem Markt. Entspricht dem `nGrid`-Eingang des EA. |
| `TimerBars` | Anzahl der abgeschlossenen Kerzen, nach denen ein eventuell ausstehender Eintrag storniert wird (MT4 `nBars`). |
| `PriceOffsetMultiplier` | ATR-Multiplikator, der auf den anfänglichen Offset vom aktuellen Preis (`Poffset`) angewendet wird. |
| `GridStepMultiplier` | ATR-Multiplikator, der für den Abstand zwischen aufeinanderfolgenden Rasterebenen verwendet wird (`Pstep`). |
| `StopLossMultiplier` | ATR-Multiplikator, der den Abstand des Stop-Loss definiert, der jeder Order zugeordnet ist (`StopLoss`). |
| `TakeProfitMultiplier` | ATR-Multiplikator, der die Entfernung des Take-Profits (`TakeProfit`) definiert. |
| `AtrPeriod` | ATR Mittelungszeitraum. Spiegelt den hartcodierten Wert 14 aus dem Skript wider. |
| `OrderVolume` | Für alle ausstehenden Aufträge verwendetes Volumen (MT4 `Lot`). |
| `CandleType` | Zeitrahmen, der die Neuberechnung des Rasters steuert (`Wtf`). |

## Handelslogik

1. Abonnieren Sie Kerzen des konfigurierten `CandleType` und füttern Sie ein ATR(14).
2. Auf jeder fertigen Kerze:
   - Schalten Sie die interne Bartheke vor und stornieren Sie ausstehende Rasterbestellungen, die `TimerBars` überschreiten.
   - Überspringen Sie die weitere Verarbeitung, wenn der ATR nicht gebildet wurde, eine Rasterreihenfolge noch aktiv ist oder die Strategie bereits eine Position hält.
   - Berechnen Sie den Breakout-Offset, den Rasterabstand, die Stop-Loss- und Take-Profit-Abstände als `ATR * multiplier`-Werte.
   - Platzieren Sie `GridLevels` Paare von Kauf-Stopp- und Verkaufs-Stopp-Orders rund um den Kerzenschluss und normalisieren Sie die Preise mit
`Security.ShrinkPrice`, um die Tick-Größe des Instruments zu berücksichtigen.
3. Wenn ein Eintrag gefüllt ist, entfernen Sie ihn aus der verfolgten Rasterliste und erzeugen Sie die entsprechenden Schutzbefehle:
   - Long-Einträge erhalten einen Stop-Loss von `SellStop` und einen Take-Profit von `SellLimit`.
   - Short-Einträge erhalten einen Stop-Loss von `BuyStop` und einen Take-Profit von `BuyLimit`.
4. Schutzaufträge werden über `OnOrderChanged` überwacht, sodass abgeschlossene oder stornierte Einträge aus der Nachverfolgung entfernt werden
Listen.

## Notizen

- Das Raster wird nur dann neu erstellt, wenn keine offenen Positionen vorhanden sind und alle vorhandenen Rasteraufträge abgelaufen sind, entsprechend der `What()`-Logik von
das Original EA.
- Die Preise werden anhand des Kerzenschlusses und nicht anhand des rohen `Bid/Ask`-Ticks berechnet. Dadurch bleibt die Implementierung kerzengesteuert
und gleichzeitig das gleiche symmetrische Layout auf dem Markt erzeugen.
- Der für das Raster verwendete ATR-Snapshot wird auch für Schutzaufträge verwendet, um den Stop und Take-Profit pro Ticket von MetaTrader nachzuahmen
Werte.
- Es gibt noch keine Python-Übersetzung, die der Anfrage entspricht.
