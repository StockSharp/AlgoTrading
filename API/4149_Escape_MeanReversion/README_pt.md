# Estratégia de Reversão à Média de Fuga
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Escape é uma versão StockSharp do MetaTrader 4 consultor especialista `escape.mq4`. O robô original negocia um gráfico de cinco minutos e reage a oportunidades de reversão à média: compra quando o preço cai abaixo de uma média móvel curta e vende quando o preço sobe acima de outra média rápida. Cada posição é protegida por um take-profit e stop-loss de distância fixa expressos em MetaTrader pontos. A implementação C# mantém a mesma lógica minimalista enquanto expõe todas as distâncias ajustáveis ​​como parâmetros de estratégia.

## Lógica de negociação
1. **Inicialização**
   - Assine a série configurável `CandleType` (velas de cinco minutos por padrão).
   - Crie dois indicadores `SimpleMovingAverage` com comprimentos 5 e 4 que são alimentados com preços de abertura de velas.
   - Calcule o equivalente MetaTrader `Point` de `Security.PriceStep`; esse valor é reutilizado para converter distâncias estilo pip em preços absolutos.

2. **Processamento por vela**
   - Somente velas finalizadas são processadas via `SubscribeCandles(...).WhenCandlesFinished(ProcessCandle)`.
   - A estratégia primeiro verifica se uma posição existente atingiu seu stop-loss ou take-profit, comparando a máxima/mínima da vela com os níveis de saída registrados. Quando um nível é ultrapassado, a posição é fechada com uma ordem de mercado e ordens de saída duplicadas são evitadas através de sinalizações internas.
   - Se a conta for plana, os valores anteriores dos dois SMAs estiverem disponíveis, a negociação for permitida e o portfólio tiver capital suficiente (`Portfolio.CurrentValue >= MinimumMarginPerLot * TradeVolume`), a estratégia avalia as entradas:
     * **Entrada longa** – o fechamento atual está abaixo dos 5 períodos anteriores SMA de aberturas.
     * **Entrada curta** – o fechamento atual está acima dos 4 períodos anteriores SMA de aberturas.
   - Quando um sinal é acionado, os níveis de stop-loss e take-profit são calculados a partir do fechamento da vela usando as distâncias dos pontos configuradas e armazenados para monitoramento posterior.

3. **Gerenciamento de riscos**
   - `TradeVolume` define o tamanho do lote de cada ordem de mercado.
   - `MinimumMarginPerLot` aproxima a verificação `AccountFreeMargin` de MetaTrader. Se o valor do portfólio disponível for muito pequeno, a entrada será ignorada e uma mensagem de diagnóstico será registrada.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `LongTakeProfitPoints` | `10` | Distância de take-profit para posições longas em MetaTrader pontos. Defina como `0` para desativar o alvo. |
| `ShortTakeProfitPoints` | `10` | Distância de take-profit para posições curtas em MetaTrader pontos. Defina como `0` para desativar o alvo. |
| `LongStopLossPoints` | `1000` | Distância de stop-loss para posições longas em MetaTrader pontos. Defina como `0` para desativar a parada de proteção. |
| `ShortStopLossPoints` | `1000` | Distância de stop-loss para posições curtas em MetaTrader pontos. Defina como `0` para desativar a parada de proteção. |
| `TradeVolume` | `0.2` | Tamanho do lote utilizado no envio de ordens de mercado. |
| `MinimumMarginPerLot` | `500` | Requisito aproximado de capital por lote antes de abrir uma nova negociação. |
| `CandleType` | Período de cinco minutos | Série de velas que impulsiona atualizações de indicadores e geração de sinais. |

## Notas de implementação
- Os indicadores são atualizados manualmente dentro de `ProcessCandle` com preços de abertura de velas para que os valores armazenados sempre representem a barra anterior (espelhando os argumentos `shift=1` usados em `iMA`).
- Os níveis de saída são rastreados em campos decimais em vez de criar coleções adicionais, atendendo às diretrizes API de alto nível.
- Stops e metas são avaliados em relação aos extremos das velas; como apenas dados de OHLC estão disponíveis, a verificação de stop é realizada antes do take-profit para emular a prioridade do pedido de MetaTrader o mais próximo possível.
- A estratégia reúne velas com médias móveis e negociações próprias quando uma área do gráfico está disponível, simplificando a validação visual.

## Diferenças em relação à versão MetaTrader
- MetaTrader anexa ordens de stop-loss e take-profit diretamente aos tickets. A porta StockSharp os reproduz monitorando os máximos e mínimos das velas e enviando saídas de mercado; a ordem de execução intrabarra não pode ser garantida se ambos os níveis forem tocados na mesma barra.
- Os preços de entrada são derivados do fechamento da vela que acionou o sinal, em vez do lance/venda exato usado por MetaTrader, portanto, o tratamento de deslizamento e spread deve ser configurado no nível do conector.
- A guarda `AccountFreeMargin()` é aproximada por meio de `Portfolio.CurrentValue`. Usuários com modelos de margem mais detalhados podem estender `HasSufficientMargin` se necessário.
- Configurações cosméticas MQL, como cores, sons e deslizamento, são omitidas; a versão StockSharp concentra-se na lógica comercial principal.
