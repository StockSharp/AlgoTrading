# Strategie TenKijun Cross Alert Strategy (ID 3562)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-High-Level-API-Portierung des MetaTrader-Expertenberaters **TenKijun.mq4**. Der ursprüngliche EA beobachtet nur den Ichimoku-Indikator und sendet Push-Benachrichtigungen, wenn der Tenkan-sen (Umrechnungslinie) den Kijun-sen (Basislinie) überschreitet. Die C#-Version behält den reinen Alarmcharakter bei, erweitert die Implementierung jedoch um StockSharp-Infrastruktur, Diagrammbindungen, Parametrisierung und sichere Sitzungsverarbeitung.

Die Logik funktioniert bei abgeschlossenen Kerzen eines konfigurierbaren Zeitrahmens. Wenn eine neue Kerze innerhalb der aktiven Handelszeiten schließt, wertet die Strategie den mit den klassischen 9/26/52-Perioden berechneten Ichimoku-Indikator aus und zeichnet die neuesten Tenkan/Kijun-Werte auf. Wenn der Tenkan den Kijun überschreitet, wird eine Informationsmeldung protokolliert, die auf ein bullisches Kreuz hinweist; Wenn Tenkan Kijun unterschreitet, wird eine bärische Warnung protokolliert. Es werden keine Trades ausgeführt – die Strategie ist zur Signalgenerierung oder zur Kombination mit externer Automatisierung gedacht.

## Indikator und Datenfluss

- **Indikator** – StockSharp `Ichimoku`-Indikator mit separat parametrisierten Tenkan-, Kijun- und Senkou-Span-B-Längen. Für die Entscheidungsfindung werden nur die Tenkan- und Kijun-Linien verwendet, die das ursprüngliche EA widerspiegeln.
- **Datenabonnement** – Verwendet `SubscribeCandles` mit einem konfigurierbaren `CandleType`. Standardmäßig werden 30-Minuten-Zeitrahmenkerzen angefordert.
- **Bindung** – `BindEx` wird verwendet, damit das typisierte `IchimokuValue` ohne manuelle Aufrufe von `GetValue` an den Handler übermittelt wird.
- **Charting** – Kerzen und der Indikator Ichimoku werden automatisch an das Strategiediagramm angehängt, um eine schnelle visuelle Validierung von Warnungen zu ermöglichen.

## Handelssitzungsfilter

Das MetaTrader-Skript beschränkte Warnungen auf ein benutzerdefiniertes Sitzungsfenster. Der Port stellt dieselbe Funktion über zwei Parameter bereit:

- `StartHour` – inklusive Beginn des aktiven Fensters (Standard 0). Akzeptiert 0-23.
- `LastHour` – inklusive Ende des aktiven Fensters (Standard 20). Akzeptiert 0-23.

Wenn `StartHour` kleiner oder gleich `LastHour` ist, werden zwischen diesen beiden Stunden des Tages Warnungen erzeugt. Wenn der Anfang länger als das Ende liegt, wird das Fenster als Nachtfenster behandelt (z. B. 20 → 6 deckt die Sitzung vom späten Abend bis zum frühen Morgen ab).

## Parameter

| Parameter | Beschreibung | Standard | Notizen |
|-----------|-------------|---------|-------|
| `StartHour` | Stunde, in der Warnungen beginnen können. | 0 | Inklusive, Bereich 0–23. |
| `LastHour` | Stunde, in der die Warnungen enden. | 20 | Inklusive, Bereich 0–23. |
| `TenkanPeriod` | Rückblick auf die Conversion-Linie. | 9 | Optimierbar. |
| `KijunPeriod` | Rückblick auf die Grundlinie. | 26 | Optimierbar. |
| `SenkouSpanBPeriod` | Führender Span-B-Lookback. | 52 | Der Vollständigkeit halber bereitgestellt, auch wenn Warnungen nicht von der Cloud abhängen. |
| `CandleType` | Für den Indikator verwendete Kerzenserie. | 30-minütiger Zeitrahmen | Wählen Sie einen beliebigen `TimeSpan`-basierten Zeitrahmen. |

## Alarmlogik

1. Warten Sie auf die erste fertige Kerze, um die Tenkan- und Kijun-Geschichte zu initialisieren.
2. Bei jeder weiteren fertigen Kerze innerhalb des Handelsfensters:
   - Extrahieren Sie Tenkan- und Kijun-Werte aus dem Indikator Ichimoku.
   - Erkennen Sie einen bullischen Cross, wenn der vorherige Tenkan kleiner oder gleich dem vorherigen Kijun war und der aktuelle Tenkan größer als der aktuelle Kijun ist.
   - Erkennen Sie einen rückläufigen Cross, wenn der vorherige Tenkan größer oder gleich dem vorherigen Kijun war und der aktuelle Tenkan kleiner als der aktuelle Kijun ist.
   - Geben Sie einen informativen Protokolleintrag aus, der die Richtung, den Preis und den Zeitstempel des Kreuzes beschreibt.

## Nutzungstipps

- Kombinieren Sie diese Strategie mit StockSharp-Benachrichtigungsadaptern (E-Mail, Telegram, Sound), indem Sie das Strategieprotokoll abonnieren oder die `ProcessCandle`-Methode mit benutzerdefiniertem Benachrichtigungscode erweitern.
- Um den automatisierten Handel voranzutreiben, erben Sie von `TenKijunCrossStrategy` und überschreiben Sie `ProcessCandle`, um Aufträge zu erteilen, anstatt oder zusätzlich zur Protokollierung von Nachrichten.
- Passen Sie den Zeitrahmen der Kerze so an, dass er mit dem ursprünglichen MetaTrader-Diagramm übereinstimmt, das von EA verwendet wurde, um die Benachrichtigungen aufeinander abzustimmen.

## Unterschiede zum Original EA

- Verwendet StockSharp-Protokollierung anstelle von MetaTrader `SendNotification`. Das Verhalten dient weiterhin nur der Warnung, hängt jedoch von der Nachrichtenpipeline der Plattform ab.
- Fügt vollständige Parametermetadaten (`SetDisplay`, Bereiche, Optimierungsflags) hinzu und macht die Strategie für Designer-/Optimierungstools bereit.
- Zeichnet automatisch Kerzen und den Indikator Ichimoku im Diagrammfenster StockSharp, sofern verfügbar.

## Dateien

- `CS/TenKijunCrossStrategy.cs` – Haupt-C#-Implementierung der Alarmlogik.
