# Estratégia Exp XWPR Histograma Vol Direto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é um port do StockSharp do expert advisor do MetaTrader **Exp_XWPR_Histogram_Vol_Direct**. Reproduz a abordagem
original de ponderar os valores de Williams %R pelo volume, suavizar o resultado e abrir operações quando a inclinação do histograma muda
de cor. As ordens são acionadas em velas completamente formadas e usam stop-loss e take-profit protetores opcionais medidos em passos de preço.

## Lógica central

1. Calcular Williams %R no período selecionado.
2. Deslocar o oscilador em +50, multiplicá-lo pela fonte de volume escolhida (tick ou real), e suavizar o fluxo com uma média móvel
   configurável.
3. Suavizar o volume bruto com a mesma média móvil para reconstruir as faixas do indicador (HighLevel2, HighLevel1, LowLevel1, LowLevel2).
4. Rastrear a cor da inclinação do histograma: azul (`0`) quando o valor suavizado sobe, magenta (`1`) quando cai. A estratégia
   mantém um buffer de histórico curto para comparar as últimas duas cores completas respeitando o parâmetro `SignalShift`.
5. Executar ações quando a cor anterior muda:
   - Transição de cor `0 → 1`: fechar vendidos (se habilitado) e opcionalmente abrir uma nova posição comprada.
   - Transição de cor `1 → 0`: fechar comprados (se habilitado) e opcionalmente abrir uma nova posição vendida.

A classificação de zona (Neutra/Alta/Baixa/Extrema) é registrada por contexto mas não bloqueia operações, correspondendo ao comportamento do
advisor original que lê apenas o buffer de cor.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `WilliamsPeriod` | Comprimento de retrospectiva para Williams %R. |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Multiplicadores aplicados ao volume suavizado para reconstruir as faixas do indicador. |
| `SmoothingType` | Família de média móvel usada tanto para o valor ponderado quanto para os fluxos de volume (SMA, EMA, SMMA, WMA, Hull, VWMA, DEMA, TEMA). |
| `SmoothingLength` | Comprimento da média móvel de suavização. |
| `SignalShift` | Quantas barras atrás ler o buffer de cor (1 reproduz o padrão do MetaTrader). |
| `EnableLongEntries` / `EnableShortEntries` | Permitir ou bloquear a abertura de posições compradas/vendidas. |
| `EnableLongExits` / `EnableShortExits` | Permitir ou bloquear o fechamento de posições compradas/vendidas. |
| `VolumeSource` | Escolher entre contagem de ticks ou volume real para ponderação. |
| `StopLossPoints` / `TakeProfitPoints` | Alvos protetores opcionais expressos em passos de preço. |
| `CandleType` | Tipo de vela e período usado para análise e trading. |

Use a propriedade base `Volume` da estratégia para definir o tamanho da entrada. A reversão de posição é tratada enviando a quantidade absoluta
de posição mais o tamanho de lote configurado, semelhante ao expert advisor MQL.

## Notas de uso

- A fase de suavização (`MA_Phase` no MetaTrader) não é suportada porque as médias móveis do StockSharp não expõem esse parâmetro.
- Certifique-se de que haja histórico suficiente carregado para o período escolhido para que as médias móveis estejam completamente formadas antes de o trading começar.
- A estratégia funciona em qualquer instrumento suportado pelo StockSharp; defina `CandleType` para a resolução desejada (por exemplo,
  período de 4 horas para corresponder aos padrões originais).
- A ponderação de volume de tick requer fontes de dados que forneçam contagens de tick dentro das mensagens de vela. Caso contrário, mude para volume real.

## Registro e visualização

A estratégia desenha velas e o indicador Williams %R na área de gráfico padrão. As ações de trading registram a zona detectada e o
valor do histograma suavizado para ajudar na depuração e comparação com a implementação de referência do MetaTrader.
