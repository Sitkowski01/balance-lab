# Balance Lab

Laboratorium balansu gier planszowych. Zasady gry podpina się jako plugin, boty (MCTS)
rozgrywają tysiące partii, a raport pokazuje, co jest niezbalansowane: przewagę miejsca
przy stole, dominujące strategie, za mocne i martwe karty oraz skutki zmian w zasadach.

> Projekt w budowie. Gotowe: kontrakt silnika, deterministyczny generator, Czwórki (ConnectFour)
> jako pierwsza gra, bot losowy, bot MCTS i symulator turniejów z CLI.

## Architektura

```
src/BalanceLab.Engine              kontrakt IGame<TState, TMove>, IPlayer, Match, Pcg32, StableHash
src/BalanceLab.Games.ConnectFour   Czwórki na bitboardach (zna tylko silnik)
src/BalanceLab.Bots                RandomBot, MctsBot (działają na kontrakcie, nie znają żadnej gry)
src/BalanceLab.Simulation          turnieje równoległe, statystyki z przedziałami ufności, raport JSON
src/BalanceLab.Cli                 balance-lab simulate — jedyne miejsce, które składa gry, boty i symulator
```

Gra to czyste funkcje na niemutowalnym stanie: `Setup(gracze, ziarno)`, `LegalMoves`, `Apply`
(nowy stan), `IsTerminal`, `Scores` i `Hash` — stabilny między procesami (FNV-1a), w przeciwieństwie
do `GetHashCode`. `Match` rozgrywa partię i nie ufa graczom: o legalności ruchu decyduje `Apply` gry.
Reguły zależności pilnuje NetArchTest.

Czwórki trzymają planszę w dwóch liczbach 64-bitowych (kolumna = 6 pól + pusty bit strażnika),
więc ruch to jedno dodawanie, a szukanie czwórki — cztery przesunięcia bitowe. Test różnicowy
porównuje bitboard z prostym sędzią na siatce po każdym ruchu setek losowych partii.

## Szybki start

```bash
dotnet run -c Release --project src/BalanceLab.Cli -- simulate   --game connectfour --bots random random --games 20000 --seed 1 --out report.json
```

```
ConnectFour: 20000 partii, ziarno 1, ukonczone 20000, remisy 55

Uczestnik            wygrane  95% przedzial     sredni wynik
random                 49.2%  48.5% - 49.9%     0.493
random#2               50.6%  49.9% - 51.3%     0.507

Miejsce przy stole   wygrane  95% przedzial     sredni wynik
1                      55.3%  54.6% - 56.0%     0.554
2                      44.4%  43.7% - 45.1%     0.446

Dlugosc partii (ruchy): srednio 21.3, mediana 21, 90% ponizej 31, od 7 do 42
```

To już jest wynik o balansie: przy losowej grze zaczynający wygrywa w Czwórkach 55% partii,
a przedział ufności wyklucza 50%. Boty: `random`, `mcts`, `mcts:ITERACJE` — po jednym na miejsce
przy stole. `--games-out` zapisuje każdą partię (ziarno, miejsca, wynik), więc dowolną da się odtworzyć.

## Symulator turniejów

- **Rotacja miejsc** — w partii *i* uczestnik *k* siada na miejscu (*k* + *i*) mod *n*: każdy zaczyna
  równie często, więc przewaga pierwszego gracza nie przechyla porównania botów, a osobno ją mierzymy.
  Dlatego liczba partii musi być wielokrotnością liczby uczestników.
- **Średni wynik miejsca** (remis = pół wygranej) to uczciwa miara przewagi, gdy są remisy — sam odsetek
  wygranych liczy remis jak przegraną; brak przewagi to 1 / liczba graczy.
- **Wynik nie zależy od liczby wątków** — ziarno partii i każdego bota wyprowadzone jest z ziarna
  turnieju i numeru partii (`StableHash`), a każda partia zapisuje wynik w swojej komórce tablicy.
  Test porównuje raport z 1 wątku i ze wszystkich bajt w bajt.
- **Przedział Wilsona** zamiast `p ± 1,96·σ` — poprawny przy małej liczbie partii i odsetkach blisko 0 lub 100%.
- Partie przerwane limitem ruchów liczone są osobno i nie wchodzą do odsetków; błąd w partii zgłasza
  jej numer i ziarno, żeby dało się ją powtórzyć — przy kilku błędach zawsze ten o najniższym numerze,
  niezależnie od liczby wątków.
- CLI sprawdza parametry i katalogi wyjściowe przed startem. Kody wyjścia: 0 — sukces,
  1 — symulacja się nie udała, 2 — błędne parametry.
- Raport JSON zapisuje ziarna jako tekst — JavaScript obcina liczby powyżej 2^53.

## Bot MCTS

`MctsBot` to Monte Carlo Tree Search z wyborem UCT. Nie zna strategii żadnej gry — tylko zasady:
rozgrywa losowe partie od kandydujących ruchów i coraz częściej sprawdza te, które wygrywają
(UCB1: średni wynik + premia za rzadko sprawdzane ruchy). Każdy węzeł zbiera wynik gracza, który
wykonał prowadzący do niego ruch, więc ten sam kod działa dla 2, 3 i 4 graczy.

- budżet: limit iteracji (powtarzalny) i/lub limit czasu;
- logarytm w UCB liczony samymi +, ×, ÷ (`DeterministicMath`) — `Math.Log` korzysta z biblioteki
  systemu i potrafi różnić się ostatnim bitem między Windowsem a Linuksem, a wtedy ten sam turniej
  dałby inny raport na innym komputerze;
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

Turniej 2000 partii MCTS(50) kontra bot losowy: 341 ms na 1 wątku, 66 ms na 16 wątkach
logicznych (5,1×). Skalowanie nie jest liniowe — ograniczają je rdzenie fizyczne i odśmiecanie pamięci
(MCTS tworzy węzeł na iterację).

## Testy

```bash
dotnet test                 # jednostkowe, właściwości (FsCheck), statystyczne, siła botów, architektura, e2e CLI
dotnet tool restore
dotnet stryker              # mutation testing silnika, próg 90%
dotnet stryker -f stryker-connectfour.json
dotnet stryker -f stryker-bots.json
dotnet stryker -f stryker-simulation.json
dotnet stryker -f stryker-cli.json
dotnet run -c Release --project benchmarks/BalanceLab.Benchmarks -- --filter '*'
```

Na Windowsie z Visual Studio 2022 Stryker może wybrać MSBuild bez .NET 10 — wtedy
`dotnet stryker --msbuild-path "C:\Program Files\dotnet\sdk\<wersja>\MSBuild.dll"`.

## Stack

.NET 10, System.CommandLine, System.Text.Json, xUnit, FsCheck, NetArchTest, Stryker.NET, BenchmarkDotNet.
