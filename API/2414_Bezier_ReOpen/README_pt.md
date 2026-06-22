# Estratégia Bezier ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Bezier ReOpen** aplica um indicador personalizado de curva Bezier para seguir a direção da tendência.
Quando o indicador vira para cima e o último valor está acima do anterior, a estratégia pode abrir uma posição comprada.
Quando o indicador vira para baixo, pode abrir uma posição vendida. As posições existentes são fechadas quando o indicador muda de direção.
Após entrar, posições adicionais são reabertas cada vez que o preço avança um passo definido pelo usuário, permitindo escalar na tendência.

Esta implementação é baseada no Expert Advisor MetaTrader `Exp_Bezier_ReOpen.mq5` (ID 16883).

## Detalhes

- **Indicador**: Curva Bezier construída a partir dos últimos `BPeriod` preços e parâmetro `T` definindo a tensão da curva.
- **Entrada**:
  - **Comprado**: a inclinação do indicador vira para cima e o valor atual está acima do valor anterior.
  - **Vendido**: a inclinação do indicador vira para baixo e o valor atual está abaixo do valor anterior.
- **Saída**:
  - **Comprado**: a inclinação do indicador vira para baixo.
  - **Vendido**: a inclinação do indicador vira para cima.
- **Re-entrada**: após a entrada inicial, uma ordem extra é enviada cada vez que o preço se move `PriceStep` a partir do último preço de entrada, até `PosTotal` ordens.
- **Stops**: stop-loss e take-profit opcionais definidos em unidades absolutas de preço.

## Parâmetros

- `CandleType` – período do candle para cálculos. Padrão: 4 horas.
- `BPeriod` – número de barras para o cálculo Bezier. Padrão: 8.
- `T` – tensão da curva Bezier (0..1). Padrão: 0.5.
- `PriceType` – fonte de preço para o indicador (close, open, high, low, median, typical, weighted). Padrão: weighted.
- `PriceStep` – distância de preço para enviar ordens adicionais. Padrão: 300.
- `PosTotal` – número máximo de posições na sequência de escalonamento. Padrão: 10.
- `BuyPosOpen` – permitir abrir posições compradas. Padrão: true.
- `SellPosOpen` – permitir abrir posições vendidas. Padrão: true.
- `BuyPosClose` – permitir fechar comprados em sinal oposto. Padrão: true.
- `SellPosClose` – permitir fechar vendidos em sinal oposto. Padrão: true.
- `StopLoss` – stop-loss em unidades de preço. Padrão: 1000.
- `TakeProfit` – take-profit em unidades de preço. Padrão: 2000.

## Tags de Filtro
- Categoria: Seguidor de tendência
- Direção: Ambos
- Indicadores: Personalizado
- Stops: Opcional
- Complexidade: Moderado
- Período: Médio prazo
- Nível de risco: Moderado
