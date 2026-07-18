# Estratégia de Parabolic SAR EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Parabolic SAR EA** é a conversão de alto nível do StockSharp do consultor especializado MetaTrader `Parabolic SAR EA.mq5` localizado em `MQL/23039`. O script MQL original reage às reversões do Parabolic SAR em um período configurável, abrindo posições de mercado com distâncias de stop-loss e take-profit fixas expressas em "pips" do MetaTrader (com suporte a pip fracional). O port em C# assina candles, vincula o indicador `ParabolicSar` integrado e reproduz o mesmo processo de decisão barra a barra respeitando as melhores práticas do StockSharp.

## Lógica de Negociação
1. **Preparação de dados**
   - A estratégia assina o tipo de candle selecionado pelo usuário (candles de 30 minutos por padrão) e vincula um indicador Parabolic SAR configurado com passo de aceleração e valores máximos ajustáveis.
   - O valor SAR é calculado para cada candle e entregue à estratégia através do callback de alto nível `Bind`.
2. **Geração de sinais**
   - Sinal de compra: quando o valor do Parabolic SAR do candle finalizado está estritamente abaixo da mínima do candle.
   - Sinal de venda: quando o valor do Parabolic SAR do candle finalizado está estritamente acima da máxima do candle.
   - Os sinais são avaliados apenas em candles completados (`CandleStates.Finished`) para corresponder ao processamento de nova barra do MQL.
3. **Gestão de posição**
   - A exposição oposta é zerada antes de uma nova entrada aumentando o tamanho da ordem de mercado solicitada com o valor absoluto da posição atual, replicando a sequência MetaTrader `ClosePosition` mais `OpenPosition`.
   - Cada entrada recalcula os níveis de stop-loss e take-profit de proteção usando as mesmas regras de conversão pip-a-preço do MetaTrader (instrumentos de 3/5 dígitos recebem um multiplicador ×10 para o `PriceStep`).
4. **Saídas de proteção**
   - Em cada candle finalizado a estratégia verifica se a máxima/mínima viola o nível de stop-loss ou take-profit armazenado. Se acionado, a posição é fechada com uma ordem de mercado e os alvos correspondentes são limpos.
   - A lógica de proteção é acionada antes de novos sinais na mesma barra, refletindo o comportamento original do Consultor Especialista onde ordens stop são do lado do corretor.

## Notas sobre Indicador e Dados
- Usa o indicador `ParabolicSar` integrado do StockSharp com parâmetros `SarStep` e `SarMaximum`.
- A assinatura de candles é tratada através de `SubscribeCandles` sem adicionar o indicador a `Strategy.Indicators`, conforme exigido pelas diretrizes do projeto.
- A negociação só é permitida quando `IsFormedAndOnlineAndAllowTrading()` reporta true, garantindo que dados ao vivo estejam presentes e o conector permita o envio de ordens.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | `1` | Tamanho da ordem de mercado em lotes. Atualizar o valor também atualiza `Strategy.Volume`. |
| `StopLossPips` | `50` | Distância de stop-loss em pips do MetaTrader. Um pip equivale a `PriceStep × 10` para instrumentos com 3 ou 5 decimais, caso contrário apenas `PriceStep`. Definir como `0` para desativar. |
| `TakeProfitPips` | `50` | Distância de take-profit em pips do MetaTrader usando as mesmas regras de conversão do stop-loss. Definir como `0` para desativar. |
| `SarStep` | `0.02` | Passo de aceleração usado pelo indicador Parabolic SAR. |
| `SarMaximum` | `0.2` | Fator de aceleração máximo para Parabolic SAR. |
| `CandleType` | `30m timeframe` | Tipo de candle usado para cálculos. Suporta qualquer `DataType` derivado de `TimeFrame`. |

## Gestão de Risco e Comportamento
- Stop-loss e take-profit são recalculados após cada execução e armazenados internamente; nenhuma ordem pendente é registrada na bolsa.
- Se ambos os níveis de proteção forem acionados dentro de um único candle, a verificação do stop-loss é acionada primeiro, replicando o tratamento conservador da lógica MQL fonte.
- Quando o conector não reporta um `PriceStep` válido, a conversão recorre a `0.0001` para evitar níveis de proteção de distância zero.
- Nenhuma média ou pirâmide é realizada; a estratégia opera com uma única posição líquida, invertendo a direção quando o Parabolic SAR cruza o preço.

## Notas de Conversão
- MetaTrader `InpBarCurrent` equivale a 1, significando que o EA avalia o candle finalizado anterior. O port do StockSharp atinge o mesmo resultado processando apenas candles `Finished` no callback `Bind`.
- O consultor especializado original usava `CheckVolumeValue` para validar lotes e restrições do corretor. O StockSharp delega essas verificações ao conector, enquanto o parâmetro `TradeVolume` ainda impõe um requisito de volume positivo.
- A implementação Python é intencionalmente omitida, atendendo aos requisitos da tarefa.
