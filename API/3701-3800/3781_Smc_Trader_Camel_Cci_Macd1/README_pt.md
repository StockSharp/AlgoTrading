# Estratégia SMC Trader Camel CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista **"Steve Cartwright Trader Camel CCI MACD"**.
Ele reproduz a lógica de negociação original baseada em um canal de média móvel exponencial estilo camelo,
um filtro de tendência MACD e limites do índice de canal de commodities (CCI). As negociações são executadas quando concluídas
velas para garantir um comportamento determinístico e ficar próximo ao fluxo de trabalho barra por barra da versão MQL.

## Lógica de negociação

1. **Indicadores**
   - Duas médias móveis exponenciais (EMA) com o mesmo período são aplicadas aos máximos e mínimos das velas para formar o
canal de camelo. Um rompimento do fechamento anterior além desses envelopes sinaliza força de impulso.
   - Um indicador MACD padrão (EMA rápida, EMA lenta e linha de sinal) é usado para confirmar a direção da tendência subjacente.
   - Um indicador CCI valida a força do impulso usando níveis de sobrecompra/sobrevenda de ±100 por padrão.
2. **Entradas longas**
   - O fechamento da vela anterior está acima da máxima do camelo EMA.
   - O valor principal anterior MACD está acima de zero **e** acima da linha de sinal.
   - O valor anterior de CCI está acima do limite positivo.
   - Nenhuma posição ativa está aberta e nenhuma saída ocorreu dentro do período atual da vela (evita a reentrada rápida).
3. **Entradas curtas**
   - O fechamento da vela anterior está abaixo do camel low EMA.
   - O valor principal anterior de MACD está abaixo de zero **e** abaixo da linha de sinal.
   - O valor anterior de CCI está abaixo do limite negativo.
   - As mesmas condições de posição plana e de resfriamento das configurações longas.
4. **Saídas**
   - As posições longas fecham quando o valor principal anterior de MACD cruza abaixo da linha de sinal ou quando o valor principal de CCI anterior
o valor cai abaixo do limite positivo.
   - As posições curtas fecham quando o valor principal anterior MACD cruza acima da linha de sinal.
   - Após qualquer saída, um cooldown igual à duração de uma vela é aplicado antes de novas entradas.

A estratégia é negociada no máximo uma vez por barra porque cada decisão é baseada nos dados da vela concluída anterior.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Tipo de dados/período de vela usado para todos os indicadores. | Período de 1 hora |
| `CamelLength` | Comprimento do canal alto/baixo EMA. | 34 |
| `CciPeriod` | Comprimento do filtro CCI. | 20 |
| `MacdFastPeriod` | Comprimento EMA rápido para MACD. | 12 |
| `MacdSlowPeriod` | Comprimento EMA lento para MACD. | 26 |
| `MacdSignalPeriod` | Período de suavização de sinal para MACD. | 9 |
| `CciThreshold` | Nível CCI absoluto que deve ser excedido para entradas (aplicado simetricamente). | 100 |

Todos os parâmetros são otimizáveis através do otimizador StockSharp graças às chamadas `SetOptimize`.

## Gestão de risco

- Os pedidos são enviados via `BuyMarket` e `SellMarket`, herdando a propriedade da estratégia `Volume`.
- `StartProtection()` está habilitado para inicializar auxiliares de proteção StockSharp padrão.
- Nenhum stop-loss ou take-profit fixo é definido no algoritmo original; as saídas dependem apenas de sinais indicadores.

## Gráficos

A estratégia traça automaticamente o canal camel EMA, indicadores MACD e CCI, juntamente com negociações próprias,
que replica as dicas visuais usadas na implementação do MT4.

## Notas

- O temporizador de resfriamento usa a duração da vela derivada de `CandleType.Arg`. Certifique-se de que `CandleType` contém um
Argumento `TimeSpan` quando você altera o período.
- Como todas as decisões são baseadas nos valores da barra anterior, a ordem das operações reflete `iMACD`, `iCCI`
e `iMA` (com shift=1) chamadas na origem EA.
