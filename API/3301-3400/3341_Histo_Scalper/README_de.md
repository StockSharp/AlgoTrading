# Histo Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Histo Scalper Strategy** ist eine C#-Portierung des MetaTrader Expert Advisors *HistoScalperEA v1.0*. Der Algorithmus verschmilzt acht Indikatoren im Histogrammstil (ADX, ATR, Bollinger Bands, Bulls/Bears Power, CCI, MACD, RSI und Stochastic) und erfordert die einstimmige Zustimmung aller aktivierten Filter, bevor ein Handel eröffnet wird. Eine zweite Anforderung besteht darin, dass mindestens ein Filter die entgegengesetzte Richtung des vorherigen Balkens meldet, was verhindert, dass die Strategie bei flachen Märkten eintritt, und die ursprüngliche Bestätigungslogik „zwei Balken“ nachahmt.

## Signalerzeugung
1. **ADX-Filter** – prüft, ob +DI größer als −DI ist. Optional können Sie die Entscheidung umkehren.
2. **ATR-Filter** – vergleicht den aktuellen ATR mit einer SMA-Basislinie und misst die prozentuale Abweichung. Long-Trades erfordern eine positive Abweichung über `AtrPositiveThreshold`; Short-Trades erfordern eine negative Abweichung unter `AtrNegativeThreshold`.
3. **Bollinger Ausbruch** – erwartet, dass der Schlusskurs das obere/untere Band durchbricht.
4. **Bullen-/Bears-Power** – nutzt Bulls-Power für Long-Einstiege und Bears-Power-Größe für Short-Einstiege.
5. **CCI** – wird ausgelöst, wenn der Wert CCI die konfigurierten überverkauften/überkauften Ebenen überschreitet.
6. **MACD-Histogramm** – misst den Abstand zwischen MACD und seiner Signallinie.
7. **RSI** – verwendet klassische überverkaufte/überkaufte Zonen.
8. **Stochastic** – liest die %K-Zeile und vergleicht sie mit konfigurierten Grenzen.

Wenn ein aktivierter Filter einen neutralen Wert erzeugt, bricht die Strategie die Verarbeitung für die aktuelle Kerze ab. Der historische Zustand jedes Filters wird gespeichert, um die Regel „vorheriger Balken gegenüber“ durchzusetzen.

## Risikomanagement
* Markteinträge verwenden den Parameter `TradeVolume`.
* Optionales Pyramiding erhöht die Zahl der offenen Positionen; Andernfalls ändert die Strategie nur die Richtung, wenn sich das Signal ändert.
* Take-Profit- und Stop-Loss-Level werden in Preisschritten des Instruments ausgedrückt und unmittelbar nach Auftragserteilung über `SetTakeProfit` und `SetStopLoss` angewendet.
* Ein Sitzungsfilter (`UseTimeFilter`, `SessionStart`, `SessionEnd`) kann den Handel außerhalb der konfigurierten Zeiten deaktivieren.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Basisvolumen für neue Trades.
| `AllowPyramiding` | Ermöglicht das Stapeln zusätzlicher Trades, während bereits positioniert.
| `CloseOnOppositeSignal` | Schließt bestehende Positionen, wenn das aggregierte Signal umkehrt.
| `UseTimeFilter`, `SessionStart`, `SessionEnd` | Beschränkt den Handel auf ein benutzerdefiniertes Tagesfenster.
| `UseTakeProfit`, `TakeProfitPoints` | Ermöglicht und konfiguriert Take-Profit in Preisschritten.
| `UseStopLoss`, `StopLossPoints` | Aktiviert und konfiguriert Stop-Loss in Preisschritten.
| `UseIndicator1` … `UseIndicator8` | Aktivieren Sie einzelne Filter.
| `ModeIndicatorX` | Wechseln Sie für jeden Filter zwischen gerader und invertierter Logik.
| Indikatorenspezifische Einstellungen | Zeiträume, Schwellenwerte und Ebenen, die die ursprünglichen Eingaben des Expertenberaters nachbilden.

## Unterschiede zum MQL Expert
* Auf die Korb-Gewinn-/Verlustverwaltung, akustische Alarme und die Rasterauftragsverwaltung wurde bewusst verzichtet.
* Die Risikoautomatisierung (automatische Losgrößenbestimmung, Break-Even- und Trailing-Logik) ist nicht enthalten; Verwenden Sie stattdessen die oben genannten Risikoparameter.
* Spread-Checks und Broker-spezifische Schutzmaßnahmen werden nicht portiert.

## Nutzungshinweise
1. Legen Sie `Security` und `Portfolio` fest, bevor Sie mit der Strategie beginnen.
2. Passen Sie den Kerzentyp (`CandleType`) an den gewünschten Zeitrahmen an.
3. Konfigurieren Sie die Indikatorschwellenwerte so, dass sie zur Volatilität des Zielinstruments passen.
4. Aktivieren oder deaktivieren Sie Filter einzeln, um die Optimierung zu vereinfachen.
5. Verwenden Sie `AllowPyramiding` und `CloseOnOppositeSignal`, um die Präsenz in schnellen Märkten zu kontrollieren.
