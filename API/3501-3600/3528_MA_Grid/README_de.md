# MA Grid-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine C#-Portierung des MetaTrader 5 Expert Advisors **MAGrid.mq5**. Es unterhält ein abgesichertes Raster von Kauf- und Verkaufspositionen um einen exponentiellen gleitenden Durchschnitt (EMA). Die Idee besteht darin, das Gitter um den Anker EMA herum im Gleichgewicht zu halten. Wenn der Preis vordefinierte Abstandsschritte über oder unter EMA überschreitet, schließt die Strategie eine Position auf der gegenüberliegenden Seite des Rasters und eröffnet eine neue Position in Richtung des Ausbruchs. Dadurch wird der Korb immer wieder um den gleitenden Durchschnitt herum zentriert.

## Originalquelle

- **MQL Repository-Ordner:** `MQL/38303`
- **Originaldatei:** `MAGrid.mq5`
- **Plattform:** MetaTrader 5 (Absicherungsmodus)

## Handelslogik

1. **EMA Anker**
   - Der Zeitraum EMA ist konfigurierbar (Standard 48).
   - Der EMA wird für die ausgewählte Kerzenserie berechnet.
   - Rasterebenen werden als Vielfache des Parameters `Distance` über und unter EMA berechnet.

2. **Gitterinitialisierung**
   - Die effektive Gittergröße muss gleichmäßig sein, um beide Seiten um EMA zu spiegeln.
   - Der aktuelle Rasterindex wird durch Vergleich des letzten Schlusskurses mit den EMA-basierten Niveaus ermittelt.
   - Ein symmetrischer Korb aus Kauf- und Verkaufsmarktaufträgen wird geöffnet, sodass die Hälfte der Positionen unter dem EMA und die andere Hälfte darüber liegt.

3. **Netzwartung**
   - Wenn der Preis über der nächsthöheren Rasterebene schließt, gilt die Strategie:
     - Erhöht den Rasterindex.
     - Schließt eine lange Bestellung, wenn noch Belichtung vorhanden ist.
     - Öffnet einen neuen kurzen Auftrag zur Erweiterung der oberen Hälfte des Rasters.
   - Wenn der Preis unter dem nächstniedrigeren Rasterniveau schließt, gilt die Strategie:
     - Dekrementiert den Rasterindex.
     - Schließt eine Short-Order, wenn noch Engagement vorhanden ist.
     - Öffnet einen neuen Langauftrag zum Wiederaufbau der unteren Hälfte des Rasters.
   - Wenn auf einer Seite des Rasters keine Belichtung mehr vorhanden ist, wird der entsprechende Auslöser deaktiviert, bis neue Aufträge geöffnet werden.

4. **Auftragsabwicklung**
   - Aufträge werden über eine einfache interne Karte verfolgt, um zwischen Eröffnungs- und Schlussausführungen zu unterscheiden.
   - Die Strategie speichert separate Exposure-Zähler für die Long- und Short-Körbe. Dies spiegelt das Absicherungsverhalten der MQL-Version wider, während das Nettopositionsmodell von StockSharp verwendet wird.

## Parameter

| Name | Standard | Beschreibung |
| --- | --- | --- |
| `MaPeriod` | 48 | EMA Zeitraum, der für die Ankerebene verwendet wird. |
| `GridAmount` | 6 | Anzahl der Rasterschritte; automatisch auf einen geraden Wert aufgerundet. |
| `Distance` | 0,005 | Relativer Abstand zwischen Rasterebenen (z. B. 0,005 = 0,5 %). |
| `OrderVolume` | 0,1 | Mit jeder Market-Order übermitteltes Volumen. |
| `CandleType` | Täglicher Zeitrahmen | Kerzenserien zur Berechnung des EMA und zur Auswertung von Signalen. |

## Risikomanagement

- Die Strategie implementiert keine Stop-Loss- oder Take-Profit-Regeln; Das Risiko wird über die Anzahl der Rasterschritte und das Auftragsvolumen gesteuert.
- Da das Raster sowohl Long- als auch Short-Engagements aufrechterhält, kann der Portfoliowert relativ stabil bleiben, die Margin-Nutzung nimmt jedoch mit der Rastergröße und -entfernung zu.
- Erwägen Sie die Verwendung von Portfoliorisikokontrollen (maximaler Drawdown, Kapitalverwendung) auf Strategie- oder Portfolioebene.

## Konvertierungshinweise

- Die C#-Implementierung reproduziert die abgesicherte Logik, indem Long- und Short-Engagement getrennt verfolgt werden.
- Die kontoabhängige Volumenberechnung von MQL wurde aus Gründen der Übersichtlichkeit durch einen konfigurierbaren `OrderVolume`-Parameter ersetzt.
- Candle-Abonnements basieren auf der StockSharp-Hochebene API unter Verwendung von `SubscribeCandles().Bind(...)` gemäß den Projektrichtlinien.
