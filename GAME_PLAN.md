# Stack Island - game plan

*Catch it. Cook it. Stack it high.*

A compact carry-and-stack island tycoon that looks and feels like the mobile hits *Idle Mafia* and
*Supermarket Tycoon: Empire*, with a castaway theme and a long progression: **80-100 hours** to finish.

## Pillars

1. **Look and feel of the references.** Bright saturated colours, warm tiled floors, thick coral walls, blue cage
   fences, round trees; a close diagonal camera; tall swaying stacks; buy tiles painted on the floor; customers
   with icon bubbles. Juice everywhere (hops, pops, sparks, coins flying).
2. **One yard at a time.** Each area is small enough to read at a glance. New areas open through a gate.
3. **Hands-on first, automated later.** You start doing everything yourself; helpers, parrots and managers slowly
   take over every job, so late game is about planning and expansion.
4. **Nothing is lost, nothing fails.** Full trays and counters just pause.
5. **Long tail.** Many upgrade levels per thing, steep yard prices, combo recipes that use earlier yards, offline
   earnings, and optional prestige for the very end.

## The ten yards

Each yard sits north of the previous one, behind a gate bought from inside the previous yard. Each introduces a
new raw item and how you get it, one or two stations, a counter, and its own helpers. Later recipes combine items
from earlier yards, carried by Delivery helpers (or by you) through the gates.

| # | Yard | Raw source (how) | Stations -> product | Counter sells |
|---|---|---|---|---|
| 1 | Crab Cove | crab pen (hit crabs) | Grill: meat -> crab cake | crab cakes |
| 2 | Coconut Grove | coconut field (walk through the pile) | Press: coconut -> coconut juice | juice |
| 3 | Fishing Pier | fish pool (net fish) | Smoker: fish -> smoked fish; Sushi bar: fish + rice | smoked fish, sushi |
| 4 | Banana Jungle | banana trees (shake) | Blender: banana + juice -> smoothie | smoothies |
| 5 | Chicken Ranch | hen house (collect eggs) | Pan: egg -> omelette; Bakery: egg + flour -> cake | omelettes, cakes |
| 6 | Wheat Fields | wheat field (cut) | Mill: wheat -> flour; Oven: flour -> bread | bread |
| 7 | Sandwich Shack | (combo yard) | Deli: bread + crab cake + egg -> sandwich | sandwiches |
| 8 | Ice Grotto | ice cave (mine ice) | Freezer: ice + juice -> ice cream | ice cream |
| 9 | Pirate Bay | treasure sand (dig) | Jeweller: gold -> trinkets | souvenirs |
| 10 | Volcano Resort | geothermal kitchen | Feast table: 3 premium dishes -> feast | feasts (huge value) |

## Progression and pacing

- **Per-yard upgrades** (each a buy tile with many levels): source size (more animals / bigger field),
  station speed and capacity, number of stations, counter speed and price, helper count, helper speed/carry,
  the player's stack size and walk speed, and a Cash Parrot.
- **Costs** grow per level (about x1.25-1.35), stations add a second and third copy at fixed points.
- **Yard prices** grow about x8-10 per yard, product prices about x6 per yard.
- **Targets** (thoughtful player, measured by a balance simulator like Castaway Co.'s):
  yard 2 at ~1 h, yard 5 at ~12 h, yard 10 at ~60-70 h, every upgrade maxed at 80-100 h.
- **Offline earnings:** helpers keep working while you're away (rate measured from the real flow), capped at a few
  hours and upgradable.
- **Late game:** an Island Manager per yard automates spending on that yard's tiles; optional prestige ("New
  Season") resets yards for a permanent price bonus after yard 10.

## Build order

1. Yard 1 core loop, art style, helpers, Cash Parrot, info hover, animations. **(done)**
2. Data-driven yards (YardDef: sources, stations, counters, tiles and their curves) and gates; move yard 1 onto it.
3. Yards 2-4 with the new source types (field pile, fish pool, shaking trees) and cross-yard delivery helpers.
4. Balance simulator + long upgrade curves; offline earnings.
5. Yards 5-10, combo recipes, managers.
6. Polish: music, more effects, settings, prestige, final balance pass.
