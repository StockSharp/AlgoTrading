# Estratégia Perceptron AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa um perceptron simples sobre o Accelerator Oscillator (AC).
O valor de AC da vela atual e de três deslocamentos passados são multiplicados por pesos ajustáveis.
A soma desses produtos forma a saída do perceptron que determina a direção da operação.

## Como funciona

1. Calcular o Accelerator Oscillator (AC) a partir da diferença entre o Awesome Oscillator e sua SMA de 5 períodos.
2. Armazenar os últimos 22 valores de AC para acessar deslocamentos de 0, 7, 14 e 21 barras.
3. Calcular a saída do perceptron:
   `P = (X1-100)*AC[0] + (X2-100)*AC[7] + (X3-100)*AC[14] + (X4-100)*AC[21]`.
4. Se `P > 0` abrir ou manter uma posição comprada; se `P < 0` abrir ou manter uma posição vendida.
5. Quando uma posição ganha pelo menos `StopLoss` pontos além do nível de stop inicial:
   - Se o perceptron mudar de direção, inverter a posição.
   - Caso contrário, trailar o stop para o novo preço menos/mais `StopLoss`.

## Parâmetros

- **X1** – peso para o valor AC atual (padrão 288).
- **X2** – peso para AC de 7 barras atrás (padrão 216).
- **X3** – peso para AC de 14 barras atrás (padrão 144).
- **X4** – peso para AC de 21 barras atrás (padrão 72).
- **Stop Loss** – limiar de rastreamento e inversão em unidades de preço (padrão 300).
- **Volume** – volume da ordem (padrão 1).
- **Candle Type** – série de velas para assinar (padrão 5 minutos).

## Regras de trading

- Entrar comprado quando `P > 0` e não há posição aberta.
- Entrar vendido quando `P < 0` e não há posição aberta.
- Para posições abertas, mover o stop-loss após o preço se mover `Stop Loss * 2` em lucro.
- Inverter a posição se a saída do perceptron mudar de sinal nesse momento.

## Versão original

Convertido do script MQL4 `auto_m5.mq4` localizado em `MQL/11102`.
