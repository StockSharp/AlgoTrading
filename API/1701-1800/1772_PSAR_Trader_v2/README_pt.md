# Estratégia PSAR Trader v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia opera reversões de mercado usando o indicador Parabolic SAR. Uma posição é aberta quando o valor do SAR muda de lado em relação ao preço, sinalizando uma possível mudança de tendência. O algoritmo opera apenas dentro de uma janela de tempo especificada e pode, opcionalmente, fechar uma posição existente quando um sinal oposto aparecer.

## Lógica da estratégia
- **Indicador**: Parabolic SAR.
- **Compra** quando o SAR se move abaixo do fechamento da vela após ter estado acima da vela anterior.
- **Venda** quando o SAR se move acima do fechamento da vela após ter estado abaixo da vela anterior.
- Opera apenas no intervalo `StartHour`–`EndHour`.
- Quando `CloseOnOppositeSignal` está habilitado, uma posição é fechada se um sinal oposto ocorrer antes de abrir uma nova.

### Gestão de risco
Ao entrar em uma posição, a estratégia define níveis internos de take-profit e stop-loss. A posição é fechada automaticamente se o preço tocar qualquer um dos níveis.

## Parâmetros
| Nome | Descrição |
|------|-------------|
| `CandleType` | Período das velas utilizadas para operar. |
| `Step` | Passo de aceleração do Parabolic SAR. |
| `Maximum` | Fator de aceleração máximo do Parabolic SAR. |
| `TakeProfit` | Meta de lucro em unidades de preço. |
| `StopLoss` | Stop loss em unidades de preço. |
| `StartHour` | Hora de início das operações (0–23). |
| `EndHour` | Hora de encerramento das operações (0–23). |
| `CloseOnOppositeSignal` | Fechar a posição atual quando um sinal oposto aparecer. |

## Observações
Este exemplo demonstra o uso básico da API de alto nível com um popular indicador de reversão de tendência. Ajuste os parâmetros e a gestão de risco de acordo com o instrumento operado e as preferências pessoais.
