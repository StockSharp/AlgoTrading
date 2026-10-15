# Zwei-MA-Vier-Level-Band-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie erstellt den MetaTrader-Expertenberater `ytg_2MA_4Level` neu. Es vergleicht einen schnellen gleitenden Durchschnitt mit einem langsameren und löst Einträge aus, wenn die schnelle Kurve die langsame Kurve entweder direkt oder innerhalb von vier konfigurierbaren Offset-Bändern kreuzt. Positionen werden wie in der ursprünglichen Implementierung durch symmetrische Stop-Loss- und Take-Profit-Distanzen geschützt, die in Pips ausgedrückt werden.

## Signallogik
1. Für die ausgewählte Kerzenserie werden zwei gleitende Durchschnitte berechnet. Sowohl die Mittelungsmethode (SMA, EMA, SMMA, LWMA) als auch der angewendete Preis können unabhängig voneinander für die schnellen und langsamen Leitungen angepasst werden.
2. Bei jeder fertigen Kerze tastet die Strategie die gleitenden Durchschnitte um `CalculationBar` Balken zurück (Standard: `1`) und auch einen Balken früher ab. Dies spiegelt den Aufruf MetaTrader `iMA(..., shift)` wider und stellt sicher, dass nur geschlossene Kerzen Trades generieren.
3. Ein **Kaufsignal** wird ausgelöst, wenn der schnelle Durchschnitt den langsamen überschreitet oder wenn der Übergang über/unter den langsamen Durchschnitt erfolgt, der um `UpperLevel1`, `UpperLevel2`, `LowerLevel1` oder `LowerLevel2` Pips verschoben ist.
4. Ein **Verkaufssignal** verwendet die gespiegelten Bedingungen, wobei der schnelle Durchschnitt die langsame Linie (und dieselben vier Offset-Bänder) unterschreitet.
5. Die Strategie eröffnet nur dann eine neue Marktposition, wenn keine Aufträge aktiv sind und die aktuelle Position flach ist, was dem Single-Ticket-Verhalten des MQL-Experten entspricht.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `TakeProfitPips` | `int` | `130` | Take-Profit-Distanz in Pips. Auf `0` setzen, um das Ziel zu deaktivieren. |
| `StopLossPips` | `int` | `1000` | Stop-Loss-Distanz in Pips. Auf `0` einstellen, um den Schutzstopp zu deaktivieren. |
| `TradeVolume` | `decimal` | `1` | Basislosgröße, die mit jeder Bestellung gesendet wird (automatisch angepasst auf `VolumeStep`). |
| `CalculationBar` | `int` | `1` | Anzahl der Balken, die als Anker für den MA-Vergleich verwendet werden (MetaTrader `shift`). |
| `FastPeriod` / `SlowPeriod` | `int` | `14` / `180` | Periodenlängen der gleitenden Durchschnitte. |
| `FastMethod` / `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Mittelungstechnik: `Simple`, `Exponential`, `Smoothed` oder `LinearWeighted`. |
| `FastPrice` / `SlowPrice` | `CandlePrice` | `Median` | Angewandter Preis, der von jedem gleitenden Durchschnitt verwendet wird. |
| `UpperLevel1` / `UpperLevel2` | `int` | `500` / `250` | Für Toleranzprüfungen werden positive Offsets (in Pips) zum langsamen MA hinzugefügt. |
| `LowerLevel1` / `LowerLevel2` | `int` | `500` / `250` | Negative Offsets (in Pips) werden für Toleranzprüfungen vom langsamen MA abgezogen. |
| `CandleType` | `DataType` | `15m` Zeitrahmen | Kerzenserie, auf die die Indikatoren wirken. |

## Hinweise zur Implementierung
- Stop-Loss- und Take-Profit-Orders werden über `StartProtection` emuliert, wobei die Distanzen mithilfe des `PriceStep` des Instruments von Pips in Preiseinheiten umgewandelt werden. Fünfstellige FX-Kurse erhalten automatisch den MetaTrader-artigen `*10`-Multiplikator.
- Interne Warteschlangen speichern nur die Daten, die zur Reproduktion der `shift`-Logik erforderlich sind. Es wird keine vollständige Kerzenhistorie akkumuliert.
- Aufträge werden mit `BuyMarket` / `SellMarket` erteilt und erben das normalisierte Volumen, sodass die Benutzeroberfläche die aktive Losgröße widerspiegelt.
- Bei der Diagrammausgabe werden die Kerzenserien mit den gleitenden Durchschnitten und den ausgeführten Trades zur schnellen visuellen Überprüfung zusammengeführt.
- Alle Inline-Kommentare sind auf Englisch, um den Projektrichtlinien zu entsprechen.

## Anwendungstipps
- Wählen Sie das gleiche Kerzenintervall, das Sie in MetaTrader verwenden würden. Die standardmäßige `15`-Minutenreihe kann über `CandleType` geändert werden.
- Reduzieren Sie die Offset-Werte, um die Signale selektiver zu machen, oder vergrößern Sie sie, um breitere „Beinahe-Unfall“-Überkreuzungen zu ermöglichen.
- Wenn Sie `CalculationBar` auf `0` setzen, reagiert die Strategie auf die letzte geschlossene Kerze (keine Verzögerung), während höhere Werte den Auslöser für zusätzliche Bestätigung weiter in die Vergangenheit verschieben.
- Deaktivieren Sie die Schutzbeine (`StopLossPips = 0`, `TakeProfitPips = 0`), wenn die Ausgänge manuell oder von einem anderen Modul verwaltet werden sollen.
