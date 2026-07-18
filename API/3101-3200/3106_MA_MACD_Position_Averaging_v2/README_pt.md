# Estratégia de Médio de Posição MA MACD v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Médio de Posição MA MACD v2** é uma tradução direta do consultor especialista do MetaTrader de Vladimir
Karputov. Combina um filtro de média móvel ponderada, um bloco de confirmação MACD e um módulo de médio que aumenta a exposição
quando as operações existentes se movem contra a posição. A versão StockSharp mantém a hierarquia de sinais original, processa
indicadores em candles terminadas e gerencia a lógica de proteção (stop loss, take profit, trailing) em código para reproduzir
o comportamento do lado do broker em MQL.

## Lógica de Trading
1. **Preparação de indicadores**
   - Uma média móvel configurável calcula no tipo de candle e componente de preço selecionados. O parâmetro `MaShift` emula
     o deslocamento para frente do MetaTrader lendo valores de candles mais antigas, enquanto `BarOffset` permite avaliar a
     barra atual ou uma anterior.
   - Um indicador de sinal MACD produz as linhas principal e de sinal usando períodos rápido, lento e de sinal personalizáveis
     e um preço aplicado que corresponde ao consultor especialista original.
2. **Validação de sinais**
   - Setups comprados requerem que ambas as linhas MACD sejam negativas, que o preço esteja acima da média móvel deslocada
     e que a distância do preço à média exceda `MaIndentPips` (convertido para preço absoluto usando o tamanho de pip do
     instrumento).
   - Setups vendidos espelham as condições: ambas as linhas MACD devem ser positivas, o preço deve permanecer abaixo da
     média móvel deslocada e a diferença à média deve ser pelo menos `MaIndentPips`.
   - O filtro de proporção `MacdRatio` impõe `MACD_main / MACD_signal >= MacdRatio` (usando divisão decimal absoluta) antes
     de permitir uma operação.
   - Quando `ReverseSignals = true`, a direção da ordem a mercado é invertida depois que todas as condições passam.
3. **Ciclo de vida da posição**
   - Se **não existe posição**, a estratégia abre uma ordem a mercado com o `OrderVolume` configurado (arredondado pelo
     passo de volume do instrumento) na direção calculada. Níveis de stop-loss e take-profit são aplicados imediatamente
     de acordo com `StopLossPips` e `TakeProfitPips`.
   - Se **uma exposição já existe**, a estratégia nunca abre o lado oposto. Em vez disso:
     - Fecha tudo se comprados e vendidos forem detectados simultaneamente (rede de segurança espelhando a verificação MQL), ou
     - Invoca o bloco de médio para o lado atual.
4. **Módulo de médio**
   - Para comprados, o algoritmo encontra o tramo aberto com o preço mais baixo cuja perda não realizada excede `StepLossPips`.
     Para vendidos seleciona o tramo perdedor com o preço mais alto.
   - Uma vez que um candidato é encontrado, uma nova ordem a mercado é enviada com volume `CandidateVolume × LotCoefficient`
     (após ajuste ao passo/mín/máx de volume permitido). Isso reproduz a progressão geométrica do especialista original.
   - Novos tramos herdam as mesmas distâncias de stop-loss e take-profit e se tornam elegíveis para atualizações de trailing.
5. **Controles de risco**
   - Um trailing stop ativa apenas quando tanto `TrailingStopPips` quanto `TrailingStepPips` são maiores que zero. Para
     comprados, o stop se move para `Close - TrailingStopPips` quando o lucro excede `TrailingStopPips + TrailingStepPips`;
     vendidos se comportam simetricamente.
   - Verificações manuais de stop-loss e take-profit são realizadas em cada candle terminada. Quando acionadas, uma ordem a
     mercado fecha o tramo exato e o remove da lista de médio.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| **OrderVolume** | Volume base para a primeira operação em um ciclo. |
| **StopLossPips** | Distância do stop-loss em pips. Definir como zero para desativar o stop. |
| **TakeProfitPips** | Distância do take-profit em pips. Definir como zero para desativar o alvo. |
| **TrailingStopPips** | Distância entre o preço e o trailing stop. Funciona junto com `TrailingStepPips`. |
| **TrailingStepPips** | Movimento favorável adicional necessário antes de atualizar o trailing stop. |
| **StepLossPips** | Perda mínima (em pips) necessária antes de adicionar um tramo de médio. |
| **LotCoefficient** | Multiplicador aplicado ao volume do tramo perdedor selecionado ao fazer médio. |
| **BarOffset** | Número de barras para trás para ler valores de indicadores (0 = barra terminada atual). |
| **ReverseSignals** | Inverte a execução comprado/vendido mantendo os mesmos filtros. |
| **MaPeriod** | Período da média móvel. |
| **MaShift** | Deslocamento para frente aplicado à média móvel (estilo MetaTrader). |
| **MaMethod** | Método de suavização da média móvel (Simples, Exponencial, Suavizado, Ponderado). |
| **MaPrice** | Componente de preço da candle usado para a média móvel. |
| **MaIndentPips** | Distância mínima do preço à média móvel antes de entrar. |
| **MacdFastPeriod** | Período de EMA rápido para MACD. |
| **MacdSlowPeriod** | Período de EMA lento para MACD. |
| **MacdSignalPeriod** | Período de EMA de sinal para MACD. |
| **MacdPrice** | Preço aplicado usado no cálculo do MACD. |
| **MacdRatio** | Proporção mínima entre as linhas principal e de sinal do MACD. |
| **CandleType** | Série de candles usada para todos os cálculos. |

## Notas de Implementação
- O tamanho do pip é calculado a partir do passo de preço do instrumento, reproduzindo o ajuste de 3/5 dígitos da versão MQL.
  Isso mantém idênticas as distâncias baseadas em pips nos símbolos Forex.
- Todos os buffers de indicadores usam filas para emular a indexação `ma_shift` e `bar` do MetaTrader sem chamar métodos de
  busca histórica proibidos pelas regras do projeto.
- Os ajustes de volume respeitam `Security.VolumeStep`, `Security.MinVolume` e `Security.MaxVolume`, evitando tamanhos de
  ordem inválidos quando `LotCoefficient` multiplica a exposição.
- A lógica de proteção (stops, takes, trailing) é executada completamente na camada de estratégia, portanto não há dependência
  de APIs de modificação de posição do broker.
- A classe reside no namespace `StockSharp.Samples.Strategies` e segue o requisito do repositório de usar recuo de tabulação
  e comentários exclusivamente em inglês.
