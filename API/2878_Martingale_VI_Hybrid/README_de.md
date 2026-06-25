# Martingale VI Hybrid-Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Martingale VI Hybrid-Strategie ist eine Konvertierung des originalen MetaTrader-Expertenberaters in die StockSharp High-Level-API. Sie kombiniert einen Schnell-/Langsam-Moving-Average-Filter mit einer MACD-Bestätigung und skaliert Positionen mithilfe eines Martingale-Multiplikators. Die Strategie akkumuliert Positionen, wenn der Preis um eine feste Pip-Distanz gegen den letzten Einstieg läuft, und vereinheitlicht den Take Profit des gesamten Clusters auf dem durch die neueste Order definierten Niveau. Weitere globale Ausstiege umfassen festen Gewinn in Geld, Gewinn als Prozentsatz des Startkapitals und einen Trailing Stop in Geld.

## Handelslogik
1. **Signalfilter** – es werden die Werte der vorherigen Kerze für die schnelle und langsame SMA sowie das MACD-Histogramm verwendet. Ein Long-Zyklus beginnt, wenn die schnelle SMA über der langsamen SMA war und die MACD-Hauptlinie unter ihrer Signallinie lag. Ein Short-Zyklus beginnt, wenn die schnelle SMA unter der langsamen SMA war, während die MACD-Hauptlinie über der Signallinie lag.
2. **Anfangsposition** – wenn ein neuer Zyklus beginnt und keine Position offen ist, sendet die Strategie eine Marktorder mit dem `Initial Volume`.
3. **Martingale-Zukäufe** – während eine Position offen ist, überwacht die Strategie den letzten Einstiegspreis. Wenn sich der Preis um `Pip Step` Pips gegen die Position bewegt, wird eine weitere Marktorder hinzugefügt, deren Volumen `vorheriges Order-Volumen × Volume Multiplier` beträgt. Die Anzahl aktiver Orders ist durch `Max Trades` begrenzt. Wenn das Limit erreicht wird und `Close Max Orders` aktiviert ist, wird die gesamte Position sofort geschlossen.
4. **Gemeinsamer Take Profit** – jede neue Order aktualisiert das gemeinsame Take-Profit-Niveau auf `Einstiegspreis ± Take Profit (pips)` je nach Richtung. Sobald das Hoch der Kerze (für Longs) oder das Tief (für Shorts) dieses Niveau berührt, werden alle Orders zusammen geschlossen.
5. **Globale Ausstiege** – der schwebende Gewinn wird kontinuierlich bewertet:
   - Wenn `Use Money TP` aktiviert ist und der Gewinn `Money TP` erreicht, wird die Position geschlossen.
   - Wenn `Use Percent TP` aktiviert ist und der Gewinn `Percent TP` Prozent des anfänglichen Portfoliowertes erreicht, wird die Position geschlossen.
   - Wenn `Enable Trailing` aktiv ist, wird ein geldbasierter Trailing Stop angewendet, sobald der Gewinn `Trailing Activation` überschreitet. Die Position wird geschlossen, wenn der Gewinn um `Trailing Drawdown` vom Höchststand fällt.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `Candle Type` | Primäre Kerzenserie für Indikator-Updates.
| `Fast MA`, `Slow MA` | Perioden der einfachen gleitenden Durchschnitte, die den Trendfilter definieren.
| `MACD Fast`, `MACD Slow`, `MACD Signal` | Parameter des zur Bestätigung verwendeten MACD-Indikators.
| `Initial Volume` | Volumen der ersten Order in einem Martingale-Zyklus.
| `Volume Multiplier` | Multiplikator, der bei jedem Zukauf auf das vorherige Order-Volumen angewendet wird.
| `Max Trades` | Maximale Anzahl gleichzeitiger Orders in der Martingale-Sequenz.
| `Take Profit (pips)` | Take-Profit-Abstand für jede Order; die neueste Order definiert den gemeinsamen Take-Profit-Preis.
| `Pip Step` | Preisbewegung gegen den aktuellen Zyklus, die den nächsten Zukauf auslöst.
| `Use Money TP`, `Money TP` | Aktiviert und setzt das Gewinnziel in Kontowährung.
| `Use Percent TP`, `Percent TP` | Aktiviert und setzt das Gewinnziel als Prozentsatz des anfänglichen Portfoliowertes.
| `Enable Trailing`, `Trailing Activation`, `Trailing Drawdown` | Parameter des geldbasierten Trailing Stops, der akkumulierte Gewinne schützt.
| `Close Max Orders` | Wenn aktiviert, wird die gesamte Position geschlossen, sobald das Martingale-Order-Limit erreicht wird.

## Risikomanagement
- Die Strategie unterstützt sowohl absolute als auch prozentbasierte Gewinnziele, um Gewinne frühzeitig zu sichern.
- Der Trailing Stop in Geld verhindert, dass die Position nach einem profitablen Lauf mehr als den konfigurierten Drawdown zurückgibt.
- Die Begrenzung der Gesamtzahl der Martingale-Schritte vermeidet unbegrenztes Positionswachstum; die Aktivierung von `Close Max Orders` erzwingt einen Notausstieg, wenn die Sequenz ihr konfiguriertes Limit erreicht.

## Implementierungshinweise
- Die Strategie verwendet die StockSharp High-Level-`SubscribeCandles`-API mit über `BindEx` gebundenen Indikatoren für MACD und manueller Verarbeitung für die gleitenden Durchschnitte.
- Die Pip-Größe wird aus dem Preisschritt des Wertpapiers abgeleitet, einschließlich Unterstützung für 5-stellige und 3-stellige Preisgestaltung.
- Gewinnberechnungen basieren auf `Security.PriceStep`, `Security.StepPrice` und `PositionAvgPrice` und gewährleisten die Kompatibilität mit Instrumenten, die die notwendigen Metadaten bereitstellen.
