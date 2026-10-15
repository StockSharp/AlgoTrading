# Estratégia RSI Expert de Rompimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Adaptação da estratégia "RSI_Expert" do MetaTrader 5 que opera rompimentos de limiares do RSI.
- Usa um único indicador RSI para detectar reversões de momentum perto das regiões de sobrevenda/sobrecompra.
- Implementa o gerenciamento original de take-profit fixo, stop-loss e trailing-stop expressos em pips.

## Lógica da estratégia
1. Construir o RSI na série de velas selecionada (período padrão: 14).
2. Rastrear os dois valores de RSI completados mais recentes.
3. Ir comprado quando o RSI volta a subir acima do limiar inferior (20 por padrão) depois de ter estado abaixo dele.
4. Ir vendido quando o RSI volta a cair abaixo do limiar superior (60 por padrão) depois de ter estado acima dele.
5. Fechar qualquer exposição oposta antes de abrir uma nova posição para manter a direção líquida.
6. Gerenciar operações abertas com distâncias opcionais de stop-loss, take-profit e trailing-stop medidas em pips.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Período usado para agregação de velas. | Velas de 1 hora |
| `TradeVolume` | Tamanho da ordem usado para entradas. | 0.1 |
| `RsiPeriod` | Comprimento de lookback do RSI. | 14 |
| `RsiUpperLevel` | Limiar do RSI que sinaliza uma reversão de baixa. | 60 |
| `RsiLowerLevel` | Limiar do RSI que sinaliza uma reversão de alta. | 20 |
| `TakeProfitPips` | Distância do take-profit em pips (0 desabilita). | 60 |
| `StopLossPips` | Distância do stop-loss em pips (0 desabilita). | 0 |
| `TrailingStopPips` | Distância do trailing-stop em pips (0 desabilita o trailing). | 15 |
| `TrailingStepPips` | Melhora mínima de preço antes que o trailing-stop seja deslocado novamente. | 5 |

> **Interpretação de pips:** No port do StockSharp, um "pip" equivale a um `Security.PriceStep`. Em símbolos FX com cotação fracionada, certifique-se de que o passo de preço corresponde à convenção de pips do instrumento; caso contrário, ajuste as distâncias de entrada adequadamente.

## Gestão de riscos
- O take-profit e o stop-loss são avaliados em cada vela completada usando o preço médio de posição mais recente.
- O trailing-stop é ativado somente após o movimento exceder `TrailingStopPips + TrailingStepPips` e então acompanha o fechamento em `TrailingStopPips` conforme o preço avança.
- As verificações de stop usam as máximas/mínimas das velas para emular gatilhos intra-barra; quando ativados, a posição é fechada a mercado.

## Notas de conversão
- A API de alto nível é usada (`SubscribeCandles` + `Bind`), e os valores do RSI são consumidos diretamente do callback de vinculação sem buffers de indicadores manuais.
- A lógica do trailing-stop reproduz as condições MQL, incluindo o limiar de passo antes de cada ajuste.
- A estratégia redefine o estado de trailing sempre que a exposição muda ou fecha para evitar que níveis obsoletos sejam carregados para uma nova operação.
