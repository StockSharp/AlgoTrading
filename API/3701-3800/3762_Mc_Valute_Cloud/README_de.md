# Mc Valute Cloud-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieser Ordner enthält den StockSharp-Port des MetaTrader-Expertenberaters „Mc_valute“. Der ursprüngliche Roboter kombinierte einen Kurzschluss
exponentieller gleitender Durchschnitt (EMA) mit drei geglätteten gleitenden Durchschnitten, einem Ichimoku-Wolkenfilter und mehreren MACD-Instanzen während
Skalierung in den Trend. Die StockSharp-Implementierung behält den zentralen Trendbestätigungsstapel bei, vereinfacht jedoch die Positionsverwaltung
auf eine einzelne Belichtung in jede Richtung, sodass die Logik natürlich in das übergeordnete API passt.

## Handelslogik

1. **Preisfilter EMA** – der `FilterMaLength` EMA muss über (für Long-Positionen) oder unter (für Short-Positionen) der beiden geglätteten Bewegungen liegen
Durchschnittswerte (`BlueMaLength` und `LimeMaLength`). Die geglätteten Durchschnittswerte emulieren die „blauen“ und „limettenfarbenen“ Linien aus der MT4-Vorlage.
2. **Ichimoku Cloud-Bestätigung** – der EMA muss sich auch außerhalb der Cloud befinden. Für lange Trades ist der Filter EMA über beiden erforderlich
Senkou spannt sich, während Short-Trades erfordern, dass es unter dem Wolkenboden bleibt.
3. **MACD-Impulsprüfung** – die Hauptlinie MACD muss bei langen Einstiegen über ihrer Signallinie und bei kurzen Einstiegen darunter liegen.
Nur der erste MACD-Satz aus der ursprünglichen EA-Version wird beibehalten, da die restlichen Kopien in der endgültigen MQL-Version deaktiviert wurden.
4. **Einzelpositionsmanagement** – immer wenn ein neues Signal erscheint, gleicht die Strategie jede bestehende Gegenposition aus und eröffnet eine
neuer Handel mit dem konfigurierten `Volume`. Schutzaufträge werden sofort nach dem Absenden der Marktorder aktualisiert.
5. **Kerze-für-Kerze-Auswertung** – alle Indikatoren arbeiten in dem durch `CandleType` definierten Zeitrahmen. Handelsentscheidungen werden getroffen
Nur bei fertigen Kerzen, um den MT4 `start()`-Handler widerzuspiegeln, der geschlossene Balken verarbeitet hat.

## Risikomanagement

- `TakeProfit` und `StopLoss` werden in Preispunkten gemessen. Nach jedem Eintrag werden die Helfer `SetTakeProfit` und `SetStopLoss` angezeigt.
Funktionen werden unter Verwendung der erwarteten resultierenden Positionsgröße aufgerufen, die das MT4-Verhalten widerspiegelt, bei dem Stopps angewendet wurden
Fahrkarte.
- Der ursprüngliche Expertenberater hat mithilfe der Distanz `Step` bis zu drei zusätzliche Orders in eine Pyramide gebracht. Der Port StockSharp behält eine
Einzelposition, um im oberen Rang der Ordnungshelfer zu bleiben. Benutzer, die eine Skalierung benötigen, können `Volume` erhöhen oder klonen
Strategie über mehrere Portfolios hinweg.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `Volume` | Basishandelsgröße, die von den übergeordneten `BuyMarket`/`SellMarket`-Aufrufen verwendet wird. |
| `CandleType` | Primäre Kerzenserien steuern die Indikatoren und die Handelslogik. |
| `FilterMaLength` | Länge des Trendfilters EMA. |
| `BlueMaLength`, `LimeMaLength` | Längen der beiden geglätteten gleitenden Durchschnitte, die als Richtungsband fungieren. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | EMA Längen für die MACD-Bestätigung. |
| `TenkanLength`, `KijunLength`, `SenkouLength` | Ichimoku Kinko Hyo-Einstellungen für den Cloud-Filter. |
| `TakeProfit`, `StopLoss` | Schutzabstände ausgedrückt in Preispunkten. |

## Nutzungshinweise

1. **Indikatorverschiebungen** – MetaTrader erlaubte „Verschiebungs“-Parameter ungleich Null bei der Erstellung der geglätteten gleitenden Durchschnitte. StockSharps
Indikatoren funktionieren auf dem aktuellen Balken, daher ignoriert der Port diese Verschiebungen, behält aber die ursprünglichen Perioden bei.
2. **MACD-Varianten** – der Quellcode deklarierte drei MACD-Blöcke, aber nur der erste nahm an Live-Signalen teil. Der Hafen
folgt diesem Verhalten; Zusätzliche MACD-Filter können durch Duplizieren der Indikatorbindungen wieder aktiviert werden.
3. **Skalierende Trades** – der MT4-Roboter sendete bis zu drei Durchschnittsaufträge im Abstand von `Step` Punkten. Dieses Verhalten ist dokumentiert
aber absichtlich weggelassen, da High-Level-Strategien mit einer einzigen aggregierten Position arbeiten.
4. **Schutzblock** – `StartProtection()` wird einmal während des Startvorgangs aufgerufen, damit die integrierte Infrastruktur den Stopp überwacht
und Zielaufträge auch nach Wiederverbindungen.

## Dateien

- `CS/McValuteCloudStrategy.cs` – C#-Implementierung unter Verwendung der High-Level-Strategie API mit Indikatorbindungen und detailliert
Kommentare.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_zh.md` – Vereinfachte chinesische Übersetzung.
- `README_ru.md` – Russische Übersetzung.
