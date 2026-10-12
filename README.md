# Balance Lab

Laboratorium balansu gier planszowych. Zasady gry podpina się jako plugin, boty (MCTS)
rozgrywają tysiące partii, a raport pokazuje, co jest niezbalansowane: przewagę miejsca
przy stole, dominujące strategie, za mocne i martwe karty oraz skutki zmian w zasadach.

> Projekt w budowie. Gotowe: deterministyczny generator liczb losowych silnika.

## Determinizm

Każda partia musi dać się odtworzyć z ziarna — na tym opierają się powtórki z dziennika
ruchów i porównania wariantów zasad. Dlatego silnik ma własny generator
[PCG32](https://www.pcg-random.org/) zamiast `System.Random`:

- niemutowalny (`readonly record struct`) — jest częścią stanu gry, losowanie zwraca nowy generator;
- zgodny z implementacją referencyjną (test na wektorze z `pcg32-demo`);
- `NextInt(bound)` bez przesunięcia (odrzuca niepełną końcówkę zakresu 2^32);
- tasowanie Fishera-Yatesa, wyniki zamrożone testami — stara powtórka zawsze daje tę samą partię.

Test architektury pilnuje, że silnik nie sięga po `System.Random`, zegar ani `Guid`.

## Wydajność

BenchmarkDotNet, 1000 losowań z przedziału [0, 6):

| Metoda | Czas | Alokacje |
|---|---:|---:|
| `System.Random.Next(6)` | 9,1 µs | 0 B |
| `Pcg32.NextInt(6)` | 2,1 µs | 0 B |
| `Pcg32.Shuffle` (90 kart) | 0,45 µs | 384 B |

## Testy

```bash
dotnet test                 # jednostkowe, właściwości (FsCheck), architektura (NetArchTest)
dotnet tool restore
dotnet stryker              # mutation testing silnika, próg 90%
dotnet run -c Release --project benchmarks/BalanceLab.Benchmarks -- --filter '*'
```

Na Windowsie z Visual Studio 2022 Stryker może wybrać MSBuild bez .NET 10 — wtedy
`dotnet stryker --msbuild-path "C:\Program Files\dotnet\sdk\<wersja>\MSBuild.dll"`.

## Stack

.NET 10, xUnit, FsCheck, NetArchTest, Stryker.NET, BenchmarkDotNet.
