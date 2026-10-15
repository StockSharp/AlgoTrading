# ADX Einfache Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **ADX Simple Trend Strategy** ist eine direkte Portierung des klassischen MetaTrader Expert Advisors „ADX Simple“. Es folgt der Richtung des durchschnittlichen Richtungsindex (ADX), indem es die positiven und negativen Richtungsbewegungsindikatoren (DI+ und DI-) vergleicht und erfordert, dass die Hauptlinie ADX ansteigt, bevor ein Handel eröffnet wird. Die StockSharp-Version behält den minimalistischen Charakter des ursprünglichen Systems bei und passt es gleichzeitig an übergeordnete API-Muster und Risikokontrollen an.

## Indikatorstapel
- **Durchschnittlicher Richtungsindex (ADX)** mit konfigurierbarem Zeitraum (Standard 25).
  - Stellt die **Hauptzeile ADX** zur Bestätigung der Trendstärke bereit.
  - Liefert **DI+**- und **DI-**-Werte, die bullische oder bärische Dominanz definieren.
- **Zeitrahmen** kann über `CandleType` ausgewählt werden (standardmäßig 15-Minuten-Kerzen).

## Signalerzeugung
### Langer Eintrag
1. Warten Sie auf eine fertige Kerze und einen endgültigen ADX-Wert.
2. Stellen Sie sicher, dass DI+ über DI- auf derselben Leiste liegt.
3. Erfordern, dass die Hauptlinie ADX unbedingt größer als ihr vorheriger Wert ist (der Trend verstärkt sich).
4. Wenn keine offene Position vorhanden ist, senden Sie eine Marktkauforder mit dem Strategievolumen.

### Kurzer Eintrag
1. Warten Sie auf eine fertige Kerze und einen abgeschlossenen Messwert von ADX.
2. Bestätigen Sie, dass DI- über DI+ liegt.
3. Erfordern, dass die Hauptzeile ADX größer als ihr vorheriger Wert ist.
4. Wenn der Kurs flach ist, senden Sie einen Marktverkaufsauftrag mit dem Strategievolumen.

### Exit-Logik
- **Close Long**: Wenn DI- über DI+ kreuzt (Trenddynamik wird bärisch).
- **Close Short**: Wenn DI+ DI- überschreitet (Trendmomentum wird bullisch).
- Die ADX-Steigungsprüfung ist für Exits nicht erforderlich und spiegelt die ursprüngliche EA wider, bei der Positionen unmittelbar nach einem DI-Crossover geschlossen wurden.

## Positionsmanagement
- Die Strategie ist immer entweder Flat, Long oder Short; es hält niemals gleichzeitige Positionen in beide Richtungen.
- Die Größe von Marktaufträgen wird mithilfe der integrierten Eigenschaft `Strategy.Volume` (Standard 1) bestimmt. Passen Sie diese Eigenschaft an, wenn Sie die Strategieinstanz so konfigurieren, dass sie zu Ihrer Instrumentengröße passt.
- Es gibt keine automatischen Stop-Loss- oder Take-Profit-Orders. Das Risiko sollte extern oder durch Änderung der Strategie kontrolliert werden.

## Parameter
| Parameter | Typ | Standard | Beschreibung |
|-----------|------|---------|-------------|
| `AdxPeriod` | `int` | 25 | Lookback-Länge für ADX-, DI+- und DI--Berechnungen. |
| `CandleType` | `DataType` | 15-minütiger Zeitrahmen | Kerzenabonnement zur Steuerung von Indikatorberechnungen. |

## Unterschiede zur Originalversion MQL
- Geldverwaltung: Die ursprünglichen EA-Lots wurden basierend auf dem Kontostand in der Größe geändert; Die StockSharp-Strategie verwendet `Strategy.Volume` und überlässt die Kapitalverwaltung der Hosting-Umgebung.
- Auftragsverfolgung: Anstatt MetaTrader Auftragspools zu durchlaufen, verlässt sich StockSharp auf den integrierten `Position`-Wert.
- Datenverarbeitung: Die Strategie ignoriert unfertige Kerzen und handelt nur mit endgültigen Daten.
- Protokollierungs- und Visualisierungs-Hooks sind über die Hilfsprogramme `CreateChartArea`, `DrawCandles` und `DrawIndicator` verfügbar, um das Debuggen zu erleichtern.

## Nutzungsrichtlinien
1. Hängen Sie die Strategie an ein Instrument mit ausreichender Trendbewegung an (z. B. FX-Majors oder Indizes).
2. Stellen Sie den gewünschten Kerzentyp und die ADX-Länge über Parameter ein, bevor Sie mit der Strategie beginnen.
3. Aktivieren Sie optional das Risikomanagement auf Portfolioebene (Stop-Outs, Drawdown-Limits) über die Hosting-Anwendung.
4. Überwachen Sie DI-Überkreuzungen und die ADX-Steigung in der Diagrammvisualisierung, um das Verhalten zu überprüfen.

## Erweiterung der Strategie
- Fügen Sie Volatilitätsfilter (ATR, Standardabweichung) hinzu, um Bedingungen mit geringer Volatilität zu vermeiden.
- Führen Sie eine Stop-Loss-/Take-Profit-Automatisierung ein, indem Sie `StartProtection` oder eine benutzerdefinierte Orderlogik in `ProcessCandle` aufrufen.
- Kombinieren Sie es mit Filtern mit höherem Zeitrahmen, indem Sie zusätzliche Kerzen-Streams abonnieren.

Ziel dieser Dokumentation ist es, einen umfassenden Überblick über die Simple Trend Strategy von ADX zu geben, damit Sie sie sicher innerhalb des StockSharp-Frameworks bereitstellen und erweitern können.
