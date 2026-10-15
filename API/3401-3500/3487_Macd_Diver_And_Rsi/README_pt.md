# MACD Mergulhador e RSI Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma conversão C# do consultor especialista **"Macd diver and rsi"** MetaTrader 5. Ele mantém a ideia original do sinal de dois estágios: o Índice de Força Relativa (RSI) detecta extremos de sobrevenda ou sobrecompra, enquanto o histograma MACD confirma que o impulso está voltando na direção da negociação. Os lados longo e curto são configurados independentemente para que o comportamento possa ser ajustado separadamente para configurações de alta e baixa.

A estratégia opera em uma assinatura de vela única (período configurável) e negocia o título traçado diretamente por meio de ordens de mercado. Todo o processamento do indicador usa o StockSharp API de alto nível via `BindEx`, correspondendo às regras do projeto.

## Lógica de negociação

1. **Preparação de indicadores**
   - Dois indicadores RSI são criados, um para a perna longa e outro para a perna curta, com comprimentos e limites individuais.
   - Dois indicadores `MovingAverageConvergenceDivergenceSignal` refletem as configurações MACD para negociações longas e curtas. Seu componente histograma é usado para confirmar reversões de impulso.

2. **Regras de entrada**
   - **Configuração longa**: quando o valor longo RSI está no limite de sobrevenda ou abaixo dele *e* o histograma longo MACD cruza acima de zero (muda o sinal de negativo para positivo), uma posição de alta é aberta. Se uma posição curta estiver ativa, ela será fechada e revertida na mesma ordem de mercado.
   - **Configuração curta**: quando o valor curto RSI estiver igual ou acima do limite de sobrecompra *e* o histograma curto MACD cruzar abaixo de zero, uma posição de baixa é aberta. A exposição longa existente é achatada antes que a nova exposição curta seja estabelecida.

3. **Gerenciamento de riscos**
   - Após cada entrada, a estratégia registra o preço de fechamento da barra de sinal como preço de referência.
   - Os níveis de stop-loss e take-profit são projetados a partir desse preço usando distâncias de pip definidas separadamente para negociações longas e curtas.
   - Os pips são convertidos em unidades de preço com o instrumento `PriceStep` e automaticamente dimensionados em 10 para símbolos com 3 ou 5 casas decimais para espelhar o comportamento do MT5.
   - Em cada vela concluída, a faixa máxima/mínima é verificada em relação a esses níveis. Atingir qualquer um dos níveis fecha imediatamente a posição com uma ordem de mercado.

4. **Gestão comercial**
   - O estado da posição é limpo sempre que o tamanho da posição retorna a zero (seja porque um stop/take-profit foi alcançado ou porque a estratégia foi revertida por um sinal oposto).
   - Nenhuma saída parcial ou ajuste final é realizada; a posição é gerida apenas através dos níveis estáticos de stop-loss e take-profit.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Prazo da assinatura da vela usada para sinais. |
| `LongRsiPeriod`, `ShortRsiPeriod` | Comprimentos RSI para detecção longa e curta. |
| `LongRsiThreshold`, `ShortRsiThreshold` | RSI limites que permitem entradas (sobrevenda para posições compradas, sobrecompra para posições vendidas). |
| `LongMacdFastLength`, `LongMacdSlowLength`, `LongMacdSignalLength` | MACD EMA comprimentos para a perna de alta. |
| `ShortMacdFastLength`, `ShortMacdSlowLength`, `ShortMacdSignalLength` | MACD EMA comprimentos para a perna de baixa. |
| `LongVolume`, `ShortVolume` | Volume de negociação por sinal. Ao reverter, a estratégia adiciona o volume aberto absoluto para que a ordem única execute o fechamento e a nova abertura. |
| `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Distância das ordens stop-loss e take-profit em pips. Zero desativa o respectivo nível. |

## Notas

- A estratégia requer instrumentos com `PriceStep` diferente de zero. Se a etapa estiver faltando, o cálculo do pip volta para 0,0001 para evitar a divisão por zero.
- Como ambos os lados usam instâncias de indicadores independentes, você pode ajustar o comportamento de alta e de baixa separadamente, por exemplo, estreitando o limite de sobrecompra e mantendo o lado de sobrevenda mais permissivo.
- O código adiciona comentários e documentação em inglês para esclarecer o processo de negociação e satisfazer as diretrizes do projeto.
