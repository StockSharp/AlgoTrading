# Estratégia de Auto Adjusting
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

AutoAdjustingStrategy replica o expert MetaTrader *Aouto Adjusting1* usando a API de alto nível do StockSharp. O port mantém o filtro de momentum multitemporal original, a confirmação de tendência MACD mensal e uma pilha de três EMAs para detectar retrações dentro da tendência. Stops e alvos são projetados a partir das recentes extremidades de oscilação e ajustados automaticamente a cada vela completada.

## Lógica principal

1. **Estrutura de tendência** – três médias móveis exponenciais no período de negociação (6, 14, 26) devem estar alinhadas (`EMA6 < EMA14 < EMA26` para comprados, invertido para vendidos). A vela anterior precisa tocar a EMA média, enquanto a vela anterior a essa forma uma mínima mais alta / máxima mais baixa para confirmar uma retração.
2. **Confirmação de momentum** – o momentum no período superior (mapeado do período de negociação, ex.: H1 → D1) deve desviar pelo menos `MomentumBuyThreshold` / `MomentumSellThreshold` de 100 em qualquer uma das últimas três barras completadas.
3. **Filtro macro** – um sinal MACD(12, 26, 9) mensal garante que as operações estejam alinhadas com a tendência dominante (`MACD > Sinal` para compras, `<` para vendas).
4. **Execução** – ordens a mercado são enviadas quando todos os filtros concordam e não há exposição oposta. Posições opostas são liquidadas antes de entrar na nova direção.
5. **Proteção** – os níveis de stop-loss são colocados um número configurável de pips além da mínima mais baixa / máxima mais alta das últimas barras `CandlesBack`. As distâncias de take-profit são escalonadas por `RewardRatio`. Tanto o stop quanto o alvo são reativados a cada fechamento de vela enquanto a posição está ativa.

## Risco e dimensionamento de posição

A estratégia espelha a parametrização de risco original:

- `RiskPercent` calcula um tamanho de posição adaptativo quando o valor do portfólio e os metadados do passo de preço estão disponíveis. O algoritmo divide a perda monetária permitida pela perda por unidade implícita pela distância de stop atual.
- Quando o dimensionamento baseado em risco não pode ser avaliado (ex.: estatísticas de portfólio ausentes), o motor recorre ao parâmetro `TradeVolume` fixo.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeFrame(H1)` | Período de negociação usado para a pilha EMA. |
| `MomentumCandleType` | `DataType` | Derivado de `CandleType` | Período superior que alimenta o indicador de momentum (H1→D1, H4→W1, etc.). |
| `MacroMacdCandleType` | `DataType` | `TimeFrame(30 days)` | Período para a confirmação MACD macro (mensal por padrão). |
| `PadAmount` | `decimal` | `3` | Pips extras além das extremidades de oscilação ao calcular stops. |
| `RiskPercent` | `decimal` | `0.1` | Percentual do capital do portfólio arriscado por operação. |
| `RewardRatio` | `decimal` | `2` | Multiplicador aplicado à distância de stop para posicionar o take-profit. |
| `CandlesBack` | `int` | `3` | Número de velas inspecionadas para detecção de máximas/mínimas de oscilação. |
| `MomentumBuyThreshold` | `decimal` | `0.3` | Desvio mínimo de momentum necessário para habilitar entradas compradas. |
| `MomentumSellThreshold` | `decimal` | `0.3` | Desvio mínimo de momentum necessário para habilitar entradas vendidas. |
| `TradeVolume` | `decimal` | `1` | Tamanho de lote de fallback quando o dimensionamento baseado em risco não está disponível. |

## Gráficos e visualização

- Assinar o período de negociação e plotar as três EMAs para observar as retrações.
- Acompanhar a série de momentum em seu painel de período superior para confirmar os limiares de energia.
- Monitorar os valores MACD do período macro para validar o filtro de tendência.

## Notas

- O mapeamento automático de períodos corresponde ao expert MQL: M1→M15, M5→M30, M15→H1, M30→H4, H1→D1, H4→W1, D1→MN1. Outros períodos mantêm seu valor original.
- A estratégia evita chamadas de `GetValue` em indicadores armazenando os valores mais recentes dentro da estratégia e alimentando-os através dos callbacks de bind.
- O comportamento de trailing espelha o EA original recalculando os níveis protetores toda vez que uma vela fecha.
