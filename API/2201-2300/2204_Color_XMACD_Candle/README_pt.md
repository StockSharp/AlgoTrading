# Estratégia de Vela Color XMACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma implementação StockSharp do consultor especialista "ColorXMACDCandle". Opera usando o indicador MACD e interpreta mudanças de cor do histograma ou da sua linha de sinal como sinais de entrada.

## Ideia

A estratégia analisa a inclinação de um componente MACD:

- **Modo Histogram** – Uma nova barra do histograma que sobe acima da barra anterior sinaliza momentum de alta crescente. Uma nova barra que desce abaixo da anterior sinaliza momentum de baixa.
- **Modo Signal line** – Em vez disso, usa-se a inclinação da linha de sinal MACD. Uma inclinação ascendente age como sinal de compra, enquanto uma descendente age como sinal de venda.

Quando o componente escolhido vira para cima e não estava a subir antes, qualquer posição vendida pode ser fechada e uma nova posição comprada pode ser aberta. Quando o componente vira para baixo e não estava a cair antes, qualquer posição comprada pode ser fechada e uma posição vendida pode ser aberta.

O comportamento de abertura e fecho de posições é controlado por parâmetros separados, permitindo ao utilizador ativar ou desativar cada ação de forma independente.

## Parâmetros

- `Mode` – Fonte de sinais: `Histogram` ou `SignalLine`.
- `FastPeriod` – Período da EMA rápida para MACD.
- `SlowPeriod` – Período da EMA lenta para MACD.
- `SignalPeriod` – Período de suavização do sinal MACD.
- `EnableBuyOpen` – Permitir abertura de posições compradas.
- `EnableSellOpen` – Permitir abertura de posições vendidas.
- `EnableBuyClose` – Permitir fecho de posições compradas.
- `EnableSellClose` – Permitir fecho de posições vendidas.
- `CandleType` – Tipo de vela para os cálculos.

## Regras de negociação

1. Subscrever a série de velas selecionada e calcular o indicador MACD.
2. Acompanhar a inclinação do histograma ou da linha de sinal dependendo do modo selecionado.
3. Quando a inclinação vira para cima, fechar qualquer posição vendida (se permitido) e opcionalmente abrir uma posição comprada.
4. Quando a inclinação vira para baixo, fechar qualquer posição comprada (se permitido) e opcionalmente abrir uma posição vendida.

A estratégia não inclui mecanismos de stop-loss ou take-profit. A gestão de risco pode ser adicionada separadamente se necessário.
