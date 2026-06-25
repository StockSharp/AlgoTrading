# Trend Me Leave Me Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Trend Me Leave Me** Strategie ist ein direkter Port des klassischen MQL5-Expertenberaters von Yury Reshetov. Sie wartet
geduldig auf ruhige Preisbewegungsphasen, schließt sich der vorherrschenden Richtung des Parabolic SAR an und wechselt die
Handelsrichtung nach profitablen Ausstiegen. Wenn ein Trade gestoppt wird, versucht die Strategie dieselbe Richtung erneut und
recreiert das ursprüngliche "trend me, leave me"-Verhalten. Diese C#-Implementierung verwendet die StockSharp High-Level-API und
bewahrt den vollständigen Entscheidungsfluss des Quellsystems, während jede numerische Eingabe als konfigurierbarer Parameter
exponiert wird.

## Kernideen
### Ruhigmarkt-Filter
- Der Average Directional Index (ADX) mit der Länge `AdxPeriod` misst die Richtungsstärke.
- Nur wenn der gleitende Durchschnitt des ADX unter `AdxQuietLevel` fällt, erlaubt die Strategie neue Einstiege, imitierend
  den EA-Fokus auf Rücksetzer mit geringer Volatilität.

### SAR-Ausrichtung für das Timing
- Parabolic SAR-Punkte dienen als direktionale Führung. Ein Long-Signal erfordert, dass der Kerzenschluss über dem SAR-Punkt
  liegt, während ein Short-Signal einen Schluss unterhalb des Punktes erfordert.
- Die SAR-Parameter `SarStep` und `SarMax` entsprechen den Beschleunigungseinstellungen der MQL-Version und können bei Bedarf
  optimiert werden.

### Richtungsplaner
- Ein `TradeDirections`-Flag repräsentiert die ursprüngliche `cmd`-Variable. Es beginnt im *Kauf*-Zustand.
- Nach einem **Take-Profit**-Ausstieg wechselt das Flag zur entgegengesetzten Seite und lädt zu einem Umkehr-Trade ein.
- Nach einem **Stop-Loss** (oder Breakeven)-Ausstieg bleibt das Flag auf derselben Seite, damit die nächste Gelegenheit die
  vorherige Richtung wiederholt.

## Trade-Management
- `StopLossPips` und `TakeProfitPips` definieren feste Abstände vom durchschnittlichen Ausführungspreis. Das Setzen eines
  Parameters auf `0` deaktiviert den entsprechenden Schutz.
- `BreakevenPips` verschiebt den Stop auf den Einstiegspreis, sobald sich der Markt um die angegebene Pip-Distanz zugunsten
  bewegt. Wenn der Preis später zum Einstiegsniveau zurückkehrt, wird der Trade für nahezu null Gewinn geschlossen, was das
  nächste Signal auf derselben Seite hält.
- Die Stop/Take-Logik wird auf jeder abgeschlossenen Kerze unter Verwendung sowohl des Hochs als auch des Tiefs ausgewertet,
  um Intrabar-Hits anzunähern und das Tick-für-Tick-Verhalten des EA in einer balkengesteuerten Umgebung so genau wie möglich
  zu bewahren.

## Positionsgröße
- Das Ordervolumen wird durch die Basis-`Strategy.Volume`-Eigenschaft gesteuert. Das Beispiel hält das Risikomodell einfach
  und enthält nicht das Fixed-Risk-Geldmanagement-Objekt aus dem MQL-Skript. Passen Sie `Volume` an oder überschreiben Sie die
  Strategie, wenn eine fortgeschrittenere Dimensionierung erforderlich ist.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `StopLossPips` | Abstand in Pips zwischen dem Einstiegspreis und dem Schutz-Stop. | `50` |
| `TakeProfitPips` | Abstand in Pips zwischen dem Einstiegspreis und dem Ziel. | `180` |
| `BreakevenPips` | Den Stop auf den Einstieg verschieben nach dieser Anzahl Pips günstiger Bewegung. | `5` |
| `AdxPeriod` | Glättungsperiode für den ADX-Filter. | `14` |
| `AdxQuietLevel` | Maximale ADX-Ablesung, die noch als ruhiger Markt gilt. | `20` |
| `SarStep` | Beschleunigungsschritt des Parabolic SAR. | `0.02` |
| `SarMax` | Maximaler Beschleunigungsfaktor des Parabolic SAR. | `0.2` |
| `CandleType` | Für Berechnungen verwendeter Zeitrahmen. | `1h` Kerzen |

## Implementierungshinweise
- Pip-Berechnungen folgen der Zifferanpassung des EA: Wenn das Instrument 3 oder 5 Dezimalstellen verwendet, wird der
  Preisschritt mit 10 multipliziert, um die Broker-Tick-Größe in einen Standard-Pip umzurechnen.
- Indikator-Bindungen verlassen sich auf die StockSharp High-Level-API, und alle Handelsaktionen verwenden
  `BuyMarket`/`SellMarket`, um mit den S#-Konventionen konform zu bleiben.
- Noch keine Python-Übersetzung enthalten. Das Verzeichnis `PY/` ist wie gewünscht absichtlich nicht vorhanden.
- Hängen Sie die Strategie an ein von StockSharp unterstütztes Symbol an. Stellen Sie `Volume` vor dem Start der Strategie
  ein und passen Sie die Parameter an die Volatilität des Instruments an.
