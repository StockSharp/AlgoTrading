# Exp ColorX2MA X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie rekonstruiert den Dual-Timeframe-Experten "Exp_ColorX2MA_X2" für StockSharp. Sie schichtet zwei ColorX2MA-Filter: eine Trendkarte für den höheren Zeitrahmen und einen Einstiegsauslöser für den niedrigeren Zeitrahmen. Beide ColorX2MA-Werte werden durch Kaskadierung zweier konfigurierbarer gleitender Durchschnitte aufgebaut und anschließend entsprechend der aktuellen Steigung eingefärbt. Handelsentscheidungen werden getroffen, wenn sich die Farbe des niedrigeren Zeitrahmens in Richtung des Trends des höheren Zeitrahmens ändert.

Die Implementierung unterstützt die ursprünglichen Optionen für angewandte Preise und die gängigsten Glättungsmodi (SMA, EMA, SMMA, LWMA, Jurik). Wenn der Jurik-Indikator eine `Phase`-Eigenschaft bereitstellt, wird diese mit dem konfigurierten Phasenwert aktualisiert.

## Handelsregeln
- **Long-Einstieg**
  - Die ColorX2MA-Farbe des höheren Zeitrahmens ist bullisch (trend direction > 0).
  - Die ColorX2MA-Farbe des niedrigeren Zeitrahmens wechselte von bullisch im vorherigen Balken zu neutral oder bärisch im letzten abgeschlossenen Balken (`Clr[1] == 1` und `Clr[0] != 1`).
  - Long-Handel ist aktiviert.
- **Short-Einstieg**
  - Die ColorX2MA-Farbe des höheren Zeitrahmens ist bärisch (trend direction < 0).
  - Die ColorX2MA-Farbe des niedrigeren Zeitrahmens wechselte von bärisch im vorherigen Balken zu neutral oder bullisch im letzten abgeschlossenen Balken (`Clr[1] == 2` und `Clr[0] != 2`).
  - Short-Handel ist aktiviert.
- **Long-Ausstieg**
  - Wenn eine bärische Farbe im niedrigeren Zeitrahmen erscheint (`Clr[1] == 2`) und die sekundäre Long-Schließerlaubnis aktiviert ist, **oder** der Trend des höheren Zeitrahmens bärisch wird, während die primäre Long-Schließerlaubnis aktiviert ist.
- **Short-Ausstieg**
  - Wenn eine bullische Farbe im niedrigeren Zeitrahmen erscheint (`Clr[1] == 1`) und die sekundäre Short-Schließerlaubnis aktiviert ist, **oder** der Trend des höheren Zeitrahmens bullisch wird, während die primäre Short-Schließerlaubnis aktiviert ist.
- **Stops**
  - Optionale Stop-Loss- und Take-Profit-Abstände werden in Punkten angegeben (multipliziert mit dem Preisschritt des Instruments). Sie werden bei jeder abgeschlossenen Signalkerze ausgewertet, indem die Kerzenextreme mit dem durchschnittlichen Positionspreis verglichen werden.

## Standardwerte
- **Trend-Zeitrahmen**: 6-Stunden-Kerzen.
- **Signal-Zeitrahmen**: 30-Minuten-Kerzen.
- **Trend-Glättung**: SMA(12) in Jurik(5, Phase 15).
- **Signal-Glättung**: SMA(12) in Jurik(5, Phase 15).
- **Angewandter Preis**: Schlusskurs.
- **Signalversatz**: 1 Balken in beiden Zeitrahmen.
- **Erlaubnisse**: Long-/Short-Einstiege und -Ausstiege sind aktiviert.
- **Stop-Loss**: 1000 Punkte (umgerechnet mit dem Preisschritt).
- **Take-Profit**: 2000 Punkte (umgerechnet mit dem Preisschritt).

## Filter und Hinweise
- Richtung: handelt Long und Short, gesteuert über Erlaubnisflags.
- Zeitrahmen: dualer Zeitrahmen (Trend auf HTF, Einstiege auf LTF).
- Indikatoren: zweistufiges ColorX2MA mit konfigurierbaren Glättungsmethoden.
- Glättungsunterstützung: `Sma`, `Ema`, `Smma`, `Lwma`, `Jurik`. Andere Modi der Originalbibliothek sind nicht implementiert.
- Angewandte Preise: alle 12 ursprünglichen Formeln einschließlich TrendFollow- und Demark-Preisen.
- Stops: optionaler Stop-Loss und Take-Profit mit festem Abstand.
- Komplexität: mittel, da zwei Zeitrahmen und Farb-Puffer synchronisiert werden.
- Geeignet für: Trendfolge-Setups auf FX, Indizes oder Krypto, bei denen der ColorX2MA-Indikator bevorzugt wird.

## Verwendungshinweise
- Den höheren Zeitrahmen deutlich größer als den Signal-Zeitrahmen halten, um häufige Whipsaws zu vermeiden.
- Den Signalversatz-Parameter (`SignalSignalBar`) enger setzen, um schneller zu reagieren, oder erhöhen, um mehr zu glätten.
- Wenn das Instrument kein `PriceStep` bereitstellt, werden die Stop-/Take-Abstände direkt in Preiseinheiten interpretiert.
- Jurik-Glättung erfordert ein lizenziertes StockSharp-Indikatorpaket; wenn nicht verfügbar, läuft die Strategie weiterhin mit den anderen Glättungsoptionen.
