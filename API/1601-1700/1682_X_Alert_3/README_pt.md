# Estratégia X-Alert 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica a lógica do especialista original **X_alert_3.mq4**. Monitora duas médias móveis com parâmetros configuráveis e produz um alerta informativo quando ocorre um cruzamento.

## Como funciona

1. Duas médias móveis são calculadas em cada vela concluída.
2. Um alerta de alta é gerado quando:
   - MA1 está acima de MA2 na vela atual.
   - MA1 está acima de MA2 na vela anterior.
   - MA1 estava abaixo de MA2 duas velas atrás.
3. Um alerta de baixa é gerado quando:
   - MA1 está abaixo de MA2 na vela atual.
   - MA1 está abaixo de MA2 na vela anterior.
   - MA1 estava acima de MA2 duas velas atrás.
4. A estratégia **não** abre ou fecha nenhuma posição. Ela apenas escreve mensagens no log.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Ma1Period` | Período da primeira média móvel. | `1` |
| `Ma1Type` | Tipo da primeira média móvel (Simple, Exponential, Smoothed, Weighted). | `Simple` |
| `Ma2Period` | Período da segunda média móvel. | `14` |
| `Ma2Type` | Tipo da segunda média móvel. | `Simple` |
| `PriceType` | Preço fonte usado nos cálculos (Close, Open, High, Low, Median, Typical, Weighted). | `Median` |
| `CandleType` | Série de velas usada para processamento. | período de `1 minuto` |

## Notas

- A implementação rastreia as últimas duas diferenças entre as médias móveis para detectar cruzamentos sem acessar diretamente valores históricos do indicador.
- Os alertas são escritos usando `AddInfoLog` para manter a estratégia sem efeitos colaterais.
- O parâmetro do MetaTrader `RunIntervalSeconds` não é necessário no StockSharp e foi omitido.
