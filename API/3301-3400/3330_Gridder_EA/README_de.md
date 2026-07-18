# Gridder-EA-Strategie (aus MQL4 portiert)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Der ursprüngliche GridderEA ist ein Multi-Symbol-Grid-Trading-Expert-Advisor für MetaTrader 4. Dieser StockSharp-Port behält die Kernkonzepte bei: progressive Abstände, adaptives Lot-Sizing, Basket-Take-Profit und Notfall-Hedging, konzentriert sich aber auf ein einzelnes Instrument, das von der Host-Strategie verwaltet wird. Die Strategie abonniert einen konfigurierbaren Kerzenstrom, beobachtet abgeschlossene Bars und eröffnet Averaging-Trades, wenn sich der Preis um eine in Pips definierte Distanz vom letzten Referenzniveau entfernt.

## Handelslogik
1. **Grid-Progression:** Ein Basisschritt (in Pips) definiert die minimale Preisbewegung, die vor Platzierung eines neuen Trades erforderlich ist. Jede zusätzliche Order kann diesen Schritt geometrisch oder exponentiell skalieren, um das Grid bei steigender Volatilität zu spreizen.
2. **Lot-Progression:** Die erste Order verwendet das Anfangsvolumen. Nachfolgende Orders multiplizieren das vorherige Volumen gemäß dem konfigurierten Lot-Progressionsmodus (statisch, geometrisch oder exponentiell).
3. **Basket-Ziele:** Nicht realisierte Gewinne und Verluste werden in Kontowährung gemessen, indem die Preisabweichung jedes offenen Trades mit dem Step-Wert des Instruments kombiniert wird. Überschreitet der Gesamtgewinn das Ziel pro Lot, werden alle Positionen geschlossen. Ebenso kann ein Zielverlust pro Lot den Basket als Schutzstop liquidieren.
4. **Notfallmodus:** Wenn die Anzahl der Trades auf einer Seite den Notfallauslöser erreicht, kann die Strategie einen Hedge-Trade in Größe eines Bruchteils des kumulierten Volumens eröffnen. Dies imitiert den "Emergency Mode" aus der MQL-Version und hilft, Drawdowns zu begrenzen.
5. **Positionsschutz:** `StartProtection()` wird beim Start aufgerufen, damit die Basisstrategie unerwartete Positionsänderungen überwacht und sich mit dem Börsenzustand neu synchronisiert.

Die StockSharp-Implementierung vermeidet die Manipulation großer historischer Sammlungen und verarbeitet nur abgeschlossene Kerzen, was dem Verhalten des ursprünglichen Experten auf fertigen Bars entspricht.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| **Initial Volume** | Volumen der allerersten Grid-Order. |
| **Volume Multiplier** | Faktor zur Berechnung des nächsten Ordervolumens, wenn Lot-Progression geometrisch oder exponentiell ist. |
| **Grid Step (pips)** | Basisdistanz (in Pips) zwischen aufeinanderfolgenden Einstiegen. |
| **Step Multiplier** | Skalierungsfaktor für Grid-Abstände bei geometrischer oder exponentieller Schrittprogression. |
| **Target Profit / Lot** | Nicht realisiertes Gewinnziel pro Lot. Bei Erreichen werden alle offenen Trades geschlossen. |
| **Target Loss / Lot** | Nicht realisierte Verlustschwelle pro Lot. Bei Erreichen werden alle Trades geschlossen, um Drawdown zu begrenzen. |
| **Max Orders Per Side** | Begrenzt die Anzahl erlaubter Averaging-Trades je Marktseite. `0` deaktiviert das Limit. |
| **Allow Long / Allow Short** | Aktiviert oder deaktiviert Kauf-/Verkaufsseiten unabhängig. |
| **Step Mode** | Bestimmt, wie der Schritt wächst: statisch, geometrisch oder exponentiell. |
| **Lot Mode** | Bestimmt, wie das Ordervolumen wächst: statisch, geometrisch oder exponentiell. |
| **Use Emergency Mode** | Aktiviert die Hedge-Logik, die gegen übergroße Baskets schützt. |
| **Emergency Trigger** | Anzahl Orders auf einer Seite, die den Hedge aktiviert. |
| **Hedge Volume Factor** | Bruchteil des gesamten Seitenvolumens, der als Hedge-Order platziert wird, wenn der Notfallmodus auslöst. |
| **Candle Type** | Zeitrahmen des Kerzenabonnements für Grid-Berechnungen. |

## Unterschiede zum ursprünglichen EA
- Der Port verwaltet jeweils eine einzelne Security; hängen Sie mehrere Strategieinstanzen an, um mehrere Instrumente zu handeln und das Multi-Symbol-Verhalten des MQL-Experten nachzubilden.
- Bildschirm-Panels und Chart-Anmerkungen aus MetaTrader werden nicht reproduziert; nutzen Sie StockSharp-Chartbereiche zur Visualisierung von Kerzen und eigenen Trades.
- Geldmanagement-Presets und detaillierte Teil-Schließungsprofile werden in die einheitliche Basket-Gewinn-/Verlustlogik vereinfacht.

## Nutzungshinweise
1. Konfigurieren Sie gewünschten Kerzentyp, Volumen und Grid-Abstand in den Konstruktorparametern (über UI oder Optimierungsinterface).
2. Starten Sie die Strategie, sobald die Security mit einem Live- oder Simulationsboard verbunden ist. Die Strategie abonniert automatisch die gewählten Kerzen.
3. Überwachen Sie Notfallauslöser und Hedge-Faktor, um die Aggressivität der Erholungsphase anzupassen. Ein höherer Hedge-Faktor bringt die Nettoposition schneller zurück Richtung neutral, reduziert aber die Profitabilität.
4. Kombinieren Sie mit StockSharp-Risikokontrollen (Portfolioschutz, Max-Position-Wächter usw.) für zusätzliche Sicherheit.

## Beispiel für Notfall-Hedge
Angenommen, die Strategie hat fünf Averaging-Kauforders mit zunehmend größeren Volumen geöffnet. Wenn der Notfallauslöser auf fünf und der Hedge-Volumenfaktor auf 0,5 gesetzt ist, sendet die Strategie beim Füllen der fünften Kauforder automatisch einen Marktverkauf in Höhe der Hälfte des gesamten Long-Volumens. Dies spiegelt die MQL-Logik wider, die den Basket teilweise sperrt und auf einen Mean-Reversion-Ausstieg wartet.

## Optimierungstipps
- Optimieren Sie **Grid Step (pips)** und **Volume Multiplier** gemeinsam; kleine Schritte benötigen konservative Multiplikatoren, um ausufernde Exposure zu vermeiden.
- Verwenden Sie **Target Profit / Lot**, um MetaTrader-Dollarziele in die StockSharp-Umgebung zu übertragen, ohne auf geschlossene Tradehistorie angewiesen zu sein.
- Stimmen Sie **Emergency Trigger** und **Hedge Volume Factor** auf die Volatilität des gehandelten Instruments ab. Höhere Volatilität profitiert meist von früherem Hedging.

## Sicherheitsempfehlungen
- Testen Sie ausführlich im Simulator, bevor Sie in Produktion gehen.
- Überwachen Sie brokerspezifische Kontraktgrößen, um sicherzustellen, dass gerundetes Volumen der tatsächlichen Lot-Granularität entspricht.
- Kombinieren Sie mit Stop-out-Regeln (z. B. über den Host-Roboter), um katastrophale Verluste in Trendmärkten zu verhindern, in denen Grids große Positionen ansammeln können.
