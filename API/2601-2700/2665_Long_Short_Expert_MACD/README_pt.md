# Estratégia Expert MACD Comprado/Vendido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia Expert MACD Comprado/Vendido** é uma conversão StockSharp do expert MetaTrader "LongShortExpertMACD". Ela combina a lógica padrão de cruzamento do Moving Average Convergence Divergence (MACD) com controles de risco de distância fixa. A estratégia reage a cruzamentos entre a linha MACD e sua linha de sinal, pode operar em modos somente comprado, somente vendido ou bidirecional, e aplica automaticamente níveis de take-profit e stop-loss expressos em pontos de preço.

A implementação usa a API de alto nível do StockSharp com subscrições de velas e vínculos de indicadores. As ordens são registradas como ordens de mercado, tornando a estratégia simples de conectar a fontes de dados em tempo real e históricas.

## Indicadores e Dados de Mercado
- **Velas** – um único período fornecido pelo parâmetro `CandleType` (período de 1 minuto por padrão). A estratégia subscreve esta série de velas via `SubscribeCandles`.
- **MovingAverageConvergenceDivergenceSignal** – o indicador MACD do StockSharp com comprimentos configuráveis de EMA rápida, EMA lenta e EMA de sinal. O valor do histograma é implicitamente derivado da diferença entre as saídas do MACD e do sinal.

## Lógica de Trading
1. **Preparação de sinal**
   - Em cada vela finalizada os valores de MACD e sinal são recuperados através do vínculo do indicador.
   - O estado histórico `_prevIsMacdAboveSignal` rastreia se o MACD estava acima da linha de sinal durante a vela anterior.

2. **Critérios de entrada**
   - **Cruzamento altista**: quando o MACD cruza acima da linha de sinal, a estratégia abre uma posição comprada se a direção de trading configurada permite entradas compradas.
     - Se uma posição vendida já está ativa e o modo de reversão está habilitado (`AllowedPosition = Both`), o tamanho da ordem inclui o volume vendido atual para fechar a posição e mudar para comprado em uma única ordem de mercado.
     - No modo somente comprado, uma posição vendida existente é fechada imediatamente, mas nenhum novo trade comprado é aberto até o sinal seguinte.
   - **Cruzamento baixista**: a ação simétrica para entradas vendidas.

3. **Critérios de saída**
   - **Gestão de risco**: tanto os níveis de stop-loss quanto de take-profit são recalculados a partir do preço médio de entrada atual cada vez que uma posição é detectada. As distâncias são definidas em pontos de preço (ou seja, `Security.PriceStep * parâmetro`), o que mantém o comportamento consistente entre instrumentos.
     - Posições compradas saem quando a mínima da vela atinge o nível de stop-loss ou a máxima atinge o nível de take-profit.
     - Posições vendidas saem quando a máxima da vela atinge o nível de stop-loss ou a mínima toca o nível de take-profit.
   - **Cruzamento oposto**: se a direção de trading permite o lado oposto, a posição é aplainada (e opcionalmente revertida) quando a relação do indicador muda.

4. **Salvaguardas operacionais**
   - A lógica de trading é executada apenas quando a estratégia está formada, online e o trading é permitido (`IsFormedAndOnlineAndAllowTrading`).
   - Os níveis de proteção são redefinidos quando nenhuma posição é mantida para evitar limites obsoletos.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `AllowedPosition` | `Both` | Restringe a estratégia a trading somente comprado, somente vendido ou bidirecional. |
| `FastLength` | `12` | Período da EMA rápida dentro do cálculo do MACD. |
| `SlowLength` | `24` | Período da EMA lenta dentro do cálculo do MACD. |
| `SignalLength` | `9` | Período da EMA de sinal usado para detecção de cruzamento. |
| `TakeProfitPoints` | `50` | Distância ao nível de take-profit medida em pontos de preço (`PriceStep * pontos`). Definir como `0` para desativar. |
| `StopLossPoints` | `20` | Distância ao nível de stop-loss medida em pontos de preço. Definir como `0` para desativar. |
| `CandleType` | `TimeFrame(1 minute)` | Série de velas usada para geração de sinais. |
| `Volume` | `1` | Número de lotes/contratos enviados com cada ordem de mercado. |

Todos os parâmetros numéricos expõem intervalos de otimização para simplificar testes walk-forward no StockSharp Designer ou Runner.

## Gestão de Posições
- **Lógica de reversão**: quando o trading bidirecional é permitido, a estratégia usa tamanhos de ordem combinados para inverter posições em uma única ordem de mercado, espelhando o comportamento do expert MetaTrader original.
- **Modos somente comprado / somente vendido**: posições existentes no lado não permitido são fechadas imediatamente, mas nenhuma nova exposição é estabelecida até que um sinal alinhado com a direção permitida ocorra.
- **Recálculo de stop/take**: a estratégia recalcula os níveis de proteção em cada vela usando o último `PositionAvgPrice`, garantindo distâncias corretas mesmo após preenchimentos parciais ou entradas escalonadas.

## Notas de Uso
- Certifique-se de que o instrumento fornece um `PriceStep` válido; se o valor estiver faltando, a estratégia retorna para `1.0` unidades de preço, o que é adequado para instrumentos do tipo ação, mas pode exigir ajuste para símbolos Forex.
- A estratégia depende de velas completadas. Cenários sensíveis à latência devem fornecer velas de granularidade adequada para evitar atrasos.
- Como as ordens são de mercado sem controles de deslizamento, o gerenciamento de risco deve considerar possíveis diferenças de preenchimento, especialmente em ativos ilíquidos.
- A visualização é criada automaticamente quando o aplicativo host suporta áreas de gráfico; MACD, velas e trades próprios são desenhados para monitoramento rápido.

## Notas de Conversão
- A implementação do StockSharp preserva os parâmetros configuráveis de MACD, as distâncias de take-profit e stop-loss, e o interruptor de disponibilidade de posição do expert MQL5.
- Os módulos de trailing-stop e gerenciamento de capital usados no MetaTrader são intencionalmente omitidos porque seu comportamento é equivalente às variantes "nenhum" incluídas com o expert original.
