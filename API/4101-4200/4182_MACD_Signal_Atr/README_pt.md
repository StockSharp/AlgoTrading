# MACD Sinal ATR Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **MACD Estratégia de Sinal** transporta o MetaTrader especialista `MACD_signal.mq4` para StockSharp. O robô original mediu a
MACD histograma contra uma banda de volatilidade baseada em ATR e abriu uma única ordem de mercado sempre que o histograma cruzou essa faixa
banda. Esta versão C# recria a mesma lógica de quebra de impulso usando o API de alto nível de StockSharp, armazena o anterior
histograma e ATR leituras explicitamente e documenta todas as regras de gerenciamento de dinheiro com parâmetros nomeados e inglês
comentários no código-fonte.

Ao contrário da implementação MetaTrader que modificou diretamente os tickets, a porta StockSharp funciona com posições líquidas. Isso
portanto, fecha a exposição atual antes de mudar de direção e atualiza os trailing stops internamente, em vez de depender de
chamadas `OrderModify` do lado do corretor.

## Lógica de negociação
1. Assine a série de velas configuradas (`CandleType`) e processe **apenas** velas finalizadas para evitar barra parcial
barulho.
2. Alimente um indicador `MovingAverageConvergenceDivergenceSignal` com os comprimentos escolhidos de rápido, lento e sinal EMA. O
o valor do histograma (`MACD - signal`) é armazenado toda vez que uma barra fecha.
3. Calcule o `AverageTrueRange` nas mesmas velas. O valor da barra **anterior** é multiplicado por
`ThresholdMultiplier` para recriar o limite `rr = ATR * LEVEL` de MQL.
4. Detecte um rompimento de alta quando o histograma atual exceder `+threshold` enquanto o histograma anterior ainda estava abaixo
isso. Se a conta for fixa ou curta e a negociação longa for permitida por `Direction`, envie uma ordem de compra de mercado dimensionada por
`TradeVolume`.
5. Detecte um rompimento de baixa quando o histograma cruzar abaixo de `-threshold` depois de estar acima dele na vela anterior. Se
a estratégia é plana ou longa e a negociação curta está habilitada, emita uma ordem de venda de mercado dimensionada em `TradeVolume`.
6. Gerencie posições abertas em cada barra:
   - feche as posições compradas assim que o histograma ficar negativo; fechar shorts quando ficar positivo;
   - monitore a distância fixa de lucro (`TakeProfitPoints`) em relação aos máximos ou mínimos das velas para emular o original
MetaTrader parâmetro de lucro;
   - atualizar os trailing stops quando o preço se mover mais de `TrailingStopPoints` longe da entrada e saída se a vela revisitar
o nível final. O stop longo segue o fechamento como uma proxy para o preço de compra, enquanto o stop curto segue o fechamento como
um proxy para o preço de venda.
7. O EA se recusa a negociar quando `TakeProfitPoints` está abaixo do mínimo histórico de 10 pontos, correspondendo à verificação de proteção
presente no código MQL.

## Gestão de risco
- **Ordem única por vez.** A estratégia sempre se estabiliza antes de abrir uma nova posição, refletindo a original
Requisito `OrdersTotal() < 1`.
- **Volume fixo.** `TradeVolume` substitui a entrada `Lots` e também é copiado para `Strategy.Volume` para que as ações manuais da IU usem
o mesmo tamanho.
- **Take-profit fixo.** `TakeProfitPoints` converte a distância de MQL pontos para o tamanho do tick do instrumento usando
`Security.PriceStep`.
- **Saída baseada em indicador.** Uma inversão do sinal do histograma desencadeia uma saída imediata do mercado, garantindo que o EA não permaneça em
o mercado quando a dinâmica se inverte.
- **Trailing stop.** Quando o preço se move a favor da negociação em mais do que o número de etapas configurado, o stop é puxado
dentro da zona de lucro e segue o preço de fechamento sem nunca retroceder.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `TradeVolume` | `decimal` | `10` | Tamanho do pedido (lotes) usado para cada entrada no mercado e copiado para `Strategy.Volume`. |
| `TakeProfitPoints` | `int` | `10` | Distância até a meta fixa de lucro expressa em etapas de preço. Valores abaixo de 10
desativar a negociação. |
| `TrailingStopPoints` | `int` | `25` | Distância em etapas de preço para o trailing stop. Defina como `0` para desativar o rastreamento. |
| `FastPeriod` | `int` | `9` | Comprimento do EMA rápido dentro do indicador MACD. |
| `SlowPeriod` | `int` | `15` | Comprimento do EMA lento dentro do indicador MACD. |
| `SignalPeriod` | `int` | `8` | Comprimento do EMA usado para suavizar a linha de sinal MACD. |
| `ThresholdMultiplier` | `decimal` | `0.004` | Multiplicador aplicado à barra anterior ATR para construir a banda de ruptura. |
| `AtrPeriod` | `int` | `200` | Número de velas usadas para calcular o filtro de volatilidade ATR. |
| `CandleType` | `DataType` | Prazo de 30 minutos | Período primário processado pela estratégia. |

## Diferenças do consultor especialista original
- MetaTrader expõe `AccountFreeMargin()` e se recusa a negociar se o valor for muito pequeno. StockSharp estratégias não
têm o mesmo instantâneo de margem, então a porta omite essa verificação. Os controles de risco em nível de portfólio devem ser tratados fora do
estratégia quando necessário.
- A versão MQL ajustou ordens de parada com `OrderModify`. StockSharp trabalha com posições líquidas, então a conversão gerencia
sai internamente monitorando os máximos/mínimos das velas e as variáveis de trailing stop.
- MetaTrader contou "barras" manualmente e imprimiu um aviso quando menos de 100 velas estavam disponíveis. StockSharp depende de
prontidão do indicador (`BindEx`) para que a estratégia permaneça ociosa automaticamente até que MACD e ATR tenham dados suficientes.
- A porta armazena explicitamente os valores anteriores de ATR e do histograma para reproduzir a comparação de limite de `Delta`/`Delta1`
sem violar a regra de StockSharp contra indexação de indicadores aleatórios.

## Dicas de uso
- Mantenha `Security.PriceStep`, `Security.MinVolume` e `Security.VolumeStep` precisos para aumentar o volume de conversões e obter lucro
os cálculos permanecem alinhados com o câmbio.
- Aumente `ThresholdMultiplier` ou `AtrPeriod` quando a estratégia é negociada com muita frequência em mercados agitados; diminua-os para
tornar o sistema mais sensível a quebras de volatilidade.
- Menor `TradeVolume` ao executar em instrumentos alavancados ou de alta volatilidade, porque o script original presumia grandes
tamanhos de lote em símbolos Forex.
- Combine a estratégia com filtros de prazo maior por meio da propriedade `Direction` integrada se desejar permitir apenas
posições compradas ou vendidas durante regimes de mercado específicos.
