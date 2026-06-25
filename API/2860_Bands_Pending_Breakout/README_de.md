# Bands Ausbruch mit ausstehenden Aufträgen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Expertenberater "Bands 2" auf Basis der StockSharp High-Level-API. Sie überwacht abgeschlossene Kerzen, prüft, ob die aktuelle Zeit innerhalb des konfigurierten Handelsfensters liegt und ob der Preis innerhalb des Bollinger-Kanals handelt. Wenn diese Bedingungen erfüllt sind, platziert sie ein symmetrisches Raster aus drei Kauf-Stop- und drei Verkauf-Stop-Aufträgen um den Bollinger-Envelope. Jeder Auftrag trägt seine eigenen Stop-Loss- und Take-Profit-Abstände, und jede Ausführung entfernt die anderen ausstehenden Aufträge.

Der Ansatz ist für Ausbrüche aus den Bollinger-Bändern konzipiert. Die Stop-Loss-Referenz kann zwischen dem gegenüberliegenden Band oder dem zentralen gleitenden Durchschnitt umgeschaltet werden. Ein separates Trailing-Stop-Modul zieht den Schutz-Stop kontinuierlich an, sobald sich die Position um einen konfigurierbaren Schritt in Profit bewegt.

## Details

- **Marktdaten**: Funktioniert mit jedem Instrument/Kerzentyp, der über StockSharp bereitgestellt wird.
- **Handelszeiten**: Verwendet `HourStart`/`HourEnd`, um die Auftragsplatzierung zu beschränken. Aufträge werden bei jeder abgeschlossenen Kerze innerhalb dieses Fensters aktualisiert.
- **Einstiegslogik**:
  - Auf eine abgeschlossene Kerze mit Schlusskurs strikt zwischen den verschobenen oberen und unteren Bollinger-Bändern warten.
  - Verbleibende ausstehende Aufträge aus dem vorherigen Balken löschen und drei Kauf-Stops über dem oberen Band und drei Verkauf-Stops unter dem unteren Band platzieren.
  - Jede Stufe ist durch `StepPips`, umgerechnet in Ticks, getrennt.
- **Stop-Loss-Modi**:
  - *BollingerBands*: Stop-Loss verwendet das gegenüberliegende Band, verschoben um die gleiche Schrittdistanz wie der Einstiegsauftrag.
  - *MovingAverage*: Stop-Loss verwendet den gleitenden Durchschnittswert plus/minus die Schrittdistanz (verwendet den konfigurierten angewandten Preis und die Methode).
  - *None*: Kein anfänglicher Stop gesetzt; Trailing-Stop kann später aktiviert werden.
- **Take-Profit-Logik**:
  - Erste Stufe verwendet `FirstTakeProfitPips` für Kauf- und Verkaufsaufträge.
  - Zweiter und dritter Kaufauftrag verwenden `Second`/`Third` Take-Profit-Abstände, während Verkaufsaufträge das ursprüngliche MQL-Skript-Verhalten befolgen und immer die erste Take-Profit-Distanz wiederverwenden.
- **Auftragsmanagement**:
  - Wenn ein ausstehender Auftrag ausgeführt wird, storniert die Strategie alle anderen Eintragsaufträge und erstellt marktunabhängige Schutzaufträge (Stop + Limit) für das ausgeführte Volumen.
  - Das Trailing-Modul bewegt den Stop-Auftrag in Richtung Markt, sobald der Preis sich um `TrailingStopPips + TrailingStepPips` vom Einstieg bewegt.
  - Schutz-Stop/Limit-Aufträge werden automatisch storniert, wenn die Position glatt geht.
- **Preisnormalisierung**: Alle Preisniveaus werden auf die Tick-Größe des Instruments gerundet, und die Punkt-zu-Pip-Konvertierung ahmt die ursprüngliche 3/5-Stellen-Behandlung nach.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Volumen für jeden ausstehenden Auftrag (gleiches Volumen für alle sechs Aufträge). |
| `CandleType` | Zeitrahmen/Datentyp für Indikatorberechnungen. |
| `HourStart`, `HourEnd` | Inklusive/exklusive Stunden (0-24), die das Platzieren neuer ausstehender Aufträge erlauben. `HourEnd` muss größer als `HourStart` sein. |
| `StopLossModes` | Platzierungsreferenz für den anfänglichen Stop-Loss (`BollingerBands`, `MovingAverage`, `None`). |
| `FirstTakeProfitPips`, `SecondTakeProfitPips`, `ThirdTakeProfitPips` | Take-Profit-Abstände (in Pips) umgerechnet in Preisoffsets für die erste, zweite und dritte Eingabe. |
| `TrailingStopPips`, `TrailingStepPips` | Trailing-Stop-Abstand und der zusätzliche Schritt, der erforderlich ist, bevor der Stop vorgerückt wird. Null zum Deaktivieren des Trailing. |
| `StepPips` | Abstand zwischen aufeinanderfolgenden ausstehenden Aufträgen (in Preis umgerechnet). |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Konfiguration des gleitenden Durchschnitts für den Bollinger-Eingang und optional für die Stop-Platzierung wenn `StopLossModes` `MovingAverage` ist. Das `MaShift` emuliert die Vorwärtsverschiebung des ursprünglichen EA. |
| `BandsPeriod`, `BandsShift`, `BandsDeviation`, `BandsPriceType` | Bollinger-Band-Einstellungen (Zeitraum, Verschiebung, Abweichungsmultiplikator und angewandter Preis). |

## Verhaltensübersicht

1. Abgeschlossene Kerzen des ausgewählten Zeitrahmens abonnieren.
2. Bei jeder abgeschlossenen Kerze innerhalb des Handelsfensters die verschobenen Bollinger-Bänder und den gleitenden Durchschnitt mit den ausgewählten angewandten Preisen berechnen.
3. Sicherstellen, dass der Kerzenschluss innerhalb des Bandkanals liegt, dann das Kauf-/Verkauf-Stop-Raster um die Kanalränder mit individuellen Stops und Zielen platzieren.
4. Wenn ein Auftrag ausgeführt wird, die verbleibenden Eintragsaufträge stornieren, Schutz-Stop/Limit-Aufträge einreichen und Trailing gemäß den konfigurierten Parametern starten.
5. Schutzaufträge schließen wenn die Position endet, bereit für die nächste Ausbruchsgelegenheit.
