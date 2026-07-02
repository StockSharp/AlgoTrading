# Einfache OzFx-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 4 Expert Advisors **OzFx** (Ordner `MQL/7994`) in den StockSharp High-Level API.
- Verwendet den Accelerator/Decelerator-Oszillator (AC) zusammen mit der %K-Linie des stochastischen Oszillators, um Impulsumkehrungen um die Nulllinie zu erkennen.
- Reproduziert das Verhalten des Experten, fünf Marktaufträge mit gestaffelten Take-Profits und Breakeven-Schutz zu stapeln, nachdem das erste Ziel erreicht wurde.

## Handelslogik
1. Bauen Sie den Awesome Oscillator (5/34) und subtrahieren Sie dessen 5-Perioden SMA, um den Accelerator Oscillator-Wert der vorherigen und aktuellen abgeschlossenen Kerze zu erhalten.
2. Abonnieren Sie den stochastischen Oszillator (%K-Länge = `StochasticLength`, Glättung 3/3) und lesen Sie die Hauptlinie bei Kerzenschluss ab.
3. **Lange Einrichtung** erfordert:
   - `%K` über dem konfigurierten Mittelwert (Standard 50).
   - Aktueller AC-Wert positiv und höher als der vorherige.
   - Vorheriger AC-Wert immer noch unter Null (Impuls überschreitet die Basislinie).
4. **Kurzaufbau** spiegelt die Regeln in die entgegengesetzte Richtung.
5. Wenn ein Signal auf einem neuen Balken erscheint, eröffnet die Strategie fünf gleiche Marktaufträge:
   - Die Schichten 1–4 erhalten Take-Profits im Abstand von `TakeProfitPips`-Vielfachen.
   - Schicht 5 hat kein Gewinnziel und bleibt der Entwicklung hinterher.
6. Tritt das gegenteilige Setup auf, während ein Stapel offen ist, werden die verbleibenden Aufträge zum Marktwert geschlossen, sodass die Strategie vor neuen Einträgen unverändert bleibt.

## Positionsmanagement
- Jede Ebene hat die gleiche Stop-Loss-Distanz, die durch `StopLossPips` definiert ist.
- Nachdem der erste Take-Profit ausgeführt wurde, verschärfen die verbleibenden Orders ihre Stops auf den Breakeven-Preis (Einstiegspreis), was der ursprünglichen „Modok“-Logik entspricht.
- Schutzausgänge werden ausgeführt, wenn die Extremwerte der Kerze die gespeicherten Stopp- oder Zielniveaus durchbrechen; Brokerseitige Pending Orders werden nicht genutzt.
- Die Strategie lässt jeweils nur eine Richtung zu und wartet, bis alle Aufträge geschlossen sind, bevor die Eingabeblock-Flags zurückgesetzt werden.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Losgröße für jede der fünf Marktaufträge. | `0.1` |
| `StopLossPips` | Abstand zwischen Einstieg und Stop-Loss, ausgedrückt in Pips. | `100` |
| `TakeProfitPips` | Inkrement zwischen aufeinanderfolgenden Take-Profit-Ebenen (Ebenen 1–4). | `50` |
| `StochasticLevel` | Auf den stochastischen %K-Wert angewendeter Schwellenwert. | `50` |
| `StochasticLength` | Lookback-Zeitraum der stochastischen %K-Berechnung. | `5` |
| `CandleType` | Von der Strategie verwendete Quellkerzenserie (standardmäßig 4-Stunden-Kerzen). | `4h time-frame` |

## Implementierungshinweise
- Signale werden nur bei fertigen Kerzen ausgewertet, um im Einklang mit dem MT4-Experten zu bleiben, der an neuen Balken arbeitet.
- Die Pip-Konvertierung passt sich automatisch an 3/5-stellige Forex-Symbole an, indem der minimale Preisschritt bei Bedarf mit 10 multipliziert wird.
- Gestaffelte Ein- und Ausstiege werden im Speicher über geschichtete Objekte abgewickelt, sodass die Strategie Teile der Position ordnungsgemäß schließen kann.
- Alle Kommentare im C#-Code sind gemäß den Repository-Richtlinien in Englisch verfasst.
