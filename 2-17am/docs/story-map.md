# Story map

## Confirmed backstory

- Alex and Maria are together, and the pregnancy was unplanned.
- Both are overwhelmed. Maria needs reassurance while Alex feels frightened and pressured by the responsibility.
- Rowan encourages Alex to attend a late-night party to clear his head.
- Alex leaves Maria at home for the evening but cannot stop thinking about the situation or enjoy the party.
- Alex decides to return home, and Rowan follows because he regrets encouraging Alex to escape the pressure.
- Rowan has drunk heavily by the time they leave. Alex drives with Rowan as his passenger.
- There was no argument at the party, and Alex did not abandon Maria or end their relationship.

## Confirmed collision

- Maria calls while Alex is driving home. Seeing her name brings Alex's fear of permanent responsibility back to the surface.
- Alex becomes frustrated—"Is this what my life is going to be now?"—and looks away from the road to reject or silence the call.
- Alex hits an unknown pedestrian while driving home.
- Alex stops only for a moment but does not get out or check the pedestrian.
- Already overwhelmed by the pregnancy and everything expected of him, Alex sees the collision as one more crisis he cannot face: "Fuck. Now I have to deal with this as well? Fuck it. We leave."
- Driving away is a conscious, selfish refusal to take on another consequence. It is Alex's decision alone.
- Rowan is too intoxicated to understand or clearly remember what happened.
- When Rowan asks about the impact, Alex tells him they hit a pothole. Rowan's intoxication leaves him with only that uncertain version of events.
- The victim remains unnamed because the story focuses on Alex's choices and his relationships with Maria and Rowan.

## Title and clock

- The interrogation takes place at 2:17 AM, which gives the game its title.
- The time is intentionally arbitrary. It is not the collision time, evidence, or a police trick.
- The malfunctioning clock creates atmosphere. Inspecting it only helps Alex notice the suspicious ticking elsewhere in the room.

## Confirmed aftermath

- Alex drops the heavily intoxicated Rowan at Rowan's home.
- Alex then returns home to Maria.
- Maria is angry that Alex left her to attend the party, so they do not speak that night.
- The next morning, Alex tells Maria the evening was uneventful and tries to brush the subject aside.
- Alex does not tell Maria about the collision.
- Alex discovers the cracked headlight and dent, wipes away what he can, and hides the car in the garage without repairing it.
- Alex tells Maria the car has a mechanical problem and postpones deciding what to do with it.

## Core state

- `suspicion`: visible pressure meter from 0 to 5
- `alibi`: `stayed_home`, `rowan_drove`, `alex_drove`, or `refused`
- Observation flags for evidence discovered during the interview
- `finalDecision`: protect Rowan, betray Rowan, deny everything, or confess

## Main scenarios

1. Waiting Room — explore the room before Voss arrives.
2. Initial Account — establish whether Alex was with Rowan and who drove.
3. Timeline — commit to the details of an alibi.
4. Hidden Car — explain why Alex's car has remained unseen since the party.
5. The Recording — confront the death, Voss's recording bluff, and Rowan's uncertain memory.
6. Flashback — reveal what actually happened on the road.
7. Final Offer — protect Rowan, betray Rowan, deny everything, or confess.

## Endings

- The Perfect Lie
- False Witness
- The Driver
- Caught in the Lie

## Required playtest routes

- Careful partial truth: Alex admits driving but denies the collision.
- Complete lie: Alex claims to have stayed home.
- Blame Rowan: Alex says Rowan drove and maintains that account.
- Defensive: Alex reveals almost nothing and challenges Voss.
- Observant: Alex discovers enough details to expose Voss's bluffs.
- Panicked: Alex contradicts earlier answers and reaches maximum suspicion.
- Loyal, self-preserving, and remorseful final decisions.
