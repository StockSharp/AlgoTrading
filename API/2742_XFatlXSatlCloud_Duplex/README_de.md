# XFatlXSatlCloud Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
XFatlXSatlCloud Duplex ist eine bidirektionale Strategie, konvertiert aus dem ursprünglichen MQL5 Expert Advisor. Sie handelt Kreuzungen des XFatlXSatlCloud-Indikators, der einen schnellen digitalen FATL-Filter mit einem langsameren SATL-Filter verbindet und dann beide mit konfigurierbaren gleitenden Durchschnitten glättet. Getrennte Konfigurationen können auf Long- und Short-Seiten angewendet werden, einschließlich verschiedener Zeitrahmen, Glättungsmethoden und angewandter Preisquellen.

## Handelslogik
Die Strategie bewertet nur abgeschlossene Kerzen. Zwei unabhängige Abonnements laufen parallel: eines steuert die Long-Logik und das andere die Short-Logik. Jedes Abonnement speist den in C# implementierten XFatlXSatlCloud-Indikator und produziert das folgende Verhalten:

- **Long-Einstieg** – ausgelöst, wenn die schnelle Linie am durch `LongSignalBar` definierten Balken die langsame Linie von unten nach oben kreuzt. Wenn eine Short-Position offen ist, wird sie zuerst geschlossen (nur wenn `ShortAllowClose` aktiviert ist). Dann wird eine Market-Kauforder mit `LongVolume` Kontrakten gesendet und der Einstiegspreis für Risikoprüfungen aufgezeichnet.
- **Long-Ausstieg** – ausgeführt, wenn die schnelle Linie am verschobenen Balken unter die langsame Linie fällt. Optionale preisbasierte Stop-Loss- und Take-Profit-Checks (`LongStopLoss`, `LongTakeProfit`) können die Position früher schließen, wenn der Kerzenbereich die definierten Offsets verletzt.
- **Short-Einstieg** – ausgelöst, wenn die schnelle Linie am durch `ShortSignalBar` definierten Balken die langsame Linie von oben nach unten kreuzt. Bestehende Long-Exposition wird zuerst abgebaut, wenn `LongAllowClose` aktiviert ist. Danach wird eine Market-Verkaufsorder mit `ShortVolume` Kontrakten gesendet.
- **Short-Ausstieg** – ausgeführt, wenn die schnelle Linie am verschobenen Balken über die langsame Linie steigt. Optionale Risikokontrollen (`ShortStopLoss`, `ShortTakeProfit`) überwachen intrabar Extreme.

Alle Indikatorwerte werden nur bei abgeschlossenen Kerzen berechnet, sodass jede Entscheidung auf finalen Daten basiert und das ursprüngliche MQL-Verhalten widerspiegelt.

## Risikomanagement
Die Strategie verfolgt den letzten Einstiegspreis separat für Long- und Short-Positionen. Wenn ein Stop-Loss- oder Take-Profit-Offset angegeben ist und die aktuelle Kerze den entsprechenden Schwellenwert überschreitet, wird die Position sofort geschlossen (abhängig vom relevanten `AllowClose`-Flag). Offsets werden in absoluten Preiseinheiten des gehandelten Instruments gemessen.

## Parameter
| Gruppe | Name | Beschreibung |
| --- | --- | --- |
| Trading | `LongVolume` | Ordergröße für Long-Einstiege (größer als null). |
| Trading | `ShortVolume` | Ordergröße für Short-Einstiege (größer als null). |
| Trading | `LongAllowOpen` | Öffnen neuer Long-Positionen aktivieren oder deaktivieren. |
| Trading | `LongAllowClose` | Long-Ausstiege aktivieren oder deaktivieren (für Stops und Kreuzungsausstiege benötigt). |
| Trading | `ShortAllowOpen` | Öffnen neuer Short-Positionen aktivieren oder deaktivieren. |
| Trading | `ShortAllowClose` | Short-Ausstiege aktivieren oder deaktivieren. |
| Signals | `LongSignalBar` | Anzahl abgeschlossener Balken, die beim Prüfen des Crossovers für Longs zurückgeblickt werden. |
| Signals | `ShortSignalBar` | Anzahl abgeschlossener Balken, die beim Prüfen des Crossovers für Shorts zurückgeblickt werden. |
| Data | `LongCandleType` | Kerzentyp (Zeitrahmen) für das Long-Indikator-Abonnement. |
| Data | `ShortCandleType` | Kerzentyp für das Short-Indikator-Abonnement. |
| Indicators | `LongMethod1` | Glättungsmethode für die FATL-Ausgabe auf der Long-Seite. Unterstützte Werte: SMA, EMA, SMMA, LWMA, Jurik, ZeroLag, Kaufman. |
| Indicators | `LongLength1` | Länge des schnellen Long-Glätters. |
| Indicators | `LongPhase1` | Phasenparameter für den schnellen Glätter (aus Kompatibilitätsgründen beibehalten, nur Jurik verwendet es konzeptionell). |
| Indicators | `LongMethod2` | Glättungsmethode für die SATL-Ausgabe auf der Long-Seite (gleicher unterstützter Satz wie oben). |
| Indicators | `LongLength2` | Länge des langsamen Long-Glätters. |
| Indicators | `LongPhase2` | Phasenparameter für den langsamen Long-Glätter. |
| Indicators | `LongAppliedPrice` | Angewandter Preis für den Long-Indikator (Schluss, Eröffnung, Median, typisch, gewichtet, einfach, Quartal, Trendfolgend oder Demark). |
| Indicators | `ShortMethod1` | Glättungsmethode für die schnelle Short-Linie. |
| Indicators | `ShortLength1` | Länge des schnellen Short-Glätters. |
| Indicators | `ShortPhase1` | Phasenparameter für den schnellen Short-Glätter. |
| Indicators | `ShortMethod2` | Glättungsmethode für die langsame Short-Linie. |
| Indicators | `ShortLength2` | Länge des langsamen Short-Glätters. |
| Indicators | `ShortPhase2` | Phasenparameter für den langsamen Short-Glätter. |
| Indicators | `ShortAppliedPrice` | Angewandter Preis für den Short-Indikator. |
| Risk | `LongStopLoss` | Absolute Preisdistanz für den Long-Stop-Loss (0 deaktiviert die Prüfung). |
| Risk | `LongTakeProfit` | Absolute Preisdistanz für den Long-Take-Profit (0 deaktiviert die Prüfung). |
| Risk | `ShortStopLoss` | Absolute Preisdistanz für den Short-Stop-Loss (0 deaktiviert die Prüfung). |
| Risk | `ShortTakeProfit` | Absolute Preisdistanz für den Short-Take-Profit (0 deaktiviert die Prüfung). |

## Implementierungshinweise
- Der XFatlXSatlCloud-Indikator ist als High-Level-StockSharp-Indikator implementiert. Die schnellen und langsamen Komponenten werden durch Anwendung der ursprünglichen FATL/SATL-Finite-Impulse-Response-Koeffizienten gefolgt von benutzerausgewählten Glättungsindikatoren erzeugt.
- Nur allgemein verfügbare StockSharp-gleitende Durchschnitte werden exponiert (`Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`, `ZeroLag`, `Kaufman`). Andere MQL-Glättungsfamilien (wie Parabolic oder T3) sind nicht enthalten.
- `LongSignalBar` und `ShortSignalBar` imitieren den ursprünglichen `SignalBar`-Parameter. Ein Wert von 1 bedeutet "vorherigen abgeschlossenen Balken verwenden" bei der Crossover-Erkennung.
- Stop-Loss- und Take-Profit-Offsets erwarten absolute Preisdistanzen. Sie werden mit dem Kerzenhoch/-tief relativ zum aufgezeichneten Einstiegspreis angewendet und hängen nicht von broker-spezifischen Punkt-Werten ab.
