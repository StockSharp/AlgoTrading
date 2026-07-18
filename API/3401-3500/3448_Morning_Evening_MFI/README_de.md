# Morgen-/Abendstern mit MFI-Bestätigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert die Logik des MetaTrader-Experten `Expert_AMS_ES_MFI` und kombiniert Multi-Candle-Umkehrmuster mit der Momentum-Bestätigung durch den Money Flow Index (MFI). Es überwacht die Morgenstern- und Abendsternformationen mit drei Kerzen im ausgewählten Zeitrahmen und filtert die Signale mithilfe von MFI-Schwellenwerten, um die Erschöpfung des aktuellen Schwungs zu bestätigen, bevor Geschäfte getätigt werden. Durch MFI-Kreuzungen erkannte Momentumumkehrungen werden auch zum Schließen offener Positionen verwendet.

## Handelslogik
- **Datenquelle**: Fertige Kerzen des konfigurierten Zeitrahmens und ihre zugehörigen MFI-Werte.
- **Indikatoren**:
  - Money Flow Index (MFI) – Zeitraum ist konfigurierbar (Standard 49).
- **Teilnahmebedingungen**:
  - **Long**: Erkennen Sie ein Morning Star-Muster (starke bärische Kerze, mittlere Kerze mit kleinem Körper, starke bullische Kerze, die über dem Mittelpunkt der ersten schließt) und erfordern Sie, dass der MFI der vorherigen Kerze unter dem bullischen Bestätigungsschwellenwert liegt (Standard 40).
  - **Short**: Erkennen Sie ein Evening Star-Muster (starke bullische Kerze, kleine mittlere Kerze mit kleinem Körper, starke bärische Kerze, die unter dem Mittelpunkt der ersten schließt) und erfordern Sie, dass der MFI der vorherigen Kerze über dem bärischen Bestätigungsschwellenwert liegt (Standard 60).
  - Beim Umtausch von Positionen schließt die Strategie zunächst das entgegengesetzte Engagement, bevor der neue Handel eröffnet wird.
- **Ausgangsregeln**:
  - **Langer Ausstieg**: Schließen Sie die Position, wenn der MFI das obere Ausstiegsniveau überschreitet (Standard 70) oder unter das untere Ausstiegsniveau (Standard 30) fällt, was entweder ein überkauftes Momentum oder eine gescheiterte Umkehr signalisiert.
  - **Short Exit**: Schließen Sie die Position, wenn der MFI das untere Ausstiegsniveau (Standard 30) oder das obere Ausstiegsniveau (Standard 70) überschreitet, was ein wachsendes Aufwärtsmomentum signalisiert.
- **Auftragstyp**: Marktaufträge unter Verwendung des in der StockSharp-Umgebung konfigurierten Strategievolumens.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Zeitrahmen der zur Analyse verwendeten Kerzen. | 1-Stunden-Kerzen |
| `MfiPeriod` | Zeitraum des MFI-Indikators. | 49 |
| `BullishMfiThreshold` | MFI-Niveau, das die Morning Star-Signale bestätigt. | 40 |
| `BearishMfiThreshold` | MFI-Wert, der Evening Star-Signale bestätigt. | 60 |
| `UpperExitLevel` | MFI-Level, das zur Erkennung überkaufter Ausstiege verwendet wird. | 70 |
| `LowerExitLevel` | MFI-Level, das zur Erkennung überverkaufter Ausstiege verwendet wird. | 30 |

Alle Parameter können im StockSharp Designer/Optimizer optimiert werden.

## Nutzungshinweise
1. Hängen Sie die Strategie an das gewünschte Wertpapier an und stellen Sie `CandleType` so ein, dass es mit dem Zeitrahmen des Diagramms des ursprünglichen MQL-Experten übereinstimmt.
2. Konfigurieren Sie die Risikoparameter, wie z. B. das Strategievolumen oder die Broker-spezifische Ordergröße, über die StockSharp-Plattform.
3. Aktivieren Sie die Strategie. Es abonniert automatisch Kerzen, berechnet MFI-Werte und verwaltet Positionen gemäß den oben genannten Regeln.

## Herkunft
Die Strategie ist eine direkte Konvertierung des MQL5-Expertenberaters in `MQL/323`, wobei sein Muster und seine MFI-basierte Entscheidungslogik beibehalten und gleichzeitig an den StockSharp-High-Level-API angepasst werden.
