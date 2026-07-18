# Alli Heik-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Alli Heik-Strategie ist eine Konvertierung des MetaTrader 5-Expertenberaters "AlliHeik". Sie handelt den **Heiken Ashi Smoothed Oscillator** (HASO), der ursprünglich von mladen veröffentlicht wurde. Der Indikator erstellt eine benutzerdefinierte Heiken-Ashi-Kerze, indem er die rohen Eröffnungs-, Hoch-, Tief- und Schlusskurse mit einem wählbaren gleitenden Durchschnitt glättet, einen zusätzlichen Glättungsdurchgang auf den Heiken-Ashi-Mittelpunkt anwendet und dann die Bar-zu-Bar-Differenz dieses geglätteten Werts misst. Ein gleitender Durchschnitt der Differenz bildet die Signallinie.

Handelsentscheidungen werden auf dem Crossover des Oszillators und der Signallinie getroffen, bewertet auf vollständig geschlossenen Kerzen. Die Strategie bietet einen optionalen Umkehrmodus, die Möglichkeit, entgegengesetzte Positionen automatisch zu schließen, statische Stop-Loss/Take-Profit-Behandlung und einen Trailing-Stop, der die Schrittlogik der ursprünglichen MetaTrader-Version imitiert.

## Handelsregeln

1. **Indikatorvorbereitung**
   - OHLC-Daten mit einer von SMA, EMA, SMMA oder LWMA vor-glätten.
   - Heiken-Ashi-Kerzen aus den geglätteten Daten erstellen und Eröffnung/Schluss mitteln, um einen Mittelpunkt zu erhalten.
   - Den Mittelpunkt nach-glätten und den Oszillator als Differenz zwischen aufeinanderfolgenden geglätteten Werten berechnen.
   - Den Oszillator mit einem konfigurierbaren gleitenden Durchschnitt glätten, um die Signallinie zu erstellen.
2. **Einstiegsbedingungen**
   - *Normalmodus*: **Long** öffnen, wenn der Oszillator **unter** die Signallinie kreuzt, **Short** öffnen, wenn er **über** die Signallinie kreuzt (exakte Reproduktion der MQL-Logik).
   - *Umkehrmodus*: Long- und Short-Bedingungen tauschen.
   - Signale werden nur auf abgeschlossenen Kerzen ausgewertet. Bestehende Positionen können optional geschlossen werden, bevor ein neuer Trade in die entgegengesetzte Richtung eingegangen wird.
3. **Ausstiegsmanagement**
   - Statische Stop-Loss- und Take-Profit-Abstände werden in Pips ausgedrückt und mithilfe der Tick-Größe und Dezimalstellen des Instruments in Preise umgerechnet.
   - Ein Trailing-Stop wird aktiv, wenn sich der Preis um *TrailingStop + TrailingStep* Pips im Gewinn bewegt. Der Stop wird dann auf `aktueller Preis - TrailingStop` für Longs (oder `aktueller Preis + TrailingStop` für Shorts) verschoben und bewegt sich nur, wenn der neue Stop mindestens `TrailingStep` Pips über dem vorherigen Level liegt.
   - Manuelle Ausstiege werden ausgelöst, wenn der Preis den konfigurierten Stop oder das Ziel berührt.

## Parameter

- **Volume** – Ordervolumen in Lots.
- **Stop Loss (pips)** – Abstand für den Schutz-Stop; auf 0 setzen zum Deaktivieren.
- **Take Profit (pips)** – Abstand für das Gewinnziel; auf 0 setzen zum Deaktivieren.
- **Trailing Stop (pips)** – Trailing-Stop-Abstand; auf 0 setzen zum Deaktivieren des Trailings.
- **Trailing Step (pips)** – Mindestfortschritt über den Trailing-Stop hinaus, bevor der Stop bewegt wird (muss positiv sein, wenn Trailing aktiviert ist).
- **Reverse Signals** – Long/Short-Interpretation des Oszillator-Crossovers umkehren.
- **Close Opposite** – Eine bestehende Position schließen, bevor ein neuer Trade in die entgegengesetzte Richtung eröffnet wird.
- **Pre Smooth Period / Method** – Gleitender Durchschnittszeitraum und -typ zum Glätten der rohen OHLC-Daten.
- **Post Smooth Period / Method** – Gleitende Durchschnittsparameter zum Glätten des Heiken-Ashi-Mittelpunkts.
- **Signal Period / Method** – Gleitende Durchschnittsparameter für die Oszillator-Signallinie.
- **Candle Type** – Für Berechnungen verwendete Kerzenquelle (Standard: 15-Minuten-Zeitrahmen).

## Implementierungshinweise

- Die Konvertierung reproduziert den ursprünglichen Heiken Ashi Smoothed Oscillator durch Verkettung von StockSharp-Gleitenden-Durchschnitts-Indikatoren (SMA, EMA, SMMA, LWMA) zum Vor-Glätten von Preisen, Erstellen der Heiken-Ashi-Reihe und Ableiten der Oszillatordifferenz.
- Pip-Abstände werden mithilfe der Tick-Größe und Dezimalpräzision des Instruments in absolute Preisoffsets übersetzt, was der 3/5-Ziffern-Behandlung von MetaTrader entspricht.
- Manuelle Stop/Ziel-Überprüfungen und der schrittbasierte Trailing-Stop werden bei jeder abgeschlossenen Kerze ausgeführt und spiegeln das Verhalten der MQL-Version eng wider.
- Signale werden nur verarbeitet, wenn alle erforderlichen Werte verfügbar sind; partielle Indikatorzustände werden ignoriert, bis genügend Daten angesammelt wurden.

In diesem Verzeichnis wird keine Python-Übersetzung bereitgestellt.
