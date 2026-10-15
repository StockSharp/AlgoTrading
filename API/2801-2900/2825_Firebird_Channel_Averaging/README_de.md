# Firebird Kanal-Averaging-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Firebird Kanal-Averaging-Strategie repliziert den MetaTrader 5-Experten "Firebird v0.60" unter Verwendung der StockSharp-High-Level-API. Sie handelt einen konfigurierbaren gleitenden Durchschnittskanal und mittelt progressiv in Positionen, wenn sich der Preis vom Kanal entfernt. Der Ansatz ist für Mean-Reversion-Forex-Trading konzipiert, bei dem Grid-artige Einstiege und pip-basierte Risikokontrollen erforderlich sind.

## Indikator-Setup
- Ein gleitender Durchschnitt (einfach, exponentiell, geglättet oder gewichtet) wird auf der ausgewählten Kerzenserie berechnet. Die Preisquelle (Schluss, Hoch, Tief, Median usw.) kann konfiguriert werden.
- Obere und untere Kanalbänder werden durch Verschieben des gleitenden Durchschnitts um einen benutzerdefinierten Prozentsatz abgeleitet.

## Einstiegslogik
1. **Kaufbedingungen**
   - Der Preis der gewählten Kerzenquelle schließt unterhalb des unteren Bandes.
   - Entweder existiert keine Position, oder der neue Einstieg liegt mindestens `Step (pips)` vom letzten Fill entfernt, wenn das `Step Exponent`-Wachstum berücksichtigt wird.
   - Die Strategie erzwingt eine Abklingzeit von zwei Kerzenintervallen zwischen Einstiegen.
2. **Verkaufsbedingungen**
   - Der Preis schließt oberhalb des oberen Bandes.
   - Identische Abstands- und Abklingprüfungen wie bei der Long-Logik müssen erfüllt sein.

Bei einem gültigen Signal sendet die Strategie eine Market Order mit dem konfigurierten Lot-Volumen. Es wird immer nur eine Richtung gehalten – entgegengesetzte Signale warten, bis das aktuelle Inventar durch Risikoregeln geschlossen wird.

## Positionsmanagement
- Jeder Einstieg wird gespeichert, damit die Strategie den Durchschnittspreis des offenen Grids berechnen kann.
- Stop-Loss- und Take-Profit-Niveaus werden in Pips definiert. Für eine einzelne Position entspricht der Stop-Loss dem Einstiegspreis minus/plus `Stop Loss (pips)` und der Take-Profit dem Einstiegspreis plus/minus `Take Profit (pips)`.
- Wenn mehrere Positionen vorhanden sind, wird die Stop-Loss-Distanz durch die Anzahl der Einstiege dividiert, was das Averaging-Verhalten des ursprünglichen Experten emuliert.
- Gewinnziele bleiben relativ zum Durchschnittspreis fest, während Stop-Loss-Ausstiege bei jeder Kerze neu berechnet werden.
- Der Handel kann optional an Freitagen deaktiviert werden.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `Volume` | Ordergröße in Lots für jeden gemittelten Einstieg (Standard 0.1). |
| `Stop Loss (pips)` | Schutz-Stop-Distanz in Pips (Standard 50). |
| `Take Profit (pips)` | Take-Profit-Distanz in Pips (Standard 150). |
| `MA Period` | Rückblicklänge des gleitenden Durchschnitts (Standard 10). |
| `MA Shift` | Vorwärtsverschiebung in Kerzen, die auf die Ausgabe des gleitenden Durchschnitts angewendet wird. |
| `MA Type` | Berechnungsmethode des gleitenden Durchschnitts: Simple, Exponential, Smoothed oder Weighted. |
| `Price Source` | Kerzenpreis für Indikatorberechnungen (Standard Schluss). |
| `Channel %` | Prozentualer Versatz vom gleitenden Durchschnitt zur Bildung der Bänder (Standard 0.3%). |
| `Trade Friday` | Aktiviert oder deaktiviert den Handel an Freitagen. |
| `Step (pips)` | Minimale Pip-Distanz zwischen gemittelten Orders (Standard 30). |
| `Step Exponent` | Exponent, der den Schritt basierend auf der Anzahl offener Einstiege skaliert (0 hält den Schritt konstant). |
| `Candle Type` | Zeitrahmen für die Arbeitskerzen. |

## Hinweise
- Die Strategie setzt voraus, dass `PriceStep` des Instruments einen Pip repräsentiert. Falls nicht verfügbar, wird auf 0.0001 zurückgegriffen.
- Schutzausstiege werden mit Market Orders statt nativer Stop-/Limit-Orders ausgeführt, um mit der High-Level-API konsistent zu bleiben.
- Das Averaging-Grid wird durch die Abklinglogik und durch die wachsende Distanz begrenzt, wenn ein Schrittexponent größer als null verwendet wird.
