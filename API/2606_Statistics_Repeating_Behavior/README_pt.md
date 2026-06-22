# Estratégia de Estatística de Comportamento Repetido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia intradiária que estuda como os candles se comportaram no mesmo horário do dia durante as últimas N sessões de negociação. Para cada nova barra, compara os tamanhos acumulados dos corpos altistas e baixistas dos dias anteriores. Se a pressão altista dominar, abre uma posição comprada na abertura da barra; caso contrário, vai vendido. As posições são encerradas na barra seguinte e um stop loss fixo em pips imita a lógica original do MetaTrader. O tamanho da posição segue um martingale de proporção áurea, crescendo após perdas e reiniciando após ganhos.

## Lógica de negociação

1. No início de cada novo candle, encerrar qualquer posição aberta da barra anterior.
2. Buscar candles dos últimos `HistoryDays` dias de negociação que abriram na mesma hora e minuto.
3. Somar os corpos dos candles (em pontos) separadamente para fechamentos altistas e baixistas, ignorando corpos menores que `MinimumBodyPoints`.
4. Se a soma altista superar a baixista → abrir uma posição comprada com o volume atual.
5. Se a soma baixista superar a altista → abrir uma posição vendida.
6. Aplicar um stop loss de `StopLossPips` convertido pelo passo de preço mínimo do instrumento. O stop é verificado contra os extremos intrabarra quando o candle é concluído.
7. Quando a operação é encerrada:
   - Se o resultado for lucrativo, redefinir o volume para `InitialVolume`.
   - Caso contrário, multiplicar o volume atual por `MartingaleFactor` (respeitando o passo de volume e os limites).

## Parâmetros

- **HistoryDays** *(padrão: 10)* — número de dias anteriores a incluir nas estatísticas.
- **MinimumBodyPoints** *(padrão: 10)* — candles com corpo menor que este limiar (em pontos) são ignorados.
- **StopLossPips** *(padrão: 15)* — distância em pips do stop de proteção.
- **InitialVolume** *(padrão: 0.1)* — tamanho inicial da ordem antes de ajustes por martingale.
- **MartingaleFactor** *(padrão: 1.618)* — multiplicador aplicado após uma operação perdedora.
- **CandleType** *(padrão: 1 hora)* — período usado para os candles.

## Características de negociação

- **Lado do mercado**: Ambos, comprado e vendido, dependendo das estatísticas.
- **Período**: Configurável (horário por padrão) com correspondência exata por hora e minuto.
- **Gestão de posição**: Uma única posição por vez, encerrada na barra seguinte ou quando o stop loss é acionado.
- **Risco**: Usa stop fixo em pips e sizing por martingale, que pode aumentar o volume rapidamente após perdas consecutivas.
- **Instrumentos**: Funciona com instrumentos que fornecem um `MinPriceStep` válido e limites de volume.

## Notas de implementação

- Os corpos dos candles são armazenados por minuto do dia em uma fila deslizante limitada por `HistoryDays`.
- Os volumes são normalizados para o passo de volume do instrumento e limitados por `MinVolume`/`MaxVolume`.
- A detecção do stop loss depende dos extremos do candle concluído para emular a execução intrabarra do expert MQL5 original.
- Todos os comentários de código inline estão em inglês, conforme os requisitos do repositório.
