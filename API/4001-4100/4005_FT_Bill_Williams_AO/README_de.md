# FT Bill Williams AO-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **FT Bill Williams AO Strategy** ist eine High-Level-StockSharp-Portierung des MetaTrader 4-Experten `FT_BillWillams_AO`. Das Original
Der Roboter wurde auf FORTRADER.RU veröffentlicht und kombiniert Bill Williams-Fraktale, den Alligator-Indikator und den Awesome Oscillator
Identifizieren Sie frühe Breakout-Chancen. Die StockSharp-Version behält die ursprüngliche Logik bei, arbeitet jedoch mit einer einzelnen Nettoposition statt
mehrere gleichzeitige Bestellungen.

Der Algorithmus arbeitet mit abgeschlossenen Kerzen innerhalb eines konfigurierbaren Zeitrahmens. Jede Bar es:

1. Erkennt bullische und bärische Fraktale, die aus einer ungeraden Anzahl von Kerzen entstehen.
2. Filtert Fraktale, indem überprüft wird, ob der Fraktalpreis außerhalb der Alligator-Zahnlinie liegt.
3. Wartet darauf, dass der Awesome Oscillator (AO) das klassische dreitaktige Beschleunigungsmuster bildet.
4. Platziert einen Ausbruchsauslöser über/unter dem aktuellen Hoch oder Tief, verschoben um eine benutzerdefinierte Anzahl von MetaTrader Punkten.
5. Wendet die Gragus-Trailing-Routine von Bill Williams und optionale Jaw-basierte Exit-Regeln an.

## Eingabelogik
### Lange Einträge
- Ein bullisches Fraktal erscheint und sein hoher Preis liegt über den Alligator-Zähnen.
- Die vor `SignalShift + 2`, `SignalShift + 1` und `SignalShift` Kerzen genommenen AO-Werte erfüllen `A > B`, `B < C`, und alle drei sind es
positiv.
- Ein ausstehendes Ausbruchsniveau wird als `High[SignalShift] + IndentPoints * price step` berechnet.
- Wenn eine abgeschlossene Kerze dieses Niveau überschreitet und AO weiterhin steigt (`C > B`), öffnet sich die Strategie oder kehrt sich in eine Long-Position um.

### Kurze Einträge
- Ein bärisches Fraktal erscheint und sein Tief liegt unter den Alligator Zähnen.
- AO-Werte erfüllen `A < B`, `B > C` und alle drei sind negativ.
- Ein Breakout-Trigger wird bei `Low[SignalShift] - IndentPoints * price step` platziert.
- Eine Short-Position (oder Umkehrung von Long) wird eröffnet, wenn die Kerze unter diesen Auslöser fällt, während AO weiter fällt (`C < B`).

## Exit- und Risikomanagement
- Anfänglicher Stop-Loss und Take-Profit werden in MetaTrader Punkten ausgedrückt und über das Instrument in den tatsächlichen Preisabstand übersetzt
Preisschritt.
- Der **CloseDropTeeth**-Modus kann Positionen schließen, wenn entweder der aktuelle oder der vorherige Schluss den Alligator-Kiefer kreuzt.
- **CloseReverseSignal** bestimmt, ob ein entgegengesetztes Fraktal oder die Aktivierung des entgegengesetzten Breakout-Signals einen erzwingen soll
Ausgang.
- Der Schalter **UseTrailing** aktiviert die ursprüngliche Gragus-Trailing-Stop-Routine: wenn die Alligator-Lippen schneller als kurz vorrücken
SMA, der Anschlag wird an die Lippen bewegt; sonst zieht es die Zähne hinter sich her. Bei beiden Zügen muss der Preis mindestens 12 Punkte entfernt sein
von der Ziellinie entfernt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `TradeVolume` | Bestellgröße in Losen. Es wird auch nach `Strategy.Volume` geschrieben. |
| `CandleType` | Datentyp und Zeitrahmen der Eingabekerzen. |
| `FractalPeriod` | Ungerade Anzahl von Kerzen, die zur Bestätigung von Fraktalen verwendet werden (Standard 5). |
| `IndentPoints` | MetaTrader Punkte wurden über/unter dem Hoch/Tief der Ausbruchskerze hinzugefügt. |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Länge der geglätteten gleitenden Durchschnitte, die von den Alligator-Linien verwendet werden. |
| `JawShift`, `TeethShift`, `LipsShift` | Vorwärtsverschiebung (in Kerzen), angewendet auf die Alligator-Linien. |
| `CloseDropTeeth` | Verhalten der kieferbasierten Schließregel: deaktiviert, aktuelle Nahkreuzung oder vorherige Nahkreuzung. |
| `CloseReverseSignal` | Austrittsbedingung bei entgegengesetzten Signalen: deaktiviert, bei neuem Fraktal oder sobald der entgegengesetzte Ausbruch aktiviert ist. |
| `UseTrailing` | Aktiviert oder deaktiviert die Gragus-Trailing-Stop-Routine. |
| `TrendSmaPeriod` | Periode des Hilfselements SMA, das vom abschließenden Vergleich verwendet wird. |
| `StopLossPoints` | Anfängliche Stop-Loss-Distanz in MetaTrader Punkten. Zum Deaktivieren auf Null setzen. |
| `TakeProfitPoints` | Anfängliche Take-Profit-Distanz in MetaTrader Punkten. Zum Deaktivieren auf Null setzen. |
| `SignalShift` | Anzahl der vollständig geschlossenen Kerzen, die beim Lesen der AO-Werte und der jüngsten Höchst-/Tiefstwerte übersprungen wurden. |

## Notizen
- Die Strategie geht davon aus, dass die Sicherheit ein gültiges `PriceStep` offenlegt (fällt auf `MinPriceStep` zurück); Wenn beide fehlen, ist ein Standardwert von
`0.0001` wird verwendet.
- Es wird nur eine Nettoposition verwaltet. Umkehrsignale schließen automatisch die gegenüberliegende Position, bevor sie eine neue eröffnen.
- Um optimale Ergebnisse zu erzielen, halten Sie `FractalPeriod` ungerade; Der ursprüngliche Experte verwendete 5 Kerzen.
- `IndentPoints`, `StopLossPoints` und `TakeProfitPoints` imitieren MetaTrader Punkte. Passen Sie sie entsprechend dem Preis des Instruments an
Skala.
