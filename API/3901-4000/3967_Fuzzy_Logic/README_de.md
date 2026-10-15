# Fuzzy-Logic-Legacy-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert den Expert Advisor „Fuzzy Logic“ von 2007 in StockSharp. Es kombiniert mehrere Bill Williams-Tools
mit Impulsoszillatoren und wertet diese anhand einer Fuzzy-Scoring-Tabelle aus. Nur wenn der aggregierte Score ein starkes bullisches o zeigt
Bei einem bärischen Konsens eröffnet das System eine neue Position. Ein fester Stop-Loss und ein optionaler Trailing-Stop spiegeln den ursprünglichen Handelswert wider
anagement rules.

## Handelslogik

1. Erstellen Sie die Rechnung Williams Alligator (Kiefer, Zähne, Lippen) mit geglätteten gleitenden Durchschnitten und berechnen Sie den *Gator*-Spread als su
m der absoluten Abstände zwischen den Linien.
2. Berechnen Sie Williams %R (Periode 14), DeMarker (Periode 14) und RSI (Periode 14) für dieselben Kerzen.
3. Leiten Sie den Accelerator Oscillator (AC) aus der Awesome Oscillator-Sequenz ab und verfolgen Sie bis zu fünf aufeinanderfolgende Balken, um AC zu erkennen
Beschleunigungsstreifen.
4. Jeder Indikator speist eine fünfstufige Fuzzy-Mitgliedschaftstabelle mit vordefinierten Haltepunkten, die aus dem Originalcode kopiert wurden.
5. Gewichtete Summen der Mitgliedschaften ergeben einen Entscheidungswert zwischen 0 und 1:
   - Werte **> 0,75** weisen auf einen bullischen Konsens hin und lösen Long-Einstiege aus.
   - Werte **< 0,25** weisen auf einen rückläufigen Konsens hin und lösen Short-Einstiege aus.
6. Es kann jeweils nur eine Position offen sein. Unmittelbar nach der Einfahrt werden Schutzstopper angebracht.

## Positionsmanagement

- **Stop-Loss**: Feste Distanz in Preisschritten (Parameter `Stop Loss (points)`).
- **Trailing Stop**: Optional; Wenn es aktiviert ist, folgt es dem Schutzstopp um die angegebene Anzahl von Preisschritten.
- **Geldmanagement**: Optionale saldobasierte Größenanpassung, die die MetaTrader-Formel „Volumen = (Balance * (ProzentMM + DeltaM) nachahmt
M) - Anfangssaldo * DeltaMM) / 10000`.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `Candle Type` | Zur Analyse verwendete Kerzendatenreihe. |
| `Long Threshold` | Entscheidungsniveau, das überschritten werden muss, um eine Long-Position zu eröffnen. |
| `Short Threshold` | Decision level that must be crossed to open a short position. |
| `Stop Loss (points)` | Abstand des anfänglichen Stop-Loss in Preisschritten. |
| `Trailing Stop (points)` | Entfernung des Trailing Stops in Preisschritten; zum Deaktivieren auf `0` setzen. |
| `Fixed Volume` | Handelsvolumen, wenn die Geldverwaltung deaktiviert ist. |
| `Use Money Management` | Aktiviert die Geldverwaltungsformel im MetaTrader-Stil. |
| `Percent MM` | Prozentsatz des Kontostands, der in der Geldverwaltungsformel verwendet wird. |
| `Delta MM` | Zusätzlicher prozentualer Ausgleich für die Money-Management-Formel. |
| `Initial Balance` | Referenzsaldo, der von der Geldverwaltungsformel verwendet wird. |

## Notizen

- Die Strategie verwendet nur abgeschlossene Kerzen (`CandleStates.Finished`), um ein Neuzeichnen zu vermeiden.
- All indicator levels and weights follow the original expert advisor, preserving its behaviour.
- Um das System im Tagesverlauf auszuführen, passen Sie den Zeitrahmen und die Schwellenwerte der Kerze an, um die gewünschte Volatilität widerzuspiegeln.
