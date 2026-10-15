# Estratégia Trend Reversal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Trend Reversal é um sistema direcional que tenta capturar rompimentos após um recuo de curto prazo dentro de uma tendência existente. Ela foi portada do expert advisor do MetaTrader "Trend Reversal" e reescrita para usar a API de alto nível do StockSharp. A conversão mantém a pilha central de confirmação (médias móveis, momentum e MACD), substituindo os filtros gráficos de linha originais por verificações de sobreposição de preço mais fáceis de reproduzir programaticamente.

## Pilha de indicadores
- **Médias móveis ponderadas lineares (LWMA)** sobre preço típico, com comprimentos rápido e lento personalizáveis. A linha rápida acompanha o swing mais recente, enquanto a lenta identifica a tendência dominante.
- **Oscilador Momentum** calculado no mesmo período. A estratégia registra a distância absoluta do nível neutro 100 para os três últimos candles fechados, emulando a lógica do MetaTrader.
- **Par de linhas de sinal MACD** configurado com comprimentos rápido, lento e de sinal independentes. A direção do histograma é usada como confirmação de período superior para operações compradas e vendidas.

## Lógica de negociação
1. Aguarde um candle finalizado no período configurado. A estratégia ignora barras parcialmente formadas.
2. Garanta que ambas as LWMAs e o indicador de momentum estejam totalmente formados. Sem histórico suficiente, o sistema permanece zerado.
3. Mantenha uma fila móvel das três divergências de momentum mais recentes a partir de 100. Uma configuração só é válida se pelo menos um desses valores exceder o respectivo limite de compra ou venda.
4. Exija que o candle de duas barras atrás tenha uma mínima menor que a máxima do candle anterior. Isso recria a estrutura "sobreposta" usada no EA original para detectar uma consolidação estreita antes do rompimento.
5. Avalie filtros direcionais:
   - **Comprado:** LWMA rápida acima da LWMA lenta e valor principal MACD acima da linha de sinal.
   - **Vendido:** LWMA rápida abaixo da LWMA lenta e valor principal MACD abaixo da linha de sinal.
6. Respeite o limite de posição líquida. A estratégia entra ou adiciona a uma posição apenas quando a exposição absoluta (posição atual dividida pelo volume de negociação) está abaixo do valor configurado `MaxPositions`.
7. Ordens são enviadas com `BuyMarket()` ou `SellMarket()`, permitindo reversões parciais ou completas dependendo da exposição atual.

## Gestão de risco
- Distâncias opcionais de **take profit** e **stop loss** (expressas em unidades de preço) podem ser anexadas pelo bloco de proteção incorporado do StockSharp. Ambos os níveis são desabilitados quando um parâmetro é definido como zero.
- Nenhum trailing stop automático ou ajuste de break-even está incluído nesta versão. Esses recursos podem ser implementados com manipuladores de eventos adicionais se necessário.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período primário usado para construir candles. | Período de 15 minutos |
| `FastLength` | Período da LWMA rápida. | 6 |
| `SlowLength` | Período da LWMA lenta. | 85 |
| `MomentumLength` | Período do oscilador momentum. | 14 |
| `MomentumBuyThreshold` | Desvio absoluto mínimo de momentum (a partir de 100) que valida uma configuração comprada. | 0.3 |
| `MomentumSellThreshold` | Desvio absoluto mínimo de momentum (a partir de 100) que valida uma configuração vendida. | 0.3 |
| `MacdFastLength` | Período da EMA rápida usado dentro do filtro MACD. | 12 |
| `MacdSlowLength` | Período da EMA lenta usado dentro do filtro MACD. | 26 |
| `MacdSignalLength` | Período da EMA de sinal usado dentro do filtro MACD. | 9 |
| `TakeProfit` | Distância de take profit em unidades de preço. Defina como 0 para desabilitar. | 50 |
| `StopLoss` | Distância de stop loss em unidades de preço. Defina como 0 para desabilitar. | 20 |
| `TradeVolume` | Volume da ordem expresso em lotes. | 1 |
| `MaxPositions` | Número máximo de unidades de volume de negociação permitidas na posição líquida. | 1 |

## Notas de uso
- Anexe a estratégia a um ativo com informações válidas de passo e preço para que as ordens de proteção funcionem corretamente.
- Para negociação multidirecional (pirâmide ou escalonamento), aumente `MaxPositions`. A estratégia continuará adicionando posições enquanto os filtros permanecerem válidos e a exposição ficar dentro desse limite.
- Backtests devem ser realizados com o mesmo período de candles especificado pelo parâmetro `CandleType`. O StockSharp solicitará automaticamente os dados adequados quando a estratégia iniciar.
- Como a versão MetaTrader dependia de linhas de tendência desenhadas à mão, esta reescrita substitui essas verificações por uma condição determinística de sobreposição de candles. Isso mantém o comportamento consistente entre backtests e execução ao vivo.

## Diferenças em relação ao EA original
- Trailing stop, movimentos de break-even e saídas emergenciais baseadas em equity não são implementados para manter o exemplo focado na geração central de sinais.
- Recursos de gestão de dinheiro, como multiplicação de lote e filtragem por Magic Number, não são necessários no StockSharp e, portanto, foram removidos.
- A confirmação MACD usa o mesmo período dos candles de negociação em vez da agregação mensal original. Você pode emular a configuração multitemporal assinando um tipo de candle mais lento e vinculando o filtro MACD a essa assinatura, se desejar.

## Dicas de otimização
- Otimize primeiro os comprimentos das médias móveis para corresponder ao ciclo dominante do mercado; depois ajuste os limites de momentum.
- Experimente distâncias mais amplas de stop-loss e take-profit ao negociar instrumentos voláteis. Como a lógica segue tendência, buffers de saída maiores muitas vezes melhoram a lucratividade.
- Monitore estatísticas de drawdown durante as execuções de otimização. Aumentar `MaxPositions` pode melhorar a responsividade, mas também amplia o risco.
