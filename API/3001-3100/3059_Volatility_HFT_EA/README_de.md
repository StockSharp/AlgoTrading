# Volatility HFT EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den **Volatility HFT EA** MetaTrader 5-Expertenberater in die High-Level-API von StockSharp. Sie reproduziert die ursprüngliche Logik, die kauft, wenn der Schlusskurs weit über einem schnellen einfachen gleitenden Durchschnitt springt, und auf einen Pullback zu diesem Durchschnitt wartet. Order-Generierung, Indikatormanagement und Schutzausstiege folgen den Richtlinien aus `AGENTS.md`, während das Verhalten des MQL-Skripts beibehalten wird.

## Funktionsweise

1. **Indikator-Setup** – ein einziger einfacher gleitender Durchschnitt (Standard-Länge: 5) wird auf dem durch `CandleType` angegebenen Arbeitszeitrahmen berechnet.
2. **Neue-Bar-Erkennung** – die Verarbeitung erfolgt nur einmal, wenn eine Kerze fertig ist (`CandleStates.Finished`), was die `IsNewBar`-Prüfungen im EA spiegelt.
3. **Aufwärmphase** – die Strategie wartet auf 60 abgeschlossene Kerzen, bevor sie Trades bewertet, entsprechend der `Bars < 60`-Schutzabfrage in MQL.
4. **Einstiegsfilter** – ein Long-Setup erscheint, wenn der letzte Schlusskurs mindestens `MaDifferencePips` über dem SMA liegt (Differenz in Preis umgerechnet mittels der Pip-Größe des Instruments) und der SMA-Wert höher ist als vor zwei Bars. Der ursprüngliche EA verwendete `val[0] < -0.0015` und `MA_Val1[0] > MA_Val1[2]`; dieser Port prüft dieselben Bedingungen ohne manuelle Array-Speicherung.
5. **Jeweils eine Position** – nur Long-Trades werden unterstützt, da der Verkaufs-Zweig in der Quelldatei auskommentiert war. Ein neues Signal wird ignoriert, während eine Position offen ist.

## Risikomanagement

- **Stop-Loss** – optionaler Schutz-Stop in Pips. Der Code leitet die Pip-Größe aus `Security.PriceStep` ab und multipliziert mit 10, wenn das Instrument 3 oder 5 Dezimalstellen hat, was die `_Digits`-Skalierung aus MetaTrader reproduziert.
- **Take-Profit** – das Ausstiegsziel ist auf den beim Einstieg erfassten SMA-Wert verankert, was den `mrequest.tp = MA_Val1[0];`-Aufruf spiegelt. Die Strategie schließt die Position, wenn das Tief der Kerze das gespeicherte SMA-Niveau berührt, was eine Limit-Order am Durchschnitt emuliert.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `OrderVolume` | Volumen für jede Marktorder. |
| `FastMaLength` | Periode des schnellen einfachen gleitenden Durchschnitts (Standard 5). |
| `StopLossPips` | Stop-Loss-Abstand in Pips; auf `0` setzen zum Deaktivieren. |
| `MaDifferencePips` | Mindestabstand (in Pips) zwischen Schluss und SMA für einen Long-Einstieg. |
| `CandleType` | Zeitrahmen für Kerzen-Abonnement und Indikatorberechnungen. |

`MinimumBars` ist eine feste interne Konstante gleich `60`, die die Anforderung des EA für ausreichende Historie widerspiegelt.

## Verwendung

1. Binden Sie die Strategie an ein Wertpapier und wählen Sie den gewünschten `CandleType` (z. B. 1-Minuten-Bars für Hochfrequenz-Verhalten).
2. Passen Sie `FastMaLength`, `MaDifferencePips` und `StopLossPips` an die Volatilität des Instruments an. Pip-basierte Eingaben werden automatisch über die erkannte Pip-Größe umgerechnet, sodass dieselben Standardwerte für 4- und 5-stellige FX-Symbole funktionieren.
3. Konfigurieren Sie `OrderVolume` entsprechend Ihren Portfolio-Sizing-Regeln. Die Strategie sendet nur Marktorders und wird keine Positionen aufstocken.
4. Starten Sie die Strategie. Sie abonniert die gewählten Kerzen, baut den SMA auf, wartet auf 60 Warm-up-Bars und bewertet dann Einstiege bei jeder abgeschlossenen Kerze.
5. Überwachen Sie das Trade-Management: Ausstiege werden entweder durch die SMA-Berührung oder durch den beim Einstieg abgeleiteten Stop-Loss-Preis ausgelöst.

## Hinweise und Unterschiede zum ursprünglichen EA

- Die MQL-Version forderte die Mindest-Lot-Größe über `SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN)` an; hier ist das Volumen als Parameter exponiert, um die Strategie bei verschiedenen Brokern und Asset-Klassen flexibel zu halten.
- Verkaufsbedingungen werden weggelassen, da sie in `Volatility_HFT_EA.mq5` auskommentiert waren. Das Verhalten entspricht daher dem veröffentlichten Skript, das nur den Long-Zweig ausführte.
- Die Take-Profit-Behandlung verwendet Kerzen-Tiefs, um eine Berührung des gleitenden Durchschnitts zu erkennen, anstatt eine Limit-Order zu registrieren, was sicherstellt, dass dieselbe Absicht zuverlässig im StockSharp-Workflow funktioniert.
- Manuelles Array-Management (`CopyRates`, `CopyBuffer`, `ArraySetAsSeries`) wird durch StockSharp-Indikator-Bindungen ersetzt. Dies reduziert Boilerplate bei gleichzeitiger Beibehaltung der ursprünglichen Schwellen und Steigungsvergleiche.
- Alle Berechnungen bleiben kerzenbasiert; die Strategie schaut nicht mit `GetValue` in Indikator-Puffer zurück und bleibt damit konform mit den Repository-Richtlinien.
