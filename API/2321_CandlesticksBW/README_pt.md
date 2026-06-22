# Estratégia CandlesticksBW
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a abordagem CandlesticksBW de Bill Williams. Colore cada vela usando o momentum do Awesome Oscillator (AO) e do Accelerator Oscillator (AC). A estratégia abre ou fecha posições com base nas transições entre cores de alta e de baixa.

## Como funciona
- Calcula o AO como a diferença entre as SMAs de 5 e 34 períodos do preço mediano.
- Calcula o AC como AO menos a SMA de 5 períodos do AO.
- Cada vela é classificada em seis cores dependendo do crescimento de AO/AC e da direção da vela.
- Uma configuração de alta ocorre quando a penúltima vela é de alta (cor 0 ou 1). Se a cor da última vela for superior a 1, uma posição comprada é aberta e as posições vendidas são fechadas.
- Uma configuração de baixa ocorre quando a penúltima vela é de baixa (cor 4 ou 5). Se a cor da última vela for inferior a 4, uma posição vendida é aberta e as posições compradas são fechadas.
- Stops e alvos são aplicados via `StartProtection`.

## Parâmetros
- `CandleType` – período das velas.
- `SignalBar` – barra de deslocamento para avaliação de sinal.
- `StopLoss` – distância do stop loss em pontos.
- `TakeProfit` – distância do take profit em pontos.
- `BuyPosOpen` – permitir abertura de posições compradas.
- `SellPosOpen` – permitir abertura de posições vendidas.
- `BuyPosClose` – permitir fechamento de posições compradas.
- `SellPosClose` – permitir fechamento de posições vendidas.

## Indicadores
- Awesome Oscillator (derivado de SMAs).
- Accelerator Oscillator.

## Regras de negociação
- **Entrada comprada:** cor da penúltima vela <2 e última cor >1.
- **Entrada vendida:** cor da penúltima vela >3 e última cor <4.
- **Saída comprada:** na condição de entrada vendida se posição >0.
- **Saída vendida:** na condição de entrada comprada se posição <0.
