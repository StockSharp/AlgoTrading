# Estratégia do comerciante FT Bill Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Trader FT Bill Williams** é uma tradução StockSharp de alto nível do consultor especialista MetaTrader "FT_BillWillams_Trader". Ele combina fractais Bill Williams com o indicador Alligator para romper tendências comerciais. A estratégia observa novos fractais, verifica se a estrutura Alligator confirma a direção do rompimento e, opcionalmente, aplica filtros de distância, alinhamento e sinal reverso antes de abrir uma posição.

## Lógica de negociação

1. **Detecção fractal** – a estratégia armazena em buffer os máximos e mínimos `FractalPeriod` mais recentes. Quando a barra do meio é o ponto mais alto (ou mais baixo) da janela, um novo nível de rompimento é registrado. Um deslocamento `IndentPoints` é adicionado acima/abaixo do fractal para evitar entradas prematuras.
2. **Confirmação do intervalo** – dependendo de `EntryConfirmation`:
   - `PriceBreakout` confirma quando o intervalo da vela cruza o nível de rompimento.
   - `CloseBreakout` espera que o fechamento da vela anterior ultrapasse o nível.
3. **Verificação de distância** – as entradas são rejeitadas quando o nível de rompimento está mais distante que `MaxDistancePoints` dos lábios Alligator (valor da barra anterior). Defina a distância como zero para desativar o filtro.
4. **Filtro de dentes** – quando `UseTeethFilter` está ativado, o fechamento anterior deve estar acima (para longos) ou abaixo (para curtos) dos dentes Alligator.
5. **Alinhamento de tendência** – com `UseTrendAlignment = true`, os lábios, dentes e mandíbula devem estar separados por pelo menos `TeethLipsDistancePoints` e `JawTeethDistancePoints` pontos, respectivamente, confirmando que Alligator está em tendência.
6. **Saídas reversas** – se `ReverseExit = OppositeFractal`, qualquer novo fractal oposto fecha imediatamente a posição aberta. Com `OppositePosition`, a estratégia primeiro fecha a negociação atual antes de abrir uma na direção oposta.
7. **Saída da mandíbula** – `JawExit` define se a posição é fechada quando o preço cruza a mandíbula Alligator (intrabarra ou no fechamento da vela).
8. **Trailing stop** – quando `EnableTrailing` é verdadeiro e a negociação é lucrativa, o stop se move para os lábios ou dentes dependendo da inclinação relativa dos lábios e do `SlopeSmaPeriod` SMA. As paradas de proteção iniciais e as metas de lucro são controladas por `StopLossPoints` e `TakeProfitPoints`.

## Parâmetros

| Propriedade | Descrição | Padrão |
|----------|-------------|---------|
| `OrderVolume` | Volume de negociação utilizado no envio de ordens de mercado. | `0.1` |
| `FractalPeriod` | Número de barras no padrão fractal (valores ímpares recomendados). | `5` |
| `IndentPoints` | Offset adicionado ao nível de rompimento (em pontos). | `1` |
| `EntryConfirmation` | Modo de confirmação de interrupção (`PriceBreakout`, `CloseBreakout`). | `CloseBreakout` |
| `UseTeethFilter` | Exija que o fechamento anterior esteja no lado correto dos dentes Alligator. | `true` |
| `MaxDistancePoints` | Distância máxima entre o nível de rompimento e Alligator lábios (pontos). | `1000` |
| `UseTrendAlignment` | Aplique separação mínima entre Alligator linhas. | `false` |
| `JawTeethDistancePoints` | Distância mínima mandíbula-dentes usada no filtro de alinhamento. | `10` |
| `TeethLipsDistancePoints` | Distância mínima entre dentes e lábios utilizada no filtro de alinhamento. | `10` |
| `JawExit` | Modo de fechamento de posições no crossover da mandíbula (`Disabled`, `PriceCross`, `CloseCross`). | `CloseCross` |
| `ReverseExit` | Tratamento de sinal oposto (`Disabled`, `OppositeFractal`, `OppositePosition`). | `OppositePosition` |
| `EnableTrailing` | Ative o gerenciamento de trailing stop baseado em Alligator. | `true` |
| `SlopeSmaPeriod` | Período do SMA que é comparado com a inclinação dos lábios. | `5` |
| `StopLossPoints` | Distância de stop-loss em pontos (0 desabilita). | `50` |
| `TakeProfitPoints` | Distância de lucro em pontos (0 desabilita). | `50` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Pontos finais para as linhas Alligator. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Deslocamento para frente para cada linha Alligator. | `8`, `5`, `3` |
| `MaMethod` | Tipo de média móvel para Alligator (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Simple` |
| `AppliedPrice` | Preço da vela fornecida ao Alligator. | `CandlePrice.Median` |
| `CandleType` | Tipo de vela subscrito a partir dos dados de mercado. | `15-minute timeframe` |

## Notas adicionais

- A estratégia desenha as Alligator linhas e executa as negociações na área padrão do gráfico.
- `FractalPeriod` deve permanecer ímpar para que a barra do meio represente o ápice do fractal; o valor padrão corresponde ao consultor especialista original.
- Parâmetros baseados em distância (`IndentPoints`, `MaxDistancePoints`, `JawTeethDistancePoints`, `TeethLipsDistancePoints`, `StopLossPoints`, `TakeProfitPoints`) são expressos em pontos de corretagem (`Security.PriceStep`).
- As paradas finais e as saídas de mandíbula dependem de velas concluídas, espelhando a lógica MQL original que funciona com os valores de barra anteriores do Alligator.
