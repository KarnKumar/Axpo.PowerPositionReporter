# lib — External Assemblies

`PowerService.dll` (+ `PowerService.pdb`) is the real Axpo trading-system assembly,
already placed here.

```
lib/
├── PowerService.dll
└── PowerService.pdb
```

This assembly targets .NET Standard 2.0 and is fully compatible with .NET 10.
It is referenced by `Axpo.PowerTradePosition.Infrastructure` via:

```xml
<Reference Include="PowerService">
  <HintPath>..\..\lib\PowerService.dll</HintPath>
  <Private>true</Private>
</Reference>
```

The public API lives in the `Axpo` namespace: `IPowerService`, `PowerService`,
`PowerTrade`, `PowerPeriod` (a struct), and `PowerServiceException`.
`TradingSystemService` (in Infrastructure) is the adapter class that wraps
`Axpo.PowerService` and exposes it as `IPositionAggregator`'s dependency.
