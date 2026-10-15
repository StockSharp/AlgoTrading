# 1H EUR/USD MACD-Swing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MetaTrader-Expert-Advisor "1H EUR_USD" in die High-Level-API von StockSharp. Sie handelt das EUR/USD-Paar auf stündlichen Kerzen mit doppelten gleitenden Durchschnitten und MACD-Swing-Erkennung. Einstiege erfordern sowohl einen Trendfilter (schnelle MA über/unter langsamer MA) als auch ein MACD-Doppelboden/Doppeltop-Muster kombiniert mit einem Ausbruch aus letzten Hochs oder Tiefs. Das Risiko wird mit Pip-basierten Stop-Loss, Take-Profit und einem inkrementellen Trailing Stop gesteuert, der die ursprüngliche EA-Logik widerspiegelt.

## Details

- **Markt**: Entwickelt für EUR/USD auf dem 1-Stunden-Zeitrahmen, kann aber auf jedes Instrument angewendet werden, das Standardkerzen produziert.
- **Einstiegskriterien**:
  - **Long**:
    - Schnelle MA liegt über der langsamen MA (Typ wählbar zwischen SMA, EMA, SMMA, LWMA).
    - MACD-Hauptlinie bildet eines der folgenden bullischen Swings vollständig unterhalb der Nulllinie:
      - `MACD[-1] > MACD[-2] < MACD[-3]` mit `MACD[-2] < 0` und der aktuelle Schluss bricht das Hoch der vorherigen Kerze.
      - `MACD[-2] > MACD[-3] < MACD[-4]` mit `MACD[-3] < 0` und der aktuelle Schluss bricht das Hoch von vor zwei Kerzen.
  - **Short**:
    - Schnelle MA liegt unter der langsamen MA.
    - MACD-Hauptlinie bildet die gespiegelten bärischen Swings vollständig oberhalb der Nulllinie und der Preis schließt unter dem relevanten vorherigen Tief.
- **Ausstiegskriterien**:
  - Pip-basierter Take-Profit und Stop-Loss werden unmittelbar nach dem Einstieg angehängt.
  - Trailing Stop aktiviert sich erst, wenn der Preis sich um `TrailingStop + TrailingStep` Pips zu Gunsten bewegt hat, und folgt dann dem Preis im Abstand von `TrailingStop` Pips, entsprechend der schrittweisen Modifikationslogik des EA.
  - Schutzorders werden am innerperiodischen Hoch/Tief der Kerze ausgelöst.
- **Positionsverwaltung**:
  - Verwendet das konfigurierte Handelsvolumen; das Umkehren von Positionen schließt die entgegengesetzte Seite vor dem Eröffnen der neuen.
  - Long- und Short-Trades teilen dieselben Pip-Berechnungen (Pip-Größe passt sich automatisch an 4/5-stellige Kurse an).
- **Indikatoren**:
  - Schnelle und langsame gleitende Durchschnitte mit wählbarem Typ (Einfach, Exponentiell, Geglättet, Linear Gewichtet) und optionaler horizontaler Verschiebung.
  - Klassischer MACD (schnelle/langsame/Signal-EMA-Längen).
- **Parameter**:
  - `TradeVolume` – Basis-Lot-Größe mit jeder Order.
  - `StopLossPips`, `TakeProfitPips` – Schutzabstände in Pips (auf null setzen zum Deaktivieren).
  - `TrailingStopPips`, `TrailingStepPips` – Trailing-Konfiguration; Trailing-Schritt muss positiv bleiben, wenn Trailing aktiv ist.
  - `FastMaLength`, `FastMaShift`, `FastMaType` – schnelle MA-Einstellungen.
  - `SlowMaLength`, `SlowMaShift`, `SlowMaType` – langsame MA-Einstellungen.
  - `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – MACD-Parameter.
  - `CandleType` – Zeitrahmen zur Verarbeitung (Standard: 1 Stunde).
  - `LookbackPeriod` – für Kompatibilität mit den MQL-Eingaben beibehalten; ändert die Logik nicht, da der ursprüngliche EA ihn auch ungenutzt ließ.

## Hinweise

- Trailing-Stop-Verhalten spiegelt die MQL-Version: Es findet keine Anpassung statt, bis sowohl die Trailing-Distanz als auch der Trailing-Schritt durch unrealisierte Gewinne überschritten werden.
- Die Strategie geht davon aus, dass der Preis-Schritt gleich dem Quote-Punkt ist; wenn das Instrument 3 oder 5 Dezimalstellen hat, skaliert der Code die Pip-Größe automatisch um 10.
- Kommentare im C#-Quellcode erklären jeden Schlüsselblock auf Englisch für einfachere Wartung und Erweiterung.
