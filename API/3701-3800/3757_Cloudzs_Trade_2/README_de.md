# Cloudzs Trade 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Cloudzs Trade 2 Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters `cloudzs_trade_2`. Der ursprüngliche Roboter kombiniert stochastische Oszillatorumkehrungen mit einem doppelfraktalen Bestätigungsfilter und nutzt aggressive Trailing-Logik, um offene Positionen zu schützen. Diese C#-Version erstellt den Signalfluss und die Handelsverwaltungsregeln neu und stellt die Parameter als `StrategyParam`-Objekte bereit, sodass sie über die StockSharp-Benutzeroberfläche optimiert oder angepasst werden können.

Die Strategie beobachtet eine einzelne Kerzenserie (konfigurierbarer Zeitrahmen) und bewertet zwei unabhängige Bedingungen:

1. **Stochastic-Umkehr** – wird ausgelöst, wenn die %D-Linie eine extreme Zone verlässt (>= 80 für Verkäufe, <= 20 für Käufe), während bestätigt wird, dass %D die %K-Linie bei der vorherigen Kerze überschritten hat, was der ursprünglichen MQL-Logik weitgehend entspricht.
2. **Doppelte fraktale Bestätigung** – wartet, bis zwei aufeinanderfolgende fraktale Signale desselben Typs erscheinen (zwei obere Fraktale für Verkäufe oder zwei untere Fraktale für Käufe).

Wenn eine der Bedingungen eine Kauf- oder Verkaufsanfrage generiert, geht die Strategie in diese Richtung (vorausgesetzt, es ist kein Handel aktiv und der vorherige Ausstieg erfolgte an einem anderen Tag). Wenn Sie sich bereits in einem Handel befinden, können dieselben Bedingungen zum vorzeitigen Beenden verwendet werden, wenn `CloseOnOpposite` aktiviert ist.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `LotSplitter` | Koeffizient zur Schätzung des Handelsvolumens anhand des aktuellen Kontowerts. | `0.1` |
| `MaxVolume` | Obergrenze für das berechnete Volumen (0 deaktiviert die Obergrenze). | `0` |
| `TakeProfitOffset` | Feste Take-Profit-Distanz in absoluten Preiseinheiten. | `0` |
| `TrailingStopOffset` | Trailing-Stop-Distanz in Preiseinheiten. | `0.01` |
| `StopLossOffset` | Feste Stop-Loss-Distanz in Preiseinheiten. | `0.05` |
| `MinProfitOffset` | Mindestgewinn, der nach einem günstigen Ausschlag beibehalten werden muss, sobald `ProfitPointsOffset` erreicht wurde. | `0` |
| `ProfitPointsOffset` | Erforderlicher günstiger Schritt, bevor `MinProfitOffset` erzwungen wird. | `0` |
| `%K Period` / `%D Period` / `Slowing` | Stochastic Oszillatorkonfiguration. | `8 / 8 / 4` |
| `Method` | Ursprüngliche Kennung der stochastischen MT4-Methode (informativ, nicht verwendet, da StockSharp eine einzelne Implementierung verfügbar macht). | `3` |
| `PriceMode` | Ursprüngliche MT4-Preismodus-ID (nur informativ). | `1` |
| `UseStochasticCondition` | Aktivieren Sie die stochastische Signalgenerierung. | `true` |
| `UseFractalCondition` | Aktivieren Sie die fraktalbasierte Signalgenerierung. | `true` |
| `CloseOnOpposite` | Schließen Sie die aktive Position, wenn das entgegengesetzte Signal erscheint. | `true` |
| `CandleType` | Zeitrahmen/Datentyp, der für Berechnungen verwendet wird. | `15-minute time frame` |

## Handelssignale
### Langer Eintrag
- Die %D-Linie liegt unter oder gleich 20 und kreuzt unter %K (entspricht dem Vorkerzenvergleich von MT4).
- **ODER** Es werden zwei aufeinanderfolgende untere Fraktale erkannt.
- Keine offene Position und der letzte Ausstieg erfolgte an einem anderen Kalendertag.

### Kurzer Eintrag
- Die %D-Linie liegt über oder gleich 80 und kreuzt über %K.
- **ODER** Es erscheinen zwei aufeinanderfolgende obere Fraktale.
- Keine offene Position und der letzte Ausstieg erfolgte an einem anderen Kalendertag.

### Ausgangsregeln
- Harte Stop-Loss- oder Take-Profit-Level werden erreicht (sofern konfiguriert).
- Der Trailing-Stop bewegt sich zu Gunsten des Handels und der Preis berührt das aktualisierte Stop-Level.
- Nachdem sich die Position um `ProfitPointsOffset` günstig bewegt hat, wird der Handel durch einen Rückzug auf `MinProfitOffset` geschlossen.
- Optionale vorzeitige Umkehr: Wenn `CloseOnOpposite` wahr ist und das entgegengesetzte Signal ausgelöst wird, wird der Handel geschlossen.

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände ahmen die rohen Pip-Offsets des MT4-Codes nach (hier als Preisunterschiede interpretiert).
- Trailing Stops werden anhand des Schlusskurses aktualisiert und bewegen sich nur in die profitable Richtung.
- Der Parameter `LotSplitter` versucht, der ursprünglichen Volumenformel zu folgen, indem er die Handelsgröße nach Kontowert skaliert und mit `MaxVolume` begrenzt.

## Hinweise und Einschränkungen
- Der StockSharp `StochasticOscillator` stellt eine einzelne Glättungsimplementierung bereit; Daher werden die Parameter `Method` und `PriceMode` als Referenz beibehalten, ändern jedoch nicht das Verhalten des Indikators.
- Das ursprüngliche MT4-Skript funktionierte Tick für Tick. Dieser Port wertet Signale an fertigen Kerzen aus, um sie an StockSharp Best Practices anzupassen.
- Die Volumenberechnung basiert auf den verfügbaren Portfoliowerten; Wenn keine Kontoinformationen vorhanden sind, wird auf den Wert `LotSplitter` zurückgegriffen.

## Nutzung
1. Fügen Sie die Strategie zu Ihrem StockSharp-Projekt hinzu und wählen Sie das Instrument aus, mit dem Sie handeln möchten.
2. Konfigurieren Sie den Kerzenzeitrahmen und passen Sie bei Bedarf die stochastischen/fraktalen Einstellungen an.
3. Bieten Sie realistische Stop-Loss-/Take-Profit-Offsets, die der Tick-Größe des Instruments entsprechen.
4. Starten Sie die Strategie in Designer, Runner oder über API und überwachen Sie die Protokollmeldungen auf Signalinformationen.
