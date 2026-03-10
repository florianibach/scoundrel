# Definition of Done (DoD) – Scoundrel R1 Classic Mode

Diese DoD gilt für **alle R1-User-Stories** (Classic Mode / Vanilla) und ist erst erfüllt, wenn **alle Punkte** nachweisbar erledigt sind.

## 1) Fachliche Vollständigkeit
- Story-Ziel, Scope und Nicht-Ziele sind klar dokumentiert.
- Alle zugehörigen Acceptance Criteria sind implementiert und einzeln überprüfbar.
- Fachliche Begriffe sind einheitlich (Run, Room, Monster, Weapon, Potion, Ruleset `vanilla`).
- Endbedingungen (Win/Loss) und Run-Regeln (kein doppelter Run) sind vollständig umgesetzt.

## 2) Funktionale Qualität
- Happy Path und relevante Edge Cases funktionieren (z. B. HP-Grenzen, Deck-Ende, doppelte Endevents).
- Ungültige Aktionen werden sauber abgewiesen und erzeugen keinen inkonsistenten Zustand.
- Nach Run-Ende sind weitere Aktionen technisch blockiert.
- Session-Daten und Profilstatistiken werden konsistent aktualisiert.

## 3) Daten & Persistenz
- Session-Datensatz wird pro abgeschlossenem Run genau einmal gespeichert.
- Pflichtfelder vorhanden: `ruleset`, `result`, `turns`, `runsUsed`, `hpRemaining`, Spielerreferenz, Zeitstempel.
- Profilfelder korrekt gepflegt: `gamesPlayed`, `wins`, `winRate`, `bestStreak`, `achievements`.
- Idempotenz/Transaktionssicherheit ist umgesetzt (keine Doppelzählung bei Retries).

## 4) API- und UI-Qualität
- API-Verträge (Request/Response, Statuscodes, Fehlermeldungen) sind dokumentiert und stabil.
- UI zeigt stets den aktuellen Spielzustand verständlich an (HP, Room, aktive Waffe, RunsUsed, Endstatus).
- Nutzerfeedback bei zentralen Events vorhanden (Kampfresultat, Heilung, Achievement-Unlock, Win/Loss).
- Leaderboard zeigt nur Classic-Mode-Daten (`vanilla`) und klar definierte Rankings.

## 5) Tests & Nachweise
- Unit-Tests für Kernregeln vorhanden (Deckbau, Kampf, Waffendegradation, Run-Regel, Endbedingungen).
- Integrationstests für End-to-End-Run vorhanden (Start -> Aktionen -> Endzustand -> Persistenz).
- Negative Tests vorhanden (ungültige Aktion, doppelter Run, Aktion nach Run-Ende).
- Alle Tests laufen im CI erfolgreich durch.
- Testfälle sind nachvollziehbar mit den ACs verknüpft (Traceability).

## 6) Nicht-funktionale Anforderungen
- Keine kritischen Bugs (Severity 1/2) offen.
- Logging ermöglicht Nachvollziehbarkeit zentraler State-Transitions.
- Performance für Standard-Use-Case ist ausreichend (Run-Aktion ohne spürbare UI-Verzögerung).
- Sicherheits- und Datenschutz-Basics sind eingehalten (keine sensiblen Daten im Klartext-Log).

## 7) Dokumentation
- Story-Beschreibungen und ACs sind aktuell und entsprechen der implementierten Logik.
- Technische Kurzdoku für relevante Regeln/Entscheidungen liegt vor.
- Release Notes für R1 sind erstellt.
- Bekannte Einschränkungen und bewusste Trade-offs sind dokumentiert.

## 8) Abnahme & Betriebsbereitschaft
- Product Owner/Business hat die Story anhand der ACs abgenommen.
- Monitoring-Grundlagen für Backend-Fehler und API-Ausfälle sind aktiv.
- Rollback- oder Hotfix-Vorgehen ist für den Release dokumentiert.
- Die Funktion ist in der Zielumgebung erfolgreich deployt.

---

## DoD-Checkliste (kurz, zum Abhaken)
- [ ] ACs vollständig umgesetzt und getestet
- [ ] Keine kritischen offenen Defects
- [ ] Persistenz + Statistik + Leaderboard konsistent
- [ ] API/UI-Dokumentation aktualisiert
- [ ] CI grün
- [ ] PO-Abnahme erfolgt
- [ ] Release Notes erstellt
