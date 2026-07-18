# Estratégia Rabbit M2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Rabbit M2 é uma estratégia seguidora de tendência que combina osciladores de momentum, rompimentos de Donchian e dimensionamento adaptativo de posição. A versão original do MetaTrader 5 de Peter Byrom alterna entre regimes de compra e venda com base em médias móveis exponenciais (EMAs) de um período de tempo superior. Dentro do regime ativo, a estratégia aguarda oscilações do Williams %R confirmadas pelo Commodity Channel Index (CCI) antes de abrir uma operação. As posições são protegidas com alvos fixos de stop-loss e take-profit e são fechadas forçosamente quando o preço viola o limite oposto do canal Donchian. Após cada saída lucrativa acima de um alvo de lucro configurável, a estratégia aumenta o tamanho base do pedido e dobra o limite do alvo de lucro, imitando a lógica de escalonamento do consultor especialista MQL.

## Indicadores e dados de mercado
- **EMA rápida (40) e EMA lenta (80)** calculadas em velas de 1 hora, direcionam a direção de trading e fecham operações em mudanças de regime.
- **Commodity Channel Index (14)** medido no período principal, confirma o momentum de sobrecompra ou sobrevenda.
- **Williams %R (50)** no período principal fornece o gatilho quando cruza os níveis -20/-80.
- **Canal Donchian (100)** derivado do período principal, define saídas por rompimento quando o preço viola a máxima ou mínima anterior de 100 barras.
- **Stop-loss e take-profit fixos** são definidos a 50 pips de distância do preço de entrada (o tamanho do pip se adapta a instrumentos de 3/5 dígitos).

Dois fluxos de dados são necessários: o período principal configurável para cálculos de CCI/Williams %R/Donchian e um fluxo dedicado de 1 hora para o filtro de tendência EMA.

## Regras de trading
### Controle de regime
1. Quando a EMA de 40 períodos no feed H1 cai abaixo da EMA de 80 períodos, todas as posições compradas são fechadas e apenas configurações vendidas são permitidas.
2. Quando a EMA de 40 períodos sobe acima da EMA de 80 períodos, todas as posições vendidas são fechadas e apenas configurações compradas são permitidas.

### Critérios de entrada
- **Entrada vendida**
  - Williams %R cai abaixo de -20 enquanto seu valor anterior estava entre -20 e 0.
  - CCI está acima do nível de venda (padrão 101).
  - O regime vendido está ativo e o volume atual da posição líquida está abaixo do limite `MaxOpenPositions`.
- **Entrada comprada**
  - Williams %R sobe acima de -80 enquanto seu valor anterior estava entre -100 e -80.
  - CCI está abaixo do nível de compra (padrão 99).
  - O regime comprado está ativo e o volume atual da posição líquida está abaixo do limite `MaxOpenPositions`.

Em cada entrada, a estratégia fecha a exposição contrária (se houver) e abre a nova posição com o volume base atual.

### Critérios de saída
1. Stop-loss e take-profit são avaliados em cada vela finalizada: comprados saem se a mínima cruza o stop ou a máxima atinge o alvo; vendidos comportam-se inversamente.
2. Independentemente do stop/alvo, vendidos saem quando o preço fecha acima da máxima de 100 barras anterior e comprados quando o preço fecha abaixo da mínima de 100 barras anterior.
3. Uma mudança de regime (EMA rápida cruzando a EMA lenta) liquida imediatamente a exposição existente.

### Lógica de dimensionamento de posição
- O volume base do pedido começa em `InitialVolume` (padrão 0.01) e segue os limites da bolsa (passo/mín/máx).
- Após cada lucro realizado maior que `BigWinTarget`, o volume base aumenta em `VolumeStep` e o limite dobra, preservando o padrão de crescimento em cascata do consultor especialista original.
- O parâmetro `MaxOpenPositions` limita a exposição líquida. No port StockSharp, as posições são compensadas, portanto atingir o limite significa que nenhum volume adicional é adicionado até que a exposição diminua.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CciSellLevel` | 101 | Valor mínimo do CCI necessário para confirmar uma configuração vendida. |
| `CciBuyLevel` | 99 | Valor máximo do CCI necessário para confirmar uma configuração comprada. |
| `CciPeriod` | 14 | Período do Commodity Channel Index no período principal. |
| `DonchianPeriod` | 100 | Janela de retrocesso para o canal Donchian usado na lógica de saída. |
| `MaxOpenPositions` | 1 | Máximos múltiplos de posição líquida permitidos do volume base. |
| `BigWinTarget` | 1.50 | Lucro (em moeda da conta) necessário para escalar o volume. |
| `VolumeStep` | 0.01 | Incremento adicionado ao volume base após uma ganho qualificado. |
| `WprPeriod` | 50 | Comprimento do oscilador Williams %R. |
| `FastEmaPeriod` | 40 | Período da EMA rápida no feed de tendência de 1 hora. |
| `SlowEmaPeriod` | 80 | Período da EMA lenta no feed de tendência de 1 hora. |
| `TakeProfitPips` | 50 | Distância do take-profit em pips. |
| `StopLossPips` | 50 | Distância do stop-loss em pips. |
| `InitialVolume` | 0.01 | Volume de pedido inicial antes das regras de escalonamento. |
| `CandleType` | Velas de 15 minutos | Período principal para cálculos de CCI/Williams %R/Donchian. |

## Notas de implementação
- O port StockSharp emula o stop-loss e take-profit do MT5 monitorando máximas/mínimas das velas em vez de colocar ordens vinculadas ao broker.
- Os passos de preço e os cálculos de pip se ajustam automaticamente para instrumentos de 3 ou 5 decimais multiplicando o tamanho do tick reportado por 10.
- A estratégia depende de atualizações de PnL realizado para detectar «grandes ganhos»; certifique-se de que as operações sejam reportadas de volta à estratégia para que o escalonamento funcione.
