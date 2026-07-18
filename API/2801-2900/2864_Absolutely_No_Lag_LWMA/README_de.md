# Absolut verzögerungsfreie LWMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den MetaTrader-Expertenberater **Exp_AbsolutelyNoLagLwma**, indem ein doppelter gewichteter gleitender Durchschnitt (LWMA) auf Kerzendaten angewendet wird. Die Indikatorausgabe ist farbcodiert: Grün (2) für eine aufwärtsgerichtete Steigung, Grau (1) für flach und Magenta (0) für eine abwärtsgerichtete Steigung. Handelsentscheidungen basieren auf Übergängen zwischen diesen Farbzuständen. Die StockSharp-Implementierung verwendet die High-Level-API, abonniert Zeitrahmen-Kerzen und sendet Marktaufträge gemäß der erkannten Trendrichtung.

## Handelslogik
### Indikator-Pipeline
1. Die gewünschte Preisserie auswählen, die durch den Parameter *Preistyp* definiert wird.
2. Einen gewichteten gleitenden Durchschnitt (LWMA) mit der konfigurierten *LWMA-Länge* anwenden.
3. Das Ergebnis mit einem zweiten LWMA gleicher Länge glätten.
4. Den geglätteten LWMA-Wert mit dem vorherigen Wert vergleichen, um die Steigungsrichtung zu klassifizieren:
   - **2 (Aufwärtstrend)** – aktueller Wert ist größer als der vorherige Wert.
   - **1 (neutral)** – aktueller Wert entspricht dem vorherigen Wert.
   - **0 (Abwärtstrend)** – aktueller Wert ist kleiner als der vorherige Wert.

### Signalauswertung
- Nur abgeschlossene Kerzen werden verarbeitet. Der Parameter *Signalbalken* verschiebt die Signalauswertung auf historische Kerzen (1 = vorherige abgeschlossene Kerze, 2 = die Kerze davor, etc.). Die Strategie merkt sich auch die Farbe des Balkens, der der ausgewählten Signalkerze vorausgeht, um doppelte Einstiege zu vermeiden.
- **Bullischer Übergang**: Die ausgewählte Signalkerze ist Farbe **2** und die vorherige Kerze ist nicht **2**. Dies öffnet Longs (wenn aktiviert) und schließt bestehende Shorts.
- **Bärischer Übergang**: Die ausgewählte Signalkerze ist Farbe **0** und die vorherige Kerze ist nicht **0**. Dies öffnet Shorts (wenn aktiviert) und schließt bestehende Longs.

### Positionsverwaltung
- Aufträge werden mit Marktaufträgen ausgeführt. Das angeforderte Volumen ist `Volume + |Position|`, wenn die Richtung umgekehrt wird, sodass die entgegengesetzte Position automatisch geschlossen wird.
- Ausstiegssignale können unabhängig von Einstiegen umgeschaltet werden, was nur-Signal- oder nur-Ausstiegs-Verhalten ermöglicht.
- `StartProtection()` wird aktiviert, um die gemeinsame StockSharp-Schutzlogik einzuschalten, sobald die Strategie startet.

## Parameter
- **LWMA-Länge** – Länge der zwei für die Glättung verwendeten LWMAs.
- **Preistyp** – Preisquelle, die den LWMA speist (Schluss, Eröffnung, Hoch, Tief, Median, typisch, gewichtet, vereinfacht, Viertel, Trendfolge-Variationen oder Demark-Preis).
- **Signalbalken** – Anzahl der zurückliegenden abgeschlossenen Kerzen für die Signalauswertung.
- **Long-Einstiege aktivieren** – erlaubt das Öffnen von Long-Positionen bei bullischen Übergängen.
- **Short-Einstiege aktivieren** – erlaubt das Öffnen von Short-Positionen bei bärischen Übergängen.
- **Long-Ausstiege aktivieren** – schließt Long-Positionen wenn der Indikator bärisch wird.
- **Short-Ausstiege aktivieren** – schließt Short-Positionen wenn der Indikator bullisch wird.
- **Kerzentyp** – Zeitrahmen des Kerzenabonnements, das vom Indikator verwendet wird.
- **Volumen** (eingebautes Strategy-Property) – Handelsgröße für neue Einstiege.

## Hinweise
- Der Standard-Zeitrahmen beträgt 4 Stunden, was der ursprünglichen Expertenkonfiguration entspricht, kann aber über den Parameter *Kerzentyp* angepasst werden.
- Es werden keine Take-Profit- oder Stop-Loss-Aufträge automatisch platziert; Benutzer können die Strategie bei Bedarf mit StockSharp-Risikomanagementkomponenten kombinieren.
- Der Python-Port wird absichtlich ausgelassen, wie angefordert.
