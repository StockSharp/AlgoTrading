# XROC2 VG X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die XROC2 VG X2-Strategie ist ein Multi-Zeitrahmen-System, das zwei geglättete Rate-of-Change-Streams kombiniert. Der höhere Zeitrahmen dient als Richtungsfilter, während der niedrigere konkrete Einstiegs- und Ausstiegssignale erzeugt. Der ursprüngliche MetaTrader 5 Expert Advisor basierte auf dem benutzerdefinierten XROC2_VG-Indikator mit flexiblen Glättungsoptionen und einem Money-Management-Modul. Der StockSharp-Port behält die Signallogik intakt und stellt die wichtigsten Parameter als Strategieeingaben bereit.

Die Strategie abonniert zwei Kerzenserien:
- **Höherer Zeitrahmen** (Standard 6 Stunden) – stellt die vorherrschende Trendrichtung fest.
- **Niedrigerer Zeitrahmen** (Standard 30 Minuten) – generiert Einstiege und Ausstiege durch Überwachung, wie die zwei geglätteten ROC-Linien kreuzen.

Beide Streams teilen denselben Rate-of-Change-Berechnungsmodus, verwenden aber individuelle Glättungseinstellungen. Standardmäßig wendet die Strategie Jurik-gleitende Durchschnitte an und ahmt damit die MQL-Version nach. Erweiterte Glättungstypen, die von StockSharp nicht direkt unterstützt werden (JurX, ParMA, T3, VIDYA, AMA mit Phasensteuerung), fallen auf die nächstgelegene verfügbare Moving-Average-Implementierung zurück.

## Handelslogik
1. **Trenderkennung (höherer Zeitrahmen)**
   - Zwei geglättete ROC-Werte mit den konfigurierten Perioden und Glättungsmethoden berechnen.
   - Das Linienpaar auf dem durch `HigherSignalBar` definierten Bar auswerten. Wenn die schnelle Linie über der langsamen liegt, ist der Trend bullisch, sonst bärisch. Eine neutrale Lesart hält den aktuellen Trend bei null und deaktiviert den Handel.
2. **Signalgenerierung (niedrigerer Zeitrahmen)**
   - Dasselbe Paar geglätteter ROC-Werte im niedrigeren Zeitrahmen berechnen.
   - Den zuletzt abgeschlossenen Bar (Shift `LowerSignalBar`) und den Bar davor betrachten. Die Kombination dieser zwei Bars bestimmt, ob gerade ein Kreuz stattgefunden hat.
   - Ein Long-Setup erscheint, wenn der höhere Zeitrahmen bullisch ist, die schnelle Linie unter die langsame gekreuzt hat (Abwärtskreuzung) und Longs aktiviert sind.
   - Ein Short-Setup erscheint, wenn der höhere Zeitrahmen bärisch ist, die schnelle Linie über die langsame gekreuzt hat (Aufwärtskreuzung) und Shorts aktiviert sind.
3. **Positionsmanagement**
   - Long-Positionen schließen, wenn die Kreuzung im niedrigeren Zeitrahmen Bärischheit anzeigt (`CloseBuyOnLower`) oder wenn der Trend im höheren Zeitrahmen auf bärisch kippt (`CloseBuyOnTrendFlip`).
   - Short-Positionen schließen, wenn die Kreuzung im niedrigeren Zeitrahmen bullisch wird (`CloseSellOnLower`) oder wenn der Trend im höheren Zeitrahmen auf bullisch kippt (`CloseSellOnTrendFlip`).
   - Neue Trades werden nur geöffnet, wenn keine Position aktiv ist. Die Ordergröße wird durch die `Volume`-Eigenschaft der Strategie gesteuert.

## Parameter
- `HigherCandleType` – Kerzentyp für den Trendfilter (Standard 6-Stunden-Zeitrahmen).
- `LowerCandleType` – Kerzentyp für die Signalgenerierung (Standard 30-Minuten-Zeitrahmen).
- `HigherSignalBar` – wie viele geschlossene Bars beim Lesen höherer Zeitrahmenwerte verschoben werden (Standard 1).
- `LowerSignalBar` – wie viele geschlossene Bars beim Lesen niedrigerer Zeitrahmenwerte verschoben werden (Standard 1).
- `HigherRocMode` / `LowerRocMode` – Rate-of-Change-Berechnungsvariante (`Momentum`, `RateOfChange`, `RateOfChangePercent`, `RateOfChangeRatio`, `RateOfChangeRatioPercent`).
- `HigherFastPeriod`, `HigherFastMethod`, `HigherFastLength`, `HigherFastPhase` – schnelle ROC-Einstellungen für den höheren Zeitrahmen.
- `HigherSlowPeriod`, `HigherSlowMethod`, `HigherSlowLength`, `HigherSlowPhase` – langsame ROC-Einstellungen für den höheren Zeitrahmen.
- `LowerFastPeriod`, `LowerFastMethod`, `LowerFastLength`, `LowerFastPhase` – schnelle ROC-Einstellungen für den niedrigeren Zeitrahmen.
- `LowerSlowPeriod`, `LowerSlowMethod`, `LowerSlowLength`, `LowerSlowPhase` – langsame ROC-Einstellungen für den niedrigeren Zeitrahmen.
- `AllowBuyOpen`, `AllowSellOpen` – Eröffnen von Longs und Shorts aktivieren oder deaktivieren.
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – Ausstiege erzwingen, wenn der höhere Zeitrahmen die Richtung ändert.
- `CloseBuyOnLower`, `CloseSellOnLower` – aussteigen, wenn die Kreuzung im niedrigeren Zeitrahmen gegen die Position geht.

## Implementierungshinweise
- Die originale MQL-Strategie verwendete eine große Glättungsbibliothek. Die StockSharp-Version mappt die unterstützten Optionen auf integrierte Indikatoren (SMA, EMA, SMMA/RMA, LWMA, Jurik, Kaufman AMA). Nicht unterstützte Modi (JurX, ParMA, T3, VIDYA) werden mit dem nächstgelegenen verfügbaren gleitenden Durchschnitt approximiert, sodass das Verhalten bei diesen Kombinationen abweichen kann.
- Money-Management-Funktionen, Stop-Loss, Take-Profit und Slippage-Einstellungen aus `TradeAlgorithms.mqh` werden nicht reproduziert. Stattdessen handelt die Strategie mit dem festen `Volume` aus den Strategieeinstellungen.
- Orders werden mit Marktorders ausgeführt. Schutzlogik wie Stop-Losses oder Trailing-Stops kann bei Bedarf über StockSharp-Schutzmodule hinzugefügt werden.
- Die Strategie handelt nur, wenn beide Kerzenabonnements vollständig gebildet sind und `IsFormedAndOnlineAndAllowTrading()` wahr zurückgibt.

## Verwendungstipps
- Kerzentypen wählen, die dem ursprünglichen Handelsstil entsprechen (z. B. 6h/30m für Swing-Trading). Andere Kombinationen sind möglich.
- Die ROC-Perioden und Glättungsmethoden abstimmen, um die bevorzugte Reaktionsfähigkeit zu erreichen. Jurik-Glättung hält das Verhalten am nächsten am Quellskript.
- Explizites Risikomanagement (Stop-Loss, Positionsgröße) in Betracht ziehen, wenn auf Live-Konten betrieben wird, da der Port einfache Marktausstiege verwendet.
