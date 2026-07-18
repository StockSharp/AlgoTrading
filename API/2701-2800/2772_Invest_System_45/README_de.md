# Invest System 4.5 Strategie (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Invest System 4.5 ist ein MetaTrader 5-Expertenberater, der in die StockSharp High-Level-Strategie-API portiert wurde. Die Strategie handelt das EUR/USD-Paar, indem sie der Richtung der vorherigen abgeschlossenen 4-Stunden-Kerze folgt. Während der ersten Minuten der neuen 4-Stunden-Session ist ein einziger Trade erlaubt und die Positionsgröße passt sich an die realisierte Performance und das Kontowachstum an.

Der Code verlässt sich ausschließlich auf die High-Level-API: Automatische Kerzenabonnements werden verwendet, um sowohl den 4-Stunden-Richtungsbias als auch das Einstiegsfenster des niedrigeren Zeitrahmens zu überwachen, während der integrierte `StartProtection`-Helper statische Take-Profit- und Stop-Loss-Niveaus in Pips durchsetzt.

## Handelslogik
1. **Richtungsbias** – beim Schließen jeder fertigen 4-Stunden-Kerze speichert die Strategie, ob die Kerze bullisch oder bärisch schloss. Eine bullische Kerze ermöglicht nur Long-Einstiege für die nächste Session, während eine bärische Kerze nur Shorts ermöglicht. Wenn die Kerze genau auf ihrem Öffnungspreis schließt, wird die vorherige Richtung beibehalten.
2. **Einstiegs-Timing** – wenn eine neue 4-Stunden-Kerze beginnt, öffnet sich ein Einstiegsfenster. Das Fenster bleibt für eine konfigurierbare Anzahl von Minuten gültig (standardmäßig 15). Die Strategie beobachtet Kerzen des niedrigeren Zeitrahmens (standardmäßig 1 Minute) und kann höchstens eine Marktorder senden, wenn alle Filter erfüllt sind, während das Fenster aktiv ist.
3. **Einzelne Position** – die Strategie pyramidisiert nie. Wenn bereits eine Position offen ist, werden keine neuen Signale bis zur nächsten 4-Stunden-Session verarbeitet. Sobald eine Order gesendet wird, schließt sich das Einstiegsfenster sofort, um das MetaTrader-Verhalten zu replizieren.
4. **Gewinn- und Verlustnachverfolgung** – wenn eine Position vollständig geschlossen wird, wird der realisierte PnL erfasst, um die unten beschriebene adaptive Lotlogik anzutreiben.

## Positionsgrößen-Regeln
Der ursprüngliche Expertenberater verwendet zwei Schichten des Geldmanagements:
- **Eigenkapital-Meilensteine**: der anfängliche Kontostand wird beim allerersten Update gespeichert. Wenn das Eigenkapital 2×, 3× … 6× den anfänglichen Saldo übersteigt, wird die Basis-Lotgröße proportional erhöht. Stufe 1 beginnt bei `BaseLot`, Stufe 2 verdoppelt ihn, Stufe 3 verdreifacht ihn, und so weiter. Sekundäre Lotgrößen (`Lot2`, `Lot3`, `Lot4`) werden mit den ursprünglichen Multiplikatoren (×2, ×7 und ×14 jeweils) abgeleitet.
- **Plan B-Eskalation**: zwischen Trades wird ein einzelner globaler Volumenwert gehalten.
  - Nach einem verlierenden Trade mit dem Basis-Lot wird das Volumen auf das zweite Lot (`Lot3`) erhöht.
  - Wenn ein weiterer Verlust beim Handeln des zweiten Lots auftritt, aktiviert sich "Plan B". Plan B ordnet die internen Lot-Optionen neu zu, sodass das Basis-Lot zu `Lot2` und das aggressive Lot zu `Lot4` wird. Das aktuelle Volumen ändert sich nicht sofort, aber jeder nachfolgende Verlust schiebt die Strategie zum aggressiven Lot. Plan B wird automatisch abgebrochen, wenn das Konto ein neues Eigenkapital-Hoch erreicht.
  - Ein profitabler Trade setzt das aktuelle Volumen immer auf das Basis-Lot für die aktive Stufe zurück.
Diese Regeln reproduzieren die kaskadierende Lot-Eskalation der MetaTrader-Version genau, ohne manuell durch Orders zu iterieren oder Sammlungen zu verwenden.

## Risikomanagement
- `StartProtection` konfiguriert sowohl den Stop-Loss als auch den Take-Profit in absoluten Preiseinheiten, die aus der Pip-Größe abgeleitet werden. Stops und Ziele werden nur einmal registriert, wenn die Strategie gestartet wird, genau wie der ursprüngliche EA die Werte an jede Order anhängt.
- Es werden nur Marktorders verwendet. Die Strategie selbst führt keine Hedge-Positionen, Skalierungen oder Teilausstiege durch; Ausstiege erfolgen über die konfigurierten Schutzorders.

## Strategie-Parameter
| Parameter | Beschreibung | Standard | Optimierungsbereich |
|-----------|-------------|---------|---------------------|
| `StopLossPips` | Stop-Loss-Abstand in Pips. `0` zum Deaktivieren des Stops verwenden. | 240 | 120 – 360, Schritt 20 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. `0` zum Deaktivieren des Ziels verwenden. | 40 | 20 – 80, Schritt 10 |
| `EntryWindowMinutes` | Länge des Einstiegsfensters nach jeder neuen 4-Stunden-Kerze. | 15 | 5 – 30, Schritt 5 |
| `SignalCandleType` | Für die Überwachung des Einstiegsfensters verwendete Kerzenreihe (standardmäßig 1 Minute). | 1-Minuten-Zeitrahmen | – |
| `TrendCandleType` | Kerze des höheren Zeitrahmens zur Richtungsbestimmung (standardmäßig 4 Stunden). | 4-Stunden-Zeitrahmen | – |
| `BaseLot` | Anfängliche Lotgröße für Stufe 1. Andere Lotgrößen werden automatisch abgeleitet. | 0.1 | 0.05 – 0.3, Schritt 0.05 |

## Dateistruktur
```
2772_Invest_System_45/
├── CS/
│   └── InvestSystem45Strategy.cs
├── README.md
├── README_ru.md
└── README_zh.md
```

## Hinweise
- Die Strategie erwartet, dass das angehängte Wertpapier sowohl die 4-Stunden-Kerzenreihe als auch die schnellere Zeitrahmen-Reihe bereitstellt. Diese Abonnements werden automatisch innerhalb von `OnStarted` erstellt.
- Die Pip-Größe wird aus `Security.PriceStep` bestimmt und für Bruchquotierungen (3 oder 5 Dezimalstellen) angepasst, um der Behandlung von Pip-Werten durch MetaTrader zu entsprechen.
- Da der ursprüngliche Roboter Kontostand-Schwellenwerte verwendet, liest die StockSharp-Implementierung `Portfolio.CurrentValue` bei jeder Einstiegskerzen-Aktualisierung. Bei der Ausführung in der Simulation stellen Sie sicher, dass das Portfoliomodell das aktuelle Eigenkapital aktualisiert, damit die Lot-Skalierung konsistent bleibt.
- Die Python-Übersetzung wird wie gewünscht absichtlich weggelassen.
