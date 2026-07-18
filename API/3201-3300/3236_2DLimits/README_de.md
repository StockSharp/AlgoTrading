# 2DLimits-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
2DLimits ist eine direkte Portierung des MetaTrader 4-Expert-Advisors `2DLimits_EA_v2`. Die Strategie bewertet die letzten zwei abgeschlossenen Tageskerzen und nimmt nur teil, wenn sie ein Treppenmuster bilden (höhere Hochs/Tiefs oder niedrigere Hochs/Tiefs). Wenn das Muster gültig ist, setzt die Strategie Stop-Orders am vorherigen Tagesextrem und schützt die Position mit einem Midpoint-Stop-Loss und einem Ziel, das dem vorherigen Tagesbereich entspricht.

Die Implementierung basiert auf StockSharp's High-Level-Kerzenabonnements zusammen mit Level-1-Kursen. Tageskerzen liefern die Ausbruchsniveaus, während die besten Bid/Ask-Snapshots sicherstellen, dass Long-Setups nur aktiv sind, wenn der Preis unter dem Midpoint handelt, und Short-Setups nur, wenn er darüber handelt.

## Strategielogik
### Tagesstrukturfilter
* Die Strategie hält ein Zwei-Tage-Rollfenster abgeschlossener Tageskerzen (konfigurierbar durch den Kerzentypparameter).
* Ein **bullisches Setup** erfordert, dass der jüngste Tag sowohl ein höheres Hoch als auch ein höheres Tief im Vergleich zum Vortag registriert.
* Ein **bearisches Setup** erfordert, dass der jüngste Tag sowohl ein niedrigeres Hoch als auch ein niedrigeres Tief als der frühere Tag aufweist.
* Der Mittelpunkt des jüngsten Tages wird als `(high + low) / 2` berechnet, und der Kerzenbereich wird für das Gewinnziel gespeichert.

### Einstiegsregeln
* Es ist jeweils nur ein Stapel ausstehender Orders aktiv; alle Orders werden storniert und neu berechnet, wenn eine neue Tageskerze schließt.
* Long-Einstiege werden vorbereitet, wenn:
  * Der bullische Strukturfilter erfüllt ist.
  * Der letzte Ask-Preis unter dem Mittelpunkt des Vortags liegt (spiegelt die `Ask < middleY`-Prüfung des ursprünglichen EA wider).
  * Eine Buy-Stop-Order genau am Hoch des Vortags platziert wird.
* Short-Einstiege werden vorbereitet, wenn:
  * Der bearische Strukturfilter erfüllt ist.
  * Der letzte Bid-Preis über dem Mittelpunkt des Vortags liegt (spiegelt `Bid > middleY` wider).
  * Eine Sell-Stop-Order am Tief des Vortags platziert wird.
* Wenn beide Strukturprüfungen fehlschlagen, werden keine Orders für die kommende Sitzung belassen.

### Ausstiegsregeln
* Wenn eine Stop-Order ausgelöst wird, wird die entgegengesetzte Einstiegsorder sofort storniert, damit die Strategie niemals gleichzeitige Long- und Short-Engagements hält.
* Nach einem Long-Ausbruch werden zwei Schutzorders registriert:
  * Eine Stop-Order am Mittelpunkt des Referenztages dient als Stop-Loss.
  * Eine Take-Profit-Order bei `vorheriges Hoch + vorheriger Bereich` entspricht der MetaTrader-Take-Profit-Distanz.
* Nach einem Short-Ausbruch wird symmetrischer Schutz angewendet:
  * Eine Stop-Order am Mittelpunkt (Buy-Stop) deckt den Stop-Loss ab.
  * Eine Take-Profit-Order bei `vorheriges Tief - vorheriger Bereich` spiegelt das ursprüngliche Ziel wider.
* Schutzorders werden erneut aktiviert, wenn sich die gefüllte Positionsgröße ändert, und werden entfernt, sobald die Position zu flat zurückkehrt.

### Orderlebenszyklus und Sicherheitsprüfungen
* Ausstehende Orders werden nur nach der nächsten abgeschlossenen Tageskerze aktualisiert, was ein einziges Setup pro Handelstag erzwingt.
* Die Strategie überspringt die Signalgenerierung, wenn sie bereits eine Position hält, um Überschneidungen zwischen Setups zu verhindern.
* Der letzte Bid/Ask-Snapshot wird von `SubscribeLevel1()` beibehalten; wenn nicht verfügbar, wird der letzte Handelspreis als Fallback verwendet, um das Einreichen blinder Orders zu vermeiden.
* Die Rundung erfolgt mit dem Instrument-Preisschritt, sodass alle Orders an der Börsen-Tick-Größe ausgerichtet sind.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Volume` | Ordervolumen für die Stop-Einstiege. Muss größer als null sein. |
| `CandleType` | Kerzentyp für den Referenzbereich (Standard: Tageskerzen). |

## Zusätzliche Hinweise
* Die Strategie verwaltet jede Order direkt über die High-Level-API; es gibt keine Abhängigkeit von benutzerdefinierten Sammlungen oder Indikatorpuffern.
* In diesem Paket wird nur die C#-Implementierung bereitgestellt. Für diese Konvertierung wird keine Python-Version erstellt.
* Tests bleiben wie gewünscht unverändert.
