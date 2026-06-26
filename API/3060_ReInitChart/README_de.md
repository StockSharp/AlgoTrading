# ReInitChart-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert das MetaTrader-Dienstprogramm **ReInitChart** auf StockSharp. Das ursprüngliche Skript erstellte auf jedem Chart eine Schaltfläche, die vorübergehend den Zeitrahmen wechselte, um die Neuberechnung der Indikatoren zu erzwingen. Die StockSharp-Version behält denselben Geist bei, indem sie einen manuellen Aktualisierungsschalter und optionalen automatischen Timer bereitstellt, die den internen SMA-Indikator zurücksetzen und das Aktualisierungsereignis protokollieren. Eine einfache SMA-Trendfolgeregel wird angewendet, um das Trading nach der Neuinitialisierung des Indikators zu demonstrieren.

## Funktionsweise

1. **Primärer Datenfeed** – die Strategie abonniert den durch `CandleType` definierten Zeitrahmen und berechnet einen einfachen gleitenden Durchschnitt mit der Länge `SmaLength`.
2. **Manuelle Aktualisierung** – wenn `ManualRefreshRequest` auf `true` gesetzt wird, wird der Zustand des gleitenden Durchschnitts zurückgesetzt, das Flag wird gelöscht, und die Aktion wird im Log zusammen mit den gespeicherten Schaltflächen-Metadaten gemeldet (`RefreshCommandName`, `RefreshCommandText`, `TextColorName`, `BackgroundColorName`).
3. **Automatische Aktualisierung** – die Aktivierung von `AutoRefreshEnabled` plant wiederkehrende Resets alle `AutoRefreshInterval`, was die timer-gesteuerte Neuinitialisierung von MetaTrader reproduziert.
4. **Handelslogik** – nachdem der SMA gebildet ist, hält die Strategie höchstens eine Position. Sie geht long, wenn der Schlusskurs über dem SMA liegt, und wechselt zu short, wenn der Preis darunter fällt, wobei zuerst die entgegengesetzte Seite geschlossen wird.

Dieses Verhalten spiegelt die Idee der Neuinitialisierung aller Charts aus dem ursprünglichen Expert Advisor wider, während idiomatische StockSharp-Komponenten (Indikator-Reset und Logging) anstatt des Wechsels von Chart-Zeitrahmen verwendet werden.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Arbeits-Zeitrahmen für Kerzen-Abonnement. |
| `SmaLength` | Anzahl der Kerzen für den gleitenden Durchschnitt, der nach jeder Aktualisierung neu erstellt wird. |
| `AutoRefreshEnabled` | Aktiviert den periodischen Aktualisierungs-Timer. |
| `AutoRefreshInterval` | Intervall zwischen automatischen Aktualisierungsereignissen. |
| `ManualRefreshRequest` | Manuell auf `true` setzen, um eine sofortige Aktualisierung auszulösen. Die Strategie löscht es nach der Verarbeitung. |
| `RefreshCommandName` | Metadaten, die den MetaTrader-Schaltflächennamen widerspiegeln; bei einer Aktualisierung im Log gemeldet. |
| `RefreshCommandText` | Metadaten, die die MetaTrader-Schaltflächenbeschriftung widerspiegeln; bei einer Aktualisierung im Log gemeldet. |
| `TextColorName` | Gespeicherte Schaltflächen-Textfarbbeschreibung aus dem MQL-Skript. |
| `BackgroundColorName` | Gespeicherte Schaltflächen-Hintergrundfarbbeschreibung aus dem MQL-Skript. |

## Verwendung

1. Konfigurieren Sie `CandleType` und `SmaLength` passend zum Markt und Zeitrahmen, den Sie überwachen möchten.
2. Aktivieren Sie `AutoRefreshEnabled` und wählen Sie `AutoRefreshInterval`, wenn Sie geplante Indikator-Neuaufbauten benötigen. Lassen Sie es deaktiviert, wenn Sie nur manuelle Steuerung wünschen.
3. Schalten Sie `ManualRefreshRequest` auf `true`, wenn Sie den Indikator-Zustand leeren möchten. Das Flag wird automatisch wieder auf `false` gesetzt, sobald die Aktualisierung registriert ist.
4. Starten Sie die Strategie, um Marktdaten zu abonnieren. Sie zeichnet Kerzen, die SMA-Kurve und Ihre eigenen Trades im Chart und führt die grundlegenden SMA-Trendfolge-Trades aus, sobald der Indikator bereit ist.

## Unterschiede zum ursprünglichen MQL-Skript

- StockSharp stellt Chart-Schaltflächen nicht auf dieselbe Weise zur Verfügung, daher wird der Aktualisierungsauslöser über Strategie-Parameter implementiert.
- Anstatt zwischen M1- und M5-Zeitrahmen zu wechseln, setzt der StockSharp-Port seine Indikatoren direkt zurück, was innerhalb des Frameworks zuverlässiger ist.
- Schaltflächenbeschriftungen und -farben werden als Metadaten für das Logging beibehalten, um eine Verbindung zur MetaTrader-Oberfläche zu erhalten, auch wenn keine Chart-Steuerelemente erstellt werden.
