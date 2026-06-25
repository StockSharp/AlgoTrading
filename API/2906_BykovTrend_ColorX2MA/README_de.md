# BykovTrend + ColorX2MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den BykovTrend V2-Farbtrend-Indikator mit dem ColorX2MA-Doppelglättungs-Moving-Average-Steigungsfilter. Beide Logikblöcke operieren auf demselben Symbol und können unabhängig Aufträge erteilen, was die Nettoposition ermöglicht, die jüngste Übereinstimmung zwischen den beiden Signalquellen widerzuspiegeln.

## Übersicht

- **Marktbias**: Funktioniert auf jedem Instrument, das Kerzendaten unterstützt. Der Standard-Zeitrahmen für beide Blöcke ist 4 Stunden (H4), was den ursprünglichen Expert Advisor widerspiegelt.
- **Indikatoren**:
  - *BykovTrend V2*: Verwendet Williams %R, um Kerzen nach dem vorherrschenden Trend einzufärben.
  - *ColorX2MA*: Wendet zwei aufeinanderfolgende gleitende Durchschnitte auf eine konfigurierbare Preisquelle an und klassifiziert die Steigungsrichtung.
- **Signale**: Einstiege und Ausstiege werden separat von jedem Block generiert. Die endgültige Position ist die Summe aller ausgeführten Trades.

## BykovTrend-Block

1. Williams %R wird mit der konfigurierten Periode (Standard 9) berechnet.
2. Schwellenwerte werden um `33 - Risk` verschoben. Wenn %R über `-Risk` steigt, wird der lokale Trend bullisch; wenn er unter `-100 + (33 - Risk)` fällt, wird der Trend bärisch.
3. Kerzenfarben:
   - Grün/Teal (Codes 0, 1): bullischer Trend.
   - Grau (Code 2): neutral, keine Trendänderung.
   - Schokolade/Gold (Codes 3, 4): bärischer Trend.
4. Signale werden an der Kerze ausgewertet, die `SignalBar` Schritte hinter dem zuletzt geschlossenen Balken liegt. Ein Wert von 1 bedeutet die vorherige abgeschlossene Kerze, was der MetaTrader-Implementierung entspricht.
5. Handelslogik:
   - **Long-Einstieg**: Aktuelle Farbe < 2 (bullisch) und vorherige Farbe > 1 (war neutral/bärisch). Optional über *Bykov Allow Long Entries*.
   - **Short-Ausstieg**: Aktuelle Farbe < 2. Optional über *Bykov Allow Short Exits*.
   - **Short-Einstieg**: Aktuelle Farbe > 2 (bärisch) und vorherige Farbe < 3 (war neutral/bullisch). Optional über *Bykov Allow Short Entries*.
   - **Long-Ausstieg**: Aktuelle Farbe > 2. Optional über *Bykov Allow Long Exits*.

## ColorX2MA-Block

1. Ein erster gleitender Durchschnitt glättet den ausgewählten angewandten Preis (Standard Schluss) mit der gewählten Methode und Länge.
2. Ein zweiter gleitender Durchschnitt glättet die erste MA-Ausgabe, ebenfalls mit konfigurierbarer Methode und Länge.
3. Die Steigung der zweiten Glättung definiert den Farbstrom:
   - 1 (Magenta): Wert ist seit der vorherigen Kerze gestiegen.
   - 2 (Violett): Wert ist gesunken.
   - 0 (Grau): unverändert.
4. Signale werden an der Kerze ausgewertet, die `SignalBar` Schritte hinter dem letzten Schluss liegt.
5. Handelslogik:
   - **Long-Einstieg**: Aktuelle Farbe = 1 und vorherige Farbe ≠ 1. Optional über *Color Allow Long Entries*.
   - **Short-Ausstieg**: Aktuelle Farbe = 1. Optional über *Color Allow Short Exits*.
   - **Short-Einstieg**: Aktuelle Farbe = 2 und vorherige Farbe ≠ 2. Optional über *Color Allow Short Entries*.
   - **Long-Ausstieg**: Aktuelle Farbe = 2. Optional über *Color Allow Long Exits*.

## Positionsverwaltung

- Aufträge sind Marktaufträge. Beim Richtungswechsel kauft/verkauft die Strategie genug Kontrakte, um die bestehende Position zu neutralisieren und eine neue der Größe `Volume` zu etablieren.
- Jeder Block kann einen Ausstieg auslösen, auch wenn der andere Block noch die aktuelle Seite befürwortet; der Nettoeffekt ist ein allmähliches Tauziehen zwischen den zwei Modulen.
- Kein automatischer Stop-Loss oder Take-Profit wird angewendet. Risikomanagement sollte extern oder durch Abstimmung der Erlaubnis-Flags gehandhabt werden.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **BykovTrend Candle** | Datentyp (Zeitrahmen) für die BykovTrend-Berechnung. |
| **Williams %R Period** | Rückblick für Williams %R. |
| **Risk Offset** | Verschiebt die Williams %R-Schwellen (`33 - Risk`). Größere Werte straffen bullische Schwellen und lockern bärische. |
| **Signal Bar** | Verzögerung (Anzahl der abgeschlossenen Kerzen) vor dem Handeln auf eine BykovTrend-Farbe. |
| **Allow Long/Short Entries** | BykovTrend-gesteuerte Einstiege aktivieren oder deaktivieren. |
| **Allow Long/Short Exits** | BykovTrend-gesteuerte Ausstiege aktivieren oder deaktivieren. |
| **ColorX2MA Candle** | Datentyp (Zeitrahmen) für den ColorX2MA-Block. |
| **First/Second MA Method** | Glättungsmethode für jede Stufe (SMA, EMA, SMMA, LWMA, Jurik). |
| **First/Second MA Length** | Periodenlänge für jede Glättungsstufe. |
| **First/Second MA Phase** | Kompatibilitätsparameter aus dem ursprünglichen EA; die aktuelle Implementierung behält ihn für die Dokumentation, aber Jurik-Glättung verwendet seine internen Standardwerte. |
| **Applied Price** | Preisquelle für ColorX2MA (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet, einfach, Viertel, Trendfolge-Variationen, DeMark). |
| **Color Signal Bar** | Verzögerung vor dem Handeln auf ColorX2MA-Farben. |
| **Allow Long/Short Entries/Exits** | ColorX2MA-gesteuerte Aktionen aktivieren oder deaktivieren. |

## Hinweise und Einschränkungen

- Nur die in StockSharp verfügbaren Moving-Average-Typen werden unterstützt. Exotische Glättungen aus der MetaTrader-Bibliothek (JurX, Parabolic, T3, VIDYA, AMA) werden nicht reproduziert; aus SMA, EMA, SMMA, LWMA oder Jurik wählen.
- Phasenparameter werden als Referenz beibehalten, ändern aber nicht die eingebauten StockSharp-Indikatoren.
- Die Strategie setzt voraus, dass die `Volume`-Eigenschaft konfiguriert ist; andernfalls werden Einstiege keine Aufträge platzieren.
- Da beide Module unabhängig handeln können, kann der resultierende Auftragsfluss von MetaTrader-Installationen abweichen, die Trades nach magischen Nummern trennen.
