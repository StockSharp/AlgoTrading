# Umkehrstrategie mit extremer Stärke
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Gegentrendsystem, konvertiert vom Expertenberater MetaTrader EXSR.
- Kombiniert Bollinger-Bänder und RSI-Extreme, um Erschöpfungsbewegungen zu lokalisieren.
- Verwendet eine prozentuale Positionsgröße mit festem Stop-Loss und Take-Profit in Pips.

## Handelslogik
1. Abonnieren Sie die konfigurierte Kerzenserie (standardmäßig 1-Stunden-Kerzen).
2. Berechnen Sie eine Bollinger-Band-Hüllkurve (Periode, Abweichung) und einen RSI-Oszillator.
3. Wenn eine Kerze schließt:
   - Ein Long-Setup erfordert: RSI unter dem überverkauften Niveau, aber über Null, das Kerzentief unter dem unteren Band und einen bullischen Körper (Schlusskurs über Eröffnung).
   - Ein Short-Setup erfordert: RSI über dem überkauften Niveau, die Kerze hoch über dem oberen Band und einen rückläufigen Körper (Schluss unter Eröffnung).
4. Es kann jeweils nur eine Stelle offen sein. Die Gegenbelichtung wird vor dem Umkehren geschlossen.
5. Stopps und Ziele werden aus dem Füllpreis mit Pips im MetaTrader-Stil projiziert. Die Engine überwacht nachfolgende Kerzen und wird beendet, wenn eine der Ebenen berührt wird.

## Money-Management
- Die Auftragsgröße wird standardmäßig auf die `Volume`-Eigenschaft der Strategie festgelegt. Wenn es Null ist, leitet die Strategie das Volumen aus `RiskPercent` und der Stoppdistanz ab.
- Das Risiko wird aus dem aktuellen Portfolio-Eigenkapital berechnet (Rückfall auf den Saldo/Anfangswert). Die Stop-Distanz wird mithilfe des Step- und Step-Preises des Instruments in Preis- oder Geldeinheiten umgerechnet.
- Die Lautstärke wird auf den Lautstärkeschritt sowie die minimalen und maximalen Einschränkungen des Instruments normalisiert.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| Risikoprozentsatz | Prozentsatz des pro Trade riskierten Eigenkapitals. | 1 % |
| Stop-Loss (Pips) | Stoppdistanz in MetaTrader Pips. | 150 |
| Take-Profit (Pips) | Take-Profit-Distanz in Pips. | 300 |
| Bollinger Zeitraum | Kerzen für Bollinger-Bands. | 20 |
| Bollinger Abweichung | Standardabweichungsmultiplikator. | 2,0 |
| RSI Zeitraum | Kerzen für RSI verwendet. | 14 |
| RSI Überkauft | Das Niveau von RSI gilt als extrem überkauft. | 80 |
| RSI Überverkauft | Das Niveau von RSI gilt als extrem überverkauft. | 20 |
| Kerzentyp | Kerzenzeitrahmen für die Analyse. | 1 Stunde |

## Notizen
- Stellen Sie sicher, dass das ausgewählte Symbol Preisschritt, Schrittpreis und Volumenschritt anzeigt, um eine genaue Größenbestimmung zu ermöglichen. Bei Nichtverfügbarkeit greift die Strategie auf angemessene Standardwerte zurück.
- Das Risikomanagement wird auch dann ausgelöst, wenn der Handel vorübergehend deaktiviert ist, sodass Schutzausstiege aktiv bleiben.
- Die Logik verarbeitet nur abgeschlossene Kerzen und spiegelt den ursprünglichen EA wider, der auf dem vorherigen Balken funktioniert.
