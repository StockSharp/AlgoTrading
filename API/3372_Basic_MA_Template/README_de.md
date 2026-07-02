# Grundlegende MA-Vorlagenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Basic MA Template Strategy** ist eine originalgetreue StockSharp-Portierung des MetaTrader 4 Expert Advisors aus dem Repository-Eintrag `MQL/27964`. Der ursprüngliche Roboter handelte ein einzelnes Symbol auf einem gleitenden Durchschnitt mit einem höheren Zeitrahmen und eröffnete eine Position, wann immer die vorherige Kerze den Durchschnitt kreuzte. Diese C#-Version behält die minimalistische Struktur bei und stellt gleichzeitig jedes Steuerelement als Parameter bereit, sodass das Verhalten direkt in StockSharp angepasst oder optimiert werden kann.

Die Vorlage wartet auf eine vollständig fertige Kerze und vergleicht ihre Eröffnungs- und Schlusskurse mit einem verschobenen gleitenden Durchschnitt. Wenn der Balken über dem Durchschnitt öffnet und darunter schließt, eröffnet die Strategie eine Short-Position. Wenn das Gegenteil geschieht, wird ein Long-Trade eröffnet. Das System lässt jeweils nur eine Marktposition zu und spiegelt die MQL-Prüfung „Kein aktives Ticket“ wider. Schutz-Stop-Loss- und Take-Profit-Abstände werden in Pips definiert. Beim Start wandelt die Strategie diese Pip-Abstände mithilfe des Instrumentenschritts und der Dezimalgenauigkeit in absolute Preisversätze um und reproduziert dabei die Point-to-Pip-Konvertierungslogik, die von den Angebotsziffern in MetaTrader abhing.

## Handelslogik

- **Datenquelle**: eine einzelne Kerzenserie, die durch den Parameter `CandleType` (Standard H4) bestimmt wird.
- **Indikator**: konfigurierbarer gleitender Durchschnitt (`SMA`, `EMA`, `SMMA` oder `LWMA`). Der Parameter `MovingAverageShift` verschiebt den Indikator genau wie die Funktion MetaTrader `iMA` nach vorne.
- **Eintrittsregeln**:
  - Long: Die vorherige Kerze öffnete unterhalb und schloss über dem verschobenen gleitenden Durchschnitt, während keine Position offen war.
  - Short: Die vorherige Kerze öffnete über und schloss unter dem verschobenen gleitenden Durchschnitt, während keine Position offen war.
- **Exit-Regeln**: werden automatisch vom Modul StockSharp `StartProtection` unter Verwendung von Pip-basierten Take-Profit- und Stop-Loss-Abständen verarbeitet. Wenn beide Ziele Null sind, aktiviert die Strategie weiterhin den Schutzdienst, sodass nachlaufende oder manuelle Exits weiterhin möglich sind.
- **Positionsfilter**: Die Strategie ignoriert neue Signale, während eine Position aktiv ist, und behält das Verhalten bei, das mit der ursprünglichen `PosSelect()`-Routine identisch ist.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Für Signale verwendete Kerzenaggregation. | H4 (4-Stunden-Kerzen) |
| `MovingAveragePeriod` | Periodenlänge des gleitenden Durchschnitts. | 49 |
| `MovingAverageShift` | Auf den gleitenden Durchschnittspuffer angewendete Vorwärtsverschiebung. | 0 |
| `MovingAverageMethod` | Berechnungsmodus für gleitenden Durchschnitt (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Simple` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips, umgerechnet zur Laufzeit in absolute Preis-Offsets. | 38,5 |
| `StopLossPips` | Stop-Loss-Distanz in Pips, umgerechnet zur Laufzeit in absolute Preis-Offsets. | 48,5 |

### Umgang mit Risiken

Das Schutzsubsystem empfängt die berechneten absoluten Distanzen und fügt sie jeder Marktorder hinzu. Da die Pip-Größe aus dem Symbolschritt und der Dezimalgenauigkeit abgeleitet wird (5-stellige und 3-stellige Anführungszeichen multiplizieren den Schritt mit zehn), respektieren die Stop-Levels den von Brokern in der MetaTrader-Version vorgeschriebenen Mindestabstand.

### Konvertierungshinweise

- Die ursprüngliche zweistufige Auftragserteilung im ECN-Stil wird auf StockSharp Marktaufträge mit automatischem Schutz vereinfacht, der bereits das Anhängen von SL/TP nach der Ausführung übernimmt.
- Die Routinen `CheckVolumeValue` und `CheckMoneyForTrade` werden weggelassen. Die Positionsgröße sollte über die Standard-Risikoeinstellungen von StockSharp konfiguriert werden.
- Protokollierungsanweisungen werden durch Chart-Drawing-Hooks ersetzt, sodass der gleitende Durchschnitt und die ausgeführten Trades direkt im Strategie-Chartbereich visualisiert werden können.

Durch diese Konvertierung bleibt das Entscheidungsmodell identisch, während idiomatische StockSharp-APIs auf hoher Ebene (`SubscribeCandles`, `Bind` und `StartProtection`) übernommen werden. Verwenden Sie es als leichtes Gerüst für den Aufbau fortschrittlicherer Systeme mit gleitendem Durchschnitt.
