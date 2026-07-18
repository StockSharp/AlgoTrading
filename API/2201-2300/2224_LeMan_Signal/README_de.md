# LeMan Signal-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die LeMan Signal-Strategie ist eine Portierung des ursprünglichen MetaTrader LeManSignal-Expertenberaters. Der Ansatz analysiert jüngste Hochs und Tiefs über zwei sequenzielle Perioden, um potenzielle Trendumkehrungen zu erkennen. Wenn bestimmte Muster gefunden werden, wird bei der nächsten Kerze eine Long- oder Short-Position eröffnet.

## Funktionsweise

1. Die Strategie beobachtet abgeschlossene Kerzen des ausgewählten Zeitrahmens.
2. Für den vorherigen Balken werden die höchsten Hochs und niedrigsten Tiefs in zwei aufeinanderfolgenden Bereichen verglichen:
   - `H1` und `H2` sind die Maxima zweier benachbarter Bereiche.
   - `H3` und `H4` sind die Maxima des nächsten Paares von Bereichen.
   - `L1` und `L2` sind die Minima zweier benachbarter Bereiche.
   - `L3` und `L4` sind die Minima des nächsten Paares von Bereichen.
3. Ein **Kauf**-Signal wird ausgelöst, wenn `H3 <= H4` und `H1 > H2`.
4. Ein **Verkauf**-Signal wird ausgelöst, wenn `L3 >= L4` und `L1 < L2`.
5. Aufträge werden zum Marktpreis ausgeführt. Jede offene entgegengesetzte Position wird automatisch geschlossen.
6. Optionales Risikomanagement wird über `StartProtection` mit standardmäßigen Stop-Loss- und Take-Profit-Werten von 1% bzw. 2% angewendet.

## Parameter

- **Period** – Lookback-Länge des Indikators.
- **Signal Bar** – Versatz zur Bestätigung des Signals (Standard 1).
- **Candle Type** – Zeitrahmen der zu analysierenden Kerzen.

## Hinweise

- Die Strategie reagiert nur auf abgeschlossene Kerzen.
- Sie hält keine zusätzlichen Sammlungen vor; interne Puffer sind auf das für Berechnungen notwendige Minimum beschränkt.
- Um die Strategie zu verwenden, fügen Sie sie einem StockSharp-Terminal hinzu, legen Sie das gewünschte Instrument und die Parameter fest und starten Sie die Strategie.
