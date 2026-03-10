# Scoundrel – User Stories für R1 (Classic Mode / Vanilla)

## Ziel und Scope von R1
R1 implementiert **ausschließlich den Classic Mode (Vanilla Rules)** als kompetitiven Referenzmodus. Alle fachlichen Anforderungen in diesem Dokument sind so formuliert, dass ein Entwicklungsteam das Feature **ohne zusätzliche Spieldokumente** implementieren kann.

**Kernidee von Classic Mode:**
- Solo-Kartenspiel mit reduzierten Deck-Regeln.
- Ein Run startet mit 20 HP.
- Das Spiel endet mit **Win** (Deck leer und regelkonform abgeschlossen) oder **Loss** (HP <= 0).
- Ergebnisse werden für Profile/Statistiken/Leaderboard persistent gespeichert.

---

## Fachliche Begriffe (einheitlich im Team verwenden)
- **Run**: Ein vollständiger Spieldurchlauf in Classic Mode von Start bis Win/Loss.
- **Room**: Sichtbare Kartenauslage im aktuellen Schritt (bis zu 4 Karten).
- **Monsterkarten**: Spades (♠) und Clubs (♣).
- **Waffenkarten**: Diamonds (♦).
- **Heiltränke**: Hearts (♥).
- **Run-Aktion**: Raum überspringen; darf nicht zweimal hintereinander verwendet werden.
- **Aktive Waffe**: aktuell ausgerüstete Waffe (optional vorhanden), deren Wirksamkeit nach Kämpfen degradiert.
- **Ruleset**: Für R1 immer `vanilla`.

---

## Story R1-US-01 – Vanilla-Deck korrekt initialisieren
**Als** Spieler im Classic Mode  
**möchte ich**, dass beim Start eines Runs automatisch ein regelkonformes Vanilla-Deck erstellt wird,  
**damit** jede Runde unter identischen und fairen Wettbewerbsbedingungen startet.

### Beschreibung
Beim Start eines neuen Runs muss das System aus einem Standard-52-Karten-Deck ein **44-Karten-Vanilla-Deck** erzeugen. Entfernt werden:
- alle Joker (falls technisch im Modell vorhanden),
- rote Bildkarten (J, Q, K von ♥ und ♦),
- rote Asse (A♥, A♦).

Alle verbleibenden Karten werden zufällig gemischt.

### Acceptance Criteria (AC)
1. **Deckgröße nach Setup**  
   **Given** ein neuer Classic-Run wird gestartet  
   **When** das Deck gebaut wird  
   **Then** enthält das Spieldeck exakt **44 Karten**.
2. **Entfernte Karten sind nicht vorhanden**  
   **Given** das initialisierte Deck  
   **Then** dürfen J♥, Q♥, K♥, A♥, J♦, Q♦, K♦, A♦ sowie Joker nicht enthalten sein.
3. **Kartentyp-Zuordnung ist korrekt**  
   **Given** jede Karte im Deck  
   **Then** wird ihr Typ korrekt auf Monster (♠/♣), Waffe (♦), Heiltrank (♥) gemappt.
4. **Mischung pro Run neu**  
   **Given** zwei unabhängige neue Runs  
   **Then** ist die Kartenreihenfolge nicht deterministisch identisch (außer bei explizit gesetztem Test-Seed).

---

## Story R1-US-02 – Run-Start mit vollständigem Anfangszustand
**Als** Spieler  
**möchte ich** beim Start direkt einen vollständigen, spielbaren Zustand sehen,  
**damit** ich ohne zusätzliche Schritte meinen ersten Zug machen kann.

### Beschreibung
Beim Start eines Classic-Runs muss ein Game State erzeugt werden:
- `hp = 20`
- `ruleset = vanilla`
- `result = in_progress`
- `runsUsed = 0`
- keine aktive Waffe
- erste Room-Auslage mit bis zu 4 Karten vom Deck

### Acceptance Criteria (AC)
1. **Standard-HP gesetzt**  
   **Given** ein neuer Run  
   **Then** startet der Spieler mit **20 HP**.
2. **Ruleset eindeutig**  
   **Then** ist der Run mit `vanilla` markiert.
3. **Erste Room-Auslage**  
   **Then** werden bis zu 4 oberste Deckkarten als aktueller Room angezeigt.
4. **Initiale Zähler**  
   **Then** sind `runsUsed = 0`, `turns = 0`, `result = in_progress`.
5. **Keine Startwaffe**  
   **Then** ist zu Beginn keine aktive Waffe ausgerüstet.

---

## Story R1-US-03 – Raumaktionen spielbar machen (Fight, Take Weapon, Drink Potion, Run)
**Als** Spieler  
**möchte ich** pro sichtbarer Karte eine zur Kartenart passende Aktion ausführen können,  
**damit** das Kern-Gameplay vollständig abgebildet ist.

### Beschreibung
Der Spieler interagiert mit Karten im Room:
- **Monster (♠/♣)** -> Kampfaktion
- **Waffe (♦)** -> aufnehmen/ausrüsten
- **Heiltrank (♥)** -> trinken (HP erhöhen)
- Zusätzlich global: **Run** zum Überspringen des aktuellen Rooms (unter Run-Regel)

Nach jeder Aktion wird der Zustand aktualisiert und bei leerem/aufgelöstem Room entsprechend neue Karten nachgezogen.

### Acceptance Criteria (AC)
1. **Nur valide Aktionen je Kartentyp**  
   **Given** der aktuelle Room  
   **Then** bietet die UI/API pro Karte nur fachlich gültige Aktionen an.
2. **Zustandsänderung nach Aktion**  
   **When** eine gültige Aktion ausgeführt wird  
   **Then** ändern sich mindestens Room/Deck/HP/Waffe/Zähler konsistent gemäß Regelwerk.
3. **Ungültige Aktion wird blockiert**  
   **When** ein Client eine unzulässige Aktion sendet  
   **Then** wird sie fachlich abgewiesen (kein stilles Durchwinken, kein inkonsistenter State).
4. **Turn-Counter**  
   **Then** erhöht jede erfolgreiche Spieleraktion den Zähler `turns` um 1.

---

## Story R1-US-04 – Kampfauflösung inkl. Waffennutzung und HP-Verlust
**Als** Spieler  
**möchte ich**, dass Monsterkämpfe eindeutig nach Kartenwert und Ausrüstung aufgelöst werden,  
**damit** ich taktisch planen kann und Ergebnisse nachvollziehbar sind.

### Beschreibung
Ein Kampf gegen eine Monsterkarte muss deterministisch aufgelöst werden. Das System berücksichtigt:
- Kartenwert des Monsters,
- ob eine aktive Waffe vorhanden ist,
- ob und wie stark HP reduziert wird,
- Verbrauch/Degradation der Waffe für Folgekämpfe.

Die Regelimplementierung muss mit dem R1-Vanilla-Verhalten konsistent sein.

### Acceptance Criteria (AC)
1. **Kampf gegen Monster ist möglich**  
   **Given** eine Monsterkarte im Room  
   **When** der Spieler „Fight“ auswählt  
   **Then** wird genau diese Monsterkarte aufgelöst und aus dem Room entfernt.
2. **HP-Verlust bei Schaden**  
   **Then** reduziert ein verlorener/ungünstiger Kampf die HP regelkonform.
3. **Waffeneinfluss berücksichtigt**  
   **Given** eine aktive Waffe  
   **Then** beeinflusst sie die Kampfauflösung gemäß Vanilla-Regelwerk.
4. **Keine negativen HP ohne Endzustand**  
   **Then** führt `hp <= 0` sofort zum Run-Ende mit `result = loss`.
5. **Nachvollziehbarkeit**  
   **Then** liefert die API/UI einen klaren Ergebnistext je Kampf (z. B. Schaden, eingesetzte Waffe, verbleibende HP).

---

## Story R1-US-05 – Waffen aufnehmen und Degradation korrekt anwenden
**Als** Spieler  
**möchte ich** Waffenkarten aufnehmen können und sehen, wie ihre Effektivität über Kämpfe abnimmt,  
**damit** der zentrale Risiko-/Timing-Mechanismus von Scoundrel funktioniert.

### Beschreibung
Beim Auswählen einer Waffenkarte wird sie aktive Waffe. Die Waffe degradiert nach Kämpfen: Sie kann in Folge nur noch schwächere Monster besiegen (Vanilla-Prinzip).

### Acceptance Criteria (AC)
1. **Waffe aus Room aufnehmen**  
   **Given** eine ♦-Karte im Room  
   **When** der Spieler sie auswählt  
   **Then** wird sie als aktive Waffe gesetzt und aus dem Room entfernt.
2. **Alte Waffe wird ersetzt**  
   **Given** bereits aktive Waffe vorhanden  
   **When** neue Waffe aufgenommen wird  
   **Then** ersetzt die neue die bisherige vollständig.
3. **Degradation nach Kampf**  
   **When** eine Waffe in einem Kampf verwendet wird  
   **Then** wird ihr Folgezustand so angepasst, dass nur noch schwächere Monster besiegbar sind.
4. **Grenzfall unbrauchbar**  
   **Then** wird eine Waffe, die kein zulässiges Monster mehr schlagen kann, fachlich als unbrauchbar behandelt.
5. **Transparenz**  
   **Then** zeigt UI/API den aktuellen Waffenstatus für den nächsten Kampf eindeutig an.

---

## Story R1-US-06 – Heiltränke nutzen, HP-Grenzen einhalten
**Als** Spieler  
**möchte ich** Heiltränke verwenden können,  
**damit** ich meine Überlebenschance im Run strategisch erhöhen kann.

### Beschreibung
Hearts-Karten repräsentieren Heiltränke. Bei Nutzung erhöhen sie HP. Es gelten Obergrenzen gemäß Vanilla-Logik (maximal sinnvolle/erlaubte HP).

### Acceptance Criteria (AC)
1. **Potion-Aktion verfügbar**  
   **Given** eine ♥-Karte im Room  
   **Then** kann der Spieler die Karte als Heiltrank verwenden.
2. **HP steigt regelkonform**  
   **When** der Heiltrank genutzt wird  
   **Then** erhöht sich HP entsprechend der Regel.
3. **HP-Maximum eingehalten**  
   **Then** kann HP die festgelegte Obergrenze nicht überschreiten.
4. **Verbrauch**  
   **Then** der verwendete Heiltrank wird aus dem Room entfernt.
5. **Feedback**  
   **Then** zeigt UI/API die HP-Änderung (vorher/nachher) für den Spieler klar an.

---

## Story R1-US-07 – Run-Regel: Room skippen, aber nie zweimal hintereinander
**Als** Spieler  
**möchte ich** in kritischen Situationen aus einem Room fliehen können,  
**damit** ich taktische Ausweichentscheidungen treffen kann, ohne das Spiel zu trivialisieren.

### Beschreibung
Die Aktion **Run** überspringt den aktuellen Room und erzeugt einen neuen Room aus dem Restdeck. Fachregel: **Run darf nicht zweimal direkt hintereinander** ausgeführt werden.

### Acceptance Criteria (AC)
1. **Run-Aktion vorhanden**  
   **Given** ein aktiver Room  
   **Then** kann der Spieler „Run“ wählen.
2. **Room wird ersetzt**  
   **When** Run erfolgreich ausgeführt wird  
   **Then** wird der aktuelle Room verworfen/übersprungen und durch neue Karten ersetzt (sofern Deck vorhanden).
3. **Run-Counter**  
   **Then** `runsUsed` erhöht sich pro erfolgreichem Run um 1.
4. **No double run**  
   **Given** letzte Aktion war bereits ein Run  
   **When** erneut Run angefragt wird  
   **Then** wird die Aktion blockiert und ein fachlicher Hinweis zurückgegeben.
5. **Run wieder möglich nach anderer Aktion**  
   **Given** nach einem Run wurde mindestens eine Nicht-Run-Aktion ausgeführt  
   **Then** ist Run wieder erlaubt.

---

## Story R1-US-08 – Win/Loss-Endbedingungen korrekt und unverzüglich setzen
**Als** Spieler  
**möchte ich** ein eindeutiges Ende des Runs erhalten,  
**damit** ich sofort weiß, ob ich gewonnen oder verloren habe.

### Beschreibung
Run endet in zwei Fällen:
- **Loss**: HP <= 0.
- **Win**: Deck ist vollständig verbraucht und es verbleiben keine aufzulösenden Karten/ausstehenden Aktionen gemäß Vanilla.

Nach Endzustand sind keine weiteren Spielaktionen mehr zulässig.

### Acceptance Criteria (AC)
1. **Loss sofort bei 0 oder weniger HP**  
   **When** HP auf 0 oder darunter fällt  
   **Then** wird `result = loss` gesetzt und der Run geschlossen.
2. **Win nur bei vollständigem Abschluss**  
   **Given** Deck und Room erfüllen die Vanilla-Win-Bedingung  
   **Then** wird `result = win` gesetzt.
3. **Exklusivität**  
   **Then** ein Run kann nie gleichzeitig win und loss sein.
4. **Keine Folgeaktionen nach Ende**  
   **Then** API/UI verweigert jede weitere Spielaktion auf diesem Run.
5. **Endstatus sichtbar**  
   **Then** Abschlussmaske/-response zeigt Resultat, Rest-HP, Turns und RunsUsed.

---

## Story R1-US-09 – Run-Ergebnis persistieren (Session Data)
**Als** Product Owner / Analyst  
**möchte ich** pro beendetem Run strukturierte Ergebnisdaten speichern,  
**damit** spätere Auswertungen, Leaderboards und KPIs möglich sind.

### Beschreibung
Bei Run-Ende wird ein Session-Record gespeichert mit mindestens:
- `ruleset` (für R1: `vanilla`)
- `result` (`win`/`loss`)
- `turns`
- `runsUsed`
- `hpRemaining`
- `playerId`/`playerName` Referenz
- Zeitstempel

### Acceptance Criteria (AC)
1. **Persistenz bei Run-Ende**  
   **When** ein Run endet  
   **Then** wird genau ein Session-Datensatz gespeichert.
2. **Vollständige Pflichtfelder**  
   **Then** alle oben genannten Felder sind vorhanden und fachlich valide.
3. **Ruleset-Trennung**  
   **Then** `ruleset` ist explizit gespeichert, um spätere Trennung von Variant-Modi zu ermöglichen.
4. **Idempotenz-Schutz**  
   **Then** wiederholte End-Events erzeugen keine doppelten Session-Datensätze.

---

## Story R1-US-10 – Profilstatistiken nach jedem Run aktualisieren
**Als** Spieler  
**möchte ich**, dass mein Profil nach jedem Run automatisch aktualisiert wird,  
**damit** meine Fortschrittswerte jederzeit konsistent sind.

### Beschreibung
Das Profil enthält mindestens:
- Player Name
- Games Played
- Wins
- Win Rate
- Best Streak
- Achievements

Nach jedem abgeschlossenen Classic-Run werden die Kennzahlen atomar aktualisiert.

### Acceptance Criteria (AC)
1. **Games Played +1**  
   **When** ein Run endet (win oder loss)  
   **Then** erhöht sich `gamesPlayed` um 1.
2. **Wins nur bei Sieg**  
   **Then** `wins` erhöht sich ausschließlich bei `result = win`.
3. **Win Rate korrekt berechnet**  
   **Then** `winRate = wins / gamesPlayed` (inkl. sinnvoller Rundungsregel in API/UI).
4. **Best Streak aktualisiert**  
   **Then** Siegserien werden korrekt fortgeführt/gebrochen und `bestStreak` ggf. erhöht.
5. **Konsistenz bei Fehlern**  
   **Then** Session-Speicherung und Profilupdate erfolgen transaktional oder mit sauberem Retry/Compensation-Konzept.

---

## Story R1-US-11 – Classic-Mode Leaderboard führen
**Als** wettbewerbsorientierter Spieler  
**möchte ich** meine Leistung im Vergleich zu anderen im Classic Mode sehen,  
**damit** der Competitive-Mehrwert des Spiels sichtbar wird.

### Beschreibung
Für R1 wird ein Leaderboard für `ruleset = vanilla` bereitgestellt. Rankingkriterien müssen im Produkt festgelegt und transparent sein (z. B. Win Rate mit Mindestanzahl Spiele, sekundär Best Streak).

### Acceptance Criteria (AC)
1. **Eigener Leaderboard-Scope**  
   **Then** es existiert ein separates Leaderboard für `vanilla`.
2. **Nur gültige Runs einbezogen**  
   **Then** nur abgeschlossene Runs mit valide gespeicherten Daten fließen in die Berechnung ein.
3. **Deterministische Sortierung**  
   **Then** Gleichstände werden über klar definierte Tie-Breaker aufgelöst.
4. **Anzeige zentraler Kennzahlen**  
   **Then** mindestens Player Name, Games Played, Wins, Win Rate und Best Streak sind sichtbar.
5. **Aktualität**  
   **Then** neue abgeschlossene Runs sind spätestens nach definiertem SLA (z. B. < 60 Sekunden) im Leaderboard reflektiert.

---

## Story R1-US-12 – Meilenstein-Achievements für Classic Mode
**Als** Spieler  
**möchte ich** bei Erreichen wichtiger Meilensteine Achievements erhalten,  
**damit** ich Motivation und Fortschritt im Spiel sehe.

### Beschreibung
R1 benötigt ein einfaches, aber robustes Achievement-System für Classic Mode. Konkrete Start-Milestones (Beispiel, fachlich freigeben):
- First Win (erster Sieg)
- 3 Wins Total
- 10 Runs Played
- 3-Win-Streak

### Acceptance Criteria (AC)
1. **Automatische Prüfung nach Run-Ende**  
   **Then** relevante Achievements werden direkt nach Session-/Profil-Update evaluiert.
2. **Einmalige Vergabe**  
   **Then** jedes Achievement wird pro Spieler höchstens einmal vergeben.
3. **Persistenz und Abrufbarkeit**  
   **Then** freigeschaltete Achievements sind im Profil gespeichert und abrufbar.
4. **Transparente Rückmeldung**  
   **Then** neu freigeschaltete Achievements werden im UI/API-Response deutlich gekennzeichnet.
5. **Ruleset-Kompatibilität**  
   **Then** Achievement-Logik ist so modelliert, dass spätere ruleset-spezifische Vergaben möglich sind.

---

## Nicht-Ziele für R1 (explizit)
- Keine House-Rule-Varianten aus R2.
- Kein Adventure-/Floor-/Relic-System aus R3.
- Keine Vermischung von Statistik/Leaderboard über mehrere Rulesets.

