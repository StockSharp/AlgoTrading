# Estratégia ADX Smoothed Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo

A estratégia opera com base em um Average Directional Index (ADX) de suavização dupla. Compara as linhas +DI e -DI suavizadas para detectar mudanças de tendência. Quando a linha +DI suavizada cruza acima da linha -DI suavizada, a estratégia entra em uma posição comprada. Quando a linha +DI suavizada cruza abaixo da linha -DI suavizada, abre uma posição vendida.

## Como Funciona

- Utiliza um indicador ADX com período configurável.
- Aplica duas passagens de suavização exponencial controladas pelos parâmetros **Alpha1** e **Alpha2**.
- Um sinal de compra ocorre quando o +DI suavizado anterior estava abaixo do -DI suavizado e o +DI suavizado atual está acima.
- Um sinal de venda ocorre no cruzamento oposto.
- Parâmetros opcionais permitem desabilitar operações compradas ou vendidas e controlar se as posições existentes podem ser fechadas quando um sinal oposto aparece.
- O gerenciamento de risco integrado define níveis de stop-loss e take-profit em pontos.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `AdxPeriod` | Período para o cálculo do ADX. |
| `Alpha1` | Primeiro coeficiente de suavização (0-1). |
| `Alpha2` | Segundo coeficiente de suavização (0-1). |
| `StopLoss` | Stop-loss em pontos. |
| `TakeProfit` | Take-profit em pontos. |
| `AllowBuy` | Habilitar entradas compradas. |
| `AllowSell` | Habilitar entradas vendidas. |
| `AllowCloseBuy` | Permitir fechar posições compradas em sinais de venda. |
| `AllowCloseSell` | Permitir fechar posições vendidas em sinais de compra. |
| `CandleType` | Período utilizado para o indicador. |

## Notas

- Apenas velas finalizadas são processadas.
- A estratégia usa a API de alto nível com vinculação de indicadores.
- Stop-loss e take-profit são gerenciados via `StartProtection`.
