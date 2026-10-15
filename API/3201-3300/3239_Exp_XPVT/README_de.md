# Exp XPVT-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Exp XPVT-Strategie** ist eine Konvertierung des MetaTrader 5-Expertenberaters *Exp_XPVT*. Das System handelt Kreuzungen zwischen dem Price and Volume Trend (PVT)-Indikator und einem konfigurierbaren gleitenden Durchschnitt, der auf die PVT-Serie angewendet wird. Wenn die rohe PVT-Linie unter ihre geglättete Variante fällt, öffnet die Strategie Long-Positionen, während Aufwärtskreuzungen Short-Einstiege auslösen. Optionale Stop-Loss- und Take-Profit-Abstände emulieren das Verhalten des ursprünglichen Expertenberaters.

## Indikatorlogik
- Der Price and Volume Trend akkumuliert volumengewichtete prozentuale Preisänderungen mithilfe des ausgewählten angewendeten Preises (Schlusskurs, Eröffnung, Median usw.).
- Ein Glättungsfilter (SMA, EMA, geglätteter MA, LWMA, Jurik, T3, VIDYA-Näherung oder Kaufman AMA) erzeugt die Signallinie.
- Ein historischer Versatz (`Signal Bar`) recreiert die MT5-Logik: Die Strategie vergleicht geglättete und rohe Werte von einer und zwei Bars zurück, um Kreuzungen und Ausstiegsbedingungen zu erkennen.
- Tick- oder reales Volumen kann für die Gewichtung verwendet werden. Wenn der angeforderte Volumentyp nicht verfügbar ist, wechselt die Strategie automatisch zur anderen Quelle.

## Handelsregeln
1. Bei jeder abgeschlossenen Kerze den PVT-Wert aus dem konfigurierten angewendeten Preis und Volumentyp berechnen.
2. Den Glättungsindikator aktualisieren und die neuesten Werte gemäß `Signal Bar` speichern.
3. Wenn die vorherige Bar PVT über der Signallinie zeigte, jede Short-Position schließen. Wenn zusätzlich der zuletzt gespeicherte PVT unter oder gleich der Signallinie ist, eine Long-Position öffnen (wenn Long-Einstiege aktiviert sind).
4. Wenn die vorherige Bar PVT unter der Signallinie zeigte, jede Long-Position schließen. Wenn zusätzlich der zuletzt gespeicherte PVT über oder gleich der Signallinie ist, eine Short-Position öffnen (wenn Short-Einstiege aktiviert sind).
5. Nach dem Einstieg in einen Trade werden optionale Stop-Loss- und Take-Profit-Orders mit den konfigurierten Abständen (in Preisschritten) angehängt.
6. Das Geldmanagement imitiert den ursprünglichen Expertenberater: Neue Orders verwenden das konfigurierte Basis-`Order Volume` und schließen das entgegengesetzte Exposure ein, um beim Richtungswechsel vollständig umzukehren.

## Parameter
- **Order Volume** – Basisvolumen für neue Orders und Umkehrungen.
- **Stop Loss** – Abstand in Preisschritten für den Schutz-Stop (0 deaktiviert ihn).
- **Take Profit** – Abstand in Preisschritten für das Gewinnziel (0 deaktiviert es).
- **Allow Buy Entry / Allow Sell Entry** – Long- oder Short-Positionen eröffnen erlauben.
- **Allow Buy Exit / Allow Sell Exit** – Automatisches Schließen bestehender Positionen bei entgegengesetztem Setup erlauben.
- **Candle Type** – Zeitrahmen für Indikatorberechnungen.
- **Volume Source** – Tick- oder reales Volumen für PVT-Gewichtung wählen.
- **Smoothing Method / Length / Phase** – Gleitender Durchschnitt angewendet auf die PVT-Linie. Der Phase-Parameter wird nur von Jurik-artigen Methoden verwendet.
- **Applied Price** – Preisformel für den PVT (Schlusskurs, Eröffnung, Trendfolge, DeMark usw.).
- **Signal Bar** – Historischer Versatz (in Bars) für die Kreuzungsbewertung, der die MT5-Implementierung reproduziert.

## Hinweise
- Die Strategie verarbeitet nur abgeschlossene Kerzen, um die Indikatorstabilität zu gewährleisten.
- Jurik-artiges Glätten verwendet Reflexion, um den Phase-Parameter weiterzuleiten, wenn der Indikator ihn freigibt.
- Wenn weder Tick- noch reales Volumen verfügbar ist, fällt die Strategie auf Nullvolumen zurück, um fehlerhafte Akkumulationen zu verhindern.
- Der optionale `StartProtection`-Aufruf aktiviert das integrierte Positionsmonitoring von StockSharp, entsprechend der einzelnen Aufruf im ursprünglichen Expertenberater.
