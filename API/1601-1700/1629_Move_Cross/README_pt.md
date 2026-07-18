# Estratégia Move Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia demonstra uma conversão simplificada do script original `move_cross.mq4`. Ela emprega o indicador RAVI (Range Action Verification Index) calculado a partir de duas médias móveis simples para determinar a direção da tendência.

A estratégia compara os valores RAVI horários e diários:

- **Comprar** quando o RAVI horário é negativo enquanto o RAVI diário é positivo e crescente.
- **Vender** quando o RAVI horário é positivo enquanto o RAVI diário é negativo e decrescente.

As posições são abertas a mercado com alvo de lucro e stop loss opcionais.

## Parâmetros

| Nome       | Descrição                             | Padrão |
|------------|---------------------------------------|--------|
| TakeProfit | Alvo de lucro em pontos               | 50     |
| StopLoss   | Limite de perda em pontos             | 100    |

## Observações

- A estratégia usa dois pares de SMA (períodos 2 e 24) para calcular o RAVI em velas horárias e diárias.
- Destina-se a fins educacionais e pode exigir ajuste adicional para negociação ao vivo.
