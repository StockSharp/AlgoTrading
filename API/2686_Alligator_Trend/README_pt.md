# Estratégia de Tendência Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz o sistema clássico Alligator de Bill Williams do script original do MetaTrader (`Alligator.mq5`). Utiliza três médias móveis suavizadas construídas sobre o preço mediano e deslocadas para a frente para visualizar a fase do mercado. Uma posição comprada é aberta quando a linha rápida Lips está acima de Teeth, e Teeth está acima de Jaw. Uma posição vendida é aberta quando o alinhamento está invertido. Apenas uma posição pode estar ativa ao mesmo tempo.

Uma vez em uma negociação, a estratégia protege a posição com um stop-loss e take-profit expressos em pips. Quando o mercado se move a favor da negociação por uma distância de nível zero configurável, o stop é movido para break-even. Um trailing stop segue o máximo mais alto (para comprados) ou o mínimo mais baixo (para vendidos) com um passo mínimo para evitar atualizações frequentes do stop. As posições são fechadas quando os níveis de stop-loss, trailing stop ou take-profit são atingidos.

A configuração padrão visa candles de 30 minutos e valores de pip estilo Forex, mas os parâmetros podem ser otimizados para outros mercados. Como a versão MQL original usa o tratamento de pips específico do corretor, a conversão depende do `PriceStep` do instrumento para traduzir distâncias em pips para preços absolutos.

## Regras de Trading

### Entrada
- **Comprado**: Sem posição aberta e `Lips > Teeth > Jaw` no último candle concluído.
- **Vendido**: Sem posição aberta e `Lips < Teeth < Jaw` no último candle concluído.

### Saída e Gestão de Risco
- **Stop Inicial**: Colocado `StopLossPips` abaixo (comprado) ou acima (vendido) do preço de execução.
- **Take Profit**: Colocado a `TakeProfitPips` do preço de execução.
- **Nível Zero**: Quando o preço avança `ZeroLevelPips`, o stop é movido para o preço de entrada.
- **Trailing Stop**: Após a ativação do nível zero, o stop segue o extremo com `TrailingStopPips`, atualizando apenas quando a melhora excede `TrailingStepPips`.
- As posições são fechadas imediatamente quando qualquer stop ou o nível de take-profit é tocado nos dados do candle.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `CandleType` | Período de 30 minutos | Série de candles usada para cálculos de indicadores e avaliação de sinais. |
| `JawLength` | 13 | Período de média móvel suavizada para a linha Jaw azul. |
| `TeethLength` | 8 | Período de média móvel suavizada para a linha Teeth vermelha. |
| `LipsLength` | 5 | Período de média móvel suavizada para a linha Lips verde. |
| `JawShift` | 8 | Deslocamento para frente da linha Jaw, expresso em barras. |
| `TeethShift` | 5 | Deslocamento para frente da linha Teeth, expresso em barras. |
| `LipsShift` | 3 | Deslocamento para frente da linha Lips, expresso em barras. |
| `EnableLong` | `true` | Permite ou bloqueia entradas compradas. |
| `EnableShort` | `true` | Permite ou bloqueia entradas vendidas. |
| `StopLossPips` | 45 | Distância de stop-loss em pips a partir do preço de execução. |
| `TakeProfitPips` | 145 | Distância de take-profit em pips a partir do preço de execução. |
| `ZeroLevelPips` | 30 | Distância em pips necessária para mover o stop para break-even. |
| `TrailingStopPips` | 50 | Distância entre o extremo atual e o trailing stop. |
| `TrailingStepPips` | 10 | Melhora mínima em pips necessária antes de atualizar o trailing stop. |

## Notas

- O indicador Alligator é calculado sobre o preço mediano `(High + Low) / 2` para corresponder à implementação do MetaTrader.
- Os valores de linha deslocados são emulados com buffers internos para que as comparações usem os mesmos dados deslocados que o script original.
- A estratégia assume que uma negociação é executada antes de um novo sinal ser processado na mesma barra, espelhando a execução barra a barra do EA fonte.
- Otimize as distâncias em pips para corresponder ao tamanho do tick e à volatilidade do instrumento negociado.
