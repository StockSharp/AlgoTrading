# Strategie TDSGlobal 4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
TDSGlobal 4 ist eine Konvertierung des MetaTrader 4 Expert Advisors „TDSGlobal 4“. Das ursprüngliche System wendet das Triple von Alexander Elder an
Bildschirmmethode durch Kombination der Steigung eines täglichen MACD-Histogramms (OsMA) mit einem Williams %R-Filter. Befehle werden nur dann eingesetzt, wenn dies der Fall ist
Das tägliche Momentum richtet sich nach den Extremwerten des Oszillators, woraufhin die Strategie die Spanne des Vortages mit dem ausstehenden Sto einschließt
p Bestellungen. Der StockSharp-Port behält die gleiche Breakout-Logik bei und fügt eine präzise Planung hinzu, sodass verschiedene FX-Symbole gestaffelt ausgelöst werden
rote Minuten und verwaltet das offene Engagement mit optionalen Trailing Stops sowie konfigurierbaren Take-Profit-Zielen.

## Strategielogik
### Filter für höhere Zeitrahmen
* **MACD Steigung** – vergleicht die letzten beiden abgeschlossenen täglichen MACD Hauptwerte (schneller EMA 12, langsamer EMA 26, Signal EMA 9). Der Bias ist b
ullish, wenn der jüngste Wert den vorherigen übersteigt, bearish, wenn er niedriger ist, und neutral, wenn sie gleich sind.
* **Williams %R** – wertet den täglichen Williams %R aus (Zeitraum 24). Lange Setups sind nur zulässig, wenn der Messwert über dem oberen Wert liegt
Schwellenwert (Standardwert −25, d. h. überkaufte Stärke), während Short-Setups erfordern, dass der Wert unter dem unteren Schwellenwert bleibt (de
Fehler −75).

### Breakout-Platzierung
* **Preisniveaus** – bei jeder abgeschlossenen Tageskerze zeichnet die Strategie das Hoch und Tief des Vortages auf. Neue Stop-Orders sind möglich
einen Pip über diese Extremwerte hinaus (konfigurierbar über *EntryBufferPips*) und imitierte damit den ±1-Punkt-Versatz des ursprünglichen EA.
* **Distanzschutz** – vor dem Senden einer Stop-Order erzwingt der Code eine Mindestlücke zwischen dem aktuell besten Kurs und dem Einstiegspreis
Eis (Standard 16 Pips, entsprechend dem 16 *Punkte*-Check von EA). Dies verhindert, dass ausstehende Aufträge zu nahe an der Marke abgelegt werden
t wenn die Volatilität gering ist.
* **Directional Gating** – Kaufstopps werden nur erstellt, wenn die MACD-Steigung positiv ist und der Williams %R eine bullische Tendenz bestätigt. S
Ellenstopps erfordern eine negative Steigung und einen Williams %R, der auf einen rückläufigen Druck hinweist.

### Ausstehende Auftragswartung
* **Tägliches Zurücksetzen** – wenn eine neue tägliche Kerze schließt, werden alle verbleibenden ausstehenden Aufträge storniert, sodass die nächste Handelssitzung beginnt
ts mit einer sauberen Weste. Wenn die Filter einen Handel nicht zulassen, werden für diesen Tag keine Aufträge erteilt.
* **Ein Trade pro Tag** – Sobald die Aufträge für einen bestimmten Tag ausgewertet wurden (unabhängig davon, ob sie platziert oder übersprungen wurden), wartet die Strategie
s für den nächsten Tagesschluss vor einer Neubewertung. Ausgeführte Stop-Orders stornieren automatisch die Gegenseite, um ein gleichzeitiges Lo zu vermeiden
ng/kurze Belichtung.

### Risikomanagement
* **Schutzstopps** – Long-Positionen erben einen schützenden Ausstieg knapp unter dem Tief des Vortages, während Short-Positionen diesen nutzen
vorheriges Hoch. Diese Werte werden im einminütigen Trigger-Stream überwacht.
* **Take Profit** – optionale feste Ziele, ausgedrückt in Pips im Verhältnis zum tatsächlichen Ausführungspreis. Setzen Sie *TakeProfitPips* auf `0`, um zu dis
in der Lage, das Ziel zu erreichen und die MT4-Einstellung zu spiegeln.
* **Trailing Stop** – wenn *TrailingStopPips* größer als Null ist, liest die Strategie die besten Geld-/Briefkurse aus Level1-Daten und Trail
Sobald sich der Preis zu Gunsten des Handels bewegt hat, ist dies der Stopp. Wenn das Trailing-Level durchbrochen wird, wird die Position zum Marktwert geschlossen.

### Terminplanung
* **Minutenfenster** – um gleichzeitige Übermittlungen über verschiedene Währungspaare hinweg zu vermeiden, verwendete EA symbolspezifische Minutenfenster
ws. Der Port reproduziert dieses Verhalten: USDCHF verwendet die Minuten 0/8/16/24/32/40/48, GBPUSD 2/10/18/26/34/42/50, USDJPY 4/12/20/28/36/44
/52 und EURUSD 6/14/22/30/38/46/54. Alle anderen Instrumente greifen auf die gesamte Stunde (0–59) zurück.
* **Trigger-Stream** – ein einminütiges Kerzenabonnement steuert sowohl die Planung der täglichen Orders als auch den Intraday-Stop/Take-
Gewinnschecks. Die eigentliche Signalauswertung erfolgt nur einmal pro Handelstag in der ersten zulässigen Minute.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Volume` | Auftragsvolumen für Stop-Einträge. | `1` |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | MACD-Konfiguration zur Messung der täglichen Steigung. | `12 / 26 / 9` |
| `WilliamsPeriod` | Lookback für den Williams %R-Filter. | `24` |
| `WilliamsBuyLevel` | Oberer Schwellenwert (typischerweise –25), der erforderlich ist, bevor Langbestellungen aktiviert werden. | `-25` |
| `WilliamsSellLevel` | Unterer Schwellenwert (normalerweise –75) erforderlich, bevor Short-Orders aktiviert werden. | `-75` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips; `0` deaktiviert das Ziel. | `999` |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips; `0` deaktiviert das Trailing. | `10` |
| `EntryBufferPips` | Offset, der über das Hoch/Tief des Vortages hinaus hinzugefügt wird, bevor eine Stop-Order platziert wird. | `1` |
| `MinDistancePips` | Mindest-Pip-Abstand vom aktuellen Angebot zur ausstehenden Order. | `16` |
| `DailyCandleType` | Zeitrahmen, der die %R-Filter MACD und Williams speist. | `1 day` Kerzen |
| `TriggerCandleType` | Geringerer Zeitrahmen zur Planung und Überwachung von Aufträgen. | `1 minute` Kerzen |

## Zusätzliche Hinweise
* Die C#-Implementierung basiert vollständig auf hochrangigen StockSharp-Helfern (`SubscribeCandles`, `BuyStop`, `SellStop`, Level1-Bindi
ng), so dass es innerhalb der Plattform wiederverwendet werden kann, ohne dass manuelle Installationsarbeiten erforderlich sind.
* Für den Trailing-Stop-Vorgang sind Level-1-Daten erforderlich, da der Algorithmus die besten Geld-/Briefkurse zum Bewegen und Auslösen verwendet
virtueller Halt.
* Das Paket enthält keine Python-Übersetzung; Es werden lediglich die C#-Strategie und die mehrsprachige Dokumentation bereitgestellt.
