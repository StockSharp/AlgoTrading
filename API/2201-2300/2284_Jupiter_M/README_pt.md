# Estratégia Jupiter M
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de grade traduzida do especialista MetaTrader "Jupiter M. 4.1.1".
O algoritmo constrói uma cesta de ordens com um passo configurável e adapta
tanto o take profit quanto o volume à medida que novos níveis são abertos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o preço cai pelo tamanho do passo e (opcional) CCI < -100
  - Vendido: o preço sobe pelo tamanho do passo e (opcional) CCI > 100
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: A cesta alcança o take profit calculado
- **Stops**: Ponto de equilíbrio após um número especificado de passos
- **Valores padrão**:
  - `TakeProfit` = 10
  - `FirstStep` = 20
  - `FirstVolume` = 0.01
  - `VolumeMultiplier` = 2
  - `CciPeriod` = 50
  - `CandleType` = velas de 5 minutos
- **Filtros**:
  - Categoria: Grade, reversão à média
  - Direção: Ambos
  - Indicadores: CCI (opcional)
  - Stops: Ponto de equilíbrio
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

## Parâmetros

- `TakeProfit` – meta de lucro em unidades de preço para a cesta.
- `UseAverageTakeProfit` – calcular o take profit a partir do preço médio das ordens abertas.
- `DynamicTakeProfit` – reduzir o take profit após `TpDynamicStep` usando `TpDecreaseFactor` com um mínimo em `MinTakeProfit`.
- `BreakevenClose` / `BreakevenStep` – mover a meta para o ponto de equilíbrio após um número de passos.
- `FirstStep` – distância inicial entre níveis da grade.
- `DynamicStep`, `StepIncreaseStep`, `StepIncreaseFactor` – aumentar o passo para cada ordem adicional.
- `MaxStepsBuy` / `MaxStepsSell` – número máximo de ordens por direção.
- `FirstVolume`, `VolumeMultiplier`, `MultiplyUseStep` – controlam o crescimento do volume na grade.
- `CciFilter` / `CciPeriod` – filtro CCI opcional para a primeira ordem.
- `AllowBuy` / `AllowSell` – habilitar direções de negociação.
- `CandleType` – período das velas para cálculos.

A estratégia visa capturar a reversão à média do preço ao aumentar as posições
e fechar a cesta em metas de lucro dinâmicas.
