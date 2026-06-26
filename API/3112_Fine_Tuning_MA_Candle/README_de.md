# Exp Fine-Tuning-MA-Kerzen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 5-Experten `Exp_FineTuningMACandle.mq5`, der auf Basis der Farbe des *Fine Tuning MA Candle*-Indikators handelt.
- Entwickelt für StockSharp's High-Level-API: abonniert eine einzelne Kerzenserie, leitet Indikatorwerte über `BindEx` ab und leitet alle Orders über die `Strategy`-Hilfsmethoden weiter.
- Implementiert dieselben Einstiegsberechtigungen und bedingten Schließungen wie der ursprüngliche Experte unter Beachtung des asynchronen Ausführungsmodells von StockSharp.

## Fine Tuning MA Candle-Indikator
- Der Indikator erstellt synthetische OHLC-Kerzen durch ein dreistufiges Gewichtungsschema der letzten `Length` Kerzen der Preisserie.
  - `Rank1`, `Rank2` und `Rank3` steuern die Krümmung der Gewichtungsrampen, während `Shift1`, `Shift2` und `Shift3` die Rampen mit einer flachen Komponente mischen.
  - Die Gewichtung ist symmetrisch: die erste Hälfte des Fensters beschleunigt sich zur Mitte, die zweite Hälfte verlangsamt sich von ihr weg.
  - Nach der Normalisierung produzieren die vier gewichteten Summen geglättete Eröffnungs-, Hoch-, Tief- und Schlusskurse.
- Wenn sich geglättete Eröffnung und Schluss um weniger als `GapPoints` (in den Preisschritt des Instruments umgerechnet) unterscheiden, wird die Eröffnung durch den vorherigen synthetischen Schluss ersetzt, um Preislücken zu entfernen.
- Die Kerze wird **2** (bullish) eingefärbt wenn `Open < Close`, **0** (bearish) wenn `Open > Close`, und **1** wenn sie gleich sind. Nur der Farbstrom wird für Handelsentscheidungen verwendet.
- `PriceShiftPoints` verschiebt jede synthetische Kerze vertikal um eine konfigurierbare Anzahl von Preisschritten.

## Handelsregeln
- Signale werden nur bei abgeschlossenen Kerzen erzeugt. Die Strategie speichert die Indikatorfarben und bewertet die Kerze, die `SignalBar` Schritte hinter der letzten abgeschlossenen liegt.
- **Bullische Rotation (Farbe wechselt zu 2):**
  - Bestehende Short-Positionen werden geschlossen, wenn `SellPosClose` aktiviert ist.
  - Sobald die Position flat ist und `BuyPosOpen` erlaubt ist, wird eine Long-Marktorder für `Volume` Lots gesendet. Musste zuvor ein Short geschlossen werden, wird der Long-Einstieg in die Warteschlange gestellt und ausgeführt, sobald die Position auf null zurückkehrt.
- **Bearische Rotation (Farbe wechselt zu 0):**
  - Bestehende Long-Positionen werden geschlossen, wenn `BuyPosClose` aktiviert ist.
  - Sobald flat und `SellPosOpen` erlaubt, wird eine Short-Marktorder für `Volume` Lots gesendet. Ausstehende Einstiege werden genauso behandelt wie bei Long-Signalen.
- Neutrale Farbe (1) löst keine Aktion aus.
- Orders werden nicht gestapelt: die Strategie öffnet höchstens eine Position gleichzeitig und wartet auf das Schließen aktiver Positionen, bevor sie umkehrt.

## Risikomanagement
- `StopLossPoints` und `TakeProfitPoints` stellen Abstände in Preisschritten dar. Nach dem Füllen einer neuen Position registriert die Strategie schützende Stop- und Zielorders unter Verwendung des tatsächlichen Füllpreises aus `OnNewMyTrade`.
- Schutzorders werden automatisch storniert, wenn die Position auf null zurückkehrt oder wenn eine neue Order in die Warteschlange gestellt wird.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Kerzendatentyp/Zeitrahmen für Indikatorberechnungen. |
| `Length` | Anzahl der vom gewichteten Fenster des Indikators verarbeiteten Kerzen. |
| `Rank1`, `Rank2`, `Rank3` | Potenzkoeffizienten, die die drei Gewichtungsstufen formen. |
| `Shift1`, `Shift2`, `Shift3` | Mischfaktoren (0–1), die die Gewichtungsstufen mit einer flachen Komponente kombinieren. |
| `GapPoints` | Maximale Differenz zwischen synthetischer Eröffnung und synthetischem Schluss, die durch Kopieren des vorherigen Schlusses unterdrückt wird. Angegeben in Preisschritten. |
| `SignalBar` | Wie viele geschlossene Kerzen übersprungen werden, bevor die Indikatorfarbe gelesen wird. `1` bedeutet "letzte abgeschlossene Kerze verwenden". |
| `BuyPosOpen` / `SellPosOpen` | Öffnen von Long/Short-Positionen erlauben. |
| `BuyPosClose` / `SellPosClose` | Schließen von Long/Short-Positionen bei entgegengesetzter Farbe erlauben. |
| `StopLossPoints` | Abstand vom Füllpreis zum Schutz-Stop. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPoints` | Abstand vom Füllpreis zum Gewinnziel. Auf `0` setzen zum Deaktivieren. |
| `PriceShiftPoints` | Vertikale Verschiebung der synthetischen Kerzen in Preisschritten. |

## Implementierungshinweise
- Verwendet `BindEx`, weil der benutzerdefinierte Indikator ein komplexes Wertobjekt zurückgibt, das synthetisches OHLC und Farbe gleichzeitig bereitstellt.
- Speichert nur eine kleine Historie von Farbwerten (`SignalBar + 2` Einträge), um Farbwechsel zu erkennen, ohne große Puffer zu speichern.
- Einstiegsumkehrungen beachten das asynchrone Ausführungsmodell, indem sie warten, bis die Position flat ist, bevor die Gegenorder gesendet wird.
