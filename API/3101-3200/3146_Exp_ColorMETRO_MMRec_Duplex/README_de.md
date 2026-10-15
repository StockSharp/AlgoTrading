# Exp ColorMETRO MMRec Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie portiert den MetaTrader 5-Expertenberater `Exp_ColorMETRO_MMRec_Duplex` zu StockSharp. Der originale Roboter führt zwei unabhängige ColorMETRO-Indikatormodule (eines für Long, eines für Short) aus und wendet eine MMRec-Überlagerung (Geldverwaltungs-Neuberechnung) an, die die Positionsgröße nach wiederholten Verlusten reduziert. Die C#-Version spiegelt dieses Verhalten mit StockSharpss High-Level-API für Kerzenabonnements und Order-Routing wider.

## Handelslogik
- Zwei verschiedene ColorMETRO-Indikatoren operieren auf konfigurierbaren Kerzentypen. Das Long-Modul verwaltet nur Long-Exposition, während das Short-Modul die Short-Exposition kontrolliert.
- Jeder Indikator erzeugt ein schnelles und ein langsames RSI-Stufenband. Die Strategie imitiert die MQL5-`CopyBuffer`-Aufrufe, indem sie historische Werte speichert und die durch `SignalBar` definierte Barre inspiziert.
- Ein Long-Einstieg wird generiert, wenn das schnelle Band auf der inspizierten Barre die langsame Band **von unten** kreuzt, während die vorherige Barre das schnelle Band noch über dem langsamen hatte. Alle offenen Short-Positionen werden vor der neuen Long-Position abgeflacht.
- Long-Ausstiege erfolgen, wenn das langsame Band auf der vorherigen inspizierten Barre über dem schnellen Band sitzt, was ein bärisches Regime im originalen EA signalisiert.
- Short-Einstiege und -Ausstiege spiegeln die Long-Logik wider (Kreuzen nach oben für Einstiege, schnelle Linie über der langsamen auf der vorherigen Barre für Ausstiege).
- Nur fertige Kerzen werden verarbeitet und der Handel wird blockiert, bis der Indikator beide Bänder als bereit meldet, was die MetaTrader-Aufwärmphase reproduziert.

## Geldverwaltung (MMRec)
- `Strategy.Volume` definiert die Referenz-Lotgröße. Die Long- und Short-Module multiplizieren sie mit ihren jeweiligen `LongMm`/`ShortMm`-Koeffizienten bei der Dimensionierung neuer Orders.
- Nach jeder abgeschlossenen Trade zeichnet die Strategie auf, ob das Ergebnis ein Verlust war (basierend auf Kerzenschlusspreisen, genauso wie der EA, der historische Deals inspiziert).
- Wenn die letzten `TotalTrigger`-Trades eines Moduls mindestens `LossTrigger`-Verlierer enthalten, wechselt das Modul zum reduzierten Multiplikator (`SmallMm`). Sobald die Verlustanzahl unter den Schwellenwert fällt, wird der Standard-Multiplikator automatisch wiederhergestellt.
- Positionsumkehrungen finalisieren zuerst das Ergebnis des bestehenden Trades (Aktualisierung der MMRec-Zähler), bevor die entgegengesetzte Richtung dimensioniert und eröffnet wird.

## Indikatorhinweise
- `ColorMetroMmrecIndicator` ist ein treuer Port des benutzerdefinierten `ColorMETRO`-Indikators. Er speist dieselben schnellen/langsamen Bänder, die von einem RSI-Kern mit Schrittverfolgung und Trend-Gedächtnis angetrieben werden.
- Der Indikator legt den internen RSI und ein Bereitschafts-Flag frei, sodass die Strategie unvollständige Werte genau wie die MQL-Implementierung ignorieren kann.

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Long | `LongCandleType` | Kerzentyp für das Long-ColorMETRO-Modul. |
| Long | `LongTotalTrigger` | Anzahl abgeschlossener Long-Trades, die bei der MMRec-Auswertung inspiziert werden. |
| Long | `LongLossTrigger` | Verlustanzahl, die den reduzierten Long-Multiplikator aktiviert. |
| Long | `LongSmallMm` | Reduzierter Multiplikator für Long-Trades nach einer Verlustserie. |
| Long | `LongMm` | Standard-Multiplikator für Long-Trades. |
| Long | `LongEnableOpen` | Öffnen von Long-Positionen aktivieren. |
| Long | `LongEnableClose` | Schließen von Long-Positionen aktivieren. |
| Long | `LongPeriodRsi` | RSI-Länge im Long-ColorMETRO-Indikator. |
| Long | `LongStepSizeFast` | Schnelle Band-Schrittgröße für das Long-Modul. |
| Long | `LongStepSizeSlow` | Langsame Band-Schrittgröße für das Long-Modul. |
| Long | `LongSignalBar` | Historischer Versatz (in geschlossenen Barren) beim Lesen von Indikatorwerten. |
| Long | `LongMagic` | Originale MT5-Magicnummer, als Referenz beibehalten. |
| Long | `LongStopLossTicks` | Stop-Loss-Abstand-Platzhalter aus dem EA (nicht durchgesetzt). |
| Long | `LongTakeProfitTicks` | Take-Profit-Abstand-Platzhalter aus dem EA (nicht durchgesetzt). |
| Long | `LongDeviationTicks` | Erlaubter Slippage-Platzhalter aus dem EA (nicht durchgesetzt). |
| Long | `LongMarginMode` | MM-Modus-Flag für Kompatibilität beibehalten (Logik verwendet rohe Multiplikatoren). |
| Short | `ShortCandleType` | Kerzentyp für das Short-ColorMETRO-Modul. |
| Short | `ShortTotalTrigger` | Anzahl abgeschlossener Short-Trades, die bei der MMRec-Auswertung inspiziert werden. |
| Short | `ShortLossTrigger` | Verlustanzahl, die den reduzierten Short-Multiplikator aktiviert. |
| Short | `ShortSmallMm` | Reduzierter Multiplikator für Short-Trades nach einer Verlustserie. |
| Short | `ShortMm` | Standard-Multiplikator für Short-Trades. |
| Short | `ShortEnableOpen` | Öffnen von Short-Positionen aktivieren. |
| Short | `ShortEnableClose` | Schließen von Short-Positionen aktivieren. |
| Short | `ShortPeriodRsi` | RSI-Länge im Short-ColorMETRO-Indikator. |
| Short | `ShortStepSizeFast` | Schnelle Band-Schrittgröße für das Short-Modul. |
| Short | `ShortStepSizeSlow` | Langsame Band-Schrittgröße für das Short-Modul. |
| Short | `ShortSignalBar` | Historischer Versatz (in geschlossenen Barren) beim Lesen von Indikatorwerten. |
| Short | `ShortMagic` | Originale MT5-Magicnummer, als Referenz beibehalten. |
| Short | `ShortStopLossTicks` | Stop-Loss-Abstand-Platzhalter aus dem EA (nicht durchgesetzt). |
| Short | `ShortTakeProfitTicks` | Take-Profit-Abstand-Platzhalter aus dem EA (nicht durchgesetzt). |
| Short | `ShortDeviationTicks` | Erlaubter Slippage-Platzhalter aus dem EA (nicht durchgesetzt). |
| Short | `ShortMarginMode` | MM-Modus-Flag für Kompatibilität beibehalten (Logik verwendet rohe Multiplikatoren). |

## Implementierungshinweise
- Die Strategie verlässt sich auf `SubscribeCandles(...).BindEx(...)` und vermeidet direkten Pufferzugriff, konform mit den Konvertierungsrichtlinien.
- Schutz-Stops aus dem EA bleiben nur als Parameter; Benutzer können `StartProtection` oder benutzerdefinierte Risikomodule bei Bedarf anhängen.
- Beide Module teilen dieselbe Sicherheitsinstanz, behalten aber ihre eigenen Kerzenabonnements und MMRec-Zähler, entsprechend dem Duplex-Layout von MetaTrader.
- Alle Code-Kommentare sind auf Englisch und die Logik vermeidet verbotene API-Aufrufe wie `GetTrades`.

## Haftungsausschluss
Dieser Port reproduziert die logische Struktur des originalen EA, aber die Ausführungsqualität hängt vom verbundenen Broker, dem Datenfeed und der StockSharp-Konfiguration ab. Validieren Sie das Verhalten immer auf historischen und Demo-Daten, bevor Sie mit echtem Kapital handeln.
