# Poker Show-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die Poker Show-Strategie ist eine direkte Portierung des MetaTrader 5-Expertenberaters „Poker_SHOW". Sie kombiniert einen gleitenden Durchschnitt als Trendfilter mit einem probabilistischen Auslöser, der das Ziehen einer Pokerhand imitiert. Trades werden nur ausgeführt, wenn der zufällig generierte Handwert unter einen konfigurierbaren Pokerkombinations-Schwellenwert fällt. Der Ansatz erzeugt seltene Einstiege, bleibt dabei aber mit dem vorherrschenden, durch den gleitenden Durchschnitt erkannten Trend ausgerichtet.

Die Strategie arbeitet auf einem einzelnen Symbol und stützt sich auf reguläre zeitbasierte Kerzen. Handelsentscheidungen werden einmal pro abgeschlossener Kerze ausgewertet, was dem ursprünglichen Berater entspricht, der auf die Eröffnung jeder neuen Bar reagiert.

## Kernlogik

1. **Gleitender-Durchschnitt-Trendfilter**
   - Ein konfigurierbarer gleitender Durchschnitt (SMA, EMA, SMMA oder LWMA) wird aus der gewählten Preisquelle berechnet (Schluss, Eröffnung, Hoch, Tief, Median, Typisch oder gewichteter Preis).
   - Der Indikator kann zeitlich nach vorne verschoben werden, um den MetaTrader-„Shift"-Input zu reproduzieren. Die Strategie verwendet immer den Wert der letzten vollständig gebildeten Kerze, genau wie der Quell-EA.

2. **Wahrscheinlichkeits-Gate**
   - Jede Seite (Long oder Short) zieht bei jeder Bar einen unabhängigen Zufallswert zwischen 0 und 32.767.
   - Der Zug wird mit der ausgewählten Pokerkombination verglichen. Kombinationen mit höherem Rang (z. B. Royal Flush) haben kleinere numerische Schwellenwerte und lösen daher weniger häufig aus, während Kombinationen mit niedrigerem Rang (z. B. Ein Paar) öfter traden.

3. **Richtungsregeln**
   - Long-Trades erfordern, dass der gleitende Durchschnitt mindestens den konfigurierten Abstand über dem Preis bleibt. Wenn die Option **Signale umkehren** aktiviert ist, wird die Bedingung invertiert.
   - Short-Trades erfordern, dass der gleitende Durchschnitt mit demselben Abstand unter dem Preis bleibt, wobei die Bedingung invertiert wird, wenn der Umkehrschalter aktiv ist.
   - Es kann nur eine Position gleichzeitig aktiv sein. Ein Einstieg in die entgegengesetzte Richtung gleicht automatisch alle offenen Positionen aus, bevor der neue Trade etabliert wird.

4. **Risikomanagement**
   - Optionale Stop-Loss- und Take-Profit-Niveaus werden in Preisschritten (Punkte) relativ zum Ausführungspreis berechnet. Eine Distanz auf null zu setzen deaktiviert das entsprechende Niveau.
   - Stops und Ziele werden bei jeder abgeschlossenen Kerze überprüft. Wenn sie erreicht werden, schließt die Strategie die Position und setzt die Risikomerker zurück.

5. **Positionsschutz**
   - Das eingebaute StockSharp-Schutzmodul wird beim Start aktiviert, um das Konto bei manuellen Läufen vor unerwarteten Verlusten zu schützen.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| **Pokerkombination** | Wahrscheinlichkeitsschwellenwert, der den Zufallszug überschreiten muss, um einen neuen Trade zu erlauben. Repräsentiert klassische Pokerhände von Royal Flush (seltenst) bis Ein Paar (häufigst). |
| **Volumen** | Ordervolumen in Lots. Wird sowohl für neue Einstiege als auch zum Umkehren bestehender Positionen verwendet. |
| **Stop-Loss** | Abstand zwischen dem Einstiegspreis und dem Schutz-Stop, gemessen in Preisschritten. Auf null setzen zum Deaktivieren. |
| **Take-Profit** | Abstand zwischen dem Einstiegspreis und dem Gewinnziel, gemessen in Preisschritten. Auf null setzen zum Deaktivieren. |
| **Kauf aktivieren** | Erlaubt der Strategie, Long-Positionen zu eröffnen. |
| **Verkauf aktivieren** | Erlaubt der Strategie, Short-Positionen zu eröffnen. |
| **MA-Abstand** | Mindestabstand in Preisschritten zwischen dem gleitenden Durchschnittswert und dem aktuellen Preis. Dient als Trendbestätigungsfilter. |
| **MA-Periode** | Anzahl der vom gleitenden Durchschnitt verwendeten Bars. |
| **MA-Verschiebung** | Horizontale Verschiebung des gleitenden Durchschnitts (in Bars), entsprechend dem MetaTrader-`ma_shift`-Input. |
| **MA-Methode** | Glättungstyp des gleitenden Durchschnitts: einfach, exponentiell, geglättet oder linear gewichtet. |
| **Angewendeter Preis** | Kerzenkurs für die Berechnung des gleitenden Durchschnitts. |
| **Signale umkehren** | Invertiert den Vergleich zwischen gleitendem Durchschnitt und Preis und tauscht effektiv Long- und Short-Logik aus. |
| **Kerzentyp** | Zeitrahmen der Kerzen-Subscription. Standard ist eine Stunde, um die ursprünglichen Einstellungen zu replizieren. |

## Hinweise und Empfehlungen

- Das Wahrscheinlichkeits-Gate macht die Strategie hochstochastisch. Backtests sollten mehrere Läufe oder Monte-Carlo-Analysen verwenden, um die Ergebnisverteilung zu verstehen.
- Da das Trade-Management auf abgeschlossenen Kerzen basiert, können große Intrabar-Spitzen Stop- oder Zielniveaus übersteigen, bevor die Strategie reagieren kann. Erwägen Sie, auf niedrigeren Zeitrahmen zu handeln, wenn dieses Verhalten unerwünscht ist.
- Um die MetaTrader-Umgebung getreu zu reproduzieren, stellen Sie sicher, dass das Instrument dieselbe Kontraktgröße und denselben Preisschritt verwendet, damit punktbasierte Abstände den ursprünglichen Lots- und Pip-Werten entsprechen.
- Die Strategie verwendet Market-Orders (`BuyMarket` und `SellMarket`) wie im Quell-Expertenberater. Die Slippage-Handhabung wird an die StockSharp-Ausführungsinfrastruktur delegiert.
