# T3MA(MTC)-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert vom MetaTrader 4 Expert Advisor **T3MA(MTC).mq4** (Verzeichnis `MQL/7904`). Der ursprüngliche Roboter handelt mit Signalen des Indikators „T3MA-ALARM“: Er erstellt einen doppelt geglätteten exponentiellen gleitenden Durchschnitt und platziert immer dann eine Order, wenn die Steigung dieser Kurve von fallend zu steigend wechselt oder umgekehrt. Der StockSharp-Port spiegelt dieselbe Logik mit idiomatischen High-Level-APIs wider.

## Handelsidee

1. Erstellen Sie ein erstes EMA mit dem ausgewählten Kerzentyp und -zeitraum.
2. Glätten Sie diese Reihe mit einem zweiten EMA desselben Zeitraums.
3. Vergleichen Sie den geglätteten Wert mit dem vorherigen (optional um `MaShift` verschoben).
4. Wenn die Steigung ihre Richtung ändert, zeichnet die Strategie ein Signal auf. Aufträge werden nach der konfigurierten `CalculationBarOffset`-Verzögerung ausgeführt, wobei der `CalculationBarIndex`-Parameter von EA reproduziert wird.
5. Jedes Signal verwendet das Tief (für einen Long-Einstieg) oder das Hoch (für einen Short-Einstieg) des Balkens als eindeutige Markierung, um doppelte Trades zu vermeiden, genau wie die Variable `LastOrder` in MetaTrader.

## Portierungsdetails

- Verwendet zwei `ExponentialMovingAverage`-Instanzen, um die T3MA-ALARM-Glättungskette zu emulieren.
- Verwaltet eine kleine Warteschlange aktueller geglätteter Werte zur Unterstützung des `MaShift`-Lookbacks.
- Signale werden in einer FIFO-Warteschlange gespeichert und nach der angeforderten Anzahl fertiger Kerzen ausgeführt.
- Schutzaufträge werden über `StartProtection` verwaltet, wobei die Entfernungen in Preisschritten ausgedrückt werden und MetaTrader Punkten entsprechen.
- Das Flag `AllowMultiplePositions` reproduziert die Eingabe `MultiPositions`: Wenn es deaktiviert ist, wartet die Strategie, bis die Nettoposition flach ist, bevor sie auf ein neues Signal reagiert.

## Parameter

- `MaPeriod` – EMA Länge, die für beide Glättungsdurchgänge verwendet wird (Standard: 4).
- `MaShift` – Anzahl der Balken, um die die geglättete Reihe verschoben wird, bevor ihre Steigung verglichen wird (Standard: 0).
- `CalculationBarOffset` – Verzögerung (in fertigen Kerzen) zwischen der Erkennung eines Signals und dem Senden der Bestellung (Standard: 1).
- `TradeVolume` – Basisauftragsvolumen in Losen (Standard: 1).
- `UseStopLoss` / `StopLossPoints` – Aktivierung und Abstand des Stop-Loss in Preisschritten (Standard: aktiviert, 40 Schritte).
- `UseTakeProfit` / `TakeProfitPoints` – Aktivierung und Entfernung des Take-Profits in Preisschritten (Standard: aktiviert, 11 Schritte).
- `AllowMultiplePositions` – Stapelpositionen zulassen, auch wenn eine gegenüberliegende Position offen ist (Standard: aktiviert).
- `CandleType` – Zeitrahmen oder Datentyp, der zur Versorgung der Indikatorkette verwendet wird (Standard: 5-Minuten-Kerzen).

## Handelsablauf

1. Abonnieren Sie die ausgewählte Kerzenserie und geben Sie Schlusskurse über die doppelte EMA-Kette ein.
2. Verfolgen Sie die aktuelle Hangrichtung und erzeugen Sie ein Signal, wenn es umkippt.
3. Schieben Sie jedes Signal (oder das Fehlen eines Signals) in die Verzögerungswarteschlange, damit die Ausführung genau nach `CalculationBarOffset` abgeschlossenen Kerzen erfolgt, genau wie das MQL4-Skript ältere Indikatorpuffer liest.
4. Wenn ein ausgereiftes Signal ausgeführt wird:
   - Überspringen Sie es, wenn der Handel deaktiviert ist, die Plattform nicht bereit ist oder `AllowMultiplePositions` ausgeschaltet ist, während bereits eine Nettoposition offen ist.
   - Stellen Sie sicher, dass sich der Signalmarker vom vorherigen unterscheidet, um Duplikate zu vermeiden.
   - Senden Sie eine Marktorder (`BuyMarket`/`SellMarket`) mit dem konfigurierten Volumen. Bei Aktivierung werden automatisch Schutzstopps angebracht.

## Notizen

- Bei Preisvergleichen wird eine kleine Dezimaltoleranz verwendet, um Gleitkomma-Artefakte bei der Überprüfung des `LastOrder`-Analogs zu vermeiden.
- Die Strategie schließt gegenüberliegende Positionen nicht automatisch, wenn `AllowMultiplePositions` deaktiviert ist, und ahmt damit das ursprüngliche EA nach, das auf Schutzausgängen beruhte.
- Die Visualisierung von Kerzen und eigenen Trades ist verfügbar, wenn das Charting-Subsystem vorhanden ist.
