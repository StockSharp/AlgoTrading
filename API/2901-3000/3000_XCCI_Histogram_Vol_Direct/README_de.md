# XCCI Histogram Vol Direct-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **XCCI Histogram Vol Direct-Strategie** ist eine Portierung des MQL5-Experten `Exp_XCCI_Histogram_Vol_Direct`. Das System multipliziert den Commodity Channel Index (CCI) mit dem Volumen, glättet beide Reihen mit einem konfigurierbaren gleitenden Durchschnitt und bewertet dann die Steigung des geglätteten Oszillators. Wenn die Richtungsfarbe des Histogramms wechselt, schließt die Strategie Positionen gegen die Bewegung und eröffnet neue Trades in der entstehenden Richtung. Die Logik arbeitet ausschließlich auf abgeschlossenen Kerzen und verhält sich daher deterministisch auf historischen und Live-Daten.

Der originale Expert Advisor verwendete eine proprietäre Glättungsbibliothek mit mehreren Algorithmen, volumenbasierten Schwellenbändern und zeitversetzter Orderausführung. Der StockSharp-Port behält die konfigurierbaren Eingaben bei, approximiert die Glättungsoptionen mit verfügbaren Indikatoren und implementiert dieselbe Öffnungs-/Schließungssequenz über die High-Level-API.

## Marktregime und Edge
- Entwickelt für Märkte, in denen Volumenexpansion Momentum-Schübe begleitet.
- Bevorzugt Zeitrahmen mit klaren Schwankungen (Standard: 2-Stunden-Kerzen), kann aber von Intraday bis Swing-Horizonte angepasst werden.
- Signale reagieren auf eine Änderung der Steigung des geglätteten CCI*Volumen; daher verhält es sich wie ein Momentum-Umkehr-Detektor.

## Indikatoren und Verarbeitungspipeline
1. **Commodity Channel Index (CCI)** – berechnet auf dem ausgewählten Kerzentyp mit Periode `CciPeriod`.
2. **Volumenquelle** – entweder `Tick` oder `Real` (beide auf Kerzenvolumen gemappt, da Tick-Zählungen in StockSharp-Kerzen nicht verfügbar sind).
3. **Gewichteter Oszillator** – CCI mit dem gewählten Volumenstrom multiplizieren.
4. **Glättung** – die ausgewählte Familie gleitender Durchschnitte auf den gewichteten Oszillator und das Rohvolumen mit Länge `SmoothingLength` anwenden.
   - `Sma` → SimpleMovingAverage
   - `Ema` → ExponentialMovingAverage
   - `Smma` → SmoothedMovingAverage
   - `Lwma` → WeightedMovingAverage
   - `Jjma` → JurikMovingAverage
   - `Jurx` → ZeroLagExponentialMovingAverage
   - `Parabolic` → ArnaudLegouxMovingAverage (Phasenparameter wird auf ALMA-Offset gemappt)
   - `T3` → TripleExponentialMovingAverage
   - `Vidya` → ExponentialMovingAverage (beste verfügbare Annäherung)
   - `Ama` → KaufmanAdaptiveMovingAverage
5. **Richtungsfarbe** – den neuesten geglätteten Oszillatorwert mit dem vorherigen vergleichen. Steigende Werte werden `0` (bullisch) gefärbt, fallende Werte `1` (bärisch), und gleiche Werte erben die vorherige Farbe, genau wie der ursprüngliche Indikator-Puffer.
6. **Signalspeicher** – die letzten Farben speichern, damit die Strategie die durch `SignalBar` angegebene Bar und die Bar davor prüfen kann.

## Handelsregeln
### Long-Management
- **Einstieg**: Wenn die Farbe der Signalbar `1` (bärisch) ist, die Bar davor jedoch `0` (bullisch) war, eine Long-Position eröffnen, sofern `AllowLongEntries = true` und die aktuelle Nettoposition nicht bereits Long ist. Die Ordergröße entspricht `Volume + |Position|`, sodass jede Short-Exposure zuerst glattgestellt wird.
- **Ausstieg**: Wann immer die Bar vor der Signalbar bullisch (`0`) ist und `AllowShortExits = true`, alle offenen Short-Positionen schließen, um nicht gegen den neuen Aufwärtsschwung zu kämpfen.

### Short-Management
- **Einstieg**: Wenn die Signalbar-Farbe nach einem vorherigen `1` zu `0` wird, eine Short-Position eröffnen, wenn `AllowShortEntries = true` und das Konto nicht bereits netto short ist. Die Ordergröße spiegelt die Long-Logik wider.
- **Ausstieg**: Wenn die Bar vor der Signalbar bärisch (`1`) ist und `AllowLongExits = true`, Long-Exposure schließen.

### Risikokontrollen
- `StopLossPoints` und `TakeProfitPoints` werden über `PriceStep` des Instruments in Preispunkt-Offsets umgerechnet und über `StartProtection` angewendet.
- Schutzorders werden für jeden Trade aktiviert; beide Werte auf `0` setzen, um ein einzelnes Bein zu deaktivieren.

## Parameterreferenz
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CciPeriod` | Länge des Commodity Channel Index. | `14` |
| `Smoothing` | Familie gleitender Durchschnitte für die Glättung von Oszillator und Volumen. | `T3` |
| `SmoothingLength` | Periode der Glättungsfilter. | `12` |
| `SmoothingPhase` | Phasen-/Offsetwert, der auf den ALMA-Offset gemappt wird; aus Kompatibilitätsgründen beibehalten. | `15` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Schwellenmultiplikatoren aus dem Indikator (nützlich für Diagnose/Visualisierung). | `100`, `80`, `-80`, `-100` |
| `SignalBar` | Rückblick-Index der Bar, die das Signal definiert (0 = zuletzt geschlossene Kerze). | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Öffnen von Trades in einer Richtung aktivieren oder deaktivieren. | `true` |
| `AllowLongExits` / `AllowShortExits` | Schließen von Trades in einer Richtung aktivieren oder deaktivieren. | `true` |
| `StopLossPoints` | Stop-Loss-Abstand in Preispunkten. | `1000` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preispunkten. | `2000` |
| `VolumeSource` | Volumenstrom (`Tick` oder `Real`). Beide verwenden Kerzenvolumen in diesem Port. | `Tick` |
| `CandleType` | Zeitrahmen für die Analyse. | `2h` |

## Kerzenverarbeitungs-Workflow
1. Auf eine abgeschlossene Kerze des konfigurierten Typs warten.
2. Den CCI-Wert berechnen und mit dem ausgewählten Volumenstrom multiplizieren.
3. Den gewichteten CCI und das Rohvolumen in die Glättungsfilter einspeisen.
4. Sobald beide Glätter gebildet sind, die neue Farbe bestimmen und den Verlaufspuffer aktualisieren.
5. Die Farbe bei `SignalBar` und `SignalBar+1` prüfen, um zu entscheiden, ob entgegengesetzte Positionen geschlossen und/oder ein neuer Trade eröffnet werden soll.
6. Risikomanagement über den vorkonfigurierten Stop-Loss und Take-Profit anwenden.

## Verwendungshinweise
- Das Basis-`Strategy.Volume` muss auf einen positiven Wert gesetzt werden; es definiert die Größe jedes Einstiegs.
- Da StockSharp-Kerzen keine Tick-Zählungen bereitstellen, verwenden sowohl `Tick`- als auch `Real`-Volumenmodi `candle.TotalVolume`. Wenn Tick-Level-Daten erforderlich sind, die Strategie mit benutzerdefinierten Kerzen füttern, die das Tick-Volumen im Feld `TotalVolume` kodieren.
- Die Glättungsphase betrifft nur ALMA. Für andere Filter wird sie ignoriert, was das Verhalten des MQL-Indikators widerspiegelt, bei dem bestimmte Modi die Phaseneingabe ignorieren.
- Schwellenmultiplikatoren (`HighLevel*` und `LowLevel*`) werden der Vollständigkeit halber beibehalten. Sie können visualisiert werden, indem das geglättete Volumen geplottet und die Multiplikatoren extern angewendet werden.

## Einschränkungen und Unterschiede zur MQL5-Version
- StockSharp verfügt derzeit über keine direkten Implementierungen von VIDYA und Parabolic MA; EMA und ALMA werden als nächste Substitute verwendet. Dies hält die Reaktionscharakteristiken ähnlich, aber nicht identisch zur ursprünglichen benutzerdefinierten Bibliothek.
- Die Orderausführung erfolgt sofort beim Schließen der Signalkerze. Der MQL-Experte plante Trades zu Beginn der nächsten Periode über `TimeShiftSec`; dieses Verhalten ist funktional äquivalent, wenn der Broker Marktorders nahezu sofort ausführt.
- Tick-Volumen wird durch das gesamte gehandelte Volumen approximiert, da individuelle Tick-Zählungen nicht in Standard-Kerzen-Nachrichten bereitgestellt werden.

## Erste Schritte
1. Die Strategie dem gewünschten `Security` zuordnen und `Volume` auf die Anzahl der Lots/Kontrakte pro Signal setzen.
2. Den Kerzen-Zeitrahmen über `CandleType` wählen (Standard: 2-Stunden-Zeitrahmen).
3. Glättungs- und Risikoparameter an das Volatilitätsprofil des Zielmarkts anpassen.
4. Zunächst im Papiermodus ausführen, den geglätteten Oszillator im Chart überprüfen und `SignalBar` feinjustieren, wenn Signale zu früh oder zu spät eintreffen.

## Optimierungsideen
- `SmoothingLength` zusammen mit `CciPeriod` optimieren, um die Reaktionsfähigkeit auf den Zielwert abzustimmen.
- `SignalBar` um `0` und `1` stressgetestet werden für schnellere/langsamere Reaktion.
- Erwägen, `StopLossPoints` / `TakeProfitPoints` zu erweitern oder zu verringern, um sich an den ATR des Instruments anzupassen.
- Die Strategie auf mehreren Zeitrahmen ausführen und Trades nach der Trendrichtung des höheren Zeitrahmens filtern, wenn zusätzliche Bestätigung benötigt wird.

## Sicherheits-Checkliste
- Bestätigen, dass `Security.PriceStep` und `Volume` mit den Kontraktspezifikationen des Instruments übereinstimmen, bevor die Live-Ausführung beginnt.
- Slippage überwachen und externe Risikokontrollen anpassen, wenn der gewählte Markt illiquide ist.
- Handelsprotokolle regelmäßig überprüfen, um sicherzustellen, dass Richtungsfilter (`Allow*`) mit der beabsichtigten Exposition übereinstimmen.
