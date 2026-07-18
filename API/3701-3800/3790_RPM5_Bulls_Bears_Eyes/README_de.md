# RPM5 BullsBearsEyes-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **RPM5 BullsBearsEyes Strategy** ist eine C#-Portierung des MetaTrader 4-Experten *Rpm5_mt4v1*. Der Berater baute den benutzerdefinierten BullsBearsEyes-Oszillator anhand der Bulls Power- und Bears Power-Messwerte um und eröffnete eine einzelne Position, die der vorherrschenden Tendenz folgte. Diese StockSharp-Version reproduziert das gleiche Verhalten unter Verwendung des High-Level-API und behält dabei die ursprünglichen Risikoparameter, die nachgestellte Logik und die Signalschwellenwerte bei.

## Rekonstruktion des Indikators
- Zwei klassische Oszillatoren – **Bulls Power** und **Bears Power** – werden auf der konfigurierten Kerzenserie berechnet.
- Ihre Summe durchläuft den identischen vierstufigen IIR-Glätter, der vom MT4-Indikator verwendet wird. Der Glättungsfaktor (`Gamma`) steuert, wie schnell der Oszillator reagiert.
- Die gefilterte Ausgabe wird in einen Wert zwischen **0** und **1** umgewandelt. Werte über dem zentralen Schwellenwert signalisieren eine bullische Dominanz, Werte darunter deuten auf eine bärische Kontrolle hin. Eine exakte Null oder Eins erscheint, wenn eine Seite vollständig erschöpft ist, was den ursprünglichen Randfällen des Indikators entspricht.

## Handelsregeln
1. Die Strategie abonniert den ausgewählten Zeitrahmen (standardmäßig 5 Minuten) und wartet nur auf abgeschlossene Kerzen.
2. Im flachen Zustand wird das BullsBearsEyes-Verhältnis ausgewertet:
   - **Langer Eintrag** – aktueller Wert streng über `Threshold` (Standard 0,5).
   - **Kurzer Eintrag** – aktueller Wert strikt unter dem `Threshold`.
   - Der Algorithmus behält höchstens eine offene Position. Gegensignale werden ignoriert, bis die aktive Position durch das Risikomanagement vollständig geschlossen ist.
3. Sobald ein Trade abgeschlossen ist, bleibt die Position unberührt, bis ein Stop-Loss-, Take-Profit- oder Trailing-Stop-Ereignis eintritt.

## Risikomanagement
- **Stop-Loss-/Take-Profit-Abstände** werden anhand der ursprünglichen 25/150-Pip-Einstellungen wiederhergestellt. Sie werden mit dem Instrument `PriceStep` (Pip) jedes Mal neu berechnet, wenn eine neue Position eröffnet wird.
- **ATR nachlaufend**: Bei jeder fertigen Kerze wird der Average True Range (Zeitraum `AtrPeriod`, Standard 5) ausgewertet. Die Nachlaufdistanz entspricht einem Pip plus `AtrMultiplier × ATR`. Wenn der Abschluss über diesen Abstand hinausgeht, wird der Schutzanschlag verschärft, um die Lücke aufrechtzuerhalten, identisch mit der MQL-Logik, die wiederholt `OrderModify` aufgerufen hat.
- Schutzstufen werden vor der Verarbeitung neuer Signale überprüft, um sicherzustellen, dass Exits immer Vorrang vor neuen Einträgen haben, genau wie in der Quelle EA.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Bulls/Bears Period` | 13 | Mittelungszeitraum für die Indikatoren Bulls Power und Bears Power. |
| `Gamma` | 0,5 | Vierstufiges IIR-Glättungsverhältnis für den BullsBearsEyes-Oszillator. |
| `Threshold` | 0,5 | Trennlinie zwischen bullischen (> Schwellenwert) und bärischen (< Schwellenwert) Zonen. |
| `ATR Period` | 5 | Lookback, der für den ATR-basierten Trailing Stop verwendet wird. |
| `ATR Multiplier` | 1.5 | Beim Ableiten der Nachlaufstrecke wird ein Multiplikator auf ATR angewendet. |
| `Stop Loss (pips)` | 25 | Schutzstoppdistanz, umgerechnet von Pips in Preis. |
| `Take Profit (pips)` | 150 | Gewinnzielentfernung, umgerechnet von Pips in Preis. |
| `Trade Volume` | 1 | Market-Order-Volumen, das für jede neue Position verwendet wird. |
| `Candle Type` | 5-Minuten-Kerzen | Von der Strategie verarbeiteter Zeitrahmen. |

## Notizen
- Der Port zeichnet nicht die visuellen täglichen Kanalobjekte, die in MT4 vorhanden waren, da sie nur kosmetischer Natur waren.
- Alle Kommentare im Code sind wie gewünscht in Englisch verfasst.
- Die Tests bleiben unverändert; Führen Sie die vorhandenen Lösungsebenenprüfungen durch, wenn eine Validierung erforderlich ist.
