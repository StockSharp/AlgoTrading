# Estratégia MartingailExpert v1.0 Stochastic (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia MartingailExpert v1.0 Stochastic** é uma conversão direta do consultor especialista MetaTrader 4
`MartingailExpert_v1_0_Stochastic.mq4`. A estratégia observa as linhas %K/%D do oscilador Stochastic
e abre uma posição quando a barra anterior concluída produz uma confirmação de impulso acima (para posições compradas)
ou abaixo (para shorts) de zonas de limite configuráveis. Assim que a primeira negociação estiver ativa, o algoritmo constrói um
escada martingale de ordens de mercado adicionais cujo volume cresce geometricamente e cujo lucro é compartilhado
permanece ancorado ao preço da adição mais recente.

A conversão depende inteiramente do API de alto nível de API: assinaturas de velas, vinculação de indicadores e
ajudantes `BuyMarket`/`SellMarket` integrados. Todos os comentários do código foram reescritos em inglês e a implementação
segue o estilo de recuo baseado em tabulações exigido pelas diretrizes do projeto.

## Lógica de negociação

### 1. Sinal de entrada

1. O oscilador Stochastic (`Length = KPeriod`, `%K` suavização = `Slowing`, `%D` suavização = `DPeriod`) é
vinculado à assinatura da vela principal. Apenas velas prontas são processadas.
2. A estratégia imita a chamada MQL original `iStochastic(..., shift = 1)` armazenando os valores da barra anterior
de %K e %D. Uma entrada longa é acionada quando `K_prev > D_prev` e `D_prev > ZoneBuy`. Uma breve entrada é
acionado quando `K_prev < D_prev` e `D_prev < ZoneSell`.
3. A primeira negociação usa `BuyVolume` ou `SellVolume` e redefine qualquer estado de direção oposta para evitar
misturando escadas longas e curtas.

### 2. Média de Martingale

1. Sempre que houver um cluster aberto (`_buyOrderCount` ou `_sellOrderCount` maior que zero) a estratégia
monitora a mínima (para posições compradas) ou a máxima (para posições vendidas) da vela.
2. **Cálculo de etapas**
   * `StepMode = 0`: a próxima adição espera que o preço se mova exatamente `StepPoints × PointSize` contra
o último pedido preenchido.
   * `StepMode = 1`: a distância se torna `StepPoints + max(0, 2 × ordersCount − 2)` pontos, correspondendo ao
MQL expressão `step + OrdersTotal*2 - 2`. A expressão é multiplicada pelo tamanho do ponto do instrumento
(derivado de `Security.PriceStep` e ajustado para cotações FX de 3/5 decimais).
3. Se a vela violar o nível de gatilho, a estratégia envia uma ordem de mercado imediata cujo volume é igual
`previousVolume × Multiplier`. Os volumes são normalizados para o `VolumeStep` do instrumento, limitados por
`VolumeMax` (quando disponível) e arredondado para zero se ficarem abaixo de `VolumeMin`.
4. Após cada adição, o preço-alvo compartilhado é atualizado para
`lastEntryPrice ± ProfitFactorPoints × PointSize × orderCount` dependendo da direção.

### 3. Gestão de lucros

1. O cluster é fechado quando a vela atinge o preço-alvo compartilhado (`High >= target` para posições compradas,
`Low <= target` para shorts). Uma verificação adicional estima o lucro preço-distância usando a ponderação
preço médio de entrada para espelhar a proteção `OrderProfit()` original de MQL.
2. Todos os pedidos em aberto são nivelados com um único `SellMarket(Math.Abs(Position))` ou
`BuyMarket(Math.Abs(Position))` chamada. Após uma saída bem-sucedida, o estado martingale interno é redefinido.
3. Se o ambiente externo fechar posições (intervenção manual, stop-outs) a próxima vela com
`Position == 0` limpa automaticamente o estado martingale em cache, mantendo a estratégia consistente.

### 4. Notas adicionais de implementação

* O tamanho do ponto é derivado de `Security.PriceStep`. Para símbolos FX de 3 ou 5 decimais, o valor é multiplicado
por dez para emular o conceito MetaTrader de um pip (`Point`).
* `StartProtection()` é invocado uma vez em `OnStarted` para que a plataforma possa anexar comportamentos de proteção comuns
(tempos limite, batimentos cardíacos, etc.).
* A estratégia desenha velas, o indicador estocástico e possui negociações em uma área de gráfico dedicada para facilitar
inspeção visual durante backtests.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `StepPoints` | decimal | `25` | Distância em pontos antes de outro pedido de martingale ser feito. |
| `StepMode` | interno | `0` | `0` – distância fixa, `1` – fixo mais `2 × ordersCount − 2` pontos. |
| `ProfitFactorPoints` | decimal | `10` | Pontos adicionados (ou subtraídos) por ordem aberta para calcular o lucro do cluster. |
| `Multiplier` | decimal | `1.5` | Multiplicador aplicado ao último volume do pedido para a próxima adição. |
| `BuyVolume` | decimal | `0.01` | Volume do pedido longo inicial. |
| `SellVolume` | decimal | `0.01` | Volume da ordem curta inicial. |
| `KPeriod` | interno | `200` | Período de lookback do oscilador estocástico. |
| `DPeriod` | interno | `20` | Período de suavização para a linha de sinal %D. |
| `Slowing` | interno | `20` | Suavização adicional aplicada a %K (MetaTrader's `slowing`). |
| `ZoneBuy` | decimal | `50` | Valor mínimo de %D necessário para permitir entradas longas. |
| `ZoneSell` | decimal | `50` | Valor máximo de %D necessário para permitir entradas curtas. |
| `CandleType` | `DataType` | `5m time frame` | Tipo de vela usado para todos os cálculos de indicadores. |

## Estrutura de pastas

```
API/3991/
├──CS/
│ └── MartingailExpertV10StochasticStrategy.cs
├── README.md
├── README_zh.md
└── README_ru.md
```

A implementação do Python é omitida intencionalmente de acordo com os requisitos da tarefa.
