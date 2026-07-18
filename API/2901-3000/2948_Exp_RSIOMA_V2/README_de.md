# Exp RSIOMA V2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Exp RSIOMA V2 ist eine Konvertierung des ursprünglichen MetaTrader 5-Expertenberaters, der auf dem RSIOMA-Oszillator (Relative Strength Index of Moving Average) handelt. Die Strategie reproduziert dieselben Ideen innerhalb der StockSharp High-Level-API: Preisdaten werden geglättet, in eine Momentum-Reihe umgewandelt und in einen RSI-artigen Akkumulator gespeist. Handelsentscheidungen werden getroffen, wenn der Oszillator die Richtung ändert oder vordefinierte Zonen kreuzt.

## Handelslogik
1. **Preisvorverarbeitung** – der ausgewählte Kerzenpreis (standardmäßig Schluss) wird mit einer von vier gleitenden Durchschnittsfamilien geglättet (einfach, exponentiell, geglättet oder linear gewichtet).
2. **Momentum-Berechnung** – der geglättete Preis wird mit dem Wert aus `MomentumPeriod` Balken zuvor verglichen, um den Momentum-Impuls zu erhalten.
3. **RSIOMA-Berechnung** – positive und negative Momentum-Komponenten werden mit einer exponentiellen Glättung der Länge `RsiomaLength` akkumuliert und erzeugen den RSIOMA-Wert im Bereich `[0; 100]`.
4. **Signalauswertung** – die zuletzt geschlossenen Kerzen werden gemäß dem gewählten `Mode` inspiziert:
   - **Breakdown** – reagiert, wenn RSIOMA die Haupttrenднiveaus verlässt (`MainTrendLong` / `MainTrendShort`). Wenn der Oszillator die obere Zone verlässt, werden Shorts geschlossen und Long-Einstiege erlaubt; das Verlassen der unteren Zone führt die entgegengesetzte Aktion durch.
   - **Twist** – sucht nach Wendepunkten. Ein Kauf erfolgt, wenn die RSIOMA-Steigung von fallend zu steigend wechselt, während Verkäufe auf einen steigend-zu-fallend-Übergang reagieren.
   - **CloudTwist** – emuliert die farbige Cloud-Logik des MT5-Indikators. Trades werden eröffnet, wenn RSIOMA von überverkauften/überkauften Extremen zurück in den Kanal kehrt, und entgegengesetzte Positionen werden gleichzeitig geschlossen.

Signale werden auf dem durch `SignalBar` angegebenen Balken ausgewertet (Standard: die vorherige vollständig geschlossene Kerze), um sicherzustellen, dass nur bestätigte Daten verwendet werden.

## Parameter
| Name | Beschreibung | Standardwert |
|------|--------------|--------------|
| `OrderVolume` | Standard-Ordervolumen für Marktorders. | `1` |
| `CandleType` | Von der Strategie verarbeitete Kerzendatenreihe. | `4-Stunden`-Zeitrahmen |
| `EnableLongEntries` / `EnableShortEntries` | Öffnen neuer Long/Short-Positionen erlauben. | `true` |
| `EnableLongExits` / `EnableShortExits` | Schließen bestehender Long/Short-Positionen erlauben. | `true` |
| `Mode` | Handelslogik (Breakdown, Twist oder CloudTwist). | `Breakdown` |
| `PriceSmoothing` | Gleitender Durchschnitt, der auf den Preis vor RSIOMA angewendet wird. | `Exponential` |
| `RsiomaLength` | RSIOMA-Mittelungsperiode. | `14` |
| `MomentumPeriod` | Verzögerung zwischen Stichproben bei der Momentum-Berechnung. | `1` |
| `AppliedPrice` | Kerzenpreis für den Oszillator (Schluss, Eröffnung, Median, DeMark usw.). | `Close` |
| `MainTrendLong` / `MainTrendShort` | RSIOMA-Level, die überkaufte/überverkaufte Zonen definieren. | `60` / `40` |
| `SignalBar` | Anzahl der geschlossenen Balken zurück, die analysiert werden sollen. | `1` |

## Implementierungshinweise
- Nur die in StockSharp verfügbaren Glättungsfamilien werden unterstützt (einfach, exponentiell, geglättet und linear gewichtet). Erweiterte Modi aus der MT5-Version (JJMA, VIDYA, AMA, …) sind nicht enthalten.
- Die RSI-Durchschnitte werden mit den ersten `RsiomaLength` Momentum-Werten initialisiert, um die MetaTrader-Initialisierung widerzuspiegeln. Danach wird eine exponentielle Aktualisierung angewendet, die dem ursprünglichen Expertenberater-Verhalten entspricht.
- Positionen werden immer geschlossen, bevor ein entgegengesetzter Einstieg ausgegeben wird. Eintrittsgenehmigungen (`EnableLongEntries`, `EnableShortEntries`) und Ausstiegsgenehmigungen (`EnableLongExits`, `EnableShortExits`) bieten volle Kontrolle über die erlaubten Richtungen.
- `SignalBar = 0` kann verwendet werden, um auf die aktuelle abgeschlossene Kerze zu reagieren; höhere Werte reproduzieren die MT5-Fähigkeit, mehrere Balken abzuwarten, bevor gehandelt wird.

## Verwendung
1. Die Strategie zu einem StockSharp-Projekt hinzufügen und das zu handelnde Instrument zuweisen.
2. Das Kerzenabonnement über `CandleType` konfigurieren (Standard ist 4-Stunden-Kerzen) und Schwellenwerte anpassen, wenn das Symbol andere Volatilitätseigenschaften hat.
3. Den bevorzugten Signalmodus auswählen, je nachdem ob Ausbruchseinstiege (`Breakdown`), Momentum-Wenden (`Twist`) oder Cloud-Farbwechsel (`CloudTwist`) gewünscht werden.
4. Die Strategie starten. Während der Ausführung abonniert die Strategie die gewählte Kerzenreihe, berechnet die RSIOMA-Kette und gibt Marktorders aus, wenn Bedingungen erfüllt sind.
