# 5min RSI Estratégia Qualificada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **5min RSI Estratégia Qualificada** é uma conversão direta do consultor especialista MetaTrader "5min_rsi_qual_01a". O robô original procurou exaustão em velas de cinco minutos usando um Índice de Força Relativa de 28 períodos (RSI). Uma vez que o oscilador permaneceu em uma zona extrema por um número predefinido de barras, o EA abriu uma posição contrária e anexou um trailing stop que seguiu o fechamento da vela anterior. A porta StockSharp mantém a lógica de confirmação exata, compensações de preço e restrição de posição única enquanto depende da assinatura de vela de alto nível API.

Por padrão, a estratégia opera em velas de cinco minutos, mas o parâmetro `CandleType` aceita qualquer outro período de tempo suportado pelo instrumento. Todos os limites dos indicadores e distâncias de parada permanecem expressos em MetaTrader "pontos" para que os usuários possam reaplicar suas configurações testadas sem ajustes adicionais.

## Lógica de negociação

1. **RSI cálculo** – Um RSI de 28 períodos é atualizado em cada vela concluída. Apenas velas concluídas são processadas para corresponder à referência MQL4 `Close[1]`.
2. **Contadores de qualificação** – Dois contadores monitoram quantas velas consecutivas o RSI permaneceu acima do limite de sobrecompra (`UpperThreshold`) ou abaixo do limite de sobrevenda (`LowerThreshold`). Isso reflete o loop MQL que inspecionou os últimos 12 compassos.
3. **Condições de entrada** – Quando nenhuma posição está aberta e o contador de sobrecompra atinge `QualificationLength`, a estratégia vende a mercado. Por outro lado, quando o contador sobrevendido atinge o requisito, ele compra no mercado. Isso reproduz o comportamento do EA de manter no máximo uma negociação por símbolo.
4. **Trailing stop** – Enquanto uma posição está ativa, o nível de stop é recalculado em cada vela finalizada usando o fechamento anterior menos/mais `StopLossPoints` convertido em preço absoluto. O stop se move apenas na direção da negociação, exatamente como as chamadas `OrderModify` no código original.
5. **Parada inicial** – Após cada preenchimento a estratégia define a parada inicial usando `InitialStopPoints`. Se o valor inicial for mais próximo do que a distância final, a lógica final não irá afrouxá-lo, preservando o comportamento MetaTrader onde a parada inicial pode estar mais próxima do que a distância final.

## Gestão de risco

- As distâncias de parada são definidas em MetaTrader pontos para corresponder ao EA. Eles são convertidos em incrementos de preço absoluto usando o `PriceStep` do instrumento (ou `MinStep` quando a etapa principal não está disponível).
- A estratégia nunca faz pirâmides nas negociações. Uma nova posição só é aberta quando a anterior estiver totalmente fechada.
- `StartProtection()` é invocado na inicialização para que a infraestrutura de proteção de StockSharp permaneça sincronizada com os níveis de parada gerenciados manualmente.

## Parâmetros

| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `RsiPeriod` | RSI comprimento de lookback. | `28` |
| `QualificationLength` | Número de velas consecutivas que RSI deve permanecer na zona extrema antes que um sinal seja confirmado. | `12` |
| `UpperThreshold` | Nível RSI que qualifica uma configuração de baixa. | `55` |
| `LowerThreshold` | Nível RSI que qualifica uma configuração otimista. | `45` |
| `StopLossPoints` | Distância da parada final em MetaTrader pontos. Convertido em preço absoluto de cada vela. Defina como `0` para desativar o rastreamento. | `21` |
| `InitialStopPoints` | Distância inicial de parada de proteção em MetaTrader pontos aplicada imediatamente após a entrada. Defina como `0` para pular a parada inicial. | `11` |
| `CandleType` | Tipo de vela usado para avaliação de sinal (5 minutos por padrão). | `5-minute time frame` |

## Diretrizes de uso

- Certifique-se de que a etapa de preço do instrumento corresponda ao tamanho do ponto usado durante a otimização MetaTrader. Para símbolos FX de cinco dígitos, um ponto é igual a 0,00010 (um pip), portanto, as distâncias padrão reproduzem os deslocamentos de 11/21 pontos do EA.
- Como o método é contrário, os sinais são mais confiáveis em mercados variados. Considere ampliar os limites ou aumentar `QualificationLength` para recursos de tendência.
- A estratégia usa a propriedade da classe base `Volume` para tamanho do pedido. Configure-o na UI ou via código antes de iniciar a estratégia.
- A otimização pode ser realizada nos limites RSI, comprimento de qualificação e distâncias de parada graças aos sinalizadores `SetCanOptimize()`.

## Notas de conversão

- O manuseio de velas, o cálculo de RSI e a restrição de uma posição refletem a implementação de MetaTrader. Nenhum filtro adicional foi introduzido.
- O trailing stop atualiza o nível de stop com o fechamento da vela anterior, assim como a lógica MQL4 `Close[1]`, garantindo que ambas as versões saiam pelo mesmo preço quando ocorrer uma reversão.
- As verificações de erros do script MQL4 (contagem de barras, margem livre) são omitidas intencionalmente porque StockSharp lida internamente com a preparação dos dados e a disponibilidade do portfólio.
