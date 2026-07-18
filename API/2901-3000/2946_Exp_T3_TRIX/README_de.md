# Exp T3 TRIX Strategie (ID 2946)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Exp T3 TRIX Strategie repliziert den MetaTrader 5 Expertenberater, der um den dreifach geglätteten TRIX-Oszillator aufgebaut ist. Sie wendet Tillson T3-Glättung an, um einen schnellen und langsamen TRIX-Strom zu erzeugen, und reagiert auf Momentum-Umkehrungen mithilfe von drei auswählbaren Modi. Jeder Modus steuert, wie sich das Histogramm oder die relative Position der schnellen und langsamen Komponenten verhalten muss, bevor die Strategie eine Position eingeht oder verlässt.

## Handelslogik

- **Tillson T3 TRIX-Berechnung**
  - Zwei Stapel von sechs exponentiellen gleitenden Durchschnitten mit gleicher Länge erzeugen Tillson T3-Werte für einen schnellen und einen langsamen Strom.
  - Die Ableitung jedes T3-Wertes (aktuell minus vorher dividiert durch vorher) wird zum TRIX-Histogramm für die Entscheidungsfindung.
- **Modus = Breakdown**
  - *Long-Einstieg*: Schnelle TRIX kreuzt von unter null auf über null, während Long-Einstiege aktiviert sind. Jede offene Short-Position wird zuerst geschlossen (wenn Short-Ausstiege erlaubt sind).
  - *Short-Einstieg*: Schnelle TRIX kreuzt von über null auf unter null, während Short-Einstiege aktiviert sind. Jede offene Long-Position wird zuerst geschlossen (wenn Long-Ausstiege erlaubt sind).
  - *Nur Ausstieg*: Wenn eine Kreuzung auftritt, aber der entsprechende Einstieg deaktiviert ist, schließt die Strategie trotzdem die entgegengesetzte Exposition, wenn die relevante Ausstiegsgenehmigung aktiviert ist.
- **Modus = Twist**
  - *Long-Einstieg*: Die Steigung der schnellen TRIX ändert sich von negativ zu positiv (d.h. der aktuelle Balken steigt nach dem Fallen). Die Strategie spiegelt die Schließ- und Genehmigungsregeln aus dem Breakdown-Modus.
  - *Short-Einstieg*: Die Steigung der schnellen TRIX ändert sich von positiv zu negativ.
- **Modus = CloudTwist**
  - *Long-Einstieg*: Die schnelle TRIX bewegt sich über die langsame TRIX, nachdem sie auf dem vorherigen abgeschlossenen Balken darunter lag.
  - *Short-Einstieg*: Die schnelle TRIX fällt unter die langsame TRIX, nachdem sie auf dem vorherigen Balken darüber saß.
- **Order-Handling**
  - Die Strategie schließt zuerst die entgegengesetzte Exposition, wenn ein Umkehrsignal erscheint und Ausstiege erlaubt sind.
  - Neue Orders verwenden `Volume + |Position|`, sodass eine Umkehr in einem einzigen Trade ausgeführt werden kann, wenn erlaubt.
  - `StartProtection()` wird aktiviert, um die integrierte StockSharp-Sicherheitsschicht aus der ursprünglichen Projektvorlage wiederzuverwenden.

## Parameter

| Parameter | Standardwert | Beschreibung |
|-----------|--------------|--------------|
| `Fast Length` | 10 | Tiefe für den schnellen Tillson T3-Stapel (sechs verknüpfte EMAs). |
| `Slow Length` | 18 | Tiefe für den langsamen Tillson T3-Stapel. |
| `Volume Factor` | 0.7 | Tillson T3-Glättungskoeffizient (0 bis 1). |
| `Mode` | Twist | Wählt zwischen Breakdown-, Twist- oder CloudTwist-Signalerkennung. |
| `Allow Long Entry` | true | Aktiviert das Öffnen von Long-Positionen. |
| `Allow Short Entry` | true | Aktiviert das Öffnen von Short-Positionen. |
| `Allow Long Exit` | true | Aktiviert das Schließen von Long-Positionen. |
| `Allow Short Exit` | true | Aktiviert das Schließen von Short-Positionen. |
| `Candle Type` | 4-Stunden-Zeitrahmen | Aggregationsintervall für Kerzenanforderungen und Indikator-Kette. |

Alle Parameter werden durch `StrategyParam<T>` exponiert, wodurch sie in der Designer-UI sichtbar und für die Optimierung bereit sind.

## Verwendungshinweise

1. Die Logik funktioniert nur mit abgeschlossenen Kerzen. Stellen Sie sicher, dass die Datenquelle den in `Candle Type` konfigurierten Zeitrahmen liefert.
2. Da die TRIX-Ableitung historische Werte benötigt, werden die ersten zwei abgeschlossenen Kerzen für die Initialisierung verwendet und erzeugen keine Signale.
3. Um das MetaTrader-Verhalten zu replizieren, deaktivieren Sie das entsprechende `Allow ...`-Flag, wenn Sie einseitiges Trading oder Ausstiegsunterdrückung wünschen.
4. Risikomanagement wie Stop-Loss- oder Take-Profit-Level war im ursprünglichen Expertenberater nicht enthalten und wird daher hier nicht implementiert. Kombinieren Sie die Strategie bei Bedarf mit StockSharp-Geldverwaltungsmodulen.

## Konvertierungsdetails

- Quelle: `MQL/2156/exp_t3_trix.mq5` plus den Indikator `t3_trix.mq5`.
- Der API-Port implementiert dieselben drei Signalmodi unter Verwendung von StockSharp-High-Level-Kerzenabonnements und Indikatorklassen.
- Tillson T3-Glättung wird mit sechs verketteten exponentiellen gleitenden Durchschnitten und dem kanonischen 0.7-Volumenfaktor recreiert, anpassbar über `Volume Factor`.
