# Rnd Trade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertierung des MetaTrader 5-Expertenberaters `RndTrade.mq5` in die StockSharp High-Level-Strategie-API.
- Schließt jede bestehende Position in einem festen Zeitintervall und öffnet sofort eine neue Marktposition in einer zufällig gewählten Richtung.
- Verwendet zeitbasierte Kerzenabonnements als deterministischen Ersatz für die ursprünglichen Timer-Callbacks.

## Parameter
| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `IntervalMinutes` | `int` | `60` | Anzahl der Minuten zwischen dem Schließen der aktuellen Position und dem Öffnen einer neuen zufälligen Position. Muss größer als null sein. |
| `Volume` | `decimal` | `1` | Positionsgröße für Markteinträge. Abgeleitet aus der `Strategy`-Basisklasse. |

## Daten-Abonnements
- Abonniert Zeitrahmen-Kerzen, deren Länge `IntervalMinutes` entspricht (z.B. `60` → 60-Minuten-Kerzen).
- Das Kerzen-Schließereignis (`CandleStates.Finished`) wird verwendet, um die Logik genau einmal pro Intervall auszulösen.

## Handelslogik
1. Warten auf den Abschluss jeder Intervallkerze.
2. Verarbeitung überspringen, bis die Strategie gebildet, online und der Handel erlaubt ist.
3. Jede in der vorherigen Periode eröffnete offene Position schließen.
4. Einen Zufallswert generieren, um zwischen einem Long- oder Short-Einstieg zu entscheiden.
5. Einen Marktauftrag (`BuyMarket` oder `SellMarket`) mit dem konfigurierten Volumen in der gewählten Richtung senden.

## Implementierungshinweise
- Basiert auf `SubscribeCandles().Bind(ProcessCandle)`, um manuelles Polling von Indikatorwerten oder Sammlungen zu vermeiden.
- Ruft `StartProtection()` beim Start auf, damit das integrierte Risikomodul aktiv ist, auch wenn kein expliziter Stop-Loss oder Take-Profit konfiguriert ist.
- Verwendet `Random` aus der Standardbibliothek, um das `MathRand()`-Verhalten aus der ursprünglichen MQL-Strategie zu imitieren.
- Der Code enthält englische Kommentare, die erklären, wie jeder Konvertierungsschritt auf StockSharp-Funktionen abgebildet wird.

## Unterschiede zur ursprünglichen MQL-Strategie
- Timer-Ereignisse (`OnTimer`) werden durch Kerzenabonnements anstelle der MetaTrader-Timer-API emuliert.
- Das Schließen von Positionen wird mit `ClosePosition()` gehandhabt, anstatt Positionslisten zu durchlaufen und für jedes Ticket `PositionClose` aufzurufen.
- Die StockSharp-Version stützt sich auf die integrierte `Volume`-Eigenschaft für die Positionsgrößenbestimmung anstelle der Abfrage des Mindest-Lots des Symbols.
- Auftragserfüllungsregeln und Slippage-Einstellungen werden vom verbundenen Broker oder Simulator verwaltet, daher sind sie in der Strategie nicht explizit konfiguriert.

## Verwendung
1. Strategie einem Portfolio und einem Wertpapier in der StockSharp-Umgebung zuordnen.
2. `IntervalMinutes` und `Volume` entsprechend der gewünschten Handelsfrequenz und -größe konfigurieren.
3. Strategie starten. Sie wird automatisch Positionen abflachen und bei jedem Intervall wieder öffnen, ohne zusätzliche Eingaben.
4. Keine Python-Implementierung ist derzeit verfügbar; nur die C#-Version ist erhältlich.
