# Estratégia MovingUp MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um sistema de cruzamento de médias móveis com gerenciamento de risco opcional.
Abre uma posição comprada quando a média móvel rápida cruza acima da média móvel lenta e abre uma posição vendida no cruzamento oposto.

## Parâmetros
- **Fast MA** (`FastLength`): período da média móvel simples rápida.
- **Slow MA** (`SlowLength`): período da média móvel simples lenta.
- **Use TP** (`UseTakeProfit`): ativa a regra de take profit.
- **TP** (`TakeProfit`): distância em preço para realizar lucros.
- **Use SL** (`UseStopLoss`): ativa a regra de stop loss.
- **SL** (`StopLoss`): distância em preço para o stop loss.
- **Use TS** (`UseTrailingStop`): ativa a lógica de trailing stop.
- **TS** (`TrailingStop`): distância do trailing stop em preço.
- **Candle** (`CandleType`): tipo de vela usado para os cálculos.

## Lógica de trading
1. Subscrever dados de velas e calcular dois indicadores SMA.
2. Detectar cruzamentos das MAs rápida e lenta.
3. Entrar comprado quando a MA rápida cruza acima da MA lenta se não houver posição comprada.
4. Entrar vendido quando a MA rápida cruza abaixo da MA lenta se não houver posição vendida.
5. Aplicar gerenciamento de risco em cada nova vela:
   - Realizar lucros quando o preço avança a distância especificada.
   - Stop loss quando o preço se move contra a posição a distância especificada.
   - O trailing stop protege o lucro uma vez que o preço se move favoravelmente.

## Estratégia MQL original
O script MQL4 original `ma_v_1_3_3.mq4` contém inúmeras funcionalidades adicionais, como lógica de incremento/decremento por passos e dimensionamento complexo de posições. Esta versão em C# foca no cruzamento central de médias móveis e nos controles de risco essenciais.
