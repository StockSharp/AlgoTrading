# Estratégia Color 3rdGen XMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia com base na direção de uma média móvel de terceira geração. O indicador é uma combinação de duas médias móveis exponenciais e fica azul quando sobe e rosa quando cai. Um sinal de compra é registrado quando a média gira para cima, e um sinal de venda é registrado quando gira para baixo.

As ordens são colocadas apenas em um horário especificado pelo usuário após o aparecimento de um sinal. As posições também podem ser fechadas quando o sinal oposto é detectado ou quando um período de retenção predefinido expira. Níveis opcionais de stop-loss e take-profit são medidos em pontos.

## Parâmetros

- **Length** – período de suavização da média de terceira geração.
- **StartHour** – hora em que novas posições podem ser abertas.
- **StartMinute** – minuto dentro da hora em que as aberturas são permitidas.
- **HoldMinutes** – tempo máximo para manter uma posição aberta.
- **Volume** – volume de ordem usado para entradas.
- **StopLoss** – distância de stop-loss em pontos. `0` desativa o stop.
- **TakeProfit** – distância de take-profit em pontos. `0` desativa o alvo.
- **UseLongEntries** – habilitar entradas compradas.
- **UseShortEntries** – habilitar entradas vendidas.
- **CloseLongBySignal** – fechar posições compradas quando um sinal de venda aparecer.
- **CloseShortBySignal** – fechar posições vendidas quando um sinal de compra aparecer.
- **CandleType** – período das velas usadas para cálculos.

## Lógica

1. Subscrever velas do período selecionado.
2. Calcular a média móvel de terceira geração para cada vela.
3. Detectar quando a média sobe ou cai entre velas consecutivas.
4. Armazenar um sinal de compra ou venda com base na mudança de direção.
5. No horário de abertura especificado, entrar na direção do sinal armazenado.
6. Fechar posições em sinais opostos, quando o tempo de retenção decorre ou quando os níveis de stop-loss/take-profit são atingidos.
