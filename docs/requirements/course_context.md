## Course context: T120B516 Objektinis programų projektavimas (KTU, fall 2026)

The course is about learning the GoF design patterns by applying them to a real
team project. The lecturer's framing: "The goal is not to learn 23 pattern names —
patterns are design decisions actually applied in the project." Each pattern must
solve a real problem in the game, not be added just to meet a checklist.

### The project
- A 2D multiplayer network game, built by a team of 4. It keeps growing over the semester.
- Language: Java, C# or TypeScript. Desktop or web app.
- **No game frameworks** (no Unity etc.). Simple graphics only (System.Drawing / javax.swing level).
- Client–server architecture over WebSockets, SignalR or REST, with JSON or XML messages.
  The server can run locally or on Azure.
- Game objects (map elements, enemies) may be stored in a database.
- The class diagram must grow: ≥10 classes and 2 packages (initial) → ≥20 (P1) → ≥40 (P2).
  All diagrams are made in MagicDraw, with attributes and methods filled in.

### Pattern rules
- **Each student implements ≥3 patterns. No pattern may be used twice in the team.**
- Each pattern must be demonstrated through a working `main()` and its key methods.
- For every pattern, the report needs: the problem it solves and why the pattern fits,
  UML class diagrams **before and after** applying it, the key code fragment, and proof
  that the pattern-specific requirement below is met.
- At the defence, the student explains the pattern and **changes the code and class
  diagram live at the lecturer's request**. So keep each pattern's footprint small, clear
  and easy to change.

### P1 — due 2026-11-10 (12 patterns, 4 × 3 covers all of them)
| Pattern | Specific requirement |
|---|---|
| Singleton | Show that it is thread-safe |
| Factory (Method) | ≥3 classes in the product family |
| Abstract Factory | ≥2 concrete factories, ≥3 classes per family |
| Strategy | ≥4 strategy classes |
| Observer | Show how it works with a sequence diagram |
| Builder | ≥2 concrete builders |
| Prototype | Compare deep vs shallow copies and report memory addresses; be able to switch the implementation at the defence |
| Decorator | ≥3 decoration levels |
| Command | Commands must support `undo()` |
| Adapter | Adapter and Adaptee have different numbers of methods |
| Facade | ≥2 client classes, ≥3 subsystem classes |
| Bridge | ≥2 abstractions, ≥2 concrete implementations; explain how it differs from Strategy and Adapter |

P1 also requires: a game description, requirements with acceptance criteria (levels,
lives, player/enemy/item properties, map generation, object spawning, movement logic,
collision logic), a use-case diagram, and a working prototype of client-server
communication, graphics and controls **before** any patterns are applied.

### P2 — due 2026-12-16 (11 patterns)
| Pattern | Specific requirement |
|---|---|
| Template Method | ≥2 concrete classes, marked final/sealed |
| Iterator | Iterate over ≥3 different data structures through one interface |
| Composite | Be able to switch between transparency and safety variants at the defence; explain how it differs from Decorator |
| Flyweight | Measure performance and memory use |
| State | State diagram with ≥4 states; explain how it differs from Strategy |
| Proxy | Be able to switch between protection, added-functionality and lazy-creation proxies at the defence; report performance and memory results |
| Chain of Responsibility | Chain of ≥4 handlers |
| Interpreter | Used for commands typed in a console |
| Mediator | Mediates between ≥3 different classes; explain how it differs from Observer |
| Memento | Secure restore: other classes cannot access the saved state |
| Visitor | ≥3 visitor classes |

P2 grading also requires the game to **actually work in multiplayer mode**.

### What I want from the analysis
For my game (details below), produce a pattern placement plan:
1. For each of the 23 patterns: where it fits naturally in the game, which classes take
   part (with real names from the game's domain), whether it lives on the client, the
   server or in shared code, and how the specific requirement above is met.
2. Point out patterns that fit badly or artificially, and say where the game design
   itself could change so they fit properly.
3. Suggest how to split the patterns between 4 students (≥3 each, none repeated). Group
   related patterns per person where it makes sense (e.g. Factory + Abstract Factory +
   Builder on enemy/level creation). Balance the difficulty across the team.
4. Prepare the "explain the difference" answers the defence will ask for:
   Bridge vs Strategy vs Adapter, Composite vs Decorator, State vs Strategy,
   Mediator vs Observer.
5. Flag anything that must be designed early so later patterns aren't blocked. For
   example: a game loop and tick model that Command/Memento/State can hook into, a
   message protocol that Interpreter/Chain can process, and entity hierarchies that
   Composite/Visitor/Iterator can walk.
