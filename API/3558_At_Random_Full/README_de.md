# Zufällige vollständige Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **At Random Full Strategy** ist eine originalgetreue Umsetzung des MetaTrader 5 Expertenberaters „At random Full“. Es hält die
Ursprüngliche Idee, Trades auf der Grundlage eines Zufallsgenerators zu eröffnen und gleichzeitig die gleichen Money-Management-Wechsel offenzulegen: Richtung
Filter, Rasterabstände, optionale Zeitfenster und ein Ein-/Ausschalter für die Mittelwertbildung. Der Port StockSharp verwendet den High-Level-Port API,
Daher wird die gesamte Entscheidungsschleife durch Kerzenabonnements und standardmäßige `StartProtection`-Helfer für Schutzanordnungen gesteuert.

## Handelslogik
1. Bei jeder abgeschlossenen Kerze überprüft die Strategie, ob der Handel zulässig ist (Sitzungsfilter, Portfoliostatus und optional).
Flag „nur eine Position“).
2. Ein Pseudozufallsgenerator entscheidet zwischen einem Long- oder Short-Einstieg. Der Parameter `ReverseSignals` kann das Ergebnis umdrehen
emulieren den MQL-Umkehrmodus.
3. Richtungsfilter (`TradeMode`) blockieren unerwünschte Signale. Der Code erzwingt auch die ursprüngliche EA-Regel eines einzelnen Trades pro
Bar in jede Richtung, indem Sie sich die Kerzenöffnungszeit des letzten Signals merken.
4. Die Rasterverwaltungsoptionen spiegeln das Verhalten von MetaTrader wider:
   - `MaxPositions` begrenzt die Anzahl der gemittelten Einträge pro Seite.
   - `MinStepPoints` erfordert einen Mindestabstand (umgerechnet unter Verwendung der Sicherheitspreisstufe in einen Preis) zwischen aufeinanderfolgenden Einträgen.
   - `CloseOpposite` erzwingt die Schließung des bestehenden Gegenrisikos, bevor ein neuer Handel gesendet wird.
5. Marktaufträge werden über `BuyMarket` / `SellMarket` mit einem normalisierten Volumen erteilt, das durch `OrderVolume` definiert ist.

## Positions- und Risikomanagement
- `StartProtection` fügt Stop-Loss- und Take-Profit-Orders hinzu, die den MetaTrader-Eingaben entsprechen. Wenn `TrailingStopPoints` ist
größer als Null ist, wird der integrierte StockSharp-Trailing-Modus aktiviert. Die Parameter `TrailingActivatePoints` und
`TrailingStepPoints` werden aus Gründen der Transparenz in Preisabstände umgewandelt und protokolliert, das tatsächliche Nachziehen wird jedoch von der verwaltet
Plattform.
- Alle Volumenberechnungen berücksichtigen die Austauschmetadaten (Minimum, Maximum und Schritt) genau wie die MQL-Hilfsroutinen.
- Die Zeitsteuerung emuliert den Block `InpTimeControl` aus dem Skript. Wenn diese Option aktiviert ist, sind Trades nur innerhalb des konfigurierten Bereichs zulässig
`[SessionStart, SessionEnd]` Fenster; Übernachtungssitzungen werden unterstützt.

## Parameter
| Parameter | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzenserien, die zum Planen der Entscheidungsschleife verwendet werden. | `15 minute timeframe` |
| `OrderVolume` | Basis-Market-Order-Volumen in Lots. | `0.1` |
| `MaxPositions` | Maximale Anzahl gemittelter Einträge pro Richtung (0 = unbegrenzt). | `5` |
| `MinStepPoints` | Mindestabstand zwischen Einträgen, ausgedrückt in MetaTrader Punkten. | `150` |
| `StopLossPoints` | Stop-Loss-Distanz in Punkten. | `150` |
| `TakeProfitPoints` | Take-Profit-Distanz in Punkten. | `460` |
| `TrailingActivatePoints` | Gewinnschwelle (in Punkten), die zu Informationszwecken protokolliert wird, wenn das Trailing aktiviert ist. | `70` |
| `TrailingStopPoints` | Trailing-Stop-Distanz wurde an `StartProtection` übergeben. | `250` |
| `TrailingStepPoints` | Schritt zwischen Nachlaufanpassungen, protokolliert neben der Aktivierungsdistanz. | `50` |
| `OnlyOnePosition` | Blockiert neue Trades, bis die aktuelle Nettoposition geschlossen ist. | `false` |
| `CloseOpposite` | Schließt das entgegengesetzte Risiko, bevor ein Handel eröffnet wird. | `false` |
| `ReverseSignals` | Kehrt die zufällige Entscheidung um, sodass Käufe zu Verkäufen werden und umgekehrt. | `false` |
| `UseTimeControl` | Aktiviert den Zeitfilter für Handelssitzungen. | `false` |
| `SessionStart` | Sitzungsstartzeit (einschließlich), wenn `UseTimeControl` `true` ist. | `10:01` |
| `SessionEnd` | Sitzungsendzeit (einschließlich), wenn `UseTimeControl` `true` ist. | `15:02` |
| `Mode` | Zulässige Handelsrichtung (`Both`, `BuyOnly`, `SellOnly`). | `Both` |
| `RandomSeed` | Optionaler deterministischer Startwert für den Pseudozufallsgenerator (0 = Anzahl der Umgebungsticks). | `0` |

## Implementierungshinweise
- Alle Kommentare sind auf Englisch verfasst und der Code verwendet Tabulatoreinrückungen, entsprechend den Repository-Richtlinien.
- Die Kerzenverarbeitung basiert auf `SubscribeCandles().Bind(...)` und stellt sicher, dass die Logik wie in EA einmal pro fertigem Balken ausgeführt wird.
- Die Strategie verfolgt die letzten Kauf- und Verkaufsfüllpreise, um die Mindestabstandsbeschränkung auch während der Mittelwertbildung durchzusetzen.
- Protokollierungsanweisungen spiegeln die detaillierten Diagnosen wider, die das Originalskript ausgibt: Jeder Eintrag gibt die gewählte Richtung bekannt.
Einstiegspreis, Volumen und die nachfolgende Konfiguration beim Start.

## Nutzungstipps
- Da das Handelssignal zufällig ist, eignet sich die Strategie am besten zum Testen der Infrastruktur oder zur Demonstration von Risikokontrollen.
- Passen Sie `OrderVolume`, `StopLossPoints` und `TakeProfitPoints` an, um sie an die Tick-Größe und Volatilität des von Ihnen verwendeten Instruments anzupassen
planen zu handeln.
- Aktivieren Sie `UseTimeControl`, wenn EA nur während einer bestimmten Sitzung (z. B. der Sitzung in London oder New York) funktionieren soll.
- Verwenden Sie `RandomSeed` während Optimierungsläufen, um reproduzierbare Sequenzen zufälliger Entscheidungen zu erreichen.
