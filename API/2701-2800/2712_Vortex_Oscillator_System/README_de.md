# Vortex-Oszillator-System-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Das Vortex-Oszillator-System ist eine direkte Portierung des MetaTrader 5-Experten, der auf dem Vortex-Oszillator basiert, um starke Verschiebungen zwischen positiver und negativer Richtungsbewegung zu erfassen. Der Oszillator wird als Differenz zwischen der positiven Vortex-Linie (VI+) und der negativen Vortex-Linie (VI-) berechnet, die auf der ausgewählten Kerzenserie kalkuliert wird. Stark negative Werte zeigen an, dass VI- VI+ dominiert, während stark positive Werte die Führung von VI+ zeigen. Die Strategie interpretiert diese Extremwerte als potenzielle Wendepunkte und reagiert mit Mean-Reversion-artigen Einstiegen, unterstützt durch oszillatorgesteuerte Ausstiege.

## Funktionsweise der Strategie
1. Kerzen werden mit dem konfigurierten Zeitrahmen erstellt und dem integrierten `VortexIndicator` zugeführt.
2. Sobald der Indikator geformt ist, wird der Oszillatorwert als `VI+ - VI-` für jede fertige Kerze abgeleitet.
3. Der Oszillator wird mit benutzerdefinierten Schwellenwerten verglichen:
   - Wenn er unter den Kaufschwellenwert fällt, wird ein Long-Setup erkannt.
   - Wenn er über den Verkaufsschwellenwert steigt, wird ein Short-Setup erkannt.
4. Optionale Filter können Long-Signale auf die Zone zwischen dem Kaufschwellenwert und einem dedizierten Stop-Loss-Level beschränken (und umgekehrt für Short-Signale).
5. Wenn ein neues Setup erscheint, schließt die Strategie jede Gegenposition und eröffnet einen Trade in der Signalrichtung mit dem konfigurierten Volumen.
6. Offene Positionen werden kontinuierlich überwacht. Wenn der Oszillator die konfigurierten Stop-Loss- oder Take-Profit-Grenzen erreicht, wird die Position sofort geschlossen.

Diese Sequenz reproduziert die ursprüngliche MetaTrader-Logik: Trades werden nur auf abgeschlossenen Bars bewertet, beide Richtungen schließen sich gegenseitig aus, und oszillatorbasierte Schutzregeln regeln die Ausstiege.

## Einstiegskriterien
- **Long-Einstieg**
  - Wird ausgelöst, wenn der Oszillator kleiner oder gleich dem Kaufschwellenwert ist.
  - Wenn die Long-Stop-Loss-Option aktiviert ist, muss der Oszillator auch über dem Long-Stop-Loss-Level bleiben.
  - Jede aktive Short-Position wird geschlossen, bevor der Long-Trade eröffnet wird.
- **Short-Einstieg**
  - Wird ausgelöst, wenn der Oszillator größer oder gleich dem Verkaufsschwellenwert ist.
  - Wenn die Short-Stop-Loss-Option aktiviert ist, muss der Oszillator auch unter dem Short-Stop-Loss-Level bleiben.
  - Jede aktive Long-Position wird geschlossen, bevor der Short-Trade eröffnet wird.
- Wenn der Oszillatorwert zwischen den Kauf- und Verkaufsschwellenwerten liegt, werden alle Setups storniert und keine Positionsänderung vorgenommen.

## Ausstiegskriterien
- **Long-Positionen**
  - Sofortiges Schließen, wenn der Oszillator unter das Long-Stop-Loss-Level kreuzt oder es erreicht (wenn aktiviert).
  - Sofortiges Schließen, wenn der Oszillator auf oder über das Long-Take-Profit-Level steigt (wenn aktiviert).
- **Short-Positionen**
  - Sofortiges Schließen, wenn der Oszillator über das Short-Stop-Loss-Level kreuzt oder es erreicht (wenn aktiviert).
  - Sofortiges Schließen, wenn der Oszillator auf oder unter das Short-Take-Profit-Level fällt (wenn aktiviert).

Die Ausstiegsprüfungen werden nach jedem Kerzenschluss durchgeführt, um eine getreue Nachbildung der MT5-Überwachungsschleife zu gewährleisten.

## Parameter
- **Vortex Length** – Rückblickperiode für den Vortex-Indikator (Standard 14).
- **Candle Type** – Zeitrahmen für die Erstellung der dem Indikator zugeführten Kerzen.
- **Use Buy Stop Loss** – Aktiviert den oszillatorbasierten Stop-Loss-Filter und -Ausstieg für Long-Trades.
- **Use Buy Take Profit** – Aktiviert den oszillatorbasierten Take-Profit-Ausstieg für Long-Trades.
- **Use Sell Stop Loss** – Aktiviert den oszillatorbasierten Stop-Loss-Filter und -Ausstieg für Short-Trades.
- **Use Sell Take Profit** – Aktiviert den oszillatorbasierten Take-Profit-Ausstieg für Short-Trades.
- **Buy Threshold** – Oszillatorwert, der einen Long-Einstieg qualifiziert (Standard -0.75).
- **Buy Stop Loss Level** – Oszillatorwert, der Long-Positionen schließt, wenn die Long-Stop-Loss-Option aktiv ist (Standard -1.00).
- **Buy Take Profit Level** – Oszillatorwert, der Long-Positionen schließt, wenn die Long-Take-Profit-Option aktiv ist (Standard 0.00).
- **Sell Threshold** – Oszillatorwert, der einen Short-Einstieg qualifiziert (Standard 0.75).
- **Sell Stop Loss Level** – Oszillatorwert, der Short-Positionen schließt, wenn die Short-Stop-Loss-Option aktiv ist (Standard 1.00).
- **Sell Take Profit Level** – Oszillatorwert, der Short-Positionen schließt, wenn die Short-Take-Profit-Option aktiv ist (Standard 0.00).
- **Volume** – Handelsgröße für neue Positionen (Standard 0.1, entsprechend dem originalen Experten).

## Implementierungshinweise
- Die Verarbeitung erfolgt ausschließlich auf abgeschlossenen Kerzen, um Signalduplizierungen innerhalb desselben Bars zu vermeiden.
- Oszillatorschwellenwerte können dank der in den Parameter-Metadaten bereitgestellten Bereiche optimiert werden.
- Die Strategie dreht Positionen automatisch um, indem sie eine Marktorder sendet, die groß genug ist, um die Gegenseite zu schließen und die neue Exponierung zu etablieren.
- Stop-Loss- und Take-Profit-Funktionen arbeiten unabhängig; die Aktivierung einer erfordert nicht die andere.
