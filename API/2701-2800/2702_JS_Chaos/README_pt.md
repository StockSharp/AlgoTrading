# Estratégia JS Chaos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia JS Chaos replica o comportamento do consultor especialista original de MetaTrader "JS-Chaos" usando a API de alto nível do StockSharp. A estratégia constrói entradas por rompimento em torno da estrutura do Alligator de Bill Williams e níveis de fractais, combina confirmação do Awesome Oscillator e Acceleration/Deceleration, e gerencia a exposição aberta com stops de rastreamento, lógica de ponto de equilíbrio e um rico filtro de tempo.

## Lógica principal
1. **Pilha de indicadores**
   - Alligator de Bill Williams (Médias Móveis Suavizadas com períodos 13/8/5 e deslocamentos de 8/5/3 barras) amostradas no preço mediano.
   - Awesome Oscillator e uma SMA de 5 períodos do AO para derivar o oscilador Acceleration/Deceleration.
   - Média móvel suavizada de 21 períodos para o motor de trailing stop.
   - Desvio padrão de 10 períodos usado como condição de rastreamento adicional.
   - Detecção de fractais sobre as últimas cinco máximas/mínimas, armazenando as formações mais recentes por dez barras.
2. **Geração de sinais**
   - O contexto altista requer `AO[0] > AO[1] > 0` e `Lips > Teeth > Jaw`.
   - O contexto baixista requer `AO[0] < AO[1] < 0` e `Lips < Teeth < Jaw`.
3. **Colocação de ordens**
   - Quando as condições se alinham e o horário atual é negociável, a estratégia enfileira duas entradas do tipo stop por direção: uma ordem primária (2× volume base) e uma ordem secundária (1× volume base). Ambas disparam no fractal qualificador mais recente que se estende além dos lábios do Alligator.
   - O take-profit primário usa `Lips ± (Fractal − Lips) * Fibo1`. O take-profit secundário usa o multiplicador `Fibo2`.
4. **Gerenciamento de negociações**
   - Saída antecipada opcional quando os lábios cruzam acima (para comprados) ou abaixo (para vendidos) da abertura do candle anterior.
   - O trailing stop leva o nível de proteção para a SMMA de 21 períodos quando desvio padrão, AO e AC avançam todos na direção da negociação.
   - A lógica de ponto de equilíbrio desloca o stop da negociação secundária assim que a negociação primária for concluída e o preço tiver percorrido os pips extras configurados.
   - O monitoramento manual dos níveis de stop-loss e take-profit fecha as negociações via ordens de mercado quando os limites de preço correspondentes são violados.
5. **Filtro de tempo**
   - Janela de negociação definida por horas de início/fim (com suporte de rotação) e filtros sazonais opcionais: desabilitado antes das 03:00 de segunda-feira, após as 18:00 de sexta-feira, durante os primeiros nove dias de janeiro e após 20 de dezembro. Definir `Use Time` como falso desativa o filtro completamente.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `UseTime` | Ativa o filtro de tempo. |
| `OpenHour` / `CloseHour` | Limites de hora para negociação (0-23). |
| `BaseVolume` | Volume de ordem base, usado para dimensionar as duas entradas escalonadas (2× para a primária, 1× para a secundária). |
| `IndentingPips` | Offset adicionado/subtraído dos níveis de fractais antes de colocar ordens stop (expresso em pips). |
| `Fibo1` / `Fibo2` | Multiplicadores tipo Fibonacci aplicados à distância entre os lábios e o fractal para os objetivos de take-profit. |
| `UseClosePositions` | Fecha posições opostas quando os lábios cruzam a abertura do candle anterior. |
| `UseTrailing` | Ativa o trailing stop baseado em MA/oscilador. |
| `UseBreakeven` | Ativa o gerenciamento de ponto de equilíbrio para a posição secundária. |
| `BreakevenPlusPips` | Pips extras adicionados ao preço de entrada ao mover o stop para o ponto de equilíbrio. |
| `CandleType` | Período dos candles processados pela estratégia. |

## Notas
- A conversão mantém a estrutura de ordens escalonadas e a lógica de gerenciamento do robô MQL5 original enquanto aproveita o fluxo de trabalho de assinatura de candles do StockSharp.
- Todos os cálculos dependem de candles finalizados; a lógica de tick intracandle do EA original é espelhada através de ordens de mercado assim que o intervalo de preço confirma um rompimento.
- A conversão de pips se adapta automaticamente a instrumentos cotados com três ou cinco casas decimais (símbolos tipo forex).
