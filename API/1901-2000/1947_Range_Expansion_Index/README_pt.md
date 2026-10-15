# Estratégia de Índice de Expansão de Intervalo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o **Índice de Expansão de Intervalo (REI)** de Tom DeMark para avaliar a força e a fraqueza do preço. O indicador compara máximas e mínimas atuais com preços anteriores e oscila entre valores positivos e negativos.

## Como Funciona

- Quando o REI sobe acima do **Nível Inferior** (padrão `-60`) após ter estado abaixo dele, a estratégia abre uma posição **comprada**.
- Quando o REI cai abaixo do **Nível Superior** (padrão `60`) após ter estado acima dele, a estratégia abre uma posição **vendida**.
- As posições opostas são fechadas automaticamente quando ocorre um sinal oposto.

## Parâmetros

- `REI Period` – número de barras usadas no cálculo do REI (padrão `8`).
- `Up Level` – limiar superior que indica fraqueza do preço quando cruzado para baixo (padrão `60`).
- `Down Level` – limiar inferior que indica força do preço quando cruzado para cima (padrão `-60`).
- `Candle Type` – período dos candles para o cálculo do indicador (padrão `8 horas`).

## Uso

Anexe a estratégia a um ativo e inicie-a. A estratégia se inscreve na série de candles especificada e usa ordens a mercado para entrar ou sair de posições com base nos sinais do REI.
