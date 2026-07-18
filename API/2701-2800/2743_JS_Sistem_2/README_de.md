# JS Sistem 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
JS Sistem 2 ist ein Trendfolge-System, das ursprünglich für MetaTrader 5 geschrieben wurde. Der StockSharp-Port behält den Multi-Indikator-Bestätigungsblock des Expert Advisors bei und handelt auf geschlossenen Kerzen des ausgewählten Zeitrahmens. Orders werden mit einem festen Volumen dimensioniert und können optional blockiert werden, wenn der verbundene Portfolio-Saldo unter einen konfigurierbaren Schwellenwert fällt. Das Risiko wird durch harte Stop-Loss- und Take-Profit-Distanzen in Pips zusammen mit einem adaptiven Trailing-Stop, der Kerzenschatten folgt, kontrolliert.

## Indikatoren und Filter
- **EMA(55), EMA(89), EMA(144)** – bilden einen direktionalen Filter. Long-Setups erfordern den schnellen EMA über dem mittleren und den mittleren über der langsamen Linie, während der Abstand zwischen den schnellen und langsamen Kurven unter `MinDifferencePips` bleiben muss.
- **MACD-Histogramm (OsMA)** – verwendet schnelle, langsame und Signal-EMA-Längen identisch zur MQL-Version. Ein Long-Trade erfordert ein positives Histogramm, ein Short-Trade ein negatives.
- **Relativer Vigor Index (RVI)** – mit Periode `RviPeriod` berechnet und durch einen zusätzlichen einfachen gleitenden Durchschnitt mit `RviSignalLength` geglättet. Long-Trades benötigen den RVI über seiner Signallinie und über dem Schwellenwert `RviMax`; Short-Trades benötigen das Inverse.
- **Höchst-/Tiefst-Swing-Envelopes** – verfolgen das höchste Hoch und das tiefste Tief über `VolatilityPeriod` Kerzen. Diese Werte steuern die Trailing-Stop-Logik und replizieren den Schatten-Trailing-Modus des ursprünglichen Expert Advisors.

## Handelslogik
1. Die Strategie verarbeitet nur abgeschlossene Kerzen des konfigurierten `CandleType`.
2. Vor der Bewertung von Einstiegen aktualisiert sie den Trailing-Stop für bestehende Positionen mit den neuesten Swing-Extremen und prüft dann, ob Stop-Loss- oder Take-Profit-Level während der Kerze erreicht wurden.
3. Long-Einstiegsbedingungen:
   - Portfolio-Saldo liegt über `MinBalance`.
   - EMA55 > EMA89 > EMA144 und die Differenz zwischen EMA55 und EMA144 liegt unter `MinDifferencePips` (in Preiseinheiten über die Pip-Größe des Instruments umgerechnet).
   - MACD-Histogramm (`macdLine`) ist größer als null.
   - RVI liegt über seiner Signallinie und die Signallinie liegt bei oder über `RviMax`.
   - Keine bestehende Long-Position (`Position <= 0`). Wenn eine Short-Position vorhanden ist, wird sie vor dem Öffnen der Long-Position abgeflacht.
4. Short-Einstiegsbedingungen spiegeln die Long-Regeln mit invertierten Vergleichen wider und verwenden den Schwellenwert `RviMin`.
5. Beim Einstieg speichert die Strategie den Kerzenschlusskurs als Referenz, setzt virtuelle Stop-Loss- und Take-Profit-Level durch Verschiebung dieses Preises um `StopLossPips` und `TakeProfitPips` und setzt den Trailing-Zustand zurück.

## Ausstieg und Trailing-Management
- **Harter Stop-Loss / Take-Profit:** Immer wenn der Kerzenbereich das gespeicherte Stop- oder Zielniveau überschneidet, schließt die Strategie die gesamte Position sofort.
- **Trailing-Stop:** Wenn `TrailingEnabled` wahr ist, versucht die Strategie den Stop in Richtung des Gewinns zu verschieben. Für Longs wird der Stop auf das tiefste Tief der letzten `VolatilityPeriod` Kerzen angehoben, sobald dieses Tief sowohl über dem Einstiegspreis als auch über dem vorherigen Stop um mindestens `TrailingIndentPips` liegt. Shorts folgen der symmetrischen Regel mit dem höchsten Hoch. Dies reproduziert das "Schatten-Trailing" des MQL-Advisors und verhindert ein verfrühtes Anziehen der Stops.
- **Bilanzschutz:** Wenn der aktuelle Portfolio-Wert unter `MinBalance` fällt, verzichtet die Strategie auf das Absenden neuer Orders, verwaltet aber weiterhin offene Trades und Trailing-Stops.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `MinBalance` | Mindest-Portfolio-Saldo für neue Einstiege. | 100 |
| `Volume` | Ordervolumen mit jedem Trade. | 1 |
| `StopLossPips` | Stop-Loss-Distanz in Pips. Auf 0 setzen, um zu deaktivieren. | 35 |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. Auf 0 setzen, um zu deaktivieren. | 40 |
| `MinDifferencePips` | Maximale erlaubte Spreizung zwischen schnellem und langsamem EMA in Pips. | 28 |
| `VolatilityPeriod` | Anzahl der Kerzen zur Berechnung von Swing-Hochs und -Tiefs für den Trailing-Stop. | 15 |
| `TrailingEnabled` | Aktiviert oder deaktiviert die Trailing-Stop-Logik. | true |
| `TrailingIndentPips` | Mindestlücke zwischen Preis, Einstieg und Stop beim Aktualisieren des Trailing-Stops. | 1 |
| `MaFastPeriod` | Periode für den schnellen EMA. | 55 |
| `MaMediumPeriod` | Periode für den mittleren EMA. | 89 |
| `MaSlowPeriod` | Periode für den langsamen EMA. | 144 |
| `OsmaFastPeriod` | Schnelle EMA-Länge für das MACD-Histogramm. | 13 |
| `OsmaSlowPeriod` | Langsame EMA-Länge für das MACD-Histogramm. | 55 |
| `OsmaSignalPeriod` | Signal-Glättungslänge für das MACD-Histogramm. | 21 |
| `RviPeriod` | Periode des Relativen Vigor Index. | 44 |
| `RviSignalLength` | Länge des auf den RVI angewendeten SMA zur Erzeugung seiner Signallinie. | 4 |
| `RviMax` | Obere Grenze, die das RVI-Signal vor Long-Einstiegen erreichen muss. | 0.04 |
| `RviMin` | Untere Grenze, die das RVI-Signal vor Short-Einstiegen erreichen muss. | -0.04 |
| `CandleType` | Zeitrahmen der für alle Berechnungen verwendeten Kerzen. | 5-Minuten-Kerzen |

## Implementierungshinweise
- Pip-Distanz wird aus dem Preisschritt des Instruments abgeleitet. Instrumente, die mit 3 oder 5 Dezimalstellen notiert werden, verwenden einen Pip gleich zehn Preisschritten, entsprechend der ursprünglichen MQL-Logik.
- Stop- und Ziel-Verwaltung erfolgt innerhalb der Strategie-Schleife, da StockSharp in dieser Vorlage keine serverseitigen Orders dafür automatisch einreicht.
- Die Strategie ruft `StartProtection()` beim Start auf, damit die Basisklasse unerwartete Verbindungsunterbrechungen und ausstehende Positionen überwachen kann.
