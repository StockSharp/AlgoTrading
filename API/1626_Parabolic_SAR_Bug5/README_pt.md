# Estratégia Parabolic SAR Bug5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A Estratégia Parabolic SAR Bug5 opera reversões de preço detectadas pelo indicador Parabolic SAR. Ela abre uma posição comprada quando o preço cruza acima do SAR e uma posição vendida quando o preço cruza abaixo. A estratégia opcionalmente inverte a direção de negociação, fecha posições abertas em viradas do SAR, e suporta stop trailing, take profit e regras de stop loss.

## Regras de entrada

- **Comprar** quando o preço cruza acima do SAR e nenhuma posição comprada está aberta.
- **Vender** quando o preço cruza abaixo do SAR e nenhuma posição vendida está aberta.
- Se `Reverse` estiver habilitado, os sinais são invertidos.

## Regras de saída

- Fechar posição quando o sinal SAR oposto aparecer se `SarClose` estiver habilitado.
- Aplicar alvos fixos de stop loss e take profit.
- Se `Trailing` estiver habilitado, o stop loss segue o preço mais alto (para comprados) ou mais baixo (para vendidos) desde a entrada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `Step` | Fator de aceleração inicial para Parabolic SAR. |
| `Maximum` | Fator de aceleração máximo para Parabolic SAR. |
| `StopLossPoints` | Distância do stop loss em pontos. |
| `TakeProfitPoints` | Distância do take profit em pontos. |
| `Trailing` | Habilitar gerenciamento de stop trailing. |
| `TrailPoints` | Distância do stop trailing em pontos. |
| `Reverse` | Inverter direção de negociação. |
| `SarClose` | Fechar posição na mudança do SAR. |
| `CandleType` | Período das velas a processar. |
