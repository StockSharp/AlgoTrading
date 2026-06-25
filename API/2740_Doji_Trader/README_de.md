# Doji Trader-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert die Kernlogik des klassischen **Doji Trader** Expert Advisors.
Sie überwacht abgeschlossene Kerzen auf kompakt-körprige Doji-Muster und wartet auf einen
Ausbruchs-Schlusskurs jenseits des Doji-Bereichs, um in die Ausbruchsrichtung in den Markt einzusteigen.

## Handelslogik

1. Nur abgeschlossene Kerzen werden verarbeitet. Der Standard-Zeitrahmen ist 1 Stunde, kann aber
   über den `CandleType`-Parameter angepasst werden.
2. Der Handel ist nur erlaubt, wenn die Schlusszeit der letzten Kerze innerhalb des
   konfigurierbaren Session-Fensters `[StartHour, EndHour)` in Exchange-Zeit liegt.
3. Der Algorithmus hält die drei zuletzt abgeschlossenen Kerzen im Speicher. Die gerade geschlossene Kerze
   wird gegen die zwei Kerzen verglichen, die ihr vorausgingen (`-2` und `-3`).
4. Eine Kerze gilt als Doji, wenn die absolute Differenz zwischen Eröffnung und Schluss
   kleiner als `MaximumDojiHeight * pip` ist, wobei der Pip-Wert aus dem Instrument-Preisschritt
   abgeleitet wird (3- oder 5-stellige Quotes werden automatisch mit ×10 skaliert).
5. Wenn die neueste Kerze **über** dem Hoch des jüngsten qualifizierenden Dojis schließt, öffnet
   die Strategie (oder wechselt zu) eine Long-Position. Wenn sie **unter** dem Doji-Tief schließt,
   öffnet sie eine Short-Position. Kein Trade wird platziert, wenn der Preis innerhalb des Doji-Bereichs bleibt.
6. Die Positionsgröße wird aus der `Volume`-Eigenschaft der Strategie entnommen. Wenn ein Umkehrsignal
   erscheint, sendet der Algorithmus genug Volumen, um die vorherige Position zu schließen und
   die gewünschte Exposition in der neuen Richtung herzustellen, sodass nur eine Netto-Position offen bleibt.

## Risikomanagement

- Stop-Loss- und Take-Profit-Distanzen werden über `StopLossPips` und `TakeProfitPips` in Pips konfiguriert.
  Ein Wert auf null zu setzen deaktiviert die entsprechende Schutzorder.
- `StartProtection` wird einmal beim Start gestartet und verwendet Market-Orders für Ausstiege, sodass das
  Verhalten die MQL-Implementierung widerspiegelt, die Positionen direkt schloss und wieder öffnete.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Zeitrahmen der verarbeiteten Kerzen. | 1-Stunden-Zeitrahmen |
| `StartHour` | Inklusive Öffnungsstunde des Handelsfensters. | 8 |
| `EndHour` | Exklusive Schlussstunde des Handelsfensters. | 17 |
| `MaximumDojiHeight` | Maximale Körperhöhe (in Pips) für eine Kerze, um als Doji behandelt zu werden. | 1 |
| `StopLossPips` | Schutz-Stop-Distanz in Pips. | 50 |
| `TakeProfitPips` | Gewinnziel-Distanz in Pips. | 50 |

### Zusätzliche Hinweise

- Die Strategie geht davon aus, dass das Plattform-Konto Netto-Positionen verwendet. Wenn Ihr Feed
  gebrochene Pip-Schritte liefert (5- oder 3-stellige Quotes), wird der Pip-Wert mit 10 multipliziert, um
  traditionellen Pip-Messungen zu entsprechen.
- Stellen Sie die gewünschte Lot-Größe in der `Volume`-Eigenschaft vor dem Ausführen der Strategie ein.
- Es sind keine zusätzlichen Indikatoren erforderlich; die Logik hängt nur von rohen Kerzendaten ab.
- Es gibt noch keinen Python-Port; nur die C#-Implementierung existiert.
