# Strategie für das Positionssystem mit gleitendem Durchschnitt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Das Moving Average Position System ist eine direkte Portierung des MetaTrader 4 Expertenberaters „MovingAveragePositionSystem.mq4“. Die Strategie überwacht einen langen gleitenden Lookback-Durchschnitt und reagiert auf Preiskreuzungen, die bei abgeschlossenen Kerzen auftreten. Es unterstützt sowohl die manuelle Losauswahl als auch eine optionale Martingal-ähnliche Volumeneskalationsroutine, die auf kumulierte Gewinne und Verluste reagiert, ausgedrückt in MetaTrader Punkten.

## Handelslogik

1. **Signalerkennung**
   - Das System berechnet einen konfigurierbaren gleitenden Durchschnitt (einfach, exponentiell, geglättet oder linear gewichtet).
   - Wenn der Schlusskurs der zuletzt abgeschlossenen Kerze den gleitenden Durchschnitt in der entgegengesetzten Richtung zum vorherigen Schlusskurs kreuzt, eröffnet die Strategie eine neue Position.
   - Pro Richtung ist nur eine Position erlaubt; Wenn die Strategie bereits Long ist, wird die Position erst dann erhöht, wenn die aktuelle Strategie geschlossen wird, und das Gleiche gilt für Short-Trades.
2. **Positionsverwaltung**
   - Wenn die Kerze, die gerade geschlossen wurde, wieder unter dem gleitenden Durchschnitt endet, während eine Long-Position offen ist, wird die Position sofort zum Marktwert geschlossen.
   - Wenn die Kerze wieder über dem gleitenden Durchschnitt schließt, während eine Short-Position geöffnet ist, wird der Short geschlossen.
   - Über die Strategieparameter kann ein Take-Profit im MetaTrader-Stil, ausgedrückt in Preisschritten (Punkten), aktiviert werden. Ansonsten werden Stopps durch das Kreuz des gleitenden Durchschnitts verwaltet.
3. **Geldmanagement**
   - Wenn der Martingale-Block aktiviert ist, akkumuliert die Strategie realisierte und variable PnL in MetaTrader Punkten für den aktuellen Zyklus.
   - Wenn die kumulierten Verluste die konfigurierte Verlustschwelle überschreiten, wird das nächste Handelsvolumen verdoppelt (wobei die maximale Losgröße nie überschritten wird) und alle offenen Positionen werden abgeflacht.
   - Wenn die kumulierten Gewinne das konfigurierte Gewinnziel überschreiten, wird das Volumen auf die anfängliche Losgröße zurückgesetzt und alle offenen Positionen werden geschlossen, um Gewinne zu sichern.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **MaType** | Methode zur Berechnung des gleitenden Durchschnitts: Einfach, exponentiell, geglättet oder linear gewichtet. Spiegelt die `TypeMA`-Eingabe des ursprünglichen Experten wider. |
| **MaPeriod** | Lookback-Zeitraum für den gleitenden Durchschnitt (Standard 240). |
| **MaShift** | Vorwärtsverschiebung, die auf die gleitenden Durchschnittswerte angewendet wird, bevor Signale generiert werden. Entspricht der Eingabe `SdvigMA`. |
| **Kerzentyp** | Kerzendatentyp, der für Signalberechnungen verwendet wird. Standardmäßig werden Kerzen mit einem Zeitrahmen von 1 Stunde verwendet. |
| **Anfangsvolumen** | Das verwendete Volumen, bevor die Martingal-Routine es ändert. Entspricht der Eingabe `Lots`. |
| **StartVolume** | Basislosgröße, auf die das Martingal nach einem profitablen Zyklus zurückgesetzt wird (`StarLots`). |
| **Maximale Lautstärke** | Obergrenze für das Handelsvolumen (`MaxLots`). Bei Überschreiten dieser Grenze halbiert die Strategie das Arbeitsvolumen. |
| **LossThresholdPips** | Verlustschwelle in MetaTrader Punkten, die ein Volumenverdoppelungsereignis auslöst (`LossPips`). |
| **ProfitThresholdPips** | Gewinnziel in Punkten, das das Volumen wieder auf den Startwert (`ProfitPips`) zurücksetzt. |
| **TakeProfitPips** | Optionale feste Take-Profit-Distanz in Punkten, die durch den integrierten Schutzhelfer angewendet wird (`TakeProfit`). |
| **UseMoneyManagement** | Aktiviert oder deaktiviert die Martingal-ähnliche Positionsgrößenroutine (`MM`). |

## Nutzungshinweise

- Konfigurieren Sie die Strategie mit demselben Symbol und Zeitrahmen, die in MetaTrader verwendet wurden. Der Standardzeitraum von 240 funktioniert gut mit H1-Kerzen und reproduziert das ursprüngliche Setup.
- Bei den Punktschwellenwerten wird davon ausgegangen, dass das Gerät gültige `PriceStep` und `StepPrice` bereitstellt. Für Symbole, denen diese Metadaten fehlen, müssen Sie die Schwellenwerte möglicherweise manuell anpassen.
- Da der ursprüngliche Code die Margen vor jedem Eintrag neu berechnet, führt der Port einen vereinfachten Volumennormalisierungsschritt durch, der die Handelsgröße halbiert, wenn sie `MaxVolume` überschreitet. Bei Bedarf können über die standardmäßigen StockSharp-Risikoanbieter zusätzliche Risikokontrollen hinzugefügt werden.
- Nur abgeschlossene Kerzen lösen Ein- und Ausgänge aus und spiegeln die MQL-Implementierung wider, die die Werte `Close[1]` und `Close[2]` auf jedem neuen Balken überprüfte.

## Dateien

- `CS/MovingAveragePositionSystemStrategy.cs` – C#-Implementierung der Handelslogik unter Verwendung der StockSharp-High-Level-Strategie API.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.
