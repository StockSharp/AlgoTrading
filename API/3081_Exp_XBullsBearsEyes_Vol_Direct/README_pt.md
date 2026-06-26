# Estratégia Exp XBullsBearsEyes Vol Direta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do especialista MetaTrader **Exp_XBullsBearsEyes_Vol_Direct**. Ela recria o oscilador personalizado
construído a partir de Bulls Power e Bears Power, multiplica-o por uma fonte de volume configurável e aplica suavização adaptativa idêntica
ao indicador original. As decisões de negociação são guiadas exclusivamente pelo buffer de direção do indicador: o algoritmo reage a
oscilações de momentum em vez de cruzamentos de nível, abrindo ou fechando posições quando o histograma suavizado muda de inclinação.

Ao contrário de muitas conversões, a versão StockSharp mantém a etapa de ponderação por volume e o filtro gamma de quatro níveis usado
pelo código MQL. O oscilador é suavizado duas vezes com o mesmo método de média móvel — uma passagem para o histograma em si e uma para
o fluxo de volume — de modo que os sinais aparecem apenas quando ambos os componentes estão totalmente formados. A estratégia processa
apenas velas fechadas e suporta volume de ticks ou volume real negociado, tornando-a portável entre diferentes mercados.

## Lógica do indicador
1. Calcular Bulls Power e Bears Power com uma média móvel exponencial do preço de fechamento ao longo de `Period` velas.
2. Aplicar o filtro gamma original de quatro estágios (parâmetros `Gamma`, `L0`–`L3`) para combinar as duas forças em um histograma
   normalizado de -50 a +50.
3. Multiplicar o histograma pela fonte de volume selecionada (contagem de ticks ou volume negociado).
4. Suavizar o histograma e o volume bruto com a mesma família de médias móveis (`Method`, `SmoothingLength`, `SmoothingPhase`).
5. Derivar um buffer de direção: cor `0` quando o histograma suavizado sobe, cor `1` quando cai. Isso imita o `ColorDirectBuffer`
   da implementação MetaTrader.

Os buffers de limiar superior/inferior do indicador são calculados internamente, mas não são usados para filtros de negociação,
correspondendo ao comportamento do consultor especialista original que dependia apenas de mudanças de direção.

## Regras de negociação
- **Fechar vendidos** quando a direção da barra anterior era de alta (`olderColor = 0`).
- **Abrir comprados** se entradas compradas são permitidas, uma barra de alta é seguida por uma de baixa (`currentColor = 1`), e a
  estratégia não está já comprada.
- **Fechar comprados** quando a direção da barra anterior era de baixa (`olderColor = 1`).
- **Abrir vendidos** se entradas vendidas são permitidas, uma barra de baixa é seguida por uma de alta (`currentColor = 0`), e nenhuma
  posição comprada está ativa.
- Reversões de posição fecham o lado oposto primeiro e depois enviam uma ordem a mercado com o `OrderVolume` configurado.

Os sinais são avaliados com um deslocamento de barra configurável (`SignalBar`). O valor padrão de `1` emula o especialista MQL que
esperava uma vela completamente fechada antes de reagir à mudança de direção.

## Parâmetros
| Nome | Descrição |
|------|-----------|
| `CandleType` | Tipo/período de vela subscrito pela estratégia (padrão: velas de 2 horas). |
| `Period` | Período de lookback usado para Bulls/Bears Power. |
| `Gamma` | Fator de suavização (0…1) do filtro gamma adaptativo. |
| `VolumeMode` | Fonte de volume: contagem de ticks ou volume negociado. |
| `Method` | Família de médias móveis usada para suavizar histograma e volume (SMA, EMA, SMMA, LWMA, Jurik; tipos legados não suportados revertem para SMA). |
| `SmoothingLength` | Comprimento de ambos os estágios de suavização. |
| `SmoothingPhase` | Parâmetro de fase Jurik (mantido para compatibilidade). |
| `SignalBar` | Número de barras atrás a ler ao avaliar o buffer de direção. |
| `AllowBuyOpen` / `AllowSellOpen` | Habilitar ou desabilitar a abertura de posições compradas/vendidas. |
| `AllowBuyClose` / `AllowSellClose` | Habilitar ou desabilitar saídas forçadas em sinais opostos. |
| `OrderVolume` | Tamanho da ordem a mercado para novas entradas. |
| `StopLossPoints` | Stop de proteção opcional em passos de preço (0 desabilita o stop). |
| `TakeProfitPoints` | Alvo de proteção opcional em passos de preço (0 desabilita o alvo). |

## Notas de uso
- A estratégia opera sobre um único ativo retornado por `GetWorkingSecurities()` e funciona melhor em símbolos que fornecem um fluxo
  de volume estável.
- Volume de ticks é recomendado para símbolos FX à vista onde o volume real negociado não está disponível. Defina `VolumeMode` como
  `Real` para bolsas que publicam volume executado.
- As distâncias de stop-loss e take-profit são expressas em passos de preço e convertidas em unidades de preço absolutas usando o
  `PriceStep` do ativo.
- Como a lógica depende de mudanças de direção, valores consecutivamente iguais do histograma mantêm a direção anterior até que uma
  nova inclinação apareça, exatamente como na versão MetaTrader.
- A saída do gráfico exibe apenas velas de preço por padrão. Você pode adicionar plotagem personalizada para o histograma se a
  confirmação visual for necessária.
