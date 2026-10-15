# Globaler Stop-Timer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Globale Stop-Timer-Strategie ist eine Risikomanagement-Überlagerung, die aus dem MetaTrader-Experten `Exp_GStop_Tm` konvertiert wurde.
Sie überwacht kontinuierlich den Portfoliowert bei jeder abgeschlossenen Kerze und hält den Handel an, sobald ein globales Gewinnziel
oder ein Verlustlimit erreicht wird. Zusätzlich kann sie den Handel auf ein benutzerdefiniertes Zeitfenster beschränken und alle offenen
Positionen zwangsweise schließen, wenn das Fenster geschlossen ist.

## Funktionsweise

- Wenn die Strategie startet, zeichnet sie den anfänglichen Portfolio-Saldo als Referenzpunkt auf.
- Jedes Mal, wenn die abonnierte Kerzenserie schließt, liest die Strategie den aktuellen Portfoliowert und berechnet die
  Differenz zum Anfangssaldo.
- Je nach ausgewähltem `StopCalculationMode` wird die Differenz in einen Prozentsatz umgerechnet oder als Währung belassen.
- Wenn der Verlust `StopLoss` übersteigt oder der Gewinn `TakeProfit` übersteigt, tritt die Strategie in den gestoppten Zustand ein, protokolliert das
  Ereignis und sendet Marktorders, um verbleibende Positionen zu schließen.
- Wenn das optionale Handelsfenster aktiviert ist und die aktuelle Zeit das Fenster verlässt, versucht die Strategie auch, die
  Position zu glätten. Sobald die Positionsgröße null wird, wird das Stop-Flag zurückgesetzt, sodass der Handel innerhalb
  des nächsten gültigen Fensters wieder aufgenommen werden kann.

Die Strategie öffnet selbst keine neuen Positionen. Sie ist darauf ausgelegt, andere Strategien oder manuelle Trades zu überwachen und die
Konto vor übermäßigem Drawdown zu schützen oder kontoweite Gewinne zu sichern.

## Handelsfenster-Logik

Das Handelsfenster repliziert die ursprüngliche Expertenlogik:

- Wenn die Startsstunde kleiner als die Endstunde ist, ist der Handel zwischen der Startminute (inklusiv) und der
  Endminute (exklusiv) am selben Tag erlaubt.
- Wenn Start- und Endstunde gleich sind, ist der Handel nur erlaubt, wenn die aktuelle Minute zwischen `StartMinute`
  (inklusiv) und `EndMinute` (exklusiv) liegt.
- Wenn die Startstunde größer als die Endstunde ist, erstreckt sich die Session über Mitternacht hinaus. Der Handel ist von der Startzeit
  bis Mitternacht aktiviert und setzt von Mitternacht bis zur Endzeit am folgenden Tag fort.

## Parameter

- `StopCalculationMode` – wählen zwischen prozentbasierten oder währungsbasierten globalen Stops.
- `StopLoss` – globaler Verlustschwellenwert. Wird als Prozentsatz behandelt, wenn der Prozentmodus aktiv ist, sonst als Kontowährung.
- `TakeProfit` – globales Gewinnziel. Verwendet dieselbe Einheit wie `StopLoss`.
- `UseTradingWindow` – Session-Filter aktivieren oder deaktivieren.
- `StartHour` / `StartMinute` – Startzeit des erlaubten Handelsfensters.
- `EndHour` / `EndMinute` – Endzeit des erlaubten Handelsfensters.
- `CandleType` – Kerzenserie, die definiert, wie oft der Kontozustand ausgewertet wird.

## Hinweise

- Da Stop-Checks beim Kerzenschluss stattfinden, einen kleinen Zeitrahmen verwenden (z.B. eine Minute), wenn schnelle Reaktion
  erforderlich ist.
- Die Strategie schließt nur die von dieser Strategieinstanz verwaltete Position. Separate Instanzen ausführen, wenn mehrere
  Wertpapiere individuelle Überwachung benötigen.
- Zusammen mit anderen Handelsstrategien verwenden, indem es als übergeordnete Strategie angehängt oder auf demselben Instrument ausgeführt wird, um
  kontoweiten Schutz zu bieten.
