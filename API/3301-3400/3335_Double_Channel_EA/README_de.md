# Double-Channel-EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Der **Double Channel EA** repliziert die Handelslogik des MetaTrader 4 Expert Advisors „DoubleChannelEA_v1.2“. Die StockSharp p
ort passt den benutzerdefinierten *iDoubleChannel_v1.5*-Indikator an und führt Breakout-Trades aus, wenn der Indikator Pfeile druckt. Die Strategie
y ist für diskretionäre Tests mit konfigurierbaren Risikomanagement- und Zeitplanfiltern konzipiert.

Hauptmerkmale:

- Benutzerdefiniertes `DoubleChannelIndicator` baut die oberen, unteren und mittleren Kanalpuffer sowie die Kauf-/Verkaufspfeilsignale neu auf.
- Hochwertige API-Nutzung mit Kerzenabonnements, Level-1-Spread-Validierung und nativen Order-Helfern.
- Optionale Geldverwaltungstools: Stapelpositionen, Break-Even, Trailing Stop, Take-Profit und Stop-Loss-Logik.
- Tageszeitfilter und Spread-Filter blockieren Einträge außerhalb benutzerdefinierter Betriebsbedingungen.

## Handelslogik

1. Abonnieren Sie den ausgewählten `CandleType` und geben Sie jede fertige Kerze in den `DoubleChannelIndicator` ein.
2. Der Indikator speichert ein bewegliches Fenster von `ChannelPeriod` Kerzen und berechnet:
   - Mittellinie: arithmetisches Mittel der Schlusskurse.
   - Obere Linie: Mitte plus die Differenz zweier Preisumschläge, die aus Höchst- und Tiefstwerten abgeleitet werden.
   - Untere Linie: Mitte plus die Differenz komplementärer Hüllkurven, abgeleitet von Eröffnungen und Tiefs.
   - Pfeilsignale: Die vorherigen beiden Kanalpositionen müssen umkehren und die vorherige Kerze muss in Richtung des Durchbruchs schließen
kout. Die Regeln entsprechen den MT4-Pufferbedingungen.
3. Signale können um `IndicatorShift` Balken verzögert werden, um den Indikatorverschiebungsparameter zu reproduzieren.
4. Ein Kaufsignal eröffnet eine Long-Position (Stacking zulässig, wenn `OpenEverySignal = true`). Ein Verkaufssignal eröffnet eine Short-Position. Op
Positive Positionen können sofort geschlossen werden, wenn `CloseInSignal = true`.
5. Schutzausgänge verwalten die aktive Position bei jeder fertigen Kerze:
   - Statische Stop-Loss-/Take-Profit-Abstände, ausgedrückt in absoluten Preiseinheiten.
   - Break-Even-Aktivierung, sobald der Preis um `BreakEvenPoints + BreakEvenAfterPoints` steigt.
   - Trailing Stop, der vor der Aktualisierung eine Verbesserung von `TrailingStepPoints` erfordert.
6. Einträge werden abgelehnt, wenn:
   - Die Strategie liegt außerhalb der Handelszeiten (`UseTimeFilter`).
   - Der Live-Spread übersteigt `MaxSpreadPoints`.
   - `MaxOrders` gestapelte Positionen sind für die aktuelle Richtung bereits offen.

## Money-Management

Das Auftragsvolumen errechnet sich wie folgt:

„
volume = ManualLotSize * (AutoLotSize ? max(RiskFactor, 0,1) : 1)
„

Beim Rückwärtsfahren berücksichtigt die Strategie automatisch die absolute Gegenposition, um in einem einzigen Marsch in die neue Richtung umzudrehen
ket-Reihenfolge.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 15-minütiger Zeitrahmen | Primäres Kerzenabonnement. |
| `ChannelPeriod` | 14 | Rückblick auf den benutzerdefinierten Kanal. |
| `IndicatorShift` | 0 | Verzögerung, bevor auf Indikatorwerte reagiert wird. |
| `OpenEverySignal` | wahr | Ermöglicht das Stapeln von Positionen auf aufeinanderfolgenden Signalen. |
| `CloseInSignal` | falsch | Schließt die aktuelle Position, wenn ein entgegengesetzter Pfeil erscheint. |
| `UseTakeProfit` | falsch | Aktiviert `TakeProfitPoints`. |
| `TakeProfitPoints` | 10 | Absoluter Preisabstand zum Ziel. |
| `UseStopLoss` | falsch | Aktiviert `StopLossPoints`. |
| `StopLossPoints` | 10 | Absoluter Preisabstand für den Schutzstopp. |
| `UseTrailingStop` | falsch | Aktiviert nachgestellte Logik mit `TrailingStopPoints` und `TrailingStepPoints`. |
| `TrailingStopPoints` | 5 | Abstand vom aktuellen Preis zum Trailing Stop. |
| `TrailingStepPoints` | 1 | Mindestverbesserung erforderlich, bevor der Trailing Stop aktualisiert wird. |
| `UseBreakEven` | falsch | Ermöglicht Break-Even-Anpassungen. |
| `BreakEvenPoints` | 4 | Ziel-Stoppniveau, sobald die Gewinnschwelle aktiviert ist. |
| `BreakEvenAfterPoints` | 2 | Vor der Aktivierung der Gewinnschwelle ist ein zusätzlicher Gewinn erforderlich. |
| `AutoLotSize` | wahr | Multipliziert das manuelle Los mit `RiskFactor`. |
| `RiskFactor` | 1 | Bei der automatischen Größenanpassung wird ein Risikomultiplikator angewendet. |
| `ManualLotSize` | 0,01 | Basisvolumen, wenn die automatische Größenanpassung deaktiviert ist. |
| `UseTimeFilter` | falsch | Aktiviert den Zeitplanfilter. |
| `TimeStartTrade` | 0 | Handelsstartstunde (einschließlich). |
| `TimeEndTrade` | 0 | Handelsschlussstunde (exklusiv). Gleicher Anfangs- und Endpunkt bedeutet keine Einschränkung. |
| `MaxOrders` | 0 | Maximal gestapelte Positionen pro Richtung (0 = unbegrenzt). |
| `MaxSpreadPoints` | 0 | Maximal zulässige Geld-Brief-Spanne in Preiseinheiten. |

## Hinweise zur Konvertierung

- Der ursprüngliche Indikator stellte Pfeile dar, indem er die Werte um einen Balken nach vorne verschob. Die StockSharp-Version speichert frühere Snapshots und
prüft die gleichen Crossover-Kriterien, bevor ein Signal für die aktuelle Kerze ausgegeben wird.
- Die Spread-Filterung basiert auf Daten der ersten Ebene. Wenn keine Angebote verfügbar sind, blockiert die Strategie neue Aufträge und ahmt den MQL-Expe nach
rt, der sich weigerte, ohne Verbreitung von Informationen zu handeln.
- Die Geldverwaltung in MT4 nutzte kontobasierte Berechnungen. Aus Gründen der Portabilität wurde die Volumenformel auf einen Risikomultiplikator vereinfacht
Er wird auf die manuelle Losgröße angewendet.
- Stop-Loss-, Take-Profit-, Trailing-Stop- und Break-Even-Abstände werden in absoluten Preiseinheiten interpretiert (dieselbe Konvention a
s andere StockSharp Conversions). Passen Sie sie entsprechend der Tick-Größe des Instruments an.
