# ATR Estratégia do Step Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia ATR Step Trader é uma porta direta do consultor especialista MetaTrader5 `atrTrader.mq5`. Ele combina um filtro de média móvel rápida/lenta com regras de quebra e pirâmide baseadas em Average True Range (ATR). A porta mantém o fluxo de trabalho baseado em barras do EA original: apenas velas concluídas são processadas, o SMA rápido deve ficar acima ou abaixo do SMA lento para um número fixo de barras, e cada decisão é ancorada em múltiplos de ATR para normalizar as distâncias entre os mercados.

## Indicadores e dados
- **Médias móveis simples (SMA).** Duas médias móveis (`FastPeriod` e `SlowPeriod`) definem o filtro de tendência principal. Ambos são aplicados à série de velas de assinatura.
- **Average True Range (ATR).** Um indicador `AverageTrueRange` (`AtrPeriod`) converte volatilidade em distâncias de preço. Cada cálculo de breakout, add-on e stop usa ATR múltiplos.
- **Canais de preços mais altos/mais baixos.** Os indicadores `Highest` e `Lowest` rastreiam os máximos e mínimos extremos das velas `MomentumPeriod` mais recentes. Eles substituem as chamadas `iHighest`/`iLowest` do código MQL.
- **Período.** O tipo de vela padrão é uma hora (`TimeSpan.FromHours(1)`), refletindo o comportamento `PERIOD_CURRENT` do script original. Você pode mudar para qualquer outro período editando o parâmetro `CandleType`.

## Lógica de entrada
1. Espere a vela terminar. Velas inacabadas são ignoradas para permanecerem sincronizadas com o protetor MT5 OnTick + iTime.
2. Atualize os contadores de sequência de média móvel. Uma seqüência de alta aumenta quando o SMA rápido é impresso acima do SMA lento; uma faixa de baixa aumenta quando é impressa abaixo. Leituras mistas redefinem a faixa oposta.
3. Quando a seqüência de alta atingir `MomentumPeriod`, verifique se o preço de fechamento ainda está abaixo da alta recente em pelo menos `StepMultiplier * ATR`. Se sim, compre no mercado.
4. Quando a seqüência de baixa atingir `MomentumPeriod`, verifique se o preço de fechamento ainda está acima da mínima recente em pelo menos `StepMultiplier * ATR`. Se sim, venda no mercado.
5. Cada nova posição inicializa o estado direcional: a estratégia lembra os preços preenchidos mais altos e mais baixos de cada lado para que as pirâmides posteriores tenham âncoras de referência. A primeira ordem também atribui um stop do tamanho da volatilidade (`StepMultiplier * StopMultiplier * ATR`).

## Gestão de posição
- **Pirâmide:** Embora o número de entradas ativas esteja abaixo de `PyramidLimit`, a estratégia adiciona outra unidade sempre que o preço se afasta `+/- StepsMultiplier * ATR` da referência extrema atual. Isso reflete a grade de escala “Etapas” do EA e funciona em direções favoráveis ​​e desfavoráveis.
- **Paradas de proteção:** A parada inicial para uma nova ordem fica a `StepMultiplier * StopMultiplier * ATR` de distância do preço de preenchimento. Quando a pirâmide está cheia, os stops são reduzidos para `StepMultiplier * ATR` atrás (para posições compradas) ou à frente (para posições vendidas) do último fechamento, emulando a atualização final de EA quando três posições estão abertas.
- **Saídas adversas:** Se o preço recuar `StepsMultiplier * ATR` além do extremo rastreado, a estratégia sai imediatamente de todas as posições desse lado com uma ordem de mercado. Isso captura a lógica EA que descarta toda a pilha quando o preço ultrapassa a borda da escada mais recente.
- **Redefinição de estado:** Após uma saída completa, os contadores de sequência e as referências de parada ATR são redefinidos para que uma nova sequência de tendência seja desenvolvida antes da reentrada.

## Parâmetros
| Grupo | Nome | Descrição | Padrão |
| --- | --- | --- | --- |
| Filtro de tendências | `FastPeriod` | Comprimento SMA rápido que mede a direção de curto prazo. | `70` |
| Filtro de tendências | `SlowPeriod` | Comprimento SMA lento que mede a direção de longo prazo. | `180` |
| Filtro de tendências | `MomentumPeriod` | Número de velas concluídas consecutivas que devem confirmar a tendência. | `50` |
| Volatilidade | `AtrPeriod` | Janela ATR usada para todos os cálculos de distância. | `100` |
| Lógica de entrada | `StepMultiplier` | ATR múltiplo que bloqueia os rompimentos iniciais. | `4` |
| Lógica de entrada | `StepsMultiplier` | ATR múltiplo que separa as camadas da pirâmide. | `2` |
| Gestão de Risco | `StopMultiplier` | Multiplicador extra aplicado à parada inicial além da distância do passo base. | `3` |
| Dimensionamento de posição | `PyramidLimit` | Número máximo de entradas por direção. | `3` |
| Negociação | `TradeVolume` | Volume de estratégia enviado com cada ordem de mercado. | `1` |
| Geral | `CandleType` | Tipo de vela (prazo) utilizado para a assinatura. | `TimeFrame(1h)` |

## Notas práticas
- A versão StockSharp usa a propriedade de estratégia `Volume` para dimensionamento. Ajuste `TradeVolume` para corresponder ao tamanho do contrato do seu instrumento antes de entrar no ar.
- Presume-se que as ordens de mercado sejam atendidas imediatamente, assim como o uso de `CTrade.Buy`/`Sell` pelo MT5. Em mercados fracos, você pode querer substituir as ordens de mercado por ordens de limite ou stop.
- As referências alto/baixo replicam as variáveis `h_price` e `l_price` do EA e são atualizadas sempre que uma nova camada é adicionada ou removida. Eles são essenciais para determinar quando adicionar ou liberar a escada.
- Como o EA armazena stop loss por posição enquanto StockSharp os gerencia no nível da estratégia, a porta aplica a lógica de stop mais rígida a toda a pilha. Isso proporciona o mesmo comportamento (todas as posições saem juntas) com menos ordens da corretora para gerenciar.
- Sempre teste a estratégia em simulação. As distâncias ATR adaptam-se à volatilidade, mas em mercados com gaps ou altas derrapagens o risco realizado ainda pode exceder a distância de parada projetada.
