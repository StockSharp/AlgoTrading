# Estratégia Godbot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera usando Bandas de Bollinger combinadas com médias móveis para detectar reversões e força de tendência.

## Lógica
- Funciona em um período de velas principal (padrão 30 minutos).
- Calcula as Bandas de Bollinger e uma EMA neste período.
- Separadamente, calcula uma DEMA em um período superior (padrão 1 dia) para determinar a tendência global.
- Fecha posições compradas quando o preço volta a cair abaixo da banda superior de Bollinger.
- Fecha posições vendidas quando o preço sobe novamente acima da banda inferior de Bollinger.
- Abre comprado quando o preço cruza acima da banda inferior enquanto tanto DEMA quanto EMA estão subindo.
- Abre vendido quando o preço cruza abaixo da banda superior enquanto tanto DEMA quanto EMA estão caindo.

## Parâmetros
- **Bollinger Period** – período das Bandas de Bollinger.
- **Bollinger Deviation** – multiplicador de largura para as bandas.
- **EMA Period** – período para o filtro de tendência EMA.
- **DEMA Period** – período para a DEMA no período superior.
- **Candle Type** – período usado para cálculos de Bandas de Bollinger e EMA.
- **DEMA Candle Type** – período superior usado para a DEMA.

## Notas
- Apenas uma posição é mantida de cada vez.
- A estratégia usa ordens de mercado para entradas e saídas.
- Os dados de DEMA devem se acumular antes que a negociação comece.
