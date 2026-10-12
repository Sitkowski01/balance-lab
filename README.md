# Balance Lab

Laboratorium balansu gier planszowych. Zasady gry podpina się jako plugin, boty (MCTS)
rozgrywają tysiące partii, a raport pokazuje, co jest niezbalansowane: przewagę miejsca
przy stole, dominujące strategie, za mocne i martwe karty oraz skutki zmian w zasadach.

> Projekt w budowie. Gotowe: kontrakt silnika, deterministyczny generator, Czwórki (ConnectFour)
> jako pierwsza gra, bot losowy i bot MCTS.

## Architektura

```
src/BalanceLab.Engine              kontrakt IGame<TState, TMove>, IPlayer, Match, Pcg32, StableHash
src/BalanceLab.Games.ConnectFour   Czwórki na bitboardach (zna tylko silnik)
src/BalanceLab.Bots                RandomBot, MctsBot (działają na kontrakcie, nie znają żadnej gry)
```

Gra to czyste funkcje na niemutowalnym stanie: `Setup(gracze, ziarno)`, `LegalMoves`, `Apply`
(nowy stan), `IsTerminal`, `Scores` i `Hash` — stabilny między procesami (FNV-1a), w przeciwieństwie
do `GetHashCode`. `Match` rozgrywa partię i nie ufa graczom: o legalności ruchu decyduje `Apply` gry.
Reguły zależności pilnuje NetArchTest.

Czwórki trzymają planszę w dwóch liczbach 64-bitowych (kolumna = 6 pól + pusty bit strażnika),
więc ruch to jedno dodawanie, a szukanie czwórki — cztery przesunięcia bitowe. Test różnicowy
porównuje bitboard z prostym sędzią na siatce po każdym ruchu setek losowych partii.

## Bot MCTS

`MctsBot` to Monte Carlo Tree Search z wyborem UCT. Nie zna strategii żadnej gry — tylko zasady:
rozgrywa losowe partie od kandydujących ruchów i coraz częściej sprawdza te, które wygrywają
(UCB1: średni wynik + premia za rzadko sprawdzane ruchy). Każdy węzeł zbiera wynik gracza, który
wykonał prowadzący do niego ruch, więc ten sam kod działa dla 2, 3 i 4 graczy.

- budżet: limit iteracji (powtarzalny) i/lub limit czasu;
- `Search` zwraca statystyki wszystkich ruchów (odwiedziny, średni wynik) — przydadzą się w raporcie;
- testy siły ze stałymi ziarnami: idealna gra w Nim („zostaw wielokrotność 4”), wygrana w jednym
  ruchu i blokowanie groźby w Czwórkach, 200/200 wygranych z botem losowym, 1000 iteracji kontra
  50 iteracji: 59:1;
- test pokazujący, po co eksploracja: bez niej (c = 0) MCTS potrafi utknąć na przegrywającym ruchu
  z pewną siebie średnią 0,99.

## Determinizm

Każda partia musi dać się odtworzyć z ziarna — na tym opierają się powtórki z dziennika
ruchów i porównania wariantów zasad. Dlatego silnik ma własny generator
[PCG32](https://www.pcg-random.org/) zamiast `System.Random`:

- niemutowalny (`readonly record struct`) — jest częścią stanu gry, losowanie zwraca nowy generator;
- zgodny z implementacją referencyjną (test na wektorze z `pcg32-demo`);
- `NextInt(bound)` bez przesunięcia (odrzuca niepełną końcówkę zakresu 2^32);
- tasowanie Fishera-Yatesa, wyniki zamrożone testami — stara powtórka zawsze daje tę samą partię.

Test architektury pilnuje, że ani silnik, ani gry, ani boty nie sięgają po `System.Random`, zegar systemowy ani `Guid`.
(Jedyny wyjątek to opcjonalny limit czasu MCTS na `Stopwatch` — z nim wynik zależy od szybkości maszyny.)
To samo ziarno daje tę samą partię, a ruchy z zapisu odtwarzają stan o identycznym hashu.

## Wydajność

BenchmarkDotNet, 1000 losowań z przedziału [0, 6):

| Metoda | Czas | Alokacje |
|---|---:|---:|
| `System.Random.Next(6)` | 9,1 µs | 0 B |
| `Pcg32.NextInt(6)` | 2,1 µs | 0 B |
| `Pcg32.Shuffle` (90 kart) | 0,45 µs | 384 B |

Losowa partia Czwórek od pustej planszy do końca (playout, jak w MCTS): **0,56 µs**,
czyli ok. 1,8 mln partii na sekundę na rdzeń. Legalne ruchy nie alokują pamięci: dla każdej
z 128 masek wolnych kolumn lista jest zbudowana raz.

Ruch MCTS z pustej planszy Czwórek:

| Iteracje | Czas | Alokacje |
|---:|---:|---:|
| 1 000 | 0,54 ms | 139 KB |
| 10 000 | 6,4 ms | 1,4 MB |

czyli ok. 1,6–1,9 mln iteracji na sekundę na rdzeń; alokacje to głównie nowe węzły drzewa (jeden na iterację),
lista dzieci powstaje dopiero przy pierwszym rozwinięciu węzła.

## Testy

```bash
dotnet test                 # jednostkowe, właściwości (FsCheck), architektura (NetArchTest)
dotnet tool restore
dotnet stryker              # mutation testing silnika, próg 90%
dotnet stryker -f stryker-connectfour.json
dotnet stryker -f stryker-bots.json
dotnet run -c Release --project benchmarks/BalanceLab.Benchmarks -- --filter '*'
```

Na Windowsie z Visual Studio 2022 Stryker może wybrać MSBuild bez .NET 10 — wtedy
`dotnet stryker --msbuild-path "C:\Program Files\dotnet\sdk\<wersja>\MSBuild.dll"`.

## Stack

.NET 10, xUnit, FsCheck, NetArchTest, Stryker.NET, BenchmarkDotNet.
