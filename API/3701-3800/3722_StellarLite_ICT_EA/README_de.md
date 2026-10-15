# StellarLite ICT EA Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
StellarLite ICT EA ist ein diskretionärer Algorithmus, der das Playbook der Prop-Firma „Stellar Lite“ in StockSharp übersetzt. Die Strategie vereint zwei Inner Circle Trader (ICT)-Einstiegsmodelle – Silver Bullet und das 2022-Modell – und automatisiert den Teil-Take-Profit-Plan, der im ursprünglichen MetaTrader-Expertenberater verwendet wurde. Es funktioniert auf jedem Instrument, das Informationen zu Kerzen, Preisschritten und Volumenschritten bereitstellt.

## Kern-Workflow
1. **Richtungsverzerrung durch den höheren Zeitrahmen** – ein gleitender Durchschnitt im ausgewählten höheren Zeitrahmen muss in die Handelsrichtung tendieren und der Preis muss über dem Durchschnitt schließen. Erst nachdem die Verzerrung bestätigt wurde, wird die Logik des unteren Zeitrahmens bewertet.
2. **Bestätigung des Liquiditäts-Sweeps** – Die Strategie überwacht ein konfigurierbares Lookback-Fenster und sucht nach Durchbrüchen bei den jüngsten Höchst- oder Tiefstständen. Silver Bullet erfordert einen Sweep in die Handelsrichtung, während das Modell 2022 einen Anreiz-Sweep in die entgegengesetzte Richtung erfordert.
3. **Market Structure Shift (MSS)** – die letzten drei abgeschlossenen Kerzen müssen eine Verschiebung bestätigen: ein höherer Schlusskurs über dem vorherigen Hoch für Long-Trades oder ein niedrigerer Schlusskurs unter dem vorherigen Tief für Short-Trades.
4. **Fair Value Gap (FVG)-Erkennung** – die Strategie scannt die letzten zehn Kerzen auf bullische oder bärische Ungleichgewichte, die durch Verschiebungskerzen verursacht werden. Der Einstieg ist nur zulässig, wenn der aktuelle Schlusskurs innerhalb der erkannten Lücke liegt.
5. **NDOG/NWOG-Filter** – die aktuelle Kerze muss ein Balken mit enger Spanne sein. Sein Hoch-Tief-Bereich darf `AtrThreshold` multipliziert mit dem Wert `AverageTrueRange` nicht überschreiten.
6. **Einstieg, Stop und Ziele** – der Einstiegspreis wird entweder in der Mitte der Lücke oder am OTE-Retracement (Optimal Trade Entry) platziert, das durch den Verhältnisparameter Fibonacci definiert wird. Der Schutzstopp liegt jenseits der aktuellen Swing-Liquidität und drei Take-Profit-Niveaus werden anhand der konfigurierten Risiko-Ertrags-Verhältnisse prognostiziert.
7. **Handelsmanagement** – die Größe der Position richtet sich nach dem gewählten Risikoprozentsatz oder fällt auf das Strategievolumen zurück. Wenn TP1, TP2 und TP3 erreicht werden, schließt die Strategie standardmäßig 50 %, 25 % und 25 % der Position, verschiebt den Stop auf die Gewinnschwelle nach TP1 (mit einem optionalen Offset), aktiviert einen Trailing Stop nach TP2 und liquidiert den Rest bei TP3 oder bei einem Stop-Treffer.

## Parameter
- **Einstiegskerze (`CandleType`)** – Kerzen mit niedrigerem Zeitrahmen, die für Einstiegssignale verwendet werden.
- **Höherer Zeitrahmen (`HigherTimeframeType`)** – Kerzen speisen den gleitenden Durchschnitt der Tendenz.
- **Höherer MA-Zeitraum (`HigherMaPeriod`)** – Länge des gleitenden Durchschnitts zur Erkennung von Verzerrungen.
- **ATR Zeitraum (`AtrPeriod`)** – Lookback für den ATR-Konsolidierungsfilter.
- **Liquiditätsrückblick (`LiquidityLookback`)** – Anzahl der überprüften Kerzen, um Liquiditätspools zu lokalisieren.
- **ATR Schwellenwert (`AtrThreshold`)** – maximal zulässiger Kerzenbereich als Bruchteil von ATR.
- **TP1/TP2/TP3 Risiko-Ertrag (`Tp1Ratio`, `Tp2Ratio`, `Tp3Ratio`)** – Risiko-Ertrags-Multiplikatoren für Ziele.
- **TP1/TP2/TP3-Abschluss % (`Tp1Percent`, `Tp2Percent`, `Tp3Percent`)** – Teilabschlussprozentsätze.
- **Break Even After TP1 (`MoveToBreakEven`)** – schaltet die Break-Even-Anpassung um.
- **Break Even Offset (`BreakEvenOffset`)** – Anzahl der Preisschritte, die beim Verschieben des Stops addiert oder subtrahiert werden.
- **Trailing Distance (`TrailingDistance`)** – Trailing-Stop-Distanz (in Preisschritten), aktiviert nach TP2.
- **Verwenden Sie Silver Bullet/Modell 2022 (`UseSilverBullet`, `Use2022Model`)** – aktivieren oder deaktivieren Sie jedes Setup.
- **Verwenden Sie den OTE-Eintrag (`UseOteEntry`)** – berechnen Sie den Eintrag innerhalb der optimalen Handelseintrittszone.
- **Risiko % (`RiskPercent`)** – Prozentsatz des Eigenkapitals, das pro Trade riskiert wird, um die Positionsgröße abzuleiten.
- **OTE-Untergrenze (`OteLowerLevel`)** – Fibonacci-Koeffizient für das OTE-Niveau.

## Praktische Hinweise
- Die Strategie erfordert fertige Kerzen; Stellen Sie sicher, dass der Datenfeed Abschlusspreise und Volumenschritte liefert.
- Die Positionsgröße greift auf den Strategieparameter `Volume` zurück, wenn der Portfoliowert oder die Tickwertinformationen nicht verfügbar sind.
- Liquiditätserkennung und MSS-Logik basieren auf dem aktuellsten Verlaufscache (standardmäßig 20 Kerzen); Ermöglichen Sie der Strategie, genügend Daten zu sammeln, bevor Signale erwartet werden.
- Bei Teilausgängen wird der Lautstärkeschritt des Instruments berücksichtigt. Wenn der angeforderte Bruchteil kleiner als das minimal handelbare Volumen ist, wird der Schlusskurs übersprungen.
- Die Trailing-Logik aktualisiert den Stop weiterhin nur in Gewinnrichtung und lockert niemals bestehende Risikokontrollen.

## Dateien
- `CS/StellarLiteIctEaStrategy.cs` – Umsetzung der StockSharp-Strategie.
- `README.md` – Englische Dokumentation.
- `README_zh.md` – Vereinfachte chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.
