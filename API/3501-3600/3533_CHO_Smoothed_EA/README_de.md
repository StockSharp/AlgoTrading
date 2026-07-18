# CHO hat die EA-Strategie geglättet
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert die Logik des ursprünglichen Expert Advisors „CHO Smoothed EA“. Es beobachtet die Überkreuzungen des Chaikin-Oszillators bei abgeschlossenen Kerzen und glättet den Oszillator mit einem konfigurierbaren gleitenden Durchschnitt. Optionale Filter beschränken den Handel auf eine bestimmte Sitzung, schränken die Handelsrichtung ein und validieren Signale mit Nulllinienbestätigung. Wenn ein Signal akzeptiert wird, sendet die Strategie eine Marktorder und verwaltet die Position mithilfe fester Distanzen in Punkten für Stop-Loss, Take-Profit und Trailing-Schutz.

## Handelslogik
- Die Werte des Chaikin-Oszillators werden für jede fertige Kerze mit konfigurierbaren schnellen und langsamen Perioden berechnet.
- Ein gleitender Durchschnitt des Oszillators erzeugt die Signallinie. Der Zeitraum und die Art des gleitenden Durchschnitts können angepasst werden.
- Lange Einträge treten auf, wenn der Oszillator die geglättete Linie überschreitet. Kurze Einträge erfolgen an der gegenüberliegenden Kreuzung. Signale können umgekehrt werden, um entgegen der ursprünglichen Richtung zu handeln.
- Wenn der Nullpegelfilter aktiviert ist, müssen beide Oszillatorwerte für Long-Trades unter Null und für Short-Trades über Null liegen.
- Die Strategie kann entgegengesetzte Positionen automatisch schließen, bevor ein neuer Handel eingegangen wird, oder Signale ignorieren, bis die aktuelle Position flach ist. Es kann auch ein Einzelpositionsmodus erzwungen werden.
- Der Handel kann auf ein tägliches Zeitfenster beschränkt werden. Windows, die Mitternacht überschreiten, werden unterstützt.
- Nach einem Einstieg speichert die Strategie den Einstiegspreis, wandelt die konfigurierten Punktabstände in Preisversätze um und überwacht Kerzen auf Stop-Loss-, Take-Profit- und Trailing-Stop-Ereignisse.

## Risikomanagement
- Stop-Loss- und Take-Profit-Level werden aus dem Einstiegspreis unter Verwendung von Punktabständen multipliziert mit dem Preisschritt des Instruments berechnet.
- Der Trailing-Stop wird aktiviert, nachdem der Preis um den konfigurierten Trailing-Schritt ansteigt, und folgt dann mit der Trailing-Distanz.
- Wenn ein Schutzniveau erreicht wird, wird die Position sofort mit einer Marktorder geschlossen und alle Risikoniveaus werden zurückgesetzt.

## Parameter
- **Kerzentyp** – Zeitrahmen, der zum Erstellen der Kerzen für die Indikatorberechnungen verwendet wird.
- **Schnelle Periode / Langsame Periode** – Schnelle und langsame Perioden des Chaikin-Oszillators.
- **Signal-MA-Periode / Signal-MA-Typ** – Glättungseinstellungen für den gleitenden Durchschnitt, die auf den Oszillator angewendet werden.
- **Nullwert verwenden** – Vor dem Handel müssen beide Oszillatorwerte auf der richtigen Seite von Null liegen.
- **Handelsmodus** – nur Long, nur Short oder beide Richtungen zulassen.
- **Umgekehrte Signale** – Long- und Short-Einträge tauschen.
- **Gegenüberliegende Positionen schließen** – bestehende entgegengesetzte Positionen schließen, bevor ein neuer Handel eröffnet wird.
- **Nur eine Position** – Einträge verhindern, wenn eine Position bereits offen ist.
- **Zeitsteuerung / Startzeit / Endzeit verwenden** – aktivieren und konfigurieren Sie das tägliche Handelsfenster.
- **Stop Loss (Pkte)** – Abstand in Punkten für den Schutzstopp.
- **Take Profit (Pkte)** – Abstand in Punkten für Gewinnziele.
- **Trailing Stop (Punkte)** – Trailing Stop-Distanz in Punkten.
- **Trailing Step (Punkte)** – minimale günstige Bewegung (in Punkten) vor der Bewegung des Trailing Stops.

## Zusätzliche Hinweise
- Legen Sie die Eigenschaft `Volume` der Strategie fest, bevor Sie sie starten, um die Handelsgröße zu steuern.
- Da die Strategie Marktaufträge ausgibt, stellen Sie eine ausreichende Liquidität sicher und berücksichtigen Sie Slippage in Live-Umgebungen.
- Wenn die Start- und Endzeiten des Handelsfensters gleich sind, bleibt die Strategie inaktiv und entspricht dem ursprünglichen Verhalten des Expert Advisors.
