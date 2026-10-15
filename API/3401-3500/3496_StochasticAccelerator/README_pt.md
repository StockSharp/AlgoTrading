# Stochastic Estratégia aceleradora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia do acelerador Stochastic é uma conversão do MetaTrader 5 especialista *#2 stoch mt5*. O robô original avalia três
osciladores estocásticos junto com Bill Williams' Accelerator Oscillator e o Awesome Oscillator. Uma posição longa é aberta
somente quando todos os filtros estocásticos concordam com o momento de alta e o oscilador do acelerador ultrapassa um limite de sensibilidade.
As posições curtas utilizam regras simétricas. Uma vez que uma negociação está em andamento, o Awesome Oscillator monitora as reversões de impulso para fechar
a exposição. A porta StockSharp reproduz essa mecânica enquanto depende da assinatura de vela de alto nível API e
vinculações de indicadores.

A estratégia mantém o perfil de gestão de dinheiro do EA. As entradas são dimensionadas com um valor de lote fixo, enquanto stop-loss e
as distâncias de lucro são expressas em MetaTrader pips. A implementação StockSharp usa `StartProtection` para que o configurado
os limites de risco são anexados automaticamente a cada nova posição. As etapas de preço são convertidas em MetaTrader unidades pip para manter o
mesmas distâncias de proteção entre corretores.

## Lógica de negociação
1. Assine a série de velas primárias definida por `CandleType` e processe apenas velas concluídas, espelhando o EA original.
2. Alimente três `StochasticOscillator` instâncias:
   - O **sinal estocástico** verifica se %K está acima ou abaixo de %D.
   - A **entrada estocástica** valida que os sinais de alta permanecem acima de `EntryLevel` (ou abaixo de `100 - EntryLevel` para vendas).
   - O **filtro estocástico** garante que as configurações de alta permaneçam abaixo de `FilterLevel` (ou acima de `100 - FilterLevel` para vendas).
3. Rastreie o oscilador do acelerador e exija que ele cruze acima de `AcceleratorLevel` para confirmar entradas longas. Shorts exigem um
cruze abaixo de `-AcceleratorLevel`.
4. Feche qualquer posição aberta quando o Awesome Oscillator cruzar a banda `AwesomeLevel` na direção oposta.
5. Após o achatamento, abra uma nova posição se exatamente um lado satisfizer todos os filtros de entrada. O volume é ajustado ao valor do título
etapa do lote para que a solicitação permaneça válida para corretores reais.
6. Aplique distâncias de stop-loss e take-profit usando `StartProtection`, mantendo os mesmos controles de risco baseados em pip que o MetaTrader
especialista.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 4 horas | Velas primárias processadas pela estratégia. |
| `TradeVolume` | `decimal` | `0.01` | Volume utilizado para novas entradas (lotes). |
| `StopLossPips` | `decimal` | `40` | Distância de stop-loss em MetaTrader pips. |
| `TakeProfitPips` | `decimal` | `70` | Distância de lucro em MetaTrader pips. |
| `SignalKPeriod` | `int` | `40` | Período %K do estocástico de confirmação. |
| `SignalDPeriod` | `int` | `10` | Suavização %D do estocástico de confirmação. |
| `SignalSlowing` | `int` | `10` | Suavização adicional para o estocástico de confirmação. |
| `EntryKPeriod` | `int` | `40` | Período %K do estocástico de entrada. |
| `EntryDPeriod` | `int` | `10` | Suavização %D do estocástico de entrada. |
| `EntrySlowing` | `int` | `10` | Suavização adicional para o estocástico de entrada. |
| `EntryLevel` | `decimal` | `20` | Limite inferior que confirma o impulso de alta (vendas usam `100 - EntryLevel`). |
| `FilterKPeriod` | `int` | `40` | Período %K do filtro estocástico. |
| `FilterDPeriod` | `int` | `10` | Suavização %D do filtro estocástico. |
| `FilterSlowing` | `int` | `10` | Suavização adicional para o filtro estocástico. |
| `FilterLevel` | `decimal` | `75` | Limite superior limitando configurações de alta (vendas usam `100 - FilterLevel`). |
| `AcceleratorLevel` | `decimal` | `0.0002` | Amplitude mínima do oscilador do acelerador necessária para entradas. |
| `AwesomeLevel` | `decimal` | `0.0013` | Banda osciladora incrível que aciona saídas comerciais. |

## Diferenças do especialista MetaTrader original
- A porta StockSharp usa assinaturas de velas com ligações de indicadores em vez de chamadas `CopyBuffer` repetidas.
- O gerenciamento de pedidos é realizado no modo de posição líquida. Quando o EA reverteria imediatamente, a conversão primeiro fecha o
exposição atual e, em seguida, emite uma nova ordem de mercado no lado oposto.
- As distâncias de stop-loss e take-profit são anexadas via `StartProtection`, usando cálculos de tamanho de pip derivados do
passo de preço do instrumento. Isso evita modificações manuais nos bilhetes, mantendo as distâncias idênticas a MetaTrader pontos.
- As solicitações de volume são normalizadas para `VolumeStep`, `MinVolume` e `MaxVolume` da segurança para que o código esteja pronto para uso
ambientes de negociação.

## Dicas de uso
- Ajuste `TradeVolume` para corresponder ao passo mínimo do lote do instrumento antes de executar a estratégia.
- Ajuste os níveis estocásticos (`EntryLevel` e `FilterLevel`) junto com os limites do oscilador para adaptar o filtro
rigor ao seu mercado.
- Ative o desenho do gráfico quando disponível para visualizar os três osciladores estocásticos, o Accelerator Oscillator, o Awesome
Oscilador e negociações executadas.
- Como a lógica espera pelas velas finalizadas, os sinais aparecem no fechamento de cada barra; use um backtester com o mesmo período
para resultados consistentes.

## Indicadores
- Três instâncias `StochasticOscillator` com configurações independentes de suavização e limite.
- `AcceleratorOscillator` para confirmação de entrada.
- `AwesomeOscillator` para tempo de saída.
