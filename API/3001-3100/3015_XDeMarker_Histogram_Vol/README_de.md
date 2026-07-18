# XDeMarker Histogramm-Volumen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie reproduziert den ursprünglichen MetaTrader-Experten-Advisor **Exp_XDeMarker_Histogram_Vol** auf der StockSharp-High-Level-API. Sie transformiert den DeMarker-Oszillator in ein volumengewichtetes Histogramm, glättet sowohl den Oszillator als auch das Volumen mit konfigurierbaren gleitenden Durchschnitten und reagiert auf Regimewechsel, wenn das Histogramm vordefinierte Bänder kreuzt.

Die Logik ist bewusst symmetrisch. Long-Positionen werden eröffnet, wenn das Histogramm in eine der bullischen Zonen tritt, während Shorts eröffnet werden, wenn es in bärische Zonen wechselt. Entgegengesetzte Signale schließen die aktive Position und kehren, wenn aktiviert, sofort die Richtung um.

## Konzept

1. **Volumengewichteter DeMarker**
   - DeMarker wird mit dem ausgewählten Zeitraum berechnet.
   - Der Oszillator wird auf den Bereich `[-50; +50]` skaliert und mit dem gewählten Kerzenvolumen multipliziert.
   - Ein gleitender Durchschnitt glättet den gewichteten Oszillator. Derselbe gleitende Durchschnitt wird auf das Volumen selbst angewendet. Es werden nur vier Typen von gleitenden Durchschnitten bereitgestellt (einfach, exponentiell, geglättet, gewichtet), da diese nativ in StockSharp verfügbar sind.
2. **Dynamische Niveaus**
   - Vier benutzerdefinierte Multiplikatoren (`HighLevel1`, `HighLevel2`, `LowLevel1`, `LowLevel2`) definieren die bullischen und bärischen Schwellenwerte.
   - Schwellenwerte werden durch das geglättete Volumen skaliert, sodass höhere Beteiligung den akzeptablen Bereich erweitert.
3. **Zustandsmaschine**
   - Jede abgeschlossene Kerze wird in einen von fünf Zuständen klassifiziert: `0` (extrem bullisch), `1` (bullisch), `2` (neutral), `3` (bärisch), `4` (extrem bärisch).
   - Signale werden generiert, wenn der Zustand der zuletzt geschlossenen Kerze (versetzt durch `SignalBar`) sich vom vorherigen Zustand so unterscheidet, dass er einen Übergang in bullisches oder bärisches Territorium anzeigt.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `CandleType` | Primärer Zeitrahmen. Standardmäßig 2-Stunden-Kerzen, um den ursprünglichen Experten-Advisor zu spiegeln. |
| `DeMarkerPeriod` | Periode des DeMarker-Oszillators. |
| `HighLevel1` / `HighLevel2` | Positive Multiplikatoren, die den ersten und zweiten bullischen Schwellenwert definieren. |
| `LowLevel1` / `LowLevel2` | Negative Multiplikatoren, die den ersten und zweiten bärischen Schwellenwert definieren. |
| `Smoothing` | Gleitender Durchschnittstyp für Histogramm und Volumen. Auswahl: Simple, Exponential, Smoothed, Weighted. |
| `SmoothingLength` | Länge der Glättungsdurchschnitte. |
| `SignalBar` | Anzahl geschlossener Balken für den Signalvergleich. `1` bedeutet „die zuletzt geschlossene Kerze verwenden". |
| `VolumeType` | Volumenquelle. Beide Optionen greifen auf das Kerzenvolumen zurück, da StockSharp nicht auf allen Feeds Tick-Counts anzeigt. |
| `EnableLongEntries` / `EnableShortEntries` | Eröffnung neuer Positionen in der jeweiligen Richtung erlauben. |
| `EnableLongExits` / `EnableShortExits` | Schließen bestehender Positionen erlauben, wenn das entgegengesetzte Setup erscheint. |

## Signale und Positionsverwaltung

- **Long einsteigen**: Der letzte Signalbalken wechselt in Zustand `1` oder `0`, während der vorherige Balken in einem höher nummerierten Zustand (>1) war. Short-Positionen werden optional vor dem Einstieg geschlossen.
- **Short einsteigen**: Der letzte Signalbalken wechselt in Zustand `3` oder `4`, während der vorherige Balken in einem niedriger nummerierten Zustand (<3 oder <4 entsprechend) war. Long-Positionen werden optional vor dem Einstieg geschlossen.
- **Ausstieg**: Wann immer ein entgegengesetztes Signal ausgelöst wird und Ausstiege für die aktuelle Richtung aktiviert sind. `ClosePosition()` wird verwendet, um vor dem Umkehren zu glätten.
- **Positionsgröße**: Die Strategie stützt sich auf die Standard-Eigenschaft `Strategy.Volume`. Die Money-Management-Blöcke aus der MetaTrader-Version (zwei separate „Magic"-IDs) werden absichtlich vereinfacht.

## Implementierungshinweise

- Nur abgeschlossene Kerzen werden verarbeitet. Die Strategie abonniert den konfigurierten Zeitrahmen über `SubscribeCandles().WhenNew(ProcessCandle)`.
- Die DeMarker-Implementierung hält gleitende Summen von DeMax/DeMin-Werten, um MetaTrader-Berechnungen zu entsprechen, und wartet, bis genügend Balken gesammelt wurden, bevor Signale ausgegeben werden.
- Wenn Volumendaten fehlen, degradiert das Histogramm gracefully auf null, weil sowohl der gewichtete Oszillator als auch die Schwellenwerte null sind.
- Nicht unterstützte Glättungsmodi des ursprünglichen Indikators (JJMA, JurX, ParMA, T3, VIDYA, AMA) werden nicht reproduziert. Wählen Sie die nächste Alternative über den Parameter `Smoothing`.
- Der `SignalBar`-Puffer behält nur den minimal notwendigen Verlauf (aktuell, vorherig und ein extra Slot), um das ursprüngliche `CopyBuffer`-Verhalten nachzuahmen und veraltete Signale zu vermeiden.

## Verwendungstipps

- Die Strategie im Designer oder Runner starten, nachdem der gewünschte Zeitrahmen und das Volumen konfiguriert wurden.
- `DeMarkerPeriod`, `SmoothingLength` und die Schwellenmultiplikatoren gemeinsam optimieren — kleine Änderungen an Schwellenwerten ändern die Einstiegshäufigkeit wesentlich.
- Da das Histogramm volumengewichtet ist, ist die Feed-Qualität wichtig. Verwenden Sie Datenanbieter, die zuverlässiges Kerzenvolumen melden, um den beabsichtigten Effekt zu erfassen.
- Erwägen Sie das Hinzufügen externer Money-Management- oder Risikomodule, wenn Sie Stop-Loss- oder Take-Profit-Regeln benötigen; diese waren in der High-Level-Konvertierung nicht vorhanden.
