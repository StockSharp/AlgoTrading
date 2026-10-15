# Estratégia MACD Waterline Cross Expectator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia vai comprado quando a linha de sinal MACD cruza acima do nível zero e vai vendido quando cruza abaixo. O gerenciamento de risco usa um stop loss e um multiplicador de risco-benefício configurável para definir a distância do take profit.

## Lógica
- Calcular o indicador MACD com períodos de EMA rápida, EMA lenta e sinal configuráveis.
- Rastrear o valor da linha de sinal em cada vela concluída.
- Quando a linha de sinal cruza de negativo para positivo e a estratégia está pronta para comprar, uma ordem de mercado comprada é colocada.
- Quando a linha de sinal cruza de positivo para negativo e a estratégia está pronta para vender, uma ordem de mercado vendida é colocada.
- Níveis de stop loss e take profit são definidos automaticamente para cada nova posição.

## Parâmetros
- **FastEmaPeriod** – comprimento da EMA rápida usada no MACD.
- **SlowEmaPeriod** – comprimento da EMA lenta usada no MACD.
- **SignalPeriod** – comprimento da EMA da linha de sinal.
- **StopLoss** – distância ao stop loss em unidades de preço absolutas.
- **Volume** – tamanho da ordem usada para novas posições.
- **RiskBenefitRatios** – proporções predefinidas de 1:5 a 1:1 que definem a distância do take profit.
- **CandleType** – período das velas utilizado pela estratégia.

## Notas
- A estratégia alterna entre operações compradas e vendidas usando uma flag interna.
- As negociações são executadas a preços de mercado e sempre fecham e revertem a posição atual.
