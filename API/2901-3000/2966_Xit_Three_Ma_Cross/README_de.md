# Xit Drei-MA-Kreuzungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Nachbildung des MetaTrader 5-Expert-Advisors **XIT_THREE_MA_CROSS.mq5**. Sie richtet drei gleitende Durchschnitte aus, prüft die MACD-Momentum-Trennung und dimensioniert Positionen aus ATR-basierten Risikolimiten. Die Methode ist Trendfolge mit Momentum-Bestätigung und zielt auf mittelfristige Swings bei liquiden Währungspaaren oder Indizes.

## Überblick

- **Marktregime**: Funktioniert am besten bei Instrumenten, die auf dem ausgewählten Zeitrahmen mehrere Kerzen lang trends.
- **Indikatoren**:
  - Langsame, mittlere und schnelle gleitende Durchschnitte (Typ vom Benutzer wählbar), auf dem Handelszeitrahmen bewertet.
  - MACD (EMA-basiert) für Momentum-Richtung und Abstand zwischen MACD- und Signallinie.
  - Zwei ATR-Berechnungen (gleiche Länge, unabhängige Zeitrahmen), die zur Projektion von Stop-Loss- und Take-Profit-Abständen verwendet werden.
- **Order-Richtung**: Bidirektional. Die Engine kann sowohl Long- als auch Short-Trades öffnen.
- **Positionierung**: Berechnet aus dem konfigurierten Risikoprozentsatz und dem ATR-basierten Stop-Abstand. Wenn die Instrument-Metadaten unvollständig sind, fällt die Strategie auf die Standard-`Volume`-Eigenschaft zurück.

## Handelslogik

### Long-Einstieg

Eine Long-Position wird geöffnet, wenn alle nachstehenden Bedingungen auf einer abgeschlossenen Kerze zutreffen:

1. MACD-Linie steigt im Vergleich zum vorherigen Balken (`MACD[t] > MACD[t-1]`).
2. MACD-Signallinie steigt im Vergleich zum vorherigen Balken.
3. Die MACD-Linie übersteigt die Signallinie um mindestens `MacdTriggerPoints * PriceStep`.
4. Mittlerer gleitender Durchschnitt steigt gegenüber dem vorherigen Wert.
5. Schneller gleitender Durchschnitt steigt gegenüber dem vorherigen Wert.
6. Mittlerer MA liegt über dem langsamen MA.
7. Schneller MA liegt über dem mittleren MA.
8. Beide ATR-Werte sind verfügbar, um Stop- und Zielabstände zu definieren.

### Short-Einstieg

Die Short-seitigen Regeln spiegeln das Long-Setup mit invertierten Vergleichen:

1. MACD-Linie sinkt im Vergleich zum vorherigen Balken.
2. MACD-Signallinie sinkt im Vergleich zum vorherigen Balken.
3. Die Signallinie ist um mindestens `MacdTriggerPoints * PriceStep` größer als die MACD-Linie.
4. Mittlerer MA fällt im Vergleich zur vorherigen Kerze.
5. Schneller MA fällt im Vergleich zur vorherigen Kerze.
6. Mittlerer MA liegt unter dem langsamen MA.
7. Schneller MA liegt unter dem mittleren MA.
8. Beide ATR-Serien haben einen fertigen Wert geliefert.

### Ausstiegslogik

- **Long-Positionen** schließen, wenn der schnelle MA unter den mittleren MA fällt, oder der Preis die ATR-basierten Stop/Take-Profit-Niveaus erreicht.
- **Short-Positionen** schließen, wenn der schnelle MA über den mittleren MA kreuzt, oder die ATR-Limits berührt werden.
- Nach dem Schließen einer Position wartet der Algorithmus auf die nächste Kerze, bevor er neue Einstiege bewertet, was dem ursprünglichen EA-Verhalten entspricht.

## Risikomanagement

- **Stop Loss**: Abstand entspricht dem letzten ATR-Wert von `AtrStopCandleType`. Für Longs ist der Stop-Preis `Entry - ATR`, für Shorts ist er `Entry + ATR`.
- **Take Profit**: Abstand entspricht dem ATR-Wert von `AtrTakeCandleType`. Ziele werden relativ zum Einstiegspreis gespiegelt.
- **Risikoprozent**: Die Strategie schätzt den monetären Verlust pro Einheit aus dem Stop-Abstand. Wenn `PriceStep` und `PriceStepCost` bekannt sind, verwendet das Risiko pro Kontrakt die Tick-Bewertung. Andernfalls wird der rohe Preisabstand verwendet. Positionsgröße ist `RiskPercent%` des aktuellen Portfolio-Werts geteilt durch das Risiko pro Einheit, abgerundet auf das nächste `VolumeStep`.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Primärer Zeitrahmen für Berechnungen von gleitenden Durchschnitten und MACD. | 1-Stunden-Kerzen |
| `SlowMaLength` / `IntermediateMaLength` / `FastMaLength` | Perioden der gleitenden Durchschnitte. | 60 / 14 / 4 |
| `SlowMaType`, `IntermediateMaType`, `FastMaType` | MA-Familien (Einfach, Exponentiell, Geglättet, Gewichtet). | Einfach |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD schnelle, langsame und Signal-EMA-Längen. | 12 / 26 / 9 |
| `MacdTriggerPoints` | Mindestabstand zwischen MACD und seiner Signallinie, gemessen in Instrument-Punkten. Konvertiert über `PriceStep`. | 7 |
| `AtrLength` | Periode für beide ATR-Indikatoren. | 14 |
| `AtrTakeCandleType` / `AtrStopCandleType` | Zeitrahmen für Take-Profit- und Stop-Loss-ATR-Serien. | 4-Stunden-Kerzen |
| `RiskPercent` | Prozent des aktuellen Portfolio-Werts, der auf jeden Trade riskiert wird. | 10% |

## Verwendungshinweise

1. Hängen Sie die Strategie an ein Wertpapier mit genauen `PriceStep`, `PriceStepCost` und `VolumeStep` an, um präzise Positionierung zu erhalten.
2. Stellen Sie sicher, dass historische Daten für jeden abonnierten Zeitrahmen (`CandleType`, `AtrTakeCandleType`, `AtrStopCandleType`) verfügbar sind. Fehlende ATR-Werte verzögern Einstiege.
3. Der Algorithmus arbeitet auf vollständig geschlossenen Kerzen und ignoriert intrabar-Schwankungen, was der ursprünglichen MetaTrader-Logik des Abrufens aktueller und vorheriger Indikator-Puffer entspricht.
4. Ändern Sie die MA-Typen, wenn der Zielmarkt glattere oder schnellere Filter bevorzugt.

## Dateien

- `CS/XitThreeMaCrossStrategy.cs` – C#-Implementierung mit High-Level-StockSharp-API, einschließlich ATR-Abonnements und Risiko-Sizing.
- `README_ru.md` – Russische Beschreibung der Strategie.
- `README_zh.md` – Chinesische Übersetzung der Dokumentation.
