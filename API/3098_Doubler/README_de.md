# Doubler Hedge-Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Doubler Hedge-Trailing-Strategie** ist eine direkte StockSharp High-Level-API-Konvertierung des MetaTrader 5 Expert Advisors `Doubler.mq5`. Der Algorithmus öffnet sofort eine symmetrische Long- und Short-Market-Position, wann immer kein Exposure besteht, und verwaltet dann beide Legs mit unabhängigen Stop-Loss-, Take-Profit- und Trailing-Stop-Regeln. Die Konvertierung bewahrt das Hedging-Verhalten des ursprünglichen MQL-Programms und passt das Risikomanagement an StockSharp-Primitive an (Market-Orders, Level1-Subscriptions und Strategieparameter).

Im Gegensatz zu direktionalen Strategien hält das System beide Richtungen aktiv, bis jedes Leg durch seine eigene Schutzlogik aussteigt. Sobald *beide* Legs flach sind, recreiert die Strategie den Hedge und hält kontinuierlich gepaarte Exposure aufrecht.

## Hauptmerkmale
- **Automatisches Hedging** – öffnet eine Kauf- und eine Verkaufsorder mit demselben Volumen, wann immer die Strategie keine aktiven Positionen hat.
- **Pip-basierte Risikokontrollen** – Stop-Loss, Take-Profit und Trailing-Offsets werden in Pips konfiguriert und intern durch Inspektion des Wertpapierpreisschritts und der Dezimalgenauigkeit in Preisschritte umgewandelt (3/5-Dezimal-Instrumente werden automatisch um den Faktor 10 skaliert).
- **Unabhängiges Trailing pro Leg** – jedes Leg verfolgt das aktuelle beste Bid/Ask. Wenn sich der Preis um mehr als `TrailingStopPips + TrailingStepPips` günstig bewegt, wird das Stop-Level um `TrailingStopPips` vorgerückt und dabei die Trailing-Schritt-Bedingung eingehalten, was die ursprüngliche EA-Logik exakt widerspiegelt.
- **Volumenvalidierung** – das Ordervolumen wird gegen `MinVolume`, `MaxVolume` und `VolumeStep` validiert, und es wird eine Ausnahme ausgelöst, wenn die angeforderte Größe die Börsenbeschränkungen verletzt.
- **Optionale Diagnose** – das `LogTradeDetails`-Flag aktiviert detaillierte Informationsmeldungen (Einstiege, Ausstiege, Trailing-Anpassungen), die beim Testen oder Live-Monitoring helfen.

## Parameter
| Parameter | Beschreibung | Standard | Hinweise |
|-----------|--------------|----------|----------|
| `OrderVolume` | Volumen jedes Hedge-Legs (Kauf- und Verkaufsorders). | `1` | Muss Börsenvolumengrenzen einhalten; normalisiert auf den nächsten `VolumeStep`. |
| `StopLossPips` | Stop-Loss-Distanz in Pips. | `150` | `0` deaktiviert den Stop-Loss. |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. | `300` | `0` deaktiviert den Take-Profit. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. | `5` | Wenn größer als null, muss `TrailingStepPips` ebenfalls positiv sein. |
| `TrailingStepPips` | Minimaler zusätzlicher Move bevor der Trailing Stop vorrückt. | `5` | Schutzgeländer, das verhindert, dass sich der Stop zu häufig bewegt. |
| `LogTradeDetails` | Aktiviert ausführliches Logging von Ausführungen und Trailing-Updates. | `false` | Auf `true` setzen für Debugging-Läufe. |

## Handelslogik
### Einstieg
1. Level1-Updates abonnieren (bestes Bid/Ask).
2. Wenn sowohl `_longPosition` als auch `_shortPosition` null sind und keine Einstiegsorders ausstehen, zwei Market-Orders registrieren: eine Kauf- und eine Verkaufsorder mit jeweils `OrderVolume`.
3. Nach Bestätigung der Ausführungen zeichnet die Strategie Einstiegspreise auf, setzt initiale Stop/Take-Levels und setzt Trailing-Tracker zurück.

### Risikomanagement
- **Stop-Loss** – für jedes Leg wird der anfängliche Stop `StopLossPips` weit vom Eintrittspreis entfernt platziert. Eine Stop-Distanz von `0` deaktiviert den Schutz-Stop vollständig.
- **Take-Profit** – symmetrischer Take-Profit bei `TakeProfitPips`. Ein Wert von `0` deaktiviert Gewinnziele.
- **Erzwungene Schließung** – wenn `NormalizeVolume` eine ungültige Größe erkennt (zu klein/groß oder nicht mit `VolumeStep` übereinstimmend), wirft die Strategie eine Ausnahme, um das Senden einer ungültigen Order zu verhindern.

### Trailing-Stop-Verhalten
1. Wenn sich der Preis günstig um mindestens `TrailingStopPips + TrailingStepPips` bewegt, wird der Stop zu `currentPrice ± TrailingStopPips` vorgerückt.
2. Die Trailing-Schritt-Prüfung reproduziert die MQL-Bedingung: Der Stop bewegt sich nur, wenn das neue Level mindestens `TrailingStepPips` näher am Preis liegt als der bestehende Stop, oder wenn noch kein Stop existiert.
3. Für Long-Positionen wird das beste Bid als Referenzpreis verwendet; für Short-Positionen das beste Ask, damit Ausstiegslevel realistische Ausführungspreise widerspiegeln.

### Ausstieg
- Jedes Leg steigt unabhängig aus, wann immer seine Stop-Loss-, Trailing-Stop- oder Take-Profit-Bedingung erfüllt ist. Ausstiegsorders werden als Market-Orders registriert, und sobald ein Leg flach ist, wird sein interner Zustand geleert.
- Nachdem beide Legs geschlossen wurden, löst das nächste Level1-Update ein brandneues gehedgtes Paar aus.

## Datenanforderungen
- **Level1 (bestes Bid/Ask)** – erforderlich für Eintrittspreis-Snapshots, Trailing-Berechnungen und Ausstiegs-Trigger.
- Keine Kerzen- oder Trade-Subscription notwendig; die Strategie reagiert ausschließlich auf Level1-Updates.

## Konvertierungshinweise
- Pip-Abstände werden in absolute Preis-Offsets umgerechnet, indem mit dem Wertpapier-`PriceStep` multipliziert wird. Instrumente mit 3 oder 5 Dezimalstellen erhalten automatisch eine ×10-Anpassung, was der im MetaTrader-Expert verwendeten Pip-Definition entspricht.
- Die Strategie basiert auf den High-Level-`Strategy`-Methoden von StockSharp (`RegisterOrder`, `StartProtection`, `SubscribeLevel1`) und vermeidet Low-Level-Connector-Operationen.
- Hedging wird durch interne `PositionState`-Objekte implementiert, damit Long- und Short-Legs verfolgt werden, auch wenn der Broker/das Portfolio Netto-Positionen verwendet.
- Die Konvertierung ist in sich geschlossen und modifiziert oder erfordert kein Test-Harness aus dem Repository.
