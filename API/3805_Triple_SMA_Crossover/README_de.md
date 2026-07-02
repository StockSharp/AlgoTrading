# Dreifache SMA Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Triple-SMA-Crossover-Strategie repliziert den ursprünglichen MQL-Expertenberater `3sma.mq4`. Das System analysiert drei einfache gleitende Durchschnitte (SMA), die auf der Grundlage des Schlusskurses berechnet werden, und handelt, wenn der kurzfristige Trend mit den mittel- und langfristigen Durchschnittswerten übereinstimmt. Bei der Konvertierung bleiben die ursprünglichen Handelsregeln erhalten, während sie gleichzeitig an die übergeordnete Strategie StockSharp API angepasst werden.

## Handelslogik
1. Berechnen Sie drei SMAs mit konfigurierbaren Zeiträumen.
2. Verlassen Sie bestehende Long-Positionen, wenn der schnelle SMA unter den mittleren SMA fällt.
3. Bestehende Short-Positionen verlassen, wenn der schnelle SMA über den mittleren SMA steigt.
4. Geben Sie eine neue Long-Position ein, wenn:
   - Schnell SMA liegt mindestens um die konfigurierte Spanne über dem Mittelwert SMA.
   - Der mittlere SMA liegt mindestens um die konfigurierte Spanne über dem langsamen SMA.
   - Derzeit ist keine Long-Position offen.
5. Geben Sie eine neue Short-Position ein, wenn:
   - Schnell SMA liegt mindestens um die konfigurierte Spanne unter dem Mittelwert SMA.
   - Der mittlere SMA liegt mindestens um die konfigurierte Spanne unter dem langsamen SMA.
   - Derzeit ist keine Short-Position offen.

## Parameter
- **Kerzentyp** – Primärer Zeitrahmen, der zur Berechnung der gleitenden Durchschnitte verwendet wird.
- **Schnelle SMA-Länge** – Zeitraum für die schnelle SMA (MQL-Eingabe `SMA1`).
- **Medium SMA Länge** – Zeitraum für das Medium SMA (MQL Eingabe `SMA2`).
- **Langsame SMA-Länge** – Zeitraum für die langsame SMA (MQL-Eingabe `SMA3`).
- **SMA Spread Steps** – Zusätzlicher Filter, der erfordert, dass SMAs um eine Reihe von Preisschritten divergieren (MQL Eingabe `SMAspread`).
- **Handelsvolumen** – Auftragsvolumen, das beim Öffnen von Positionen verwendet wird (MQL Eingabe `lots`).

## Notizen
- Die Stop-Loss-Behandlung der Version MQL wird weggelassen, da sie im Quellskript deaktiviert wurde.
- Bei allen Ausstiegen handelt es sich um Marktaufträge, die dem unkomplizierten Verhalten des ursprünglichen Experten entsprechen.
