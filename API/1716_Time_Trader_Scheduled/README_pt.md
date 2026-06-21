# Estratégia de Negociação por Horário Programado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia envia ordens de mercado em um horário predefinido e as protege com níveis fixos de stop loss e take profit.

## Regras de Negociação

- Quando a hora atual atinge `Trade Hour:Trade Minute:Trade Second`, a estratégia dispara uma vez por sessão.
- Se `Allow Buy` estiver habilitado, uma posição comprada é aberta com o `Volume` especificado.
- Se `Allow Sell` estiver habilitado, uma posição vendida é aberta com o mesmo `Volume`.
- As ordens de proteção são gerenciadas via `StartProtection` usando valores em pontos para stop loss e take profit.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `Volume` | Tamanho da ordem. |
| `Take Profit (ticks)` | Distância do take profit desde a entrada em ticks. |
| `Stop Loss (ticks)` | Distância do stop loss desde a entrada em ticks. |
| `Allow Buy` | Habilitar operações compradas. |
| `Allow Sell` | Habilitar operações vendidas. |
| `Trade Hour` | Hora do dia para negociar (0-23). |
| `Trade Minute` | Minuto da hora para negociar (0-59). |
| `Trade Second` | Segundo do minuto para negociar (0-59). |
| `Candle Type` | Série de velas usada para rastrear o tempo, padrão são velas de 1 segundo. |

## Notas

A estratégia abre operações apenas uma vez por execução. Para negociar novamente, reinicie a estratégia ou ajuste o horário de negociação.
