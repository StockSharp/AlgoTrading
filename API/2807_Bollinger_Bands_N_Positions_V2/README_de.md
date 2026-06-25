# Bollinger Bands N Positionen v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den Expert Advisor "Bollinger Bands N positions v2" von Vladimir Karputov. Sie arbeitet auf abgeschlossenen Kerzen und sucht nach Preisausbrüchen relativ zur Bollinger-Bands-Hülle. Der StockSharp-Port behält das ursprüngliche Pyramiding-Verhalten, Risikokontrollen und Trailing-Logik bei, während er das Order-Management an das Netting-Modell der Plattform anpasst.

## Handelslogik
- Ein Bollinger-Bands-Indikator (Periode und Abweichung konfigurierbar) wird auf der ausgewählten Kerzenreihe berechnet.
- Wenn der Kerzenschluss über dem oberen Band endet, schließt die Strategie jede aktive Short-Exposure und eröffnet eine zusätzliche Long-Position (bis zur konfigurierten maximalen Anzahl gestapelter Einträge).
- Wenn der Kerzenschluss unter dem unteren Band endet, schließt die Strategie jede aktive Long-Exposure und eröffnet eine zusätzliche Short-Position (ebenfalls durch den Parameter für maximale Einträge begrenzt).
- Die Positionsgröße wird in festen Schritten (der Parameter **Volume**) erhöht, wenn in dieselbe Richtung pyramidisiert wird.
- Der durchschnittliche Einstiegspreis der gestapelten Position wird verfolgt, um Stop-Loss-, Take-Profit- und Trailing-Stop-Niveaus konsistent zu verwalten.

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände werden in Pips eingegeben. Sie werden in absolute Preisoffsets umgerechnet, indem mit dem Instrument-Preisschritt multipliziert wird. Instrumente, die mit 3 oder 5 Dezimalstellen notiert werden, multiplizieren den Schritt automatisch mit 10, um MetaTraders Pip-Größenanpassung zu emulieren.
- Trailing-Stop-Offset und Trailing-Schritt werden ebenfalls in Pips konfiguriert. Der Trailing-Mechanismus aktualisiert den Stop-Preis nur, nachdem der Trade sich um `TrailingStop + TrailingStep` Pips vom aktuellen Durchschnittseinstieg bewegt hat. Jede Aktualisierung verschiebt den Stop um den Trailing-Offset, während der zusätzliche Schrittpuffer respektiert wird, um übermäßige Modifikationen zu vermeiden.
- Schutz-Ausstiegsorders werden innerhalb der Strategie simuliert: Immer wenn eine fertige Kerze das Stop- oder Zielniveau kreuzt, wird die gesamte Position mit Marktorders geschlossen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| **Bollinger Period** | Rückblickperiode für den gleitenden Bollinger-Bands-Durchschnitt. |
| **Bollinger Deviation** | Standardabweichungsmultiplikator für die Bollinger-Hülle. |
| **Max Positions** | Maximale Anzahl gestapelter Einträge pro Richtung. |
| **Volume** | Ordervolumen für jeden einzelnen Einstieg. |
| **Stop Loss (pips)** | Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). |
| **Take Profit (pips)** | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). |
| **Trailing Stop (pips)** | Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing). |
| **Trailing Step (pips)** | Zusätzlicher Gewinn in Pips, der erforderlich ist, bevor der Trailing-Stop erneut bewegt wird. Muss positiv sein, wenn Trailing aktiviert ist. |
| **Candle Type** | Von der Strategie verarbeitete Kerzenreihe. |

## Implementierungshinweise
- Die Strategie verwendet hochstufige Kerzenabonnements mit Indikatorbindung nach den StockSharp-Richtlinien.
- Es werden nur fertige Kerzen verarbeitet, um die ursprüngliche "neue Bar"-Logik aus MetaTrader zu spiegeln.
- Da StockSharp im Netting-Modus arbeitet, schließt die Konvertierung die entgegengesetzte Exposure, bevor eine neue Pyramidenschicht in die andere Richtung geöffnet wird.
- Der Trailing-Schritt muss größer als null bleiben, wenn der Trailing-Stop aktiv ist, was der Sicherheitsprüfung des ursprünglichen Expert Advisors entspricht.
- Die Python-Implementierung ist in dieser Version nicht enthalten.
