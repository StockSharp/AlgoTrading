# iCCI iRSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **iCCI iRSI-Strategie** ist eine direkte Konvertierung des MetaTrader 5 Expert Advisors `iCCI iRSI.mq5`. Das ursprüngliche System kombiniert den Commodity Channel Index (CCI) und den Relative Strength Index (RSI), um Erschöpfungszonen zu erkennen. Wenn beide Oszillatoren auf einen überkauften oder überverkauften Zustand einig sind, öffnet der Advisor eine Position, befestigt Schutzorders und verfolgt optional den Stop, während der Trade in Gewinn geht. Dieser StockSharp-Port spiegelt dieses Verhalten mit High-Level-APIs wider, einschließlich pip-basierter Eingaben, automatischem Schließen von Gegenpositionen und einem umkehrbaren Signalmodus.

## Handelslogik
1. Den konfigurierten Kerzentyp abonnieren und einen `CommodityChannelIndex` mit Periode `CciPeriod` sowie einen `RelativeStrengthIndex` mit Periode `RsiPeriod` berechnen.
2. Nur abgeschlossene Kerzen auswerten. Intrabar-Rauschen wird ignoriert, genau wie die MQL-Implementierung, die auf eine neue Bar wartet.
3. Wenn beide Indikatoren unter ihre jeweiligen unteren Schwellenwerte fallen (`CciLowerLevel` und `RsiLowerLevel`), öffnet oder dreht die Strategie in eine Long-Position. Wenn beide Indikatoren über die oberen Schwellenwerte steigen (`CciUpperLevel` und `RsiUpperLevel`), wird ein Short-Setup ausgelöst. Das Aktivieren von `ReverseSignals` tauscht die Richtungen.
4. Bevor eine neue Order gesendet wird, wird das aktuelle entgegengesetzte Exposure geschlossen, sodass die Nettoposition immer dem aktiven Signal entspricht.
5. Nach dem Einstieg überwacht die Strategie den Schlusskurs der folgenden Kerzen. In Pips ausgedrückte Take-Profit- und Stop-Loss-Niveaus werden mit dem `PriceStep` des Instruments in Preiseinheiten umgerechnet. Für 3- oder 5-stellige Forex-Symbole reproduziert eine zusätzliche ×10-Anpassung die MetaTrader-Pip-Definition.
6. Wenn `TrailingStopPips` positiv ist, wird der Stop-Loss Richtung Markt vorgerückt, sobald sich der Preis mehr als `TrailingStopPips + TrailingStepPips` in der günstigen Richtung bewegt. Updates respektieren den konfigurierten Schritt, um schnelle Stop-Modifikationen zu vermeiden.

## Risiko- und Handelsmanagement
- **Take-Profit / Stop-Loss** – optionale Pip-Abstände, die unmittelbar nach einer Ausführung zu absoluten Preisniveaus werden. Wenn eines der Niveaus beim Kerzen-Schluss durchbrochen wird, wird die Position zum Marktpreis liquidiert.
- **Trailing-Stop** – repliziert die Trailing-Logik des EA. Gewinne müssen die Trailing-Distanz plus den Trailing-Schritt überschreiten, bevor der Stop enger gezogen wird.
- **Volumen** – ein fester `TradeVolume`-Parameter ersetzt den ursprünglichen Lot-oder-Risiko-Selektor (`ENUM_LOT_OR_RISK`). Optimierung verwenden, um geeignete Volumina zu entdecken, wenn Geldverwaltungsvarianten benötigt werden.
- **Positionshygiene** – wenn ein neues Signal erscheint, flättet die Strategie jedes entgegengesetzte Holding, bevor der neue Trade eröffnet wird, genau wie der EA `ClosePositions` durchführt.

## Parameter
- **Candle Type** – von den Indikatoren verarbeitete Kerzendatenserie (Standard: 1-Stunden-Kerzen).
- **CciPeriod** – CCI-Mittelungslänge (Standard: 14).
- **CciUpperLevel / CciLowerLevel** – überkaufte und überverkaufte CCI-Schwellenwerte (Standards: +80 / −80).
- **RsiPeriod** – RSI-Mittelungslänge (Standard: 42).
- **RsiUpperLevel / RsiLowerLevel** – RSI-Auslöseniveaus (Standards: 60 / 30).
- **ReverseSignals** – kehrt die Interpretation der Oszillatorsignale um (Standard: `false`).
- **TradeVolume** – Marktordergröße. Auf den MT5-Lot-Input abstimmen (Standard: 0.1).
- **StopLossPips / TakeProfitPips** – Schutzabstände in Pips (Standards: 0 und 140). Auf null setzen zum Deaktivieren.
- **TrailingStopPips / TrailingStepPips** – Trailing-Stop-Distanz und Mindestschritt (Standards: 5 / 5). Eine null Trailing-Distanz deaktiviert Trailing, auch wenn ein Schritt angegeben ist.

## Implementierungshinweise
- StockSharp-Indikatoren (`CommodityChannelIndex`, `RelativeStrengthIndex`) liefern gebrauchsfertige Dezimalwerte über die `Bind`-API, sodass keine manuelle `CopyBuffer`-Logik erforderlich ist.
- Das gesamte Handelsmanagement findet auf abgeschlossenen Kerzen statt. Dies entspricht der `PrevBars`-Bewachung des EA und verhindert mehrere Einstiege innerhalb derselben Bar.
- Die Pip-Konvertierung berücksichtigt fraktionale Pip-Notierungen, indem der `PriceStep` für Instrumente mit 3 oder 5 Dezimalstellen mit 10 multipliziert wird – ein direktes Analogon zur MQL `digits_adjust`-Logik.
- Schutziele werden über Marktausstiege simuliert, da StockSharp-Strategien in einer Sandbox-Umgebung arbeiten, in der synchrone Ordermodifikationen nicht verfügbar sind.
- Zusätzliche Chartbereiche zeichnen die CCI- und RSI-Linien zur visuellen Validierung der Einstiegszonen.

## Unterschiede zum ursprünglichen Expert Advisor
- Das MetaTrader-Modul `MoneyFixedMargin` ist nicht portiert. Die Positionsdimensionierung ist jetzt ein einfacher fester Volumensparameter.
- Brokerspezifische Prüfungen wie `FreezeStopsLevels` sind in StockSharp nicht verfügbar. Der Trailing-Stop beobachtet daher nur Preisabstand und Schrittanforderungen.
- Logging- und Warnungsstrings wurden zugunsten einer sauberen Strategieausgabe entfernt. Das Logging-System von StockSharp kann bei Bedarf extern angehängt werden.
- Das Handelsmanagement arbeitet auf Kerzen-Schlusskursen. Die MT5-Version konnte intrabar reagieren, wenn Stop oder Take-Profit berührt werden, aber die End-of-Bar-Approximation hält die Logik für Backtests deterministisch.

## Nutzungshinweise
1. Mit dem standardmäßigen 1-Stunden-Zeitrahmen beginnen, um die Originalvorlage zu replizieren. Kürzere Frames können mehr Signale, aber auch mehr Fehlsignale einführen.
2. `CciUpperLevel`, `CciLowerLevel`, `RsiUpperLevel` und `RsiLowerLevel` zusammen optimieren – der EA basiert auf Übereinstimmung beider Oszillatoren, daher sind ausgewogene Schwellenwerte wesentlich.
3. Bei Forex-Paaren prüfen, ob die Wertpapier-Metadaten `PriceStep` und `Decimals` bereitstellen, damit Pip-Abstände korrekt konvertiert werden.
4. `ReverseSignals` für klassisches Trendumkehr-Verhalten deaktivieren. Aktivieren, um Ausbrüche aus überkauften/überverkauften Zonen zu handeln.
5. Mit StockSharp-Risikomodulen (Eigenkapital-Stop, Drawdown-Schutz) kombinieren, wenn Portfolio-Kontrollen erforderlich sind – sie ersetzen den MT5 `m_money`-Helper.

Diese Dokumentation liefert den gesamten notwendigen Kontext für Einsatz, Anpassung und Erweiterung der iCCI iRSI-Strategie innerhalb der StockSharp-Umgebung.
