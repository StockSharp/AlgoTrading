# Advanced EA Panel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein StockSharp-Port des **Advanced EA Panel**-Dienstprogramms aus MQL5. Der ursprüngliche Expertenberater stellte ein manuelles Trading-Dashboard mit Multi-Zeitrahmen-Analysen, Pivot-Verwaltung und Schnellhandelsschaltflächen bereit. Die C#-Implementierung recreiert diese analytischen Fähigkeiten innerhalb einer automatisierten Strategie, damit sie ohne ein On-Chart-Bedienpanel verfügbar bleiben.

## Hauptmerkmale

- Aggregiert neun Zeitrahmen (M1 … MN1) und verfolgt EMA(3/6/9)-, SMA(50/200)-, CCI(14)- und RSI(21)-Stimmen für jeden Horizont.
- Berechnet Floor-Trader-, Woodie- oder Camarilla-Pivot-Niveaus auf einer konfigurierbaren Kerzenserie.
- Überwacht die Volatilität mit einem ATR-Feed und protokolliert jede bedeutende Änderung.
- Führt ein internes Risikopanel durch Berechnung von Stop-Abstand, Reward-Abstand und Live-Risiko/Reward-Verhältnis für die aktive Position.
- Unterstützt automatische Orderausführung, wenn die Multi-Zeitrahmen-Stimme einen konfigurierbaren Schwellenwert überschreitet. Gegenläufige Trades werden vor der Umkehr geschlossen, genau wie beim Drücken der Panel-Schaltflächen.
- Nutzt `StartProtection`, damit Stop-Loss- und Take-Profit-Wächter Neustarts überleben und die Schutzlogik des ursprünglichen Panels widerspiegeln.

## Handelslogik

1. Jedes Zeitrahmen-Abonnement produziert Indikatorwerte für EMA(3/6/9), SMA(50/200), CCI(14) und RSI(21). Eine bullische Stimme wird hinzugefügt, wenn der Schlusskurs über den gleitenden Durchschnitten liegt, CCI über +100 ist und RSI über 60 liegt. Bärische Stimmen werden für entgegengesetzte Bedingungen erzeugt. Neutrale Werte tragen nicht zur Punktzahl bei.
2. Die Gesamtpunktzahl über bereite Zeitrahmen wird mit `DirectionalThreshold` verglichen. Punkte ≥ Schwellenwert erzeugen ein **Kauf**-Signal; Punkte ≤ –Schwellenwert erzeugen ein **Verkauf**-Signal.
3. Wenn der automatische Handel aktiviert ist, wird die Strategie:
   - Die entgegengesetzte Position mit `ClosePosition()` schließen, bevor die Umkehrorder gesendet wird.
   - Eine Marktorder senden, die nach `Volume` dimensioniert und auf den nächsten `Security.VolumeStep` gerundet ist.
   - Sich auf `StartProtection` stützen, um Stop-Loss/Take-Profit-Brackets in Pips anzufügen.
4. ATR aus der primären Kerzenserie wird protokolliert. Jede Änderung über die Rundungspräzision hinaus gibt einen neuen Volatilitätsbericht aus.
5. Pivot-Niveaus werden neu berechnet, wenn der Pivot-Zeitrahmen eine abgeschlossene Kerze produziert. Das Protokoll zeigt PP, R1–R4 und S1–S4, damit sie als diskretionäre Niveaus oder für Dashboards verwendet werden können.

## Parameter

| Name | Beschreibung | Gruppe | Standard |
| --- | --- | --- | --- |
| `Volume` | Handelsvolumen in Lots. Vor dem Senden von Orders auf `VolumeStep` gerundet. | Handel | 1.0 |
| `StopLossPips` | Abstand vom Einstieg zum Stop-Loss in Preisschritten. `0` deaktiviert den Stop. | Risiko | 50 |
| `TakeProfitPips` | Abstand vom Einstieg zum Take-Profit in Preisschritten. `0` deaktiviert den Take. | Risiko | 100 |
| `VolatilityPeriod` | ATR-Lookback-Länge für die Volatilitätsprotokollierung. | Volatilität | 14 |
| `PrimaryCandleType` | Kerzentyp für ATR-Berechnungen und Chart-Zeichnung. | Allgemein | 15-Minuten-Kerzen |
| `PivotCandleType` | Kerzentyp für die Pivot-Neuberechnung. | Allgemein | 1-Stunden-Kerzen |
| `DirectionalThreshold` | Absoluter Score zum Auslösen eines Kauf/Verkauf-Signals. | Signale | 3 |
| `AutoTradingEnabled` | Aktiviert die automatische Ausführung erkannter Signale. | Signale | true |
| `PivotFormula` | Pivot-Preset (`Classic`, `Woodie`, `Camarilla`). | Allgemein | Classic |

## Risikomanagement

- `StartProtection` fügt preisbasierte Brackets an, berechnet aus `StopLossPips` und `TakeProfitPips` (in absoluten Preis über `PriceStep` umgerechnet).
- `_entryPrice`, `_stopPrice` und `_takePrice` werden bei Fills aktualisiert, damit die Strategie Risiko, Reward und Risiko/Reward-Verhältnis in Pips protokollieren kann.
- Wenn der automatische Handel deaktiviert ist, arbeitet der Risikomonitor weiterhin für manuell außerhalb der Strategie ausgeführte Eintritte.

## Unterschiede zum MQL5-Panel

- Der ursprüngliche EA zeigte Schaltflächen und ziehbare Linien auf dem Chart; die StockSharp-Version exponiert dieselbe Analyse über Protokolle und Strategieparameter. Alle Kommentare im Code erklären, wie die Ergebnisse in eine UI erweitert oder eingehakt werden können.
- Das Positionsmanagement ist automatisiert. Klicken auf **Kaufen**, **Verkaufen**, **Umkehren** oder **Schließen** wird durch `RequestExecution`, `SendOrder` und `ClosePosition()` in Reaktion auf den Multi-Zeitrahmen-Score ersetzt.
- Points of Interest, manuelle Tab-Bearbeitungen und Chart-Objektmanipulation sind nicht portiert. Stattdessen werden Pivots programmatisch neu berechnet und protokolliert. Trader können das Protokoll nutzen oder die Strategie um Chart-Objekte erweitern.
- Volatilität, Risikometriken und Pivots bleiben über Neustarts hinaus erhalten, da sie aus Live-Daten statt aus Chart-Objekten neu berechnet werden.

## Nutzungshinweise

1. Hängen Sie die Strategie an ein Symbol und stellen Sie sicher, dass der Connector alle in `PanelTimeFrames` aufgeführten Kerzentypen liefert. Fehlende Daten verzögern die Signalerstellung, bis mindestens eine Kerze pro Zeitrahmen abgeschlossen ist.
2. Passen Sie `DirectionalThreshold` an, um die Empfindlichkeit zu steuern. Höhere Schwellenwerte erfordern mehr Übereinstimmung über Zeitrahmen vor dem Handeln.
3. Setzen Sie `AutoTradingEnabled = false`, um das Modul als Informations-Dashboard zu nutzen und Orders manuell von einem anderen Tool zu platzieren.
4. Die Klasse fügt Standard-Chart-Rendering für primäre Kerzen, ATR und eigene Trades hinzu. Entfernen oder erweitern Sie diese Aufrufe wenn eine benutzerdefinierte Visualisierung erforderlich ist.

## Konvertierungszusammenfassung

- **UI-Aktionen → Strategiemethoden.** Panel-Button-Handler (`EAPanelClickHandler`, `T0ClickHandler` usw.) werden auf Order-Ausführungshelfer abgebildet, die den Kauf/Verkauf/Umkehren/Schließen-Fluss bewahren.
- **Pivot-Formeln.** Die MQL5-Spinner erlaubten unabhängige Formeln pro Niveau; dieser Port behält die Preset-Kombinationen (`Classic`, `Woodie`, `Camarilla`) bei, die das Panel über seine Schnellauswahl-Schaltflächen anbot.
- **Indikator-Tracking.** Native MQL5-Indikator-Handles werden durch `ExponentialMovingAverage`, `SimpleMovingAverage`, `CommodityChannelIndex` und `RelativeStrengthIndex` aus StockSharp mit `Bind`-Callbacks ersetzt.
- **Risikopanel.** Alle Risiko/Reward-Berechnungen, die früher in Editfeldern gerendert wurden, werden jetzt protokolliert und können von jeder Überwachungskomponente verbraucht werden.

Die Strategie bewahrt daher die Intention des Advanced EA Panel—zentralisiertes situatives Bewusstsein mit schneller Reaktionslogik—und präsentiert sich als vollständig automatisierte StockSharp-Strategie, die für Optimierung oder diskretionäre Überwachung bereit ist.
