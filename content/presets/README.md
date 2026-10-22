# Preset maps

Hand-made levels for tests and demos. One character per tile, one line per row. The origin
is the top-left corner: x grows to the right, y grows down. Every row must have the same
length, and the map must be at least 20×15 (LVL-2).

| Char | Meaning | Char | Meaning |
|---|---|---|---|
| `#` | wall | `1` / `2` | player 1 / player 2 start |
| `.` | floor | `Z` | zombie spawn |
| `~` | water (needs Swim) | `h` | health item |
| `O` | pit (needs Jump) | `r` | reward item |
| `D` | exit door (closed until both levers are active) | `j` | Jump power item |
| `E` | exit tile | `s` | Sprint power item |
| `L` | lever | `w` | Swim power item |

The server uses the same legend for the static layout it sends in `LevelStarted`
(`string[] rows`). There, item, zombie and lever characters mark their starting positions,
and live positions come in `StateUpdated`.

| File | Purpose |
|---|---|
| `tutorial.txt` | No zombies. Integration tests walk both players onto the levers, then the exit. |
| `arena.txt` | Two zombies, a walled room, water and pits. Used for contact and chase demos. |
