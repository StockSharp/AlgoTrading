# Estratégia Moving Averages
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o expert advisor clássico de cruzamento de médias móveis do MQL. Ela usa APIs de alto nível do StockSharp para monitorar duas médias móveis simples calculadas a partir da série de candles selecionada. Sinais são gerados quando a média rápida cruza a lenta, e a estratégia pode opcionalmente fechar uma posição ativa quando ocorre o cruzamento oposto.

## Lógica de negociação
- Assinar o tipo de candle configurado e calcular valores de SMA rápida e lenta em cada candle concluído.
- Detectar um cruzamento altista quando a SMA rápida passa de abaixo para acima da SMA lenta. Se nenhuma posição estiver ativa, abrir uma posição comprada com o volume especificado.
- Detectar um cruzamento baixista quando a SMA rápida passa de acima para abaixo da SMA lenta. Se nenhuma posição estiver ativa, abrir uma posição vendida com o volume especificado.
- Opcionalmente fechar uma posição existente imediatamente quando o cruzamento oposto é detectado, espelhando a chave "Close on Opposite Signal" do script original.

## Gestão de risco
- Aplicar stop loss e take profit fixos expressos em pontos de preço. Ambos os níveis são recalculados para cada nova entrada.
- Mover o stop protetor para break-even depois que o preço percorre a distância de gatilho configurada e manter um offset adicional como lucro travado.
- Ativar trailing stop quando a posição ganha a distância inicial definida. O stop é deslocado usando o preço de candle mais favorável e nunca se move contra a operação.

## Parâmetros
- **Fast MA Period:** comprimento da SMA rápida usada para detecção de cruzamento.
- **Slow MA Period:** comprimento da SMA lenta usada para detecção de cruzamento.
- **Trade Volume:** tamanho da ordem em lotes.
- **Stop Loss (points):** distância em pontos do instrumento para o stop loss inicial.
- **Take Profit (points):** distância em pontos do instrumento para o take profit inicial.
- **Break-even Trigger:** distância de lucro que ativa mover o stop para break-even.
- **Break-even Offset:** pontos adicionais mantidos como lucro após ativar break-even.
- **Trailing Start:** distância de lucro exigida antes de habilitar o trailing stop.
- **Trailing Distance:** distância mantida entre preço e trailing stop.
- **Close On Opposite:** se uma operação ativa deve ser fechada quando um cruzamento oposto aparece.
- **Candle Type:** série de candles usada para cálculos de indicadores.

## Notas de uso
- Garanta que a estratégia esteja anexada a um ativo com `PriceStep` válido. Quando o passo está indisponível, um valor de 1 é usado.
- Gestão de trailing e break-even opera em candles concluídos, correspondendo ao comportamento do EA original que reage no fechamento da barra.
- Otimize os comprimentos das médias móveis e ajustes de risco para adaptar o sistema a diferentes mercados ou timeframes.
