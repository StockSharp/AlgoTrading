# XCCI-Histogramm-Vol-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader Expert Advisors `Exp_XCCI_Histogram_Vol`. Sie reproduziert die farbcodierte Logik des benutzerdefinierten "XCCI Histogram Vol"-Indikators: ein Commodity Channel Index (CCI), der mit dem Volumen multipliziert, durch einen auswählbaren gleitenden Durchschnitt geglättet und mit dynamischen Schwellenwerten verglichen wird. Die Implementierung folgt den High-Level-API-Richtlinien, verarbeitet nur geschlossene Kerzen und behält die ursprüngliche Dual-Positions-Struktur bei, indem sie separate Volumen für die primären und sekundären Einstiege exponiert.

## Indikator-Workflow
1. Den CCI-Wert mit dem konfigurierbaren Zeitraum berechnen.
2. Den CCI-Wert mit dem Kerzenvolumen multiplizieren.
3. Sowohl die CCI×Volumen-Reihe als auch das Rohvolumen mit dem gewählten gleitenden Durchschnitt (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `Hull` oder `VolumeWeighted`) glätten.
4. Vier benutzerdefinierte Schwellenwert-Multiplikatoren (HighLevel2/1 und LowLevel1/2) mit dem geglätteten Volumen skalieren.
5. Den geglätteten CCI×Volumen-Wert in eine von fünf Zonen klassifizieren: `0` extrem bullisch, `1` bullisch, `2` neutral, `3` bärisch, `4` extrem bärisch.

Die Strategie speichert die Zone für jede fertige Kerze. Der Parameter `SignalBarOffset` steuert, wie viele vollständig geschlossene Kerzen gewartet werden soll, bevor die Zone für Handelsentscheidungen verwendet wird (entspricht dem ursprünglichen `SignalBar`-Input).

## Handelsregeln
- **Long-Ausstiege**: Wenn die bewertete Zone `3` oder `4` ist, wird jede offene Long-Position geschlossen.
- **Short-Ausstiege**: Wenn die bewertete Zone `1` oder `0` ist, wird jede offene Short-Position geschlossen.
- **Primärer Long-Einstieg**: Auslösung, wenn die aktuelle Zone zu `1` wird und die vorherige Zone (ältere Kerze) über `1` lag. Dies spiegelt den Übergang vom neutral/bärischen Territorium in das bullische Band wider. Das Ordervolumen ist `PrimaryEntryVolume` und schließt jedes bestehende Short-Exposure vor dem Wechsel.
- **Sekundärer Long-Einstieg**: Auslösung, wenn die aktuelle Zone zu `0` wird und die vorherige Zone über `0` lag. Dies stellt einen Surge in die extrem bullische Region dar und verwendet `SecondaryEntryVolume`.
- **Primärer Short-Einstieg**: Auslösung, wenn die aktuelle Zone zu `3` wird und die vorherige Zone unter `3` lag, was eine neue Bewegung in bärisches Territorium anzeigt. Verwendet `PrimaryEntryVolume` und schließt zuerst Longs, wenn nötig.
- **Sekundärer Short-Einstieg**: Auslösung, wenn die aktuelle Zone zu `4` wird und die vorherige Zone unter `4` lag, was eine extreme bärische Beschleunigung signalisiert. Verwendet `SecondaryEntryVolume`.

Einstiegs-Flags werden zurückgesetzt, wann immer die Nettoposition null kreuzt, sodass das Verhalten dem "Zwei-Magische-Zahlen"-Design von MetaTrader entspricht – nur eine Order pro Tier ist erlaubt, bis das entgegengesetzte Signal oder das Risikomodul den Trade schließt.

## Risikomanagement
- `UseStopLoss` / `UseTakeProfit` aktivieren absolute Schutzabstände (in Preis-Punkten ausgedrückt) über den eingebauten `StartProtection`-Helfer. Stops sind optional, genau wie im ursprünglichen Code.
- Die Strategie verwendet Marktorders für jede Aktion und respektiert daher die plattformweit konfigurierten Slippage-Einstellungen in StockSharp.
- Protokollierungsaufrufe beschreiben jeden Einstieg und Ausstieg, was die Prüfung vereinfacht, warum ein Trade ausgeführt wurde.

## Parameter
- **CciPeriod** – Länge des Commodity Channel Index.
- **MaLength** – Länge, die auf beide glättende gleitende Durchschnitte angewendet wird.
- **HighLevel2 / HighLevel1 / LowLevel1 / LowLevel2** – Multiplikatoren, die auf das geglättete Volumen angewendet werden, um adaptive Schwellenwerte zu erstellen.
- **SignalBarOffset** – Anzahl der geschlossenen Kerzen, die gewartet werden soll, bevor auf eine Zone reagiert wird (0 = letzte geschlossene Kerze, 1 = vorherige Kerze, etc.).
- **Smoothing** – Typ des gleitenden Durchschnitts für die Glättung (Teilmenge der ursprünglichen Optionen: SMA, EMA, SMMA, WMA, Hull MA, VWMA).
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – Jede Seite unabhängig aktivieren oder deaktivieren.
- **PrimaryEntryVolume / SecondaryEntryVolume** – Volumen für die zwei Einstiegsstufen (für Long- und Short-Trades verwendet).
- **UseStopLoss / StopLossPoints** – Optionaler absoluter Stop-Loss.
- **UseTakeProfit / TakeProfitPoints** – Optionaler absoluter Take-Profit.
- **CandleType** – Zeitrahmen (oder jeder andere Kerzendatentyp), der vom Connector angefordert wird.

## Unterschiede zur MetaTrader-Version
- Es werden nur Glättungsmethoden exponiert, die in StockSharp existieren; exotische Filter wie JJMA, JurX, Parabolic MA, VIDYA und AMA sind nicht enthalten. Wählen Sie die nächste verfügbare Alternative, wenn Sie ähnliches Verhalten benötigen.
- Das Kerzenvolumen wird aus `ICandleMessage.TotalVolume` entnommen. Tick-Volumen wird nicht emuliert. Wenn der zugrundeliegende Connector nur Transaktionszählungen liefert, wird das Ergebnis vom ursprünglichen Terminal abweichen.
- Das Ordermanagement ist genetzt (einzelne Position) anstatt zwei unabhängiger magischer Zahlen. Separate primäre/sekundäre Einstiegs-Flags emulieren dieselbe Absicht, während sie mit dem StockSharp-Ausführungsmodell kompatibel bleiben.
