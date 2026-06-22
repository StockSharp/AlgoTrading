# Estratégia Lossless MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos entre uma Média Móvel Simples (SMA) rápida e uma lenta.
Opcionalmente evita realizar perdas movendo posições perdedoras para o ponto de equilíbrio quando o sinal oposto aparece.

## Como Funciona

1. **Indicadores**
   - SMA rápida
   - SMA lenta
2. **Entradas**
   - **Comprado** quando `SMA rápida > SMA lenta` e a direção atual não é comprada.
   - **Vendido** quando `SMA rápida < SMA lenta` e a direção atual não é vendida.
   - Entradas adicionais são permitidas se `Close Losses` estiver desabilitado e o número de operações abertas estiver abaixo de `Max Deals`.
3. **Saídas**
   - Em um cruzamento oposto.
   - Se `Close Losses` estiver habilitado, a posição é fechada imediatamente.
   - Se `Close Losses` estiver desabilitado e a operação estiver com prejuízo, uma ordem limitada é colocada no preço de entrada para sair no ponto de equilíbrio.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `FastLength` | Período da SMA rápida. | `10` |
| `SlowLength` | Período da SMA lenta. | `30` |
| `MaxDeals` | Número máximo de operações simultâneas. | `5` |
| `CloseLosses` | Fechar operações com prejuízo imediatamente. | `true` |
| `Volume` | Volume da ordem. | `1` |
| `CandleType` | Candles para cálculos. | `1-minute` |

## Observações

A estratégia utiliza ordens de mercado para entradas e saídas. Quando `CloseLosses` está desabilitado, tenta proteger as posições colocando uma ordem limitada no preço de entrada em vez de fechar com prejuízo.
