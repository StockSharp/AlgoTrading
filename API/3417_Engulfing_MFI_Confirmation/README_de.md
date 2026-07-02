# Verschlingende MFI-Bestätigungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader-Experten „Expert_ABE_BE_MFI“, indem sie japanische Candlestick-Engulfing-Muster mit der Bestätigung durch den Money Flow Index (MFI)-Oszillator kombiniert. Eine Long-Position wird eröffnet, wenn eine bullische Engulfing-Kerze erscheint, während der Geldfluss in einer überverkauften Zone bleibt. Eine Short-Position wird eröffnet, wenn sich unter überkauften Geldflussbedingungen eine bärische Engulfing-Kerze bildet. Positionen werden geschlossen, wenn MFI dynamische Ausstiegsschwellen überschreitet, was eine Momentumumkehr signalisiert.

## Kernidee

1. **Mustererkennung** – der Körper der aktuell fertigen Kerze muss die vorherige Kerze in Handelsrichtung vollständig umschließen.
2. **Volumenbestätigung** – der MFI-Indikator (Länge konfigurierbar, Standard 37) muss bei Long-Einstiegen unter dem überverkauften Niveau (40) oder bei Short-Einstiegen über dem überkauften Niveau (60) liegen.
3. **Momentum-Ausgänge** – offene Positionen werden geschlossen, wenn MFI wichtige Umkehrniveaus (30 und 70) in die entgegengesetzte Richtung überschreitet, was die ursprüngliche Abstimmungslogik des MQL-Experten nachahmt.

## Indikatoren

- **Money Flow Index (MFI)** – berechnet das volumenbereinigte Momentum. Die Strategie speichert die letzten beiden MFI-Messwerte, um Bahnübergänge zu erkennen.
- **Candlestick-Körperanalyse** – es ist kein zusätzlicher Indikator registriert; Die Engulfing-Erkennung verwendet die letzten beiden abgeschlossenen Kerzen.

## Handelsregeln

### Langer Eintrag

- Die vorherige Kerze ist bärisch und die aktuelle Kerze ist bullisch.
- Der aktuelle Kerzenkörper öffnet sich unterhalb oder gleich dem vorherigen Schlusskurs und schließt oberhalb oder gleich dem vorherigen Eröffnungskurs (striktes Engulfing).
- Der neueste MFI-Wert liegt unter dem konfigurierbaren `OversoldLevel` (Standard 40).

### Kurzer Eintrag

- Die vorherige Kerze ist bullisch und die aktuelle Kerze ist bärisch.
- Der aktuelle Kerzenkörper öffnet sich über oder gleich dem vorherigen Schlusskurs und schließt unter oder gleich dem vorherigen Eröffnungskurs.
- Der neueste MFI-Wert liegt über dem konfigurierbaren `OverboughtLevel` (Standard 60).

### Ausstiegsbedingungen

- **Short schließen**, wenn MFI von unten über `ExitLongLevel` (30) oder `ExitShortLevel` (70) kreuzt.
- **Long schließen**, wenn der MFI von oben unter `ExitShortLevel` (70) oder `ExitLongLevel` (30) fällt.

Diese Ausstiegsschwellen stellen die doppelte Abstimmungslogik des ursprünglichen Experten wieder her und stellen sicher, dass längere Bewegungen des Geldflusses eine rechtzeitige Liquidation von Positionen auslösen.

### Handelsmanagement

- Für Ein- und Ausstiege werden Marktaufträge (`BuyMarket` / `SellMarket`) verwendet.
- Es wird kein expliziter Stop-Loss oder Take-Profit verwendet; Das Risikomanagement stützt sich auf die MFI-Umkehrsignale.

## Parameter

| Name | Beschreibung | Standard | Bereich / Hinweise |
| ---- | ----------- | ------- | ------------- |
| `CandleType` | Für die Analyse verwendeter Kerzenzeitrahmen. | 1 Minute | Jeder unterstützte Kerzentyp. |
| `MfiPeriod` | Länge des Geldflussindex. | 37 | Muss > 0 sein; Entspricht der ursprünglichen EA-Standardeinstellung. |
| `OversoldLevel` | MFI-Niveau, das bullische Engulfing-Setups bestätigt. | 40 | Aktivieren Sie bei Bedarf die Optimierung. |
| `OverboughtLevel` | MFI-Niveau, das rückläufige Engulfing-Setups bestätigt. | 60 | Aktivieren Sie bei Bedarf die Optimierung. |
| `ExitLongLevel` | Untere MFI-Grenze zur Erkennung von Umkehrungen. | 30 | Wird sowohl für lange Exits als auch für kurze Bestätigungen verwendet. |
| `ExitShortLevel` | Obere MFI-Grenze zur Erkennung von Umkehrungen. | 70 | Wird sowohl für kurze Exits als auch für lange Bestätigungen verwendet. |

## Hinweise zur Konvertierung

- Der ursprüngliche MQL-Experte sammelte „Stimmen“ aus Engulfing-Mustern und MFI-Filtern. Die C#-Strategie reproduziert denselben Entscheidungsfluss, indem sie die Abstimmungsregeln direkt in diskrete Eintritts- und Austrittsbedingungen umwandelt.
- Money-Management- und Trailing-Module aus der MQL-Version werden weggelassen; Die Positionsgröße von StockSharp wird durch das Strategievolumen gesteuert.
- Alle Indikatorbindungen nutzen nach Bedarf die übergeordnete API (`SubscribeCandles().Bind(...)`).

## Nutzungstipps

- Optimieren Sie `MfiPeriod`, `OversoldLevel` und `OverboughtLevel`, um die Strategie an bestimmte Märkte anzupassen.
- Kombinieren Sie es mit Risikokontrollen (Schutzstopps) über `StartProtection` in der Host-Anwendung, wenn zusätzliche Sicherheit erforderlich ist.
- Stellen Sie sicher, dass ausreichend historische Daten vorhanden sind, damit der Geldflussindex vollständig gebildet ist, bevor Sie den Handel aktivieren.
