# 20PRExp-3 Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die 20PRExp-3-Strategie ist ein Ausbruchssystem, das die aktuelle Handelssitzung mit den Preisextremen des Vortages vergleicht. Es baut den Tageskanal bei jeder abgeschlossenen Fünf-Minuten-Kerze neu auf, bestätigt den Momentum durch eine 30-Minuten-Tick-Volumen-Expansion und steigt nur ein, wenn der Preis über das aktualisierte Sitzungshoch oder -tief hinausbricht. Einmal im Markt spiegelt es den ursprünglichen MetaTrader 5-Experten durch die Verwendung von Parabolic SAR-Ausstiegen, dynamischen Trailing-Stops und fester Risikogröße basierend auf dem Abstand zum Schutz-Stop wider.

## Konzept
- **Tageskanal**: Das laufende Hoch, Tief und den Mittelpunkt des aktuellen Handelstages verfolgen.
- **Ausbruchsbestätigung**: Der Preis muss über die Kanalgrenze hinaus schließen, mit einem Mindest-Tagesbereich-Filter (`GapPoints`).
- **Volumenexpansion**: Die letzten zwei abgeschlossenen 30-Minuten-Kerzen vergleichen und mindestens eine 1,5-fache Erhöhung des Tick-Volumens verlangen, um dünne Ausbrüche zu vermeiden.
- **Zeitfilter**: Neue Positionen nur nach der konfigurierten Sitzungsstartzeit (`SessionStartHour`) zulassen, um den Overnight-Bereich mit geringer Liquidität zu vermeiden.
- **Risikosymmetrie**: Long-Trades verwenden das Tagestief als Stop-Loss, Short-Trades das Tageshoch. Take-Profit und Trailing-Offsets werden in Preispunkten gemessen.

## Marktdaten
- Fünf-Minuten-Kerzen für das primäre Signal und die Parabolic SAR-Berechnung.
- Dreißig-Minuten-Kerzen für den Tick-Volumen-Verhältnis-Filter.
- Tages-Hoch/Tief-Statistiken werden aus den Fünf-Minuten-Daten abgeleitet, sodass kein separates Tages-Abonnement erforderlich ist.

## Einstiegslogik
1. Nach der konfigurierten Startzeit eine abgeschlossene Fünf-Minuten-Kerze abwarten.
2. Das aktuelle Tages-Hoch/Tief/Mitte und die Kanalbreite berechnen.
3. Überprüfen, ob die Kanalbreite `GapPoints * PriceStep` überschreitet.
4. Das Tick-Volumen-Verhältnis berechnen = (letztes abgeschlossenes 30-Minuten-Volumen) / (vorheriges 30-Minuten-Volumen) und sicherstellen, dass es größer als 1,5 ist.
5. **Long-Setup**: Die Kerze schließt bei oder über dem aktuellen Tageshoch → kaufen.
6. **Short-Setup**: Die Kerze schließt bei oder unter dem aktuellen Tagestief → verkaufen.
7. Neue Einstiege überspringen, solange eine Position aktiv ist (maximal ein offener Trade).

## Ausstiegsverwaltung
- **Anfangs-Stop**: Long-Trades verwenden das Tagestief, Short-Trades das Tageshoch, das beim Einstieg erfasst wurde.
- **Take-Profit**: Optional; platziert `TakeProfitPoints * PriceStep` vom Einstieg auf beiden Seiten des Marktes.
- **Parabolic SAR-Umkehr**: Schließt die Position, wenn der SAR-Wert den vorherigen Kerzenschluss kreuzt (ursprüngliches EA-Verhalten).
- **Trailing-Stop**: Aktiviert sich, sobald der Gewinn `TrailingStopPoints * PriceStep` überschreitet und bewegt sich um mindestens `TrailingStepPoints * PriceStep`.
- **Spiegel-Trailing-Take**: Wenn der Trailing-Stop aktualisiert wird, wird das Take-Profit-Level symmetrisch um den aktuellen Schluss repositioniert.

## Positionsgrößenbestimmung
- Das Positionsvolumen wird aus `RiskPercent` abgeleitet: Die Strategie riskiert einen Prozentsatz des aktuellen Portfoliowerts basierend auf dem Abstand zwischen Einstieg und Stop.
- Wenn die Portfolio-Bewertung nicht verfügbar ist, fällt der Algorithmus auf `Volume + |Position|` zurück und handelt als letztes Mittel einen einzelnen Kontrakt.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 5-Minuten-Kerzen | Primärer Zeitrahmen für Signale und Parabolic SAR. |
| `VolumeCandleType` | 30-Minuten-Kerzen | Zeitrahmen zur Bewertung der Tick-Volumen-Expansion. |
| `TakeProfitPoints` | 20 | Gewinnziel-Abstand in Preispunkten. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPoints` | 10 | Abstand in Punkten für die Trailing-Stop-Aktivierung. |
| `TrailingStepPoints` | 10 | Minimaler zusätzlicher Fortschritt (in Punkten), bevor der Trailing-Stop erneut bewegt wird. |
| `RiskPercent` | 5 | Prozentualer Anteil des Portfolio-Eigenkapitals, der pro Trade riskiert wird. |
| `GapPoints` | 50 | Minimale Tageskanal-Breite in Punkten, die für einen Ausbruch erforderlich ist. |
| `SessionStartHour` | 7 | Stunde (0–23), nach der die Strategie neue Positionen eröffnen darf. |

## Hinweise
- Die Parabolic SAR-Parameter (Schritt 0,005, Max 0,01) entsprechen der ursprünglichen MQL-Strategie.
- Tages-Mittelpunktwerte werden der Vollständigkeit halber berechnet und können bei Bedarf zur visuellen Referenz geplottet werden.
- Da die Volumenexpansion auf abgeschlossenen 30-Minuten-Kerzen ausgewertet wird, verwendet die Ausbruchsbestätigung die neuesten verfügbaren Close-to-Close-Informationen, was für historische Tests und Live-Trading robust ist.
- Alle Code-Kommentare sind auf Englisch, um den Repository-Richtlinien zu entsprechen.
