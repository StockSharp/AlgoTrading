# Bullish & Bearish Engulfing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie repliziert das klassische bullische und bärische Engulfing-Kerzenmuster, das ursprünglich in MetaTrader für den Expert Advisor "Bullish and Bearish Engulfing" implementiert wurde. Der StockSharp-Port bewertet abgeschlossene Kerzen in einem konfigurierbaren Zeitrahmen, überspringt optional eine Anzahl aktueller Bars und reagiert, wenn ein Engulfing-Muster einen minimalen Abstandsfilter erfüllt. Die Logik ist für diskretionäre Trader konzipiert, die ein bewährtes Price-Action-Muster automatisieren möchten, während sie die Kontrolle über Richtung, Volumen und die Behandlung bestehender Positionen behalten.

## Musterdefinition
Ein Engulfing-Signal wird bestätigt, wenn zwei aufeinanderfolgende abgeschlossene Kerzen die folgenden Regeln erfüllen (nach Anwendung des konfigurierten Versatzes):

- **Bullisches Engulfing**
  - Die zuletzt bewertete Kerze schließt über ihrem Eröffnungskurs (bullischer Körper).
  - Die vorhergehende Kerze schließt unter ihrem Eröffnungskurs (bärischer Körper).
  - Die bullische Kerze hat ein höheres Hoch und ein niedrigeres Tief als die vorherige Kerze, und zwar mindestens um den Abstandsfilter.
  - Der bullische Schluss liegt über dem vorherigen Eröffnungskurs und sein Eröffnungskurs liegt unter dem vorherigen Schluss, wiederum unter Einhaltung des Abstandsfilters.
- **Bärisches Engulfing**
  - Die bewertete Kerze schließt unter ihrem Eröffnungskurs (bärischer Körper).
  - Die vorhergehende Kerze schließt über ihrem Eröffnungskurs (bullischer Körper).
  - Die bärische Kerze druckt noch ein höheres Hoch, schließt aber deutlich unter dem vorherigen Eröffnungskurs, und ihr Eröffnungskurs übersteigt den vorherigen Schluss, jeweils um den Abstandsfilter.
  - Das Tief des bärischen Bars liegt unter dem vorherigen Tief um den Abstandsfilter.

Diese Bedingungen reproduzieren die ursprüngliche MetaTrader-Implementierung, die forderte, dass die Engulfing-Kerze den vorherigen Körper vollständig abdeckt und über beide Extreme hinausgeht. Der Abstandsfilter wird in Pips gemessen und mit dem Preisschritt und den Dezimalstellen des Instruments in einen Preis umgerechnet (5-stellige und 3-stellige Forex-Kurse werden automatisch auf 10-Punkt-Pips skaliert).

## Handelslogik
1. Den ausgewählten Kerzentyp über die High-Level-API abonnieren und nur abgeschlossene Kerzen verarbeiten.
2. Einen kleinen rollierenden Puffer pflegen, der die OHLC-Werte speichert, die für den aktuellen Versatzzwert erforderlich sind.
3. Wenn mindestens zwei historische Kerzen zur Auswertung verfügbar sind, die oben beschriebenen bullischen und bärischen Engulfing-Bedingungen prüfen.
4. Bei einem bullischen Signal eine Marktorder auf der durch **BullishSide** definierten Seite senden. Bei einem bärischen Signal die über **BearishSide** konfigurierte Seite verwenden.
5. Wenn **CloseOppositePositions** aktiviert ist und ein gegenläufiges Exposure besteht, erhöht die Strategie das Ordervolumen um die absolute aktuelle Position, sodass der resultierende Trade das gegenläufige Bein schließt und ein neues in der gewünschten Richtung eröffnet. Wenn das Flag deaktiviert ist, werden Signale ignoriert, solange eine gegenläufige Position offen ist.
6. Die Positionsgröße wird durch den Strategieparameter **Volume** gesteuert (Standard: 1 Kontrakt/Lot). Standardmäßig wird kein automatischer Stop-Loss oder Take-Profit angehängt; das Risikomanagement bleibt dem Endbenutzer oder Schutzmodulen überlassen (kann mit den integrierten Schutzfunktionen von StockSharp kombiniert werden).

## Parameter
| Parameter | Beschreibung | Standard | Hinweise |
|-----------|--------------|----------|----------|
| `CandleType` | Zeitrahmen (StockSharp `DataType`) zur Signalentdeckung. | 1-Stunden-Zeitrahmen | Anpassbar an jeden unterstützten Kerzentyp. |
| `Shift` | Anzahl der abgeschlossenen Kerzen, die vor der Musterbewertung übersprungen werden sollen. | 1 | Wert 1 analysiert den zuletzt geschlossenen Bar; höhere Werte schauen weiter zurück. |
| `DistanceInPips` | Mindestabstand in Pips, den die Engulfing-Kerze gegenüber der vorherigen überschreiten muss. | 0 | In Preis umgerechnet anhand des Instruments-Preisschritts; nützlich zum Herausfiltern von Kerzen mit kleinen Körpern. |
| `CloseOppositePositions` | Ob eine bestehende gegenläufige Position geschlossen werden soll, wenn ein neues Signal ausgelöst wird. | `true` | Wenn deaktiviert, wird der Trade übersprungen, wenn das aktuelle Exposure mit dem Signal in Konflikt steht. |
| `BullishSide` | Orderseite bei einem bullischen Engulfing-Signal. | `Buy` | Kann für konträres Verhalten auf `Sell` umgestellt werden. |
| `BearishSide` | Orderseite bei einem bärischen Engulfing-Signal. | `Sell` | Kann auf `Buy` umgestellt werden, um gegen den Trend zu handeln. |
| `Volume` | Basisordergröße. | 1 | Das Ordervolumen wird beim Schließen der Gegenseite um `abs(Position)` erhöht. |

## Positionsmanagement und Risiko
- Da Orders ohne Schutz-Stops zum Marktpreis gesendet werden, die Strategie mit zusätzlichen Modulen (z. B. `StartProtection`) kombinieren oder externe Risikokontrollen konfigurieren.
- Der originale MetaTrader-Code dimensionierte Trades über einen risikobasierten Money Manager. In diesem Port wird die Dimensionierung auf einen direkten Volumensparameter vereinfacht, damit das Verhalten innerhalb von StockSharp deterministisch ist; einen benutzerdefinierten Money-Management-Block integrieren, wenn dynamisches Sizing erforderlich ist.
- Wenn `CloseOppositePositions` `true` ist, erfolgen Umkehrungen sofort: Das Tradevolumen entspricht dem Basisvolumen plus der absoluten offenen Position, was einen direkten Übergang von flach zur neuen Richtung garantiert.

## Dateien
- `CS/BullishBearishEngulfingStrategy.cs` – Haupt-C#-Implementierung, basierend auf der High-Level-StockSharp-Strategie-API.

> **Hinweis:** Für diese ID ist keine Python-Implementierung vorgesehen; nur die C#-Version ist wie angefordert enthalten.
