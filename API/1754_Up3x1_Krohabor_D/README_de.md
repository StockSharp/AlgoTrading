# Up3x1 Krohabor D-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie verwendet drei einfache gleitende Durchschnitte (schnell, mittel, langsam) zur Identifizierung der Trendrichtung. Eine Long-Position wird eröffnet, wenn der schnelle MA den mittleren MA von unten kreuzt und beide MAs auf dem aktuellen und vorherigen Balken über dem langsamen MA liegen. Eine Short-Position wird eröffnet, wenn der schnelle MA den mittleren MA von oben kreuzt und beide MAs unter dem langsamen MA liegen.

Positionen werden mit Take-Profit-, Stop-Loss- und optionalen Trailing-Stop-Niveaus geschützt. Orders werden zu Marktpreisen ausgeführt.

## Parameter
- **Volume** – Ordergröße.
- **Fast Period** – Periode des schnellen SMA.
- **Middle Period** – Periode des mittleren SMA.
- **Slow Period** – Periode des langsamen SMA.
- **Take Profit** – Abstand zum Gewinnziel in Preiseinheiten.
- **Stop Loss** – Abstand zum Schutz-Stop in Preiseinheiten.
- **Trailing Stop** – Abstand zur Trailing-Stop-Aktivierung in Preiseinheiten.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Signale
- **Kauf** – schneller MA kreuzt mittleren MA von unten, beide MAs bleiben über dem langsamen MA.
- **Verkauf** – schneller MA kreuzt mittleren MA von oben, beide MAs bleiben unter dem langsamen MA.

## Schutzmaßnahmen
- Take-Profit- und Stop-Loss-Niveaus werden beim Einstieg gesetzt.
- Wenn aktiviert, bewegt der Trailing-Stop den Schutz-Stop in Handelsrichtung, wenn der Preis voranschreitet.

## Hinweise
Dies ist eine direkte Konvertierung der ursprünglichen MQL-Strategie in StockSharp unter Verwendung der High-Level-API und eingebauter Indikatoren.
