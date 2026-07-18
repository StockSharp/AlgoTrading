# BrainTrend2 + AbsolutelyNoLagLWMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie kombiniert zwei unabhängige Module, die ursprünglich in MetaTrader 5 implementiert wurden: BrainTrend2_V2 und AbsolutelyNoLagLWMA. Jedes Modul analysiert sein eigenes Kerzenabonnement und entscheidet, wann es Long, Short oder wieder flach gehen soll. Der C#-Port hält beide Entscheidungsflüsse intakt und aggregiert ihr gewünschtes Engagement in einer einzigen StockSharp-Strategie.

* **BrainTrend2-Modul.** Verwendet einen trendfolgenden Farbzustand, der vom BrainTrend2-Indikator generiert wird. Der Zustand wird von einem ATR-basierten Kanal abgeleitet, der wechselt, wenn der Preis die gegenüberliegende Grenze verletzt.
* **AbsolutelyNoLagLWMA-Modul.** Verfolgt die Steigung eines doppelt geglätteten linear gewichteten gleitenden Durchschnitts, der auf einem auswählbaren angewandten Preis berechnet wird.

Wenn eines der Module eine neue Positionsrichtung anfordert, berechnet die Strategie das kombinierte Zielvolumen neu und sendet Marktaufträge, um dieses Engagement zu erreichen. Das Standardsetup handelt auf H4-Kerzen für beide Indikatoren, aber jedes Modul kann einen anderen Zeitrahmen abonnieren.

## Indikatoren
### BrainTrend2
Der BrainTrend2-Indikator rekonstruiert die Fünf-Farben-Kerzenüberlagerung aus der ursprünglichen MQL-Datei:
* Eine triangular gewichtete True-Range-Reihe (Periodenparameter) wird durch einen Koeffizienten von 0.7 skaliert, um ein dynamisches Band (`widcha`) zu bilden.
* Ein schwimmendes Referenzniveau (`Emaxtra`) folgt den Preisextremen innerhalb des aktuellen Regimes.
* Wenn das Tief unter `Emaxtra - widcha` fällt, wechselt das Regime zu bärisch. Wenn das Hoch `Emaxtra + widcha` übersteigt, wechselt das Regime zu bullisch.
* Das resultierende Regime bestimmt die Farbe: Lime/Teal (Werte 0 oder 1) für bullische Kontexte, Kastanienbraun/Magenta (Werte 3 oder 4) für bärische Kontexte, Grau (Wert 2) bevor der Indikator bereit ist.

Der C#-Indikator behält die gleiche Mechanik bei, einschließlich der triangularen ATR-Schätzung, sodass die generierten Farben mit der MQL-Referenz übereinstimmen.

### AbsolutelyNoLagLWMA
Das AbsolutelyNoLagLWMA-Modul wendet zwei aufeinanderfolgende linear gewichtete gleitende Durchschnitte auf die ausgewählte Preisreihe an. Die Steigung der resultierenden Linie treibt die Farbwerte an:
* **2 (blau)** – Linie steigt.
* **1 (grau)** – Linie ist flach.
* **0 (violett)** – Linie fällt.

Beide Indikatoren stellen `IsFormed` bereit, sodass die Strategie wartet, bis genug Historie verfügbar ist, bevor sie auf Farben reagiert.

## Handelslogik
Die Strategie pflegt zwei interne Ziele, `_brainTrendTarget` und `_lwmaTarget`, die das gewünschte Volumen für jedes Modul darstellen. Jedes Mal, wenn eines der Module sein Ziel ändert, ruft die Strategie `RebalancePosition` auf, um die aggregierte Position auf `_brainTrendTarget + _lwmaTarget` anzupassen.

### BrainTrend2-Modul
* Bewertet die Farbe von der Kerze `SignalBar` Perioden zurück (Standard 1) und die vorherige Farbe zur Erkennung von Zustandsübergängen.
* Wenn die aktuelle Farbe bullisch ist (`< 2`) und die vorherige Farbe nicht bullisch war (`> 1`), das Modul:
  * Schließt jede Short-Exposition, die von diesem Modul erstellt wurde.
  * Öffnet eine Long-Position mit `BrainTrendVolume`, wenn Long-Einstiege aktiviert sind.
* Wenn die aktuelle Farbe bärisch ist (`> 2`) und die vorherige Farbe nicht bärisch war (`< 3`), das Modul:
  * Schließt jede ausstehende Long-Exposition.
  * Öffnet eine Short-Position mit `BrainTrendVolume`, wenn Short-Einstiege aktiviert sind.

### AbsolutelyNoLagLWMA-Modul
* Verwendet dieselbe `SignalBar`-Logik, reagiert aber auf Farbwerte 2 (aufwärts) und 0 (abwärts).
* Wenn die Farbe **2** wird und die vorherige Farbe anders war:
  * Optionales Schließen der Short-Exposition (`LwmaCloseShortAllowed`).
  * Optionales Öffnen einer Long-Position mit `LwmaVolume`, wenn `LwmaBuyAllowed` wahr ist.
* Wenn die Farbe **0** wird und die vorherige Farbe anders war:
  * Optionales Schließen der Long-Exposition (`LwmaCloseLongAllowed`).
  * Optionales Öffnen einer Short-Position mit `LwmaVolume`, wenn `LwmaSellAllowed` wahr ist.

Jedes Modul modifiziert nur sein eigenes Zielvolumen, sodass beide gleichzeitig aktiv sein können. Zum Beispiel kann das BrainTrend2-Modul long bleiben, während das LWMA-Modul Shorts um die Kernposition herum skalpt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `BrainTrendAtrPeriod` | Periode des triangularen ATR, der von BrainTrend2 verwendet wird. |
| `BrainTrendSignalBar` | Anzahl der abgeschlossenen Kerzen, die zur Verschiebung von BrainTrend2-Signalen verwendet werden. `1` bedeutet, dass die Strategie darauf wartet, dass die vorherige Barre schließt. |
| `BrainTrendBuyAllowed` / `BrainTrendSellAllowed` | Long/Short-Einstiege für das BrainTrend2-Modul aktivieren oder deaktivieren. |
| `BrainTrendVolume` | Volumen, das vom BrainTrend2-Modul beim Einstieg in eine Position platziert wird. |
| `BrainTrendCandleType` | Kerzentyp (Zeitrahmen), der vom BrainTrend2-Modul abonniert wird. |
| `LwmaLength` | Länge jedes gewichteten Durchschnitts im AbsolutelyNoLagLWMA-Indikator. |
| `LwmaSignalBar` | Signalversatz für das LWMA-Modul (gleiche Semantik wie das BrainTrend-Modul). |
| `LwmaAppliedPrice` | Angewandter Preis zum Aufbau des LWMA (Schluss, Eröffnung, Median, Demark, etc.). |
| `LwmaBuyAllowed` / `LwmaSellAllowed` | Long/Short-Einstiege für das LWMA-Modul aktivieren oder deaktivieren. |
| `LwmaCloseLongAllowed` / `LwmaCloseShortAllowed` | Dem LWMA-Modul erlauben, gegenläufige Exposition zu schließen, wenn ein Signal invertiert. |
| `LwmaVolume` | Volumen, das vom LWMA-Modul gesendet wird, wenn es einen Trade öffnet. |
| `LwmaCandleType` | Kerzentyp (Zeitrahmen), der vom LWMA-Modul abonniert wird. |

## Positionsverwaltung und Aufträge
* Die Strategie verwendet immer Marktaufträge (`BuyMarket` / `SellMarket`), um das aggregierte Zielvolumen zu erreichen.
* Volumen beider Module sind additiv. Wenn zum Beispiel jedes Modul `1` Lot in entgegengesetzten Richtungen anfordert, wird die Nettoposition null, was das Konto effektiv absichert.
* Kein automatischer Stop-Loss oder Take-Profit aus dem ursprünglichen Expert Advisor wird nachgebaut, da diese Funktionen in MQL broker-spezifisch waren. Risikokontrolle kann bei Bedarf über StockSharp-Schutzmaßnahmen hinzugefügt werden.
* Wenn beide Module verschiedene Zeitrahmen abonnieren, abonniert die Strategie automatisch beide Kerzenströme und zeichnet sie im Diagrammbereich zusammen mit Fills.

## Hinweise
* Die Implementierung hält Indikatorberechnungen in sich, sodass keine externen Indikatorbibliotheken erforderlich sind.
* `SignalBar = 0` ermöglicht sofortiges Reagieren auf die zuletzt abgeschlossene Kerze, während größere Versätze zusätzliche Bestätigung erzwingen.
* BrainTrend2 benötigt mindestens `AtrPeriod + 2` historische Kerzen, bevor es gültige Farben ausgibt; AbsolutelyNoLagLWMA benötigt mindestens `Length` Kerzen.
* Da beide Module dasselbe `Strategy.Security` teilen, werden ihre Trades durch dieselbe Portfolioverbindung abgestimmt, genau wie im ursprünglichen MT5 Expert Advisor, der verschiedene magische Nummern verwendete.

## Strategie erweitern
* StockSharp-Risikoschutzmaßnahmen hinzufügen (z.B. Trailing Stops), wenn feste Stops aus der MQL-Version erforderlich sind.
* `BrainTrendVolume` und `LwmaVolume` unabhängig anpassen, um entweder das trendfolgepnde oder das Mean-Reversion-Verhalten zu betonen.
* Die Module mit zusätzlichen Filtern kombinieren, indem die Indikatorwerte beobachtet werden, die innerhalb von `ProcessBrainTrend` und `ProcessLwma` bereitgestellt werden.
